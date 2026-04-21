#pragma once

/*
 * Copyright (C) 2026  Behdad Esfahbod
 *
 *  This is part of HarfBuzz, a text shaping library.
 *
 * Permission is hereby granted, without written agreement and without
 * license or royalty fees, to use, copy, modify, and distribute this
 * software and its documentation for any purpose, provided that the
 * above copyright notice and the following two paragraphs appear in
 * all copies of this software.
 *
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE TO ANY PARTY FOR
 * DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES
 * ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION, EVEN
 * IF THE COPYRIGHT HOLDER HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 *
 * THE COPYRIGHT HOLDER SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING,
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE.  THE SOFTWARE PROVIDED HEREUNDER IS
 * ON AN "AS IS" BASIS, AND THE COPYRIGHT HOLDER HAS NO OBLIGATION TO
 * PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
 */


/* Shared fragment-shader helpers for the hb-gpu renderers.
 *
 * Requires Shader Model 5.0+.
 *
 * The caller must declare:
 *   StructuredBuffer<int4> hb_gpu_atlas : register(t0);
 */

#include "TmdGlobalsApi_GPU.hlsl"

// HB_GPU_UNITS_PER_EM defines the precision of the encoding
#ifndef HB_GPU_UNITS_PER_EM
#define HB_GPU_UNITS_PER_EM 4
#endif

#define HB_GPU_INV_UNITS (1.0 / (float) HB_GPU_UNITS_PER_EM)

// Fetch a texel from the GPU blob atlas (RGBA16I = 8 bytes per texel)
int4 hb_gpu_fetch(int offset)
{
    int2 data = _hbGpuAtlas[offset];
    return int4(
        (data.x << 16) >> 16,
        data.x >> 16,
        (data.y << 16) >> 16,
        data.y >> 16
    );
}
/*
 * Calculate root code for quadratic curve intersection
 */
uint _hb_gpu_calc_root_code(float y1, float y2, float y3)
{
    uint i1 = asuint(y1) >> 31u;
    uint i2 = asuint(y2) >> 30u;
    uint i3 = asuint(y3) >> 29u;

    uint shift = (i2 & 2u) | (i1 & ~2u);
    shift = (i3 & 4u) | (shift & ~4u);

    return (0x2E74u >> shift) & 0x0101u;
}

/*
 * Solve horizontal quadratic polynomial for curve intersections
 */
float2 _hb_gpu_solve_horiz_poly(float2 a, float2 b, float2 p1)
{
    float ra = 1.0 / a.y;
    float rb = 0.5 / b.y;

    float d = sqrt(max(b.y * b.y - a.y * p1.y, 0.0));
    float t1 = (b.y - d) * ra;
    float t2 = (b.y + d) * ra;

    if (a.y == 0.0)
        t1 = t2 = p1.y * rb;

    return float2((a.x * t1 - b.x * 2.0) * t1 + p1.x,
                  (a.x * t2 - b.x * 2.0) * t2 + p1.x);
}

/*
 * Solve vertical quadratic polynomial for curve intersections
 */
float2 _hb_gpu_solve_vert_poly(float2 a, float2 b, float2 p1)
{
    float ra = 1.0 / a.x;
    float rb = 0.5 / b.x;

    float d = sqrt(max(b.x * b.x - a.x * p1.x, 0.0));
    float t1 = (b.x - d) * ra;
    float t2 = (b.x + d) * ra;

    if (a.x == 0.0)
        t1 = t2 = p1.x * rb;

    return float2((a.y * t1 - b.y * 2.0) * t1 + p1.y,
                  (a.y * t2 - b.y * 2.0) * t2 + p1.y);
}

/*
 * Calculate coverage from horizontal and vertical ray casting
 */
float _hb_gpu_calc_coverage(float xcov, float ycov, float xwgt, float ywgt)
{
    float coverage = max(abs(xcov * xwgt + ycov * ywgt) /
                        max(xwgt + ywgt, 1.0 / 65536.0),
                        min(abs(xcov), abs(ycov)));

    return clamp(coverage, 0.0, 1.0);
}

