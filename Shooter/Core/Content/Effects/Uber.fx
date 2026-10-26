// URP UberPost: bloom composite, vignette and ACES tonemapping (HDR colour-grading path evaluated analytically instead of
// through the 32^3 LogC LUT: all colour-grading overrides in the project are at their neutral defaults, so the LUT is
// identity colour grading followed by Tonemap()). Output is LINEAR LDR (unsaturated, may be marginally outside 0..1);
// FinalPost does FXAA, film grain and the linear -> sRGB encode.
//
// Texture slots: 0 SourceTex (HDR scene), 1 BloomTex.

#include "PostCommon.fxh"

float4 BloomParams;      // x intensity, yzw tint
float4 VignetteParams1;  // rgb colour (raw volume value), w roundness
float4 VignetteParams2;  // xy centre, z intensity * 3, w smoothness * 5
float4 UberFlags = float4(1, 1, 1, 0); // x bloom on, y vignette on, z tonemap ACES on, w unused
float  PostExposure = 1;

DECLARE_TEXTURE(SourceTex, 0);
DECLARE_TEXTURE(BloomTex, 1);

// ---- ACES (core ACES.hlsl / Color.hlsl AcesTonemap, TONEMAPPING_USE_FULL_ACES = 0) ----

static const float3x3 sRGB_2_AP0 = {
    0.4397010, 0.3829780, 0.1773350,
    0.0897923, 0.8134230, 0.0967616,
    0.0175440, 0.1115440, 0.8707040
};

static const float3x3 AP0_2_AP1_MAT = {
     1.4514393161, -0.2365107469, -0.2149285693,
    -0.0765537734,  1.1762296998, -0.0996759264,
     0.0083161484, -0.0060324498,  0.9977163014
};

static const float3x3 AP1_2_XYZ_MAT = {
     0.6624541811, 0.1340042065, 0.1561876870,
     0.2722287168, 0.6740817658, 0.0536895174,
    -0.0055746495, 0.0040607335, 1.0103391003
};

static const float3x3 XYZ_2_AP1_MAT = {
     1.6410233797, -0.3248032942, -0.2364246952,
    -0.6636628587,  1.6153315917,  0.0167563477,
     0.0117218943, -0.0082844420,  0.9883948585
};

static const float3x3 XYZ_2_REC709_MAT = {
     3.2409699419, -1.5373831776, -0.4986107603,
    -0.9692436363,  1.8759675015,  0.0415550574,
     0.0556300797, -0.2039769589,  1.0569715142
};

static const float3x3 D60_2_D65_CAT = {
     0.98722400, -0.00611327, 0.0159533,
    -0.00759836,  1.00186000, 0.0053302,
     0.00307257, -0.00509595, 1.0816800
};

static const float3 AP1_RGB2Y = float3(0.272229, 0.674082, 0.0536895);

float rgb_2_saturation(float3 rgb)
{
    const float TINY = 1e-4;
    float mi = min(rgb.r, min(rgb.g, rgb.b));
    float ma = max(rgb.r, max(rgb.g, rgb.b));
    return (max(ma, TINY) - max(mi, TINY)) / max(ma, 1e-2);
}

float rgb_2_yc(float3 rgb)
{
    const float ycRadiusWeight = 1.75;
    float r = rgb.x;
    float g = rgb.y;
    float b = rgb.z;
    float k = b * (b - g) + g * (g - r) + r * (r - b);
    k = max(k, 0.0);
    float chroma = sqrt(k);
    return (b + g + r + ycRadiusWeight * chroma) / 3.0;
}

float rgb_2_hue(float3 rgb)
{
    float hue;
    if (rgb.x == rgb.y && rgb.y == rgb.z)
        hue = 0.0;
    else
        hue = (180.0 / PI) * atan2(sqrt(3.0) * (rgb.y - rgb.z), 2.0 * rgb.x - rgb.y - rgb.z);
    if (hue < 0.0) hue = hue + 360.0;
    return hue;
}

float center_hue(float hue, float centerH)
{
    float hueCentered = hue - centerH;
    if (hueCentered < -180.0) hueCentered = hueCentered + 360.0;
    else if (hueCentered > 180.0) hueCentered = hueCentered - 360.0;
    return hueCentered;
}

float sigmoid_shaper(float x)
{
    float t = max(1.0 - abs(x / 2.0), 0.0);
    float sgn = (x < 0.0) ? -1.0 : 1.0;
    float y = 1.0 + sgn * (1.0 - t * t);
    return y / 2.0;
}

