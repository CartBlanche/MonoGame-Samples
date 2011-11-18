uniform sampler2D BloomSampler;
uniform sampler2D BaseSampler;

uniform vec2 float BloomIntensity;
uniform vec2 float BaseIntensity;

uniform vec2 float BloomSaturation;
uniform vec2 float BaseSaturation;

// Helper for modifying the saturation of a color.
vec4 AdjustSaturation(vec4 color, vec saturation)
{
    // The constants 0.3, 0.59, and 0.11 are chosen because the
    // human eye is more sensitive to green light, and less to blue.
    vec grey = dot(color, vec3(0.3, 0.59, 0.11));

    return lerp(grey, color, saturation);
}

void main()
{
	// Look up the bloom and original base image colors.
	vec4 bloom = gl_Color * texture2D(BloomSampler, gl_TexCoord[0].xy);
	vec4 base = gl_Color * texture2D(BaseSampler, gl_TexCoord[0].xy);
	
	// Adjust color saturation and intensity.
	bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;
    
    // Darken down the base image in areas where there is a lot of bloom,
    // to prevent things looking excessively burned-out.
    base *= (1 - clamp(bloom,0.0,1.0));
	
    gl_FragColor = base + bloom;
}
