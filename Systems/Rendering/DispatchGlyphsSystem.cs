using static Unity.Entities.SystemAPI;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.HarfBuzz.Bitmap;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.TextCore;

namespace TextMeshDOTS.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(UpdateGlyphsRenderersSystem))]
    public unsafe partial class DispatchGlyphsSystem : SystemBase
    {
        const int kTextureDimension = 4096;
        const int kShelfAlignment   = 16;

        EntityQuery m_query;

        UnityObjectRef<ComputeShader> m_uploadGlyphsShader;
        UnityObjectRef<ComputeShader> m_copyBytesShader;

        PersistentBuffer         m_glyphsBuffer;
        GraphicsBufferUploadPool m_byteAddressUploadBuffers;

        TextureAtlasArray<byte>    m_sdf8Array;
        TextureAtlasArray<ushort>  m_sdf16Array;
        TextureAtlasArray<Color32> m_bitmapArray;

        DrawDelegates  m_drawDelegates;
        PaintDelegates m_paintDelegates;

        // Shader bindings
        int _src;
        int _dst;
        int _startOffset;
        int _meta;

        int _tmdSdf8;
        int _tmdSdf16;
        int _tmdBitmap;
        int _tmdGlyphs;

        AtlasTable    m_atlasToDestroy;
        GlyphGpuTable m_glyphGpuTableToDestroy;

        protected override void OnCreate()
        {
            ref var state = ref CheckedStateRef;

            m_query = QueryBuilder().WithAll<MaterialMeshInfo>().WithAllRW<GpuState>().WithPresent<PreviousRenderGlyph>().WithPresentRW<ResidentRange>().Build();

            m_uploadGlyphsShader = Resources.Load<ComputeShader>("UploadGlyphs");
            m_copyBytesShader    = Resources.Load<ComputeShader>("CopyBytes");

            m_glyphsBuffer             = new PersistentBuffer(1024 * 16 * 128, 4, GraphicsBuffer.Target.Raw, m_copyBytesShader);
            m_byteAddressUploadBuffers = new GraphicsBufferUploadPool(1024 * 8 * 4, GraphicsBuffer.Target.Raw, 4);

            _src         = Shader.PropertyToID("_src");
            _dst         = Shader.PropertyToID("_dst");
            _startOffset = Shader.PropertyToID("_startOffset");
            _meta        = Shader.PropertyToID("_meta");

            _tmdSdf8   = Shader.PropertyToID("_tmdSdf8");
            _tmdSdf16  = Shader.PropertyToID("_tmdSdf16");
            _tmdBitmap = Shader.PropertyToID("_tmdBitmap");
            _tmdGlyphs = Shader.PropertyToID("_tmdGlyphs");

            m_sdf8Array   = new TextureAtlasArray<byte>(_tmdSdf8, kTextureDimension, 2, TextureFormat.R8, false);
            m_sdf16Array  = new TextureAtlasArray<ushort>(_tmdSdf16, kTextureDimension, 2, TextureFormat.R16, false);
            m_bitmapArray = new TextureAtlasArray<Color32>(_tmdBitmap, kTextureDimension, 2, TextureFormat.RGBA32, true);

            m_drawDelegates  = new DrawDelegates(true);
            m_paintDelegates = new PaintDelegates(true);

            var atlas        = new AtlasTable(Allocator.Persistent, kTextureDimension, kShelfAlignment);
            m_atlasToDestroy = atlas;
            EntityManager.CreateSingleton(atlas);
            var glyphGpuTable = new GlyphGpuTable
            {
                bufferSize          = new NativeReference<uint>(Allocator.Persistent, NativeArrayOptions.ClearMemory),
                dispatchDynamicGaps = new NativeList<uint2>(Allocator.Persistent),
                residentGaps        = new NativeList<uint2>(Allocator.Persistent)
            };
            m_glyphGpuTableToDestroy = glyphGpuTable;
            EntityManager.CreateSingleton(glyphGpuTable);
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

            m_sdf8Array.Dispose();
            m_sdf16Array.Dispose();
            m_bitmapArray.Dispose();

            m_drawDelegates.Dispose();
            m_paintDelegates.Dispose();

            m_atlasToDestroy.TryDispose(default);
            m_glyphGpuTableToDestroy.TryDispose(default);

            m_glyphsBuffer.Dispose();
            m_byteAddressUploadBuffers.Dispose();
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
            WriteState writeState = default;

            if (collected.glyphsToUpload.IsEmpty && collected.glyphEntryIDsToRasterize.IsEmpty)
                return writeState;

            var glyphTable = SystemAPI.GetSingleton<GlyphTable>();
            var fontTable  = SystemAPI.GetSingleton<FontTable>();

            var rasterizeJh    = state.Dependency;
            var uploadGlyphsJh = rasterizeJh;

            if (!collected.glyphEntryIDsToRasterize.IsEmpty)
            {
                int dirtySdf8Count;
                for (dirtySdf8Count = 0; dirtySdf8Count < collected.atlasDirtyIDs.Length; dirtySdf8Count++)
                {
                    var dirtyId = collected.atlasDirtyIDs[dirtySdf8Count];
                    if (dirtyId >= 0x40000000u)
                        break;
                }
                int dirtySdf16Count;
                for (dirtySdf16Count = dirtySdf8Count; dirtySdf16Count < collected.atlasDirtyIDs.Length; dirtySdf16Count++)
                {
                    var dirtyId = collected.atlasDirtyIDs[dirtySdf8Count];
                    if (dirtyId >= 0x80000000u)
                        break;
                }
                dirtySdf16Count      -= dirtySdf8Count;
                var dirtyBitmapCount  = collected.atlasDirtyIDs.Length - dirtySdf8Count - dirtySdf16Count;

                var sdf8Ptrs = CollectionHelper.CreateNativeArray<TextureAtlasArray<byte>.AtlasPtr>(dirtySdf8Count,
                                                                                                    state.WorldUpdateAllocator,
                                                                                                    NativeArrayOptions.UninitializedMemory);
                var sdf16Ptrs = CollectionHelper.CreateNativeArray<TextureAtlasArray<ushort>.AtlasPtr>(dirtySdf16Count,
                                                                                                       state.WorldUpdateAllocator,
                                                                                                       NativeArrayOptions.UninitializedMemory);
                var bitmapPtrs = CollectionHelper.CreateNativeArray<TextureAtlasArray<Color32>.AtlasPtr>(dirtyBitmapCount,
                                                                                                         state.WorldUpdateAllocator,
                                                                                                         NativeArrayOptions.UninitializedMemory);

                if (dirtySdf8Count > 0)
                {
                    m_sdf8Array.GetAtlasPtrsForDirtyIndices(collected.atlasDirtyIDs.AsArray().GetSubArray(0, dirtySdf8Count).AsSpan(), sdf8Ptrs.AsSpan());
                    writeState.isSdf8Dirty = true;
                }
                if (dirtySdf16Count > 0)
                {
                    m_sdf16Array.GetAtlasPtrsForDirtyIndices(collected.atlasDirtyIDs.AsArray().GetSubArray(dirtySdf8Count, dirtySdf16Count).AsSpan(), sdf16Ptrs.AsSpan());
                    writeState.isSdf16Dirty = true;
                }
                if (dirtyBitmapCount > 0)
                {
                    m_bitmapArray.GetAtlasPtrsForDirtyIndices(collected.atlasDirtyIDs.AsArray().GetSubArray(dirtySdf8Count + dirtySdf16Count, dirtyBitmapCount).AsSpan(),
                                                              bitmapPtrs.AsSpan());
                    writeState.isBitmapDirty = true;
                }

                rasterizeJh = new RasterizeJob
                {
                    bitmapPtrs               = bitmapPtrs,
                    drawDelegates            = m_drawDelegates,
                    fontTable                = fontTable,
                    glyphEntryIDsToRasterize = collected.glyphEntryIDsToRasterize.AsArray(),
                    glyphTable               = glyphTable,
                    paintDelegates           = m_paintDelegates,
                    sdf16Ptrs                = sdf16Ptrs,
                    sdf8Ptrs                 = sdf8Ptrs,
                }.ScheduleParallel(collected.glyphEntryIDsToRasterize.Length, 1, rasterizeJh);
            }
            if (!collected.glyphsToUpload.IsEmpty)
            {
                var lastCapture      = collected.glyphsToUpload[collected.glyphsToUpload.Length - 1];
                var glyphCount       = lastCapture.writeStart + lastCapture.glyphCount;
                var uploadBuffer     = m_byteAddressUploadBuffers.Allocate(glyphCount * UnsafeUtility.SizeOf<RenderGlyph>() / 4);
                var uploadArray      = uploadBuffer.LockBufferForWrite<RenderGlyph>(0, glyphCount);
                var captureCount     = collected.glyphsToUpload.Length;
                var uploadMetaBuffer = m_byteAddressUploadBuffers.Allocate(captureCount * 3);
                var uploadMetaArray  = uploadMetaBuffer.LockBufferForWrite<uint3>(0, captureCount);

                uploadGlyphsJh = new WriteRenderGlyphsToGpuJob
                {
                    captures        = collected.glyphsToUpload.AsArray(),
                    uploadArray     = uploadArray,
                    uploadMetaArray = uploadMetaArray,
                    glyphTable      = glyphTable
                }.ScheduleParallel(collected.glyphsToUpload.Length, 8, uploadGlyphsJh);

                writeState.uploadBuffer               = uploadBuffer;
                writeState.uploadBufferWriteCount     = glyphCount;
                writeState.uploadMetaBuffer           = uploadMetaBuffer;
                writeState.uploadMetaBufferWriteCount = captureCount;
            }

            state.Dependency = JobHandle.CombineDependencies(rasterizeJh, uploadGlyphsJh);
            return writeState;
        }

        public void Dispatch(ref SystemState state, ref WriteState written)
        {
            if (written.isSdf8Dirty)
                m_sdf8Array.ApplyChanges();
            if (written.isSdf16Dirty)
                m_sdf16Array.ApplyChanges();
            if (written.isBitmapDirty)
                m_bitmapArray.ApplyChanges();

            if (written.uploadBufferWriteCount > 0)
            {
                var glyphGpuTable = SystemAPI.GetSingleton<GlyphGpuTable>();

                written.uploadMetaBuffer.UnlockBufferAfterWrite<uint3>(written.uploadMetaBufferWriteCount);
                written.uploadBuffer.UnlockBufferAfterWrite<RenderGlyph>(written.uploadBufferWriteCount);

                var persistentBuffer = m_glyphsBuffer.GetBuffer(glyphGpuTable.bufferSize.Value);
                var shader           = m_uploadGlyphsShader.Value;
                shader.SetBuffer(0, _dst,  persistentBuffer);
                shader.SetBuffer(0, _src,  written.uploadBuffer);
                shader.SetBuffer(0, _meta, written.uploadMetaBuffer);

                for (uint dispatchesRemaining = (uint)written.uploadMetaBufferWriteCount, offset = 0; dispatchesRemaining > 0;)
                {
                    uint dispatchCount = math.min(dispatchesRemaining, 65535);
                    shader.SetInt(_startOffset, (int)offset);
                    shader.Dispatch(0, (int)dispatchCount, 1, 1);
                    offset              += dispatchCount;
                    dispatchesRemaining -= dispatchCount;
                }

                Shader.SetGlobalBuffer(_tmdGlyphs, persistentBuffer);
            }
        }

        public struct CollectState
        {
            internal NativeList<RenderGlyphCapture> glyphsToUpload;
            internal NativeList<uint>               glyphEntryIDsToRasterize;
            internal NativeList<uint>               atlasDirtyIDs;
        }

        public struct WriteState
        {
            internal bool isSdf8Dirty;
            internal bool isSdf16Dirty;
            internal bool isBitmapDirty;

            internal GraphicsBuffer uploadBuffer;
            internal GraphicsBuffer uploadMetaBuffer;
            internal int            uploadBufferWriteCount;
            internal int            uploadMetaBufferWriteCount;
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

        #region Collect Jobs
        [BurstCompile]
        struct AllocateJob : IJob
        {
            [ReadOnly] public GlyphTable       glyphTable;
            public NativeParallelHashSet<uint> glyphEntryIDsToRasterizeSet;

            public void Execute()
            {
                glyphEntryIDsToRasterizeSet.Capacity = math.max(glyphTable.entries.Length, glyphEntryIDsToRasterizeSet.Capacity);
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
                    for (int i = reader.BeginForEachIndex(stream); i > 0; i--)
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
                atlasDirtyIDs.Sort();
            }
        }
        #endregion

        #region Write Jobs
        [BurstCompile]
        struct RasterizeJob : IJobFor
        {
            [ReadOnly] public NativeArray<uint>                                                           glyphEntryIDsToRasterize;
            [ReadOnly] public GlyphTable                                                                  glyphTable;
            [ReadOnly] public FontTable                                                                   fontTable;
            [NativeDisableParallelForRestriction] public NativeArray<TextureAtlasArray<byte>.AtlasPtr>    sdf8Ptrs;
            [NativeDisableParallelForRestriction] public NativeArray<TextureAtlasArray<ushort>.AtlasPtr>  sdf16Ptrs;
            [NativeDisableParallelForRestriction] public NativeArray<TextureAtlasArray<Color32>.AtlasPtr> bitmapPtrs;

            [NativeDisableUnsafePtrRestriction] public DrawDelegates  drawDelegates;
            [NativeDisableUnsafePtrRestriction] public PaintDelegates paintDelegates;

            [NativeDisableContainerSafetyRestriction] DrawData drawData;
            [NativeSetThreadIndex] int                         threadIndex;

            public void Execute(int glyphIndex)
            {
                var glyphEntry = glyphTable.GetEntry(glyphEntryIDsToRasterize[glyphIndex]);

                // If the glyph doesn't have any real size, then there's nothing to rasterize.
                if (glyphEntry.width == 0 || glyphEntry.height == 0)
                    return;

                var face         = fontTable.faces[glyphEntry.key.faceIndex];
                var font         = fontTable.GetOrCreateFont(glyphEntry.key.faceIndex, threadIndex);
                var samplingSize = glyphEntry.key.textureSize.GetSamplingSize();
                font.SetScale(samplingSize, samplingSize);
                var maxDeviation = BezierMath.GetMaxDeviation(font.GetScale().x);
                if (!drawData.edges.IsCreated)
                    drawData = new DrawData(256, 16, maxDeviation, Allocator.Temp);
                drawData.Clear();
                drawData.maxDeviation = maxDeviation;

                if (glyphEntry.key.format == RenderFormat.SDF8)
                {
                    font.DrawGlyph(glyphEntry.key.glyphIndex, drawDelegates, ref drawData);
                    var sdf8TextureSlice = GetSdf8TextureSlice(glyphEntry.z);
                    var atlasRect        = new GlyphRect(glyphEntry.x, glyphEntry.y, glyphEntry.width, glyphEntry.height);
                    SDF_SPMD.SDFGenerateSubDivisionLineEdges(face.sdfOrientation,
                                                             ref drawData,
                                                             sdf8TextureSlice,
                                                             atlasRect,
                                                             glyphEntry.padding,
                                                             kTextureDimension,
                                                             kTextureDimension,
                                                             glyphEntry.padding);
                }
                else if (glyphEntry.key.format == RenderFormat.Bitmap8888)
                {
                    PaintData paintData     = default;
                    paintData.drawDelegates = drawDelegates;
                    paintData.clipGlyph     = drawData;
                    paintData.Clear();
                    font.PaintGlyph(glyphEntry.key.glyphIndex, ref paintData, paintDelegates, 0, new ColorARGB(255, 0, 0, 0));
                    if (paintData.paintSurface.Length > 0)
                    {
                        var bitmapTextureSlice = GetBitmapTextureSlice(glyphEntry.z);
                        for (int y = 0; y < glyphEntry.height; y++)
                        {
                            for (int x = 0; x < glyphEntry.width; x++)
                            {
                                var argb                     = paintData.paintSurface[y * glyphEntry.width + x];
                                var dstY                     = y + glyphEntry.y;
                                var dstX                     = x + glyphEntry.x;
                                var dstIndex                 = dstY * kTextureDimension + dstX;
                                bitmapTextureSlice[dstIndex] = new Color32(argb.r, argb.g, argb.b, argb.a);
                            }
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("SDF16 is not supported yet.");
                }
            }

            unsafe NativeArray<byte> GetSdf8TextureSlice(short z)
            {
                foreach (var ptr in sdf8Ptrs)
                {
                    if (ptr.atlasIndex == z)
                    {
                        return CollectionHelper.ConvertExistingDataToNativeArray<byte>(ptr.ptr, ptr.dimension * ptr.dimension, Allocator.None, true);
                    }
                }
                return default;
            }

            unsafe NativeArray<Color32> GetBitmapTextureSlice(short z)
            {
                foreach (var ptr in bitmapPtrs)
                {
                    if (ptr.atlasIndex == z)
                    {
                        return CollectionHelper.ConvertExistingDataToNativeArray<Color32>(ptr.ptr, ptr.dimension * ptr.dimension, Allocator.None, true);
                    }
                }
                return default;
            }
        }

        [BurstCompile]
        struct WriteRenderGlyphsToGpuJob : IJobFor
        {
            [ReadOnly] public GlyphTable                                          glyphTable;
            [ReadOnly] public NativeArray<RenderGlyphCapture>                     captures;
            [NativeDisableParallelForRestriction] public NativeArray<RenderGlyph> uploadArray;
            public NativeArray<uint3>                                             uploadMetaArray;

            public void Execute(int index)
            {
                const float kTextureResolutionFloatInverse = 1f / kTextureDimension;
                var         capture                        = captures[index];
                for (int i = 0; i < capture.glyphCount; i++)
                {
                    var glyph = capture.glyphBuffer[i];
                    var entry = glyphTable.GetEntry(glyph.glyphEntryId);

                    glyph.arrayIndex = (uint)entry.z;
                    // Todo: Currently we are overwriting these values because glyph generation doesn't need to augment these.
                    // Should we change that there? Or should we change the RenderGlyph comment?
                    glyph.blUVA = new float2(entry.x, entry.y) * kTextureResolutionFloatInverse;
                    glyph.trUVA = glyph.blUVA + new float2(entry.width, entry.height) * kTextureResolutionFloatInverse;

                    // Debug:
                    if (i < 5 && entry.key.format == RenderFormat.SDF8)
                    {
                        //UnityEngine.Debug.Log($"x: {entry.x}, y: {entry.y}, width: {entry.width}, height: {entry.height}, arrayIndex: {entry.z}, blUVA: {glyph.blUVA}, trUVA: {glyph.trUVA}");
                    }
                    capture.glyphBuffer[i] = glyph;

                    uploadArray[capture.writeStart + i] = glyph;
                }
                uploadMetaArray[index] = new uint3((uint)capture.writeStart, (uint)capture.gpuStart, (uint)capture.glyphCount);
            }
        }
        #endregion
    }
}

