#include "Macros.fxh"
// Shows the sky bg with help of a cube map texture,
// should be called before anything else is rendered.";

// We need view, projection, ambientColor for the scene brightness and
// diffuseTexture, which is the background cube map texture for the sky.
BEGIN_CONSTANTS
static const float SkyCubeScale = 100.0f;

float4x4 view : View;
float4x4 projection : Projection;

// The ambient color for the sky, should be 1 for normal brightness.
float4 ambientColor : Ambient = {1.0f, 1.0f, 1.0f, 1.0f};

// Night stars layer (see Documentation/Lighting-Plan.md): x = blend (0 = off, set from C#
// every frame, GL starts parameters at 0), y = gain, z/w = sine of the elevation where the
// stars start to fade in and are fully visible (hides the horizon of the star photo).
float4 NightSky;

// Moon (drawn in the sky pass so mountains and the landscape cover it): xyz = direction to the
// moon in cube space, w = 1 - cos(angular radius); MoonParams.x = intensity (0 = off, set from
// C# every frame), y = halo strength, z = halo size relative to the disc.
float4 MoonDir;
float4 MoonParams;
END_CONSTANTS

BEGIN_DECLARE_CUBE_TARGET(diffuseTexture, Environment)
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
END_DECLARE_TEXTURE;

// Stars-only night sky (CC0 photo, see Documentation/Credits-Night-Sky.md).
BEGIN_DECLARE_CUBE_TARGET(nightTexture, Environment)
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
END_DECLARE_TEXTURE;

//-------------------------------------

struct VertexInput
{
    float3 pos : POSITION;
};

struct VB_OutputPos3DTexCoord
{
    float4 pos      : SV_POSITION;
    float3 texCoord : TEXCOORD0;
};

VB_OutputPos3DTexCoord VS_SkyCubeMap(VertexInput In)
{
    VB_OutputPos3DTexCoord Out;
    
    // Scale the box up so that we don't hit the near clip plane
    float3 pos = In.pos * SkyCubeScale;
    
    // In.pos is a float 3 for this calculation so that translation is ignored
    pos = mul(pos, view);
    // However, we need the translation for the projection
    Out.pos = mul(float4(pos, 1.0f), projection);
    
    // Just use the positions to infer the texture coordinates
    // Swap y and z because we use +z as up
    Out.texCoord = float3(In.pos.xzy);
        
    return Out;
}

float4 PS_SkyCubeMap(VB_OutputPos3DTexCoord In) : SV_TARGET
{
    float4 day = SAMPLE_CUBE(diffuseTexture, In.texCoord);
    float4 texCol = ambientColor * day;

    // Stars only where the day cube is open blue sky (mountains and desert have r > b,
    // clouds are white), and not near the horizon. NightSky.x == 0 adds nothing.
    float3 dir = normalize(In.texCoord);
    float skyMask = saturate((day.b - day.r) * 4.0f);
    float elev = smoothstep(NightSky.z, NightSky.w, dir.y);
    float3 stars = SAMPLE_CUBE(nightTexture, In.texCoord).rgb;
    texCol.rgb += stars * (NightSky.x * NightSky.y * skyMask * elev);

    // Moon disc with a soft edge, faint maria and a halo; hidden by terrain (brown, r > b) and
    // mostly by clouds (bright red channel) so it sits behind the landscape and the day clouds.
    float off = 1.0f - dot(dir, normalize(MoonDir.xyz));
    float disc = 1.0f - smoothstep(MoonDir.w * 0.7f, MoonDir.w, off);
    float halo = exp(-off / (MoonDir.w * MoonParams.z)) * MoonParams.y;
    float3 mp = dir * 55.0f;
    float maria = 0.5f + 0.5f * sin(mp.x * 3.1f + sin(mp.y * 2.7f) * 1.7f) * sin(mp.z * 2.3f + mp.x * 1.1f);
    float terrain = saturate((day.r - day.b - 0.05f) * 8.0f);
    float cloud = smoothstep(0.62f, 0.85f, day.r) * (1.0f - terrain);
    float moonVis = (1.0f - terrain) * (1.0f - 0.85f * cloud);
    // The disc replaces the sky (so the markings show instead of saturating to white), the halo adds.
    float3 moonCol = float3(0.93f, 0.95f, 1.0f);
    float vis = saturate(MoonParams.x) * moonVis;
    texCol.rgb = lerp(texCol.rgb, moonCol * (0.66f + 0.34f * maria), saturate(disc * vis));
    texCol.rgb += moonCol * halo * vis;
    return texCol;
}

TECHNIQUE (SkyCubeMap, VS_SkyCubeMap, PS_SkyCubeMap)
