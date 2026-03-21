// Pixel shader applies a one dimensional gaussian blur filter.
// This is used twice by the bloom postprocess, first to
// blur horizontally, and then again to blur vertically.

#include "Macros.hlsl"

DECLARE_TEXTURE(TextureSampler, 0);

#define SAMPLE_COUNT 15

BEGIN_CONSTANTS
float2 SampleOffsets[SAMPLE_COUNT];
float SampleWeights[SAMPLE_COUNT];
END_CONSTANTS

float4 PixelShaderFunction(float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 c = 0;
    
    // Combine a number of weighted image filter taps.
    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        c += tex2D(TextureSampler, texCoord + SampleOffsets[i]) * SampleWeights[i];
    }
    
    return c;
}


technique GaussianBlur
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
