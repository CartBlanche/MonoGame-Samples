#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// PbrLitSkinned = PbrLit with a 4-bone skinning vertex shader (Bones palette per mesh part).
#include "PbrCommon.fxh"
float4x4 Bones[72];
struct VSIn { float4 Pos : POSITION0; float3 Nrm : NORMAL0; float4 Tan : TANGENT0; float2 UV : TEXCOORD0; int4 Idx : BLENDINDICES0; float4 Wgt : BLENDWEIGHT0; };

VSOut VS(VSIn i)
{
    VSOut o;
    float4x4 skin = Bones[i.Idx.x] * i.Wgt.x + Bones[i.Idx.y] * i.Wgt.y + Bones[i.Idx.z] * i.Wgt.z + Bones[i.Idx.w] * i.Wgt.w;
    float4 sp = mul(i.Pos, skin);
    float4 w = mul(sp, World);
    o.Pos = mul(w, ViewProj);
    o.WPos = w.xyz;
    o.N = mul(mul(i.Nrm, (float3x3)skin), (float3x3)World);
    o.T = float4(mul(mul(i.Tan.xyz, (float3x3)skin), (float3x3)World), i.Tan.w);
    o.UV = i.UV * UvTransform.xy + UvTransform.zw;
    return o;
}

technique PbrLitSkinned
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}

technique PbrLitSkinnedShadow
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PSShadow();
    }
}

technique PbrLitSkinnedSpot
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PSSpot();
    }
}
