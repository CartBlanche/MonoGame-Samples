using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>One kind of wind-blown particle, e.g. near and far snowflakes.</summary>
    public sealed class WindParticleLayer
    {
        /// <summary>How often this layer is picked, relative to the others.</summary>
        public float Share { get; init; } = 1f;
        public float Size { get; init; } = 3f;
        public float SizeRandom { get; init; }
        /// <summary>Fall speed (pixels per second, with Drag 1). Negative rises, such as embers, and starts at the bottom.</summary>
        public float Gravity { get; init; } = 40f;
        public float Drag { get; init; } = 1f;
        public float WindInfluence { get; init; } = 1f;
        public float Opacity { get; init; } = 1f;
        public Color Tint { get; init; } = Color.White;
        public float Life { get; init; } = 10f;
        public float LifeRandom { get; init; }
        /// <summary>Spin in radians per second.</summary>
        public float Spin { get; init; }
        public bool Additive { get; init; }
    }

    /// <summary>
    /// What a theme carries on the wind: how many particles, what they look like and how hard the
    /// wind pushes them.
    /// </summary>
    public sealed class WindParticleSpec
    {
        /// <summary>The sprite; null uses a soft round dot.</summary>
        public Func<GraphicsDevice, Texture2D> Art { get; init; }
        /// <summary>New particles per second across the whole screen.</summary>
        public float Rate { get; init; } = 15f;
        /// <summary>Wind acceleration in pixels per second squared for each unit of wind (0 to 10).</summary>
        public float WindPush { get; init; } = 10f;
        public WindParticleLayer[] Layers { get; init; }
    }

    /// <summary>
    /// Snow, leaves or embers drifting across the battlefield with the wind. Purely visual and local:
    /// each machine runs its own, pushed by the same wind value that moves the shots. Built on the
    /// particle engine, so it allocates nothing while playing.
    /// </summary>
    public sealed class WindEffect
    {
        const float Width = 800f, Height = 480f;

        readonly WindParticleSpec spec;
        readonly ParticleEngine engine;
        readonly ParticleSprayType[] sprays;
        readonly float[] cumulativeShare;
        readonly Random random = Battlefield.MakeRandom();
        float carry;

        public WindEffect(GraphicsDevice device, ThemeDefinition theme)
        {
            spec = theme.WindParticles;
            ThemeArt art = ThemeArt.Get(device);
            Texture2D sprite = art.For(theme).WindParticle ?? art.SoftDisc;

            float longest = 1f;
            sprays = new ParticleSprayType[spec.Layers.Length];
            cumulativeShare = new float[sprays.Length];
            float total = 0f;
            for (int i = 0; i < sprays.Length; i++)
            {
                WindParticleLayer l = spec.Layers[i];
                sprays[i] = new ParticleSprayType
                {
                    Texture = sprite,
                    Gravity = l.Gravity,
                    Drag = l.Drag,
                    WindInfluence = l.WindInfluence,
                    Size = l.Size,
                    SizeRandom = l.SizeRandom,
                    AngleRandom = MathHelper.TwoPi,
                    AngleInc = l.Spin,
                    Tint = l.Tint,
                    Opacity = l.Opacity,
                    LifeTime = l.Life,
                    LifeTimeRandom = l.LifeRandom,
                    FadeStart = .85f,
                    Additive = l.Additive,
                };
                longest = Math.Max(longest, l.Life + l.LifeRandom);
                total += l.Share;
                cumulativeShare[i] = total;
            }

            engine = new ParticleEngine((int)(spec.Rate * longest * 1.25f) + 16, random);

            // Start with the air already full rather than filling up over the first seconds
            for (float t = 0f; t < longest; t += .1f)
                Update(.1f, 0f);
        }

        /// <param name="wind">Signed wind, the same value the shots use: negative blows left.</param>
        public void Update(float elapsed, float wind)
        {
            engine.Wind = new Vector2(wind * spec.WindPush, 0f);

            carry += spec.Rate * elapsed;
            while (carry >= 1f)
            {
                carry -= 1f;
                Emit(wind);
            }
            engine.Run(elapsed);
        }

        void Emit(float wind)
        {
            float pick = (float)random.NextDouble() * cumulativeShare[cumulativeShare.Length - 1];
            int layer = 0;
            while (layer < cumulativeShare.Length - 1 && pick > cumulativeShare[layer])
                layer++;

            // Start upwind, so by the time the wind has carried them they cross the screen. Anything
            // that falls starts at the top, anything that rises at the bottom.
            float x = -100f + (float)random.NextDouble() * (Width + 200f) - wind * 50f;
            bool rises = spec.Layers[layer].Gravity < 0f;
            float y = rises ? Height + 10f : -10f;
            engine.Spray(sprays[layer], x, y, 0f, 0f, rises ? -20f : 20f, 0f, 1);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            engine.Draw(spriteBatch);
        }
    }
}
