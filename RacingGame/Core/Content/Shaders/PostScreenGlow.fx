#include "Macros.fxh"
string description = "Post screen shader for glowing with big radius";

// Glow/bloom post processing effect, adjusted for RacingGameManager.
// Parts are based on NVidias Post_bloom.fx, (c) NVIDIA Corporation 2004
// Also includes a border darken effect with help of screenBorderFadeout.dds.

// This script is only used for FX Composer, most values here
// are treated as constants by the application anyway.
// Values starting with an upper letter are constants.
BEGIN_CONSTANTS
float Script : STANDARDSGLOBAL
<
    string ScriptClass = "scene";
    string ScriptOrder = "postprocess";
    string ScriptOutput = "color";

    // We just call a script in the main technique.
    string Script = "Technique=ScreenGlow;";
> = 0.5;

const float DownsampleMultiplicator = 0.25f;
const float4 ClearColor : DIFFUSE = { 0.0f, 0.0f, 0.0f, 1.0f};
const float ClearDepth = 1.0f;

float GlowIntensity <
    string UIName = "Glow intensity";
    string UIWidget = "slider";
    float UIMin = 0.0f;
    float UIMax = 1.0f;
    float UIStep = 0.1f;
> = 0.7f;

// Only used in ps_2_0
float HighlightThreshold <
    string UIName = "Highlight threshold";
    string UIWidget = "slider";
    float UIMin = 0.0f;
    float UIMax = 1.0f;
    float UIStep = 0.1f;
> = 0.975f;

float HighlightIntensity <
    string UIName = "Highlight intensity";
    string UIWidget = "slider";
    float UIMin = 0.0f;
    float UIMax = 1.0f;
    float UIStep = 0.1f;
> = 0.4f;

float radialBlurScaleFactor <
    string UIName = "Radial Blur Scale Factor";
    string UIWidget = "slider";
    float UIMin = -0.1f;
    float UIMax = +0.1f;
    float UIStep = 0.0025f;
> = -0.004f;

// Render-to-Texture stuff
float2 windowSize : VIEWPORTPIXELSIZE;
const float downsampleScale = 0.25;

// blur filter weights
const half weights7[7] =
{
    0.05,
    0.1,
    0.2,
    0.3,
    0.2,
    0.1,
    0.05,
};  

// Blur Width is only used for ps_2_0, ps_1_1 is optimized!
float BlurWidth <
    string UIName = "Blur width";
    string UIWidget = "slider";
    float UIMin = 0.0f;
    float UIMax = 10.0f;
    float UIStep = 0.5f;
> = 8.0f;
END_CONSTANTS

BEGIN_DECLARE_TEXTURE_TARGET(sceneMap, RENDERCOLORTARGET)
	AddressU = CLAMP;
	AddressV = CLAMP;
	AddressW = CLAMP;
	MIPFILTER = NONE;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(radialSceneMap, RENDERCOLORTARGET)
	AddressU = CLAMP;
	AddressV = CLAMP;
	AddressW = CLAMP;
	MIPFILTER = NONE;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(downsampleMap, RENDERCOLORTARGET)
	AddressU = CLAMP;
	AddressV = CLAMP;
	AddressW = CLAMP;
	MIPFILTER = NONE;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(blurMap1, RENDERCOLORTARGET)
	AddressU = CLAMP;
	AddressV = CLAMP;
	AddressW = CLAMP;
	MIPFILTER = NONE;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
END_DECLARE_TEXTURE;

BEGIN_DECLARE_TEXTURE_TARGET(blurMap2, RENDERCOLORTARGET)
	AddressU = CLAMP;
	AddressV = CLAMP;
	AddressW = CLAMP;
	MIPFILTER = NONE;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
END_DECLARE_TEXTURE;

// For the last pass we add this screen border fadeout map to darken the borders
BEGIN_DECLARE_TEXTURE (screenBorderFadeoutMap, 0)
    AddressU  = CLAMP;
    AddressV  = CLAMP;
    AddressW  = CLAMP;
    MIPFILTER = NONE;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
END_DECLARE_TEXTURE;


// Returns luminance value of col to convert color to grayscale
float Luminance(float3 col)
{
    return dot(col, float3(0.3, 0.59, 0.11));
}

struct VB_OutputPosTexCoord
{
    float4 pos      : SV_POSITION;
    float2 texCoord : TEXCOORD0;
};

struct VB_OutputPos2TexCoords
{
    float4 pos         : SV_POSITION;
    float2 texCoord[2] : TEXCOORD0;
};

struct VB_OutputPos3TexCoords
{
    float4 pos         : SV_POSITION;
    float2 texCoord[3] : TEXCOORD0;
};

