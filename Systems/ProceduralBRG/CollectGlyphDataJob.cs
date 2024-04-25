using System.Runtime.CompilerServices;
using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Chart3D.LayerSpawner.Jobs
{
    [BurstCompile]
    partial struct CollectGlyphDataJob : IJobEntity
    {
        //data for persistent GraphicsBuffer providing instanced shader property data
        public NativeArray<float3x4> objectToWorld;
        public NativeArray<float3x4> worldToObject;
        public NativeArray<TextShaderIndex> textShaderIndices;
        public NativeArray<uint> maxTextLength;

        //the combined Bounds
        public NativeArray<AABB> globalBounds;

        //data for visible instances GraphicsBuffer 
        public NativeArray<int> visibleInstances;

        //keep track of firstGlyphIndex and GlyphCount across all entities
        public uint firstGlyphIndex;

        //glyph data for global _latiosTextBuffer 
        public NativeList<RenderGlyph> renderGlyphs;

        void Execute([EntityIndexInQuery] int idx, 
            ref TextShaderIndex textShaderIndex, 
            in LocalToWorld localToWorld, 
            in RenderBounds renderBounds, 
            in DynamicBuffer<RenderGlyph> renderGlyphBuffer)
        {            
            textShaderIndex.firstGlyphIndex = firstGlyphIndex;
            textShaderIndex.glyphCount = (uint)renderGlyphBuffer.Length;
            firstGlyphIndex += textShaderIndex.glyphCount;
            maxTextLength[0] = math.max(maxTextLength[0], textShaderIndex.glyphCount);

            //collect data for global instance property buffer
            objectToWorld[idx] = BRGStaticHelper.GetPackedMatrix(localToWorld.Value);                 // compute the new current frame matrix
            worldToObject[idx] = BRGStaticHelper.GetPackedMatrix(math.inverse(localToWorld.Value));   // compute the new inverse matrix
            textShaderIndices[idx] = textShaderIndex;
            visibleInstances[idx] = idx;

            //collect glyph data for global _latiosTextBuffer
            renderGlyphs.AddRange(renderGlyphBuffer.AsNativeArray());

            globalBounds[0] = GetAABB(math.min(globalBounds[0].Min, renderBounds.Value.Min), 
                                      math.max(globalBounds[0].Max, renderBounds.Value.Max));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AABB GetAABB(float3 min, float3 max)
        {
            var extents = (max - min) * 0.5f;
            var center = min + extents;
            return new AABB { Extents = extents, Center = center };
        }
    }    
}
