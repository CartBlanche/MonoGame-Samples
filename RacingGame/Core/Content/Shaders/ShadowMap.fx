#include "Macros.fxh"
string description = "Generate and use a shadow map with a directional light";

BEGIN_CONSTANTS
float4x4 worldViewProj         : WorldViewProjection;
float4x4 world                 : World;
float3 viewInverse           : ViewInverse; //not used

// Extra values for this shader
// Transformation matrix for converting world pos
// to texture coordinates of the shadow map.
float4x4 shadowTexTransform;
// worldViewProj of the light projection
float4x4 worldViewProjLight : WorldViewProjection;

// Hand adjusted near and far plane for better percision.
const float nearPlane = 2.0f;
const float farPlane = 8.0f;
// Depth bias, controls how much we remove from the depth
// to fix depth checking artifacts. For ps_1_1 this should
// be a very high value (0.01f), for ps_2_0 it can be very low.
const float depthBias = 0.0025f;
// Substract a very low value from shadow map depth to
// move everything a little closer to the camera.
// This is done when the shadow map is rendered before any
// of the depth checking happens, should be a very small value.
const float shadowMapDepthBias = -0.0005f;

// Color for shadowed areas, should be black too, but need
// some alpha value (e.g. 0.5) for blending the color to black.
const float4 ShadowColor = {0.25f, 0.26f, 0.27f, 1.0f};

const float3 lightDir : Direction = {1.0f, -1.0f, 1.0f};

// Shadown Map size
const float2 shadowMapTexelSize = float2(1.0f/1024.0f, 1.0f/1024);

// Poison filter pseudo random filter positions for PCF with 10 samples
const float2 FilterTaps[10] =
{
    // First test, still the best.
    {-0.84052f, -0.073954f},
    {-0.326235f, -0.40583f},
    {-0.698464f, 0.457259f},
    {-0.203356f, 0.6205847f},
    {0.96345f, -0.194353f},
    {0.473434f, -0.480026f},
    {0.519454f, 0.767034f},
    {0.185461f, -0.8945231f},
    {0.507351f, 0.064963f},
    {-0.321932f, 0.5954349f}
};
END_CONSTANTS

BEGIN_DECLARE_TEXTURE_TARGET (shadowDistanceFadeoutTexture, Diffuse)
    AddressU  = Wrap;
    AddressV  = Wrap;
    AddressW  = Wrap;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
END_DECLARE_TEXTURE;

// Vertex input structure (used for ALL techniques here!)
struct VertexInput
{
    // We just use the position here, nothing else is required.
    float3 pos      : POSITION;
};

// Struct used for passing data from VS_GenerateShadowMap to ps
struct VB_GenerateShadowMap
{
    float4 pos      : SV_POSITION;
    // Ps 1.1 will use color, ps 2.0 will use TexCoord.
    // This way we get the most percision in each ps model.
    float4 depth    : SV_TARGET0;
};

// Helper functions
float4 TransformPosition(float3 pos)
{
    return mul(float4(pos.xyz, 1), worldViewProj);
}

float3 GetWorldPos(float3 pos)
{
    return mul(float4(pos, 1), world).xyz;
}

float3 GetCameraPos()
{
    return viewInverse;
}

float3 CalcNormalVector(float3 nor)
{
    return normalize(mul(nor, (float3x3)world));
}

//-------------------------------------------------------------------

// Struct used for passing data from VS_GenerateShadowMap to ps
struct VB_GenerateShadowMap20
{
    float4 pos      : SV_POSITION;
    float2 depth    : TEXCOORD0;
};

// Vertex shader function
VB_GenerateShadowMap20 VS_GenerateShadowMap20(VertexInput In)
{
    VB_GenerateShadowMap20 Out = (VB_GenerateShadowMap20) 0;
    Out.pos = TransformPosition(In.pos);

    // Use farPlane/10 for the internal near plane, we don't have any
    // objects near the light, use this to get much better percision!
    float internalNearPlane = farPlane / 10;

    // Linear depth calculation instead of normal depth calculation.
    Out.depth = float2(
        (Out.pos.z - internalNearPlane),
        (farPlane - internalNearPlane));

    return Out;
}

