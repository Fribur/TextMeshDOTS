using Unity.Burst.Intrinsics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using TextMeshDOTS.HarfBuzz;
using Unity.Mathematics;

namespace TextMeshDOTS.Rendering
{
    [BurstCompile]
    partial struct TempPatchUVsJob : IJobEntity
    {
        [ReadOnly] public FontTable fontTable;
        [ReadOnly] public GlyphTable glyphTable;
        [ReadOnly] public ComponentLookup<AtlasData> atlasLookup;
        [ReadOnly] public ComponentLookup<DynamicFontAsset> dynamicFontLookup;

        public void Execute(ref DynamicBuffer<RenderGlyphOld> glyphBuffer)
        {
            for (int i = 0; i < glyphBuffer.Length; i++)
            {
                ref var glyph = ref glyphBuffer.ElementAt(i);
                var entry = glyphTable.GetEntry(glyph.glyphID);
                var fontEntity = fontTable.faceIndexToFontEntityMap[entry.key.faceIndex];
                var atlas = atlasLookup[fontEntity];

                float2 blUVA, tlUVA, trUVA, brUVA;
                blUVA.x = (entry.xOld - entry.paddingOld) / (float)atlas.atlasWidth;
                blUVA.y = (entry.yOld - entry.paddingOld) / (float)atlas.atlasHeight;

                tlUVA.x = blUVA.x;
                tlUVA.y = (entry.yOld + entry.paddingOld + entry.height) / (float)atlas.atlasHeight;

                trUVA.x = (entry.xOld + entry.paddingOld + entry.width) / (float)atlas.atlasWidth;
                trUVA.y = tlUVA.y;

                brUVA.x = trUVA.x;
                brUVA.y = blUVA.y;

                glyph.blUVA = blUVA;
                glyph.trUVA = trUVA;
            }
        }
    }

    // Schedule Single
    [BurstCompile]
    struct GatherGlyphUploadOperationsJobChunk : IJobChunk
    {
        [ReadOnly] public BufferTypeHandle<RenderGlyphOld> renderGlyphHandle;
        [ReadOnly] public BufferTypeHandle<RenderGlyphMask> glyphMaskHandle;    //only valid for multi-font
        public ComponentTypeHandle<TextShaderIndexOld> textShaderIndexHandle;
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
                TextShaderIndexOld textShaderIndex;
                //if (masksBuffers.Length > 0)//if(chunkHasMaskBuffer) instead of this?
                if (chunkHasMaskBuffer)
                {
                    textShaderIndex = new TextShaderIndexOld
                    {
                        firstGlyphIndex = glyphCountThisFrame,
                        glyphCount = (uint)masksBuffers[i].Length * 16 //this causes early out in Text Shader to ensure masked glyphs are not rendered
                    };
                }
                else
                {
                    textShaderIndex = new TextShaderIndexOld
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
                    DstOffset = (int)glyphCountThisFrame * sizeof(RenderGlyphOld),
                    DstOffsetInverse = -1,
                    Size = buffer.Length * sizeof(RenderGlyphOld), //still need to upload entire GlyphBuffer (including masked out glyphs) to ensure the child entities have the data they need
                });
                glyphCountThisFrame += (uint)buffer.Length;
            }
            streamWriter.EndForEachIndex();
        }
    }
}
