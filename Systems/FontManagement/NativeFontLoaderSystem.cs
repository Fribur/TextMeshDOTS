using System.IO;
using TextMeshDOTS.HarfBuzz;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using static TextMeshDOTS.TextCoreExtensions;
using Font = TextMeshDOTS.HarfBuzz.Font;


namespace TextMeshDOTS
{
    //[DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    partial struct NativeFontLoaderSystem : ISystem
    {
        EntityQuery changedFontRequestQ;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var fontAssetRefToFaceIndexMap = new NativeHashMap<FontAssetRef, int>(64, Allocator.Persistent);
            var perThreadFontCaches = new NativeArray<UnsafeList<Font>>(Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndexCount, Allocator.Persistent);
            for (int i = 0; i < perThreadFontCaches.Length; i++)
            {
                perThreadFontCaches[i] = new UnsafeList<Font>(64, Allocator.Persistent);
            }
            state.EntityManager.CreateSingleton(new FontTable
            {
                faces = new NativeList<Face>(Allocator.Persistent),
                perThreadFontCaches = perThreadFontCaches,
                fontAssetRefs = new NativeList<FontAssetRef>(Allocator.Persistent),
                fontAssetRefToFaceIndexMap = fontAssetRefToFaceIndexMap,               
            });

            changedFontRequestQ = SystemAPI.QueryBuilder()
                .WithAll<FontRequest>()
                .Build();
            changedFontRequestQ.SetChangedVersionFilter(ComponentType.ReadWrite<FontRequest>());
            
            state.RequireForUpdate(changedFontRequestQ);
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (changedFontRequestQ.IsEmpty)
                return;

            var changedFontRequestBuffer = changedFontRequestQ.GetSingletonBuffer<FontRequest>();
            var fontTable = SystemAPI.GetSingletonRW<FontTable>().ValueRW;
            state.CompleteDependency();

            //copy to nativeArray because LoadFont would invalidate DynamicBuffer due to structural changes
            var fontRequests = CollectionHelper.CreateNativeArray<FontRequest>(changedFontRequestBuffer.AsNativeArray(), state.WorldUpdateAllocator);            

            for (int i = 0, ii = fontRequests.Length; i < ii; i++)
            {
                var fontRequest = fontRequests[i];
                if (!fontTable.fontAssetRefToFaceIndexMap.ContainsKey(fontRequest.fontAssetRef))
                {
                    LoadFont(fontRequest, ref state, ref fontTable);
                }
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<FontTable>().ValueRW.TryDispose(state.Dependency).Complete();
        }
        void LoadFont(FontRequest fontRequest, ref SystemState state, ref FontTable fontTable)
        {
            Blob blob;
            var typeographicFamilyDataMissing = (fontRequest.typographicFamily.IsEmpty || fontRequest.typographicSubfamily.IsEmpty);
            var family = typeographicFamilyDataMissing ? fontRequest.fontFamily : fontRequest.typographicFamily;
            var subFamily = typeographicFamilyDataMissing ? fontRequest.fontSubFamily : fontRequest.typographicSubfamily;

            if (fontRequest.useSystemFont)
            {
                //loading rules: https://www.high-logic.com/fontcreator/manual15/fonttype.html
                if (!TryGetSystemFontReference(family.ToString(), subFamily.ToString(), out UnityFontReference unityFontReference))
                {
                    Debug.Log($"Could not find system font {family} {subFamily}");
                    return;
                }
                else
                {
                    blob = new Blob(unityFontReference.filePath);
                }                
            }
            else
            {
                string fontPath;
                if (fontRequest.streamingAssetLocationValidated)
                    fontPath = Path.Combine(Application.streamingAssetsPath, fontRequest.fontAssetPath.ToString());
                else
                    fontPath = fontRequest.fontAssetPath.ToString();

                if (!File.Exists(fontPath))
                {
                    Debug.Log($"Could not find font in {fontPath}");
                    return;
                }
                else
                {
                    blob = new Blob(fontPath);
                    blob.MakeImmutable();
                }
            }             

            if (!fontTable.fontAssetRefToFaceIndexMap.ContainsKey(fontRequest.fontAssetRef))
            {
                var id = fontTable.fontAssetRefToFaceIndexMap.Count;
                fontTable.fontAssetRefs.Add(fontRequest.fontAssetRef);
                fontTable.fontAssetRefToFaceIndexMap.Add(fontRequest.fontAssetRef, id);
                var face = new Face(blob.ptr, 0);
                face.MakeImmutable();
                fontTable.faces.Add(face);
                for (int i = 0; i < fontTable.perThreadFontCaches.Length; i++)
                {
                    var list = fontTable.perThreadFontCaches[i];
                    list.Add(default);
                    fontTable.perThreadFontCaches[i] = list;
                }
            }

            //blob can be disposed here, face and font are disposed at world shutdown via FontTable.TryDispose 
            blob.Dispose();
        }        
    }
}