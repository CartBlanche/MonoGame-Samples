#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif
// Blob shadow: ground quad with a procedural radial gradient (Godot blob_shadow: black a=1 centre -> a=0 edge). Alpha blended.
float4x4 ViewProj;
float Opacity;
struct VSIn { float4 Pos : POSITION0; float2 UV : TEXCOORD0; };
struct VSOut { float4 Pos : SV_POSITION; float2 UV : TEXCOORD0; };
VSOut VS(VSIn i) { VSOut o; o.Pos = mul(i.Pos, ViewProj); o.UV = i.UV; return o; }
float4 PS(VSOut i) : COLOR0
{
    float r = saturate(length(i.UV * 2.0 - 1.0));
    float a = pow(1.0 - r, 1.6) * Opacity;
    return float4(0, 0, 0, a);
}
technique Blob { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PS(); } }
