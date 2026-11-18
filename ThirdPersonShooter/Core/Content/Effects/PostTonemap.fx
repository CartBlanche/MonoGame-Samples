#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif
// Full-screen tonemap: HDR scene (+ bloom) * Exposure -> AgX -> saturation -> dither. Godot env: AgX, exposure 4.5, saturation 1.4.
float Exposure;
float Saturation;
float Gamma;          // contrast: pow(col, Gamma) after AgX, >1 deepens shadows
float SCurve;         // 0..1 blend towards smoothstep S-curve (mid contrast)
float BlackPoint;     // subtracted after AgX (display space) so shadows reach near-black
float HdrScale;
float BloomIntensity;
float2 InvSize;
texture SceneTex; sampler2D SceneSamp = sampler_state { Texture = <SceneTex>; AddressU = Clamp; AddressV = Clamp; Filter = Linear; };
texture Bloom1Tex; sampler2D Bloom1Samp = sampler_state { Texture = <Bloom1Tex>; AddressU = Clamp; AddressV = Clamp; Filter = Linear; };
texture Bloom2Tex; sampler2D Bloom2Samp = sampler_state { Texture = <Bloom2Tex>; AddressU = Clamp; AddressV = Clamp; Filter = Linear; };
texture Bloom3Tex; sampler2D Bloom3Samp = sampler_state { Texture = <Bloom3Tex>; AddressU = Clamp; AddressV = Clamp; Filter = Linear; };

struct VSIn { float4 Pos : POSITION0; float2 UV : TEXCOORD0; };
struct VSOut { float4 Pos : SV_POSITION; float2 UV : TEXCOORD0; };
VSOut VS(VSIn i) { VSOut o; o.Pos = i.Pos; o.UV = i.UV; return o; }

float3 AgxContrast(float3 x)
{
    float3 x2 = x * x; float3 x4 = x2 * x2; float3 x6 = x4 * x2;
    return -17.86 * x6 * x + 78.01 * x6 - 126.7 * x4 * x + 92.06 * x4 - 28.72 * x2 * x + 4.361 * x2 - 0.1718 * x + 0.002857;
}
float3 TonemapAgx(float3 c)
{
    const float3x3 m = float3x3(0.842479062253094, 0.0784335999999992, 0.0792237451477643,
                                0.0423282422610123, 0.878468636469772, 0.0791661274605434,
                                0.0423756549057051, 0.0784336, 0.879142973793104);
    const float3x3 inv = float3x3(1.19687900512017, -0.0980208811401368, -0.0990297440797205,
                                  -0.0528968517574562, 1.15190312990417, -0.0989611768448433,
                                  -0.0529716355144438, -0.0980434501171241, 1.15107367264116);
    const float minEv = -12.47393; const float maxEv = 4.026069;
    c = mul(m, c);
    c = max(c, 1e-10);
    c = clamp((log2(c) - minEv) / (maxEv - minEv), 0.0, 1.0);
    c = AgxContrast(c);
    c = mul(inv, c);
    return saturate(c);
}

float4 PS(VSOut i) : COLOR0
{
    float3 hdr = tex2D(SceneSamp, i.UV).rgb / HdrScale;
    hdr += (tex2D(Bloom1Samp, i.UV).rgb + tex2D(Bloom2Samp, i.UV).rgb + tex2D(Bloom3Samp, i.UV).rgb) * BloomIntensity;
    float3 col = TonemapAgx(hdr * Exposure);
    float l = dot(col, float3(0.2126, 0.7152, 0.0722));
    col = saturate(lerp(l.xxx, col, Saturation));
    col = pow(max(col - BlackPoint, 0.0) / (1.0 - BlackPoint), Gamma);
    col = lerp(col, col * col * (3.0 - 2.0 * col), SCurve);
    float n = frac(52.9829189 * frac(dot(i.UV / InvSize, float2(0.06711056, 0.00583715))));
    col += (n - 0.5) / 255.0;
    return float4(col, 1);
}

technique Tonemap { pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL PS(); } }
