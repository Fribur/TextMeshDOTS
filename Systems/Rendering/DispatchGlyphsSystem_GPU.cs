using TextMeshDOTS.LatiosInterop.Kinemation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using static TextMeshDOTS.DispatchGlyphsSystem;
using Unity.Jobs.LowLevel.Unsafe;

namespace TextMeshDOTS
{
    /// <summary>
    /// GPU-based glyph rendering system using Harfbuzz's hb-gpu API.
    /// This system encodes glyph outlines directly into GPU blobs using the Slug algorithm,
    /// eliminating the need for CPU rasterization and texture atlases.
    ///
    /// Glyphs are encoded into compact RGBA16I blobs on the CPU and uploaded to a structured buffer.
    /// The GPU decodes and rasterizes glyphs directly in the fragment shader.
    /// </summary>
    //[DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(UpdateGlyphsRenderersSystem))]
    [RequireMatchingQueriesForUpdate]
    public unsafe partial class DispatchGlyphsSystem_GPU : SystemBase
    {
        GraphicsBufferBroker broker;

        static GraphicsBufferBroker.StaticID shbGpuAtlasBufferID = GraphicsBufferBroker.ReservePersistentBuffer();
        static GraphicsBufferBroker.StaticID sHbGpuAtlasUploadID = GraphicsBufferBroker.ReserveUploadPool();
        static GraphicsBufferBroker.StaticID sMetaUint3UploadID = GraphicsBufferBroker.ReserveUploadPool();

        EntityQuery m_query;

        UnityObjectRef<ComputeShader> m_uploadHbGpuAtlasShader;
        UnityObjectRef<ComputeShader> m_uploadGlyphsShader;
        UnityObjectRef<ComputeShader> m_copyBytesShader;

        GraphicsBufferBroker.StaticID m_hbGpuAtlasBufferID;
        GraphicsBufferBroker.StaticID m_hbGpuAtlasUploadID;
        GraphicsBufferBroker.StaticID m_metaUint3UploadID;


        // Shader bindings for GPU blob approach
        int _src;
        int _dst;
        int _startOffset;
        int _meta;

        int _hbGpuAtlas;      // RGBAI16 (4*16=64 bits) for glyph blob data. Use StructuredBuffer<int2> in hlsl (2 * 32 =  64bit)
        int _tmdGlyphs;  // Buffer for glyph index mappings

        GraphicsBufferBroker.StaticID sGlyphIndexBufferID = GraphicsBufferBroker.ReservePersistentBuffer();
        GraphicsBufferBroker.StaticID sGlyphIndexUploadID = GraphicsBufferBroker.ReserveUploadPool();
        GraphicsBufferBroker.StaticID m_glyphIndexBufferID;
        GraphicsBufferBroker.StaticID m_glyphIndexUploadID;

        AtlasTable    m_atlasToDestroy;
        GlyphGpuTable m_glyphGpuTableToDestroy;

        // Per-thread GPU draw contexts for encoding
        //NativeArray<IntPtr> m_gpuDrawContexts;
        int m_numThreads;

