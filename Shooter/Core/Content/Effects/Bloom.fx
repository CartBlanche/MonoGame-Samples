// URP Bloom.shader (Gaussian mode, non-HQ): prefilter (threshold + soft knee + clamp), separable blur down the mip chain,
// scatter-weighted upsample. Pass parameters follow BloomPostProcessPass.cs:
//   Params = (scatter = lerp(0.05, 0.95, userScatter), clamp, threshold (linear), knee = threshold * 0.5)
// Samplers must be LinearClamp. SourceTexelSize = 1 / size of the texture bound to SourceTex (xy).

#include "PostCommon.fxh"

float4 Params;
float4 SourceTexelSize;

DECLARE_TEXTURE(SourceTex, 0);
DECLARE_TEXTURE(LowMipTex, 1);

float2 ClampUV(float2 uv, float2 texel)
{
    return clamp(uv, 0.5 * texel, 1.0 - 0.5 * texel);
}

float3 SampleHDR(float2 uv, float2 offset)
{
    float2 ts = SourceTexelSize.xy;
    return tex2Dlod(SourceTexSampler, float4(ClampUV(uv - offset * ts, ts), 0, 0)).rgb;
}

float4 PSPrefilter(PostVSOut i) : SV_TARGET0
{
    float3 color = tex2Dlod(SourceTexSampler, float4(i.UV, 0, 0)).rgb;
    color = min(Params.y, color);
    float threshold = Params.z;
    float knee = Params.w;
    float brightness = max(color.r, max(color.g, color.b));
    float softness = clamp(brightness - threshold + knee, 0.0, 2.0 * knee);
    softness = (softness * softness) / (4.0 * knee + 1e-4);
    float multiplier = max(brightness - threshold, softness) / max(brightness, 1e-4);
    color *= multiplier;
    return float4(max(color, 0), 1);
}

float4 PSBlurH(PostVSOut i) : SV_TARGET0
{
    float2 ts = SourceTexelSize.xy * 2.0;
    float2 uv = i.UV;
    float3 c0 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv - float2(ts.x * 4.0, 0.0), ts), 0, 0)).rgb;
    float3 c1 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv - float2(ts.x * 3.0, 0.0), ts), 0, 0)).rgb;
    float3 c2 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv - float2(ts.x * 2.0, 0.0), ts), 0, 0)).rgb;
    float3 c3 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv - float2(ts.x * 1.0, 0.0), ts), 0, 0)).rgb;
    float3 c4 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv, ts), 0, 0)).rgb;
    float3 c5 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv + float2(ts.x * 1.0, 0.0), ts), 0, 0)).rgb;
    float3 c6 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv + float2(ts.x * 2.0, 0.0), ts), 0, 0)).rgb;
    float3 c7 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv + float2(ts.x * 3.0, 0.0), ts), 0, 0)).rgb;
    float3 c8 = tex2Dlod(SourceTexSampler, float4(ClampUV(uv + float2(ts.x * 4.0, 0.0), ts), 0, 0)).rgb;

    float3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
                 + c4 * 0.22702703
                 + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;
    return float4(color, 1);
}

float4 PSBlurV(PostVSOut i) : SV_TARGET0
{
    float2 uv = i.UV;
    float3 c0 = SampleHDR(uv, -float2(0.0, 3.23076923));
    float3 c1 = SampleHDR(uv, -float2(0.0, 1.38461538));
    float3 c2 = SampleHDR(uv, float2(0.0, 0.0));
    float3 c3 = SampleHDR(uv, float2(0.0, 1.38461538));
    float3 c4 = SampleHDR(uv, float2(0.0, 3.23076923));
    float3 color = c0 * 0.07027027 + c1 * 0.31621622
                 + c2 * 0.22702703
                 + c3 * 0.31621622 + c4 * 0.07027027;
    return float4(color, 1);
}

float4 PSUpsample(PostVSOut i) : SV_TARGET0
{
    float3 highMip = tex2Dlod(SourceTexSampler, float4(i.UV, 0, 0)).rgb;
    float3 lowMip = tex2Dlod(LowMipTexSampler, float4(i.UV, 0, 0)).rgb;
    return float4(lerp(highMip, lowMip, Params.x), 1);
}

TECHNIQUE(BloomPrefilter, PostVS, PSPrefilter);
TECHNIQUE(BloomBlurH, PostVS, PSBlurH);
TECHNIQUE(BloomBlurV, PostVS, PSBlurV);
TECHNIQUE(BloomUpsample, PostVS, PSUpsample);
