//-----------------------------------------------------------------------------
// TrackShadows.fxh
// Integrated shadows: the shadow map lookup of ShadowMap.fx (PS_UseShadowMap20)
// done inside the lit shaders, applied to the sun/moon term only.
// See Documentation/Shadow-Prototype-Plan.md. Set from TrackLighting.Shadows.cs.
// Include after Macros.fxh. Only included by the shaders converted so far.
//
// GL starts every parameter at 0, so ShadowMode 0 means "no shadow term".
//-----------------------------------------------------------------------------
#ifndef TRACKSHADOWS_FXH
#define TRACKSHADOWS_FXH

BEGIN_CONSTANTS
// 0 = no shadow term (classic overlay or shadows off), 1 = integrated
float ShadowMode;
// World -> light clip space with the texture bias of ShadowMapShader.texScaleBiasMatrix
// folded in: xy / w = shadow map texture coordinates, w = distance to the light.
// Three named columns of that matrix (ShadowX = output x, ShadowY = y, ShadowW = w).
// Not a float4x4: its unused z column was compacted away by the GL shader translation
// and shifted every following uniform (all lit surfaces rendered wrong).
float4 ShadowX, ShadowY, ShadowW;
// x = texel size (1 / map size), y = depth bias, z = normal offset (world units),
// w = strength (1 = fully shadowed pixels lose all their sun term)
float4 ShadowParams;
// Environment fill: what a shadowed pixel keeps of the ambient (sky) light, rgb, per time of
// day (TrackLighting.TimeOfDay.cs SkyFill). The sun term is removed in shadow, the ambient term
// is multiplied by this ("sky occlusion"); 1 = pure sun-term removal. Not the classic ShadowColor.
// w = how much wider the shadow map reaches than the classic one (texel world size scale).
float4 ShadowTint;
// x = sun angular size (radians, penumbra = receiver to blocker distance x this),
// y = quality (1 = 8 blocker + 16 filter taps, 0 = 4 + 8), z = min filter radius (texels),
// w = max filter radius (texels)
float4 ShadowPcss;
// Per material (set by ShaderEffect.SetParametersOptimized): 1 = this surface does not receive
// shadows (banners and signs, like the classic receive pass excludes them). Inverted on purpose:
// GL starts every parameter at 0, so 0 (receive) must be the default for everything else.
float ShadowSkip;
// x = 1: surfaces facing away from the sun are fully in shadow (smooth, no depth compare noise),
// y = 1: corrected texel size estimate (feedback round 1); 0 reproduces the first integrated
// version (A/B tests).
float4 ShadowFlags;
END_CONSTANTS

// Must match farPlane of ShadowMap.fx and shadowFarPlane in ShadowMapShader.cs.
static const float TS_FarPlane = 6.25f * 28 * 1.25f;

BEGIN_DECLARE_TEXTURE_TARGET(ShadowMap, ShadowMapTex)
    AddressU  = Clamp;
    AddressV  = Clamp;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(shadowDistanceFadeoutTexture, ShadowFadeTex)
    AddressU  = Wrap;
    AddressV  = Wrap;
    AddressW  = Wrap;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
END_DECLARE_TEXTURE;

// World units covered by the map per unit of light distance (2 * tan(fov / 2) =
// 2 * virtualVisibleRange / virtualLightDistance of ShadowMapShader.cs).
static const float TS_MapWorldPerDist = 2.0f * 129.25f / 171.6f;
// What the map really covers per unit of light distance: CreatePerspective(width, height) takes
// the extent at distance 1: 0.85 for both axes of the integrated map (the old width was 0.645).
static const float TS_MapWorldPerDistFixed = 0.85f;
// Width (in map uv) of the smooth fade at the border of the shadow map.
static const float TS_EdgeFade = 0.06f;

