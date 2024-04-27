using Unity.Burst.Intrinsics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TextMeshDOTS.Rendering
{
    // Schedule Single
    [BurstCompile]
    struct GatherGlyphUploadOperationsJob : IJobChunk
    {
        [ReadOnly] public BufferTypeHandle<RenderGlyph> renderGlyphHandle;
        public ComponentTypeHandle<TextShaderIndex> textShaderIndexHandle;
        public ComponentLookup<GlyphCountThisFrame> glyphCountThisFrameLookup;
        public Entity worldBlackboardEntity;
        public uint glyphCountThisPass;

        public uint lastSystemVersion;

        [NativeDisableParallelForRestriction] public NativeStream.Writer streamWriter;

        public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            ref var glyphCountThisFrame = ref glyphCountThisFrameLookup.GetRefRW(worldBlackboardEntity).ValueRW.glyphCount;

            streamWriter.BeginForEachIndex(unfilteredChunkIndex);
            var glyphsBuffers = chunk.GetBufferAccessor(ref renderGlyphHandle);
            var shaderIndices = chunk.GetNativeArray(ref textShaderIndexHandle);

            for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
            {

                var buffer = glyphsBuffers[i];
                int glyphCount = buffer.Length;

                var textShaderIndex = new TextShaderIndex
                {
                    firstGlyphIndex = glyphCountThisFrame,
                    glyphCount = (uint)glyphCount
                };
                shaderIndices[i] = textShaderIndex;

                streamWriter.Write(new GpuUploadOperation
                {
                    Kind = GpuUploadOperation.UploadOperationKind.Memcpy,
                    Src = buffer.GetUnsafeReadOnlyPtr(),
                    //DstOffset = (int)textShaderIndex.firstGlyphIndex * 96,
                    DstOffset = (int)glyphCountThisPass * 96,
                    DstOffsetInverse = -1,
                    Size = glyphCount * 96,
                });

                glyphCountThisPass += (uint)glyphCount;
                glyphCountThisFrame += (uint)glyphCount;
            }
            streamWriter.EndForEachIndex();
        }
    }
}
