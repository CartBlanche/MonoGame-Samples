using System;
using Microsoft.Xna.Framework;

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
        };

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
