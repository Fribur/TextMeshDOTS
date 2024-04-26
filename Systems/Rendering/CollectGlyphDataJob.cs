using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TextMeshDOTS.Rendering
{
    [BurstCompile]
    partial struct CollectEGGlyphDataJob : IJobEntity
    {
        //keep track of firstGlyphIndex and GlyphCount across all entities
        public uint firstGlyphIndex;

        //glyph data for global _latiosTextBuffer 
        public NativeList<RenderGlyph> renderGlyphs;

        void Execute(ref TextShaderIndex textShaderIndex, in DynamicBuffer<RenderGlyph> renderGlyphBuffer)
        {            
            textShaderIndex.firstGlyphIndex = firstGlyphIndex;
            textShaderIndex.glyphCount = (uint)renderGlyphBuffer.Length;
            firstGlyphIndex += textShaderIndex.glyphCount;
            renderGlyphs.AddRange(renderGlyphBuffer.AsNativeArray());
        }
    }    
}
