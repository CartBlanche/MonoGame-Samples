using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// A small burst of puffs and chunks where a projectile lands. How much there is, and
    /// how far it flies, scales with the speed of the impact; the colours follow the season.
    /// </summary>
    public class ImpactDust
    {
        struct Particle
        {
            public Vector2 Position, Velocity;
            public float Age, Life, Size, Growth, Drag, Gravity;
            public Color Color;
            public bool Chunk;
        }

        // Impact speeds seen in play run from roughly 200 (weak shot) to 600 (full power).
        const float SlowSpeed = 200f, FastSpeed = 600f;

        static readonly Random random = new Random();
        static GraphicsDevice textureDevice;
        static Texture2D softTexture, hardTexture;

        readonly List<Particle> particles = new List<Particle>();
        readonly GraphicsDevice device;

        public ImpactDust(GraphicsDevice device)
        {
            this.device = device;
        }

        /// <summary>
        /// True while any particle is still visible.
        /// </summary>
        public bool IsActive
        {
            get { return particles.Count > 0; }
        }

        /// <summary>
        /// Throws up a burst at a landing point.
        /// </summary>
        /// <param name="speed">Speed of the projectile at impact.</param>
        /// <param name="forward">+1 or -1: the way the projectile was travelling.</param>
        public void Burst(Vector2 position, float speed, int forward)
        {
            float power = MathHelper.Clamp((speed - SlowSpeed) / (FastSpeed - SlowSpeed), 0f, 1f);
            Color puffColor, chunkColor;
            Palette(out puffColor, out chunkColor);

            int puffs = 6 + (int)(power * 10);
            for (int i = 0; i < puffs; i++)
            {
                float angle = -MathHelper.PiOver2 + (Next() - .5f) * MathHelper.Pi * .9f;
                float v = (25f + Next() * 60f) * (.6f + power);
                float size = 8f + Next() * 8f;
                particles.Add(new Particle
                {
                    Position = position + new Vector2((Next() - .5f) * 14f, (Next() - .5f) * 6f),
                    Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * v
                        + new Vector2(forward * power * 50f, 0f),
                    Life = .5f + Next() * .4f + power * .3f,
                    Size = size,
                    Growth = size * (1.6f + power),
                    Drag = 2.5f,
                    Gravity = -15f,
                    Color = Vary(puffColor),
                });
            }

            int chunks = 5 + (int)(power * 14);
            for (int i = 0; i < chunks; i++)
            {
                float angle = -MathHelper.PiOver2 + (Next() - .5f) * 1.7f;
                float v = (70f + Next() * 190f) * (.5f + power);
                particles.Add(new Particle
                {
                    Position = position + new Vector2((Next() - .5f) * 8f, 0f),
                    Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * v
                        + new Vector2(forward * power * 90f, 0f),
                    Life = .45f + Next() * .35f,
                    Size = 3f + Next() * 3f,
                    Drag = .3f,
                    Gravity = 500f,
                    Color = Vary(chunkColor),
                    Chunk = true,
                });
            }
        }

        public void Update(float elapsed)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Age += elapsed;
                if (p.Age >= p.Life)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                p.Velocity *= Math.Max(0f, 1f - p.Drag * elapsed);
                p.Velocity.Y += p.Gravity * elapsed;
                p.Position += p.Velocity * elapsed;
                particles[i] = p;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (particles.Count == 0)
                return;

            EnsureTextures();
            foreach (Particle p in particles)
            {
                float t = p.Age / p.Life;
                float alpha = p.Chunk ? 1f - t * t * t : .7f * (float)Math.Pow(1f - t, 1.5);
                Texture2D texture = p.Chunk ? hardTexture : softTexture;
                float size = p.Size + p.Growth * t;

                spriteBatch.Draw(texture, p.Position, null, p.Color * alpha, 0f,
                    new Vector2(texture.Width / 2f), size / texture.Width, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// Puff and chunk colours for the current look. A Christmas case returns white here.
        /// </summary>
        static void Palette(out Color puff, out Color chunk)
        {
            switch (Theme.Current)
            {
                case Season.Halloween:
                    puff = new Color(255, 140, 40);
                    chunk = new Color(210, 85, 15);
                    break;
                default:
                    puff = new Color(165, 165, 165);
                    chunk = new Color(105, 105, 105);
                    break;
            }
        }

        static float Next()
        {
            return (float)random.NextDouble();
        }

        // A little brightness variation so a burst never looks stamped.
        static Color Vary(Color c)
        {
            float k = .85f + Next() * .3f;
            return new Color((int)Math.Min(255, c.R * k), (int)Math.Min(255, c.G * k), (int)Math.Min(255, c.B * k));
        }

        void EnsureTextures()
        {
            if (softTexture != null && textureDevice == device)
                return;

            textureDevice = device;
            softTexture = BuildDisc(device, 32, soft: true);
            hardTexture = BuildDisc(device, 8, soft: false);
        }

        // Premultiplied white disc: feathered for puffs, crisp for chunks.
        static Texture2D BuildDisc(GraphicsDevice device, int size, bool soft)
        {
            Texture2D t = new Texture2D(device, size, size);
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
    }
}
