#include "Macros.fxh"
string description = "Normal mapping shaders for RacingGame";

// Shader techniques in this file, all shaders work with vs/ps 1.1, shaders not
// working with 1.1 have names with 20 at the end:
// Diffuse           : Full vertex ambient+diffuse+specular lighting
// Diffuse20         : Same for ps20, only required for 3DS max to show shader!
//
// Specular           : Full vertex ambient+diffuse+specular lighting
// Specular20         : Nicer effect for ps20, also required for 3DS max to show shader!
//
// DiffuseSpecular    : Same as specular, but adding the specular component
//                        to diffuse (per vertex)
// DiffuseSpecular20  : Nicer effect for ps20, also required for 3DS max to show shader!
BEGIN_CONSTANTS
float4x4 worldViewProj    : WorldViewProjection;
float4x4 world            : World;
float3 viewInverse      : ViewInverse;

const static float3 lightDir : Direction
<
    string UIName = "Light Direction";
    string Object = "DirectionalLight";
    string Space = "World";
> = {-0.65f, 0.65f, -0.39f}; // Normalized by app. FxComposer still uses inverted stuff

// The ambient, diffuse and specular colors are pre-multiplied with the light color!
const static float4 ambientColor : Ambient
<
    string UIName = "Ambient Color";
    string Space = "material";
> = {0.1f, 0.1f, 0.1f, 1.0f};

const static float4 diffuseColor : Diffuse
<
    string UIName = "Diffuse Color";
    string Space = "material";
> = {1.0f, 1.0f, 1.0f, 1.0f};

const static float4 specularColor : Specular
<
    string UIName = "Specular Color";
    string Space = "material";
> = {1.0f, 1.0f, 1.0f, 1.0f};

const static float shininess : SpecularPower
<
    string UIName = "Specular Power";
    string UIWidget = "slider";
    float UIMin = 1.0;
    float UIMax = 128.0;
    float UIStep = 1.0;
> = 16.0;

float DetailFactor = 24;
END_CONSTANTS

BEGIN_DECLARE_TEXTURE_TARGET(diffuseTexture, Diffuse)
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(normalTexture, Diffuse)
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(reflectionCubeTexture, Environment)
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(detailTexture, Diffuse)
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_CUBE_TARGET(NormalizeCubeTexture, Environment)
AddressU = Wrap;
AddressV = Wrap;
AddressW = Wrap;
MinFilter = Linear;
MagFilter = Linear;
MipFilter = None;
END_DECLARE_TEXTURE;

//----------------------------------------------------

// Vertex input structure (used for ALL techniques here!)
struct VertexInput
{
    float3 pos      : POSITION0;
    float2 texCoord : TEXCOORD0;
    float3 normal   : NORMAL;
    float3 tangent  : TANGENT;
};

// vertex shader output structure
struct VertexOutput
{
    float4 pos          : SV_POSITION;
    float2 diffTexCoord : TEXCOORD0;
    float2 normTexCoord : TEXCOORD1;
    float3 lightVec     : COLOR0;
};

//----------------------------------------------------

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

// Get light direction
float3 GetLightDir()
{
    return lightDir;
}
    
float3x3 ComputeTangentMatrix(float3 tangent, float3 normal)
{
    // Compute the 3x3 tranform from tangent space to object space
    float3x3 worldToTangentSpace;
    worldToTangentSpace[0] =
        //left handed: mul(cross(tangent, normal), world);
        mul(cross(normal, tangent), world);
    worldToTangentSpace[1] = mul(tangent, world);
    worldToTangentSpace[2] = mul(normal, world);
    return worldToTangentSpace;
}

//----------------------------------------------------

// Vertex shader function
VertexOutput VS_Diffuse(VertexInput In)
{
    VertexOutput Out = (VertexOutput) 0; 
    Out.pos = TransformPosition(In.pos);
    // Duplicate texture coordinates for diffuse and normal maps
    Out.diffTexCoord = In.texCoord;
    Out.normTexCoord = In.texCoord;

    // Compute the 3x3 tranform from tangent space to object space
    float3x3 worldToTangentSpace =
        ComputeTangentMatrix(In.tangent, In.normal);

    // Transform light vector and pass it as a color (clamped from 0 to 1)
    Out.lightVec = 0.5 + 0.5 *
        normalize(mul(worldToTangentSpace, GetLightDir()));

    // And pass everything to the pixel shader
    return Out;
}

