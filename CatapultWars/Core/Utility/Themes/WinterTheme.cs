using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CatapultGame.ShapeArt;

namespace CatapultGame
{
    /// <summary>
    /// The winter look, active all of December: a pale dusk sky, snow-white hills with snow lying
    /// on the ground, snowballs for ammunition and warm lit windows. Palette and art only.
    /// </summary>
    static class WinterTheme
    {
        public static readonly ThemeDefinition Definition = new ThemeDefinition
        {
            Id = Season.Winter,
            Name = "winter",
            StartMonth = 12, StartDay = 1, EndMonth = 12, EndDay = 31,
            IsStyled = true,

            // Pale winter dusk: slate blue overhead, lilac, then a peach glow on the horizon
            SkyStops = new[] { new Color(62, 80, 138), new Color(116, 126, 178), new Color(188, 172, 202), new Color(246, 212, 194) },
            SkyAt = new[] { 0f, .4f, .75f, 1f },
            SkyHeight = 390f,

            // The daytime layers are frosted toward snow first, then tinted a touch blue
            FrostColor = new Color(244, 248, 255),
            FrostClouds = .85f,
            FrostMountain = .6f,
            FrostForeground = .72f,
            FrostTitle = .8f,
            CloudTint = new Color(232, 236, 252),
            MountainTint = new Color(226, 232, 250),
            ForegroundTint = new Color(236, 240, 255),

            // Snow on every column; a crater shows brown earth, ringed by what snow is left
            SnowCap = new Color(246, 249, 255),
            SnowCapThickness = 7f,
            CraterColor = new Color(122, 88, 54),

            TitleTop = new Color(40, 58, 120),
            TitleBottom = new Color(255, 205, 175),
            TitleAlphaTop = .30f,
            TitleAlphaBottom = .18f,

            StarCount = 60,
            StarColor = new Color(228, 236, 255),

            BodyArt = BuildMoon,
            BodyOrigin = new Vector2(40, 40),
            BodyArtRadius = 38f,
            BodyGamePosition = new Vector2(640, 92),
            BodyGameRadius = 34f,
            BodyTitlePosition = new Vector2(700, 108),
            BodyTitleRadius = 36f,
            BodyGlow = new Color(190, 212, 255, 0),
            BodyGlowAlpha = .4f,

            ProjectileArt = BuildSnowball,

            ImpactPuff = new Color(238, 243, 252),
            ImpactChunk = new Color(205, 218, 240),

            LitWindows = true,
            WindowColor = new Color(255, 200, 110),
            WindowGlow = new Color(255, 178, 72, 0),

            // Christmas lights on the trees, red, green, gold and blue in turn
            TreeLightColors = new[]
            {
                new Color(255, 70, 60), new Color(90, 230, 110), new Color(255, 210, 80), new Color(90, 160, 255),
            },

            // Santa hats for both crews
            CrewHat = new CrewHat
            {
                Body = new Color(206, 40, 44),
                Trim = new Color(246, 248, 252),
                Bobble = new Color(246, 248, 252),
            },

            // A red-nosed reindeer in the woods, to the right of the middle
            MascotArt = BuildReindeer,
            MascotFeet = new Vector2(548, 384),
            MascotOrigin = new Vector2(28, 48),
            MascotScale = .85f,
            MascotGlowOffsets = new[] { new Vector2(-23.5f, -33.5f), new Vector2(-24f, -13f) },
            MascotGlow = new Color(255, 40, 30, 0),
            MascotGlowSize = 26f,

            // Snow drifting on the wind, small and slow far away, bigger and quicker near
            WindParticles = new WindParticleSpec
            {
                Rate = 22f,
                WindPush = 10f,
                Layers = new[]
                {
                    new WindParticleLayer
                    {
                        Share = 1f, Size = 3.2f, SizeRandom = 1.4f, Gravity = 36f, Opacity = .8f,
                        Tint = new Color(224, 234, 255), Life = 12f, LifeRandom = 3f,
                    },
                    new WindParticleLayer
                    {
                        Share = .6f, Size = 5f, SizeRandom = 2f, Gravity = 60f, Opacity = .95f,
                        Tint = new Color(250, 252, 255), Life = 8f, LifeRandom = 2f,
                    },
                },
            },
        };

        static Texture2D BuildMoon(GraphicsDevice gd)
        {
            Color disc = new Color(238, 243, 255), crater = new Color(208, 216, 238);
            return Render(gd, 80, 80,
                (Ellipse(40, 40, 38, 38), disc),
                (Ellipse(40 - 38 * .3f, 40 - 38 * .2f, 38 * .22f, 38 * .22f), crater),
                (Ellipse(40 + 38 * .25f, 40 + 38 * .15f, 38 * .3f, 38 * .3f), crater),
                (Ellipse(40 - 38 * .05f, 40 + 38 * .45f, 38 * .14f, 38 * .14f), crater));
        }

