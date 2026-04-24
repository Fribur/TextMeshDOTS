#ifndef TMDGLOBALSAPI_GPU
#define TMDGLOBALSAPI_GPU

#define HB_GPU_NO_MSAA

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

uniform ByteAddressBuffer _tmdGlyphs; //ByteAddressBuffer that mirrors the RenderGlyph layout.
StructuredBuffer<int2> _hbGpuAtlas; // GPU blob atlas - ByteAddressBuffer of RGBA16I data (8 bytes per texel)

// _tmdGlyphs – ByteAddressBuffer that mirrors the RenderGlyph layout.
//
//   bytes   0-15   blPosition (float2), brPosition (float2)
//   bytes  16-31   tlPosition (float2), trPosition (float2)
//   bytes  32-47   blUVB (float2),      brUVB (float2)       – unused in GPU path
//   bytes  48-63   tlUVB (float2),      trUVB (float2)       – unused in GPU path
//   bytes  64-79   blColor (half4),     brColor (half4)
//   bytes  80-95   tlColor (half4),     trColor (half4)
//   bytes  96-111  blUVA (float2),      trUVA (float2)
//   bytes 112-127  glyphLoc (uint), glyphEntryId (uint),  scale (float), reserved (uint)


// Helpers
float2 UnpackHalf2(uint packed) // Unpack half2 to float2
{
    uint2 expanded = packed.xx;
    expanded.y = expanded.y >> 16u;
    expanded = expanded & 0xffffu;
    return f16tof32(expanded);
}

float4 UnpackHalf4(uint2 packedColor) // Unpack half4 to float4
{
    uint4 expanded = packedColor.xxyy;
    expanded.yw = expanded.yw >> 16u;
    expanded = expanded & 0xffffu;
    return f16tof32(expanded);
}

// Base APIs
void GetGlyphIndexAndCornerFromQuadVertexID(uint vertexID, out uint glyphIndex, out uint cornerIndex)
{
    glyphIndex = vertexID >> 2u;
    cornerIndex = vertexID & 3u;
}

/*
 * GetGlyphFromBuffer_float
 *
 * Fetches glyph data from the _tmdGlyphs buffer for a given vertex.
 * This is the GPU-blob equivalent of GetGlyphFromBuffer in TmdGlobalsApi.hlsl.
 *
 * Parameters:
 *   textShaderIndex: x=glyphStartIndex, y=glyphCount (as float2)
 *   vertexID: Vertex index within the mesh (0-3 per glyph quad)
 *
 * Outputs:
 *   position: Object-space position (float3) - transformed by MVP for screen position
 *   renderCoord: Em-space design unit coordinates (float2) — raw HarfBuzz extents
 *         (Y-down, matching the GPU blob encoding). blUVA = (x_bearing, y_bearing),
 *         trUVA = (x_bearing + width, y_bearing + height)
 *   color: Vertex color (float4)
 *   glyphLoc: Glyph blob location in atlas (float - will be uint in shader)
 *   isCOLR: Whether this glyph uses COLR paint rendering
 */
void GetGlyphFromBuffer_float(
    float2 textShaderIndex,
    float vertexID,
    out float3 position,
    out float2 renderCoord,
    out float4 color,
    out float glyphLoc,
	out bool isCOLR)
{
    // Decode vertex ID to get glyph index and corner index (mirrors TmdGlobalsApi.hlsl)
    uint vertexID_Int = (uint)vertexID;
    uint glyphIndex;
    uint cornerIndex;
    GetGlyphIndexAndCornerFromQuadVertexID(vertexID_Int, glyphIndex, cornerIndex);

    // Get glyph start index and count
    uint glyphStartIndex = asuint(textShaderIndex.x);
    uint glyphCount = asuint(textShaderIndex.y);

    // Check for invalid glyph
    if (glyphIndex >= glyphCount)
    {
        float4 nanVal = asfloat(0x7fc00000u);
        position = float3(nanVal.xy, 0);
        renderCoord = nanVal.xy;
        color = 0;
        glyphLoc = 0;
		isCOLR = false;
        return;
    }

    // Calculate base address (128 bytes per glyph)
    uint baseAddress = (glyphStartIndex + glyphIndex) * 128;

    // Load positions (0-31 bytes)
    uint4 load0_15 = _tmdGlyphs.Load4(baseAddress);
    float2 blPosition = asfloat(load0_15.xy);
    float2 brPosition = asfloat(load0_15.zw);
    uint4 load16_31 = _tmdGlyphs.Load4(baseAddress + 16);
    float2 tlPosition = asfloat(load16_31.xy);
    float2 trPosition = asfloat(load16_31.zw);

    // Load colors (64-95 bytes)
    uint4 load64_79 = _tmdGlyphs.Load4(baseAddress + 64);   //load half4 blColor and half4 brColor
    float4 blColor = UnpackHalf4(load64_79.xy);             //convert blColor from half4 to float4
    float4 brColor = UnpackHalf4(load64_79.zw);             //convert blColor from half4 to float4
    uint4 load80_95 = _tmdGlyphs.Load4(baseAddress + 80);
    float4 tlColor = UnpackHalf4(load80_95.xy);
    float4 trColor = UnpackHalf4(load80_95.zw);

    // Load UVA (96-111 bytes) - em-space design unit coordinates for GPU blob rendering
    // gpu blob is encoded in Y-down ccordante system, so
    // ... blUVA in y-up is tlUVA in y-down (origin = x_bearing, y_bearing)
    // ... trUVA in y-up is brUVA in y-down (x_bearing + width, y_bearing + height) (height is negative!)
    uint4 load96_111 = _tmdGlyphs.Load4(baseAddress + 96);
    float2 tlUVA = asfloat(load96_111.xy);
    float2 brUVA = asfloat(load96_111.zw);

    // Load GPU-specific data (112-127 bytes)
    uint4 load112_127 = _tmdGlyphs.Load4(baseAddress + 112);
    glyphLoc = (float)load112_127.x; //arrayIndex for TextureArray, glyphLoc for hbGpuAtlas
	uint glyphEntryID = load112_127.y;
	uint format = glyphEntryID >> 30u;
	isCOLR = format == 3;
    //scale = (float)load112_127.z;
    //reserved = load112_127.w;

    // Select corner data — UVA stores raw HarfBuzz extents (Y-down)
    // tlUVA = (xb, yb) [em-space top-left], brUVA = (xb+W, yb+H) [em-space bottom-right]
    // Corner order: bl=0, tl=1, tr=2, br=3
    if (cornerIndex == 0)
    {
        // bottom left
        position = float3(blPosition, 0);
        renderCoord = float2(tlUVA.x, brUVA.y);
        color = blColor;
    }
    else if (cornerIndex == 1)
    {
        // top left
        position = float3(tlPosition, 0);
        renderCoord = tlUVA;
        color = tlColor;
    }
    else if (cornerIndex == 2)
    {
        // top right
        position = float3(trPosition, 0);
        renderCoord = float2(brUVA.x, tlUVA.y);
        color = trColor;
    }
    else
    {
        // bottom right
        position = float3(brPosition, 0);
        renderCoord = brUVA;
        color = brColor;
    }
}