// Pixel shader function, only used to ps2.0 because of .agb
float4 PS_Diffuse(VertexOutput In) : SV_TARGET
{
    // Grab texture data
    float4 diffusePixel = SAMPLE_TEXTURE(diffuseTexture, In.diffTexCoord);
    float3 normalPixel = SAMPLE_TEXTURE(normalTexture, In.normTexCoord).agb;
    float3 normalVector =
        (2.0 * normalPixel) - 1.0;
    // Normalize normal to fix blocky errors
    normalVector = normalize(normalVector);

    // Unpack the light vector to -1 - 1
    float3 lightVector =
        (2.0 * In.lightVec) - 1.0;

    // Compute the angle to the light
    float bump = saturate(dot(normalVector, lightVector));
    
    float4 ambDiffColor = ambientColor + bump * diffuseColor;
    return diffusePixel * ambDiffColor;
}

// Techniques
TECHNIQUE(Diffuse20, VS_Diffuse, PS_Diffuse)

// Pixel shader function, only used to ps2.0 because of .agb
float4 PS_Diffuse_Transparent(VertexOutput In) : SV_TARGET
{
    // Grab texture data
    float4 diffusePixel = SAMPLE_TEXTURE(diffuseTexture, In.diffTexCoord);
    //return diffuseTexture;
    float3 normalPixel = SAMPLE_TEXTURE(normalTexture, In.normTexCoord).agb;
    float3 normalVector =
        (2.0 * normalPixel) - 1.0;
    // Normalize normal to fix blocky errors
    normalVector = normalize(normalVector);

    // Unpack the light vector to -1 - 1
    float3 lightVector =
        (2.0 * In.lightVec) - 1.0;

    // Compute the angle to the light
    float bump = saturate(dot(normalVector, lightVector));
    
    float4 ambDiffColor = ambientColor + bump * diffuseColor;
    ambDiffColor.a = 0.33f;
    return diffusePixel * ambDiffColor;
}

BEGIN_TECHNIQUE(Diffuse20Transparent)
	BEGIN_PASS(P0)
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		SHADERS(VS_Diffuse, PS_Diffuse_Transparent)
	END_PASS
END_TECHNIQUE

//------------------------------------------------

// vertex shader output structure (optimized for ps_1_1)
struct VertexOutput_Specular
{
    float4 pos          : SV_POSITION;
    float2 diffTexCoord : TEXCOORD0;
    float2 normTexCoord : TEXCOORD1;
    float3 viewVec      : TEXCOORD2;
    float3 lightVec     : TEXCOORD3;
    float3 lightVecDiv3 : COLOR0;
};

// Vertex shader function
VertexOutput_Specular VS_Specular(VertexInput In)
{
    VertexOutput_Specular Out = (VertexOutput_Specular) 0; 
    Out.pos = TransformPosition(In.pos);
    // Duplicate texture coordinates for diffuse and normal maps
    Out.diffTexCoord = In.texCoord;
    Out.normTexCoord = In.texCoord;

    // Compute the 3x3 tranform from tangent space to object space
    float3x3 worldToTangentSpace =
        ComputeTangentMatrix(In.tangent, In.normal);

    float3 worldEyePos = GetCameraPos();
    float3 worldVertPos = GetWorldPos(In.pos);

    // Transform light vector and pass it as a color (clamped from 0 to 1)
    // For ps_2_0 we don't need to clamp form 0 to 1
    float3 lightVec = normalize(mul(worldToTangentSpace, GetLightDir()));
    Out.lightVec = 0.5 + 0.5 * lightVec;
    Out.lightVecDiv3 = 0.5 + 0.5 * lightVec / 3;
    Out.viewVec = mul(worldToTangentSpace, worldEyePos - worldVertPos);

    // And pass everything to the pixel shader
    return Out;
}

// Techniques

//----------------------------------------

// vertex shader output structure
struct VertexOutput_Specular20
{
    float4 pos          : SV_POSITION;
    float2 diffTexCoord : TEXCOORD0;
    float2 normTexCoord : TEXCOORD1;
    float3 lightVec     : TEXCOORD2;
    float3 viewVec      : TEXCOORD3;
};

