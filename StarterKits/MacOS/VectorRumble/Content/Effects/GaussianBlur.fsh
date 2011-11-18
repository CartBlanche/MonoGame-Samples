uniform sampler2D TextureSampler;

#define SAMPLE_COUNT 15

vec2 SampleOffsets[SAMPLE_COUNT];
vec SampleWeights[SAMPLE_COUNT];

void main()
{
	// Look up the original image color.
	vec4 color = vec4(0);
   	vec2 texcoord = vec2(gl_TexCoord[0]);
	
	// Combine a number of weighted image filter taps.
    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        color += gl_Color * (texture2D(TextureSampler, texcoord + SampleOffsets[i]) * SampleWeights[i]);
    }
	
    gl_FragColor = color;
}
