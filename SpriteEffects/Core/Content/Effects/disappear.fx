// Effect uses a scrolling overlay texture to make different parts of
// an image fade in or out at different speeds.

#include "Macros.hlsl"

BEGIN_CONSTANTS
float2 OverlayScroll;
END_CONSTANTS

DECLARE_TEXTURE(TextureSampler, 0);
DECLARE_TEXTURE(OverlaySampler, 1);


float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the texture color.
    float4 tex = SAMPLE_TEXTURE(TextureSampler, texCoord);
    
    // Look up the fade speed from the scrolling overlay texture.
    float fadeSpeed = SAMPLE_TEXTURE(OverlaySampler, OverlayScroll + texCoord).x;
    
    // Apply a combination of the input color alpha and the fade speed.
    tex *= saturate((color.a - fadeSpeed) * 2.5 + 1);
    
    return tex;
}


technique Desaturate
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 main();
    }
}