// 1 = lit .. 0 = shadowed (scaled by ShadowParams.w). P world position, N world normal.
// Contact-hardening (PCSS-lite): an 8 tap blocker search, the penumbra width from the average
// blocker distance, then a variable radius 16 tap filter (literal Poisson taps: a uniform array
// hit the GLSL std140 float2 padding problem).
float TL_Shadow(float3 P, float3 N)
{
    const float2 Taps[16] =
    {
        {-0.94201624f, -0.39906216f}, {0.94558609f, -0.76890725f},
        {-0.094184101f, -0.92938870f}, {0.34495938f, 0.29387760f},
        {-0.91588581f, 0.45771432f}, {-0.81544232f, -0.87912464f},
        {-0.38277543f, 0.27676845f}, {0.97484398f, 0.75648379f},
        {0.44323325f, -0.97511554f}, {0.53742981f, -0.47373420f},
        {-0.26496911f, -0.41893023f}, {0.79197514f, 0.19090188f},
        {-0.24188840f, 0.99706507f}, {-0.81409955f, 0.91437590f},
        {0.19984126f, 0.78641367f}, {0.14383161f, -0.14100790f}
    };

    float internalNear = TS_FarPlane / 10;
    float range = TS_FarPlane - internalNear;
    float texel = ShadowParams.x;

    // Light direction (towards the sun) from the w column (the light's forward axis)
    float3 L = -normalize(ShadowW.xyz);
    float3 Nn = normalize(N);
    float ndl = saturate(dot(Nn, L));
    float sinT = sqrt(saturate(1.0f - ndl * ndl));
    float tanT = min(sinT / max(ndl, 0.05f), 4.0f);

    // World size of one shadow texel at this distance from the light
    float w0 = dot(float4(P, 1), ShadowW);
    float texW = max(w0, 1.0f) * (ShadowFlags.y > 0.5f ? TS_MapWorldPerDistFixed : TS_MapWorldPerDist)
        * ShadowTint.w * texel;

    // Normal offset (grows with the slope) moves the receiver off the surface
    float3 Pn = P + Nn * (ShadowParams.z * texW * (0.3f + sinT));
    float4 P4 = float4(Pn, 1);
    float lw = max(abs(dot(P4, ShadowW)), 0.0001f);
    float2 uv = float2(dot(P4, ShadowX), dot(P4, ShadowY)) / lw;
    float2 shadowTex = uv - texel / 2.0f;
    float depthR = (lw - internalNear) / range;

    float maxR = ShadowPcss.w;
    // Receiver depth with only the base bias plus the slope term (blocker test)
    float biasSearch = ShadowParams.y + texW * tanT * (1.0f + maxR * 0.5f) / range;
    int nB = ShadowPcss.y > 0.5f ? 8 : 4;
    int nF = ShadowPcss.y > 0.5f ? 16 : 8;

    // 1) Blocker search over the largest possible penumbra
    float bSum = 0;
    float bCount = 0;
    for (int i = 0; i < 8; i++)
    {
        if (i >= nB)
            break;
        float d = SAMPLE_TEXTURE(ShadowMap, shadowTex + Taps[i] * (texel * maxR)).r;
        float isB = depthR - biasSearch > d ? 1.0f : 0.0f;
        bSum += d * isB;
        bCount += isB;
    }
    float avgB = bSum / max(bCount, 1.0f);

    // 2) Penumbra width in world units, as filter radius in texels (clamped)
    float penWorld = max(depthR - avgB, 0.0f) * range * ShadowPcss.x;
    float radius = clamp(0.5f * penWorld / texW, ShadowPcss.z, maxR);

    // 3) Variable radius filter (the slope bias grows with the radius)
    float depth = depthR - (ShadowParams.y + texW * tanT * (1.0f + radius) / range);
    float occ = 0;
    for (int j = 0; j < 16; j++)
    {
        if (j >= nF)
            break;
        occ += depth > SAMPLE_TEXTURE(ShadowMap, shadowTex + Taps[j] * (texel * radius)).r ?
            1.0f : 0.0f;
    }
    occ /= (float)nF;
    // Nothing in front of the receiver within the search area: lit
    occ = bCount > 0.5f ? occ : 0.0f;

    // Smooth fade at the map border (no cut line), at the far end of the map, and the
    // existing distance fade texture
    float edge = min(min(uv.x, 1.0f - uv.x), min(uv.y, 1.0f - uv.y));
    occ *= smoothstep(0.0f, TS_EdgeFade, edge);
    occ *= smoothstep(1.0f, 0.9f, depthR);
    occ *= SAMPLE_TEXTURE(shadowDistanceFadeoutTexture, shadowTex).r;
    // Surfaces turned away from the sun get no sun term anyway: treat them as shadowed
    // consistently (smooth in N dot L) instead of trusting the noisy depth compare there.
    if (ShadowFlags.x > 0.5f)
        occ = max(occ, 1.0f - smoothstep(0.0f, 0.2f, dot(Nn, L)));

    // ShadowMode 0 must be neutral even if the compiler evaluates both branches
    return ShadowMode > 0.5 ? 1.0f - occ * ShadowParams.w * (1.0f - ShadowSkip) : 1.0f;
}

// The natural model in one place for the converted techniques: lit = 1 (sun fully on) .. 0,
// tint = what the ambient (sky) light keeps in shadow. Only the sun/moon term is scaled by lit,
// never the lamp and headlight light (TL_Lights). ShadowMode 0 returns lit 1 and tint 1.
void TL_ShadowTerms(float3 P, float3 N, out float lit, out float3 tint)
{
    lit = 1.0f;
    tint = float3(1.0f, 1.0f, 1.0f);
    if (ShadowMode > 0.5)
    {
        lit = TL_Shadow(P, N);
        tint = lerp(ShadowTint.rgb, float3(1.0f, 1.0f, 1.0f), lit);
    }
}

#endif
