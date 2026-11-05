using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// The ground between the catapults: one surface height per screen column. Projectiles
    /// collide against it and it draws itself over the flat background art. Heights are kept
    /// per column so that craters can be carved out of it later.
    /// </summary>
    static class Terrain
    {
        public const int Width = 800;

        /// <summary>The flat ground line, where the catapults stand. Screen y, so bigger is lower.</summary>
        public const float GroundY = 392f;

        // The drawn strip. It covers every row a hill or crater can reach.
        const int TopY = 280;
        const int BottomY = 480;

        static readonly float[] surface = new float[Width];
        static Texture2D texture;
        static bool dirty = true;

        static Terrain()
        {
            Reset();
        }

        /// <summary>Back to a fresh battlefield: flat ground with the hill in the middle.</summary>
        public static void Reset()
        {
            for (int x = 0; x < Width; x++)
            {
                surface[x] = GroundY - Battlefield.HillAt(x);
            }

            if (texture != null)
            {
                texture.Dispose();
                texture = null;
            }
            dirty = true;
        }

        // Ground under the catapults and their crews is never dug out, so nobody floats
        const float MaxDepth = 36f; // deepest a crater can go below the flat ground line

        /// <summary>
        /// Digs a crater where a shot landed. A harder hit digs a wider, deeper one.
        /// </summary>
        public static void Carve(float x, float impactSpeed)
        {
        	float power = MathHelper.Clamp((impactSpeed - 200f) / 400f, 0f, 1f);
        	float radius = 18f + 26f * power;
        	float depth = 12f + 20f * power;

        	int from = Math.Max(0, (int)Math.Floor(x - radius));
        	int to = Math.Min(Width - 1, (int)Math.Ceiling(x + radius));
        	for (int i = from; i <= to; i++)
        	{
        		if (OnPad(i))
        			continue;

        		float d = Math.Abs(i - x);
        		if (d >= radius)
        			continue;

        		float dig = depth * 0.5f * (1f + (float)Math.Cos(Math.PI * d / radius));
        		surface[i] = Math.Min(surface[i] + dig, GroundY + MaxDepth);
        	}
        	dirty = true;
        }

        static bool OnPad(int x)
        {
        return (x >= Battlefield.LeftPad.X && x <= Battlefield.LeftPad.Y) ||
                   (x >= Battlefield.RightPad.X && x <= Battlefield.RightPad.Y);
        }

        /// <summary>The ground height at x, smoothed between columns. Off screen it is the flat ground line.</summary>
        public static float SurfaceY(float x)
        {
            if (x <= 0f || x >= Width - 1)
                return GroundY;

            int i = (int)x;
            float f = x - i;
            return surface[i] * (1f - f) + surface[i + 1] * f;
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (texture == null)
                texture = new Texture2D(spriteBatch.GraphicsDevice, Width, BottomY - TopY);

            if (dirty)
            {
                Rebuild();
                dirty = false;
            }

            spriteBatch.Draw(texture, new Vector2(0, TopY), Color.White);
        }

        // Earth to match the dark band in the background art
        // and the hillside showing through where ground has been carved away
        static readonly Color Earth = new Color(58, 40, 14);
        static readonly Color Cavity = new Color(196, 180, 96);

        // Seasonal tint for the earth, and what shows through a crater in each column
        static Color tint = Color.White;
        static Color[] cavity;

        // A row just above the ground band, where the background is hillside, sampled for crater colour
        const int SampleRow = 376;

        /// <summary>
        /// Looks up what the background shows just above the ground in every column, so a crater
        /// reveals the same hillside as the ground either side of it instead of a flat colour.
        /// </summary>
        public static void Prepare(Texture2D foreground, Texture2D mountain, Color foregroundTint, Color mountainTint)
        {
            tint = foregroundTint;
            var fg = new Color[foreground.Width * foreground.Height];
            var mt = new Color[mountain.Width * mountain.Height];
            foreground.GetData(fg);
            mountain.GetData(mt);

            cavity = new Color[Width];
            for (int x = 0; x < Width; x++)
            {
                Color f = Multiply(fg[SampleRow * foreground.Height / 480 * foreground.Width + x * foreground.Width / Width], foregroundTint);
                Color m = Multiply(mt[SampleRow * mountain.Height / 480 * mountain.Width + x * mountain.Width / Width], mountainTint);

                // Foreground over mountain, then whatever is still see-through falls back to the flat colour
                float through = 1f - f.A / 255f;
                float r = f.R + m.R * through, g = f.G + m.G * through, b = f.B + m.B * through;
                float a = f.A + m.A * through;
                float rest = 1f - Math.Min(a, 255f) / 255f;
                Color fallback = Multiply(Cavity, foregroundTint);
                cavity[x] = new Color(
                    (int)Math.Min(255f, r + fallback.R * rest),
                    (int)Math.Min(255f, g + fallback.G * rest),
                    (int)Math.Min(255f, b + fallback.B * rest), 255);
            }
            dirty = true;
        }

        static Color Multiply(Color c, Color t)
        {
            return new Color(c.R * t.R / 255, c.G * t.G / 255, c.B * t.B / 255, (int)c.A);
        }

        static void Rebuild()
        {
            int h = BottomY - TopY;
            var pixels = new Color[Width * h];

            for (int x = 0; x < Width; x++)
            {
                float s = surface[x];
                if (Math.Abs(s - GroundY) < 0.25f)
                    continue; // flat ground, the background art already draws it

                if (s < GroundY)
                {
                    // Raised ground: plain earth, with a soft edge at the top
                    for (int y = (int)s; y < BottomY; y++)
                    {
                        float coverage = MathHelper.Clamp(y + 1 - s, 0f, 1f);
                                                Color c = Multiply(Earth, tint);
                        pixels[(y - TopY) * Width + x] = c * coverage;
                    }
                }
                else
                {
                    // Carved ground: the hillside shows where the earth was, then a lip of earth
                    for (int y = (int)GroundY - 12; y < BottomY; y++)
                    {
                        // Premultiplied blend of earth over cavity along the soft edge
                        float coverage = MathHelper.Clamp(y + 1 - s, 0f, 1f);
                        Color back = cavity != null ? cavity[x] : Cavity;
                        Color e = Multiply(Earth, tint) * coverage, c = back * (1f - coverage);
                        pixels[(y - TopY) * Width + x] = new Color(e.R + c.R, e.G + c.G, e.B + c.B, 255);
                    }
                }
            }

            texture.SetData(pixels);
        }
    }
}
