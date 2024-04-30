using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using static Unity.Entities.SystemAPI;

namespace TextMeshDOTS.Rendering.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    [UpdateBefore(typeof(TextRenderingDispatchSystem))]
    public partial struct TextRenderingUpdateSystem : ISystem
    {
        EntityQuery m_singleFontQuery;
        EntityQuery m_multiFontQuery;
        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_singleFontQuery = QueryBuilder()
                     .WithAll<RenderGlyph>()
                     .WithAllRW<RenderBounds>()
                     .WithAllRW<TextRenderControl>()
                     .WithAllRW<MaterialMeshInfo>()
                     .WithAbsent<FontMaterialSelectorForGlyph>()
                     .Build();
            m_singleFontQuery.AddChangedVersionFilter(typeof(TextRenderControl));

            m_multiFontQuery = QueryBuilder()
                     .WithAll<RenderGlyph>()
                     .WithAll<FontMaterialSelectorForGlyph>()
                     .WithAll<AdditionalFontMaterialEntity>()
                     .WithAllRW<RenderBounds>()
                     .WithAllRW<TextRenderControl>()
                     .WithAllRW<MaterialMeshInfo>()
                     .Build();

            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!TryGetSingletonEntity<TextStatisticsTag>(out Entity textStats))
                return;            

            state.EntityManager.SetComponentData(textStats, new GlyphCountThisFrame { glyphCount = 0 });
            state.EntityManager.SetComponentData(textStats, new MaskCountThisFrame { maskCount = 1 });  // Zero reserved for no mask

            state.Dependency = new UpdateSingleFontJob
            {
            }.ScheduleParallel(m_singleFontQuery, state.Dependency);

            state.Dependency = new UpdateMultiFontJobChunk
            {
                additionalEntityHandle = GetBufferTypeHandle<AdditionalFontMaterialEntity>(true),
                boundsLookup = GetComponentLookup<RenderBounds>(false),
                controlLookup = GetComponentLookup<TextRenderControl>(false),
                entityHandle = GetEntityTypeHandle(),
                glyphHandle = GetBufferTypeHandle<RenderGlyph>(true),
                glyphMaskLookup = GetBufferLookup<RenderGlyphMask>(false),
                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
                materialMeshInfoLookup = GetComponentLookup<MaterialMeshInfo>(false),
                selectorHandle = GetBufferTypeHandle<FontMaterialSelectorForGlyph>(true)
            }.ScheduleParallel(m_multiFontQuery, state.Dependency);
        }
    }
}

