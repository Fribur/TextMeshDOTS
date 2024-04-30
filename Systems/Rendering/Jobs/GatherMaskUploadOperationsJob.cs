using Unity.Burst.Intrinsics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TextMeshDOTS.Rendering
{
    // Schedule Single
    [BurstCompile]
    struct GatherMaskUploadOperationsJobChunk : IJobChunk
    {
        [ReadOnly] public BufferTypeHandle<RenderGlyphMask> glyphMasksHandle;
        public ComponentTypeHandle<TextMaterialMaskShaderIndex> textMaterialMaskShaderIndexHandle;
        public ComponentLookup<MaskCountThisFrame> maskCountThisFrameLookup;
        public Entity textStatisticsSingleton;

        [NativeDisableParallelForRestriction] public NativeStream.Writer streamWriter;

        public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            ref var maskCountThisFrame = ref maskCountThisFrameLookup.GetRefRW(textStatisticsSingleton).ValueRW.maskCount;

            streamWriter.BeginForEachIndex(unfilteredChunkIndex);
            var glyphMasksBuffers = chunk.GetBufferAccessor(ref glyphMasksHandle);
            var textMaterialMaskShaderIndices = chunk.GetNativeArray(ref textMaterialMaskShaderIndexHandle);

            for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
            {
                var buffer = glyphMasksBuffers[i];
                var textMaterialMaskShaderIndex = new TextMaterialMaskShaderIndex
                {
                    firstMaskIndex = maskCountThisFrame
                };
                textMaterialMaskShaderIndices[i] = textMaterialMaskShaderIndex;

                streamWriter.Write(new GpuUploadOperation
                {
                    Kind = GpuUploadOperation.UploadOperationKind.Memcpy,
                    Src = buffer.GetUnsafeReadOnlyPtr(),
                    DstOffset = (int)maskCountThisFrame * sizeof(uint),
                    DstOffsetInverse = -1,
                    Size = buffer.Length * sizeof(uint),
                });
                maskCountThisFrame += (uint)buffer.Length;
            }
            streamWriter.EndForEachIndex();
        }
    }    
}