/*
 * Glyph info structure decoded from blob header
 */
struct _hb_gpu_glyph_info
{
    int glyphLoc;     // Texel offset of glyph blob in atlas
    int bandBase;     // Offset to band data
    int2 bandIndex;   // Current band indices (x=vband, y=hband)
    int numHBands;    // Number of horizontal bands
    int numVBands;    // Number of vertical bands
    float2 scale;     // Scale factor for this glyph
};

/*
 * Decode glyph header from blob
 *
 * renderCoord: em-space sample position
 * glyphLoc: byte offset of glyph blob in atlas (divided by 8 internally to get texel offset)
 */
_hb_gpu_glyph_info _hb_gpu_decode_glyph(float2 renderCoord, uint glyphLoc)
{
    _hb_gpu_glyph_info gi;
    gi.glyphLoc = (int)glyphLoc;

    // Read glyph header (first 2 texels)
    int4 header0 = hb_gpu_fetch(gi.glyphLoc);
    int4 header1 = hb_gpu_fetch(gi.glyphLoc + 1);

    // Extents in font design units
    float4 ext = (float4)header0 * HB_GPU_INV_UNITS;
    gi.numHBands = header1.r;
    gi.numVBands = header1.g;
    gi.scale = float2((float)header1.b, (float)header1.a);

    // Calculate band indices for this render coordinate
    float2 extSize = ext.zw - ext.xy;
    float2 bandScale = float2((float)gi.numVBands, (float)gi.numHBands) / max(extSize, float2(1.0 / 65536.0, 1.0 / 65536.0));
    float2 bandOffset = -ext.xy * bandScale;

    gi.bandIndex = clamp((int2)(renderCoord * bandScale + bandOffset),
                         int2(0, 0),
                         int2(gi.numVBands - 1, gi.numHBands - 1));

    gi.bandBase = gi.glyphLoc + 2;
    return gi;
}

/* Return pixels per em at this fragment.
 *
 * renderCoord:  em-space sample position
 * glyphLoc:     texel offset of glyph blob in atlas
 */
float hb_gpu_ppem (float2 renderCoord, uint glyphLoc_)
{
  _hb_gpu_glyph_info gi = _hb_gpu_decode_glyph (renderCoord, glyphLoc_);
  float2 emsPerPixel = fwidth (renderCoord);
  return min (gi.scale.x, gi.scale.y) /
	 max (emsPerPixel.x, emsPerPixel.y);
}

/*
 * Single-sample coverage rendering
 *
 * renderCoord: em-space sample position
 * pixelsPerEm: pixels per em at this fragment
 * glyphLoc: texel offset of glyph blob in atlas
 */
