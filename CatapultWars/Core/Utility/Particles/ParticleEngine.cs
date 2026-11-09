using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// A fixed pool of particles. The spray types describe the effects; the engine moves and draws
    /// them. Everything is allocated up front, so spraying, running and drawing create no garbage
    /// and cannot cause a collection in the middle of a game. Motion is scaled by elapsed time, so
    /// it looks the same at any frame rate and in slow motion.
    /// </summary>
    public sealed class ParticleEngine
    {
        struct Particle
        {
            public ParticleSprayType Spray;
            public float X, Y, Dx, Dy, Size, SizeInc, Angle, AngleInc;
            public float Age, Life;
            public int Tile;
            public Color Tint;
        }

        readonly Particle[] particles;
        readonly Random random;
        int cursor;
        int live;

        public ParticleEngine(int capacity = 512, Random random = null)
        {
            particles = new Particle[capacity];
            this.random = random ?? new Random();
        }

        /// <summary>Wind as an acceleration in pixels per second squared; each spray takes its share.</summary>
        public Vector2 Wind { get; set; }

        /// <summary>How many particles are alive.</summary>
        public int Count
        {
            get { return live; }
        }

        public bool IsActive
        {
            get { return live > 0; }
        }

        public void Clear()
        {
            Array.Clear(particles, 0, particles.Length);
            live = 0;
        }

        float Next()
        {
            return (float)random.NextDouble();
        }

        /// <summary>
        /// Starts <paramref name="count"/> particles near (x, y). Each starts up to
        /// <paramref name="posRandom"/> pixels from the point, with velocity
        /// (baseDx, baseDy) plus a random push of up to <paramref name="dirRandom"/> in each direction.
        /// </summary>
        public void Spray(ParticleSprayType spray, float x, float y, float posRandom,
                          float baseDx, float baseDy, float dirRandom, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // A random point and push within the unit circle, as the other games' sprays do
                float nx = Next() * 2f - 1f, ny = Next() * 2f - 1f;
                float l = (float)Math.Sqrt(nx * nx + ny * ny);
                if (l == 0f)
                    l = 1f;
                float v = Next();
                nx = nx * v / l;
                ny = ny * v / l;

                ref Particle p = ref particles[Take()];
                p.X = x + nx * posRandom;
                p.Y = y + ny * posRandom;
                p.Dx = baseDx + nx * dirRandom;
                p.Dy = baseDy + ny * dirRandom;
                Start(ref p, spray);
            }
        }

        /// <summary>
        /// Starts one particle with exactly this position and velocity, for effects that work out their
        /// own numbers. Size, spin, life, tile and colour still come from the spray type.
        /// </summary>
        public void Add(ParticleSprayType spray, float x, float y, float dx, float dy)
        {
            Add(spray, x, y, dx, dy, -1f, 0f, 0f);
        }

        /// <summary>
        /// Like <see cref="Add(ParticleSprayType, float, float, float, float)"/> but sets life, size and
        /// size growth itself (a negative life keeps the spray type's), for effects whose values depend on
        /// how hard something hit.
        /// </summary>
        public void Add(ParticleSprayType spray, float x, float y, float dx, float dy,
                        float life, float size, float sizeInc)
        {
            ref Particle p = ref particles[Take()];
            p.X = x;
            p.Y = y;
            p.Dx = dx;
            p.Dy = dy;
            Start(ref p, spray);
            if (life >= 0f)
            {
                p.Life = life;
                p.Size = size;
                p.SizeInc = sizeInc;
            }
        }

        // The next free slot. When the pool is full the oldest slot is reused, so a burst never fails.
        int Take()
        {
            int n = particles.Length;
            for (int i = 0; i < n; i++)
            {
                int slot = (cursor + i) % n;
                if (particles[slot].Life <= 0f || particles[slot].Age >= particles[slot].Life)
                {
                    cursor = (slot + 1) % n;
                    live++;
                    return slot;
                }
            }

            int reuse = cursor;
            cursor = (cursor + 1) % n;
            return reuse;
        }

        void Start(ref Particle p, ParticleSprayType s)
        {
            p.Spray = s;
            p.Age = 0f;
            p.Life = s.LifeTime + Next() * s.LifeTimeRandom;
            p.Size = s.Size + Next() * s.SizeRandom;
            p.SizeInc = s.SizeInc + Next() * s.SizeIncRandom;
            p.Angle = s.Angle + Next() * s.AngleRandom;
            p.AngleInc = s.AngleInc + Next() * s.AngleIncRandom;
            p.Tile = s.BlockCount > 0 ? s.FirstBlock + random.Next(s.BlockCount) : s.FirstBlock;

            Color tint = s.Tint;
            if (s.TintVariation > 0f)
            {
                float k = 1f - s.TintVariation + Next() * s.TintVariation * 2f;
                tint = new Color((int)Math.Min(255, tint.R * k), (int)Math.Min(255, tint.G * k),
                    (int)Math.Min(255, tint.B * k), (int)tint.A);
            }
            p.Tint = tint;
        }

        /// <summary>Moves every live particle on by <paramref name="elapsed"/> seconds.</summary>
        public void Run(float elapsed)
        {
            int alive = 0;
            Vector2 wind = Wind;

            for (int i = 0; i < particles.Length; i++)
            {
                ref Particle p = ref particles[i];
                if (p.Spray == null || p.Age >= p.Life)
                    continue;

                p.Age += elapsed;
                if (p.Age >= p.Life)
                    continue;

                ParticleSprayType s = p.Spray;
                p.Dx *= Math.Max(0f, 1f - s.Drag * elapsed);
                p.Dy *= Math.Max(0f, 1f - s.Drag * elapsed);
                p.Dx += wind.X * s.WindInfluence * elapsed;
                p.Dy += (s.Gravity + wind.Y * s.WindInfluence) * elapsed;

                p.X += p.Dx * elapsed;
                p.Y += p.Dy * elapsed;
                p.Size += p.SizeInc * elapsed;
                p.Angle += p.AngleInc * elapsed;

                if (p.Size <= 0f)
                    p.Age = p.Life; // shrunk away
                else
                    alive++;
            }
            live = alive;
        }

        /// <summary>Draws every live particle. Call between SpriteBatch.Begin and End.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (live == 0)
                return;

            for (int i = 0; i < particles.Length; i++)
            {
                ref Particle p = ref particles[i];
                if (p.Spray == null || p.Age >= p.Life || p.Size <= 0f)
                    continue;

                ParticleSprayType s = p.Spray;
                Texture2D texture = s.Texture;
                if (texture == null)
                    continue;

                float t = p.Age / p.Life;
                float alpha = s.Opacity;
                if (t > s.FadeStart)
                {
                    float u = (t - s.FadeStart) / Math.Max(.0001f, 1f - s.FadeStart);
                    alpha *= s.FadePower == 1f ? 1f - u : (float)Math.Pow(1f - u, s.FadePower);
                }

                // Premultiplied colour; alpha 0 with colour left in makes the sprite additive
                Color color = p.Tint * alpha;
                if (s.Additive)
                    color.A = 0;

                int tileW = s.TileWidth > 0 ? s.TileWidth : texture.Width;
                int tileH = s.TileHeight > 0 ? s.TileHeight : texture.Height;
                Rectangle? source = null;
                if (s.BlockCount > 0 || s.TilesPerRow > 1)
                {
                    int perRow = Math.Max(1, s.TilesPerRow);
                    source = new Rectangle((p.Tile % perRow) * tileW, (p.Tile / perRow) * tileH, tileW, tileH);
                }

                spriteBatch.Draw(texture, new Vector2(p.X, p.Y), source, color, p.Angle,
                    new Vector2(tileW / 2f, tileH / 2f), p.Size / tileW, SpriteEffects.None, 0f);
            }
        }
    }
}
