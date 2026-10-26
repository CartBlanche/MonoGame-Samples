// Port of URP 17 "Universal Render Pipeline/Lit" (forward, one directional light, flat ambient) for MonoGame DesktopGL.
//
// Sources ported: Lit.shader / LitForwardPass.hlsl / LitInput.hlsl / BRDF.hlsl / GlobalIllumination.hlsl /
// AmbientOcclusion.hlsl / Shadows.hlsl (5x5 tent soft shadows) / ShadowCasterPass.hlsl.
//
// Techniques:   LitStatic, LitSkinned          main colour pass (HDR, linear)
//               ShadowStatic, ShadowSkinned    shadow caster (linear 0..1 light depth in R32F, URP-style caster-side bias)
//               DepthNormalsStatic/Skinned     view-space normal (rgb) + linear eye depth (a) for SSAO / motion blur
//
// Texture slots: 0 BaseMap, 1 NormalMap, 2 MetallicGlossMap, 3 OcclusionMap, 4 EmissionMap, 5 ShadowMap, 6 SsaoMap.
// Colours/lighting constants are expected LINEAR (the C# side converts Unity's gamma-serialised colours).

#include "Common.fxh"

float4x4 World;
float4x4 ViewProjection;
float4x4 View;                 // for DepthNormals
float4x4 ShadowViewProjection; // world -> light clip (orthographic, z in 0..1)
float4x4 Bones[MAX_BONES];

float3 CameraPosition;

// Material
float4 BaseColor = float4(1, 1, 1, 1);
float4 TilingOffset = float4(1, 1, 0, 0);
float3 EmissionColor;
float  Metallic;
float  Smoothness = 0.5;
float  BumpScale = 1;
float  OcclusionStrength = 1;
float  Cutoff = 0.5;
// x: metallic map present, y: alpha clip, z: specular highlights on, w: environment reflections on
float4 MaterialFlags = float4(0, 0, 1, 1);
// x: premultiply alpha (diffuse *= alpha), y: receive shadows, z: use ssao, w: unused
float4 MaterialFlags2 = float4(0, 1, 0, 0);
// 1 => sample is sRGB encoded and must be decoded: x base, y emission, z occlusion, w metallic/gloss
float4 SrgbFlags = float4(1, 1, 1, 1);

// Lighting (linear, intensity already applied)
float3 SunDirection;           // direction the light travels
float3 SunColor;
float3 AmbientColor;           // flat ambient == SH L0 == _GlossyEnvironmentColor
float4 ShadowParams;           // x: shadow strength (0 = off), y: 1 / map resolution, z: map resolution, w: unused
float2 ShadowFade;             // scale, bias applied to squared camera distance (URP _MainLightShadowParams.zw)
float3 ShadowBias;             // x depth bias, y normal bias (world units, negative), z unused

DECLARE_TEXTURE(BaseMap, 0);
DECLARE_TEXTURE(NormalMap, 1);
DECLARE_TEXTURE(MetallicGlossMap, 2);
DECLARE_TEXTURE(OcclusionMap, 3);
DECLARE_TEXTURE(EmissionMap, 4);
DECLARE_TEXTURE(ShadowMap, 5);
DECLARE_TEXTURE(SsaoMap, 6);

struct VSInStatic
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 UV       : TEXCOORD0;
    float3 Tangent  : TANGENT0;
    float3 Binormal : BINORMAL0;
};

struct VSInSkinned
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 UV       : TEXCOORD0;
    float4 Tangent  : TANGENT0;
    float4 Indices  : BLENDINDICES0;
    float4 Weights  : BLENDWEIGHT0;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float3 PositionWS : TEXCOORD0;
    float3 NormalWS   : TEXCOORD1;
    float3 TangentWS  : TEXCOORD2;
    float3 BitangentWS: TEXCOORD3;   // already flipped for Unity's green-up convention
    float2 UV         : TEXCOORD4;
    float4 Clip       : TEXCOORD5;
};

VSOut BuildOutput(float3 positionOS, float3 normalOS, float3 tangentOS, float3 bitangentUnityOS, float2 uv)
{
    VSOut o;
    float4 posWS = mul(float4(positionOS, 1), World);
    o.PositionWS = posWS.xyz;
    o.NormalWS = mul(normalOS, (float3x3)World);
    o.TangentWS = mul(tangentOS, (float3x3)World);
    o.BitangentWS = mul(bitangentUnityOS, (float3x3)World);
    o.UV = uv;
    o.Position = mul(posWS, ViewProjection);
    o.Clip = o.Position;
    return o;
}

VSOut VSStatic(VSInStatic i)
{
    // Binormal0 = dP/dv' (v' flipped) -> Unity green-up bitangent is the negative (ASSET-CONVENTIONS.md section 2).
    return BuildOutput(i.Position.xyz, i.Normal, i.Tangent, -i.Binormal, i.UV);
}

