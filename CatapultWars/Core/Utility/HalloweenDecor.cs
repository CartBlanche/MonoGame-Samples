using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// The animated Halloween extras: night sky and moon, bats, lit windows and lanterns.
    /// Purely visual; it never touches game state.
    /// </summary>
    public class HalloweenDecor
    {
        // Multiply tints that turn the daytime layers into dusk.
        public static readonly Color CloudTint = new Color(130, 100, 170);
        public static readonly Color MountainTint = new Color(110, 80, 150);
        public static readonly Color ForegroundTint = new Color(100, 80, 140);

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
        class Bat { public float X, BaseY, Scale, Speed, Phase, Amplitude, Flee, Up; }

        readonly HalloweenArt art;
        readonly Star[] stars = new Star[80];
        readonly Bat[] bats = new Bat[9];
        float time;

        public HalloweenDecor(GraphicsDevice device)
        {
            art = HalloweenArt.Get(device);

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

            for (int i = 0; i < bats.Length; i++)
            {
                bats[i] = new Bat
                {
                    X = (float)rand.NextDouble() * Width,
                    BaseY = 50f + (float)rand.NextDouble() * 190f,
                    Scale = .5f + (float)rand.NextDouble() * .55f,
                    Speed = (35f + (float)rand.NextDouble() * 45f) * (rand.NextDouble() < .5 ? -1f : 1f),
                    Phase = (float)rand.NextDouble() * 6f,
                    Amplitude = 12f + (float)rand.NextDouble() * 22f,
                };
            }

            // One bat flies across the moon.
            bats[0].BaseY = 88f;
            bats[0].Scale = .9f;
        }

        public Texture2D Pumpkin
        {
            get { return art.Pumpkin; }
        }

        public void Update(float elapsed)
        {
            time += elapsed;

            foreach (Bat bat in bats)
            {
                float boost = bat.Flee > 0f ? 3f : 1f;
                bat.X += bat.Speed * boost * elapsed;
                bat.Phase += elapsed * (bat.Flee > 0f ? 16f : 9f);
                bat.Flee = Math.Max(0f, bat.Flee - elapsed);
                bat.Up = bat.Flee > 0f ? Math.Min(bat.Up + 60f * elapsed, 50f) : Math.Max(bat.Up - 20f * elapsed, 0f);

                if (bat.X > Width + 40f)
                    bat.X = -40f;
                else if (bat.X < -40f)
                    bat.X = Width + 40f;
            }
        }

        /// <summary>
        /// Startles the bats, for when a shot lands.
        /// </summary>
        public void Scatter()
        {
            foreach (Bat bat in bats)
                bat.Flee = 1.2f;
        }

        /// <summary>
        /// Night sky, twinkling stars and the harvest moon, replacing the daytime sky.
        /// </summary>
        public void DrawSky(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(art.NightSky, new Rectangle(0, 0, (int)Width, (int)Height), Color.White);

            foreach (Star star in stars)
            {
                float alpha = .45f + .45f * (float)Math.Sin(time * 2f + star.Phase);
                spriteBatch.Draw(art.Pixel,
                    new Rectangle((int)star.Position.X, (int)star.Position.Y, (int)star.Size, (int)star.Size),
                    new Color(255, 245, 220) * alpha);
            }

            DrawMoon(spriteBatch, new Vector2(640, 92), 38f, 1f);
        }

        /// <summary>
        /// Flickering windows and jack-o'-lanterns for the battlefield.
        /// </summary>
        public void DrawGameLights(SpriteBatch spriteBatch)
        {
            DrawLights(spriteBatch, GameWindows, GameLanterns, 1f);
        }

        /// <summary>
        /// Dusk wash, moon, flickering windows and lanterns for the title screen.
        /// </summary>
        public void DrawTitleOverlay(SpriteBatch spriteBatch, float alpha)
        {
            spriteBatch.Draw(art.TitleShade, new Rectangle(0, 0, (int)Width, (int)Height), Color.White * alpha);
            DrawMoon(spriteBatch, new Vector2(700, 108), 40f, alpha);
            DrawLights(spriteBatch, TitleWindows, TitleLanterns, alpha);
        }

        public void DrawBats(SpriteBatch spriteBatch, float alpha)
        {
            foreach (Bat bat in bats)
            {
                int frame = (int)((Math.Sin(bat.Phase) * .5 + .5) * (HalloweenArt.BatFrameCount - 1) + .5);
                Vector2 position = new Vector2(
                    bat.X,
                    bat.BaseY + (float)Math.Sin(bat.Phase * .35f + bat.X * .01f) * bat.Amplitude - bat.Up);

                spriteBatch.Draw(art.BatFrames[frame], position, null, Color.White * alpha, 0f,
                    HalloweenArt.BatOrigin, bat.Scale, SpriteEffects.None, 0f);
            }
        }

        void DrawMoon(SpriteBatch spriteBatch, Vector2 centre, float radius, float alpha)
        {
            // Alpha 0 with colour makes a premultiplied sprite additive, which gives the glow.
            float glowSize = radius * 6.4f;
            spriteBatch.Draw(art.Glow, centre, null, new Color(255, 170, 60, 0) * (.45f * alpha), 0f,
                new Vector2(32, 32), glowSize / 64f, SpriteEffects.None, 0f);
            spriteBatch.Draw(art.Moon, centre, null, Color.White * alpha, 0f,
                new Vector2(40, 40), radius / 38f, SpriteEffects.None, 0f);
        }

        void DrawLights(SpriteBatch spriteBatch, Rectangle[] windows, Vector2[] lanterns, float alpha)
        {
            for (int i = 0; i < windows.Length; i++)
            {
                float f = Flicker(i) * alpha;
                Rectangle w = windows[i];
                Vector2 centre = new Vector2(w.Center.X, w.Center.Y);

                spriteBatch.Draw(art.Glow, centre, null, new Color(255, 170, 40, 0) * (.8f * f), 0f,
                    new Vector2(32, 32), (Math.Max(w.Width, w.Height) * 3f) / 64f, SpriteEffects.None, 0f);
                spriteBatch.Draw(art.Pixel, w, new Color(255, 190, 70) * f);
            }

            for (int i = 0; i < lanterns.Length; i++)
            {
                float f = Flicker(windows.Length + i) * alpha;
                Vector2 c = lanterns[i];

                spriteBatch.Draw(art.Glow, c, null, new Color(255, 170, 50, 0) * (.5f * f), 0f,
                    new Vector2(32, 32), 92f / 64f, SpriteEffects.None, 0f);
                spriteBatch.Draw(art.Pumpkin, new Rectangle((int)c.X - 15, (int)c.Y - 14, 30, 28), Color.White * alpha);
            }
        }

        float Flicker(int index)
        {
            float jitter = ((index * 7919 + (int)(time * 30f)) % 10) / 100f - .05f;
            return Math.Max(.4f, .8f + .2f * (float)Math.Sin(time * 9f + index * 1.7f) + jitter);
        }
    }
}
