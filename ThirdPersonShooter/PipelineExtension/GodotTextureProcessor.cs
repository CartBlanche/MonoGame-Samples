using System.ComponentModel;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace ThirdPersonShooter.PipelineExtension;

/// <summary>
/// Texture processor for Godot textures: downsizes (box/bilinear via BitmapContent.Copy) so the longest side is at most
/// MaxSize (power of two kept), then builds mipmaps and optionally DXT-compresses. Colour data is never converted to sRGB
/// (the shader decodes albedo/emission with pow 2.2; normal and ORM maps stay linear).
/// </summary>
[ContentProcessor(DisplayName = "Godot Texture - ThirdPersonShooter")]
public class GodotTextureProcessor : ContentProcessor<TextureContent, TextureContent>
{
    [DefaultValue(2048)] public int MaxSize { get; set; } = 2048;
    [DefaultValue(true)] public bool GenerateMipmaps { get; set; } = true;
    [DefaultValue(TextureProcessorOutputFormat.Color)] public TextureProcessorOutputFormat TextureFormat { get; set; } = TextureProcessorOutputFormat.Color;

    public override TextureContent Process(TextureContent input, ContentProcessorContext context)
    {
        var face = input.Faces[0][0];
        if (face.Width > MaxSize || face.Height > MaxSize)
        {
            float s = MaxSize / (float)System.Math.Max(face.Width, face.Height);
            int w = System.Math.Max(1, (int)(face.Width * s)), h = System.Math.Max(1, (int)(face.Height * s));
            var small = new PixelBitmapContent<Microsoft.Xna.Framework.Color>(w, h);
            BitmapContent.Copy(face, small);
            input.Faces[0].Clear();
            input.Faces[0].Add(small);
            context.Logger.Log(string.Format("Resized {0}x{1} -> {2}x{3}", face.Width, face.Height, w, h));
        }
        var inner = new TextureProcessor { GenerateMipmaps = GenerateMipmaps, TextureFormat = TextureFormat, ColorKeyEnabled = false, PremultiplyAlpha = false, ResizeToPowerOfTwo = false, MakeSquare = false };
        return inner.Process(input, context);
    }
}
