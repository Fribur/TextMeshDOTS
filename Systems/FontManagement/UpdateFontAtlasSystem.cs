using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Profiling;
using Unity.Jobs;
using TextMeshDOTS.HarfBuzz;
using Unity.Burst;
using TextMeshDOTS.Rendering;

namespace TextMeshDOTS.TextProcessing
{
    //[DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(TextRenderingUpdateSystem))]
    partial struct UpdateFontAtlasSystem : ISystem
    {
        EntityQuery fontEntityQ;

        static readonly ProfilerMarker marker = new ProfilerMarker("COLR");
        static readonly ProfilerMarker marker2 = new ProfilerMarker("SDF");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            fontEntityQ = SystemAPI.QueryBuilder()
                .WithAll<AtlasData>()
                .WithAll<MissingGlyphs>()
                .WithAll<UsedGlyphs>()
                .WithAll<UsedGlyphRects>()
                .WithAll<FreeGlyphRects>()
                .WithAll<DynamicFontAsset>()
                .Build();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(fontEntityQ.IsEmpty) 
                return;
            
            var fontsRequiringUpdate = new NativeList<Entity>(16, Allocator.TempJob);
            foreach (var (fontAssetRef, fontAssetMetadata, missingGlyphs, entity) in SystemAPI.Query<FontAssetRef, FontAssetMetadata, DynamicBuffer<MissingGlyphs>>()
                .WithAll<FontAssetRef>()
                .WithAll<FontAssetMetadata>()
                .WithAll<AtlasData>()
                .WithAll<MissingGlyphs>()
                .WithEntityAccess())
            {
                if (missingGlyphs.Length > 0)
                {
                    fontsRequiringUpdate.Add(entity);
                    //Debug.Log($"Request to add {missingGlyphs.Length} glyphs to texture of {fontAssetMetadata.family} {fontAssetMetadata.subfamily}");
                }
            }
            if(fontsRequiringUpdate.IsEmpty)
            {
                fontsRequiringUpdate.Dispose();
                return;
            }

            state.Dependency.Complete();
            var placedGlyphs = new NativeList<GlyphTable.Key> (1024, Allocator.TempJob);
            var fontTable = SystemAPI.GetSingleton<FontTable>();
            var glyphTable = SystemAPI.GetSingletonRW<GlyphTable>().ValueRW;
            var fontAssetMetadataLookup = SystemAPI.GetComponentLookup<FontAssetMetadata>(true); //temporary link between FontTable and Font Entities
            var atlasDataLookup = SystemAPI.GetComponentLookup<AtlasData>(true);
            var drawAndPaintFunctionsLookup = SystemAPI.GetComponentLookup<DrawAndPaintFunctions>(true);
            var missingGlyphsLookup = SystemAPI.GetBufferLookup<MissingGlyphs>(false);
            var usedGlyphsLookup = SystemAPI.GetBufferLookup<UsedGlyphs>(false);
            var usedGlyphRectsLookup = SystemAPI.GetBufferLookup<UsedGlyphRects>(false);
            var freeGlyphRectsLookup = SystemAPI.GetBufferLookup<FreeGlyphRects>(false);
            var dynamicFontAssetsLookup = SystemAPI.GetComponentLookup<DynamicFontAsset>(false);

            for (int i = 0, ii = fontsRequiringUpdate.Length; i < ii; i++)
            {
                var fontEntity = fontsRequiringUpdate[i];                

                var getGlyphRectsJob = new GetGlyphRectsJob()
                {
                    placedGlyphs = placedGlyphs,

                    fontEntity = fontEntity,
                    fontTable = fontTable,
                    glyphTable = glyphTable,
                    fontAssetMetadataLookup = fontAssetMetadataLookup, //temporary link between FontTable and Font Entities
                    atlasDataLookup = atlasDataLookup,                    

                    missingGlyphsBuffer = missingGlyphsLookup,
                    usedGlyphsBuffer = usedGlyphsLookup,
                    usedGlyphRectsBuffer = usedGlyphRectsLookup,
                    freeGlyphRectsBuffer = freeGlyphRectsLookup,
                };
                state.Dependency = getGlyphRectsJob.Schedule(state.Dependency);

                var dynamicFontAsset = dynamicFontAssetsLookup[fontEntity];
                if (dynamicFontAsset.textureType == TextureType.SDF)
                {
                    var updateAtlasTextureJob = new UpdateSDFAtlasTextureJob()
                    {
                        //this managed call to texture object is reason why we cannot BURST compile the update method of this system
                        textureData = dynamicFontAsset.texture.Value.GetRawTextureData<byte>(), 
                        
                        fontEntity = fontEntity,
                        fontTable = fontTable,
                        glyphTable = glyphTable,
                        fontAssetMetadataLookup = fontAssetMetadataLookup, //temporary link between FontTable and Font Entities
                        placedGlyphs = placedGlyphs,
                        atlasDataLookup = atlasDataLookup,
                        drawAndPaintFunctionsLookup = drawAndPaintFunctionsLookup,
                        usedGlyphsBuffer = usedGlyphsLookup,
                        usedGlyphRectsBuffer = usedGlyphRectsLookup,
                        marker = marker2,
                    };
                    state.Dependency = updateAtlasTextureJob.Schedule(placedGlyphs, 1, state.Dependency);
                }
                else if (dynamicFontAsset.textureType == TextureType.ARGB)
                {
                    var updateAtlasTextureJob = new UpdateBitmapAtlasTextureJob()
                    {
                        //this managed call to texture object is reason why we cannot BURST compile the update method of this system
                        textureData = dynamicFontAsset.texture.Value.GetRawTextureData<ColorARGB>(),

                        fontEntity = fontEntity,
                        fontTable = fontTable,
                        glyphTable = glyphTable,
                        fontAssetMetadataLookup = fontAssetMetadataLookup, //temporary link between FontTable and Font Entities
                        placedGlyphs = placedGlyphs,
                        atlasDataLookup = atlasDataLookup,
                        drawAndPaintFunctionsLookup = drawAndPaintFunctionsLookup,
                        usedGlyphsBuffer = usedGlyphsLookup,
                        usedGlyphRectsBuffer = usedGlyphRectsLookup,
                        marker = marker,
                    };
                    state.Dependency = updateAtlasTextureJob.Schedule(placedGlyphs, 1, state.Dependency);
                }

                state.Dependency.Complete(); //To-Do: remove sync point. 

                dynamicFontAsset.texture.Value.Apply();
                placedGlyphs.Clear();
            }

            placedGlyphs.Dispose(state.Dependency);            
            fontsRequiringUpdate.Dispose(state.Dependency);
        }
    }
}