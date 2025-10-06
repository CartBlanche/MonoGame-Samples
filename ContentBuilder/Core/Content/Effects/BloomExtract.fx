///-------------------------------------------------------------------------------------------------
/// <remarks>   
///     Charles Humphrey, 12/07/2025. 
///     Fixes for DX implementation.
/// </remarks>
///-------------------------------------------------------------------------------------------------
#include "PPVertexShader.fxh"

sampler TextureSampler : register(s0);

float BloomThreshold;

///-------------------------------------------------------------------------------------------------
/// <summary>   Function now uses correct vertex input structure for both OGL and DX </summary>
///
/// <remarks>   Charles Humphrey, 12/07/2025. </remarks>
///-------------------------------------------------------------------------------------------------
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 c = tex2D(TextureSampler, input.TexCoord);
    return saturate((c - BloomThreshold) / (1 - BloomThreshold));
}

technique BloomExtract
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}
