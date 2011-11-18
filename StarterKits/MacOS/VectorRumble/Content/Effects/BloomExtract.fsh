uniform sampler2D TextureSampler;

uniform vec2 BloomThreshold;

void main()
{
	// Look up the original image color.
	vec4 tex = gl_Color * texture2D(TextureSampler, gl_TexCoord[0].xy);
	
	// Adjust it to keep only values brighter than the specified threshold.
	vec4 color = tex;
	color *= clamp((tex.a - BloomThreshold) / (1 - BloomThreshold),0.0,1.0);
	
    gl_FragColor = color;
}