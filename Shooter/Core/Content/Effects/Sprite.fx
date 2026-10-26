// "Unlit/Sprite": texture * tint * vertex colour (Sprites/Default, Sprite-Lit-Default without 2D lights).
// Used for mesh based sprites (e.g. the sun disc). Blend state is chosen by the C# side (alpha / additive).

#include "Common.fxh"

float4x4 World;
float4x4 ViewProjection;
float4   BaseColor = float4(1, 1, 1, 1); // linear, may be HDR
float4   TilingOffset = float4(1, 1, 0, 0);
float    SrgbDecode = 1;
float    AlphaCutoff = 0; // > 0 => clip

DECLARE_TEXTURE(BaseMap, 0);

struct VSIn
{
    float4 Position : POSITION0;
    float2 UV       : TEXCOORD0;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float2 UV       : TEXCOORD0;
};

VSOut VSSprite(VSIn i)
{
    VSOut o;
    float4 posWS = mul(float4(i.Position.xyz, 1), World);
    o.Position = mul(posWS, ViewProjection);
    o.UV = i.UV * TilingOffset.xy + TilingOffset.zw;
    return o;
}

float4 PSSprite(VSOut i) : SV_TARGET0
{
    float4 t = SAMPLE_TEXTURE(BaseMap, i.UV);
    t.rgb = lerp(t.rgb, SRGBToLinear3(t.rgb), SrgbDecode);
    float4 c = t * BaseColor;
    if (AlphaCutoff > 0)
        clip(c.a - AlphaCutoff);
    c.rgb = min(c.rgb, 60000.0);
    return c;
}

TECHNIQUE(Sprite, VSSprite, PSSprite);