// Vertex shader function
VertexOutput_Specular20 VS_Specular20(VertexInput In)
{
    VertexOutput_Specular20 Out = (VertexOutput_Specular20) 0; 
    Out.pos = TransformPosition(In.pos);
    // Duplicate texture coordinates for diffuse and normal maps
    Out.diffTexCoord = In.texCoord;
    Out.normTexCoord = In.texCoord;

    // Compute the 3x3 tranform from tangent space to object space
    float3x3 worldToTangentSpace =
        ComputeTangentMatrix(In.tangent, In.normal);

    float3 worldEyePos = GetCameraPos();
    float3 worldVertPos = GetWorldPos(In.pos);

    // Transform light vector and pass it as a color (clamped from 0 to 1)
    // For ps_2_0 we don't need to clamp form 0 to 1
    Out.lightVec = normalize(mul(worldToTangentSpace, GetLightDir()));
    Out.viewVec = mul(worldToTangentSpace, worldEyePos - worldVertPos);

    // And pass everything to the pixel shader
    return Out;
}

// Pixel shader function
float4 PS_Specular20(VertexOutput_Specular20 In) : SV_TARGET
{
    // Grab texture data
    float4 diffusePixel = SAMPLE_TEXTURE(diffuseTexture, In.diffTexCoord);
    float3 normalVector = (2.0 * SAMPLE_TEXTURE(normalTexture, In.normTexCoord).agb) - 1.0;
    // Normalize normal to fix blocky errors
    normalVector = normalize(normalVector);

    // Additionally normalize the vectors
    float3 lightVector = In.lightVec;
    float3 viewVector = normalize(In.viewVec);
    // For ps_2_0 we don't need to unpack the vectors to -1 - 1

    // Compute the angle to the light
    float bump = saturate(dot(normalVector, lightVector));
    // Specular factor
    float3 reflect = normalize(2 * bump * normalVector - lightVector);
    float spec = pow(saturate(dot(reflect, viewVector)), shininess);

    float4 ambDiffColor = ambientColor + bump * diffuseColor;
    return diffusePixel * ambDiffColor +
        bump * spec * specularColor * diffusePixel.a;
}


TECHNIQUE(Specular20, VS_Specular20, PS_Specular20)

//----------------------------------------

// Techniques

//----------------------------------------

// Pixel shader function
float4 PS_DiffuseSpecular20(VertexOutput_Specular20 In) : SV_TARGET
{
    // Grab texture data
    float4 diffusePixel = SAMPLE_TEXTURE(diffuseTexture, In.diffTexCoord);
    float3 normalVector = (2.0 * SAMPLE_TEXTURE(normalTexture, In.normTexCoord).agb) - 1.0;
    // Normalize normal to fix blocky errors
    normalVector = normalize(normalVector);

    // Additionally normalize the vectors
    float3 lightVector = In.lightVec;
    float3 viewVector = normalize(In.viewVec);
    // For ps_2_0 we don't need to unpack the vectors to -1 - 1

    // Compute the angle to the light
    float bump = saturate(dot(normalVector, lightVector));
    // Specular factor
    float3 reflect = normalize(2 * bump * normalVector - lightVector);
    float spec = pow(saturate(dot(reflect, viewVector)), shininess);

    return diffusePixel * (ambientColor +
        bump * (diffuseColor + spec * specularColor));
}

TECHNIQUE(DiffuseSpecular20, VS_Specular20, PS_DiffuseSpecular20)

// ------------------------------

// ------------------------------

// vertex shader output structure
struct VertexOutput_SpecularWithReflection20
{
    float4 pos          : SV_POSITION;
    float2 texCoord     : TEXCOORD0;
    float3 lightVec     : TEXCOORD1;
    float3 viewVec      : TEXCOORD2;
    float3 cubeTexCoord : TEXCOORD3;
};

// Vertex shader function
VertexOutput_SpecularWithReflection20
    VS_SpecularWithReflection20(VertexInput In)
{
    VertexOutput_SpecularWithReflection20 Out =
        (VertexOutput_SpecularWithReflection20) 0;
    
    float4 worldVertPos = mul(float4(In.pos.xyz, 1), world);
    Out.pos = mul(float4(In.pos.xyz, 1), worldViewProj);
    
    // Copy texture coordinates for diffuse and normal maps
    Out.texCoord = In.texCoord;

    // Compute the 3x3 tranform from tangent space to object space
    float3x3 worldToTangentSpace =
        ComputeTangentMatrix(In.tangent, In.normal);

    float3 worldEyePos = GetCameraPos();

    // Transform light vector and pass it as a color (clamped from 0 to 1)
    // For ps_2_0 we don't need to clamp form 0 to 1
    Out.lightVec = normalize(mul(worldToTangentSpace, GetLightDir()));
    Out.viewVec = mul(worldToTangentSpace, worldEyePos - worldVertPos);

    float3 normal = CalcNormalVector(In.normal);
    float3 viewVec = normalize(worldEyePos - worldVertPos);
    float3 R = reflect(-viewVec, normal);
    Out.cubeTexCoord = R;
    
    // And pass everything to the pixel shader
    return Out;
}

