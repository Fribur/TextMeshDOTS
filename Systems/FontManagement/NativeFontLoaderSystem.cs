using System.IO;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.HarfBuzz.Bitmap;
using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using static TextMeshDOTS.TextCoreExtensions;
using Font = TextMeshDOTS.HarfBuzz.Font;


namespace TextMeshDOTS.TextProcessing
{
    //[DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial struct NativeFontLoaderSystem : ISystem
    {
        EntityQuery textRendererQ, changedFontRequestQ, fontstateQ;
        EntityArchetype nativeFontDataArchetype, fontStateArchetype;
        DrawDelegates drawFunctions;
        PaintDelegates paintFunctions;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            nativeFontDataArchetype = TextMeshDOTSArchetypes.GetNativeFontDataArchetype(ref state);

            fontStateArchetype = TextMeshDOTSArchetypes.GetFontStateArchetype(ref state);
            state.EntityManager.CreateEntity(fontStateArchetype);

            var faceIndexToFontEntityMap = new NativeHashMap<int, Entity>(64, Allocator.Persistent);
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
                faceIndexToFontEntityMap = faceIndexToFontEntityMap,
                fontAssetRefToFaceIndexMap = fontAssetRefToFaceIndexMap,
            });

            drawFunctions = new DrawDelegates(true);
            paintFunctions = new PaintDelegates(true);

            fontstateQ = SystemAPI.QueryBuilder()
                .WithAll<FontState>()
                .Build();

            textRendererQ = SystemAPI.QueryBuilder()
                .WithAll<FontBlobReference>()
                .WithAll<TextRenderControl>()
                .WithPresent<MaterialMeshInfo>()
                .Build();

            changedFontRequestQ = SystemAPI.QueryBuilder()
                .WithAll<FontRequest>()
                .Build();
            changedFontRequestQ.SetChangedVersionFilter(ComponentType.ReadWrite<FontRequest>());

            
            state.RequireForUpdate(fontstateQ);
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
            bool newFontsAdded = false;

            for (int i = 0, ii = fontRequests.Length; i < ii; i++)
            {
                var fontRequest = fontRequests[i];
                if (!(fontTable.fontAssetRefToFaceIndexMap.TryGetValue(fontRequest.fontAssetRef, out int id) && fontTable.faceIndexToFontEntityMap.ContainsKey(id)))
                {
                    newFontsAdded = true;
                    LoadFont(fontRequest, ref state, ref fontTable);
                }
            }

            //To-Do: determine wich fonts are actually used by TextRender. Unload all others. Likely only possible after running ShapeJob?

            //even if no new fonts are found, the backer will reset all MaterialMeshInfo (disable it, values are zero
            //-->run EnableAndValidateMaterialMeshInfoJob (same job that run after registering new materials)
            if (!newFontsAdded) 
            {                
                //validate MaterialMeshInfo (TextRender connected to correct FontAssets?)
                var dynamicFontAssetLookup = SystemAPI.GetComponentLookup<DynamicFontAsset>(false);
                var validateMaterialMeshInfoJob = new EnableAndValidateMaterialMeshInfoJob
                {                    
                    fontAssetRefToFaceIndexMap = fontTable.fontAssetRefToFaceIndexMap,
                    faceIndexToFontEntityMap = fontTable.faceIndexToFontEntityMap,
                    dynamicFontAssetLookup = dynamicFontAssetLookup,
                };
                state.Dependency = validateMaterialMeshInfoJob.ScheduleParallel(textRendererQ, state.Dependency);
                return;
            }

            var fontStateEntity = fontstateQ.GetSingletonEntity();
            if (!SystemAPI.HasComponent<FontsDirtyTag>(fontStateEntity))
                state.EntityManager.AddComponent<FontsDirtyTag>(fontStateEntity);
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
                }
            }
            
            var face = new Face(blob.ptr, 0);
            var fontAssetMetadata = new FontAssetMetadata { family = family, subfamily = subFamily, faceIndex = fontTable.fontAssetRefToFaceIndexMap.Count };

            //initialize texture. To save space, review how to initialize it with size 0
            //(as done by TextCore), and only increase once needed
            DynamicFontAsset dynamicFontAsset;
            AtlasData atlasData;

            if (face.HasCOLR() || face.HasColorBitmap())
            {
                atlasData = new AtlasData
                {
                    atlasHeight = 2048,
                    atlasWidth = 2048,
                    padding = 8,                //10% of atlas height or width
                    samplingPointSize = fontRequest.samplingPointSizeBitmap,    //size of font (in pixel) in atlas
                };
                var texture2D = new Texture2D(atlasData.atlasWidth, atlasData.atlasHeight, TextureFormat.ARGB32,false);
                var textureData = texture2D.GetRawTextureData<ColorARGB>();
                Blending.SetTransparent(textureData);
                dynamicFontAsset = new DynamicFontAsset { texture = texture2D, textureType = TextureType.ARGB };
            }
            else
            {
                atlasData = new AtlasData
                {
                    atlasHeight = 2048,
                    atlasWidth = 2048,
                    padding = fontRequest.samplingPointSizeSDF / 6,  //samplingPointSizeSDF is clamped to 64..96, so padding will be clamped to 10..16
                    samplingPointSize = fontRequest.samplingPointSizeSDF,  //size of font (in pixel) in atlas
                };
                var texture2D = new Texture2D(atlasData.atlasWidth, atlasData.atlasHeight, TextureFormat.Alpha8, false);
                var rawTextureData = texture2D.GetRawTextureData<byte>();

                //initialize to black
                for (int i = 0; i < rawTextureData.Length; i++)
                    rawTextureData[i] = 0;
                texture2D.Apply();
                dynamicFontAsset = new DynamicFontAsset { texture = texture2D, textureType = TextureType.SDF};
            }

            var drawAndPaintFunctions = new DrawAndPaintFunctions
            {
                drawFunctions = drawFunctions,
                paintFunctions = paintFunctions,
            };

            var fontEntity = state.EntityManager.CreateEntity(nativeFontDataArchetype);
            state.EntityManager.SetComponentData(fontEntity, fontRequest.fontAssetRef);
            state.EntityManager.SetComponentData(fontEntity, fontAssetMetadata);
            state.EntityManager.SetComponentData(fontEntity, atlasData);
            state.EntityManager.AddComponentData(fontEntity, dynamicFontAsset);
            state.EntityManager.AddComponentData(fontEntity, drawAndPaintFunctions);

            var freeGlyphRects = state.EntityManager.GetBuffer<FreeGlyphRects>(fontEntity);
            NativeAtlas.InitializeFreeGlyphRects(ref freeGlyphRects, atlasData.atlasWidth, atlasData.atlasHeight);

            if (fontTable.fontAssetRefToFaceIndexMap.TryGetValue(fontRequest.fontAssetRef, out var id))
            {
                fontTable.faceIndexToFontEntityMap[id] = fontEntity;
            }
            else
            {
                id = fontTable.fontAssetRefToFaceIndexMap.Count;
                fontTable.fontAssetRefToFaceIndexMap.Add(fontRequest.fontAssetRef, id);
                fontTable.faceIndexToFontEntityMap.Add(id, fontEntity);
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