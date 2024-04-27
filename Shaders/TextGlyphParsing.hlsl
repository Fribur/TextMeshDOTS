#ifndef LATIOS_TEXT_GLYPH_PARSING_INCLUDED
#define LATIOS_TEXT_GLYPH_PARSING_INCLUDED

struct GlyphVertex
{
    float3 position;
    float3 normal;
    float3 tangent;
    float4 uvA;
    float2 uvB;
    float4 packedColor;
    uint unicode;
};

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
uniform ByteAddressBuffer _textBuffer;
uniform ByteAddressBuffer _textMaskBuffer;
#endif

void SampleGlyph_float(uint VertexID, float2 TextShaderIndex, float TextMaterialMaskShaderIndex, out float3 Position, out float3 Normal, out float3 Tangent, out float4 UVA, out float2 UVB, out float4 Color)
{
    uint BaseIndex = asuint(TextShaderIndex.x);
    uint GlyphCount = asuint(TextShaderIndex.y);
    uint MaskBase = asuint(TextMaterialMaskShaderIndex);
    Position = (float3) 0;
    Normal = (float3) 0;
    Tangent = (float3) 0;
    UVA = (float4) 0;
    UVB = (float2) 0;
    Color = (float4) 0;
#if defined(UNITY_DOTS_INSTANCING_ENABLED)

    if (GlyphCount <= (VertexID >> 2))
    {
        Position = asfloat(~0u);
        return;
    }
    uint glyphBase = 96 * (BaseIndex + (VertexID >> 2));
    if (MaskBase > 0)
    {
        uint mask = _textMaskBuffer.Load(4 * (MaskBase + (VertexID >> 6)));
        uint bit = (VertexID >> 2) & 0xf;
        bit += 16;
        if ((mask & (1 << bit)) == 0)
        {
            Position = asfloat(~0u);
            return;
        }
        bit -= 16;
        glyphBase = 96 * (BaseIndex + (mask & 0xffff) + bit);
    }

    const bool isBottomLeft = (VertexID & 0x3) == 0;
    const bool isTopLeft = (VertexID & 0x3) == 1;
    const bool isTopRight = (VertexID & 0x3) == 2;
    const bool isBottomRight = (VertexID & 0x3) == 3;

    const uint4 glyphMeta = _textBuffer.Load4(glyphBase + 80);
    
    Normal = float3(0, 0, -1);
    Tangent = float3(1, 0, 0);
    Position.z = 0;
    UVA.z = 0;
    UVA.w = asfloat(glyphMeta.z);
    //vertex.unicode = glyphMeta.x;

    uint packedColor = 0;

    if (isBottomLeft)
    {
        Position.xy = asfloat(_textBuffer.Load2(glyphBase));
        UVA.xy = asfloat(_textBuffer.Load2(glyphBase + 16));
        UVB = asfloat(_textBuffer.Load2(glyphBase + 32));
        packedColor = _textBuffer.Load(glyphBase + 64);
    }
    else if (isTopLeft)
    {
        Position.x = asfloat(_textBuffer.Load(glyphBase)) + asfloat(glyphMeta.y);
        Position.y = asfloat(_textBuffer.Load(glyphBase + 12));
        UVA.x = asfloat(_textBuffer.Load(glyphBase + 16));
        UVA.y = asfloat(_textBuffer.Load(glyphBase + 28));
        UVB = asfloat(_textBuffer.Load2(glyphBase + 40));
        packedColor = _textBuffer.Load(glyphBase + 68);
    }
    else if (isTopRight)
    {
        Position.xy = asfloat(_textBuffer.Load2(glyphBase + 8));
        UVA.xy = asfloat(_textBuffer.Load2(glyphBase + 24));
        UVB = asfloat(_textBuffer.Load2(glyphBase + 48));
        packedColor = _textBuffer.Load(glyphBase + 72);
    }
    else // if (isBottomRight)
    {
        float2 position = asfloat(_textBuffer.Load2(glyphBase + 4).yx);
        Position.x = position.x - asfloat(glyphMeta.y);
        Position.y = position.y;
        UVA.xy = asfloat(_textBuffer.Load2(glyphBase + 20).yx);
        UVB = asfloat(_textBuffer.Load2(glyphBase + 56));
        packedColor = _textBuffer.Load(glyphBase + 76);
    }

    if (glyphMeta.w != 0)
    {
        // Todo: What is the most optimal way to precompute parts of this in the compute shader
        // when we only have 4 bytes of storage?
        float angle = asfloat(glyphMeta.w);
        float cosine = cos(angle);
        float sine = sin(angle);
        
        float4 corners = asfloat(_textBuffer.Load4(glyphBase));
        float2 center = (corners.xy + corners.zw) * 0.5;
        float2 relative = Position.xy - center;
        float newX = relative.x * cosine - relative.y * sine;
        float newY = relative.x * sine + relative.y * cosine;
        Position.x = center.x + newX;
        Position.y = center.y + newY;
    }

    Color.x = (packedColor & 0xff) / 255.;
    Color.y = ((packedColor >> 8) & 0xff) / 255.;
    Color.z = ((packedColor >> 16) & 0xff) / 255.;
    Color.w = ((packedColor >> 24) & 0xff) / 255.;

#endif
    return;
}

#endif