void GetGlyphFromBufferForDilate_float(
    float2 textShaderIndex,
    float vertexID,
    out float2 position,
    out float2 renderCoord,
    out float2 normal,    
    out float4 jacobian,
    out float4 color,
    out float glyphLoc,
    out bool isCOLR)
{
    uint vertexID_Int = (uint)vertexID;
    uint glyphIndex;
    uint cornerIndex;
    GetGlyphIndexAndCornerFromQuadVertexID(vertexID_Int, glyphIndex, cornerIndex);

    uint glyphStartIndex = asuint(textShaderIndex.x);
    uint glyphCount = asuint(textShaderIndex.y);

    if (glyphIndex >= glyphCount)
    {
        float4 nanVal = asfloat(0x7fc00000u);
        position = float2(nanVal.xy);
        renderCoord = nanVal.xy;
        normal = float2(nanVal.xy);
        jacobian = nanVal;
        color = 0;
        glyphLoc = 0;
        isCOLR = false;
        return;
    }

    uint baseAddress = (glyphStartIndex + glyphIndex) * 128;
    
    // Load Positions 
    uint4 load0_15 = _tmdGlyphs.Load4(baseAddress);
    float2 blPos = asfloat(load0_15.xy);
    float2 brPos = asfloat(load0_15.zw);
    uint4 load16_31 = _tmdGlyphs.Load4(baseAddress + 16);
    float2 tlPos = asfloat(load16_31.xy);
    float2 trPos = asfloat(load16_31.zw);

    // Load UVA and Glyph Data 
    uint4 load96_111 = _tmdGlyphs.Load4(baseAddress + 96);
    float2 tlUVA = asfloat(load96_111.xy);
    float2 brUVA = asfloat(load96_111.zw);
    uint4 load112_127 = _tmdGlyphs.Load4(baseAddress + 112);
    glyphLoc = (float)load112_127.x;
    uint glyphEntryID = load112_127.y;
	uint format = glyphEntryID >> 30u;
	isCOLR = format == 3;
    //scale = (float)load112_127.z;
    //reserved = load112_127.w;

    // Load Colors 
    float4 blColor = UnpackHalf4(_tmdGlyphs.Load4(baseAddress + 64).xy);
    float4 brColor = UnpackHalf4(_tmdGlyphs.Load4(baseAddress + 64).zw);
    float4 tlColor = UnpackHalf4(_tmdGlyphs.Load4(baseAddress + 80).xy);
    float4 trColor = UnpackHalf4(_tmdGlyphs.Load4(baseAddress + 80).zw);

    // 1. Calculate Jacobian (em-space size / object-space size)
    float2 objSize = trPos - blPos;
    float2 emSize = brUVA - tlUVA;
    float2 ratio = emSize / max(objSize, 0.00001);
    // Row-major 2x2 inverse — ratio.y is naturally negative because
    // emSize.y > 0 (Y-down) and objSize.y < 0 (Y-up screen)
    jacobian = float4(ratio.x, 0.0, 0.0, ratio.y);

    // 2. Select initial data and normal based on corner
    if (cornerIndex == 0) { // BL
        position = blPos;
        renderCoord = float2(tlUVA.x, brUVA.y);
        normal = float2(-1.0, -1.0);
        color = blColor;
    } else if (cornerIndex == 1) { // TL
        position = tlPos;
        renderCoord = tlUVA;
        normal = float2(-1.0, 1.0);
        color = tlColor;
    } else if (cornerIndex == 2) { // TR
        position = trPos;
        renderCoord = float2(brUVA.x, tlUVA.y);
        normal = float2(1.0, 1.0);
        color = trColor;
    } else { // BR
        position = brPos;
        renderCoord = brUVA;
        normal = float2(1.0, -1.0);
        color = brColor;
    }
}
#endif // TMDGLOBALSAPI_GPU
