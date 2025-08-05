using TextMeshDOTS.HarfBuzz;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

using static Unity.Entities.SystemAPI;

namespace TextMeshDOTS.Rendering
{
    [DisableAutoCreation]
    public unsafe partial class DispatchGlyphsSystem : SystemBase
    {
        const int kTextureDimension = 4096;
        const int kShelfAlignment   = 16;

        EntityQuery m_query;

        UnityObjectRef<ComputeShader> m_uploadGlyphsShader;
        UnityObjectRef<ComputeShader> m_copyBytesShader;

        PersistentBuffer         m_glyphsBuffer;
        GraphicsBufferUploadPool m_byteAddressUploadBuffers;

        UnityObjectRef<Texture2DArray> m_sdf8Array;
        UnityObjectRef<Texture2DArray> m_sdf16Array;
        UnityObjectRef<Texture2DArray> m_bitmapArray;

        // Shader bindings
        int _src;
        int _dst;
        int _startOffset;
        int _meta;

        int _tmdSdf8;
        int _tmdSdf16;
        int _tmdBitmap;
        int _tmdGlyphs;

        protected override void OnCreate()
        {
            ref var state = ref CheckedStateRef;

            m_query = QueryBuilder().WithAll<MaterialMeshInfo>().WithAllRW<GpuState>().WithPresent<PreviousRenderGlyph>().WithPresentRW<ResidentRange>().Build();

            m_uploadGlyphsShader = Resources.Load<ComputeShader>("UploadGlyphs");
            m_copyBytesShader    = Resources.Load<ComputeShader>("CopyBytes");

            m_glyphsBuffer             = new PersistentBuffer(1024 * 16 * 128, 4, GraphicsBuffer.Target.Raw, m_copyBytesShader);
            m_byteAddressUploadBuffers = new GraphicsBufferUploadPool(1024 * 8 * 4, GraphicsBuffer.Target.Raw, 4);

            m_sdf8Array   = new Texture2DArray(kTextureDimension, kTextureDimension, 2, TextureFormat.R8, false);
            m_sdf16Array  = new Texture2DArray(kTextureDimension, kTextureDimension, 2, TextureFormat.R16, false);
            m_bitmapArray = new Texture2DArray(kTextureDimension, kTextureDimension, 2, TextureFormat.RGBA32, true);

            _src         = Shader.PropertyToID("_src");
            _dst         = Shader.PropertyToID("_dst");
            _startOffset = Shader.PropertyToID("_startOffset");
            _meta        = Shader.PropertyToID("_meta");

            _tmdSdf8   = Shader.PropertyToID("_tmdSdf8");
            _tmdSdf16  = Shader.PropertyToID("_tmdSdf16");
            _tmdBitmap = Shader.PropertyToID("_tmdSdfBitmap");
            _tmdGlyphs = Shader.PropertyToID("_tmdGlyphs");

            var atlas = new AtlasTable(Allocator.Persistent, kTextureDimension, kShelfAlignment);
            EntityManager.CreateSingleton(atlas);
        }

        protected override void OnUpdate()
        {
            ref var state     = ref CheckedStateRef;
            var     collected = Collect(ref state);
            state.CompleteDependency();
            var written = Write(ref state, ref collected);
            state.CompleteDependency();
            Dispatch(ref state, ref written);
        }

        protected override void OnDestroy()
        {
            ref var state = ref CheckedStateRef;

            GraphicsBuffer b = null;
            Shader.SetGlobalBuffer(_tmdGlyphs, b);
            Texture2DArray t = null;
            Shader.SetGlobalTexture(_tmdSdf8,   t);
            Shader.SetGlobalTexture(_tmdSdf16,  t);
            Shader.SetGlobalTexture(_tmdBitmap, t);
        }

