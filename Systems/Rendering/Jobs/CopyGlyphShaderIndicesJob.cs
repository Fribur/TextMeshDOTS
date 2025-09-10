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
        [NativeDisableContainerSafetyRestriction] public ComponentLookup<TextShaderIndexOld> textShaderIndexLookup;
        void Execute(in TextShaderIndexOld textShaderIndices, DynamicBuffer<AdditionalFontMaterialEntity> additionalEntitiesBuffer)
        {
            foreach (var child in additionalEntitiesBuffer)
            {
                var maskBuffer = renderGlyphMaskLookup[child.entity];
                textShaderIndexLookup[child.entity] = new TextShaderIndexOld 
                { 
                    firstGlyphIndex = textShaderIndices.firstGlyphIndex, 
                    glyphCount = (uint)(16 * maskBuffer.Length) 
                };
            }
        }
    }
}
