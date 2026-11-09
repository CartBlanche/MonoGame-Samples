using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// The registry of looks. Adding a season means adding its definition to <see cref="All"/>.
    /// </summary>
    public static class Themes
    {
        public static readonly ThemeDefinition Classic = new ThemeDefinition
        {
            Id = Season.None,
            Name = "none",

            // A plain reindeer on the far hill, the same one every theme gives its own look
            MascotArt = gd => ReindeerArt.Build(gd, ReindeerArt.Natural),
            MascotFeet = new Vector2(520, 311),
            MascotOrigin = new Vector2(28, 48),
            MascotScale = .75f,
            MascotRotation = MathHelper.ToRadians(-15f),

            // A few leaves tumbling across the battlefield, in the greens and golds of the trees
            WindParticles = new WindParticleSpec
            {
                Art = BuildLeaf,
                Rate = 5f,
                WindPush = 10f,
                Layers = new[]
                {
                    Leaf(new Color(150, 188, 72)),
                    Leaf(new Color(196, 204, 84)),
                    Leaf(new Color(206, 152, 72)),
                },
            },
        };

        static WindParticleLayer Leaf(Color tint)
        {
            return new WindParticleLayer
            {
                Size = 11f, SizeRandom = 5f, Gravity = 30f, WindInfluence = 1.2f, Opacity = .92f,
                Tint = tint, Life = 13f, LifeRandom = 3f, Spin = -1.6f, SpinRandom = 3.2f,
            };
        }

        // A pointed leaf with a pale vein, white so the layer's tint colours it
        static Texture2D BuildLeaf(GraphicsDevice gd)
        {
            return ShapeArt.Render(gd, 16, 10,
                (ShapeArt.Poly(new Vector2(.5f, 5f), new Vector2(4.5f, 1.4f), new Vector2(10, 1.6f),
                    new Vector2(15.5f, 5f), new Vector2(10, 8.4f), new Vector2(4.5f, 8.6f)), Color.White),
                (ShapeArt.Rect(2f, 4.6f, 14f, 5.4f), new Color(205, 205, 205)));
        }

        public static readonly ThemeDefinition[] All = { Classic, HalloweenTheme.Definition, WinterTheme.Definition };

        public static ThemeDefinition For(Season season)
        {
            foreach (ThemeDefinition t in All)
            {
                if (t.Id == season)
                    return t;
            }
            return Classic;
        }

        /// <summary>The theme whose date window holds the day, or classic.</summary>
        public static ThemeDefinition ForDate(DateTime d)
        {
            foreach (ThemeDefinition t in All)
            {
                if (t.StartMonth > 0 && InWindow(d, t.StartMonth, t.StartDay, t.EndMonth, t.EndDay))
                    return t;
            }
            return Classic;
        }

        /// <summary>For <c>--theme=</c>: a theme name, or classic/default for the original look.</summary>
        public static bool TryParse(string name, out ThemeDefinition theme)
        {
            name = name.Trim();
            foreach (ThemeDefinition t in All)
            {
                if (t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || t.Id.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    theme = t;
                    return true;
                }
            }
            if (name.Equals("classic", StringComparison.OrdinalIgnoreCase)
                || name.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                theme = Classic;
                return true;
            }
            theme = null;
            return false;
        }

        /// <summary>
        /// Inclusive month/day window. A window may run over the new year.
        /// </summary>
        static bool InWindow(DateTime d, int m1, int d1, int m2, int d2)
        {
            int n = d.Month * 100 + d.Day, a = m1 * 100 + d1, b = m2 * 100 + d2;
            return a <= b ? n >= a && n <= b : n >= a || n <= b;
        }
    }
}
