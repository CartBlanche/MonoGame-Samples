//-----------------------------------------------------------------------------
// Macros.fxh
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#ifdef SM5

// Macros for targeting shader 5.0(DX11)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_5_0 vsname (); PixelShader = compile ps_5_0 psname(); } }

#define TECHNIQUE_NO_VS(name, psname ) \
	technique name { pass { PixelShader = compile ps_5_0 psname(); } }

#elif SM4

// Macros for targeting shader model 4.0(DX11)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_4_0_level_9_1 vsname (); PixelShader = compile ps_4_0_level_9_1 psname(); } }

#define TECHNIQUE_NO_VS(name, psname ) \
	technique name { pass { PixelShader = compile ps_4_0_level_9_1 psname(); } }


#elif OPENGL

// Macros for targeting shader model 3.0 (OPENGL)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_3_0 vsname (); PixelShader = compile ps_3_0 psname(); } }

#define TECHNIQUE_NO_VS(name, psname ) \
	technique name { pass { PixelShader = compile ps_3_0 psname(); } }

#else

// Macros for targeting shader model 2.0 (DX9)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_2_0 vsname (); PixelShader = compile ps_2_0 psname(); } }

#define TECHNIQUE_NO_VS(name, psname ) \
	technique name { pass { PixelShader = compile ps_2_0 psname(); } }

#endif

#if defined(SM5) || defined(SM4)

// Macros for targeting shader model 4.0 and 5.0(DX11)

#define BEGIN_CONSTANTS     cbuffer Parameters : register(b0) {
#define END_CONSTANTS       };

#define DECLARE_TEXTURE(Name, index) \
    Texture2D<float4> Name : register(t##index); \
    sampler Name##Sampler : register(s##index)

#define BEGIN_DECLARE_TEXTURE(Name, index) \
	DECLARE_TEXTURE(Name, index) \
	{

#define END_DECLARE_TEXTURE \
	}

#define SAMPLE_TEXTURE(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)

#else

// Macros for targeting shader model 2.0, 3.0 (DX9/OPENGL)

#define SV_POSITION POSITION
#define SV_POSITION0 POSITION0
#define SV_TARGET COLOR
#define SV_TARGET0 COLOR0

#define BEGIN_CONSTANTS
#define END_CONSTANTS

#define DECLARE_TEXTURE(Name, index) \
    sampler Name##Sampler : register(s##index) \
	{ \
		Texture = (Name); \
	}

#define BEGIN_DECLARE_TEXTURE(Name, index) \
	sampler Name##Sampler : register(s##index) \
	{ \
		Texture = (Name);

#define END_DECLARE_TEXTURE \
	}

#define SAMPLE_TEXTURE(Name, texCoord)  tex2D(Name##Sampler, texCoord)

#endif
