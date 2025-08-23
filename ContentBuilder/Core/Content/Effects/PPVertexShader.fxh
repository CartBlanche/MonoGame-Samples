///-------------------------------------------------------------------------------------------------
/// <summary>   
///     This header file is for used to ensure vertex patching in the DX build.
/// </summary>
///
/// <remarks>   Charles Humphrey, 12/07/2025. </remarks>
///-------------------------------------------------------------------------------------------------


#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};