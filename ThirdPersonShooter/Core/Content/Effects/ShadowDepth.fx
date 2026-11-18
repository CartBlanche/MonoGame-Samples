#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// Depth-only pass into a float colour target (DesktopGL cannot sample depth renderbuffers): writes the orthographic light-space depth (0..1, linear).
float4x4 World;
float4x4 LightVP;
float4x4 Bones[72];
float2 DepthK;   // x 1 = perspective (depth = view distance * y), 0 = orthographic (z)

struct VSIn { float4 Pos : POSITION0; };
struct VSInSk { float4 Pos : POSITION0; int4 Idx : BLENDINDICES0; float4 Wgt : BLENDWEIGHT0; };
struct VSOut { float4 Pos : SV_POSITION; float D : TEXCOORD0; };

VSOut VS(VSIn i)
{
    VSOut o;
    o.Pos = mul(mul(i.Pos, World), LightVP);
    o.D = lerp(o.Pos.z, o.Pos.w * DepthK.y, DepthK.x);
    return o;
}

VSOut VSSk(VSInSk i)
{
    VSOut o;
    float4x4 skin = Bones[i.Idx.x] * i.Wgt.x + Bones[i.Idx.y] * i.Wgt.y + Bones[i.Idx.z] * i.Wgt.z + Bones[i.Idx.w] * i.Wgt.w;
    o.Pos = mul(mul(mul(i.Pos, skin), World), LightVP);
    o.D = lerp(o.Pos.z, o.Pos.w * DepthK.y, DepthK.x);
    return o;
}

float4 PS(VSOut i) : COLOR0 { return float4(i.D, 0, 0, 1); }

technique Depth { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PS(); } }
technique DepthSkinned { pass P0 { VertexShader = compile VS_SHADERMODEL VSSk(); PixelShader = compile PS_SHADERMODEL PS(); } }
