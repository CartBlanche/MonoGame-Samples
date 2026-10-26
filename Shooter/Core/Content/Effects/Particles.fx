// URP "Particles/Unlit" and "Particles/Lit" (forward) for MonoGame. Blend mode / depth state are set by the C# side:
//   Alpha: SrcAlpha, InvSrcAlpha   Premultiply: One, InvSrcAlpha (shader multiplies rgb by alpha)
//   Additive: SrcAlpha, One        Multiply: DstColor, Zero
// Two vertex paths share the pixel shader:
//   ParticlesBillboard : CPU generated quads (world-space corners, vertex colour (LINEAR), uv0, uv1+blend)
//   ParticlesMesh      : ordinary static mesh vertices (MeshRenderer using a particle material)
// Soft particles / camera fading / distortion are not implemented (no material in the project enables them).
// Texture slots: 0 BaseMap, 1 NormalMap, 2 EmissionMap, 5 ShadowMap.

#include "Common.fxh"

float4x4 World;
float4x4 ViewProjection;
float4x4 ShadowViewProjection;

float3 CameraPosition;
float3 CameraRight;
float3 CameraUp;
float3 CameraForward;

float4 BaseColor = float4(1, 1, 1, 1);   // linear
float3 EmissionColor;                    // HDR linear
float4 ColorAddSubDiff;                  // _BaseColorAddSubDiff
float4 TilingOffset = float4(1, 1, 0, 0);
// x: lit, y: alpha clip, z: premultiply alpha (rgb *= a), w: colour mode (0 multiply, 1 add/sub/diff, 3 overlay, 4 color)
float4 Flags = float4(0, 0, 0, 0);
// x: emission on, y: base map is sRGB, z: normal map present, w: emission map is sRGB
float4 Flags2 = float4(0, 1, 0, 1);
// x: metallic, y: smoothness, z: bump scale, w: cutoff
float4 Params3 = float4(0, 0.5, 1, 0.5);
// x: receive shadows
float4 MaterialFlags2 = float4(0, 1, 0, 0);

// lighting
float3 SunDirection;
float3 SunColor;
float3 AmbientColor;
float4 ShadowParams;
float2 ShadowFade;

DECLARE_TEXTURE(BaseMap, 0);
DECLARE_TEXTURE(NormalMap, 1);
DECLARE_TEXTURE(EmissionMap, 2);
DECLARE_TEXTURE(ShadowMap, 5);

#include "Lighting.fxh"

struct VSInBillboard
{
    float4 Position : POSITION0;
    float4 Color    : COLOR0;
    float2 UV       : TEXCOORD0;
    float3 UV2Blend : TEXCOORD1;
};

struct VSInMesh
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 UV       : TEXCOORD0;
    float3 Tangent  : TANGENT0;
    float3 Binormal : BINORMAL0;
};

struct VSOut
{
    float4 Position  : SV_POSITION;
    float4 Color     : COLOR0;
    float2 UV        : TEXCOORD0;
    float3 PositionWS: TEXCOORD1;
    float3 NormalWS  : TEXCOORD2;
    float3 TangentWS : TEXCOORD3;
    float3 BitangentWS : TEXCOORD4;
};

VSOut VSBillboard(VSInBillboard i)
{
    VSOut o;
    o.PositionWS = i.Position.xyz;
    o.Position = mul(float4(i.Position.xyz, 1), ViewProjection);
    o.Color = i.Color;
    o.UV = i.UV * TilingOffset.xy + TilingOffset.zw;
    o.NormalWS = -CameraForward;
    o.TangentWS = CameraRight;
    o.BitangentWS = CameraUp;
    return o;
}

VSOut VSMesh(VSInMesh i)
{
    VSOut o;
    float4 posWS = mul(float4(i.Position.xyz, 1), World);
    o.PositionWS = posWS.xyz;
    o.Position = mul(posWS, ViewProjection);
    o.Color = float4(1, 1, 1, 1);
    o.UV = i.UV * TilingOffset.xy + TilingOffset.zw;
    o.NormalWS = mul(i.Normal, (float3x3)World);
    o.TangentWS = mul(i.Tangent, (float3x3)World);
    o.BitangentWS = mul(-i.Binormal, (float3x3)World);
    return o;
}

float3 RgbToHsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-4;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HsvToRgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

