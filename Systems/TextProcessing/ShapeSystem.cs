using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.Rendering;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using System;
using Font = TextMeshDOTS.HarfBuzz.Font;
using System.Collections.Generic;

namespace TextMeshDOTS.TextProcessing
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    //[DisableAutoCreation]
    public partial struct ShapeSystem : ISystem
    {
        EntityQuery textRendererQ, fontstateQ;
        static readonly ProfilerMarker marker = new ProfilerMarker("hb_shape");
        static readonly ProfilerMarker marker2 = new ProfilerMarker("buffer");

        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            fontstateQ = SystemAPI.QueryBuilder()
                .WithAll<FontState>()
                .WithNone<FontsDirtyTag>()
                .Build();

            textRendererQ = SystemAPI.QueryBuilder()
                .WithAllRW<XMLTag>()
                .WithAllRW<GlyphOTF>()
                .WithAllRW<CalliByte>()           
                .WithAll<TextBaseConfiguration>()
                .WithAll<FontBlobReference>()
                .Build();

            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;
            state.RequireForUpdate(fontstateQ);

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
            //Debug.Log("Shape system");
            
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
                entitesHandle = SystemAPI.GetEntityTypeHandle(),
                additionalFontMaterialEntityHandle = SystemAPI.GetBufferTypeHandle<AdditionalFontMaterialEntity>(true),
                textBaseConfigurationHandle = SystemAPI.GetComponentTypeHandle<TextBaseConfiguration>(true),
                fontBlobReferenceHandle = SystemAPI.GetComponentTypeHandle<FontBlobReference>(true),
                fontBlobReferenceLookup = SystemAPI.GetComponentLookup<FontBlobReference>(true),
                calliByteHandle = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                glyphOTFHandle = SystemAPI.GetBufferTypeHandle<GlyphOTF>(false),
                selectorHandle = SystemAPI.GetBufferTypeHandle<FontMaterialSelectorForGlyph>(false),
                xmlTagHandle = SystemAPI.GetBufferTypeHandle<XMLTag>(true),
                glyphsInUseLookup = SystemAPI.GetBufferLookup<UsedGlyphs>(true),

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

            state.Dependency = new PopulateNewGlyphsJob
            {
                fontTable = fontTable,
                glyphEntries = glyphTable.entries.AsDeferredJobArray(),
                missingGlyphs = missingGlyphsToAdd.AsDeferredJobArray()
            }.Schedule(missingGlyphsToAdd, 32, state.Dependency);

            state.Dependency = new TempAddMissingGlyphsToFontEntitiesJob
            {
                fontTable = fontTable,
                missingGlyphs = missingGlyphsToAdd.AsDeferredJobArray(),
                missingGlyphsLookup = SystemAPI.GetBufferLookup<MissingGlyphs>(false)
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
        struct PopulateNewGlyphsJob : IJobParallelForDefer
        {
            [ReadOnly] public NativeArray<GlyphTable.Key> missingGlyphs;
            [ReadOnly] public FontTable fontTable;
            [NativeDisableParallelForRestriction] public NativeArray<GlyphTable.Entry> glyphEntries;

            [NativeSetThreadIndex]
            int threadIndex;

            GlyphTable.Key lastKey;
            [NativeDisableUnsafePtrRestriction] Font lastFont;
            bool initialized;

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

        [BurstCompile]
        struct TempAddMissingGlyphsToFontEntitiesJob : IJob
        {
            [ReadOnly] public FontTable fontTable;
            public NativeArray<GlyphTable.Key> missingGlyphs;

            public BufferLookup<MissingGlyphs> missingGlyphsLookup;

            public void Execute()
            {
                missingGlyphs.Sort(new KeySorter());

                for (int i = 0; i < missingGlyphs.Length; i++)
                {
                    var key = missingGlyphs[i];
                    var entity = fontTable.faceIndexToFontEntityMap[key.faceIndex];
                    var buffer = missingGlyphsLookup[entity];
                    buffer.Add(new MissingGlyphs { key = key });
                }
            }

            struct KeySorter : IComparer<GlyphTable.Key>
            {
                public int Compare(GlyphTable.Key x, GlyphTable.Key y)
                {
                    var result = x.faceIndex.CompareTo(y.faceIndex);
                    if (result == 0)
                        return x.glyphIndex.CompareTo(y.glyphIndex);
                    return result;
                }
            }
        }
    }
}