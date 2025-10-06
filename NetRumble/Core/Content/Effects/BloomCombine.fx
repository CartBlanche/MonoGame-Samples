// Pixel shader combines the bloom image with the original
// scene, using tweakable intensity levels and saturation.
// This is the final step in applying a bloom postprocess.

#include "Macros.hlsl"

BEGIN_CONSTANTS
float BloomIntensity;
float BaseIntensity;
float BloomSaturation;
float BaseSaturation;
END_CONSTANTS

DECLARE_TEXTURE(BloomSampler, 0);
DECLARE_TEXTURE(BaseSampler, 1);


// Helper for modifying the saturation of a color.
float3 AdjustSaturation(float3 color, float saturation)
{
    // The constants 0.3, 0.59, and 0.11 are chosen because the
    // human eye is more sensitive to green light, and less to blue.
    float grey = dot(color, float3(0.3, 0.59, 0.11));

    return lerp(grey.xxx, color, saturation);
}


float4 PixelShaderF(float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the bloom and original base image colors.
    float3 bloom = SAMPLE_TEXTURE(BloomSampler, texCoord).rgb;
    float4 baseSample = SAMPLE_TEXTURE(BaseSampler, texCoord);
    float3 base = baseSample.rgb;
    
    // Adjust color saturation and intensity.
    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;
    
    // Darken down the base image in areas where there is a lot of bloom,
    // to prevent things looking excessively burned-out.
    base *= (1 - saturate(bloom));
    
    // Combine the two images.
    float3 combined = base + bloom;
    return float4(combined, baseSample.a);
}


technique BloomCombine
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderF();
    }
}
