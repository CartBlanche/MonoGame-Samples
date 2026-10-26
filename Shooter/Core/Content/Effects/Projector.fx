// "Projector/Multiply" (Assets/FPS/Art/Shaders/ProjectorMultiply.shader) for MonoGame.
// Draws receiver geometry (the projector's volume of influence is supplied by the caller, normally the scene meshes inside the
// projector frustum) with  Blend DstColor Zero  and a projected cookie:
//   texS = tex2Dproj(ShadowTex, uvShadow); texS.a = 1 - texS.a;
//   texF = tex2Dproj(FalloffTex, uvFalloff);
//   result = lerp(float4(1,1,1,0), texS, texF.a)
// ProjectorMatrix  = World-to-cookie  (unity_Projector):   world -> projector clip, remapped to 0..1
// FalloffMatrix    = World-to-falloff (unity_ProjectorClip)
// NOTE: URP does not render the legacy Projector component, so the Unity baseline shows no projected blobs.

#include "Common.fxh"

float4x4 World;
float4x4 ViewProjection;
float4x4 ProjectorMatrix;
float4x4 FalloffMatrix;

DECLARE_TEXTURE(ShadowTex, 0);
DECLARE_TEXTURE(FalloffTex, 1);

struct VSIn
{
    float4 Position : POSITION0;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float4 UvShadow : TEXCOORD0;
    float4 UvFalloff : TEXCOORD1;
};

VSOut VSProj(VSIn i)
{
    VSOut o;
    float4 posWS = mul(float4(i.Position.xyz, 1), World);
    o.Position = mul(posWS, ViewProjection);
    o.UvShadow = mul(posWS, ProjectorMatrix);
    o.UvFalloff = mul(posWS, FalloffMatrix);
    return o;
}

float4 PSProj(VSOut i) : SV_TARGET0
{
    // .z of both projections is not needed by tex2Dproj; reference it so MojoShader does not compact away a matrix column
    // (see MotionBlur.fx / Lighting.fxh notes on MGFX matrix parameters).
    float zGuard = max(abs(i.UvShadow.z), abs(i.UvFalloff.z));
    float4 texS = tex2Dproj(ShadowTexSampler, i.UvShadow);
    texS.a = 1.0 - texS.a;
    float4 texF = tex2Dproj(FalloffTexSampler, i.UvFalloff);
    float4 result = lerp(float4(1, 1, 1, 0), texS, texF.a);
    return (zGuard < 1.0e9) ? result : float4(1, 1, 1, 0);
}

TECHNIQUE(ProjectorMultiply, VSProj, PSProj);
