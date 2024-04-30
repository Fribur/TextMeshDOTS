using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;

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
            state.EntityManager.AddComponent(state.SystemHandle, TextMeshDOTSArchetypes.GetTextStatisticsTypeset());

            m_singleFontQuery = SystemAPI.QueryBuilder()
                     .WithAll<RenderGlyph>()
                     .WithAllRW<RenderBounds>()
                     .WithAllRW<TextRenderControl>()
                     .WithAllRW<MaterialMeshInfo>()
                     .WithAbsent<FontMaterialSelectorForGlyph>()
                     .Build();
            m_singleFontQuery.AddChangedVersionFilter(ComponentType.ReadOnly<TextRenderControl>());

            m_multiFontQuery = SystemAPI.QueryBuilder()
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
            Entity textStats = SystemAPI.GetSingletonEntity<TextStatisticsTag>();

            state.EntityManager.SetComponentData(textStats, new GlyphCountThisFrame { glyphCount = 0 });
            state.EntityManager.SetComponentData(textStats, new MaskCountThisFrame { maskCount = 1 });  // Zero reserved for no mask

            state.Dependency = new UpdateSingleFontJob
            {
            }.ScheduleParallel(m_singleFontQuery, state.Dependency);

            state.Dependency = new UpdateMultiFontJobChunk
            {
                additionalEntityHandle = SystemAPI.GetBufferTypeHandle<AdditionalFontMaterialEntity>(true),
                boundsLookup = SystemAPI.GetComponentLookup<RenderBounds>(false),
                controlLookup = SystemAPI.GetComponentLookup<TextRenderControl>(false),
                entityHandle = SystemAPI.GetEntityTypeHandle(),
                glyphHandle = SystemAPI.GetBufferTypeHandle<RenderGlyph>(true),
                glyphMaskLookup = SystemAPI.GetBufferLookup<RenderGlyphMask>(false),
                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
                materialMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>(false),
                selectorHandle = SystemAPI.GetBufferTypeHandle<FontMaterialSelectorForGlyph>(true)
            }.ScheduleParallel(m_multiFontQuery, state.Dependency);
        }
    }
}

