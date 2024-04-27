using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

namespace TextMeshDOTS.Rendering
{
    /// <summary>
    /// When changed RenderGlyph chunks are detected, the system builds the entire gpuTextBuffer fresh. Uploads into 
    /// gpuTextBuffer start at offset 0, TextShaderIndex.firstGlyphIndex also starting at 0 patching up chunks to accomodating 
    /// changed chunks appears complicated, prone to fragmentation --> research if it's worth it and how to do
    /// </summary>
    //[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    public partial class TextRenderingDispatchSystem : SystemBase
    {
        EntityQuery m_glyphsQuery, m_changedGlyphsQuery;
        SparseUploader m_GPUUploader;
        ThreadedSparseUploader m_ThreadedGPUUploader;
        HeapAllocator m_GPUPersistentAllocator;
        HeapBlock currentHeap;

        const ulong kMaxGPUAllocatorMemory = 1024 * 1024 * 1024; // 1GiB of potential memory space
        const long kGPUBufferSizeInitial = 8 * 1024 * 1024; //start with 8MB textbuffer, will grow when more is needed
        const long kGPUBufferSizeMax = 1023 * 1024 * 1024;
        const int kGPUUploaderChunkSize = 4 * 1024 * 1024;
        long m_PersistentInstanceDataSize;

        //Shader properties valid for entire batch uploaded via SetGlobalBuffer
        GraphicsBuffer gpuTextBuffer;
        int textBufferID;

        protected override void OnCreate()
        {
            m_glyphsQuery = SystemAPI.QueryBuilder()
                        .WithAll<RenderGlyph, RenderBounds>()
                        .WithAllRW<TextShaderIndex>()
                        .Build();
            m_changedGlyphsQuery = SystemAPI.QueryBuilder()
                        .WithAll<RenderGlyph, RenderBounds>()
                        .WithAll<TextShaderIndex>()
                        .Build();
            m_changedGlyphsQuery.SetChangedVersionFilter(ComponentType.ReadWrite<RenderGlyph>());

            m_GPUPersistentAllocator = new HeapAllocator(kMaxGPUAllocatorMemory, 16);
            m_PersistentInstanceDataSize = kGPUBufferSizeInitial;
            gpuTextBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)m_PersistentInstanceDataSize / 4, 4);
            currentHeap = m_GPUPersistentAllocator.Allocate((ulong)m_PersistentInstanceDataSize, 1);
            m_GPUUploader = new SparseUploader(gpuTextBuffer, kGPUUploaderChunkSize);

