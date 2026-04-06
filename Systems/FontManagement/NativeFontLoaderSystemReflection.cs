using System;
using System.IO;
using System.Reflection;
using TextMeshDOTS.HarfBuzz;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Scenes;
using UnityEngine;
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
        MethodInfo methodInfo;
        FieldInfo[] fontReference;
        object m_fontRef;

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

            for (int i = 0, ii = fontLoadDescriptions.Length; i < ii; i++)
            {
                var fontReference = fontLoadDescriptions[i];
                if (!fontTable.fontLookupKeyToFaceIndexMap.ContainsKey(fontReference.fontLookupKey))
                    LoadFont(fontReference, ref CheckedStateRef, ref fontTable);
            }
        }

        protected override void OnDestroy()
        {
            SystemAPI.GetSingletonRW<FontTable>().ValueRW.TryDispose(Dependency).Complete();
        }

        void GetSystemFontsMethod()
        {
            Assembly textCoreFontEngineModule = default;
            foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
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
            fontReference = fontReferenceType.GetFields();
            var m_fontRef = Activator.CreateInstance(fontReferenceType);

            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            methodInfo = typeof(FontEngine).GetMethod("TryGetSystemFontReference", bindingFlags);
            //MakeDelegate<fontReference>(methodInfo);
        }
        public static Func<string, string, object, bool> MakeDelegate<U>(MethodInfo methodInfo)
        {
            var f = (Func<string, string, U, bool>)Delegate.CreateDelegate(typeof(Func<string, string, U, bool>), methodInfo);
            return (a, b, c) => f(a, b, (U)c);
        }
        void LoadFont(FontLoadDescription fontLoadDescription, ref SystemState state, ref FontTable fontTable)
        {
            Blob blob;
            string fontAssetPath;

            if (fontLoadDescription.isSystemFont)
            {
                //loading rules: https://www.high-logic.com/fontcreator/manual15/fonttype.html

                var typeographicFamilyDataMissing = (fontLoadDescription.typographicFamily.IsEmpty || fontLoadDescription.typographicSubfamily.IsEmpty);
                var family = typeographicFamilyDataMissing ? fontLoadDescription.fontFamily : fontLoadDescription.typographicFamily;
                var subFamily = typeographicFamilyDataMissing ? fontLoadDescription.fontSubFamily : fontLoadDescription.typographicSubfamily;
                object[] args = new object[] { family.ToString(), subFamily.ToString(), m_fontRef };
                var systemFontFound = (bool)methodInfo.Invoke(null, args);
                var result = args[2];

                //if (!TryGetSystemFontReference(family.ToString(), subFamily.ToString(), out UnityFontReference unityFontReference))
                if (!systemFontFound)
                {
                    //Debug.Log($"Could not find system font {fontReference.fontFamily} {fontReference.fontSubFamily}");
                    return;
                }
                //Debug.Log($"Found {fieldInfos[0].GetValue(result)} {fieldInfos[1].GetValue(result)} {fieldInfos[2].GetValue(result)} {fieldInfos[3].GetValue(result)}");
                fontAssetPath = (string)this.fontReference[3].GetValue(result);
            }
            else
            {
                if (fontLoadDescription.streamingAssetLocationValidated)
                    fontAssetPath = Path.Combine(Application.streamingAssetsPath, fontLoadDescription.filePath.ToString());
                else
                    fontAssetPath = fontLoadDescription.filePath.ToString();

                if (!File.Exists(fontAssetPath))
                {
                    //Debug.Log($"Could not find font in {fontAssetPath}");
                    return;
                }
            }

            blob = new Blob(fontAssetPath);
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
                        //Debug.Log($"found {axisCount} variation axis for font {fontReference.fontFamily} {fontReference.fontSubFamily}, {face.NamedInstanceCount} named instances");
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
                                //Debug.Log($"Add FontLookupKey {tempFontLookupKey} for variation axis: {axisInfo.axisTag} {face.GetName(axisInfo.nameID, language)}, value = {coord}");
                            }
                            fontTable.fontLookupKeyToNamedVariationIndexMap.Add(variableFontLookupKey, k);
                        }
                    }
                }
            }
            //blob can be disposed here, face and font are disposed at world shutdown via FontTable.TryDispose 
            blob.Dispose();
        }
    }
}