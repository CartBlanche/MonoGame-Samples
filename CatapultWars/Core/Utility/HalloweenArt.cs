using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// Halloween textures, drawn in code so the theme needs no new content files.
    /// All textures are premultiplied alpha, like the rest of the game's content.
    /// </summary>
    public class HalloweenArt
    {
        public const int BatFrameCount = 6;
        public static readonly Vector2 BatOrigin = new Vector2(26, 18);

        public Texture2D Pixel { get; private set; }
        public Texture2D Glow { get; private set; }
        public Texture2D NightSky { get; private set; }
        public Texture2D TitleShade { get; private set; }
        public Texture2D Moon { get; private set; }
        public Texture2D Pumpkin { get; private set; }
        public Texture2D[] BatFrames { get; private set; }

        static HalloweenArt instance;
        static GraphicsDevice instanceDevice;

        /// <summary>
        /// Textures are built once per graphics device and shared by every screen.
        /// </summary>
        public static HalloweenArt Get(GraphicsDevice device)
        {
            if (instance == null || instanceDevice != device)
            {
                instance = new HalloweenArt(device);
                instanceDevice = device;
            }
            return instance;
        }

        internal delegate bool Shape(float x, float y);

        HalloweenArt(GraphicsDevice gd)
        {
            Pixel = Solid(gd, 1, 1, Color.White);
            Glow = BuildGlow(gd, 64);
            NightSky = BuildNightSky(gd, 4, 480);
            TitleShade = BuildTitleShade(gd, 4, 480);
            Moon = BuildMoon(gd);
            Pumpkin = BuildPumpkin(gd);
            BatFrames = new Texture2D[BatFrameCount];
            for (int i = 0; i < BatFrameCount; i++)
            {
                // Wings sweep from fully up (+10) to fully down (-10).
                float wing = 10f - 20f * i / (BatFrameCount - 1);
                BatFrames[i] = BuildBat(gd, wing);
            }
        }

        static Texture2D Solid(GraphicsDevice gd, int w, int h, Color c)
        {
            Texture2D t = new Texture2D(gd, w, h);
            Color[] data = new Color[w * h];
            for (int i = 0; i < data.Length; i++)
                data[i] = c;
            t.SetData(data);
            return t;
        }

        static Texture2D BuildGlow(GraphicsDevice gd, int size)
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

        // Dusk gradient: deep indigo at the top, plum, then an orange horizon.
        static Texture2D BuildNightSky(GraphicsDevice gd, int w, int h)
        {
            Color[] stops = { new Color(13, 10, 36), new Color(42, 18, 80), new Color(122, 47, 90), new Color(224, 112, 42) };
            float[] at = { 0f, .4f, .78f, 1f };

            Texture2D t = new Texture2D(gd, w, h);
            Color[] data = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float p = Math.Min(y / 390f, 1f);
                int s = 0;
                while (s < at.Length - 2 && p > at[s + 1])
                    s++;
                float k = MathHelper.Clamp((p - at[s]) / (at[s + 1] - at[s]), 0f, 1f);
                Color c = Color.Lerp(stops[s], stops[s + 1], k);
                for (int x = 0; x < w; x++)
                    data[y * w + x] = c;
            }
            t.SetData(data);
            return t;
        }

        // Soft wash over the title art: cool and dark at the top, warm at the bottom.
        static Texture2D BuildTitleShade(GraphicsDevice gd, int w, int h)
        {
            Texture2D t = new Texture2D(gd, w, h);
            Color[] data = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float p = y / (float)(h - 1);
                Color top = new Color(20, 10, 50), bottom = new Color(255, 110, 20);
                float alpha = MathHelper.Lerp(.35f, .26f, p);
                Color c = Color.Lerp(top, bottom, p) * alpha;
                for (int x = 0; x < w; x++)
                    data[y * w + x] = c;
            }
            t.SetData(data);
            return t;
        }

        static Texture2D BuildMoon(GraphicsDevice gd)
        {
            Color disc = new Color(255, 179, 71), crater = new Color(232, 151, 57);
            return Render(gd, 80, 80,
                (Ellipse(40, 40, 38, 38), disc),
                (Ellipse(40 - 38 * .3f, 40 - 38 * .2f, 38 * .22f, 38 * .22f), crater),
                (Ellipse(40 + 38 * .25f, 40 + 38 * .15f, 38 * .3f, 38 * .3f), crater),
                (Ellipse(40 - 38 * .05f, 40 + 38 * .45f, 38 * .14f, 38 * .14f), crater));
        }

        static Texture2D BuildPumpkin(GraphicsDevice gd)
        {
            Color dark = new Color(201, 84, 14), mid = new Color(232, 112, 26), light = new Color(245, 138, 34);
            Color stem = new Color(79, 122, 42), leaf = new Color(106, 160, 58), lit = new Color(255, 219, 85);
            return Render(gd, 28, 26,
                (Ellipse(8, 15, 7, 9), dark),
                (Ellipse(20, 15, 7, 9), dark),
                (Ellipse(11, 15, 7, 10), mid),
                (Ellipse(17, 15, 7, 10), mid),
                (Ellipse(14, 15, 6, 10.5f), light),
                (Rect(12.5f, 2, 15.5f, 7), stem),
                (Rect(15, 3, 18, 5), leaf),
                (Poly(new Vector2(6, 15), new Vector2(11, 15), new Vector2(8.5f, 10.5f)), lit),
                (Poly(new Vector2(17, 15), new Vector2(22, 15), new Vector2(19.5f, 10.5f)), lit),
                (Poly(new Vector2(7, 19), new Vector2(10, 18), new Vector2(12, 20), new Vector2(14, 18),
                      new Vector2(16, 20), new Vector2(18, 18), new Vector2(21, 19), new Vector2(20, 22.5f),
                      new Vector2(8, 22.5f)), lit));
        }

        static Texture2D BuildBat(GraphicsDevice gd, float wing)
        {
            Color ink = new Color(11, 7, 22);
            Vector2 o = BatOrigin;

            List<(Shape, Color)> layers = new List<(Shape, Color)>();
            layers.Add((Ellipse(o.X, o.Y, 4, 6), ink));
            layers.Add((Poly(o + new Vector2(-3, -4), o + new Vector2(-2.5f, -9), o + new Vector2(0, -5),
                             o + new Vector2(2.5f, -9), o + new Vector2(3, -4)), ink));
            layers.Add((Poly(WingOutline(o, -1, wing)), ink));
            layers.Add((Poly(WingOutline(o, 1, wing)), ink));
            return Render(gd, 52, 36, layers.ToArray());
        }

        static Vector2[] WingOutline(Vector2 o, int side, float wing)
        {
            Vector2[] p =
            {
                new Vector2(3, -2),
                new Vector2(24, -3 - wing * .6f),
                new Vector2(16, 3 - wing * .1f),
                new Vector2(9, 5),
                new Vector2(3, 4),
            };
            Vector2[] c =
            {
                new Vector2(14, -10 - wing),
                new Vector2(19, -wing * .2f),
                new Vector2(12, 0),
                new Vector2(5, 2),
            };

            List<Vector2> pts = new List<Vector2>();
            pts.Add(o + new Vector2(p[0].X * side, p[0].Y));
            for (int seg = 0; seg < 4; seg++)
            {
                for (int s = 1; s <= 8; s++)
                {
                    float t = s / 8f, u = 1f - t;
                    Vector2 q = u * u * p[seg] + 2f * u * t * c[seg] + t * t * p[seg + 1];
                    pts.Add(o + new Vector2(q.X * side, q.Y));
                }
            }
            return pts.ToArray();
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