        protected override void OnCreate()
        {

            m_numThreads = JobsUtility.MaxJobThreadCount;
            ref var state = ref CheckedStateRef;

            m_query = QueryBuilder()
                .WithAll<MaterialMeshInfo>()
                .WithAllRW<GpuState>()
                .WithPresent<PreviousRenderGlyph>()
                .WithPresentRW<ResidentRange>()
                .Build();

            broker = new GraphicsBufferBroker(Allocator.Persistent);

            m_metaUint3UploadID = sMetaUint3UploadID;
            broker.InitializeUploadPool(sMetaUint3UploadID, 4, GraphicsBuffer.Target.Raw);

            m_uploadHbGpuAtlasShader  = Resources.Load<ComputeShader>("UploadGlyphBlobs");
            m_uploadGlyphsShader = Resources.Load<ComputeShader>("UploadGlyphs");
            m_copyBytesShader = Resources.Load<ComputeShader>("CopyBytes");

            m_hbGpuAtlasBufferID = shbGpuAtlasBufferID;
            // Initial blob buffer size: 1MB (enough for ~125k glyphs at avg 80 bytes each)
            broker.InitializePersistentBuffer(m_hbGpuAtlasBufferID, 1024 * 1024, 8, GraphicsBuffer.Target.Structured, m_copyBytesShader);

            m_hbGpuAtlasUploadID = sHbGpuAtlasUploadID;
            broker.InitializeUploadPool(m_hbGpuAtlasUploadID, 8, GraphicsBuffer.Target.Structured);

            m_glyphIndexBufferID = sGlyphIndexBufferID;
            broker.InitializePersistentBuffer(m_glyphIndexBufferID, 1024 * 16 * 128, 4, GraphicsBuffer.Target.Raw, m_copyBytesShader);
            m_glyphIndexUploadID = sGlyphIndexUploadID;
            broker.InitializeUploadPool(m_glyphIndexUploadID, 4, GraphicsBuffer.Target.Raw);

            _src         = Shader.PropertyToID("_src");
            _dst         = Shader.PropertyToID("_dst");
            _startOffset = Shader.PropertyToID("_startOffset");
            _meta        = Shader.PropertyToID("_meta");

            _hbGpuAtlas    = Shader.PropertyToID("_hbGpuAtlas");
            _tmdGlyphs = Shader.PropertyToID("_tmdGlyphs");

            var dummyBuffer = broker.GetPersistentBufferNoResize(m_hbGpuAtlasBufferID);
            GraphicsUnmanaged.SetGlobalBuffer(_hbGpuAtlas, dummyBuffer);  // fix unbound _tmdGlyphs buffer issue
            dummyBuffer = broker.GetPersistentBufferNoResize(m_glyphIndexBufferID);
            GraphicsUnmanaged.SetGlobalBuffer(_tmdGlyphs, dummyBuffer);  // fix unbound _tmdGlyphs buffer issue

            var atlas = new AtlasTable(Allocator.Persistent, 4096, 16);
            m_atlasToDestroy = atlas;
            EntityManager.CreateSingleton(atlas);

            var glyphGpuTable = new GlyphGpuTable
            {
                bufferSize                  = new NativeReference<uint>(Allocator.Persistent, NativeArrayOptions.ClearMemory),
                dispatchDynamicGaps         = new NativeList<uint2>(Allocator.Persistent),
                residentGaps                = new NativeList<uint2>(Allocator.Persistent),
                bufferSizeHbGpuAtlas        = new NativeReference<uint>(Allocator.Persistent, NativeArrayOptions.ClearMemory),
                hbGpuAtlasGaps            = new NativeList<uint2>(Allocator.Persistent),
                hbGpuAtlasGcCandidates    = new NativeHashSet<uint>(256, Allocator.Persistent),
            };
            m_glyphGpuTableToDestroy = glyphGpuTable;
            var graphicsEntity = EntityManager.CreateSingleton(glyphGpuTable);
            EntityManager.AddComponentData(graphicsEntity, broker);
        }

        protected override void OnUpdate()
        {
            broker.Update();
            ref var state = ref CheckedStateRef;

            var collected = Collect(ref state);
            state.CompleteDependency();            

            var written = Write(ref state, ref collected);
            state.CompleteDependency();            

            Dispatch(ref state, ref written);
        }

        protected override void OnDestroy()
        {
            broker.Dispose();

            ref var state = ref CheckedStateRef;

            // Clear global shader bindings
            Shader.SetGlobalBuffer(_hbGpuAtlas, (GraphicsBuffer)null);
            Shader.SetGlobalBuffer(_tmdGlyphs, (GraphicsBuffer)null);

            m_atlasToDestroy.TryDispose(default);
            m_glyphGpuTableToDestroy.TryDispose(default);
        }

