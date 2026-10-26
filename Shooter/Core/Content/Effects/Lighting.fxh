// Shared URP lighting helpers (main-light soft shadows + direct BRDF). The including effect must declare, BEFORE including this file:
//   float3 CameraPosition, SunDirection; float4 ShadowParams; float2 ShadowFade; float4 MaterialFlags2 (y = receive shadows);
//   DECLARE_TEXTURE(ShadowMap, 5)   (R32F light-space depth, point sampled)
#ifndef SHOOTER_LIGHTING_FXH
#define SHOOTER_LIGHTING_FXH

// ---- soft shadows: URP "Medium" quality = 5x5 tent, evaluated exactly with 6x6 point taps ----

float TentCdf(float x)
{
    // CDF of a triangular kernel with half width 2.5 texels.
    float t = clamp(x, -2.5, 2.5);
    float a = t + 2.5;
    float b = 2.5 - t;
    return (t < 0.0) ? (a * a) / 12.5 : 1.0 - (b * b) / 12.5;
}

float SampleShadowTent(float2 uv, float z)
{
    float res = ShadowParams.z;
    float inv = ShadowParams.y;
    float2 tc = uv * res;
    float2 j0 = floor(tc - 2.5);
    float2 rel = j0 - tc;

    float wx[6];
    float wy[6];
    float cx = TentCdf(rel.x);
    float cy = TentCdf(rel.y);
    [unroll] for (int k = 0; k < 6; k++)
    {
        float nx = TentCdf(rel.x + k + 1.0);
        float ny = TentCdf(rel.y + k + 1.0);
        wx[k] = nx - cx;
        wy[k] = ny - cy;
        cx = nx;
        cy = ny;
    }

    float sum = 0.0;
    [unroll] for (int y = 0; y < 6; y++)
    {
        float v = (j0.y + y + 0.5) * inv;
        float rowSum = 0.0;
        [unroll] for (int x = 0; x < 6; x++)
        {
            float u = (j0.x + x + 0.5) * inv;
            float d = tex2Dlod(ShadowMapSampler, float4(u, v, 0, 0)).r;
            rowSum += wx[x] * ((z <= d) ? 1.0 : 0.0);
        }
        sum += rowSum * wy[y];
    }
    return sum;
}

float MainLightShadow(float3 positionWS)
{
    if (ShadowParams.x <= 0.0 || MaterialFlags2.y < 0.5)
        return 1.0;

    // NOTE: use all four components of the result. The MGFX compiler trims matrix parameters to the columns that the first
    // shader reading them uses (float4x3); a pixel shader that ignored .w would shrink ShadowViewProjection and break the caster VS.
    float4 sc = mul(float4(positionWS, 1), ShadowViewProjection);
    sc.xyz /= sc.w;
    float2 uv = float2(sc.x * 0.5 + 0.5, 0.5 - sc.y * 0.5);
    float z = sc.z;

    float realtime = 1.0;
    if (z > 0.0 && z < 1.0 && uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0)
        realtime = SampleShadowTent(uv, z);

    realtime = lerp(1.0, realtime, ShadowParams.x);

    float3 camToPixel = positionWS - CameraPosition;
    float fade = saturate(dot(camToPixel, camToPixel) * ShadowFade.x + ShadowFade.y);
    return lerp(realtime, 1.0, fade);
}

// ---- URP BRDF ----

float DirectBRDFSpecular(float roughness2, float roughness2MinusOne, float normalizationTerm, float3 N, float3 L, float3 V)
{
    float3 halfDir = normalize(L + V + 1e-6);
    float NoH = saturate(dot(N, halfDir));
    float LoH = saturate(dot(L, halfDir));
    float d = NoH * NoH * roughness2MinusOne + 1.00001;
    float LoH2 = LoH * LoH;
    return roughness2 / ((d * d) * max(0.1, LoH2) * normalizationTerm);
}

#endif