struct VB_OutputPos4TexCoords
{
    float4 pos         : SV_POSITION;
    float2 texCoord[4] : TEXCOORD0;
};


VB_OutputPos4TexCoords VS_SimpleBlur(
    uniform float2 direction,
    float4 pos      : POSITION,
    float2 texCoord : TEXCOORD0)
{
    VB_OutputPos4TexCoords Out = (VB_OutputPos4TexCoords)0;
    Out.pos = pos;
    float2 texelSize = 1.0f / windowSize;

    Out.texCoord[0] = texCoord + texelSize*(float2(2.0f, 2.0f)+direction*(-3.0f));
    Out.texCoord[1] = texCoord + texelSize*(float2(2.0f, 2.0f)+direction*(-1.0f));
    Out.texCoord[2] = texCoord + texelSize*(float2(2.0f, 2.0f)+direction*(+1.0f));
    Out.texCoord[3] = texCoord + texelSize*(float2(2.0f, 2.0f)+direction*(+3.0f));
    
    return Out;
}

VB_OutputPos2TexCoords VS_ScreenQuad(
    float4 pos      : POSITION,
    float2 texCoord : TEXCOORD0)
{
    VB_OutputPos2TexCoords Out;
    float2 texelSize = 1.0 /
        (windowSize * downsampleScale);
    Out.pos = pos;
    // Don't use bilinear filtering
    Out.texCoord[0] = texCoord + texelSize*0.5;
    Out.texCoord[1] = texCoord + texelSize*0.5;
    return Out;
}

VB_OutputPos3TexCoords VS_ScreenQuadSampleUp(
    float4 pos      : POSITION,
    float2 texCoord : TEXCOORD0)
{
    VB_OutputPos3TexCoords Out;
    float2 texelSize = 1.0 / windowSize;
    Out.pos = pos;
    // Don't use bilinear filtering
    Out.texCoord[0] = texCoord + texelSize*0.5f;
    Out.texCoord[1] = texCoord + texelSize*0.5f/downsampleScale;
    Out.texCoord[2] = texCoord + (1.0/128.0f)*0.5f;
    return Out;
}


float4 PS_ComposeFinalImage20(
    VB_OutputPos3TexCoords In) : SV_TARGET0
{
    float4 orig = SAMPLE_TEXTURE(radialSceneMap, In.texCoord[0]);
    float4 blur = SAMPLE_TEXTURE(blurMap2, In.texCoord[1]);

    float4 screenBorderFadeout =
		SAMPLE_TEXTURE(screenBorderFadeoutMap, In.texCoord[2]);
        
    float4 ret =
        0.75f*orig +
        GlowIntensity*blur +
        HighlightIntensity*blur.a;
    ret.rgb *= screenBorderFadeout;
    
    // Change colors a bit, sub 20% red and add 25% blue (photoshop values)
    // Here the values are -4% and +5%
    ret.rgb = float3(
        ret.r+0.054f/2,
        ret.g-0.021f/2,
        ret.b-0.035f/2);
    
    // Change brightness -5% and contrast +10%
    ret.rgb = ret.rgb * 0.95f;
    ret.rgb = (ret.rgb - float3(0.5, 0.5, 0.5)) * 1.05f +
        float3(0.5, 0.5, 0.5);

    return ret;
}

//////////////////
// ps_2_0 stuff //
//////////////////

// Works only on ps_2_0 and up
struct VB_OutputPos7TexCoords
{
    float4 pos         : SV_POSITION;
    float2 texCoord[7] : TEXCOORD0;
};

struct VB_OutputPos8TexCoords
{
    float4 pos         : SV_POSITION;
    float2 texCoord[4] : TEXCOORD0;
};

VB_OutputPos4TexCoords VS_DownSample20(
    float4 pos : POSITION,
    float2 texCoord : TEXCOORD0)
{
    VB_OutputPos4TexCoords Out;
    float2 texelSize = DownsampleMultiplicator /
        (windowSize * downsampleScale);
    float2 s = texCoord;
    Out.pos = pos;
    
    Out.texCoord[0] = s + float2(-1, -1)*texelSize;
    Out.texCoord[1] = s + float2(+1, +1)*texelSize;
    Out.texCoord[2] = s + float2(+1, -1)*texelSize;
    Out.texCoord[3] = s + float2(+1, +1)*texelSize;
    
    return Out;
}

