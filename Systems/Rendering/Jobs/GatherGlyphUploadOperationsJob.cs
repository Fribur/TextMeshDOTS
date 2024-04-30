using Unity.Burst.Intrinsics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TextMeshDOTS.Rendering
{
    // Schedule Single
    [BurstCompile]
    struct GatherGlyphUploadOperationsJobChunk : IJobChunk
    {
        [ReadOnly] public BufferTypeHandle<RenderGlyph> renderGlyphHandle;
        [ReadOnly] public BufferTypeHandle<RenderGlyphMask> glyphMaskHandle;    //only valid for multi-font
        public ComponentTypeHandle<TextShaderIndex> textShaderIndexHandle;
        public ComponentLookup<GlyphCountThisFrame> glyphCountThisFrameLookup;
        public Entity textStatisticsSingleton;

        [NativeDisableParallelForRestriction] public NativeStream.Writer streamWriter;

        public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            ref var glyphCountThisFrame = ref glyphCountThisFrameLookup.GetRefRW(textStatisticsSingleton).ValueRW.glyphCount;

            streamWriter.BeginForEachIndex(unfilteredChunkIndex);
            var glyphsBuffers = chunk.GetBufferAccessor(ref renderGlyphHandle);
            var masksBuffers = chunk.GetBufferAccessor(ref glyphMaskHandle);
            var textShaderIndices = chunk.GetNativeArray(ref textShaderIndexHandle);

            bool chunkHasMaskBuffer = chunk.Has(ref glyphMaskHandle);
            for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++)
            {
                var buffer = glyphsBuffers[i];
                TextShaderIndex textShaderIndex;
                //if (masksBuffers.Length > 0)//if(chunkHasMaskBuffer) instead of this?
                if (chunkHasMaskBuffer)
                {
                    textShaderIndex = new TextShaderIndex
                    {
                        firstGlyphIndex = glyphCountThisFrame,
                        glyphCount = (uint)masksBuffers[i].Length * 16 //this causes early out in Text Shader to ensure masked glyphs are not rendered
                    };
                }
                else
                {
                    textShaderIndex = new TextShaderIndex
                    {
                        firstGlyphIndex = glyphCountThisFrame,
                        glyphCount = (uint)buffer.Length
                    };
                }
                textShaderIndices[i] = textShaderIndex;

                streamWriter.Write(new GpuUploadOperation
                {
                    Kind = GpuUploadOperation.UploadOperationKind.Memcpy,
                    Src = buffer.GetUnsafeReadOnlyPtr(),
                    DstOffset = (int)glyphCountThisFrame * sizeof(RenderGlyph),
                    DstOffsetInverse = -1,
                    Size = buffer.Length * sizeof(RenderGlyph), //still need to upload entire GlyphBuffer (including masked out glyphs) to ensure the child entities have the data they need
                });
                glyphCountThisFrame += (uint)buffer.Length;
            }
            streamWriter.EndForEachIndex();
        }
    }
}
