// Unity "Skybox/Panoramic" (lat-long, 360 degrees, no mirror) for MonoGame. Full-screen triangle, no depth.
// Maths per ASSET-CONVENTIONS.md section 3: view dir in UNITY space d = (x, y, -z) of the MonoGame direction,
// latitude = acos(d.y), longitude = atan2(d.z, d.x), u = 0.5 - longitude / 2pi, v(image top = 0) = latitude / pi.
// Output = sky texture (sRGB decoded) * tint.rgb * unity_ColorSpaceDouble (4.5948 in linear colour space) * exposure.

#include "Common.fxh"

float4x4 InverseViewProjectionRotation; // inverse of (View without translation * Projection)
float4   SkyTint = float4(1, 1, 1, 0.5); // linear
float    SkyExposure = 0.6;
float    SkyRotation = 0; // degrees about Unity Y
float    SkySrgb = 1;     // 1 => decode the panorama as sRGB

DECLARE_TEXTURE(SkyMap, 0);

struct VSIn
{
    float4 Position : POSITION0;
    float2 UV       : TEXCOORD0;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float2 Ndc      : TEXCOORD0;
};

VSOut VSSky(VSIn i)
{
    VSOut o;
    o.Position = float4(i.Position.xy, 1, 1);
    o.Ndc = i.Position.xy;
    return o;
}

float4 PSSky(VSOut i) : SV_TARGET0
{
    float4 h = mul(float4(i.Ndc, 1, 1), InverseViewProjectionRotation);
    float3 dirMG = normalize(h.xyz / h.w);
    float3 d = float3(dirMG.x, dirMG.y, -dirMG.z); // Unity space

    float a = SkyRotation * PI / 180.0;
    float sa = sin(a);
    float ca = cos(a);
    // RotateAroundYInDegrees: m = float2x2(cosa, -sina, sina, cosa); xz = mul(m, xz)
    d = float3(ca * d.x - sa * d.z, d.y, sa * d.x + ca * d.z);

    float latitude = acos(clamp(d.y, -1.0, 1.0));
    float longitude = atan2(d.z, d.x);
    float2 uv = float2(0.5 - longitude / (2.0 * PI), latitude / PI);

    float3 c = tex2Dlod(SkyMapSampler, float4(uv, 0, 0)).rgb;
    c = lerp(c, SRGBToLinear3(c), SkySrgb);
    c = c * SkyTint.rgb * 4.59479380 * SkyExposure;
    return float4(min(c, 60000.0), 1);
}

TECHNIQUE(Sky, VSSky, PSSky);
