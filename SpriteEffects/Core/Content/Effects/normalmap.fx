// Effect applies normalmapped lighting to a 2D sprite.

#include "Macros.hlsl"

BEGIN_CONSTANTS
float3 LightDirection;
float3 LightColor = 1.5;
float3 AmbientColor = 0;
END_CONSTANTS

DECLARE_TEXTURE(TextureSampler, 0);
DECLARE_TEXTURE(NormalSampler, 1);


float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // Look up the texture and normalmap values.
    float4 tex = SAMPLE_TEXTURE(TextureSampler, texCoord);
    float3 normal = SAMPLE_TEXTURE(NormalSampler, texCoord);
    
    // Compute lighting.
    float lightAmount = max(dot(normal, LightDirection), 0);
    
    color.rgb *= AmbientColor + lightAmount * LightColor;
    
    return tex * color;
}


technique Normalmap
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 main();
    }
}