float glow_fwd(float ycIn, float glowGainIn, float glowMid)
{
    float glowGainOut;
    if (ycIn <= 2.0 / 3.0 * glowMid)
        glowGainOut = glowGainIn;
    else if (ycIn >= 2.0 * glowMid)
        glowGainOut = 0.0;
    else
        glowGainOut = glowGainIn * (glowMid / ycIn - 1.0 / 2.0);
    return glowGainOut;
}

float3 darkSurround_to_dimSurround(float3 linearCV)
{
    float3 XYZ = mul(AP1_2_XYZ_MAT, linearCV);
    float divisor = max(dot(XYZ, float3(1, 1, 1)), 1e-4);
    float2 xy = XYZ.xy / divisor;
    float Y = XYZ.y;
    Y = clamp(Y, 0.0, 65504.0);
    Y = pow(Y, 0.9811);
    float m = Y / max(xy.y, 1e-4);
    float3 XYZ2 = float3(xy.x * m, Y, (1.0 - xy.x - xy.y) * m);
    return mul(XYZ_2_AP1_MAT, XYZ2);
}

float3 AcesTonemap(float3 aces)
{
    // glow module
    float saturation = rgb_2_saturation(aces);
    float ycIn = rgb_2_yc(aces);
    float s = sigmoid_shaper((saturation - 0.4) / 0.2);
    float addedGlow = 1.0 + glow_fwd(ycIn, 0.05 * s, 0.08);
    aces *= addedGlow;

    // red modifier
    float hue = rgb_2_hue(aces);
    float centeredHue = center_hue(hue, 0.0);
    float hueWeight = smoothstep(0.0, 1.0, 1.0 - abs(2.0 * centeredHue / 135.0));
    hueWeight *= hueWeight;
    aces.r += hueWeight * saturation * (0.03 - aces.r) * (1.0 - 0.82);

    // ACES to RGB rendering space
    float3 acescg = max(0.0, mul(AP0_2_AP1_MAT, aces));

    // global desaturation
    acescg = lerp(dot(acescg, AP1_RGB2Y).xxx, acescg, 0.96);

    // RRT + ODT fit
    const float a = 0.0245786;
    const float b = 0.000090537;
    const float c = 0.983729;
    const float d = 0.4329510;
    const float e = 0.238081;
    float3 rgbPost = (acescg * (acescg + a) - b) / (acescg * (c * acescg + d) + e);

    float3 linearCV = darkSurround_to_dimSurround(rgbPost);
    linearCV = lerp(dot(linearCV, AP1_RGB2Y).xxx, linearCV, 0.93);

    float3 XYZ = mul(AP1_2_XYZ_MAT, linearCV);
    XYZ = mul(D60_2_D65_CAT, XYZ);
    return mul(XYZ_2_REC709_MAT, XYZ);
}

// LogC (EI 1000) clamp of the 32-entry LUT domain
float3 LinearToLogC(float3 x)
{
    float3 lo = 5.301883 * x + 0.092819;
    float3 hi = 0.244161 * log10(max(5.555556 * x + 0.047996, 1e-8)) + 0.386036;
    return (x > 0.011361) ? hi : lo;
}

float3 LogCToLinear(float3 x)
{
    return (pow(10.0, (x - 0.386036) / 0.244161) - 0.047996) / 5.555556;
}

float4 PSUber(PostVSOut i) : SV_TARGET0
{
    float2 uv = i.UV;
    float3 color = tex2Dlod(SourceTexSampler, float4(uv, 0, 0)).rgb;

    if (UberFlags.x > 0.5)
    {
        float3 bloom = tex2Dlod(BloomTexSampler, float4(uv, 0, 0)).rgb;
        bloom *= BloomParams.x;
        color += bloom * BloomParams.yzw;
    }

    if (UberFlags.y > 0.5)
    {
        // ApplyVignette (Common.hlsl)
        float2 dist = abs(uv - VignetteParams2.xy) * VignetteParams2.z;
        dist.x *= VignetteParams1.w;
        float vfactor = pow(saturate(1.0 - dot(dist, dist)), VignetteParams2.w);
        color = color * lerp(VignetteParams1.rgb, float3(1, 1, 1), vfactor);
    }

    color *= PostExposure;

    if (UberFlags.z > 0.5)
    {
        // HDR grading LUT domain (LogC, saturated) followed by the ACES tonemap
        float3 inLut = saturate(LinearToLogC(color));
        color = LogCToLinear(inLut);
        float3 aces = mul(sRGB_2_AP0, color);
        color = AcesTonemap(aces);
    }

    return float4(color, 1);
}

TECHNIQUE(Uber, PostVS, PSUber);
