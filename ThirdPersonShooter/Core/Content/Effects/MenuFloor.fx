#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// Menu floor with the baked HDR lightmap (MenuBackdrop) darkened by the real spot-light shadow map of the robot (Shadows setting on).
texture Lightmap; sampler2D LmSamp = sampler_state { Texture = <Lightmap>; AddressU = Clamp; AddressV = Clamp; Filter = Linear; };
float ShadowDark;   // fraction of the lightmap removed in full shadow
#include "PbrCommon.fxh"

struct VSIn { float4 Pos : POSITION0; float2 UV : TEXCOORD0; };
struct FOut { float4 Pos : SV_POSITION; float2 UV : TEXCOORD0; float3 WPos : TEXCOORD1; };

FOut VSF(VSIn i)
{
    FOut o; float4 w = mul(i.Pos, World);
    o.Pos = mul(w, ViewProj); o.UV = i.UV; o.WPos = w.xyz; return o;
}

float4 PSF(FOut i) : COLOR0
{
    float sf = ShadowFactor(i.WPos, float3(0, 1, 0));
    float3 c = tex2D(LmSamp, i.UV).rgb * HdrScale * lerp(1.0 - ShadowDark, 1.0, sf);
    return float4(c, 1);
}

technique MenuFloor { pass P0 { VertexShader = compile VS_SHADERMODEL VSF(); PixelShader = compile PS_SHADERMODEL PSF(); } }
