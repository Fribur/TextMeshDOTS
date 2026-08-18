using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TextMeshDOTS.Authoring;
using TextMeshDOTS.HarfBuzz;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.Networking;
using UnityEngine.TextCore.LowLevel;

using Font = TextMeshDOTS.HarfBuzz.Font;


namespace TextMeshDOTS
{
    //[DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    partial class NativeFontLoaderSystem : SystemBase
    {
        EntityQuery changedFontLoadDescriptionQ;
        static MethodInfo sMethodInfo;
        static FieldInfo[] sFontLoadDescription;
        static object sFontRef;

        protected override void OnCreate()
        {
            var perThreadFontCaches = new NativeArray<UnsafeList<Font>>(JobsUtility.ThreadIndexCount, Allocator.Persistent);
            for (int i = 0; i < perThreadFontCaches.Length; i++)
            {
                perThreadFontCaches[i] = new UnsafeList<Font>(64, Allocator.Persistent);
            }
            EntityManager.CreateSingleton(new FontTable
            {
                faces = new NativeList<Face>(Allocator.Persistent),
                perThreadFontCaches = perThreadFontCaches,
                fontLookupKeys = new NativeList<FontLookupKey>(Allocator.Persistent),
                fontLookupKeyToFaceIndexMap = new NativeHashMap<FontLookupKey, int>(64, Allocator.Persistent),
                fontLookupKeyToNamedVariationIndexMap = new NativeHashMap<FontLookupKey, int>(64, Allocator.Persistent),
                useSlug = TextMeshDOTSSettings.Loaded?.useGPURendering ?? false,
            });

            changedFontLoadDescriptionQ = SystemAPI.QueryBuilder()
                .WithAll<FontLoadDescription>()
                .Build();
            changedFontLoadDescriptionQ.SetChangedVersionFilter(ComponentType.ReadWrite<FontLoadDescription>());

            RequireForUpdate(changedFontLoadDescriptionQ);

            GetSystemFontsMethod();
        }

        //[BurstCompile]
        protected override void OnUpdate()
        {
            if (changedFontLoadDescriptionQ.IsEmpty)
                return;

            var changedFontLoadDescriptionBuffer = changedFontLoadDescriptionQ.GetSingletonBuffer<FontLoadDescription>();
            var fontTable = SystemAPI.GetSingletonRW<FontTable>().ValueRW;
            CompleteDependency();

            //copy to nativeArray because LoadFont would invalidate DynamicBuffer due to structural changes
            var fontLoadDescriptions = CollectionHelper.CreateNativeArray<FontLoadDescription>(changedFontLoadDescriptionBuffer.AsNativeArray(), WorldUpdateAllocator);

            // avoid opening the same collection file multiple times in one update
            var processedPaths = new NativeHashSet<FixedString512Bytes>(8, WorldUpdateAllocator);

            for (int i = 0, ii = fontLoadDescriptions.Length; i < ii; i++)
            {
                var fontLoadDescription = fontLoadDescriptions[i];
                if (fontTable.fontLookupKeyToFaceIndexMap.ContainsKey(fontLoadDescription.fontLookupKey))
                    continue;

                LoadFont(fontLoadDescription, ref CheckedStateRef, ref fontTable, ref processedPaths);
            }
        }

        protected override void OnDestroy()
        {
            SystemAPI.GetSingletonRW<FontTable>().ValueRW.TryDispose(Dependency).Complete();
        }

        void GetSystemFontsMethod()
        {
            Assembly textCoreFontEngineModule = default;
#if UNITY_6000_4_OR_NEWER
            var loadedAssemblies = CurrentAssemblies.GetLoadedAssemblies();
            var assemblyCount = loadedAssemblies.Count;
#else
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblyCount = loadedAssemblies.Length;
#endif

            for (int i = 0; i < assemblyCount; i++)
            {
                var loadedAssembly = loadedAssemblies[i];
                if (loadedAssembly.GetName().Name == "UnityEngine.TextCoreFontEngineModule")
                {
                    textCoreFontEngineModule = loadedAssembly;
                    FontEngine.GetSystemFontNames();
                    UnityEngine.Font.GetPathsToOSFonts();
                    //Debug.Log($"Found UnityEngine.TextCoreFontEngineModule in loaded assemblies: {loadedAssembly.FullName}");
                    break;
                }
            }
            var fontReferenceType = textCoreFontEngineModule.GetType("UnityEngine.TextCore.LowLevel.FontReference");
            sFontLoadDescription = fontReferenceType.GetFields();
            sFontRef = Activator.CreateInstance(fontReferenceType);

            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            sMethodInfo = typeof(FontEngine).GetMethod("TryGetSystemFontReference", bindingFlags);
            //MakeDelegate<sFontLoadDescription>(sMethodInfo);
        }
        public static Func<string, string, object, bool> MakeDelegate<U>(MethodInfo methodInfo)
        {
            var f = (Func<string, string, U, bool>)Delegate.CreateDelegate(typeof(Func<string, string, U, bool>), methodInfo);
            return (a, b, c) => f(a, b, (U)c);
        }
       
        void LoadFont(FontLoadDescription fontLoadDescription, ref SystemState state, ref FontTable fontTable, ref NativeHashSet<FixedString512Bytes> processedPaths)
        {
            Blob blob;

            if (fontLoadDescription.isSystemFont)
            {
                //loading rules: https://www.high-logic.com/fontcreator/manual15/fonttype.html

                var typeographicFamilyDataMissing = (fontLoadDescription.typographicFamily.IsEmpty || fontLoadDescription.typographicSubfamily.IsEmpty);
                var family = typeographicFamilyDataMissing ? fontLoadDescription.fontFamily : fontLoadDescription.typographicFamily;
                var subFamily = typeographicFamilyDataMissing ? fontLoadDescription.fontSubFamily : fontLoadDescription.typographicSubfamily;
                object[] args = new object[] { family.ToString(), subFamily.ToString(), sFontRef };
                var systemFontFound = (bool)sMethodInfo.Invoke(null, args);
                var result = args[2];

                //if (!TryGetSystemFontReference(family.ToString(), subFamily.ToString(), out UnityFontReference unityFontReference))
                if (!systemFontFound)
                {
                    //Debug.Log($"Could not find system font {fontLoadDescription.fontFamily} {fontLoadDescription.fontSubFamily}");
                    return;
                }
                //Debug.Log($"Found {fieldInfos[0].GetValue(result)} {fieldInfos[1].GetValue(result)} {fieldInfos[2].GetValue(result)} {fieldInfos[3].GetValue(result)}");
                var systemFontPath = (string)sFontLoadDescription[3].GetValue(result);
                if (!processedPaths.Add(systemFontPath))
                    return; // all faces from system font file where already loaded this update, skip.
                blob = new Blob(systemFontPath);
            }
            else if (fontLoadDescription.streamingAssetLocationValidated)
            {
                if (!processedPaths.Add(fontLoadDescription.filePath))
                    return; // all faces from this file were already loaded this update, skip.
                if (!TryCreateStreamingAssetBlob(fontLoadDescription.filePath.ToString(), out blob))
                    return;
            }
            else
            {
                var fontAssetPath = fontLoadDescription.filePath.ToString();
                if (!File.Exists(fontAssetPath))
                {
                    //Debug.Log($"Could not find font in {fontAssetPath}");
                    return;
                }
                if (!processedPaths.Add(fontLoadDescription.filePath))
                    return; // all faces from this file were already loaded this update, skip.
                blob = new Blob(fontAssetPath);
            }
            blob.MakeImmutable();//is this neccessary considering we dispose the blob in next instruction?

            // in case font file is a collection font, chances are that none of the faces have been loaded yet
            // while file is open, load them all to avoid opening file again
            var fontLoadDescriptions = new NativeList<FontLoadDescription>(blob.FaceCount, Allocator.Temp);
            var language = Language.English;
            TextHelper.GetFaceInfo(blob, language, fontLoadDescription, fontLoadDescriptions);

            for (int i = 0, ii = fontLoadDescriptions.Length; i < ii; i++)
            {
                var tempFontLoadDescription = fontLoadDescriptions[i];
                var fontLookupKey = tempFontLoadDescription.fontLookupKey;
                if (!fontTable.fontLookupKeyToFaceIndexMap.ContainsKey(fontLookupKey))
                {
                    var id = fontTable.fontLookupKeyToFaceIndexMap.Count;
                    fontTable.fontLookupKeys.Add(fontLookupKey);
                    fontTable.fontLookupKeyToFaceIndexMap.Add(fontLookupKey, id);
                    var face = new Face(blob, tempFontLoadDescription.faceIndexInFile);
                    face.MakeImmutable();
                    fontTable.faces.Add(face);

                    for (int k = 0, kk = fontTable.perThreadFontCaches.Length; k < kk; k++)
                    {
                        var list = fontTable.perThreadFontCaches[k];
                        list.Add(default);
                        fontTable.perThreadFontCaches[k] = list;
                    }

                    //setup lookup of named variable instance
                    if (face.HasVarData)
                    {
                        var axisCount = (int)face.AxisCount;

                        //fetch a list of all variation axis
                        Span<AxisInfo> axisInfos = stackalloc AxisInfo[axisCount];
                        face.GetAxisInfos(0, 0, ref axisInfos, out _);
                        AxisInfo axisInfo;
                        float coord;

                        //fetch a list of named variants                        
                        //Debug.Log($"found {axisCount} variation axis for font {fontLoadDescription.fontFamily} {fontLoadDescription.fontSubFamily}, {face.NamedInstanceCount} named instances");
                        Span<float> coords = stackalloc float[axisCount];
                        for (int k = 0, kk = (int)face.NamedInstanceCount; k < kk; k++)
                        {
                            face.GetNamedInstanceDesignCoords(k, ref coords, out uint coordLength);
                            var variableFontLookupKey = fontLookupKey;
                            for (int f = 0, ff = (int)coordLength; f < ff; f++)
                            {
                                //axisInfos and coords should be aligned in length and order
                                axisInfo = axisInfos[f];
                                coord = coords[f];
                                switch (axisInfo.axisTag)
                                {
                                    case AxisTag.WIDTH:
                                        variableFontLookupKey.width = coord; break;
                                    case AxisTag.WEIGHT:
                                        variableFontLookupKey.weight = coord; break;
                                    case AxisTag.ITALIC:
                                        variableFontLookupKey.isItalic = (int)coord == 1; break;
                                    case AxisTag.SLANT:
                                        variableFontLookupKey.slant = coord; break;
                                }
                                //Debug.Log($"Add FontLookupKey {variableFontLookupKey} for variation axis: {axisInfo.axisTag} {face.GetName(axisInfo.nameID, language)}, value = {coord}");
                            }
                            fontTable.fontLookupKeyToNamedVariationIndexMap.Add(variableFontLookupKey, k);
                        }
                    }
                }
            }
            //blob can be disposed here, face and font are disposed at world shutdown via FontTable.TryDispose 
            blob.Dispose();
        }

         // On Android StreamingAssets are inside the APK; per Unity docs only UnityWebRequest can read them
        static bool TryCreateStreamingAssetBlob(string relativePath, out Blob blob)
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            var path = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(path))
            {
                blob = default;
                return false;
            }
            blob = new Blob(path);
            return true;
#else
            var source = Path.Combine(Application.streamingAssetsPath, relativePath);
            using (var request = UnityWebRequest.Get(source))
            {
                request.SendWebRequest();
                while (!request.isDone) { }
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"TextMeshDOTS: failed to read '{source}': {request.error}");
                    blob = default;
                    return false;
                }
                var nativeData = request.downloadHandler.nativeData;
                unsafe
                {
                    byte* ptr = (byte*)nativeData.GetUnsafeReadOnlyPtr();
                    blob = new Blob(ptr, (uint)nativeData.Length, MemoryMode.DUBLICATE);
                }
                return true;
            }
#endif
        }
    }
}