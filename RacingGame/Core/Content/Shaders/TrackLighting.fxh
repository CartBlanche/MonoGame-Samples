//-----------------------------------------------------------------------------
// TrackLighting.fxh
// Shared lighting parameters, set from TrackLighting.cs (Core/Graphics) every frame.
// See Documentation/Lighting-Plan.md section 4.2. This is the contract between the
// C# side and all lit shaders; do not rename parameters without updating both.
//
// MonoGame on GL does not apply .fx initial values (every parameter starts at 0),
// so each parameter must mean "off" at 0 or be set from C# before use. WS0 only
// declares them; the shader compiler drops unused uniforms, so nothing changes
// until WS2 uses them.
//-----------------------------------------------------------------------------
#ifndef TRACKLIGHTING_FXH
#define TRACKLIGHTING_FXH

#define TL_MAX_LIGHTS 4

BEGIN_CONSTANTS
// 0 = legacy per-vertex directional lighting (today), 1 = per-pixel lights
float LightingMode;
// Number of active lights in the arrays below (0..TL_MAX_LIGHTS)
float LightCount;
// x = fog density (0 = off), y = fog start distance
float4 FogParams;
float3 FogColor;
// Hemisphere ambient multipliers on top of the material ambient (1,1,1 = off)
float3 AmbSky;
float3 AmbGround;
// Light list (WS2): xyz position / w range; rgb colour*intensity / w cos(outer)
// (-3 = point light); xyz direction / w cos(inner) (-2 = point light)
float4 LPos0, LPos1, LPos2, LPos3;
float4 LCol0, LCol1, LCol2, LCol3;
float4 LDir0, LDir1, LDir2, LDir3;
END_CONSTANTS

//-----------------------------------------------------------------------------
// WS2 functions. Light model: LCol.w = cos(outer) for spots, -3 for points;
// LDir.w = cos(inner) for spots, -2 for points. Range falloff is (1 - d/range)^2.
// All functions are only reached from the LightingMode > 0.5 / FogParams.x > 0
// branches of the pixel shaders, so the legacy path stays untouched.
//-----------------------------------------------------------------------------
void TL_AddLight(float4 lp, float4 lc, float4 ld, float3 P, float3 N, float3 V,
    float shin, inout float3 diff, inout float3 spec)
{
    float3 Lv = lp.xyz - P;
    float d = length(Lv);
    float3 L = Lv / max(d, 0.0001);
    float att = saturate(1.0 - d / max(lp.w, 0.001));
    att *= att;
    float isSpot = step(-2.5, lc.w);
    float cone = lerp(1.0, smoothstep(lc.w, ld.w, dot(-L, ld.xyz)), isSpot);
    float nl = saturate(dot(N, L));
    float k = att * cone;
    diff += lc.rgb * (nl * k);
    float3 H = normalize(L + V);
    spec += lc.rgb * (pow(saturate(dot(N, H)), shin) * nl * k);
}

// P world position, N world normal, V world view vector (to the eye).
void TL_Lights(float3 P, float3 N, float3 V, float shin,
    out float3 diff, out float3 spec)
{
    diff = 0;
    spec = 0;
    if (LightCount > 0.5) TL_AddLight(LPos0, LCol0, LDir0, P, N, V, shin, diff, spec);
    if (LightCount > 1.5) TL_AddLight(LPos1, LCol1, LDir1, P, N, V, shin, diff, spec);
    if (LightCount > 2.5) TL_AddLight(LPos2, LCol2, LDir2, P, N, V, shin, diff, spec);
    if (LightCount > 3.5) TL_AddLight(LPos3, LCol3, LDir3, P, N, V, shin, diff, spec);
}

// Hemisphere ambient multiplier (world Z is up).
float3 TL_Hemi(float3 N)
{
    return lerp(AmbGround, AmbSky, N.z * 0.5 + 0.5);
}

// Tangent space (B, T, N rows as in ComputeTangentMatrix) to world space.
float3 TL_WorldNormal(float3 nts, float3 Nw, float3 Tw)
{
    Nw = normalize(Nw);
    Tw = normalize(Tw);
    float3 Bw = cross(Nw, Tw);
    return normalize(nts.x * Bw + nts.y * Tw + nts.z * Nw);
}

// Exponential fog; density 0 returns the colour unchanged.
float3 TL_Fog(float3 color, float3 P, float3 eye)
{
    if (FogParams.x > 0.0)
    {
        float f = 1.0 - exp(-FogParams.x * max(length(P - eye) - FogParams.y, 0.0));
        color = lerp(color, FogColor, f);
    }
    return color;
}

#endif