float _hb_gpu_slug_single(float2 renderCoord, float2 pixelsPerEm, uint glyphLoc)
{
    _hb_gpu_glyph_info gi = _hb_gpu_decode_glyph(renderCoord, glyphLoc);
    int glyphLocInt = gi.glyphLoc;
    int bandBase = gi.bandBase;
    int numHBands = gi.numHBands;

    // Horizontal ray casting
    float xcov = 0.0;
    float xwgt = 0.0;

    int4 hbandData = hb_gpu_fetch(bandBase + gi.bandIndex.y);
    int hCurveCount = hbandData.r;
    float hSplit = (float)hbandData.a * HB_GPU_INV_UNITS;
    bool hLeftRay = (renderCoord.x < hSplit);
    int hDataOffset = (hLeftRay ? hbandData.b : hbandData.g) + 32768;

    int ci=0;
    for (ci = 0; ci < hCurveCount; ci++)
    {
        int curveOffset = hb_gpu_fetch(glyphLocInt + hDataOffset + ci).r + 32768;

        int4 raw12 = hb_gpu_fetch(glyphLocInt + curveOffset);
        int4 raw3 = hb_gpu_fetch(glyphLocInt + curveOffset + 1);

        float4 q12 = (float4)raw12 * HB_GPU_INV_UNITS;
        float2 q3 = (float2)raw3.rg * HB_GPU_INV_UNITS;

        float4 p12 = q12 - float4(renderCoord, renderCoord);
        float2 p3 = q3 - renderCoord;

        // Early exit based on curve bounds
        if (hLeftRay)
        {
            if (min(min(p12.x, p12.z), p3.x) * pixelsPerEm.x > 0.5)
                break;
        }
        else
        {
            if (max(max(p12.x, p12.z), p3.x) * pixelsPerEm.x < -0.5)
                break;
        }

        uint code = _hb_gpu_calc_root_code(p12.y, p12.w, p3.y);
        if (code != 0u)
        {
            float2 a = q12.xy - q12.zw * 2.0 + q3;
            float2 b = q12.xy - q12.zw;
            float2 r = _hb_gpu_solve_horiz_poly(a, b, p12.xy) * pixelsPerEm.x;
            float2 cov = hLeftRay ? clamp(float2(0.5, 0.5) - r, 0.0, 1.0)
                                  : clamp(r + float2(0.5, 0.5), 0.0, 1.0);

            if ((code & 1u) != 0u)
            {
                xcov += cov.x;
                xwgt = max(xwgt, clamp(1.0 - abs(r.x) * 2.0, 0.0, 1.0));
            }

            if (code > 1u)
            {
                xcov -= cov.y;
                xwgt = max(xwgt, clamp(1.0 - abs(r.y) * 2.0, 0.0, 1.0));
            }
        }
    }

    // Vertical ray casting
    float ycov = 0.0;
    float ywgt = 0.0;

    int4 vbandData = hb_gpu_fetch(bandBase + numHBands + gi.bandIndex.x);
    int vCurveCount = vbandData.r;
    float vSplit = (float)vbandData.a * HB_GPU_INV_UNITS;
    bool vLeftRay = (renderCoord.y < vSplit);
    int vDataOffset = (vLeftRay ? vbandData.b : vbandData.g) + 32768;

    for (ci = 0; ci < vCurveCount; ci++)
    {
        int curveOffset = hb_gpu_fetch(glyphLocInt + vDataOffset + ci).r + 32768;

        int4 raw12 = hb_gpu_fetch(glyphLocInt + curveOffset);
        int4 raw3 = hb_gpu_fetch(glyphLocInt + curveOffset + 1);

        float4 q12 = (float4)raw12 * HB_GPU_INV_UNITS;
        float2 q3 = (float2)raw3.rg * HB_GPU_INV_UNITS;

        float4 p12 = q12 - float4(renderCoord, renderCoord);
        float2 p3 = q3 - renderCoord;

        // Early exit based on curve bounds
        if (vLeftRay)
        {
            if (min(min(p12.y, p12.w), p3.y) * pixelsPerEm.y > 0.5)
                break;
        }
        else
        {
            if (max(max(p12.y, p12.w), p3.y) * pixelsPerEm.y < -0.5)
                break;
        }

        uint code = _hb_gpu_calc_root_code(p12.x, p12.z, p3.x);
        if (code != 0u)
        {
            float2 a = q12.xy - q12.zw * 2.0 + q3;
            float2 b = q12.xy - q12.zw;
            float2 r = _hb_gpu_solve_vert_poly(a, b, p12.xy) * pixelsPerEm.y;
            float2 cov = vLeftRay ? clamp(float2(0.5, 0.5) - r, 0.0, 1.0)
                                  : clamp(r + float2(0.5, 0.5), 0.0, 1.0);

            if ((code & 1u) != 0u)
            {
                ycov -= cov.x;
                ywgt = max(ywgt, clamp(1.0 - abs(r.x) * 2.0, 0.0, 1.0));
            }

            if (code > 1u)
            {
                ycov += cov.y;
                ywgt = max(ywgt, clamp(1.0 - abs(r.y) * 2.0, 0.0, 1.0));
            }
        }
    }

    return _hb_gpu_calc_coverage(xcov, ycov, xwgt, ywgt);
}

/* Return coverage in [0, 1].
 *
 * Caller must declare: StructuredBuffer<int4> hb_gpu_atlas
 *
 * renderCoord:  em-space sample position
 * glyphLoc:     texel offset of glyph blob in atlas
 */
