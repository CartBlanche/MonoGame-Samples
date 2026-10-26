// Shared full-screen vertex shader for the post-processing effects.
// The quad is drawn with positions in NDC (-1..1) and uv with (0,0) at the top-left (MonoGame render target convention).
#ifndef SHOOTER_POSTCOMMON_FXH
#define SHOOTER_POSTCOMMON_FXH

#include "Common.fxh"

struct PostVSIn
{
    float4 Position : POSITION0;
    float2 UV       : TEXCOORD0;
};

struct PostVSOut
{
    float4 Position : SV_POSITION;
    float2 UV       : TEXCOORD0;
};

PostVSOut PostVS(PostVSIn i)
{
    PostVSOut o;
    o.Position = float4(i.Position.xy, 0, 1);
    o.UV = i.UV;
    return o;
}

#endif