        // The reindeer faces left. Frame 0 has its head up, frame 1 bends down to graze.
        static Texture2D[] BuildReindeer(GraphicsDevice gd)
        {
            return new[] { Reindeer(gd, false), Reindeer(gd, true) };
        }

        static Texture2D Reindeer(GraphicsDevice gd, bool grazing)
        {
            Color fur = new Color(150, 102, 62), dark = new Color(112, 76, 46), belly = new Color(214, 178, 138);
            Color horn = new Color(222, 196, 150), nose = new Color(226, 34, 34), ink = new Color(40, 26, 18);

            var layers = new System.Collections.Generic.List<(Shape, Color)>();

            // Legs, then the body over them
            layers.Add((Capsule(new Vector2(22, 32), new Vector2(21, 46), 1.5f), dark));
            layers.Add((Capsule(new Vector2(26.5f, 33), new Vector2(26.5f, 46), 1.5f), dark));
            layers.Add((Capsule(new Vector2(35, 33), new Vector2(36, 46), 1.5f), dark));
            layers.Add((Capsule(new Vector2(39, 32), new Vector2(40, 46), 1.5f), dark));
            layers.Add((Ellipse(30.5f, 28, 13.5f, 7.5f), fur));
            layers.Add((Ellipse(30.5f, 31.5f, 9.5f, 3.6f), belly));
            layers.Add((Ellipse(44, 24, 2.6f, 2.2f), Color.White));

            Vector2 head;
            if (!grazing)
            {
                head = new Vector2(9, 13.5f);
                layers.Add((Capsule(new Vector2(20, 25), new Vector2(12, 15), 3.4f), fur));
                layers.Add((Ellipse(head.X, head.Y, 5.4f, 3.5f), fur));
                layers.Add((Ellipse(13.5f, 9.6f, 1.6f, 2.4f), dark));
                layers.Add((Ellipse(8.6f, 12.2f, .9f, .9f), ink));
                layers.Add((Capsule(new Vector2(11.5f, 10), new Vector2(15, 2), .9f), horn));
                layers.Add((Capsule(new Vector2(13.4f, 6), new Vector2(9.5f, 3), .8f), horn));
                layers.Add((Capsule(new Vector2(14.6f, 3.4f), new Vector2(18.5f, 1), .8f), horn));
            }
            else
            {
                head = new Vector2(8.5f, 34);
                layers.Add((Capsule(new Vector2(20, 25), new Vector2(12, 31), 3.4f), fur));
                layers.Add((Ellipse(head.X, head.Y, 5.4f, 3.5f), fur));
                layers.Add((Ellipse(13f, 30.6f, 1.6f, 2.4f), dark));
                layers.Add((Ellipse(8.2f, 32.6f, .9f, .9f), ink));
                layers.Add((Capsule(new Vector2(12.5f, 29), new Vector2(17, 21.5f), .9f), horn));
                layers.Add((Capsule(new Vector2(14.5f, 25.5f), new Vector2(10.5f, 22.5f), .8f), horn));
            }

            // The nose, with a little shine
            Vector2 tip = head + new Vector2(-5f, 1.3f);
            layers.Add((Ellipse(tip.X, tip.Y, 2.3f, 2.3f), nose));
            layers.Add((Ellipse(tip.X - .7f, tip.Y - .8f, .7f, .7f), new Color(255, 170, 160)));
            return Render(gd, 56, 50, layers.ToArray());
        }

        // A thick line with round ends
        static Shape Capsule(Vector2 a, Vector2 b, float radius)
        {
            Vector2 ab = b - a;
            float len2 = System.Math.Max(ab.LengthSquared(), 0.0001f);
            return (x, y) =>
            {
                float t = MathHelper.Clamp(Vector2.Dot(new Vector2(x, y) - a, ab) / len2, 0f, 1f);
                return Vector2.DistanceSquared(new Vector2(x, y), a + ab * t) <= radius * radius;
            };
        }

        // A packed snowball: blue-grey shading underneath, a bright lit side and a few hand marks
        static Texture2D BuildSnowball(GraphicsDevice gd)
        {
            Color shade = new Color(168, 186, 222), body = new Color(214, 226, 246);
            Color lit = new Color(244, 249, 255), spark = Color.White;
            return Render(gd, 26, 26,
                (Ellipse(13, 13, 12, 12), shade),
                (Ellipse(12.2f, 12.2f, 10.6f, 10.6f), body),
                (Ellipse(10.6f, 10.4f, 7.6f, 7.4f), lit),
                (Ellipse(8.6f, 8.2f, 2.6f, 2.2f), spark),
                (Ellipse(16.5f, 17f, 1.1f, 1f), shade),
                (Ellipse(14.2f, 19f, .9f, .8f), shade));
        }
    }
}