/* The MSAA-aware implementation.  Caller supplies pixelsPerEm so
 * this function can be invoked from non-uniform control flow (for
 * example from a paint op-stream branch). */
//  original
float _hb_gpu_slug (float2 renderCoord, float2 pixelsPerEm, uint glyphLoc_)
{

    float c = _hb_gpu_slug_single (renderCoord, pixelsPerEm, glyphLoc_);
 
#ifndef HB_GPU_NO_MSAA
  float ppem = hb_gpu_ppem (renderCoord, glyphLoc_);

  if (ppem < 16.0)
  {
    float2 emsPerPixel = 1.0 / pixelsPerEm;
    float2 d = emsPerPixel * (1.0 / 3.0);
    float msaa = 0.25 *
      (_hb_gpu_slug_single (renderCoord + float2 (-d.x, -d.y), pixelsPerEm, glyphLoc_) +
       _hb_gpu_slug_single (renderCoord + float2 ( d.x, -d.y), pixelsPerEm, glyphLoc_) +
       _hb_gpu_slug_single (renderCoord + float2 (-d.x,  d.y), pixelsPerEm, glyphLoc_) +
       _hb_gpu_slug_single (renderCoord + float2 ( d.x,  d.y), pixelsPerEm, glyphLoc_));

    c = lerp (c, msaa, smoothstep (16.0, 8.0, ppem));
  }
#endif

    return c;
}

/*
 * Stem darkening for small sizes
 *
 * coverage: output of _hb_gpu_slug
 * brightness: foreground brightness in [0, 1]
 * ppem: pixels per em at this fragment
 */
float hb_gpu_stem_darken(float coverage, float brightness, float ppem)
{
    return pow(coverage,
               lerp(pow(2.0, brightness - 0.5), 1.0,
                    smoothstep(8.0, 48.0, ppem)));
}

// ============================================================================
// ShaderGraph Custom Functions
// ============================================================================

/* Draw-renderer fragment shader entry.  The heavy lifting (Slug
 * coverage, MSAA, ppem, stem darkening) lives in the shared
 * hb-gpu-fragment.glsl that must be prepended to this source;
 * this file only adds the thin hb_gpu_draw() wrapper that lifts
 * pixelsPerEm out of fwidth() at uniform control flow before
 * calling the shared _hb_gpu_slug(). */

void hb_gpu_draw_float(float2 renderCoord, uint glyphLoc_, out float coverage)
{
  float2 pixelsPerEm = 1.0 / fwidth (renderCoord);  
  coverage = _hb_gpu_slug (renderCoord, pixelsPerEm, glyphLoc_);
}

/*
 * RenderGPUGlyphWithDarkening
 *
 * Renders a glyph with stem darkening for small sizes.
 *
 * Parameters:
 *   renderCoord: Em-space render coordinate (float2)
 *   glyphLoc: Glyph blob location in atlas (float - cast to uint internally)
 *   pixelsPerEm: Pixels per EM for size-dependent darkening (float)
 *   brightness: Brightness factor [0,1] for darkening (float)
 *
 * Outputs:
 *   coverage: Glyph coverage/alpha with darkening applied (float)
 */
void RenderGPUGlyphWithDarkening_float(
    float2 renderCoord,
    uint glyphLoc,
    float brightness,
    out float coverage)
{
    //uint glyphLocUint = asuint(glyphLoc);
    float2 pixelsPerEm = 1.0 / fwidth (renderCoord);
    float baseCoverage = _hb_gpu_slug(renderCoord, pixelsPerEm, glyphLoc);
    coverage = hb_gpu_stem_darken(baseCoverage, brightness, pixelsPerEm.x);
}


// Face only
void Layer1_float(float alpha, float4 color0, out float4 outColor)
{
    color0.a *= alpha;
    outColor = color0;
}
void ApplyVertexAlpha_float(
    float4 vertexColor,
    float4 colorIN,
    out float4 colorOUT)
{
    colorOUT = colorIN * vertexColor.w;
}

