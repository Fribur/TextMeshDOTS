using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

namespace TextMeshDOTS.Rendering
{
    /// <summary>
    /// When changed RenderGlyph chunks are detected, the system builds the entire gpuTextBuffer fresh. Uploads 
    /// into  gpuTextBuffer start at offset 0, TextShaderIndex.firstGlyphIndex also starting at 0. Patching up 
    /// chunks to accomodate changed chunks appears complicated, prone to fragmentation 
    /// --> research if it's worth it and how to do
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    public partial class TextRenderingDispatchSystem : SystemBase
    {
        EntityQuery m_glyphsQuery, m_changedGlyphsQuery, m_glyphsAndMasksQuery, m_masksQuery;

        #region setup _textBuffer Shader Property
        GraphicsBuffer m_GPUTextBuffer;
        int m_textBufferID;
        SparseUploader m_TextGPUUploader;
        ThreadedSparseUploader m_TextThreadedGPUUploader;
        HeapAllocator m_TextGPUAllocator;
        HeapBlock m_TextCurrentHeap;
        long m_TextCurrentDataSize;
        const long m_TextGPUBufferSizeInitial = 8 * 1024 * 1024; //start with 8MB textbuffer, will grow when more is needed
        const long m_TextGPUBufferSizeMax = 1023 * 1024 * 1024;
        #endregion

        #region setup _textMAskBuffer Shader Property
        GraphicsBuffer m_GPUMaskBuffer;
        int m_MaskBufferID;
        SparseUploader m_MaskGPUUploader;
        ThreadedSparseUploader m_MaskThreadedGPUUploader;
        HeapAllocator m_MaskGPUAllocator;
        HeapBlock m_MaskCurrentHeap;
        long m_MaskCurrentDataSize;
        const long m_MaskGPUBufferSizeInitial = 256 * 1024; //start with 256kB maskbuffer, will grow when more is needed
        const long m_MaskGPUBufferSizeMax = 1 * 1024 * 1024;
        #endregion

        const ulong kMaxGPUAllocatorMemory = 1024 * 1024 * 1024; // 1GiB of potential memory space
        const int kGPUUploaderChunkSize = 4 * 1024 * 1024;

        protected override void OnCreate()
        {
            //query single font entitities, and parents entities that have multiple fonts (have AdditionalFontMaterialEntity)
            m_glyphsQuery = SystemAPI.QueryBuilder()
                       .WithAll<RenderGlyph, TextRenderControl, RenderBounds>()
                       .WithAllRW<TextShaderIndex>()
                       .Build();

            //same as m_glyphsQuery, except detecing changes is RenderGlyph. This system will only run and
            //fully rebuild all GPU buffer when this query has entities
            m_changedGlyphsQuery = SystemAPI.QueryBuilder()
                       .WithAll<RenderGlyph, TextRenderControl, RenderBounds>()
                       .WithAll<TextShaderIndex>()
                       .Build();
            m_changedGlyphsQuery.SetChangedVersionFilter(ComponentType.ReadWrite<RenderGlyph>());

            //query all entities having a mask, regardless if they are parent (= have AdditionalFontMaterialEntity) or child 
            m_masksQuery = SystemAPI.QueryBuilder()
                        .WithAllRW<TextMaterialMaskShaderIndex>() //review if I need RW
                        .WithAll<RenderBounds, RenderGlyphMask>()
                        .Build();

            //query parents entities that have multiple fonts (have AdditionalFontMaterialEntity)
            m_glyphsAndMasksQuery = SystemAPI.QueryBuilder()
                        .WithAll<RenderGlyph, TextRenderControl, RenderBounds>()
                        .WithAll<TextShaderIndex, TextMaterialMaskShaderIndex, RenderGlyphMask>()
                        .WithAll<AdditionalFontMaterialEntity>()
                        .Build();


            //setup textBuffer            
            m_TextGPUAllocator = new HeapAllocator(kMaxGPUAllocatorMemory, 16);
            m_TextCurrentDataSize = m_TextGPUBufferSizeInitial;
            m_GPUTextBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)m_TextCurrentDataSize / 4, 4);
            m_TextCurrentHeap = m_TextGPUAllocator.Allocate((ulong)m_TextCurrentDataSize, 1);
            m_TextGPUUploader = new SparseUploader(m_GPUTextBuffer, kGPUUploaderChunkSize);
            m_textBufferID = Shader.PropertyToID("_textBuffer");
            Shader.SetGlobalBuffer(m_textBufferID, m_GPUTextBuffer);

