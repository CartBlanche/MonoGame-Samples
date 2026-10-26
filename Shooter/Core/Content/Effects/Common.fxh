// Shared helpers for the Shooter effects (DesktopGL, ps_3_0 / vs_3_0 via Macros.fxh).
#ifndef SHOOTER_COMMON_FXH
#define SHOOTER_COMMON_FXH

#include "Macros.fxh"

#define PI 3.14159265359
#define MAX_BONES 48

// Exact sRGB -> linear (Unity's SRGBToLinear, non-fast variant).
float3 SRGBToLinear3(float3 c)
{
    float3 lo = c / 12.92;
    float3 hi = pow(max((c + 0.055) / 1.055, 0.0), 2.4);
    return (c <= 0.04045) ? lo : hi;
}

float3 LinearToSRGB3(float3 c)
{
    c = max(c, 0.0);
    float3 lo = c * 12.92;
    float3 hi = 1.055 * pow(c, 1.0 / 2.4) - 0.055;
    return (c <= 0.0031308) ? lo : hi;
}

float Luma(float3 c)
{
    return dot(c, float3(0.2126729, 0.7151522, 0.0721750));
}

// Screen uv (top-left origin, MonoGame render-target convention) from a clip-space position.
float2 ClipToScreenUV(float4 clip)
{
    float2 ndc = clip.xy / clip.w;
    return float2(ndc.x * 0.5 + 0.5, 0.5 - ndc.y * 0.5);
}

#endif