/*
 * RenderGPUGlyphWithOutline
 *
 * Renders a glyph with outline using distance-based approach.
 * Note: Full outline support requires edge detection which is complex
 * with the Slug algorithm. This is a simplified approximation.
 *
 * Parameters:
 *   renderCoord: Em-space render coordinate (float2)
 *   glyphLoc: Glyph blob location in atlas (float)
 *   pixelsPerEm: Pixels per EM (float)
 *   outlineWidth: Outline width in EM units (float)
 *   faceColor: Face color (float4)
 *   outlineColor: Outline color (float4)
 *
 * Outputs:
 *   outColor: Final blended color (float4)
 */
void RenderGPUGlyphWithOutline_float(
    float2 renderCoord,
    uint glyphLoc,
    float outlineWidth,
    float4 faceColor,
    float4 outlineColor,
    out float4 outColor)
{
    //uint glyphLocUint = asuint(glyphLoc);
    float2 pixelsPerEm = 1.0 / fwidth (renderCoord);

    // Render face
    float faceCoverage = _hb_gpu_slug(renderCoord, pixelsPerEm, glyphLoc);
    faceCoverage = hb_gpu_stem_darken(faceCoverage, 0.5, pixelsPerEm.x);

    // Simplified outline: sample at offset positions
    // This is an approximation - proper outline needs edge distance field
    float2 offset = float2(outlineWidth / pixelsPerEm.x, 0);
    float outlineCoverage = 0;

    // Sample in 4 directions to approximate outline
    outlineCoverage = max(outlineCoverage, _hb_gpu_slug(renderCoord + offset, pixelsPerEm, glyphLoc));
    outlineCoverage = max(outlineCoverage, _hb_gpu_slug(renderCoord - offset, pixelsPerEm, glyphLoc));
    offset = float2(0, outlineWidth / pixelsPerEm.x);
    outlineCoverage = max(outlineCoverage, _hb_gpu_slug(renderCoord + offset, pixelsPerEm, glyphLoc));
    outlineCoverage = max(outlineCoverage, _hb_gpu_slug(renderCoord - offset, pixelsPerEm, glyphLoc));

    // Blend outline and face (face overlays outline)
    outlineColor.a *= outlineCoverage;
    faceColor.a *= faceCoverage;

    outColor = lerp(outlineColor, faceColor, faceCoverage);
}

/*
 * RenderGPUGlyphWithUnderlay
 *
 * Renders a glyph with underlay (shadow-like effect).
 *
 * Parameters:
 *   renderCoord: Em-space render coordinate (float2)
 *   glyphLoc: Glyph blob location in atlas (float)
 *   pixelsPerEm: Pixels per EM (float)
 *   underlayOffset: Underlay offset in EM units (float2)
 *   underlayDilate: Underlay dilation (float)
 *   faceColor: Face color (float4)
 *   underlayColor: Underlay color (float4)
 *
 * Outputs:
 *   outColor: Final blended color (float4)
 */
void RenderGPUGlyphWithUnderlay_float(
    float2 renderCoord,
    uint glyphLoc,
    float2 underlayOffset,
    float4 faceColor,
    float4 underlayColor,
    out float4 outColor)
{
    //uint glyphLocUint = asuint(glyphLoc);
    float2 pixelsPerEm = 1.0 / fwidth (renderCoord);    

    // Render underlay (offset)
    float2 underlayCoord = renderCoord + underlayOffset;
    float underlayCoverage = _hb_gpu_slug(underlayCoord, pixelsPerEm, glyphLoc);
    underlayCoverage = hb_gpu_stem_darken(underlayCoverage, 0.5, pixelsPerEm.x);

    // Render face
    float faceCoverage = _hb_gpu_slug(renderCoord, pixelsPerEm, glyphLoc);
    faceCoverage = hb_gpu_stem_darken(faceCoverage, 0.5, pixelsPerEm.x);

    // Blend underlay and face (face overlays underlay)
    underlayColor.a *= underlayCoverage;
    faceColor.a *= faceCoverage;

    outColor = lerp(underlayColor, faceColor, faceCoverage);
}