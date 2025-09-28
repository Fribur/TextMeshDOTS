using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;
using TextMeshDOTS.HarfBuzz;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Font = TextMeshDOTS.HarfBuzz.Font;
using UnityEditor.Build.Reporting;

namespace TextMeshDOTS.TextProcessing
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    //[DisableAutoCreation]
    public partial struct ShapeSystem : ISystem
    {
        EntityQuery textRendererQ;
        static readonly ProfilerMarker marker = new ProfilerMarker("hb_shape");
        static readonly ProfilerMarker marker2 = new ProfilerMarker("buffer");

        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            textRendererQ = SystemAPI.QueryBuilder()
                .WithAllRW<XMLTag>()
                .WithAllRW<GlyphOTF>()
                .WithAllRW<CalliByte>()           
                .WithAll<TextBaseConfiguration>()
                .Build();

            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;

            var glyphTable = new GlyphTable
            {
                entries = new NativeList<GlyphTable.Entry>(1024, Allocator.Persistent),
                glyphHashToIdMap = new NativeHashMap<GlyphTable.Key, uint>(1024, Allocator.Persistent)
            };
            state.EntityManager.CreateSingleton(glyphTable);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //if (textRendererQ.IsEmpty)
            //    return;
            //Debug.Log($"Shape system {textRendererQ.CalculateEntityCount()}");
            
            state.Dependency = new ExtractTagsJob
            {
                calliByteHandle = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                xmlTagHandle = SystemAPI.GetBufferTypeHandle<XMLTag>(false),

                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
            }.ScheduleParallel(textRendererQ, state.Dependency);

            var chunkCount = textRendererQ.CalculateChunkCountWithoutFiltering();
            var missingGlyphStream = new NativeStream(chunkCount, state.WorldUpdateAllocator);
            var glyphTable = SystemAPI.GetSingletonRW<GlyphTable>().ValueRW;
            var fontTable = SystemAPI.GetSingleton<FontTable>();


            state.Dependency = new ShapeJob
            {
                marker = marker,
                marker2 = marker2,

                missingGlyphsStream = missingGlyphStream.AsWriter(),

                glyphTable = glyphTable,
                fontTable = fontTable,
                textBaseConfigurationHandle = SystemAPI.GetComponentTypeHandle<TextBaseConfiguration>(true),
                calliByteHandle = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                glyphOTFHandle = SystemAPI.GetBufferTypeHandle<GlyphOTF>(false),
                xmlTagHandle = SystemAPI.GetBufferTypeHandle<XMLTag>(true),

                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
            }.ScheduleParallel(textRendererQ, state.Dependency);

            var missingGlyphsToAdd = new NativeList<GlyphTable.Key>(state.WorldUpdateAllocator);
            state.Dependency = new AllocateNewGlyphsJob
            {
                fontTable = fontTable,
                glyphTable = glyphTable,
                missingGlyphsStream = missingGlyphStream.AsReader(),
                missingGlyphsToAdd = missingGlyphsToAdd
            }.Schedule(state.Dependency);

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
            state.Dependency = new PopulateNewGlyphsJob
            {
                fontTable = fontTable,
                glyphEntries = glyphTable.entries.AsDeferredJobArray(),
                missingGlyphs = missingGlyphsToAdd.AsDeferredJobArray()
            //}.Schedule(missingGlyphsToAdd, 4, state.Dependency);
            }.Schedule(state.Dependency);
        }
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            state.CompleteDependency();
            SystemAPI.GetSingletonRW<GlyphTable>().ValueRW.TryDispose(default).Complete();
        }

        [BurstCompile]
        struct AllocateNewGlyphsJob : IJob
        {
            [ReadOnly] public FontTable fontTable;
            public GlyphTable glyphTable;
            public NativeStream.Reader missingGlyphsStream;
            public NativeList<GlyphTable.Key> missingGlyphsToAdd;

            public void Execute()
            {
                // Deduplicate
                var requestCount = missingGlyphsStream.Count();
                var uniqueMissingGlyphSet = new UnsafeHashSet<GlyphTable.Key>(requestCount, Allocator.Temp);
                for (int chunk = 0; chunk < missingGlyphsStream.ForEachCount; chunk++)
                {
                    var elementsInChunk = missingGlyphsStream.BeginForEachIndex(chunk);
                    for (int i = 0; i < elementsInChunk; i++)
                    {
                        var key = missingGlyphsStream.Read<GlyphTable.Key>();
                        uniqueMissingGlyphSet.Add(key);
                    }
                }

                missingGlyphsToAdd.Capacity = uniqueMissingGlyphSet.Count;
                uint nextIndex = (uint)glyphTable.glyphHashToIdMap.Count;
                foreach (var key in uniqueMissingGlyphSet)
                {
                    missingGlyphsToAdd.AddNoResize(key);
                    var nextId = nextIndex;
                    Bits.SetBits(ref nextId, 30, 2, (uint)key.format);
                    glyphTable.glyphHashToIdMap.Add(key, nextId);
                    nextIndex++;
                }
                glyphTable.entries.AddReplicate(default, missingGlyphsToAdd.Length);
            }
        }

        [BurstCompile]
        struct PopulateNewGlyphsJob : IJobParallelForDefer, IJob
        {
            [ReadOnly] public NativeArray<GlyphTable.Key> missingGlyphs;
            [ReadOnly] public FontTable fontTable;
            [NativeDisableParallelForRestriction] public NativeArray<GlyphTable.Entry> glyphEntries;

            [NativeSetThreadIndex]
            int threadIndex;

            GlyphTable.Key lastKey;
            [NativeDisableUnsafePtrRestriction] Font lastFont;
            bool initialized;

            public void Execute()
            {
                for (int i = 0; i < missingGlyphs.Length; i++)
                {
                    Execute(i);
                }
            }

            public void Execute(int i)
            {
                var missingGlyph = missingGlyphs[i];
                var font = lastFont;

                if (!initialized || RequiresFontSetup(lastKey, missingGlyph))
                {
                    font = fontTable.GetOrCreateFont(missingGlyph.faceIndex, threadIndex);
                    var samplingSize = missingGlyph.textureSize.GetSamplingSize();
                    font.SetScale(samplingSize, samplingSize);
                    initialized = true;
                    lastFont = font;
                }

                // performance watchout:  hb_font_get_glyph_extents is a very costly function.
                // For a COLR glyph, rect is determined by parsing all vertices of maybe 20 sub-glyphs
                // in my tests getting glyph extents in parallel resulted in
                // total time = thread * (single thread time) 
                // reason unknown. Could be mutex lock. Or single thread benefits more
                // from font acceleration structures populated with each hb_font_get_glyph_extents call
                font.GetGlyphExtents(missingGlyph.glyphIndex, out var extents);

                var padding = missingGlyph.format switch
                {
                RenderFormat.SDF8 => missingGlyph.textureSize.GetSamplingSize() / 6,
                RenderFormat.SDF16 => missingGlyph.textureSize.GetSamplingSize() / 6,
                RenderFormat.Bitmap8888 => 8,
                _ => 0,
                };
                var newEntry = new GlyphTable.Entry
                {
                    key = missingGlyph,
                    refCount = 0,
                    x = -1,
                    y = -1,
                    z = -1,
                    width = (short)extents.width,
                    height = (short)(extents.height),  
                    xBearing = (short)extents.x_bearing,
                    yBearing = (short)extents.y_bearing,  // Harfbuzz is y-up
                    padding = (short)padding,
                };
                var baseIndex = glyphEntries.Length - missingGlyphs.Length;
                glyphEntries[baseIndex + i] = newEntry;
            }

            bool RequiresFontSetup(GlyphTable.Key lastKey, GlyphTable.Key thisKey)
            {
                var a = lastKey.packed & 0xffffffffffff0000;
                var b = thisKey.packed & 0xffffffffffff0000;
                return a != b;
            }
        }
    }
}