using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

namespace TextMeshDOTS.Rendering
{
    /// <summary>
    /// System Uploads every frame all RenderGlyphs into gpuLatiosTextBuffer starting at offset 0, 
    /// places Glyph Start offsets into TextShaderIndex (also starting at offset 0)
    /// accomodating changed chunks and new chunks and mapping all this into the GPU buffer appears complicated
    /// ...research how to do best
    /// </summary>
    //[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    public partial class TextRenderingDispatchSystem : SystemBase
    {
        EntityQuery m_glyphsQuery;
        SparseUploader m_GPUUploader;
        private ThreadedSparseUploader m_ThreadedGPUUploader;        

        const int kMaxChunkMetadata = 1 * 1024 * 1024;
        const ulong kMaxGPUAllocatorMemory = 1024 * 1024 * 1024; // 1GiB of potential memory space
        const long kGPUBufferSizeInitial = 32 * 1024 * 1024;
        const long kGPUBufferSizeMax = 1023 * 1024 * 1024;
        const int kGPUUploaderChunkSize = 4 * 1024 * 1024;
        long m_PersistentInstanceDataSize;


        //Shader properties valid for entire batch uploaded via SetGlobalBuffer
        GraphicsBuffer gpuLatiosTextBuffer;
        int latiosTextBufferID;

        protected override void OnCreate()
        {
            m_glyphsQuery = SystemAPI.QueryBuilder()
                        .WithAll<RenderGlyph, RenderBounds>()
                        .WithAllRW<TextShaderIndex>()
                        .Build();
            //m_glyphsQuery.SetChangedVersionFilter(ComponentType.ReadWrite<RenderGlyph>());
            //m_glyphsQuery.AddOrderVersionFilter();
            RequireForUpdate(m_glyphsQuery);

            latiosTextBufferID = Shader.PropertyToID("_latiosTextBuffer");
            m_PersistentInstanceDataSize = kGPUBufferSizeInitial;
            gpuLatiosTextBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)m_PersistentInstanceDataSize / 4, 4);
            m_GPUUploader = new SparseUploader(gpuLatiosTextBuffer, kGPUUploaderChunkSize);
            Shader.SetGlobalBuffer(latiosTextBufferID, gpuLatiosTextBuffer);
        }
        protected override void OnDestroy()
        {
            m_GPUUploader.Dispose();
            gpuLatiosTextBuffer.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonEntity<TextStatisticsTag>(out Entity worldBlackboardEntity))
                return;

            var glyphStreamCount = CollectionHelper.CreateNativeArray<int>(1, WorldUpdateAllocator);
            glyphStreamCount[0] = m_glyphsQuery.CalculateChunkCountWithoutFiltering();
            if (glyphStreamCount[0] == 0)
                return;

            var glyphStreamConstructJh = NativeStream.ScheduleConstruct(out var glyphStream, glyphStreamCount, Dependency, WorldUpdateAllocator);
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

            if (numGpuUploadOperations==0)
                return;
            
            m_ThreadedGPUUploader = m_GPUUploader.Begin(totalUploadBytes.Value, biggestUploadBytes.Value, numGpuUploadOperations);
            var uploadsExecuted = new ExecuteGpuUploads
            {
                GpuUploadOperations = gpuUploadOperations.AsArray(),
                ThreadedSparseUploader = m_ThreadedGPUUploader,
            }.Schedule(numGpuUploadOperations, 1);

            uploadsExecuted.Complete();
            gpuUploadOperations.Dispose();
            EndUpdate();
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