float4 PS_DownSample20(
    VB_OutputPos4TexCoords In) : SV_TARGET0
{
    float4 c;

    // box filter (only for ps_2_0)
    c = SAMPLE_TEXTURE(radialSceneMap, In.texCoord[0])/4;
    c += SAMPLE_TEXTURE(radialSceneMap, In.texCoord[1])/4;
    c += SAMPLE_TEXTURE(radialSceneMap, In.texCoord[2])/4;
    c += SAMPLE_TEXTURE(radialSceneMap, In.texCoord[3])/4;

    // store hilights in alpha, can't use smoothstep version!
    // Fake it with highly optimized version using 80% as treshold.
    float l = Luminance(c.rgb);
    float treshold = 0.75f;
    if (l < treshold)
        c.a = 0;
    else
    {
        l = l-treshold;
        l = l+l+l+l; // bring 0..0.25 back to 0..1
        c.a = l;
    }

    return c;
}


// Blur downsampled map
VB_OutputPos7TexCoords _VS_Blur20(
    float2 direction,
    float4 pos : POSITION,
    float2 texCoord : TEXCOORD0)
{
    VB_OutputPos7TexCoords Out = (VB_OutputPos7TexCoords)0;
    Out.pos = pos;

    float2 texelSize = BlurWidth / windowSize;
    float2 s = texCoord - texelSize*(7-1)*0.5*direction;
    for (int i=0; i<7; i++)
    {
        Out.texCoord[i] = s + texelSize*i*direction;
    }

    return Out;
}

VB_OutputPos7TexCoords VS_Blur20Horizontal(
	float4 pos : POSITION,
	float2 texCoord : TEXCOORD0)
{
	return _VS_Blur20(float2 (1, 0), pos, texCoord);
}

VB_OutputPos7TexCoords VS_Blur20Vertical(
	float4 pos : POSITION,
	float2 texCoord : TEXCOORD0)
{
	return _VS_Blur20(float2 (0, 1), pos, texCoord);
}  

float4 PS_Blur20DownSampler(
	VB_OutputPos7TexCoords In) : SV_TARGET0
{
	float4 c = 0;

	// this loop will be unrolled by compiler
	for (int i = 0; i<7; i++)
	{
		c += SAMPLE_TEXTURE(downsampleMap, In.texCoord[i]) * weights7[i];
	}

	return c;
}

float4 PS_Blur20BlurSampler(
	VB_OutputPos7TexCoords In) : SV_TARGET0
{
	float4 c = 0;

	// this loop will be unrolled by compiler
	for (int i = 0; i<7; i++)
	{
		c += SAMPLE_TEXTURE(blurMap1, In.texCoord[i]) * weights7[i];
	}

	return c;
}

VB_OutputPos8TexCoords VS_RadialBlur20(
    float4 pos      : POSITION,
    float2 texCoord : TEXCOORD0)
{
    VB_OutputPos8TexCoords Out;
    float2 texelSize = 1.0 / windowSize;
    Out.pos = pos;
    // Don't use bilinear filtering, correct pixel locations
    
    // This is our original finalSceneMap, reuse existing locations
    Out.texCoord[0] = texCoord + texelSize*0.5f;
    
    // For all radial blur steps scale the finalSceneMap
    float2 texCentered = (texCoord-float2(0.5f, 0.5f))*2.0f;
    
    // Now apply formula to nicely increase blur factor to the borders
    for (int i=1; i<4; i++)
    {
        texCentered = texCentered+
            radialBlurScaleFactor*(0.5f+(i*2)*0.15f)*texCentered*abs(texCentered);
        Out.texCoord[i] = (texCentered+float2(1.0f, 1.0f))/2.0f + texelSize*0.5;
    }
    
    return Out;
}

float4 PS_RadialBlur20(
    VB_OutputPos8TexCoords In) : SV_TARGET0
{
    float4 radialBlur = SAMPLE_TEXTURE(sceneMap, In.texCoord[0]);
    for (int i=1; i<4; i++)
        radialBlur += SAMPLE_TEXTURE(sceneMap, In.texCoord[i]);
    return radialBlur/4;
}

BEGIN_TECHNIQUE(ScreenGlow20)
    BEGIN_PASS(RadialBlur)
        SHADERS(VS_RadialBlur20,PS_RadialBlur20)
    END_PASS
    BEGIN_PASS(DownSample)
        SHADERS(VS_DownSample20,PS_DownSample20)
    END_PASS
    BEGIN_PASS(GlowBlur1)
        SHADERS(VS_Blur20Horizontal,PS_Blur20DownSampler)
    END_PASS
    BEGIN_PASS(GlowBlur2)
        SHADERS(VS_Blur20Vertical,PS_Blur20BlurSampler)
    END_PASS    
    BEGIN_PASS(ComposeFinalScene)
        SHADERS(VS_ScreenQuadSampleUp,PS_ComposeFinalImage20)
    END_PASS              
END_TECHNIQUE
