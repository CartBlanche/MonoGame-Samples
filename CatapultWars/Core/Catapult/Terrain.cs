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

        // Snow lying on the ground (themes with a snow cap): thickness per column, dug away by craters
        static readonly float[] snow = new float[Width];
        static Color snowColor;
        static float snowThickness;
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

            ResetSnow();

            if (texture != null)
            {
                texture.Dispose();
                texture = null;
            }
            dirty = true;
        }

        static void ResetSnow()
        {
            for (int x = 0; x < Width; x++)
                snow[x] = snowThickness;
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

        		// The middle of a crater is bare earth; towards its edge the snow stays as a rim
        		if (snowThickness > 0f)
        			snow[i] *= 1f - MathHelper.Clamp(dig / (depth * 0.45f), 0f, 1f);
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
        static Color[] rebuildBuffer;
        static Color? craterColor; // earth that shows in the middle of a crater, instead of the hillside

        // A row just above the ground band, where the background is hillside, sampled for crater colour
        const int SampleRow = 376;

        /// <summary>
        /// Looks up what the background shows just above the ground in every column, so a crater
        /// reveals the same hillside as the ground either side of it instead of a flat colour.
        /// </summary>
        public static void Prepare(Texture2D foreground, Texture2D mountain, ThemeDefinition theme)
        {
            Color foregroundTint = theme.ForegroundTint, mountainTint = theme.MountainTint;
            tint = foregroundTint;
            snowThickness = theme.SnowCap.HasValue ? theme.SnowCapThickness : 0f;
            snowColor = theme.SnowCap ?? Color.White;
            ResetSnow();
            craterColor = theme.CraterColor;
            cavity = new Color[Width];

            var fg = new Color[foreground.Width * foreground.Height];
            var mt = new Color[mountain.Width * mountain.Height];
            foreground.GetData(fg);
            mountain.GetData(mt);

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

        // Colour of the ground at row y: earth, or snow within cap pixels of the surface s
        static Color Surface(int y, float s, float cap)
        {
            Color earth = Multiply(Earth, tint);
            if (cap <= 0f)
                return earth;
            float k = MathHelper.Clamp(s + cap - (y + .5f) + .5f, 0f, 1f);
            return Color.Lerp(earth, snowColor, k);
        }

        static void Rebuild()
        {
            int h = BottomY - TopY;
            // One buffer reused for every rebuild: a new array each crater is large enough to
            // land on the large object heap and invite a collection mid-game
            var pixels = rebuildBuffer ??= new Color[Width * h];
            Array.Clear(pixels, 0, pixels.Length);

            for (int x = 0; x < Width; x++)
            {
                float s = surface[x];
                float cap = snowThickness > 0f ? snow[x] : 0f;
                if (Math.Abs(s - GroundY) < 0.25f)
                {
                    // Flat ground: the background art already draws it, apart from any snow on top
                    for (int y = (int)s; cap > 0f && y <= (int)(s + cap) + 1 && y < BottomY; y++)
                    {
                        float coverage = MathHelper.Clamp(y + 1 - s, 0f, 1f);
                        pixels[(y - TopY) * Width + x] = Surface(y, s, cap) * coverage;
                    }
                    continue;
                }

                if (s < GroundY)
                {
                    // Raised ground: plain earth under any snow, with a soft edge at the top
                    for (int y = (int)s; y < BottomY; y++)
                    {
                        float coverage = MathHelper.Clamp(y + 1 - s, 0f, 1f);
                        pixels[(y - TopY) * Width + x] = Surface(y, s, cap) * coverage;
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
                        if (craterColor.HasValue)
                        {
                            // Brown earth in the bare middle of the crater, fading to the hillside at its
                            // rim and towards the top, so the hole has no hard edges
                            float bare = snowThickness > 0f ? 1f - snow[x] / snowThickness : 1f;
                            float down = MathHelper.Clamp((y - (GroundY - 12f)) / 12f, 0f, 1f);
                            back = Color.Lerp(back, craterColor.Value, bare * down);
                        }
                        Color e = Surface(y, s, cap) * coverage, c = back * (1f - coverage);
                        pixels[(y - TopY) * Width + x] = new Color(e.R + c.R, e.G + c.G, e.B + c.B, 255);
                    }
                }
            }

            texture.SetData(pixels);
        }
    }
}
