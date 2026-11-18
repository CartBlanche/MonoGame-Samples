#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif
// Bloom chain. Prefilter: 4-tap downsample with soft threshold on the exposed colour. Blur: separable 9-tap gaussian along BlurDir.
float2 TexelSize;   // of the SOURCE texture
float2 BlurDir;     // (1,0) or (0,1)
float Threshold;    // in exposed linear units
float Exposure;
float HdrScale;
texture SrcTex; sampler2D SrcSamp = sampler_state { Texture = <SrcTex>; AddressU = Clamp; AddressV = Clamp; Filter = Linear; };

struct VSIn { float4 Pos : POSITION0; float2 UV : TEXCOORD0; };
struct VSOut { float4 Pos : SV_POSITION; float2 UV : TEXCOORD0; };
VSOut VS(VSIn i) { VSOut o; o.Pos = i.Pos; o.UV = i.UV; return o; }

float4 PSPre(VSOut i) : COLOR0
{
    float3 s = 0;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2(-1, -1)).rgb;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2( 1, -1)).rgb;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2(-1,  1)).rgb;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2( 1,  1)).rgb;
    s *= 0.25 * Exposure / HdrScale;
    float l = max(s.r, max(s.g, s.b));
    float k = saturate((l - Threshold) / max(l, 1e-4));
    s = min(s * k, 8.0);
    return float4(s, 1);
}
float4 PSDown(VSOut i) : COLOR0
{
    float3 s = 0;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2(-1, -1)).rgb;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2( 1, -1)).rgb;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2(-1,  1)).rgb;
    s += tex2D(SrcSamp, i.UV + TexelSize * float2( 1,  1)).rgb;
    return float4(s * 0.25, 1);
}
float4 PSBlur(VSOut i) : COLOR0
{
    float3 s = tex2D(SrcSamp, i.UV).rgb * 0.227027;
    s += (tex2D(SrcSamp, i.UV + TexelSize * BlurDir * 1.3846).rgb + tex2D(SrcSamp, i.UV - TexelSize * BlurDir * 1.3846).rgb) * 0.3162162;
    s += (tex2D(SrcSamp, i.UV + TexelSize * BlurDir * 3.2308).rgb + tex2D(SrcSamp, i.UV - TexelSize * BlurDir * 3.2308).rgb) * 0.0702703;
    return float4(s, 1);
}
technique Prefilter { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PSPre(); } }
technique Down { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PSDown(); } }
technique Blur { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PSBlur(); } }
