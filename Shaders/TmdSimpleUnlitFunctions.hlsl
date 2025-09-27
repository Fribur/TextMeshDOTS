#ifndef TMDSIMPLEUNLITFUNCTIONS
#define TMDSIMPLEUNLITFUNCTIONS

#include "TmdGlobalsApi.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

void DoSimpleUnlitVert_float(float2 textShaderIndex, float vertexID, out float3 position, out float3 normal, out float3 tangent, out float4 vertexColor, out float4 uvAandB, out float4 atlasIndexScaleIsSdf16IsBitmap)
{
	uint glyphIndex;
	uint cornerIndex;
	GetGlyphIndexAndCornerFromQuadVertexID(vertexID, glyphIndex, cornerIndex);
	uint glyphStartIndex = asuint(textShaderIndex.x);
	uint glyphCount = asuint(textShaderIndex.y);
	float2 position2D;
	float3 uvA;
	float2 uvB;
	float4 color;
	float scale;
	uint glyphEntryID;
	GetGlyphCorner(glyphIndex, cornerIndex, glyphStartIndex, glyphCount, position2D, uvA, uvB, color, scale, glyphEntryID);
	bool isSdf16;
	bool isBitmap;
	ExtractGlyphFlagsFromEntryID(glyphEntryID, isSdf16, isBitmap);
	position = float3(position2D, 0.0);
	normal = float3(0.0, -1.0, 0.0);
	tangent = float3(1.0, 0.0, 0.0);
	vertexColor = color;
	uvAandB = float4(uvA.xy, uvB);
	atlasIndexScaleIsSdf16IsBitmap = float4(uvA.z, scale, isSdf16, isBitmap);
}

void DoSimpleUnlitFrag_float(float4 vertexColor, float4 uvAandB, float4 atlasIndexScaleIsSdf16IsBitmap, out float3 finalColor, out float finalAlpha)
{
	float3 uvA = float3(uvAandB.xy, atlasIndexScaleIsSdf16IsBitmap.x);
	float2 uvB = uvAandB.zw;
	float scale = atlasIndexScaleIsSdf16IsBitmap.y;
	bool isSdf16 = atlasIndexScaleIsSdf16IsBitmap.z;
	bool isBitmap = atlasIndexScaleIsSdf16IsBitmap.w;

	if (isBitmap)
	{
		UnityTexture2DArray bitmap = GetBitmapTextureArray();
		float4 bitmapColor = bitmap.Sample(sampler_LinearClamp, uvA);
		bitmapColor *= vertexColor;
		finalColor = bitmapColor.xyz;
		finalAlpha = bitmapColor.w;
		return;
	}
	else
	{
		float screenSpaceRatio = rsqrt(abs(ddx(uvA.x) * ddy(uvA.y) - ddy(uvA.x) * ddx(uvA.y))) * GetGlyphTexelSize();
		
		// For faking bold in this simple shader, we use the constant normal weight of 0 and bold weight of 0.75.
		// After selecting the correct weight, this value is divided by 4, added to the outline width, and then divided by 2.
		// Since we don't have outlines, we jump straight to the conclusion.
		float isoPerimeter = scale >= 0.0 ? 0.0 : (0.75 / 8.0);

		// The signed distance ratio is the padding value + 1.
		// Todo: Need to pack the sampling point size enumeration into glyphEntryID.
		float signedDistanceRatio = 11.0;

		float signedDistance;
		if (isSdf16)
		{
			UnityTexture2DArray sdf = GetSdf16TextureArray();
			signedDistance = sdf.Sample(sampler_LinearClamp, uvA).r;
		}
		else
		{
			UnityTexture2DArray sdf = GetSdf8TextureArray();
			signedDistance = sdf.Sample(sampler_LinearClamp, uvA).r;
		}

		float signedDistanceToEdge = (signedDistance - 0.5) * signedDistanceRatio; // Signed distance to edge, in Texture space
		float pixelCoverage = saturate(signedDistanceToEdge * 2.0 * screenSpaceRatio + 0.5 + isoPerimeter * signedDistanceRatio * screenSpaceRatio);	// Screen pixel coverage (alpha)

		finalColor = vertexColor.xyz;
		finalAlpha = vertexColor.w * pixelCoverage;
		return;
	}
}

void DoSimpleUnlitFragDebug_float(float4 vertexColor, float4 uvAandB, float4 atlasIndexScaleIsSdf16IsBitmap, out float3 finalColor, out float finalAlpha)
{
	float3 uvA = float3(uvAandB.xy, atlasIndexScaleIsSdf16IsBitmap.x);
	float2 uvB = uvAandB.zw;
	float scale = atlasIndexScaleIsSdf16IsBitmap.y;
	bool isSdf16 = atlasIndexScaleIsSdf16IsBitmap.z;
	bool isBitmap = atlasIndexScaleIsSdf16IsBitmap.w;

	if (isBitmap)
	{
		UnityTexture2DArray bitmap = GetBitmapTextureArray();
		float4 bitmapColor = bitmap.Sample(sampler_LinearClamp, uvA);
		bitmapColor *= vertexColor;
		finalColor = bitmapColor.xyz;
		finalAlpha = bitmapColor.w;
		return;
	}
	else
	{
		float screenSpaceRatio = rsqrt(abs(ddx(uvA.x) * ddy(uvA.y) - ddy(uvA.x) * ddx(uvA.y))) * GetGlyphTexelSize();

		// For faking bold in this simple shader, we use the constant normal weight of 0 and bold weight of 0.75.
		// After selecting the correct weight, this value is divided by 4, added to the outline width, and then divided by 2.
		// Since we don't have outlines, we jump straight to the conclusion.
		float isoPerimeter = scale >= 0.0 ? 0.0 : (0.75 / 8.0);

		// The signed distance ratio is the padding value + 1.
		// Todo: Need to pack the sampling point size enumeration into glyphEntryID.
		float signedDistanceRatio = 11.0;

		float signedDistance;
		if (isSdf16)
		{
			UnityTexture2DArray sdf = GetSdf16TextureArray();
			signedDistance = sdf.Sample(sampler_LinearClamp, uvA).r;
		}
		else
		{
			UnityTexture2DArray sdf = GetSdf8TextureArray();
			signedDistance = sdf.Sample(sampler_LinearClamp, uvA).r;
		}

		float signedDistanceToEdge = (signedDistance - 0.5) * signedDistanceRatio; // Signed distance to edge, in Texture space
		float pixelCoverage = saturate(signedDistanceToEdge * 2.0 * screenSpaceRatio + 0.5 + isoPerimeter * signedDistanceRatio * screenSpaceRatio);	// Screen pixel coverage (alpha)

		//finalColor = vertexColor.xyz; 
		finalColor = signedDistance - 0.5; //float3(signedDistance, signedDistanceToEdge, pixelCoverage);
		finalAlpha = 1.0; //vertexColor.w * pixelCoverage;
		return;
	}
}


#endif