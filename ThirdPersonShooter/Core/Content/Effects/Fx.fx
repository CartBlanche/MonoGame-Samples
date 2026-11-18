#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif
// Laser beam tube, soft smoke sprites and barrel smoke. World-space vertices (CPU billboards / tube).
float4x4 ViewProj;
float3 Tint;          // HDR multiplier
texture Tex;   sampler2D TexSamp   = sampler_state { Texture = <Tex>;   AddressU = Clamp; AddressV = Clamp; Filter = Linear; };
texture Noise; sampler2D NoiseSamp = sampler_state { Texture = <Noise>; AddressU = Wrap;  AddressV = Wrap;  Filter = Linear; };
// Beam (enemies/red_robot/laser/LaserShader.gdshader)
float2 UvScale; float OffsetU; float Cutoff; float Energy;
// BarrelSmoke (enemies/red_robot/laser/smoke.gdshader)
float Life;

struct VSIn { float4 Pos : POSITION0; float4 Col : COLOR0; float2 UV : TEXCOORD0; };
struct VSOut { float4 Pos : SV_POSITION; float4 Col : COLOR0; float2 UV : TEXCOORD0; };
VSOut VS(VSIn i) { VSOut o; o.Pos = mul(i.Pos, ViewProj); o.Col = i.Col; o.UV = i.UV; return o; }

// LaserShader.gdshader fragment: noise = texture(laser_body_noise, UV*uv_scale + (offset,0)).r; discard if noise < cutoff; EMISSION = color1 * energy.
float4 PSBeam(VSOut i) : COLOR0
{
    float n = tex2D(NoiseSamp, i.UV * UvScale + float2(OffsetU, 0)).r;
    clip(n - Cutoff);
    return float4(Tint * Energy, 1);
}
// bullet.tscn Smoke: unshaded, albedo texture * vertex colour, alpha blend.
float4 PSSmoke(VSOut i) : COLOR0
{
    float4 t = tex2D(TexSamp, i.UV);
    return float4(t.rgb * i.Col.rgb * Tint, t.a * i.Col.a);
}
// smoke.gdshader fragment: ALPHA = smoothstep(0, 1-life, clamp(alpha*noise - life^2, 0, 1) * COLOR.a), ALBEDO = COLOR.rgb
float4 PSBarrel(VSOut i) : COLOR0
{
    float a = tex2D(TexSamp, i.UV).r;
    float n = tex2D(NoiseSamp, i.UV + float2(0, Life)).r;
    float x = saturate(a * n - Life * Life) * i.Col.a;
    float e1 = max(1.0 - Life, 0.001);
    float s = saturate(x / e1); s = s * s * (3 - 2 * s);
    return float4(i.Col.rgb * Tint, s);
}
technique Beam   { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PSBeam(); } }
technique Smoke  { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PSSmoke(); } }
technique Barrel { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PSBarrel(); } }