void Skin(float4 indices, float4 weights, inout float3 pos, inout float3 nrm, inout float3 tan)
{
    int4 idx = (int4)(indices + 0.5);
    float4x4 skin = Bones[idx.x] * weights.x
                  + Bones[idx.y] * weights.y
                  + Bones[idx.z] * weights.z
                  + Bones[idx.w] * weights.w;
    float4 p4 = mul(float4(pos, 1), skin);
    pos = p4.xyz / p4.w; // using .w keeps every Bones[] element a full 4x4 (MGFX trims matrix parameters to the columns a shader uses)
    nrm = mul(nrm, (float3x3)skin);
    tan = mul(tan, (float3x3)skin);
}

VSOut VSSkinned(VSInSkinned i)
{
    float3 pos = i.Position.xyz;
    float3 nrm = i.Normal;
    float3 tan = i.Tangent.xyz;
    Skin(i.Indices, i.Weights, pos, nrm, tan);
    float3 bit = cross(nrm, tan) * i.Tangent.w;
    return BuildOutput(pos, nrm, tan, bit, i.UV);
}

#include "Lighting.fxh"

float4 PSLit(VSOut i) : SV_TARGET0
{
    float2 uv = i.UV * TilingOffset.xy + TilingOffset.zw;

    float4 baseTex = SAMPLE_TEXTURE(BaseMap, uv);
    baseTex.rgb = lerp(baseTex.rgb, SRGBToLinear3(baseTex.rgb), SrgbFlags.x);
    float3 albedo = baseTex.rgb * BaseColor.rgb;
    float alpha = baseTex.a * BaseColor.a;
    if (MaterialFlags.y > 0.5)
        clip(alpha - Cutoff);

    // metallic / smoothness
    float4 mg = SAMPLE_TEXTURE(MetallicGlossMap, uv);
    mg.rgb = lerp(mg.rgb, SRGBToLinear3(mg.rgb), SrgbFlags.w);
    float metallic = lerp(Metallic, mg.r, MaterialFlags.x);
    float smoothness = lerp(Smoothness, mg.a * Smoothness, MaterialFlags.x);

    // occlusion (LitInput.hlsl SampleOcclusion: lerp(1, g, strength))
    float4 occTex = SAMPLE_TEXTURE(OcclusionMap, uv);
    float occ = lerp(occTex.g, SRGBToLinear3(occTex.rgb).g, SrgbFlags.z);
    float occlusion = lerp(1.0, occ, OcclusionStrength);

    float3 emission = SAMPLE_TEXTURE(EmissionMap, uv).rgb;
    emission = lerp(emission, SRGBToLinear3(emission), SrgbFlags.y) * EmissionColor;

    // normal map (UnpackNormalmapRGorAG: scale before z reconstruction)
    float4 nt = SAMPLE_TEXTURE(NormalMap, uv);
    float2 nxy = (nt.xy * 2.0 - 1.0) * BumpScale;
    float nz = max(1.0e-16, sqrt(1.0 - saturate(dot(nxy, nxy))));
    float3 N0 = normalize(i.NormalWS);
    float3 T = normalize(i.TangentWS);
    float3 B = normalize(i.BitangentWS);
    float3 N = normalize(T * nxy.x + B * nxy.y + N0 * nz);

    float3 V = normalize(CameraPosition - i.PositionWS);
    float3 L = -normalize(SunDirection);

    // BRDFData (BRDF.hlsl InitializeBRDFData, metallic workflow)
    float oneMinusReflectivity = 0.96 - metallic * 0.96;
    float reflectivity = 1.0 - oneMinusReflectivity;
    float3 brdfDiffuse = albedo * oneMinusReflectivity;
    float3 brdfSpecular = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
    float perceptualRoughness = 1.0 - smoothness;
    float roughness = max(perceptualRoughness * perceptualRoughness, 0.0078125);
    float roughness2 = max(roughness * roughness, 6.103515625e-5);
    float grazingTerm = saturate(smoothness + reflectivity);
    float normalizationTerm = roughness * 4.0 + 2.0;
    float roughness2MinusOne = roughness2 - 1.0;
    if (MaterialFlags2.x > 0.5)
        brdfDiffuse *= alpha;

    // ambient occlusion (screen space) - AmbientOcclusion.hlsl
    float ssao = 1.0;
    float directAO = 1.0;
    if (MaterialFlags2.z > 0.5)
    {
        float2 suv = ClipToScreenUV(i.Clip);
        ssao = saturate(SAMPLE_TEXTURE(SsaoMap, suv).r);
        directAO = lerp(1.0, ssao, 0.25); // DirectLightingStrength
    }
    float indirectAO = min(ssao, occlusion);

    // main light
    float shadow = MainLightShadow(i.PositionWS);
    float NdotL = saturate(dot(N, L));
    float3 radiance = SunColor * directAO * (shadow * NdotL);
    float3 brdf = brdfDiffuse;
    if (MaterialFlags.z > 0.5)
        brdf += brdfSpecular * DirectBRDFSpecular(roughness2, roughness2MinusOne, normalizationTerm, N, L, V);
    float3 direct = brdf * radiance;

    // GI: flat ambient (SH L0) diffuse + glossy environment (fallback colour when probes are off)
    float NoV = saturate(dot(N, V));
    float fresnelTerm = pow(1.0 - NoV, 4.0);
    float3 indirectDiffuse = AmbientColor;
    float3 indirectSpecular = AmbientColor; // _GlossyEnvironmentColor (also used as the reflection fallback)
    float surfaceReduction = 1.0 / (roughness2 + 1.0);
    float3 gi = indirectDiffuse * brdfDiffuse
              + indirectSpecular * (surfaceReduction * lerp(brdfSpecular, grazingTerm, fresnelTerm));
    gi *= indirectAO;

    float3 color = gi + direct + emission;
    return float4(color, alpha);
}