            textBufferID = Shader.PropertyToID("_textBuffer");
            Shader.SetGlobalBuffer(textBufferID, gpuTextBuffer);
        }
        protected override void OnDestroy()
        {
            m_GPUUploader.Dispose();
            gpuTextBuffer.Dispose();
            m_GPUPersistentAllocator.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonEntity<TextStatisticsTag>(out Entity worldBlackboardEntity))
                return;

            if (m_changedGlyphsQuery.IsEmpty)
                return;

            try
            {
                Dependency = UpdateRenderGlyphChunks(Dependency, worldBlackboardEntity);
                EndUpdate();
            }
            finally
            {
                m_GPUUploader.FrameCleanup();
            }        
        }
        JobHandle UpdateRenderGlyphChunks(JobHandle inputDependencies, Entity worldBlackboardEntity)
        {
            JobHandle done = inputDependencies;
            if (!m_glyphsQuery.IsEmptyIgnoreFilter)
            {
                var glyphStreamCount = CollectionHelper.CreateNativeArray<int>(1, WorldUpdateAllocator);
                glyphStreamCount[0] = m_glyphsQuery.CalculateChunkCountWithoutFiltering();
                if (glyphStreamCount[0] == 0)
                    return done;

                var glyphStreamConstructJh = NativeStream.ScheduleConstruct(out var glyphStream, glyphStreamCount, inputDependencies, WorldUpdateAllocator);
                var collectGlyphsJh = new GatherGlyphUploadOperationsJob
                {
                    glyphCountThisFrameLookup = SystemAPI.GetComponentLookup<GlyphCountThisFrame>(false),
                    glyphCountThisPass = 0,
                    renderGlyphHandle = SystemAPI.GetBufferTypeHandle<RenderGlyph>(true),
                    streamWriter = glyphStream.AsWriter(),
                    textShaderIndexHandle = SystemAPI.GetComponentTypeHandle<TextShaderIndex>(false),
                    worldBlackboardEntity = worldBlackboardEntity
                }.Schedule(m_glyphsQuery, glyphStreamConstructJh);

                var gpuUploadOperations = new NativeList<GpuUploadOperation>(1, WorldUpdateAllocator);
                var totalUploadBytes = new NativeReference<int>(WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
                var biggestUploadBytes = new NativeReference<int>(WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
                var finalFirstPhaseJh = new MapPayloadsToUploadBufferJob
                {
                    gpuUploadOperationStream = glyphStream.AsReader(),
                    gpuUploadOperations = gpuUploadOperations,
                    totalUploadBytes = totalUploadBytes,
                    biggestUploadBytes = biggestUploadBytes,
                }.Schedule(collectGlyphsJh);
                finalFirstPhaseJh.Complete();

                var numGpuUploadOperations = gpuUploadOperations.Length;
                if (numGpuUploadOperations == 0)
                    return finalFirstPhaseJh;               
                
                if (currentHeap.Length < (ulong)totalUploadBytes.Value)
                    currentHeap = m_GPUPersistentAllocator.Allocate((ulong)totalUploadBytes.Value, 1);

                StartUpdate(totalUploadBytes.Value, biggestUploadBytes.Value, numGpuUploadOperations);
                var uploadsExecuted = new ExecuteGpuUploads
                {
                    GpuUploadOperations = gpuUploadOperations.AsArray(),
                    ThreadedSparseUploader = m_ThreadedGPUUploader,
                }.Schedule(numGpuUploadOperations, 1, finalFirstPhaseJh);
                uploadsExecuted.Complete();
                done = uploadsExecuted;                
            }
            return done;
        }
        private void StartUpdate(int totalUploadBytes, int biggestUploadBytes, int numOperations)
        {
            var persistentBytes = EntityGraphicsInternals.OnePastHighestUsedAddress(ref m_GPUPersistentAllocator);
            if (persistentBytes > (ulong)m_PersistentInstanceDataSize)
            {
                //Debug.Log($"TextMeshDOTS: Growing heap from {m_PersistentInstanceDataSize} to {persistentBytes}");
                while ((ulong)m_PersistentInstanceDataSize < persistentBytes)
                {
                    m_PersistentInstanceDataSize *= 2;
                }

                if (m_PersistentInstanceDataSize > kGPUBufferSizeMax)
                {
                    m_PersistentInstanceDataSize = kGPUBufferSizeMax; // Some backends fails at loading 1024 MiB, but 1023 is fine... This should ideally be a device cap.
                }

                if (persistentBytes > kGPUBufferSizeMax)
                    Debug.LogError("TextMeshDOTS: Current loaded RenderGlyphs need more than 1GiB of persistent GPU memory. This is more than some GPU backends can allocate. Try to reduce amount of loaded data.");

                var newBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)m_PersistentInstanceDataSize / 4, 4);
                m_GPUUploader.ReplaceBuffer(newBuffer, true);

                if (gpuTextBuffer != null)
                    gpuTextBuffer.Dispose();
                gpuTextBuffer = newBuffer;
                Shader.SetGlobalBuffer(textBufferID, gpuTextBuffer);
            }

            m_ThreadedGPUUploader = m_GPUUploader.Begin(totalUploadBytes, biggestUploadBytes, numOperations);
        }
        void EndUpdate()
        {
            if (m_ThreadedGPUUploader.IsValid)
                m_GPUUploader.EndAndCommit(m_ThreadedGPUUploader);

            // Set the uploader struct to null to ensure that any calls
            // to EndAndCommit are made with a struct returned from Begin()
            // on the same frame. This is important in case Begin() is skipped
            // on a frame.
            m_ThreadedGPUUploader = default;
        }
    }
}

