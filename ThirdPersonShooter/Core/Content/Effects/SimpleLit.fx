#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float4x4 World;
float4x4 ViewProj;
float4 AlbedoColor;
float3 LightDir;     // direction light travels
float3 LightColor;
float3 AmbientColor;

struct VSIn { float4 Pos : POSITION0; float3 Nrm : NORMAL0; float4 Tan : TANGENT0; float2 UV : TEXCOORD0; };
struct VSOut { float4 Pos : SV_POSITION; float3 Nrm : TEXCOORD0; };

VSOut VS(VSIn i)
{
    VSOut o;
    float4 w = mul(i.Pos, World);
    o.Pos = mul(w, ViewProj);
    o.Nrm = mul(i.Nrm, (float3x3)World);
    return o;
}

float4 PS(VSOut i) : COLOR0
{
    float3 n = normalize(i.Nrm);
    float d = saturate(dot(n, -LightDir));
    return float4(AlbedoColor.rgb * (AmbientColor + LightColor * d), 1);
}

technique SimpleLit
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
