using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// Shape helpers for art drawn in code, so themes and the crew need no new content files.
    /// All textures are premultiplied alpha, like the rest of the game's content.
    /// </summary>
    internal static class ShapeArt
    {
        internal delegate bool Shape(float x, float y);

        internal static Texture2D Solid(GraphicsDevice gd, int w, int h, Color c)
        {
            Texture2D t = new Texture2D(gd, w, h);
            Color[] data = new Color[w * h];
            for (int i = 0; i < data.Length; i++)
                data[i] = c;
            t.SetData(data);
            return t;
        }

        /// <summary>A soft round blob, white at the centre fading to nothing, for additive glows.</summary>
        internal static Texture2D Glow(GraphicsDevice gd, int size)
        {
            Texture2D t = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(half, half)) / half;
                    float a = MathHelper.Clamp(1f - d, 0f, 1f);
                    a *= a;
                    data[y * size + x] = new Color(a, a, a, a);
                }
            }
            t.SetData(data);
            return t;
        }

        /// <summary>A premultiplied white disc: feathered for puffs, crisp for chunks.</summary>
        internal static Texture2D Disc(GraphicsDevice gd, int size, bool soft)
        {
            Texture2D t = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(half, half)) / half;
                    float a = soft ? MathHelper.Clamp(1f - d, 0f, 1f) : MathHelper.Clamp((1f - d) * half, 0f, 1f);
                    if (soft)
                        a = a * a * (3f - 2f * a);
                    data[y * size + x] = new Color(a, a, a, a);
                }
            }
            t.SetData(data);
            return t;
        }

        internal static Shape Ellipse(float cx, float cy, float rx, float ry)
        {
            return (x, y) =>
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                return dx * dx + dy * dy <= 1f;
            };
        }

        internal static Shape Rect(float x0, float y0, float x1, float y1)
        {
            return (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
        }

        internal static Shape Poly(params Vector2[] p)
        {
            return (x, y) =>
            {
                bool inside = false;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                {
                    if ((p[i].Y > y) != (p[j].Y > y) &&
                        x < (p[j].X - p[i].X) * (y - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X)
                        inside = !inside;
                }
                return inside;
            };
        }

        /// <summary>
        /// Paints opaque shapes back to front with 3x3 supersampling for smooth edges.
        /// </summary>
        internal static Texture2D Render(GraphicsDevice gd, int w, int h, params (Shape shape, Color color)[] layers)
        {
            const int ss = 3;
            Color[] data = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float px = x + (sx + .5f) / ss, py = y + (sy + .5f) / ss;
                            for (int i = layers.Length - 1; i >= 0; i--)
                            {
                                if (!layers[i].shape(px, py))
                                    continue;
                                Color c = layers[i].color;
                                r += c.R / 255f; g += c.G / 255f; b += c.B / 255f; a += 1f;
                                break;
                            }
                        }
                    }
                    float n = ss * ss;
                    data[y * w + x] = new Color(r / n, g / n, b / n, a / n);
                }
            }

            Texture2D t = new Texture2D(gd, w, h);
            t.SetData(data);
            return t;
        }
    }
}
