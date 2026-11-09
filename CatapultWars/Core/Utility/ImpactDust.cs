using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// A small burst of puffs and chunks where a projectile lands. How much there is, and
    /// how far it flies, scales with the speed of the impact; the colours follow the theme.
    /// The particles live in a <see cref="ParticleEngine"/>, so a burst allocates nothing.
    /// </summary>
    public class ImpactDust
    {
        // Impact speeds seen in play run from roughly 200 (weak shot) to 600 (full power).
        const float SlowSpeed = 200f, FastSpeed = 600f;

        static readonly Random random = Battlefield.MakeRandom();

        readonly ParticleEngine engine = new ParticleEngine(96);
        readonly ParticleSprayType puff, chunk;

        public ImpactDust(GraphicsDevice device)
        {
            ThemeArt art = ThemeArt.Get(device);

            // Soft rising puffs that swell and thin out
            puff = new ParticleSprayType
            {
                Texture = art.SoftDisc,
                Drag = 2.5f,
                Gravity = -15f,
                TintVariation = .15f,
                Opacity = .7f,
                FadeStart = 0f,
                FadePower = 1.5f,
            };

            // Hard little chunks that fall back down
            chunk = new ParticleSprayType
            {
                Texture = art.HardDisc,
                Drag = .3f,
                Gravity = 500f,
                TintVariation = .15f,
                FadeStart = .45f,
            };
        }

        /// <summary>
        /// True while any particle is still visible.
        /// </summary>
        public bool IsActive
        {
            get { return engine.IsActive; }
        }

        /// <summary>
        /// Throws up a burst at a landing point.
        /// </summary>
        /// <param name="speed">Speed of the projectile at impact.</param>
        /// <param name="forward">+1 or -1: the way the projectile was travelling.</param>
        public void Burst(Vector2 position, float speed, int forward)
        {
            float power = MathHelper.Clamp((speed - SlowSpeed) / (FastSpeed - SlowSpeed), 0f, 1f);

            // The theme can change between bursts, so the colours are set each time
            ThemeDefinition theme = Theme.Active;
            puff.Tint = theme.ImpactPuff;
            chunk.Tint = theme.ImpactChunk;

            int puffs = 6 + (int)(power * 10);
            for (int i = 0; i < puffs; i++)
            {
                float angle = -MathHelper.PiOver2 + (Next() - .5f) * MathHelper.Pi * .9f;
                float v = (25f + Next() * 60f) * (.6f + power);
                float size = 8f + Next() * 8f;
                float life = .5f + Next() * .4f + power * .3f;
                engine.Add(puff,
                    position.X + (Next() - .5f) * 14f, position.Y + (Next() - .5f) * 6f,
                    (float)Math.Cos(angle) * v + forward * power * 50f, (float)Math.Sin(angle) * v,
                    life, size, size * (1.6f + power) / life);
            }

            int chunks = 5 + (int)(power * 14);
            for (int i = 0; i < chunks; i++)
            {
                float angle = -MathHelper.PiOver2 + (Next() - .5f) * 1.7f;
                float v = (70f + Next() * 190f) * (.5f + power);
                engine.Add(chunk,
                    position.X + (Next() - .5f) * 8f, position.Y,
                    (float)Math.Cos(angle) * v + forward * power * 90f, (float)Math.Sin(angle) * v,
                    .45f + Next() * .35f, 3f + Next() * 3f, 0f);
            }
        }

        public void Update(float elapsed)
        {
            engine.Run(elapsed);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            engine.Draw(spriteBatch);
        }

        static float Next()
        {
            return (float)random.NextDouble();
        }
    }
}
