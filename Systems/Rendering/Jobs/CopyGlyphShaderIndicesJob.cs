using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace TextMeshDOTS.Rendering
{
    // Schedule Parallel

    [BurstCompile]
    partial struct CopyGlyphShaderIndicesJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<RenderGlyphMask> renderGlyphMaskLookup;
        [NativeDisableContainerSafetyRestriction] public ComponentLookup<TextShaderIndex> textShaderIndexLookup;
        void Execute(in TextShaderIndex textShaderIndices, DynamicBuffer<AdditionalFontMaterialEntity> additionalEntitiesBuffer)
        {
            foreach (var child in additionalEntitiesBuffer)
            {
                var maskBuffer = renderGlyphMaskLookup[child.entity];
                textShaderIndexLookup[child.entity] = new TextShaderIndex 
                { 
                    firstGlyphIndex = textShaderIndices.firstGlyphIndex, 
                    glyphCount = (uint)(16 * maskBuffer.Length) 
                };
            }
        }
    }
}
