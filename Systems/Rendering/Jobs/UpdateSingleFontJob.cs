using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using TextMeshDOTS.Rendering.Authoring;
using UnityEngine;

namespace TextMeshDOTS.Rendering
{
    [BurstCompile]
    partial struct UpdateSingleFontJob : IJobEntity
    {
        public unsafe void Execute(ref RenderBounds renderBounds, ref TextRenderControl textRenderControl, ref MaterialMeshInfo materialMeshInfo, in DynamicBuffer<RenderGlyph> renderGlyphBuffer)
        {
            if ((textRenderControl.flags & TextRenderControl.Flags.Dirty) != TextRenderControl.Flags.Dirty)
                return;
            textRenderControl.flags &= ~TextRenderControl.Flags.Dirty;

            var aabb = new Aabb { Min = float.MaxValue, Max = float.MinValue };
            var renderGlyphArray = renderGlyphBuffer.AsNativeArray();
            var renderGlyphCount = renderGlyphArray.Length;
            for (int i = 0; i < renderGlyphCount; i++)
            {
                var glyph = renderGlyphArray[i];
                var c = (glyph.blPosition + glyph.trPosition) / 2f;
                var e = math.length(c - glyph.blPosition);
                e += glyph.shear;
                aabb.Include(new Aabb { Min = new float3(c - e, 0f), Max = new float3(c + e, 0f) });
            }
            if (renderGlyphCount == 0)
            {
                aabb.Min = 0f;
                aabb.Max = 0f;
            }
            renderBounds = new RenderBounds { Value = new AABB { Center = aabb.Center, Extents = aabb.Extents } };
            TextBackendBakingUtility.SetSubMesh(renderGlyphCount, ref materialMeshInfo);
        }
    }
}
