// URP ScreenSpaceAmbientOcclusion (standard mode, InterleavedGradient noise, 4 samples, source = depth normals,
// bilateral "High" blur: horizontal, vertical, final small blur). Settings from Default_PipelineAsset_ForwardRenderer:
//   intensity 0.3, radius 0.035, falloff 100, direct lighting strength 0.25 (applied in Lit.fx).
// Reconstruction is done in VIEW space (URP uses camera-relative world space; the maths is rotation invariant).
// Slots: 0 DepthTex (rgb view-space normal, a linear eye depth, <= 0 => skip), 1 AoTex (previous pass: r = ao, gba = packed normal)
// Output of the first three techniques: r = ao, gba = normal * 0.5 + 0.5. SsaoFinal: r = 1 - ao (1 => unoccluded).

#include "PostCommon.fxh"

float4 SsaoParams = float4(0.3, 0.035, 1.0, 100.0); // intensity, radius, downsample, falloff
float2 ProjScale;                                  // projection m00, m11
float2 TexelSize;                                  // 1 / size
float2 ScreenSize;
float2 BlurDirection;                              // (1,0) or (0,1)

DECLARE_TEXTURE(DepthTex, 0);
DECLARE_TEXTURE(AoTex, 1);

static const float kContrast = 0.6;
static const float kBeta = 0.004;
static const float kEpsilon = 0.0001;
static const float kGeometryCoeff = 0.8;

static const float RandomA[4] = { 0.00000000, 0.33984375, 0.75390625, 0.56640625 };
static const float RandomB[4] = { 0.92968750, 0.76171875, 0.13333330, 0.01562500 };

float InterleavedGradientNoise(float2 pixCoord, float frameCount)
{
    pixCoord += frameCount * float2(2.083, 4.867);
    return frac(52.9829189 * frac(dot(pixCoord, float2(0.06711056, 0.00583715))));
}

float3 ReconstructViewPos(float2 uv, float depth)
{
    float2 ndc = float2(uv.x * 2.0 - 1.0, 1.0 - uv.y * 2.0);
    return float3(ndc.x * depth / ProjScale.x, ndc.y * depth / ProjScale.y, -depth);
}

float4 PSSsao(PostVSOut i) : SV_TARGET0
{
    float2 uv = i.UV;
    float4 center = tex2Dlod(DepthTexSampler, float4(uv, 0, 0));
    float centerDepth = center.a;
    if (centerDepth <= 0.0 || centerDepth > SsaoParams.w)
        return float4(0, 0.5, 0.5, 1.0);

    float3 centerNormal = normalize(center.xyz);
    float3 centerViewPos = ReconstructViewPos(uv, centerDepth);
    float radius = SsaoParams.y;

    const int SAMPLE_COUNT = 4;
    float rcpSampleCount = 1.0 / SAMPLE_COUNT;
    float ao = 0.0;
    float2 positionSS = uv * ScreenSize;

    [unroll] for (int s = 0; s < SAMPLE_COUNT; s++)
    {
        float noise = InterleavedGradientNoise(positionSS, s);
        float u = frac(RandomA[s] + noise) * 2.0 - 1.0;
        float theta = (RandomB[s] + noise) * 6.28318530717958647693;
        float u2 = sqrt(max(1.0 - u * u, 0.0));
        float sinT, cosT;
        sincos(theta, sinT, cosT);
        float3 v = float3(u2 * cosT, u2 * sinT, u);
        v *= sqrt((s + 1.0) * rcpSampleCount);
        // faceforward(v, -centerNormal, v): flip so that v points into the normal's hemisphere
        v = (dot(-centerNormal, v) < 0.0) ? v : -v;
        v *= radius;

        float3 vpos = centerViewPos + v;
        float zDist = -vpos.z;
        float2 ndc = float2(ProjScale.x * vpos.x / zDist, ProjScale.y * vpos.y / zDist);
        float2 uvS = saturate(float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5));

        float depthS = tex2Dlod(DepthTexSampler, float4(uvS, 0, 0)).a;
        float isInside = (abs(zDist - depthS) < radius) ? 1.0 : 0.0;
        isInside *= (depthS > 0.0 && depthS < SsaoParams.w + 1.0) ? 1.0 : 0.0;

        float3 v2 = ReconstructViewPos(uvS, depthS) - centerViewPos;
        float dotVal = dot(v2, centerNormal) - kBeta * centerDepth;
        float a1 = max(dotVal, 0.0);
        float a2 = dot(v2, v2) + kEpsilon;
        ao += a1 / a2 * isInside;
    }

    ao *= radius;
    float falloff = 1.0 - centerDepth / SsaoParams.w;
    falloff = falloff * falloff;
    ao = pow(saturate(ao * SsaoParams.x * falloff * rcpSampleCount), kContrast);
    return float4(ao, centerNormal * 0.5 + 0.5);
}

