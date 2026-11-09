using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// The animated extras of a theme: sky, stars, sun or moon, flyers, lit windows and lanterns.
    /// Purely visual; it never touches game state. All textures come from <see cref="ThemeArt"/>,
    /// and drawing allocates nothing.
    /// </summary>
    public class ThemeDecor
    {
        const float Width = 800f, Height = 480f;

        // Castle windows in the gameplay foreground layer (x, y, w, h).
        static readonly Rectangle[] GameWindows =
        {
            new Rectangle(144, 213, 20, 26),
            new Rectangle(152, 292, 15, 16),
            new Rectangle(229, 303, 28, 32),
        };

        // Castle windows in the title screen art.
        static readonly Rectangle[] TitleWindows =
        {
            new Rectangle(388, 188, 22, 28),
            new Rectangle(478, 236, 20, 22),
            new Rectangle(374, 276, 24, 30),
        };

        // Lantern centres: two at the edges of the battlefield, several resting on the title hill.
        static readonly Vector2[] GameLanterns = { new Vector2(40, 372), new Vector2(760, 372) };
        static readonly Vector2[] TitleLanterns =
        {
            new Vector2(58, 408), new Vector2(215, 350), new Vector2(592, 326),
            new Vector2(665, 348), new Vector2(735, 384),
        };

        class Star { public Vector2 Position; public float Size; public float Phase; }
        class Flyer { public float X, BaseY, Scale, Speed, Phase, Amplitude, Flee, Up; }

        readonly ThemeDefinition theme;
        readonly ThemeArt art;
        readonly ThemeTextures textures;
        readonly Star[] stars;
        readonly Flyer[] flyers;
        float time;

        public ThemeDecor(GraphicsDevice device, ThemeDefinition theme)
        {
            this.theme = theme;
            art = ThemeArt.Get(device);
            textures = art.For(theme);
            stars = new Star[theme.StarCount];
            flyers = new Flyer[textures.Flyers != null ? theme.FlyerCount : 0];

            // Fixed seed so the sky looks the same every run.
            Random rand = new Random(7);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = new Star
                {
                    Position = new Vector2((float)rand.NextDouble() * Width, (float)rand.NextDouble() * 300f),
                    Size = rand.NextDouble() < .25 ? 2f : 1f,
                    Phase = (float)rand.NextDouble() * 6f,
                };
            }

            for (int i = 0; i < flyers.Length; i++)
            {
                flyers[i] = new Flyer
                {
                    X = (float)rand.NextDouble() * Width,
                    BaseY = 50f + (float)rand.NextDouble() * 190f,
                    Scale = .5f + (float)rand.NextDouble() * .55f,
                    Speed = (35f + (float)rand.NextDouble() * 45f) * (rand.NextDouble() < .5 ? -1f : 1f),
                    Phase = (float)rand.NextDouble() * 6f,
                    Amplitude = 12f + (float)rand.NextDouble() * 22f,
                };
            }

            // One flyer crosses the sun or moon.
            if (flyers.Length > 0 && textures.Body != null)
            {
                flyers[0].BaseY = 88f;
                flyers[0].Scale = .9f;
            }
        }

        /// <summary>The theme's projectile art, which the HUD also uses as its ammo icon.</summary>
        public Texture2D ProjectileArt
        {
            get { return textures.Projectile; }
        }

        public void Update(float elapsed)
        {
            time += elapsed;

            foreach (Flyer f in flyers)
            {
                float boost = f.Flee > 0f ? 3f : 1f;
                f.X += f.Speed * boost * elapsed;
                f.Phase += elapsed * (f.Flee > 0f ? 16f : 9f);
                f.Flee = Math.Max(0f, f.Flee - elapsed);
                f.Up = f.Flee > 0f ? Math.Min(f.Up + 60f * elapsed, 50f) : Math.Max(f.Up - 20f * elapsed, 0f);

                if (f.X > Width + 40f)
                    f.X = -40f;
                else if (f.X < -40f)
                    f.X = Width + 40f;
            }
        }

        /// <summary>
        /// Startles the flyers, for when a shot lands.
        /// </summary>
        public void Scatter()
        {
            foreach (Flyer f in flyers)
                f.Flee = 1.2f;
        }

        /// <summary>
        /// The theme's sky, twinkling stars and sun or moon, replacing the daytime sky.
        /// </summary>
        public void DrawSky(SpriteBatch spriteBatch)
        {
            if (textures.Sky != null)
                spriteBatch.Draw(textures.Sky, new Rectangle(0, 0, (int)Width, (int)Height), Color.White);

            foreach (Star star in stars)
            {
                float alpha = .45f + .45f * (float)Math.Sin(time * 2f + star.Phase);
                spriteBatch.Draw(art.Pixel,
                    new Rectangle((int)star.Position.X, (int)star.Position.Y, (int)star.Size, (int)star.Size),
                    theme.StarColor * alpha);
            }

            DrawBody(spriteBatch, theme.BodyGamePosition, theme.BodyGameRadius, 1f);
        }

        /// <summary>
        /// Flickering windows and lanterns for the battlefield.
        /// </summary>
        public void DrawGameLights(SpriteBatch spriteBatch)
        {
            DrawLights(spriteBatch, GameWindows, GameLanterns, 1f);
        }

        /// <summary>
        /// Wash, sun or moon, flickering windows and lanterns for the title screen.
        /// </summary>
        public void DrawTitleOverlay(SpriteBatch spriteBatch, float alpha)
        {
            if (textures.TitleShade != null)
                spriteBatch.Draw(textures.TitleShade, new Rectangle(0, 0, (int)Width, (int)Height), Color.White * alpha);
            DrawBody(spriteBatch, theme.BodyTitlePosition, theme.BodyTitleRadius, alpha);
            DrawLights(spriteBatch, TitleWindows, TitleLanterns, alpha);
        }

        public void DrawFlyers(SpriteBatch spriteBatch, float alpha)
        {
            Texture2D[] frames = textures.Flyers;
            foreach (Flyer f in flyers)
            {
                int frame = (int)((Math.Sin(f.Phase) * .5 + .5) * (frames.Length - 1) + .5);
                Vector2 position = new Vector2(
                    f.X,
                    f.BaseY + (float)Math.Sin(f.Phase * .35f + f.X * .01f) * f.Amplitude - f.Up);

                spriteBatch.Draw(frames[frame], position, null, Color.White * alpha, 0f,
                    theme.FlyerOrigin, f.Scale, SpriteEffects.None, 0f);
            }
        }

        void DrawBody(SpriteBatch spriteBatch, Vector2 centre, float radius, float alpha)
        {
            if (textures.Body == null)
                return;

            // Alpha 0 with colour makes a premultiplied sprite additive, which gives the glow.
            float glowSize = radius * 6.4f;
            spriteBatch.Draw(art.Glow, centre, null, theme.BodyGlow * (theme.BodyGlowAlpha * alpha), 0f,
                new Vector2(32, 32), glowSize / 64f, SpriteEffects.None, 0f);
            spriteBatch.Draw(textures.Body, centre, null, Color.White * alpha, 0f,
                theme.BodyOrigin, radius / theme.BodyArtRadius, SpriteEffects.None, 0f);
        }

        void DrawLights(SpriteBatch spriteBatch, Rectangle[] windows, Vector2[] lanterns, float alpha)
        {
            if (theme.LitWindows)
            {
                for (int i = 0; i < windows.Length; i++)
                {
                    float f = Flicker(i) * alpha;
                    Rectangle w = windows[i];
                    Vector2 centre = new Vector2(w.Center.X, w.Center.Y);

                    spriteBatch.Draw(art.Glow, centre, null, theme.WindowGlow * (.8f * f), 0f,
                        new Vector2(32, 32), (Math.Max(w.Width, w.Height) * 3f) / 64f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(art.Pixel, w, theme.WindowColor * f);
                }
            }

            if (textures.Prop == null)
                return;

            for (int i = 0; i < lanterns.Length; i++)
            {
                float f = Flicker(windows.Length + i) * alpha;
                Vector2 c = lanterns[i];

                spriteBatch.Draw(art.Glow, c, null, theme.PropGlow * (.5f * f), 0f,
                    new Vector2(32, 32), theme.PropGlowSize / 64f, SpriteEffects.None, 0f);
                spriteBatch.Draw(textures.Prop,
                    new Rectangle((int)c.X - theme.PropSize.X / 2, (int)c.Y - theme.PropSize.Y / 2,
                        theme.PropSize.X, theme.PropSize.Y), Color.White * alpha);
            }
        }

        float Flicker(int index)
        {
            float jitter = ((index * 7919 + (int)(time * 30f)) % 10) / 100f - .05f;
            return Math.Max(.4f, .8f + .2f * (float)Math.Sin(time * 9f + index * 1.7f) + jitter);
        }
    }
}