        public CollectState Collect(ref SystemState state)
        {
            var glyphTable    = SystemAPI.GetSingletonRW<GlyphTable>().ValueRW;
            var glyphGpuTable = SystemAPI.GetSingletonRW<GlyphGpuTable>().ValueRW;
            var atlasTable    = SystemAPI.GetSingletonRW<AtlasTable>().ValueRW;

            var glyphEntryIDsToRasterizeSet = new NativeParallelHashSet<uint>(1, state.WorldUpdateAllocator);
            var allocateJh                  = new AllocateJob
            {
                glyphTable                  = glyphTable,
                glyphEntryIDsToRasterizeSet = glyphEntryIDsToRasterizeSet,
            }.Schedule(state.Dependency);

            var chunkCount                = m_query.CalculateChunkCountWithoutFiltering();
            var renderGlyphCapturesStream = new NativeStream(chunkCount, state.WorldUpdateAllocator);
            var captureJh                 = new CaptureRenderGlyphsJob
            {
                glyphEntryIDsToRasterizeSet = glyphEntryIDsToRasterizeSet.AsParallelWriter(),
                glyphTable                  = glyphTable,
                gpuStateHandle              = GetComponentTypeHandle<GpuState>(false),
                renderGlyphCapturesStream   = renderGlyphCapturesStream.AsWriter(),
                renderGlyphHandle           = GetBufferTypeHandle<PreviousRenderGlyph>(true),
                residentRangeHandle         = GetComponentTypeHandle<ResidentRange>(false),
                textShaderIndexHandle       = GetComponentTypeHandle<TextShaderIndex>(false),
            }.ScheduleParallel(m_query, allocateJh);

            var captures = new NativeList<RenderGlyphCapture>(state.WorldUpdateAllocator);
            var assignJh = new AssignShaderIndicesJob
            {
                captures                  = captures,
                glyphGpuTable             = glyphGpuTable,
                renderGlyphCapturesStream = renderGlyphCapturesStream
            }.Schedule(captureJh);

            var glyphEntryIDsToRasterize = new NativeList<uint>(state.WorldUpdateAllocator);
            var atlasDirtyIDs            = new NativeList<uint>(state.WorldUpdateAllocator);
            var atlasJh                  = new AllocateGlyphsInAtlasJob
            {
                atlasDirtyIDs               = atlasDirtyIDs,
                atlasTable                  = atlasTable,
                glyphEntryIDsToRasterize    = glyphEntryIDsToRasterize,
                glyphEntryIDsToRasterizeSet = glyphEntryIDsToRasterizeSet,
                glyphTable                  = glyphTable,
            }.Schedule(captureJh);

            state.Dependency = JobHandle.CombineDependencies(assignJh, atlasJh);

            return new CollectState
            {
                atlasDirtyIDs            = atlasDirtyIDs,
                glyphEntryIDsToRasterize = glyphEntryIDsToRasterize,
                glyphsToUpload           = captures
            };
        }

        public WriteState Write(ref SystemState state, ref CollectState collected)
        {
            return default;
        }

        public void Dispatch(ref SystemState state, ref WriteState written)
        {
        }

        public struct CollectState
        {
            internal NativeList<RenderGlyphCapture> glyphsToUpload;
            internal NativeList<uint>               glyphEntryIDsToRasterize;
            internal NativeList<uint>               atlasDirtyIDs;
        }

        public struct WriteState
        {
        }

        internal struct RenderGlyphCapture
        {
            public TextShaderIndex* textShaderIndexPtr;
            public ResidentRange*   residentRangePtr;
            public RenderGlyph*     glyphBuffer;
            public int              glyphCount;
            public bool             makeResident;
            public int              writeStart;
            public int              gpuStart;
        }

        [BurstCompile]
        struct AllocateJob : IJob
        {
            [ReadOnly] public GlyphTable       glyphTable;
            public NativeParallelHashSet<uint> glyphEntryIDsToRasterizeSet;

            public void Execute()
            {
                glyphEntryIDsToRasterizeSet.Capacity = glyphTable.entries.Length;
            }
        }

        [BurstCompile]
        struct CaptureRenderGlyphsJob : IJobChunk
        {
            [ReadOnly] public GlyphTable                            glyphTable;
            [ReadOnly] public BufferTypeHandle<PreviousRenderGlyph> renderGlyphHandle;
            public ComponentTypeHandle<TextShaderIndex>             textShaderIndexHandle;
            public ComponentTypeHandle<ResidentRange>               residentRangeHandle;
            public ComponentTypeHandle<GpuState>                    gpuStateHandle;

            [NativeDisableParallelForRestriction] public NativeStream.Writer renderGlyphCapturesStream;
            public NativeParallelHashSet<uint>.ParallelWriter                glyphEntryIDsToRasterizeSet;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                renderGlyphCapturesStream.BeginForEachIndex(unfilteredChunkIndex);

                var shaderPtr    = chunk.GetComponentDataPtrRW(ref textShaderIndexHandle);
                var residentPtr  = (ResidentRange*)chunk.GetRequiredComponentDataPtrRW(ref residentRangeHandle);
                var gpuStates    = (GpuState*)chunk.GetRequiredComponentDataPtrRW(ref gpuStateHandle);
                var glyphBuffers = chunk.GetBufferAccessor(ref renderGlyphHandle);
                var gpuStateMask = chunk.GetEnabledMask(ref gpuStateHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndex))
                {
                    gpuStateMask[entityIndex]    = false;
                    bool resident                = gpuStates[entityIndex].state == GpuState.State.DynamicPromoteToResident;
                    gpuStates[entityIndex].state = resident ? GpuState.State.Resident : GpuState.State.Dynamic;
                    var glyphs                   = glyphBuffers[entityIndex];
                    renderGlyphCapturesStream.Write(new RenderGlyphCapture
                    {
                        glyphBuffer        = glyphs.Length != 0 ? (RenderGlyph*)glyphs.GetUnsafeReadOnlyPtr() : null,
                        glyphCount         = glyphs.Length,
                        makeResident       = resident,
                        residentRangePtr   = residentPtr + entityIndex,
                        textShaderIndexPtr = shaderPtr != null ? shaderPtr + entityIndex : null,
                    });

                    foreach (var glyph in glyphs)
                    {
                        var entry = glyphTable.GetEntry(glyph.glyph.glyphEntryId);
                        if (!entry.isInAtlas)
                            glyphEntryIDsToRasterizeSet.Add(glyph.glyph.glyphEntryId);
                    }
                }