// Pixel shader function
float4 PS_SpecularWithReflection20(VertexOutput_SpecularWithReflection20 In) : SV_TARGET
{
    // Grab texture data
    float4 diffusePixel = SAMPLE_TEXTURE(diffuseTexture, In.texCoord);
    float3 normalVector = (2.0 * SAMPLE_TEXTURE(normalTexture, In.texCoord).agb) - 1.0;
    // Normalize normal to fix blocky errors
    normalVector = normalize(normalVector);

    // Additionally normalize the vectors
    float3 lightVector = normalize(In.lightVec);
    float3 viewVector = normalize(In.viewVec);
    // Compute the angle to the light
    float bump = dot(normalVector, lightVector);
    // Specular factor
    float3 reflect = normalize(2 * bump * normalVector - lightVector);
    float spec = pow(saturate(dot(reflect, viewVector)), shininess);

    // Darken down bump factor on back faces
    float4 reflection = SAMPLE_CUBE(reflectionCubeTexture, In.cubeTexCoord);
    float3 ambDiffColor = ambientColor + bump * diffuseColor;
    float4 ret;
    ret.rgb = diffusePixel * ambDiffColor +
        bump * spec * specularColor * diffusePixel.a;
    // Apply color
    ret.a = diffusePixel.a * diffuseColor.a;
    return ret * (0.85f + reflection * 0.75f);
}

TECHNIQUE(SpecularWithReflection20, VS_SpecularWithReflection20, PS_SpecularWithReflection20)

// -----------------------------------

// vertex shader output structure
struct VertexOutput_Detail
{
    float4 pos            : SV_POSITION;
    float2 diffTexCoord   : TEXCOORD0;
    float2 normTexCoord   : TEXCOORD1;
    float2 detailTexCoord : TEXCOORD2;
    float3 lightVec       : COLOR0;
};

// Vertex shader function
VertexOutput_Detail VS_DiffuseWithDetail(VertexInput In)
{
    VertexOutput_Detail Out = (VertexOutput_Detail) 0; 
    Out.pos = TransformPosition(In.pos);
    // Duplicate texture coordinates for diffuse and normal maps
    Out.diffTexCoord = In.texCoord;
    Out.normTexCoord = In.texCoord;
    Out.detailTexCoord = In.texCoord * DetailFactor;

    // Compute the 3x3 tranform from tangent space to object space
    float3x3 worldToTangentSpace =
        ComputeTangentMatrix(In.tangent, In.normal);

    // Transform light vector and pass it as a color (clamped from 0 to 1)
    Out.lightVec = 0.5 + 0.5 *
        normalize(mul(worldToTangentSpace, GetLightDir()));

    // And pass everything to the pixel shader
    return Out;
}

// Pixel shader function, only used to ps2.0 because of .agb
float4 PS_DiffuseWithDetail(VertexOutput_Detail In) : SV_TARGET
{
    // Grab texture data
    float4 diffusePixel = SAMPLE_TEXTURE(diffuseTexture, In.diffTexCoord);
    //return diffuseTexture;
    float3 normalPixel = SAMPLE_TEXTURE(normalTexture, In.normTexCoord).agb;
    float3 normalVector = (2.0 * normalPixel) - 1.0;
    // Normalize normal to fix blocky errors
    normalVector = normalize(normalVector);
    
    // Detail texture
    float4 detailPixel = SAMPLE_TEXTURE(detailTexture, In.detailTexCoord);
    detailPixel = (2.0 * detailPixel);

    // Unpack the light vector to -1 - 1
    float3 lightVector = (2.0 * In.lightVec) - 1.0;

    // Compute the angle to the light
    float bump = saturate(dot(normalVector, lightVector));
    
    float4 ambDiffColor = ambientColor + bump * diffuseColor;
    return diffusePixel * ambDiffColor * detailPixel;
}

// Techniques
TECHNIQUE(DiffuseWithDetail20, VS_DiffuseWithDetail, PS_DiffuseWithDetail)
