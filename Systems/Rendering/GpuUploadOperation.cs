using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace TextMeshDOTS.Rendering
{
    // Describes a single set of data to be uploaded from the CPU to the GPU during this frame.
    // The operations are collected up front so their total size can be known for buffer allocation
    // purposes, and for effectively load balancing the upload memcpy work.
    internal unsafe struct GpuUploadOperation
    {
        public enum UploadOperationKind
        {
            Memcpy, // raw upload of a byte block to the GPU
            SOAMatrixUpload3x4, // upload matrices from CPU, invert on GPU, write in SoA arrays, 3x4 destination
            SOAMatrixUpload4x4, // upload matrices from CPU, invert on GPU, write in SoA arrays, 4x4 destination
            // TwoMatrixUpload, // upload matrices from CPU, write them and their inverses to GPU (for transform sharing branch)
        }

        // Which kind of upload operation this is
        public UploadOperationKind Kind;
        // If a matrix upload, what matrix type is this?
        public ThreadedSparseUploader.MatrixType SrcMatrixType;
        // Pointer to source data, whether raw byte data or float4x4 matrices
        public void* Src;
        // GPU offset to start writing destination data in
        public int DstOffset;
        // GPU offset to start writing any inverse matrices in, if applicable
        public int DstOffsetInverse;
        // Size in bytes for raw operations, size in whole matrices for matrix operations
        public int Size;

        // Raw uploads require their size in bytes from the upload buffer.
        // Matrix operations require a single 48-byte matrix per matrix.
        public int BytesRequiredInUploadBuffer => (Kind == UploadOperationKind.Memcpy)
            ? Size
            : (Size * UnsafeUtility.SizeOf<float3x4>());
    }

    // Describes a GPU blitting operation (= same bytes replicated over a larger area).
    internal struct ValueBlitDescriptor
    {
        public float4x4 Value;
        public uint DestinationOffset;
        public uint ValueSizeBytes;
        public uint Count;

        public int BytesRequiredInUploadBuffer => (int)(ValueSizeBytes * Count);
    }

    [BurstCompile]
    internal unsafe struct ExecuteGpuUploads : IJobParallelFor
    {
        [ReadOnly] public NativeArray<GpuUploadOperation> GpuUploadOperations;
        public ThreadedSparseUploader ThreadedSparseUploader;

        public void Execute(int index)
        {
            var uploadOperation = GpuUploadOperations[index];
            switch (uploadOperation.Kind)
            {
                case GpuUploadOperation.UploadOperationKind.Memcpy:
                    ThreadedSparseUploader.AddUpload(
                        uploadOperation.Src,
                        uploadOperation.Size,
                        uploadOperation.DstOffset);
                    break;
                case GpuUploadOperation.UploadOperationKind.SOAMatrixUpload3x4:
                case GpuUploadOperation.UploadOperationKind.SOAMatrixUpload4x4:
                    var dstType = (uploadOperation.Kind == GpuUploadOperation.UploadOperationKind.SOAMatrixUpload3x4)
                            ? ThreadedSparseUploader.MatrixType.MatrixType3x4
                            : ThreadedSparseUploader.MatrixType.MatrixType4x4;
                    if (uploadOperation.DstOffsetInverse < 0)
                    {
                        ThreadedSparseUploader.AddMatrixUpload(
                            uploadOperation.Src,
                            uploadOperation.Size,
                            uploadOperation.DstOffset,
                            uploadOperation.SrcMatrixType,
                            dstType);
                    }
                    else
                    {
                        ThreadedSparseUploader.AddMatrixUploadAndInverse(
                            uploadOperation.Src,
                            uploadOperation.Size,
                            uploadOperation.DstOffset,
                            uploadOperation.DstOffsetInverse,
                            uploadOperation.SrcMatrixType,
                            dstType);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    [BurstCompile]
    internal unsafe struct UploadBlitJob : IJobParallelFor
    {
        [ReadOnly] public NativeList<ValueBlitDescriptor> BlitList;
        public ThreadedSparseUploader ThreadedSparseUploader;

        public void Execute(int index)
        {
            ValueBlitDescriptor blit = BlitList[index];
            ThreadedSparseUploader.AddUpload(
                &blit.Value,
                (int)blit.ValueSizeBytes,
                (int)blit.DestinationOffset,
                (int)blit.Count);
        }
    }
}
