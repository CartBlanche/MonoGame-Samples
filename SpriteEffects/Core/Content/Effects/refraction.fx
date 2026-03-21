// Effect uses a scrolling displacement texture to offset the position of the main
// texture. Depending on the contents of the displacement texture, this can give a
// wide range of refraction, rippling, warping, and swirling type effects.

#include "Macros.hlsl"

BEGIN_CONSTANTS
float2 DisplacementScroll;
END_CONSTANTS

DECLARE_TEXTURE(TextureSampler, 0);
DECLARE_TEXTURE(DisplacementSampler, 1);


float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the displacement amount.
    float2 displacement = SAMPLE_TEXTURE(DisplacementSampler, DisplacementScroll + texCoord / 3);
    
    // Offset the main texture coordinates.
    texCoord += displacement * 0.2 - 0.15;
    
    // Look up into the main texture.
    return SAMPLE_TEXTURE(TextureSampler, texCoord) * color;
}


technique Refraction
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 main();
    }
}
