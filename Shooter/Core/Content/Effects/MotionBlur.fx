// URP CameraMotionBlur (camera-only, "Low" quality = 2 iterations / 4 taps). Velocity is rebuilt from the depth prepass:
//   world = CameraPosition + (TopLeft + uv.x * XExtent + uv.y * YExtent) * eyeDepth   (world-space ray per unit eye depth)
//   velocity = prevNDC - curNDC (y flipped for top-left uv), clamped to Clamp, scaled by Intensity.
// Slots: 0 SourceTex (HDR colour, point/clamp), 1 DepthTex (rgb view normal, a linear eye depth; <= 0 => ignore / viewmodel).

#include "PostCommon.fxh"

float4x4 ViewProjection;
float4x4 PreviousViewProjection;
float3 CameraPosition;
float3 RayTopLeft;
float3 RayXExtent;
float3 RayYExtent;
float  Intensity = 0.35;
float  VelocityClamp = 0.05;
float2 SourceSize;

DECLARE_TEXTURE(SourceTex, 0);
DECLARE_TEXTURE(DepthTex, 1);

float InterleavedGradientNoise(float2 pixCoord, float frameCount)
{
    pixCoord += frameCount * float2(2.083, 4.867);
    return frac(52.9829189 * frac(dot(pixCoord, float2(0.06711056, 0.00583715))));
}

float2 ClampVelocity(float2 velocity, float maxVelocity)
{
    float len = length(velocity);
    return (len > 0.0) ? min(len, maxVelocity) * (velocity * (1.0 / len)) : float2(0, 0);
}

float4 GatherSample(float sampleNumber, float2 velocity, float invSampleCount, float2 centerUV, float randomVal, float velocitySign)
{
    float offsetLength = (sampleNumber + 0.5) + (velocitySign * (randomVal - 0.5));
    float2 sampleUV = centerUV + (offsetLength * invSampleCount) * velocity * velocitySign;
    return tex2Dlod(SourceTexSampler, float4(sampleUV, 0, 0));
}

float4 PSMotionBlur(PostVSOut i) : SV_TARGET0
{
    float2 uv = i.UV;
    float depth = tex2Dlod(DepthTexSampler, float4(uv, 0, 0)).a;
    float2 velocity = float2(0, 0);
    if (depth > 0.0)
    {
        float3 ray = RayTopLeft + RayXExtent * uv.x + RayYExtent * uv.y;
        float4 world = float4(CameraPosition + ray * depth, 1);
        float4 prevClip = mul(world, PreviousViewProjection);
        float4 curClip = mul(world, ViewProjection);
        float2 prevCS = prevClip.xy / prevClip.w;
        float2 curCS = curClip.xy / curClip.w;
        velocity = prevCS - curCS;
        velocity.y = -velocity.y;
        velocity = ClampVelocity(velocity, VelocityClamp) * Intensity;
        // Touch .z of both matrices' results: MGFX/MojoShader compacts away unused matrix columns (registers) while the
        // CPU side writes full 4x4 matrices, which corrupts the constant layout. Data dependent, never true.
        float zGuard = max(abs(prevClip.z), abs(curClip.z));
        velocity *= (zGuard < 1.0e9) ? 1.0 : 0.0;
    }

    float randomVal = InterleavedGradientNoise(uv * SourceSize, 0);
    const int iterations = 2;
    float invSampleCount = 1.0 / (iterations * 2.0);
    float4 color = 0;
    [unroll] for (int k = 0; k < iterations; k++)
    {
        color += GatherSample(k, velocity, invSampleCount, uv, randomVal, -1.0);
        color += GatherSample(k, velocity, invSampleCount, uv, randomVal, 1.0);
    }
    return float4(color.xyz * invSampleCount, 1);
}

TECHNIQUE(MotionBlur, PostVS, PSMotionBlur);
