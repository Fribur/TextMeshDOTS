using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace TextMeshDOTS.Rendering
{
    [BurstCompile]
    struct MapPayloadsToUploadBufferJob : IJob
    {
        [ReadOnly] public NativeStream.Reader gpuUploadOperationStream;
        public NativeList<GpuUploadOperation> gpuUploadOperations;
        public NativeReference<int> totalUploadBytes;
        public NativeReference<int> biggestUploadBytes;

        public void Execute()
        {
            var totalCount = gpuUploadOperationStream.Count();
            gpuUploadOperations.Capacity = totalCount;
            int totalUploadBytesTemp = 0;  // Prefixing is done in previous job.
            int biggestUploadBytesTemp = 0;
            for (int streamIndex = 0, streamCount= gpuUploadOperationStream.ForEachCount; streamIndex < streamCount; streamIndex++)
            {
                var count = gpuUploadOperationStream.BeginForEachIndex(streamIndex);
                for (int i = 0; i < count; i++)
                {
                    var gpuUploadOperation = gpuUploadOperationStream.Read<GpuUploadOperation>();
                    var numBytes = gpuUploadOperation.BytesRequiredInUploadBuffer;
                    totalUploadBytesTemp += numBytes;
                    biggestUploadBytesTemp = math.max(biggestUploadBytesTemp, numBytes);
                    gpuUploadOperations.AddNoResize(gpuUploadOperation);
                }
            }
            totalUploadBytes.Value = totalUploadBytesTemp;
            biggestUploadBytes.Value = biggestUploadBytesTemp;
        }
    }
}
