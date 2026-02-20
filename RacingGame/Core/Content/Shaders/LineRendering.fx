#include "Macros.fxh"
string description = "Line rendering helper shader for XNA";

BEGIN_CONSTANTS

// Default variables, supported by the engine
float4x4 worldViewProj : WorldViewProjection;

END_CONSTANTS

struct VertexInput
{
    float3 pos   : POSITION;
    float4 color : COLOR0;
};

struct VertexOutput 
{
   float4 pos   : SV_POSITION;
   float4 color : COLOR0;
};

VertexOutput LineRenderingVS(VertexInput In)
{
    VertexOutput Out;
    
    // Transform position
    Out.pos = mul(float4(In.pos, 1), worldViewProj);
    Out.color = In.color;

    // And pass everything to the pixel shader
    return Out;
}

float4 LineRenderingPS(VertexOutput In) : SV_TARGET
{
    return In.color;
}

VertexOutput LineRendering2DVS(VertexInput In)
{
    VertexOutput Out;
    
    // Transform position (just pass over)
    Out.pos = float4(In.pos, 1);
    Out.color = In.color;

    // And pass everything to the pixel shader
    return Out;
}

float4 LineRendering2DPS(VertexOutput In) : SV_TARGET
{
    return In.color;
}

TECHNIQUE (LineRendering3D, LineRenderingVS, LineRenderingPS)
TECHNIQUE (LineRendering2D, LineRendering2DVS, LineRendering2DPS)
