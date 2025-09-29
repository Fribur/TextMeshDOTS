using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Entities;
using TextMeshDOTS.HarfBuzz;

namespace TextMeshDOTS
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    //[UpdateAfter(typeof(UpdateAtlasSystem))]
    [UpdateAfter(typeof(ShapeSystem))]
    public partial struct GenerateGlyphsSystem : ISystem
    {
        EntityQuery m_query;

        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_query = SystemAPI.QueryBuilder()
                      .WithAllRW<RenderGlyph>()
                      .WithAll<CalliByte>()
                      .WithAll<GlyphOTF>()
                      .WithAll<XMLTag>()
                      .WithAll<TextBaseConfiguration>()
                      .Build();

            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;
            m_query.SetChangedVersionFilter(ComponentType.ReadWrite<GlyphOTF>());
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (m_query.IsEmpty)
                return;
            //Debug.Log("Generate glyphs system");

            SystemAPI.TryGetSingletonEntity<TextColorGradient>(out Entity textColorGradientEntity);
            state.Dependency = new GenerateRenderGlyphsJob
            {
                renderGlyphHandle = SystemAPI.GetBufferTypeHandle<RenderGlyph>(false),

                fontTable = SystemAPI.GetSingleton<FontTable>(),
                glyphTable = SystemAPI.GetSingleton<GlyphTable>(),
                
                entitesHandle = SystemAPI.GetEntityTypeHandle(),
                calliByteHandle = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                glyphOTFHandle = SystemAPI.GetBufferTypeHandle<GlyphOTF>(true),
                xmlTagHandle = SystemAPI.GetBufferTypeHandle<XMLTag>(true),
                textBaseConfigurationHandle = SystemAPI.GetComponentTypeHandle<TextBaseConfiguration>(true),

                textColorGradientEntity = textColorGradientEntity,
                textColorGradientLookup = SystemAPI.GetBufferLookup<TextColorGradient>(true),

                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
            }.ScheduleParallel(m_query, state.Dependency);
        }
    }
}