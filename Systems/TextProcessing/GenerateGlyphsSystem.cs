using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace TextMeshDOTS
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    //[DisableAutoCreation]
    public partial struct GenerateGlyphsSystem : ISystem
    {
        EntityQuery m_query;
        static readonly ProfilerMarker shapeMarker = new ProfilerMarker("hb_shape");
        static readonly ProfilerMarker bufferMarker = new ProfilerMarker("buffer");

        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_query = SystemAPI.QueryBuilder()
                .WithAllRW<CalliByte, RenderGlyph>()           
                .WithAll<TextBaseConfiguration>()
                .Build();

            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;

            var glyphTable = new GlyphTable
            {
                glyphEntries = new NativeList<GlyphTable.Entry>(1024, Allocator.Persistent),
                glyphHashToGlyphEntryIDMap = new NativeHashMap<GlyphTable.Key, uint>(1024, Allocator.Persistent)
            };
            state.EntityManager.CreateSingleton(glyphTable);
        }
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            state.CompleteDependency();
            SystemAPI.GetSingletonRW<GlyphTable>().ValueRW.TryDispose(default).Complete();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<FontTable>(out FontTable fontTable))
                return;

            if (fontTable.faces.Length == 0)
                return;

            SystemAPI.TryGetSingletonEntity<TextColorGradient>(out Entity textColorGradientEntity);
            var glyphTable = SystemAPI.GetSingletonRW<GlyphTable>().ValueRW;

            var chunkCount         = m_query.CalculateChunkCountWithoutFiltering();            

            var missingGlyphStream = new NativeStream(chunkCount, state.WorldUpdateAllocator);
            var glyphOTFStream     = new NativeStream(chunkCount, state.WorldUpdateAllocator);
            var xmlTagStream       = new NativeStream(chunkCount, state.WorldUpdateAllocator);

            var inputJh = state.Dependency;

            //optional single threaded job to pre-allcoate RenderGlyphbuffer...pays off when spawning a lot of new TextRenderer
            var allocateBuffersJh = new AllocateRenderGlyphsJob
            {
                calliByteHandle = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                renderGlyphHandle = SystemAPI.GetBufferTypeHandle<RenderGlyph>(false),

                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
            }.Schedule(m_query, inputJh);

            var tagsJh = new ExtractTagsJob
            {
                xmlTagStream                = xmlTagStream.AsWriter(),
                calliByteHandle             = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                textBaseConfigurationHandle = SystemAPI.GetComponentTypeHandle<TextBaseConfiguration>(true),

                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
            }.ScheduleParallel(m_query, inputJh);
            
            var shapeJh = new ShapeJob
            {
                shapeMarker = shapeMarker,
                bufferMarker = bufferMarker,


                glyphOTFStream = glyphOTFStream.AsWriter(),
                missingGlyphsStream = missingGlyphStream.AsWriter(),
                xmlTagStream = xmlTagStream.AsReader(),

                glyphTable = glyphTable,
                fontTable = fontTable,
                textBaseConfigurationHandle = SystemAPI.GetComponentTypeHandle<TextBaseConfiguration>(true),
                calliByteHandle = SystemAPI.GetBufferTypeHandle<CalliByte>(true),

                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
            }.ScheduleParallel(m_query, tagsJh);

            var missingGlyphsToAdd = new NativeList<GlyphTable.Key>(state.WorldUpdateAllocator);
            var allocateNewGlyphsJh = new AllocateNewGlyphsJob
            {
                fontTable = fontTable,
                glyphTable = glyphTable,
                missingGlyphsStream = missingGlyphStream.AsReader(),
                missingGlyphsToAdd = missingGlyphsToAdd
            }.Schedule(shapeJh);

            // Todo: As of harfbuzz 12.0.0, a Face object contains various table accerators for each glyph type.
            // For example, true-type outlines have a separate accelerator than COLR. Each accelerator contains
            // a scratch buffer which is acquired by mutex. And fetching the glyph extents locks this mutex.
            // Based on this, the most likely way to parallelize capturing glyph extents would be to group new
            // glyphs by face and then by type. However, this isn't the full story.
            // True-type glyphs are cheap to calculate extents for, and so there may not be any benefit to
            // parallelizing those in practice. COLR may be more expensive, but current tests still show this
            // to not be very significant except for the very first glyph processed, which has multiple
            // milliseconds of latency. And if multiple threads attempt to operate on COLR glyphs before the
            // first one is done, the CPU runs into some kind of thrashing situation. This requires more
            // investigation and testing to characterize what operations are actually parallelizable. In the
            // meantime, we run this job single-threaded.
            var populateJh = new PopulateNewGlyphsJob
            {
                fontTable = fontTable,
                glyphEntries = glyphTable.glyphEntries.AsDeferredJobArray(),
                missingGlyphs = missingGlyphsToAdd.AsDeferredJobArray()
            //}.Schedule(missingGlyphsToAdd, 4, state.Dependency);
            }.Schedule(allocateNewGlyphsJh);

            state.Dependency = new GenerateRenderGlyphsJob
            {
                renderGlyphHandle = SystemAPI.GetBufferTypeHandle<RenderGlyph>(false),
                previousRenderGlyphHandle = SystemAPI.GetBufferTypeHandle<PreviousRenderGlyph>(false),

                fontTable = fontTable,
                glyphTable = glyphTable,

                glyphOTFStream = glyphOTFStream.AsReader(),
                xmlTagStream = xmlTagStream.AsReader(),

                calliByteHandle             = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                textBaseConfigurationHandle = SystemAPI.GetComponentTypeHandle<TextBaseConfiguration>(true),

                textColorGradientEntity = textColorGradientEntity,
                textColorGradientLookup = SystemAPI.GetBufferLookup<TextColorGradient>(true),

                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
            }.ScheduleParallel(m_query, JobHandle.CombineDependencies(populateJh, allocateBuffersJh));
        }

        internal struct XMLTagStreamHeader
        {
            public int tagCount;
        }

        internal struct GlyphOTFStreamHeader
        {
            public float2 penStart;
            public int glyphCount;
        }

    }
}