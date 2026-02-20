#include "Macros.fxh"
string description = "Post screen shader for shadow blurring";

// Blur post processing effect.
// ScreenAdvancedBlur : 2 pass blur filter (horizontal and vertical) for ps11
// ScreenAdvancedBlur20 : 2 pass blur filter (horizontal and vertical) for ps20

BEGIN_CONSTANTS
// This script is only used for FX Composer, most values here
// are treated as constants by the application anyway.
// Values starting with an upper letter are constants.
float Script : STANDARDSGLOBAL
<
    string UIWidget = "none";
    string ScriptClass = "scene";
    string ScriptOrder = "postprocess";
    string ScriptOutput = "color";

    // We just call a script in the main technique.
    string Script = "Technique=ScreenAdvancedBlur20;";
> = 1.0;

const float4 ClearColor : DIFFUSE = { 0.0f, 0.0f, 0.0f, 1.0f};
const float ClearDepth = 1.0f;

// Render-to-Texture stuff
float2 windowSize : VIEWPORTPIXELSIZE;

const float BlurWidth = 1.25f;

// For ps_2_0 use only half the blur width
// because we cover a range twice as big.
// Update: For shadows 2.0 looks much better and smoother :)
// ps_1_1 can't archive that effect with just 4 samples.
const float BlurWidth20 = 1.5f;

// 8 Weights for ps_2_0
const float Weights8[8] =
{
	// more strength to middle to reduce effect of lighten up
	// shadowed areas due mixing and bluring!
	0.035,
	0.09,
	0.125,
	0.25,
	0.25,
	0.125,
	0.09,
	0.035,
};
END_CONSTANTS

BEGIN_DECLARE_TEXTURE_TARGET (sceneMap, RENDERCOLORTARGET)
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
	MIPFILTER = None;
	MINFILTER = Linear;
	MAGFILTER = Linear;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(blurMap, RENDERCOLORTARGET)
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
	MIPFILTER = None;
	MINFILTER = Linear;
	MAGFILTER = Linear;
END_DECLARE_TEXTURE;

//-----------------------------------------------------------

struct VB_OutputPos8TexCoords
{
    float4 pos         : SV_POSITION;
    float2 texCoord[7] : TEXCOORD0;
};

// generate texcoords for avanced blur
VB_OutputPos8TexCoords _VS_AdvancedBlur20(
    float4 pos      : POSITION,
    float2 texCoord : TEXCOORD0,
    uniform float2 dir)
{
    VB_OutputPos8TexCoords Out = (VB_OutputPos8TexCoords)0;
    Out.pos = pos;
    float2 texelSize = 1.0 / windowSize;
    float2 s = texCoord - texelSize*(7-1)*0.5*dir*BlurWidth20 + texelSize*0.5;
    for(int i=0; i<7; i++)
    {
        Out.texCoord[i] = s + texelSize*i*dir*BlurWidth20;
    }
    return Out;
}

VB_OutputPos8TexCoords VS_AdvancedBlur20Vertical(
	float4 pos      : POSITION,
	float2 texCoord : TEXCOORD0)
{
	return _VS_AdvancedBlur20(pos, texCoord, float2 (0, 1));
}

VB_OutputPos8TexCoords VS_AdvancedBlur20Horizontal(
	float4 pos      : POSITION,
	float2 texCoord : TEXCOORD0)
{
	return _VS_AdvancedBlur20(pos, texCoord, float2 (1, 0));
}

float4 PS_AdvancedBlur20Scene(
	VB_OutputPos8TexCoords In) : SV_TARGET
{
	float4 ret = 0;
	// This loop will be unrolled by the compiler
	for (int i = 0; i<7; i++)
	{
		float4 col = SAMPLE_TEXTURE(sceneMap, In.texCoord[i]);
		ret += col * Weights8[i];
	}
	return ret;
}

float4 PS_AdvancedBlur20Blur(
	VB_OutputPos8TexCoords In) : SV_TARGET
{
	float4 ret = 0;
	// This loop will be unrolled by the compiler
	for (int i = 0; i<7; i++)
	{
		float4 col = SAMPLE_TEXTURE(blurMap, In.texCoord[i]);
		ret += col * Weights8[i];
	}
	return ret;
}

BEGIN_TECHNIQUE(ScreenAdvancedBlur20)
	BEGIN_PASS(AdvancedBlurHorizontal)
		SHADERS(VS_AdvancedBlur20Horizontal, PS_AdvancedBlur20Scene)
	END_PASS
	BEGIN_PASS(AdvancedBlurVertical)
		SHADERS(VS_AdvancedBlur20Vertical, PS_AdvancedBlur20Blur)
	END_PASS
END_TECHNIQUE