                renderGlyphCapturesStream.EndForEachIndex();
            }
        }

        [BurstCompile]
        struct AssignShaderIndicesJob : IJob
        {
            [ReadOnly] public NativeStream        renderGlyphCapturesStream;
            public NativeList<RenderGlyphCapture> captures;
            public GlyphGpuTable                  glyphGpuTable;

            public void Execute()
            {
                int captureCount  = renderGlyphCapturesStream.Count();
                captures.Capacity = captureCount;

                int writeBufferOffset  = 0;
                int dynamicCount       = 0;
                var residentBufferSize = glyphGpuTable.bufferSize.Value;

                for (int stream = 0; stream < renderGlyphCapturesStream.ForEachCount; stream++)
                {
                    var reader = renderGlyphCapturesStream.AsReader();
                    for (int i = reader.BeginForEachIndex(stream); i >= 0; i--)
                    {
                        var capture         = reader.Read<RenderGlyphCapture>();
                        capture.writeStart  = writeBufferOffset;
                        writeBufferOffset  += capture.glyphCount;
                        if (capture.makeResident)
                        {
                            GapAllocator.TryAllocate(glyphGpuTable.residentGaps, (uint)capture.glyphCount, ref residentBufferSize, out var newLocation);
                            capture.gpuStart = (int)newLocation;
                            if (capture.textShaderIndexPtr != null)
                            {
                                capture.textShaderIndexPtr->firstGlyphIndex = newLocation;
                                capture.textShaderIndexPtr->glyphCount      = (uint)capture.glyphCount;
                            }
                            capture.residentRangePtr->start = newLocation;
                            capture.residentRangePtr->count = (uint)capture.glyphCount;
                        }
                        else
                        {
                            dynamicCount += capture.glyphCount;
                        }
                        captures.AddNoResize(capture);
                    }
                    reader.EndForEachIndex();
                }
                GapAllocator.TryAllocate(glyphGpuTable.residentGaps, (uint)dynamicCount, ref residentBufferSize, out var dynamicStart);
                glyphGpuTable.dispatchDynamicGaps.Add(new uint2(dynamicStart, (uint)dynamicCount));
                for (int i = 0; i < captures.Length; i++)
                {
                    ref var capture = ref captures.ElementAt(i);
                    if (capture.makeResident)
                        continue;
                    capture.gpuStart  = (int)dynamicStart;
                    dynamicStart     += (uint)capture.glyphCount;
                    if (capture.textShaderIndexPtr != null)
                    {
                        capture.textShaderIndexPtr->firstGlyphIndex = (uint)capture.gpuStart;
                        capture.textShaderIndexPtr->glyphCount      = (uint)capture.glyphCount;
                    }
                    capture.residentRangePtr->start = (uint)capture.gpuStart;
                    capture.residentRangePtr->count = (uint)capture.glyphCount;
                }

                glyphGpuTable.bufferSize.Value = residentBufferSize;

                // Remove empty buffers from upload list.
                int dstIndex = 0;
                for (int i = 0; i < captures.Length; i++)
                {
                    if (captures[i].glyphCount != 0)
                    {
                        captures[dstIndex] = captures[i];
                        dstIndex++;
                    }
                }
                captures.Length = dstIndex;
            }
        }

        [BurstCompile]
        struct AllocateGlyphsInAtlasJob : IJob
        {
            [ReadOnly] public NativeParallelHashSet<uint> glyphEntryIDsToRasterizeSet;
            public NativeList<uint>                       glyphEntryIDsToRasterize;
            public NativeList<uint>                       atlasDirtyIDs;
            public GlyphTable                             glyphTable;
            public AtlasTable                             atlasTable;

            public void Execute()
            {
                var count                         = glyphEntryIDsToRasterizeSet.Count();
                glyphEntryIDsToRasterize.Capacity = count;
                foreach (var glyph in glyphEntryIDsToRasterizeSet)
                    glyphEntryIDsToRasterize.AddNoResize(glyph);
                glyphEntryIDsToRasterize.Sort();

                UnsafeHashSet<uint> dirtyAtlasIDSet = new UnsafeHashSet<uint>(32, Allocator.Temp);

                foreach (var glyph in glyphEntryIDsToRasterize)
                {
                    ref var glyphEntry = ref glyphTable.GetEntryRW(glyph);
                    atlasTable.Allocate(glyph, glyphEntry.width, glyphEntry.height, out glyphEntry.x, out glyphEntry.y, out glyphEntry.z);
                    uint id  = (uint)glyphEntry.z;
                    id      |= glyph & 0xc0000000;
                    dirtyAtlasIDSet.Add(id);
                }

                atlasDirtyIDs.Capacity = dirtyAtlasIDSet.Count;
                foreach (var id in dirtyAtlasIDSet)
                    atlasDirtyIDs.AddNoResize(id);
            }
        }
    }
}

