// Pixel shader extracts the brighter areas of an image.
// This is the first step in applying a bloom postprocess.

#include "Macros.hlsl"

DECLARE_TEXTURE(TextureSampler, 0);

BEGIN_CONSTANTS
float BloomThreshold;
END_CONSTANTS


float4 PixelShaderF(float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the original image color.
    float4 c = SAMPLE_TEXTURE(TextureSampler, texCoord);

    // Adjust it to keep only values brighter than the specified threshold.
    return saturate((c - BloomThreshold) / (1 - BloomThreshold));
}


technique BloomExtract
{
    pass Pass1
    {
    PixelShader = compile ps_4_0_level_9_1 PixelShaderF();
    }
}
