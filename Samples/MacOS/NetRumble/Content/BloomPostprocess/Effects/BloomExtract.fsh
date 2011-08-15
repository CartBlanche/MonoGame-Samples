uniform sampler2D TextureSampler_s0;
uniform float BloomThreshold;

void main()
{
  vec4 c = texture2D(TextureSampler_s0, gl_TexCoord[0].xy);
  gl_FragColor = clamp((c - BloomThreshold )/ (1.0-BloomThreshold),0.0, 1.0);
}

