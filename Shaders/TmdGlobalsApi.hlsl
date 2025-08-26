#ifndef TMDGLOBALSAPI
#define TMDGLOBALSAPI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

uniform ByteAddressBuffer _tmdGlyphs;
TEXTURE2D_ARRAY(_tmdSdf8);
SAMPLER(sampler_tmdSdf8);
TEXTURE2D_ARRAY(_tmdSdf16);
SAMPLER(sampler_tmdSdf16);
TEXTURE2D_ARRAY(_tmdBitmap);
SAMPLER(sampler_tmdBitmap);

// Helpers
float4 UnpackHalfColor(uint2 packedColor)
{
    uint4 expanded = packedColor.xyxy;
    expanded.yw = expanded.yw >> 16u;
    expanded = expanded & 0xffffu;
    return f16tof32(expanded);
}

// Base APIs
void GetGlyph_float(uint glyphIndex, uint glyphStartIndex, uint glyphCount,
    out float2 blPosition,
    out float2 brPosition,
    out float2 tlPosition,
    out float2 trPosition,

    out float2 blUVB,
    out float2 brUVB,
    out float2 tlUVB,
    out float2 trUVB,

    out float4 blColor,
    out float4 brColor,
    out float4 tlColor,
    out float4 trColor,

    out float2 blUVA,
    out float2 trUVA,

    out float arrayIndex,
    out uint glyphEntryId,
    out float scale,
    out uint reserved)
{
    if (glyphIndex < glyphStartIndex || glyphIndex >= glyphStartIndex + glyphCount)
    {
        blPosition = asfloat(~0u);
        brPosition = blPosition;
        tlPosition = blPosition;
        trPosition = blPosition;
        blUVB = blPosition;
        brUVB = blPosition;
        tlUVB = blPosition;
        trUVB = blPosition;
        blColor = 0;
        brColor = blColor;
        tlColor = blColor;
        trColor = blColor;
        blUVA = blPosition;
        trUVA = blPosition;
        arrayIndex = 0;
        glyphEntryId = 0;
        scale = 0;
        reserved = 0u;
    }

    uint baseAddress = glyphIndex * 128;
    uint4 load0_15 = _tmdGlyphs.Load4(baseAddress);
    blPosition = asfloat(load0_15.xy);
    brPosition = asfloat(load0_15.zw);
    uint4 load16_31 = _tmdGlyphs.Load4(baseAddress + 16);
    tlPosition = asfloat(load16_31.xy);
    trPosition = asfloat(load16_31.zw);

    uint4 load32_47 = _tmdGlyphs.Load4(baseAddress + 32);
    blUVB = asfloat(load32_47.xy);
    brUVB = asfloat(load32_47.zw);
    uint4 load48_63 = _tmdGlyphs.Load4(baseAddress + 48);
    tlUVB = asfloat(load48_63.xy);
    trUVB = asfloat(load48_63.zw);

    uint4 load64_79 = _tmdGlyphs.Load4(baseAddress + 64);
    blColor = UnpackColor(load64_79.xy);
    brColor = UnpackColor(load64_79.zw);
    uint4 load80_95 = _tmdGlyphs.Load4(baseAddress + 80);
    tlColor = UnpackColor(load80_95.xy);
    trColor = UnpackColor(load80_95.zw);

    uint4 load96_111 = _tmdGlyphs.Load4(baseAddress + 96);
    blUVA = asfloat(load96_111.xy);
    trUVA = asfloat(load96_111.zw);

    uint4 load112_127 = _tmdGlyphs.Load4(baseAddress + 112);
    arrayIndex = asfloat(load112_127.x);
    glyphEntryId = load112_127.y;
    scale = asfloat(load112_127.z);
    reserved = load112_127.w;
}

void GetSdfTextureArray_float(bool is16Bit, out UnityTexture2DArray sdfTexture)
{
    if (is16Bit)
    {
        return UnityBuildTexture2DArrayStruct(_tmdSdf16);
    }
    else
    {
        return UnityBuildTexture2DArrayStruct(_tmdSdf8);
    }
}

void GetBitmapTextureArray_float(out UnityTexture2DArray bitmapTexture)
{
    return UnityBuildTexture2DArrayStruct(_tmdBitmap);
}

// Additional APIs

// Corner order:  bl = 0, tl = 1, tr = 2, br = 3
void GetGlyphCorner_float(uint glyphIndex, uint cornerIndex, out float2 position, out float3 uvA, out float2 uvB, out float4 color, out float scale, out uint glyphEntryID)
{
    float2 blPosition;
    float2 brPosition;
    float2 tlPosition;
    float2 trPosition;
    float2 blUVB;
    float2 brUVB;
    float2 tlUVB;
    float2 trUVB;
    float4 blColor;
    float4 brColor;
    float4 tlColor;
    float4 trColor;
    float2 blUVA;
    float2 trUVA;
    float arrayIndex;
    uint reserved;
    GetGlyph_float(glyphIndex, blPosition, brPosition, tlPosition, trPosition, blUVB, brUVB, tlUVB, trUVB, blColor, brColor, tlColor, trColor, blUVA, brUVA, tlUVA, trUVA, arrayIndex, glyphEntry, scale, reserved);
    if (cornerIndex == 0)
    {
        // bottom left
        position = blPosition;
        uvA = float3(blUVA, arrayIndex);
        uvB = blUVB;
        color = blColor;
    }
    else if (cornerIndex == 1)
    {
        // top left
        position = tlPosition;
        uvA = float3(blUVA.x, trUVA.y, arrayIndex);
        uvB = tlUVB;
        color = tlColor;
    }
    else if (cornerIndex == 2)
    {
        // top right
        position = trPosition;
        uvA = float3(trUVA, arrayIndex);
        uvB = trUVB;
        color = trColor;
    }
    else
    {
        // bottom right
        position = brPosition;
        uvA = float3(trUVA.x, blUVA.y, arrayIndex);
        uvB = brUVB;
        color = brColor;
    }
}

void ExtractGlyphFlagsFromEntryID_float(uint glyphEntryID, out bool isSdf16, out bool isBitmap)
{
    uint format = glyphEntryID >> 30u;
    isSdf16 = format == 1;
    isBitmap = format == 2;
}

void GetGlyphIndexAndCornerFromQuadVertexID_float(uint vertexID, out uint glyphIndex, out uint cornerIndex)
{
    glyphIndex = vertexID >> 2u;
    cornerIndex = vertexID & 3u;
}

#endif