float4 MixParticleColor(float4 baseColor, float4 particleColor)
{
    float4 o = baseColor;
    if (Flags.w > 3.5)         // Color
    {
        float3 a = RgbToHsv(baseColor.rgb);
        float3 b = RgbToHsv(particleColor.rgb);
        o = float4(HsvToRgb(float3(b.x, b.y, a.z)), baseColor.a * particleColor.a);
    }
    else if (Flags.w > 2.5)    // Overlay
    {
        o.rgb = lerp(1 - 2 * (1 - baseColor.rgb) * (1 - particleColor.rgb), 2 * baseColor.rgb * particleColor.rgb, step(baseColor.rgb, 0.5));
        o.a *= particleColor.a;
    }
    else if (Flags.w > 0.5)    // Additive / Subtractive / Difference
    {
        o.rgb = baseColor.rgb + particleColor.rgb * ColorAddSubDiff.x;
        o.rgb = lerp(o.rgb, abs(o.rgb), ColorAddSubDiff.y);
        o.a *= particleColor.a;
    }
    else                       // Multiply
    {
        o = baseColor * particleColor;
    }
    return o;
}

float4 PSParticles(VSOut i) : SV_TARGET0
{
    float4 t = SAMPLE_TEXTURE(BaseMap, i.UV);
    t.rgb = lerp(t.rgb, SRGBToLinear3(t.rgb), Flags2.y);
    float4 albedo = t * BaseColor;
    albedo = MixParticleColor(albedo, i.Color);

    if (Flags.y > 0.5)
        clip(albedo.a - Params3.w);
    if (Flags.z > 0.5)
        albedo.rgb *= albedo.a;

    float3 emission = float3(0, 0, 0);
    if (Flags2.x > 0.5)
    {
        float3 e = SAMPLE_TEXTURE(EmissionMap, i.UV).rgb;
        e = lerp(e, SRGBToLinear3(e), Flags2.w);
        emission = e * EmissionColor;
    }

    float3 color = albedo.rgb + emission;

    if (Flags.x > 0.5)
    {
        // Particles/Lit: metallic workflow, flat ambient + main light
        float2 nxy = float2(0, 0);
        float nz = 1;
        if (Flags2.z > 0.5)
        {
            float4 nt = SAMPLE_TEXTURE(NormalMap, i.UV);
            nxy = (nt.xy * 2.0 - 1.0) * Params3.z;
            nz = max(1.0e-16, sqrt(1.0 - saturate(dot(nxy, nxy))));
        }
        float3 N0 = normalize(i.NormalWS);
        float3 N = normalize(normalize(i.TangentWS) * nxy.x + normalize(i.BitangentWS) * nxy.y + N0 * nz);
        float3 V = normalize(CameraPosition - i.PositionWS);
        float3 L = -normalize(SunDirection);

        float metallic = Params3.x;
        float smoothness = Params3.y;
        float oneMinusReflectivity = 0.96 - metallic * 0.96;
        float reflectivity = 1.0 - oneMinusReflectivity;
        float3 diffuse = albedo.rgb * oneMinusReflectivity;
        float3 specular = lerp(float3(0.04, 0.04, 0.04), albedo.rgb, metallic);
        float perceptualRoughness = 1.0 - smoothness;
        float roughness = max(perceptualRoughness * perceptualRoughness, 0.0078125);
        float roughness2 = max(roughness * roughness, 6.103515625e-5);
        float grazing = saturate(smoothness + reflectivity);
        float normalization = roughness * 4.0 + 2.0;
        float shadow = MainLightShadow(i.PositionWS);
        float NdotL = saturate(dot(N, L));
        float3 brdf = diffuse + specular * DirectBRDFSpecular(roughness2, roughness2 - 1.0, normalization, N, L, V);
        float3 direct = brdf * SunColor * (shadow * NdotL);
        float fres = pow(1.0 - saturate(dot(N, V)), 4.0);
        float3 gi = AmbientColor * diffuse + AmbientColor * (1.0 / (roughness2 + 1.0)) * lerp(specular, grazing, fres);
        color = gi + direct + emission;
    }

    return float4(min(color, 60000.0), albedo.a);
}

TECHNIQUE(ParticlesBillboard, VSBillboard, PSParticles);
TECHNIQUE(ParticlesMesh, VSMesh, PSParticles);