        public CollectState Collect(ref SystemState state)
        {
            var glyphTable    = SystemAPI.GetSingletonRW<GlyphTable>().ValueRW;
            var glyphGpuTable = SystemAPI.GetSingletonRW<GlyphGpuTable>().ValueRW;

            var glyphEntryIDsToEncodeSet = new NativeParallelHashSet<uint>(1, state.WorldUpdateAllocator);
            var allocateJh = new AllocateJob
            {
                glyphTable                  = glyphTable,
                glyphEntryIDsToEncodeSet = glyphEntryIDsToEncodeSet,
            }.Schedule(state.Dependency);

            var chunkCount                = m_query.CalculateChunkCountWithoutFiltering();
            var renderGlyphCapturesStream = new NativeStream(chunkCount, state.WorldUpdateAllocator);
            var captureJh = new CaptureRenderGlyphsJob_GPU
            {
                glyphEntryIDsToEncodeSet = glyphEntryIDsToEncodeSet.AsParallelWriter(),
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
                glyphTable                = glyphTable,
                glyphGpuTable             = glyphGpuTable,
                renderGlyphCapturesStream = renderGlyphCapturesStream
            }.Schedule(captureJh);

             var glyphEntryIDsToEncode = new NativeList<uint>(state.WorldUpdateAllocator);

            // Copy glyph entry IDs from set to list for encoding
            var copyJh = new CopyGlyphEntryIDsJob
            {
                glyphEntryIDsToEncode  = glyphEntryIDsToEncode,
                glyphEntryIDsToEncodeSet = glyphEntryIDsToEncodeSet,
            }.Schedule(captureJh);

            state.Dependency = JobHandle.CombineDependencies(assignJh, copyJh);

            return new CollectState
            {
                glyphEntryIDsToEncode = glyphEntryIDsToEncode,
                glyphsToUpload        = captures,
            };
        }

        public WriteState Write(ref SystemState state, ref CollectState collected)
        {
            WriteState writeState = default;

            var totalSizeRef = new NativeReference<int>(0, Allocator.TempJob);
            writeState.blobUploadBufferWriteCountRef = totalSizeRef;

            // Note: these two conditions are now independent
            bool hasGlyphsToUpload = !collected.glyphsToUpload.IsEmpty;
            bool hasGlyphsToEncode = !collected.glyphEntryIDsToEncode.IsEmpty;

            if (!hasGlyphsToUpload && !hasGlyphsToEncode)
                return writeState;


            var glyphTable    = SystemAPI.GetSingleton<GlyphTable>();
            var glyphGpuTable = SystemAPI.GetSingleton<GlyphGpuTable>();
            var fontTable     = SystemAPI.GetSingleton<FontTable>();
            var broker        = SystemAPI.GetSingleton<GraphicsBufferBroker>();
            writeState.broker = broker;

            var encodeJh    = state.Dependency;
            var uploadGlyphsJh = encodeJh;

            if (hasGlyphsToEncode)
            {
                // Phase 1: Encode glyphs to temp buffer (single-threaded)
                var encodedBlobs = new NativeList<byte>(collected.glyphEntryIDsToEncode.Length * 512, state.WorldUpdateAllocator); //cannot use NativeArray as gpu blobs have unpredictable size (SDF ~2-4k, COLR ~32-64k?)
                var blobMeta = CollectionHelper.CreateNativeArray<uint3>(collected.glyphEntryIDsToEncode.Length, state.WorldUpdateAllocator);
                encodeJh = new EncodeGlyphsToGpuBlobsJob
                {
                    fontTable           = fontTable,
                    glyphEntryIDsToEncode = collected.glyphEntryIDsToEncode.AsArray(),
                    glyphTable          = glyphTable,
                    glyphGpuTable       = glyphGpuTable,
                    encodedBlobs        = encodedBlobs,
                    blobMeta            = blobMeta,
                    totalSizeRef        = totalSizeRef,
                }.Schedule(encodeJh);

                // Phase 2: After encoding, get correctly sized upload buffer and copy
                // Note: totalSizeRef.Value is read here, but the actual copy happens in Dispatch after jobs complete
                writeState.encodedBlobsTemp           = encodedBlobs;
                writeState.blobMetaTemp              = blobMeta;
                writeState.blobUploadBufferWriteCountRef  = totalSizeRef;
                writeState.hbGpuAtlasUploadMetaBufferWriteCount = collected.glyphEntryIDsToEncode.Length;
            }            

            if (hasGlyphsToUpload)
            {
                // ... existing glyph upload code, unchanged ...
                // uploadGlyphsJh must still wait on encodeJh so blobOffset is set
                var lastCapture = collected.glyphsToUpload[^1];
                var glyphCount = lastCapture.writeStart + lastCapture.glyphCount;
                var uploadBuffer = broker.GetUploadBuffer(m_glyphIndexUploadID, (uint)(glyphCount * UnsafeUtility.SizeOf<RenderGlyph>() / 4));
                var uploadArray = uploadBuffer.LockBufferForWrite<RenderGlyph>(0, glyphCount);
                var captureCount = collected.glyphsToUpload.Length;
                var uploadMetaBuffer = broker.GetUploadBuffer(m_metaUint3UploadID, (uint)captureCount * 3);
                var uploadMetaArray = uploadMetaBuffer.LockBufferForWrite<uint3>(0, captureCount);

                // WriteRenderGlyphsToGpuJob_GPU must wait for encoding to complete (blobOffset is set in GlyphTable.Entry)
                uploadGlyphsJh = new WriteRenderGlyphsToGpuJob_GPU
                {
                    captures        = collected.glyphsToUpload.AsArray(),
                    uploadArray     = uploadArray,
                    uploadMetaArray = uploadMetaArray,
                    glyphTable      = glyphTable
                }.ScheduleParallel(collected.glyphsToUpload.Length, 8, encodeJh);

                writeState.glyphUploadBuffer               = uploadBuffer;
                writeState.glyphUploadBufferWriteCount     = glyphCount;
                writeState.glyphUploadMetaBuffer           = uploadMetaBuffer;
                writeState.glyphUploadMetaBufferWriteCount = captureCount;
            }

            state.Dependency = JobHandle.CombineDependencies(encodeJh, uploadGlyphsJh);
            return writeState;
        }

