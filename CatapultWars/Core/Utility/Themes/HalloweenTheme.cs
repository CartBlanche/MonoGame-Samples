using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CatapultGame.ShapeArt;

namespace CatapultGame
{
    /// <summary>
    /// The Halloween look: dusk sky and harvest moon, pumpkins, bats and lit windows.
    /// Palette and art only; <see cref="ThemeDecor"/> and the rest of the game draw it.
    /// </summary>
    static class HalloweenTheme
    {
        public const int BatFrameCount = 6;
        static readonly Vector2 BatOrigin = new Vector2(26, 18);

        public static readonly ThemeDefinition Definition = new ThemeDefinition
        {
            Id = Season.Halloween,
            Name = "halloween",
            StartMonth = 10, StartDay = 24, EndMonth = 11, EndDay = 1,
            IsStyled = true,

            // Dusk gradient: deep indigo at the top, plum, then an orange horizon
            SkyStops = new[] { new Color(13, 10, 36), new Color(42, 18, 80), new Color(122, 47, 90), new Color(224, 112, 42) },
            SkyAt = new[] { 0f, .4f, .78f, 1f },
            SkyHeight = 390f,

            CloudTint = new Color(130, 100, 170),
            MountainTint = new Color(110, 80, 150),
            ForegroundTint = new Color(100, 80, 140),

            // Cool and dark at the top of the title, warm at the bottom
            TitleTop = new Color(20, 10, 50),
            TitleBottom = new Color(255, 110, 20),
            TitleAlphaTop = .35f,
            TitleAlphaBottom = .26f,

            StarCount = 80,
            StarColor = new Color(255, 245, 220),

            BodyArt = BuildMoon,
            BodyOrigin = new Vector2(40, 40),
            BodyArtRadius = 38f,
            BodyGamePosition = new Vector2(640, 92),
            BodyGameRadius = 38f,
            BodyTitlePosition = new Vector2(700, 108),
            BodyTitleRadius = 40f,
            BodyGlow = new Color(255, 170, 60, 0),
            BodyGlowAlpha = .45f,

            ProjectileArt = BuildPumpkin,

            ImpactPuff = new Color(255, 140, 40),
            ImpactChunk = new Color(210, 85, 15),

            LitWindows = true,
            WindowColor = new Color(255, 190, 70),
            WindowGlow = new Color(255, 170, 40, 0),
            PropArt = BuildPumpkin,
            PropSize = new Point(30, 28),
            PropGlow = new Color(255, 170, 50, 0),
            PropGlowSize = 92f,

            // Embers drifting up from the ground, pushed along by the wind
            WindParticles = new WindParticleSpec
            {
                Rate = 14f,
                WindPush = 10f,
                Layers = new[]
                {
                    new WindParticleLayer
                    {
                        Share = 1f, Size = 3f, SizeRandom = 2f, Gravity = -24f, Opacity = .85f,
                        Tint = new Color(255, 150, 50), Life = 10f, LifeRandom = 4f, Additive = true,
                    },
                    new WindParticleLayer
                    {
                        Share = .35f, Size = 7f, SizeRandom = 3f, Gravity = -16f, Opacity = .28f,
                        Tint = new Color(255, 110, 30), Life = 9f, LifeRandom = 3f, Additive = true,
                    },
                },
            },

            // The same reindeer on the far hill, gone dusky, with its antlers on fire
            MascotArt = gd => ReindeerArt.Build(gd, ReindeerArt.Haunted),
            MascotFeet = new Vector2(520, 311),
            MascotOrigin = new Vector2(28, 48),
            MascotScale = .75f,
            MascotRotation = MathHelper.ToRadians(-15f),
            MascotFlameArt = BuildFlame,
            MascotFlameGlow = new Color(255, 120, 30, 0),
            // Antler tips, measured from the origin of each frame: head up has three, grazing has two
            MascotFlames = new[]
            {
                new[] { new Vector2(-13f, -46f), new Vector2(-18.5f, -45f), new Vector2(-9.5f, -47f) },
                new[] { new Vector2(-11f, -26.5f), new Vector2(-17.5f, -25.5f) },
            },

            FlyerArt = BuildBats,
            FlyerOrigin = BatOrigin,
            FlyerCount = 9,
        };

        // A small teardrop flame: orange outside, gold inside and a white-hot heart. Its base is at the bottom.
        static Texture2D BuildFlame(GraphicsDevice gd)
        {
            return Render(gd, 12, 18,
                (Union(Ellipse(6, 12.2f, 5f, 5.6f), Poly(new Vector2(1.3f, 10.5f), new Vector2(6.4f, .2f), new Vector2(10.7f, 10.5f))), new Color(255, 104, 18)),
                (Union(Ellipse(6, 13f, 3.3f, 4f), Poly(new Vector2(3f, 12f), new Vector2(6.2f, 4.5f), new Vector2(9f, 12f))), new Color(255, 186, 52)),
                (Ellipse(6, 14.2f, 1.8f, 2.5f), new Color(255, 244, 180)));
        }

        static Shape Union(Shape a, Shape b)
        {
            return (x, y) => a(x, y) || b(x, y);
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

        static Texture2D[] BuildBats(GraphicsDevice gd)
        {
            Texture2D[] frames = new Texture2D[BatFrameCount];
            for (int i = 0; i < BatFrameCount; i++)
            {
                // Wings sweep from fully up (+10) to fully down (-10).
                float wing = 10f - 20f * i / (BatFrameCount - 1);
                frames[i] = BuildBat(gd, wing);
            }
            return frames;
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
    }
}
