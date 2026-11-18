#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif
// Additive unlit billboard glow for bullets / muzzle flash / hit effects. Vertices are in world space (billboarded on the CPU).
// Tint is an HDR multiplier (uniform, so it is not clamped like a vertex colour); vertex colour rgb modulates, vertex alpha = intensity.
float4x4 ViewProj;
float3 Tint;
texture Tex; sampler2D TexSamp = sampler_state { Texture = <Tex>; AddressU = Clamp; AddressV = Clamp; Filter = Linear; };

struct VSIn { float4 Pos : POSITION0; float4 Col : COLOR0; float2 UV : TEXCOORD0; };
struct VSOut { float4 Pos : SV_POSITION; float4 Col : COLOR0; float2 UV : TEXCOORD0; };
VSOut VS(VSIn i) { VSOut o; o.Pos = mul(i.Pos, ViewProj); o.Col = i.Col; o.UV = i.UV; return o; }
float4 PS(VSOut i) : COLOR0
{
    float a = tex2D(TexSamp, i.UV).a * i.Col.a;
    return float4(Tint * i.Col.rgb * a, 1);
}
technique Glow { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PS(); } }