        public void Dispatch(ref SystemState state, ref WriteState written)
        {
            // Upload encoded GPU blobs to persistent buffer
            int hbGpuAtlasUploadBufferWriteCount = written.blobUploadBufferWriteCountRef.IsCreated == true 
                    ? written.blobUploadBufferWriteCountRef.Value : 0;
            written.blobUploadBufferWriteCountRef.Dispose();

            // Blob upload (only when new glyphs were encoded)
            if (hbGpuAtlasUploadBufferWriteCount > 0)
            {
                var lastMeta = written.blobMetaTemp[written.hbGpuAtlasUploadMetaBufferWriteCount - 1];
                // Calculate max destination offset + size needed
                uint maxDestOffset = lastMeta.y + lastMeta.z;
                uint uploadTexelCount = (maxDestOffset + 7) / 8;
                var uploadBuffer = written.broker.GetUploadBuffer(m_hbGpuAtlasUploadID, uploadTexelCount);
                var uploadArray = uploadBuffer.LockBufferForWrite<byte>(0, (int)uploadTexelCount * 8);

                var uploadMetaBuffer = written.broker.GetUploadBuffer(m_metaUint3UploadID, (uint)written.hbGpuAtlasUploadMetaBufferWriteCount * 3);
                var uploadMetaArray = uploadMetaBuffer.LockBufferForWrite<uint3>(0, written.hbGpuAtlasUploadMetaBufferWriteCount);

                // Copy encoded blobs to upload buffer at their destination offsets
                // blobMeta format: x=sourceOffset, y=destOffset, z=length
                for (int i = 0; i < written.blobMetaTemp.Length; i++)
                {
                    var meta = written.blobMetaTemp[i];
                    if (meta.z > 0)  // Skip empty entries
                    {
                        UnsafeUtility.MemCpy(
                            (byte*)uploadArray.GetUnsafePtr() + meta.y,
                            (byte*)written.encodedBlobsTemp.GetUnsafePtr() + meta.x,
                            (int)meta.z
                        );
                        uploadMetaArray[i] = meta;
                    }
                }

                uploadBuffer.UnlockBufferAfterWrite<byte>((int)maxDestOffset);
                uploadMetaBuffer.UnlockBufferAfterWrite<uint3>(written.hbGpuAtlasUploadMetaBufferWriteCount);

                // Copy to persistent buffer
                var persistentHbGpuAtlasBuffer = written.broker.GetPersistentBuffer(m_hbGpuAtlasBufferID, maxDestOffset);

                m_uploadHbGpuAtlasShader.SetBuffer(0, _dst, persistentHbGpuAtlasBuffer);
                m_uploadHbGpuAtlasShader.SetBuffer(0, _src, uploadBuffer);
                m_uploadHbGpuAtlasShader.SetBuffer(0, _meta, uploadMetaBuffer);

                for (uint dispatchesRemaining = (uint)written.hbGpuAtlasUploadMetaBufferWriteCount, offset = 0; dispatchesRemaining > 0;)
                {
                    uint dispatchCount = math.min(dispatchesRemaining, 65535);
                    m_uploadHbGpuAtlasShader.SetInt(_startOffset, (int)offset);
                    m_uploadHbGpuAtlasShader.Dispatch(0, (int)dispatchCount, 1, 1);
                    offset              += dispatchCount;
                    dispatchesRemaining -= dispatchCount;
                }

                GraphicsUnmanaged.SetGlobalBuffer(_hbGpuAtlas, persistentHbGpuAtlasBuffer);

                // Cleanup temp buffers
                written.encodedBlobsTemp.Dispose();
                written.blobMetaTemp.Dispose();
            }

            //Glyph index upload - runs whenever any text needs uploading,
            //including resident promotions with no new blobs
            if (written.glyphUploadBufferWriteCount > 0)
            {
                var glyphGpuTable = SystemAPI.GetSingleton<GlyphGpuTable>();
                written.glyphUploadMetaBuffer.UnlockBufferAfterWrite<uint3>(written.glyphUploadMetaBufferWriteCount);
                written.glyphUploadBuffer.UnlockBufferAfterWrite<RenderGlyph>(written.glyphUploadBufferWriteCount);

                var persistentBuffer = written.broker.GetPersistentBuffer(m_glyphIndexBufferID, glyphGpuTable.bufferSize.Value * 128 / 4);

                m_uploadGlyphsShader.SetBuffer(0, _dst, persistentBuffer);
                m_uploadGlyphsShader.SetBuffer(0, _src, written.glyphUploadBuffer);
                m_uploadGlyphsShader.SetBuffer(0, _meta, written.glyphUploadMetaBuffer);

                for (uint dispatchesRemaining = (uint)written.glyphUploadMetaBufferWriteCount, offset = 0; 
                    dispatchesRemaining > 0;)
                {
                    uint dispatchCount = math.min(dispatchesRemaining, 65535);
                    m_uploadGlyphsShader.SetInt(_startOffset, (int)offset);
                    m_uploadGlyphsShader.Dispatch(0, (int)dispatchCount, 1, 1);
                    offset              += dispatchCount;
                    dispatchesRemaining -= dispatchCount;
                }
                GraphicsUnmanaged.SetGlobalBuffer(_tmdGlyphs, persistentBuffer);
            }
        }       

        public struct CollectState
        {
            internal NativeList<RenderGlyphCapture> glyphsToUpload;
            internal NativeList<uint>               glyphEntryIDsToEncode;
        }

        public struct WriteState
        {
            internal GraphicsBufferBroker broker;

            internal GraphicsBufferUnmanaged glyphUploadBuffer;
            internal GraphicsBufferUnmanaged glyphUploadMetaBuffer;
            internal int            glyphUploadBufferWriteCount;
            internal int            glyphUploadMetaBufferWriteCount;

            // Temp buffers for encoded blobs (phase 1 output)
            internal NativeList<byte>   encodedBlobsTemp;
            internal NativeArray<uint3>  blobMetaTemp;
            internal NativeReference<int> blobUploadBufferWriteCountRef;
            internal int            hbGpuAtlasUploadMetaBufferWriteCount;
        }
    }
}