// ---- shadow caster ----

struct CasterOut
{
    float4 Position : SV_POSITION;
    float  Depth    : TEXCOORD0;
    float2 UV       : TEXCOORD1;
};

CasterOut BuildCaster(float3 positionOS, float3 normalOS, float2 uv)
{
    CasterOut o;
    float4 posWS4 = mul(float4(positionOS, 1), World);
    float3 posWS = posWS4.xyz / posWS4.w; // .w: keep World a full 4x4 in this shader (see note in Lighting.fxh)
    float3 nrmWS = normalize(mul(normalOS, (float3x3)World));
    float3 toLight = -normalize(SunDirection);

    // ShadowCasterPass.hlsl ApplyShadowBias (bias values are negative -> push casters away from the light / inset)
    float invNdotL = 1.0 - saturate(dot(toLight, nrmWS));
    float scale = invNdotL * ShadowBias.y;
    posWS = toLight * ShadowBias.x + posWS;
    posWS = nrmWS * scale + posWS;

    float4 clipPos = mul(float4(posWS, 1), ShadowViewProjection);
    // ApplyShadowClamping (directional): clamp to the near plane instead of clipping casters in front of it
    clipPos.z = max(clipPos.z, 0.0);
    o.Position = clipPos;
    o.Depth = clipPos.z / clipPos.w;
    o.UV = uv;
    return o;
}

CasterOut VSShadowStatic(VSInStatic i)
{
    return BuildCaster(i.Position.xyz, i.Normal, i.UV);
}

CasterOut VSShadowSkinned(VSInSkinned i)
{
    float3 pos = i.Position.xyz;
    float3 nrm = i.Normal;
    float3 tan = i.Tangent.xyz;
    Skin(i.Indices, i.Weights, pos, nrm, tan);
    return BuildCaster(pos, nrm, i.UV);
}

float4 PSShadow(CasterOut i) : SV_TARGET0
{
    if (MaterialFlags.y > 0.5)
    {
        float2 uv = i.UV * TilingOffset.xy + TilingOffset.zw;
        clip(SAMPLE_TEXTURE(BaseMap, uv).a * BaseColor.a - Cutoff);
    }
    return float4(i.Depth, i.Depth, i.Depth, 1);
}

// ---- depth + normals prepass ----

struct DNOut
{
    float4 Position : SV_POSITION;
    float3 NormalVS : TEXCOORD0;
    float  EyeDepth : TEXCOORD1;
    float2 UV       : TEXCOORD2;
};

DNOut BuildDN(float3 positionOS, float3 normalOS, float2 uv)
{
    DNOut o;
    float4 posWS = mul(float4(positionOS, 1), World);
    float4 posVS = mul(posWS, View);
    o.Position = mul(posWS, ViewProjection);
    o.NormalVS = mul(mul(normalOS, (float3x3)World), (float3x3)View);
    o.EyeDepth = -posVS.z / posVS.w; // .w keeps the View parameter a full 4x4 (see Lighting.fxh note)
    o.UV = uv;
    return o;
}

DNOut VSDepthNormalsStatic(VSInStatic i)
{
    return BuildDN(i.Position.xyz, i.Normal, i.UV);
}

DNOut VSDepthNormalsSkinned(VSInSkinned i)
{
    float3 pos = i.Position.xyz;
    float3 nrm = i.Normal;
    float3 tan = i.Tangent.xyz;
    Skin(i.Indices, i.Weights, pos, nrm, tan);
    return BuildDN(pos, nrm, i.UV);
}

float4 PSDepthNormals(DNOut i) : SV_TARGET0
{
    if (MaterialFlags.y > 0.5)
    {
        float2 uv = i.UV * TilingOffset.xy + TilingOffset.zw;
        clip(SAMPLE_TEXTURE(BaseMap, uv).a * BaseColor.a - Cutoff);
    }
    // MaterialFlags2.w > 0.5 marks viewmodel geometry: depth 0 = "ignore" for SSAO / motion blur
    float depth = (MaterialFlags2.w > 0.5) ? 0.0 : i.EyeDepth;
    return float4(normalize(i.NormalVS), depth);
}

TECHNIQUE(LitStatic, VSStatic, PSLit);
TECHNIQUE(LitSkinned, VSSkinned, PSLit);
TECHNIQUE(ShadowStatic, VSShadowStatic, PSShadow);
TECHNIQUE(ShadowSkinned, VSShadowSkinned, PSShadow);
TECHNIQUE(DepthNormalsStatic, VSDepthNormalsStatic, PSDepthNormals);
TECHNIQUE(DepthNormalsSkinned, VSDepthNormalsSkinned, PSDepthNormals);
