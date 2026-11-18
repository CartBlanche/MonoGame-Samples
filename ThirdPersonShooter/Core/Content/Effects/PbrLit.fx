#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// PbrLit: textured Godot-StandardMaterial3D-like shading, outputs linear HDR (see PbrCommon.fxh).
#include "PbrCommon.fxh"
struct VSIn { float4 Pos : POSITION0; float3 Nrm : NORMAL0; float4 Tan : TANGENT0; float2 UV : TEXCOORD0; };

VSOut VS(VSIn i)
{
    VSOut o;
    float4 w = mul(i.Pos, World);
    o.Pos = mul(w, ViewProj);
    o.WPos = w.xyz;
    o.N = mul(i.Nrm, (float3x3)World);
    o.T = float4(mul(i.Tan.xyz, (float3x3)World), i.Tan.w);
    o.UV = i.UV * UvTransform.xy + UvTransform.zw;
    return o;
}

technique PbrLit
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}

technique PbrLitShadow
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PSShadow();
    }
}

technique PbrLitSpot
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PSSpot();
    }
}
