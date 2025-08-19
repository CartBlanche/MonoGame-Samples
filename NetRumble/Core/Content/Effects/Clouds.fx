//-----------------------------------------------------------------------------
// Clouds.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.hlsl"

// Constants
BEGIN_CONSTANTS
float2 Position;
END_CONSTANTS

// Texture + sampler (portable across SM3/SM4 via macros)
DECLARE_TEXTURE(CloudTexture, 0);

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    // Blue component
    float2 coord1 = texCoord + Position * 0.00025f;
    coord1 *= 0.5f;
    float4 results = float4(0,0,1,0.25) * SAMPLE_TEXTURE(CloudTexture, coord1);

    // Green component
    float2 coord2 = texCoord + Position * 0.00025f + float2(0.25f, -0.15f);
    coord2 *= 0.4f;
    results += float4(0,1,0,0.15) * SAMPLE_TEXTURE(CloudTexture, coord2);

    return results;
}

technique Clouds
{
    pass P0
    {
    PixelShader = compile ps_4_0_level_9_1 MainPS();
    }
}