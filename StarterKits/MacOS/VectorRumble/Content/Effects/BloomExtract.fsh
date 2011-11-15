uniform sampler2D bgl_RenderedTexture;

void main()
{
   vec4 sum = vec4(0);
   vec2 texcoord = vec2(gl_TexCoord[0]);
   int j;
   int i;

   for( i= -4 ;i < 4; i++)
   {
        for (j = -3; j < 3; j++)
        {
            sum += texture2D(bgl_RenderedTexture, texcoord + vec2(j, i)*0.004) * 0.25;
        }
   }
       if (texture2D(bgl_RenderedTexture, texcoord).r < 0.3)
    {
       gl_FragColor = sum*sum*0.012 + texture2D(bgl_RenderedTexture, texcoord);
    }
    else
    {
        if (texture2D(bgl_RenderedTexture, texcoord).r < 0.5)
        {
            gl_FragColor = sum*sum*0.009 + texture2D(bgl_RenderedTexture, texcoord);
        }
        else
        {
            gl_FragColor = sum*sum*0.0075 + texture2D(bgl_RenderedTexture, texcoord);
        }
    }
}