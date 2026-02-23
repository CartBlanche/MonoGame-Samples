#include "Macros.fxh"
// Simple shader for RacingGame
BEGIN_CONSTANTS
float4x4 worldViewProj : WorldViewProjection;
float4x4 world : World;
float3 viewInverse : ViewInverse;

const static float3 lightDir : Direction
<
    string Object = "DirectionalLight";
    string Space = "World";
> = { 1, 0, 0 };

const static float4 ambientColor : Ambient = { 0.2f, 0.2f, 0.2f, 1.0f };
const static float4 diffuseColor : Diffuse = { 0.5f, 0.5f, 0.5f, 1.0f };
const static float4 specularColor : Specular = { 1.0, 1.0, 1.0f, 1.0f };
const static float specularPower : SpecularPower = 24.0f;

// Special shader for car rendering, which allows to change the car color!
float4 shadowCarColor
<
    string UIName = "Shadow Car Color";
    string Space = "material";
> = {1.0f, 1.0f, 1.0f, 0.125f};

END_CONSTANTS

BEGIN_DECLARE_TEXTURE(diffuseTexture, 0)
    AddressU  = Wrap;
    AddressV  = Wrap;
    AddressW  = Wrap;
    MinFilter=linear;
    MagFilter=linear;
    MipFilter=linear;
END_DECLARE_TEXTURE;

// Vertex input structure (used for ALL techniques here!)
struct VertexInput
{
    float3 pos      : POSITION;
    float2 texCoord : TEXCOORD0;
    float3 normal   : NORMAL;
    float3 tangent  : TANGENT;
};

// Vertex output structure
struct VertexOutput_SpecularPerPixel
{
    float4 pos      : SV_POSITION;
    float2 texCoord : TEXCOORD0;
    float3 normal   : TEXCOORD1;
    float3 halfVec  : TEXCOORD2;
};

// Common functions
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

// Vertex output structure
struct VertexOutput_Diffuse
{
    float4 pos      : SV_POSITION;
    float2 texCoord : TEXCOORD0;
    float3 normal   : TEXCOORD1;
};

// Very simple diffuse mapping shader
VertexOutput_Diffuse VS_Diffuse(VertexInput In)
{
    VertexOutput_Diffuse Out = (VertexOutput_Diffuse)0;      
    Out.pos = TransformPosition(In.pos);
    Out.texCoord = In.texCoord;

    // Calc normal vector
    Out.normal = 0.5 + 0.5 * CalcNormalVector(In.normal);
    
    // Rest of the calculation is done in pixel shader
    return Out;
}

// Pixel shader
float4 PS_Diffuse(VertexOutput_Diffuse In) : SV_TARGET
{
    float4 textureColor = SAMPLE_TEXTURE(diffuseTexture, In.texCoord);
    // Convert colors back to vectors. Without normalization it is
    // a bit faster (2 instructions less), but not as correct!
    float3 normal = 2.0 * (saturate(In.normal)-0.5);

    // Diffuse factor
    float diff = saturate(dot(normal, lightDir));

    // Output the color
    float4 diffAmbColor = ambientColor + diff * diffuseColor;
    return textureColor * diffAmbColor;
}

TECHNIQUE(Diffuse20, VS_Diffuse, PS_Diffuse)

//-------------------------------------

// Vertex shader
VertexOutput_SpecularPerPixel VS_SpecularPerPixel20(VertexInput In)
{
    VertexOutput_SpecularPerPixel Out = (VertexOutput_SpecularPerPixel)0;
    float4 pos = float4(In.pos, 1); 
    Out.pos = mul(pos, worldViewProj);
    Out.texCoord = In.texCoord;
    Out.normal = mul(In.normal, world);
    // Eye pos
    float3 eyePos = viewInverse;
    // World pos
    float3 worldPos = mul(pos, world);
    // Eye vector
    float3 eyeVector = normalize(eyePos-worldPos);
    // Half vector
    Out.halfVec = normalize(eyeVector+lightDir);
    
    return Out;
}

// Pixel shader
float4 PS_SpecularPerPixel20(VertexOutput_SpecularPerPixel In) : SV_TARGET
{
    float4 textureColor = SAMPLE_TEXTURE(diffuseTexture, In.texCoord);
    float3 normal = normalize(In.normal);
    float brightness = dot(normal, lightDir);
	float dotp = dot(normal, In.halfVec);
    float specular = pow(dotp, specularPower);
    return textureColor *
        (ambientColor +
        brightness * diffuseColor) +
        specular * specularColor;
}

TECHNIQUE(SpecularPerPixel20, VS_SpecularPerPixel20, PS_SpecularPerPixel20)

//---------------------------------------------------

// vertex shader output structure
struct VertexOutput_ShadowCar20
{
    float4 pos          : SV_POSITION;
    float2 texCoord     : TEXCOORD0;
};

// Vertex shader function
float4 VS_ShadowCar20(VertexInput In) : SV_POSITION
{
    return TransformPosition(In.pos);
}

// Pixel shader function
float4 PS_ShadowCar20() : SV_TARGET
{
    return shadowCarColor;
}

BEGIN_TECHNIQUE(ShadowCar20)
	BEGIN_PASS(P0)
        ZWriteEnable = false;
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = One;
		SHADERS(VS_ShadowCar20, PS_ShadowCar20)
	END_PASS
END_TECHNIQUE