// Pixel shader function
float4 PS_GenerateShadowMap20(VB_GenerateShadowMap20 In) : SV_TARGET
{
    // Just set the interpolated depth value.
    return (In.depth.x/In.depth.y) + shadowMapDepthBias;
}

BEGIN_TECHNIQUE(GenerateShadowMap20)
    BEGIN_PASS(P0)
        CullMode = None;
        SHADERS(VS_GenerateShadowMap20,PS_GenerateShadowMap20)
    END_PASS
END_TECHNIQUE

//-------------------------------------------------------------------

BEGIN_DECLARE_TEXTURE_TARGET (ShadowMap, Diffuse)
    AddressU  = Clamp;
    AddressV  = Clamp;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
END_DECLARE_TEXTURE;

//-------------------------------------------------------------------

// Vertex shader output structure for using the shadow map
struct VB_UseShadowMap20
{
    float4 pos            : SV_POSITION;
    float4 shadowTexCoord : TEXCOORD0;
    float2 depth          : TEXCOORD1;
};

VB_UseShadowMap20 VS_UseShadowMap20(VertexInput In)
{
    VB_UseShadowMap20 Out = (VB_UseShadowMap20)0;
    // Convert to float4 pos, used several times here.
    float4 pos = float4(In.pos, 1);
    Out.pos = mul(pos, worldViewProj);

    // Transform model-space vertex position to light-space:
    float4 shadowTexPos =
        mul(pos, shadowTexTransform);
    // Set first texture coordinates
    Out.shadowTexCoord = float4(
        shadowTexPos.x,
        shadowTexPos.y,
        0.0f,
        shadowTexPos.w);

    // Get depth of this point relative to the light position
    float4 depthPos = mul(pos, worldViewProjLight);
    
    // Use farPlane/10 for the internal near plane, we don't have any
    // objects near the light, use this to get much better percision!
    float internalNearPlane = farPlane / 10;
    
    // Same linear depth calculation as above.
    // Also substract depthBias to fix shadow mapping artifacts.
    Out.depth = float2(
        (depthPos.z - internalNearPlane),
        (farPlane - internalNearPlane));

    return Out;
}

// Advanced pixel shader for shadow depth calculations in ps 2.0.
// However this shader looks blocky like PCF3x3 and should be smoothend
// out by a good post screen blur filter. This advanced shader does a good
// job faking the penumbra and can look very good when adjusted carefully.
float4 PS_UseShadowMap20(VB_UseShadowMap20 In) : SV_TARGET
{
    float depth = (In.depth.x/In.depth.y) - depthBias;

    float2 shadowTex =
        (In.shadowTexCoord.xy / In.shadowTexCoord.w) -
        shadowMapTexelSize / 2.0f;

    float resultDepth = 0;
    for (int i=0; i<10; i++)
        resultDepth += depth > SAMPLE_TEXTURE(ShadowMap,
            shadowTex+FilterTaps[i]*shadowMapTexelSize).r ? 1.0f/10.0f : 0.0f;
            
    // Simulate texture border addressing mode on Windows
    if (shadowTex.x < 0 || shadowTex.y < 0 ||
        shadowTex.x > 1 || shadowTex.y > 1)
    {
        resultDepth = 0;
    }
            
    // Multiply the result by the shadowDistanceFadeoutTexture, which
    // fades shadows in and out at the max. shadow distances
    resultDepth *= SAMPLE_TEXTURE(shadowDistanceFadeoutTexture, shadowTex).r;

    // We can skip this if its too far away anway (else very far away landscape
    // parts will be darkenend)
    if (depth > 1)
        return 1;
    else
        // And apply
        return lerp(1, ShadowColor, resultDepth);
}

TECHNIQUE (UseShadowMap20, VS_UseShadowMap20, PS_UseShadowMap20);
