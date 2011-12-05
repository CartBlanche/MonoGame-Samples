uniform sampler2D TextureSampler;

uniform float BloomThreshold;

void main()
{    
    // Look up the original image color.
    vec4 c = texture2D(TextureSampler, gl_TexCoord[0].xy);
    
    // Adjust it to keep only values brighter than the specified threshold.
	gl_FragColor = clamp((c - BloomThreshold )/ (1.0-BloomThreshold),0.0, 1.0);
}