            //setup MaskBuffer
            m_MaskGPUAllocator = new HeapAllocator(kMaxGPUAllocatorMemory, 16);
            m_MaskCurrentDataSize = m_MaskGPUBufferSizeInitial;
            m_GPUMaskBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)m_MaskCurrentDataSize / 4, 4);
            m_MaskCurrentHeap = m_MaskGPUAllocator.Allocate((ulong)m_MaskCurrentDataSize, 1);
            m_MaskGPUUploader = new SparseUploader(m_GPUMaskBuffer, kGPUUploaderChunkSize);
            m_MaskBufferID = Shader.PropertyToID("_textMaskBuffer");
            Shader.SetGlobalBuffer(m_MaskBufferID, m_GPUMaskBuffer);
        }
        protected override void OnDestroy()
        {
            m_TextGPUUploader.Dispose();
            m_GPUTextBuffer.Dispose();
            m_TextGPUAllocator.Dispose();
            m_MaskGPUAllocator.Dispose();
        }

        protected override void OnUpdate()
        {
            Entity textStats = SystemAPI.GetSingletonEntity<TextStatisticsTag>();

            if (m_glyphsQuery.IsEmpty)
                return;

            if (m_changedGlyphsQuery.IsEmpty)
                return;

            try
            {
                Dependency = UpdateRenderGlyphChunks(Dependency, textStats);
                EndBufferUpdate(ref m_TextThreadedGPUUploader, ref m_TextGPUUploader);
                EndBufferUpdate(ref m_MaskThreadedGPUUploader, ref m_MaskGPUUploader);
            }
            finally
            {
                m_TextGPUUploader.FrameCleanup();
                m_MaskGPUUploader.FrameCleanup();
            }
        }
        JobHandle UpdateRenderGlyphChunks(JobHandle inputDependencies, Entity textStatisticsSingleton)
        {
            JobHandle done = inputDependencies;
            var glyphStreamCount = CollectionHelper.CreateNativeArray<int>(1, WorldUpdateAllocator);
            glyphStreamCount[0] = m_glyphsQuery.CalculateChunkCountWithoutFiltering();

            var glyphsWithChildrenCount = m_glyphsAndMasksQuery.CalculateChunkCountWithoutFiltering();

            //first schedule all jobs related to updating textBuffer
            var glyphStreamConstructJh = NativeStream.ScheduleConstruct(out var glyphStream, glyphStreamCount, inputDependencies, WorldUpdateAllocator);
            var collectGlyphsJh = new GatherGlyphUploadOperationsJobChunk
            {
                glyphCountThisFrameLookup = SystemAPI.GetComponentLookup<GlyphCountThisFrame>(false),
                renderGlyphHandle = SystemAPI.GetBufferTypeHandle<RenderGlyph>(true),
                glyphMaskHandle = SystemAPI.GetBufferTypeHandle<RenderGlyphMask>(true),
                streamWriter = glyphStream.AsWriter(),
                textShaderIndexHandle = SystemAPI.GetComponentTypeHandle<TextShaderIndex>(false),
                textStatisticsSingleton = textStatisticsSingleton
            }.Schedule(m_glyphsQuery, glyphStreamConstructJh);

            var textGPUUploadOperations = new NativeList<GpuUploadOperation>(1, WorldUpdateAllocator);
            var textTotalUploadBytes = new NativeReference<int>(WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var textBiggestUploadBytes = new NativeReference<int>(WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var finalFirstPhaseJh = new MapPayloadsToUploadBufferJob
            {
                gpuUploadOperationStream = glyphStream.AsReader(),
                gpuUploadOperations = textGPUUploadOperations,
                totalUploadBytes = textTotalUploadBytes,
                biggestUploadBytes = textBiggestUploadBytes,
            }.Schedule(collectGlyphsJh);
            finalFirstPhaseJh.Complete();

            var textGpuUploadOperationsCount = textGPUUploadOperations.Length;
            if (textGpuUploadOperationsCount != 0)
            {
                if (m_TextCurrentHeap.Length < (ulong)textTotalUploadBytes.Value)
                    m_TextCurrentHeap = m_TextGPUAllocator.Allocate((ulong)textTotalUploadBytes.Value, 1);

                m_TextThreadedGPUUploader = StartBufferUpdate(ref m_GPUTextBuffer, m_textBufferID, m_TextGPUBufferSizeMax, ref m_TextGPUUploader, ref m_TextGPUAllocator, m_TextCurrentDataSize, textTotalUploadBytes.Value, textBiggestUploadBytes.Value, textGpuUploadOperationsCount);
                var textUploadsExecuted = new ExecuteGpuUploads
                {
                    GpuUploadOperations = textGPUUploadOperations.AsArray(),
                    ThreadedSparseUploader = m_TextThreadedGPUUploader,
                }.Schedule(textGpuUploadOperationsCount, 1, finalFirstPhaseJh);
                done = textUploadsExecuted;
            }
            else
                done = finalFirstPhaseJh;

            //second, schedule all jobs related to updating textMaskBuffer
            if (glyphsWithChildrenCount > 0)
            {
                var maskStreamCount = CollectionHelper.CreateNativeArray<int>(1, WorldUpdateAllocator);
                maskStreamCount[0] = m_masksQuery.CalculateChunkCountWithoutFiltering();//change this to m_masksQuery when combining with single font system

                var maskStreamConstructJh = NativeStream.ScheduleConstruct(out var maskStream, maskStreamCount, done, WorldUpdateAllocator);
                var collectMasksJh = new GatherMaskUploadOperationsJobChunk
                {
                    glyphMasksHandle = SystemAPI.GetBufferTypeHandle<RenderGlyphMask>(true),
                    maskCountThisFrameLookup = SystemAPI.GetComponentLookup<MaskCountThisFrame>(false),
                    textMaterialMaskShaderIndexHandle = SystemAPI.GetComponentTypeHandle<TextMaterialMaskShaderIndex>(false),
                    streamWriter = maskStream.AsWriter(),
                    textStatisticsSingleton = textStatisticsSingleton
                }.Schedule(m_masksQuery, JobHandle.CombineDependencies(maskStreamConstructJh, done));//change this to m_masksQuery when combining with single font system

                var maskGPUUploadOperations = new NativeList<GpuUploadOperation>(1, WorldUpdateAllocator);
                var totalMaskUploadBytes = new NativeReference<int>(WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
                var biggestMaskUploadBytes = new NativeReference<int>(WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
                var batchMasksJh = new MapPayloadsToUploadBufferJob
                {
                    gpuUploadOperationStream = maskStream.AsReader(),
                    gpuUploadOperations = maskGPUUploadOperations,
                    totalUploadBytes = totalMaskUploadBytes,
                    biggestUploadBytes = biggestMaskUploadBytes,
                }.Schedule(collectMasksJh);

                var copyPropertiesJh = new CopyGlyphShaderIndicesJob
                {
                    renderGlyphMaskLookup = SystemAPI.GetBufferLookup<RenderGlyphMask>(true),
                    textShaderIndexLookup = SystemAPI.GetComponentLookup<TextShaderIndex>(false)
                }.ScheduleParallel(m_glyphsAndMasksQuery, collectGlyphsJh);

                finalFirstPhaseJh = JobHandle.CombineDependencies(batchMasksJh, copyPropertiesJh);

                finalFirstPhaseJh.Complete();

                var numGpuMaskUploadOperations = maskGPUUploadOperations.Length;
                if (numGpuMaskUploadOperations != 0)
                {
                    if (m_MaskCurrentHeap.Length < (ulong)totalMaskUploadBytes.Value)
                        m_MaskCurrentHeap = m_MaskGPUAllocator.Allocate((ulong)totalMaskUploadBytes.Value, 1);

                    m_MaskThreadedGPUUploader = StartBufferUpdate(ref m_GPUMaskBuffer, m_MaskBufferID, m_MaskGPUBufferSizeMax, ref m_MaskGPUUploader, ref m_MaskGPUAllocator, m_MaskCurrentDataSize, totalMaskUploadBytes.Value, biggestMaskUploadBytes.Value, numGpuMaskUploadOperations);
                    var maskUploadsExecuted = new ExecuteGpuUploads
                    {
                        GpuUploadOperations = maskGPUUploadOperations.AsArray(),
                        ThreadedSparseUploader = m_MaskThreadedGPUUploader,
                    }.Schedule(numGpuMaskUploadOperations, 1, batchMasksJh);
                    done = maskUploadsExecuted;
                }
                else
                    done = batchMasksJh;
            }
            done.Complete();
            return done;
        }

        static ThreadedSparseUploader StartBufferUpdate(
            ref GraphicsBuffer targetBuffer,
            int targetBufferID,
            long targetBufferMaxSize,
            ref SparseUploader sparseUploader,
            ref HeapAllocator heapAllocator,
            long currentBufferBytes,
            int totalUploadBytes,
            int biggestUploadBytes,
            int uploadOperationsCount)
        {
            var persistentBytes = (long)EntityGraphicsInternals.OnePastHighestUsedAddress(ref heapAllocator);
            if (persistentBytes > currentBufferBytes)
            {
                //Debug.Log($"TextMeshDOTS: Growing heap from {currentBufferBytes} to at least {persistentBytes}");
                while (currentBufferBytes < persistentBytes)
                    currentBufferBytes *= 2;

                if (currentBufferBytes > targetBufferMaxSize)
                    currentBufferBytes = targetBufferMaxSize; // Some backends fails at loading 1024 MiB, but 1023 is fine... This should ideally be a device cap.

                if (persistentBytes > targetBufferMaxSize)
                    Debug.LogError("TextMeshDOTS: The text of mask GPU buffer needs more than 1GiB of persistent GPU memory. This is more than some GPU backends can allocate. Try to reduce amount of loaded data.");

                var newBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, (int)currentBufferBytes / 4, 4);
                sparseUploader.ReplaceBuffer(newBuffer, true);

                if (targetBuffer != null)
                    targetBuffer.Dispose();
                targetBuffer = newBuffer;
                Shader.SetGlobalBuffer(targetBufferID, targetBuffer);
            }
            return sparseUploader.Begin(totalUploadBytes, biggestUploadBytes, uploadOperationsCount);
        }
        void EndBufferUpdate(ref ThreadedSparseUploader m_MaskThreadedGPUUploader, ref SparseUploader m_MaskGPUUploader)
        {
            if (m_MaskThreadedGPUUploader.IsValid)
                m_MaskGPUUploader.EndAndCommit(m_MaskThreadedGPUUploader);

            // Set the uploader struct to null to ensure that any calls
            // to EndAndCommit are made with a struct returned from Begin()
            // on the same frame. This is important in case Begin() is skipped
            // on a frame.
            m_MaskThreadedGPUUploader = default;
        }
    }
}

