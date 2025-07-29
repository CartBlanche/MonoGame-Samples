///-------------------------------------------------------------------------------------------------
/// <remarks>   
///     Charles Humphrey, 12/07/2025. 
///     Fixes for DX implementation.
/// </remarks>
///-------------------------------------------------------------------------------------------------
#include "PPVertexShader.fxh"

// Pixel shader applies a one dimensional gaussian blur filter.
// This is used twice by the bloom postprocess, first to
// blur horizontally, and then again to blur vertically.

sampler TextureSampler : register(s0);

#define SAMPLE_COUNT 15

float2 SampleOffsets[SAMPLE_COUNT];
float SampleWeights[SAMPLE_COUNT];


///-------------------------------------------------------------------------------------------------
/// <summary>   Function now uses correct vertex input structure for both OGL and DX </summary>
///
/// <remarks>   Charles Humphrey, 12/07/2025. </remarks>
///-------------------------------------------------------------------------------------------------
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 c = 0;
    
    // Combine a number of weighted image filter taps.
    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        // Charles Humphrey, altered this to give a better effect with the blur. Was too heavy before.
        // This should really be done where the offsets and weights are calculated.
        c += tex2D(TextureSampler, input.TexCoord + SampleOffsets[i] * .0125) * SampleWeights[i] * 5;        
        
        // Was:
        //c += tex2D(TextureSampler, input.TexCoord + SampleOffsets[i]) * SampleWeights[i];
    }
    
    return c;
}


technique GaussianBlur
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}