float CompareNormal(float3 d1, float3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

float4 PSBlur(PostVSOut i) : SV_TARGET0
{
    float2 uv = i.UV;
    float2 delta = BlurDirection * TexelSize;
    float4 p0 = tex2Dlod(AoTexSampler, float4(uv, 0, 0));
    float4 p1a = tex2Dlod(AoTexSampler, float4(uv - delta * 1.3846153846, 0, 0));
    float4 p1b = tex2Dlod(AoTexSampler, float4(uv + delta * 1.3846153846, 0, 0));
    float4 p2a = tex2Dlod(AoTexSampler, float4(uv - delta * 3.2307692308, 0, 0));
    float4 p2b = tex2Dlod(AoTexSampler, float4(uv + delta * 3.2307692308, 0, 0));

    float3 n0 = p0.gba * 2.0 - 1.0;
    float w0 = 0.2270270270;
    float w1a = CompareNormal(n0, p1a.gba * 2.0 - 1.0) * 0.3162162162;
    float w1b = CompareNormal(n0, p1b.gba * 2.0 - 1.0) * 0.3162162162;
    float w2a = CompareNormal(n0, p2a.gba * 2.0 - 1.0) * 0.0702702703;
    float w2b = CompareNormal(n0, p2b.gba * 2.0 - 1.0) * 0.0702702703;

    float s = p0.r * w0 + p1a.r * w1a + p1b.r * w1b + p2a.r * w2a + p2b.r * w2b;
    s /= (w0 + w1a + w1b + w2a + w2b);
    return float4(s, n0 * 0.5 + 0.5);
}

float4 PSFinal(PostVSOut i) : SV_TARGET0
{
    float2 uv = i.UV;
    float2 d = TexelSize;
    float4 p0 = tex2Dlod(AoTexSampler, float4(uv, 0, 0));
    float4 p1 = tex2Dlod(AoTexSampler, float4(uv + float2(-d.x, -d.y), 0, 0));
    float4 p2 = tex2Dlod(AoTexSampler, float4(uv + float2(d.x, -d.y), 0, 0));
    float4 p3 = tex2Dlod(AoTexSampler, float4(uv + float2(-d.x, d.y), 0, 0));
    float4 p4 = tex2Dlod(AoTexSampler, float4(uv + float2(d.x, d.y), 0, 0));
    float3 n0 = p0.gba * 2.0 - 1.0;
    float w1 = CompareNormal(n0, p1.gba * 2.0 - 1.0);
    float w2 = CompareNormal(n0, p2.gba * 2.0 - 1.0);
    float w3 = CompareNormal(n0, p3.gba * 2.0 - 1.0);
    float w4 = CompareNormal(n0, p4.gba * 2.0 - 1.0);
    float s = p0.r + p1.r * w1 + p2.r * w2 + p3.r * w3 + p4.r * w4;
    s /= (1.0 + w1 + w2 + w3 + w4);
    float v = 1.0 - s;
    return float4(v, v, v, 1);
}

TECHNIQUE(SsaoMain, PostVS, PSSsao);
TECHNIQUE(SsaoBlur, PostVS, PSBlur);
TECHNIQUE(SsaoFinal, PostVS, PSFinal);
