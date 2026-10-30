using System;
using System.IO;

namespace CatapultGame
{
    /// <summary>
    /// Seasonal looks. Only Halloween has art so far; more seasons slot in here later.
    /// </summary>
    public enum Season
    {
        None,
        Halloween,
    }

    /// <summary>
    /// Decides which seasonal look the game uses. Themes are cosmetic and local to each
    /// machine: nothing about them is sent over the network.
    /// </summary>
    /// <remarks>
    /// Override for testing with <c>--theme=halloween</c> (or <c>--theme=none</c>) on the
    /// command line, or the CATAPULT_THEME environment variable.
    /// </remarks>
    public static class Theme
    {
        static readonly string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CatapultWars", "theme.txt");

        static bool seasonalEnabled = true;
        static Season? forced;

        static Theme()
        {
            LoadSetting();
            ParseOverride();
            Blend = IsHalloween ? 1f : 0f;
        }

        const float FadeSeconds = .6f;

        /// <summary>
        /// How much of the Halloween look to show on screens that can change live (the title
        /// screen): 0 is classic, 1 is fully themed. Eases toward the current setting.
        /// </summary>
        public static float Blend { get; private set; }

        /// <summary>
        /// Moves <see cref="Blend"/> toward the current look; call once a frame.
        /// </summary>
        public static void Fade(float elapsed)
        {
            float target = IsHalloween ? 1f : 0f;
            float step = elapsed / FadeSeconds;
            Blend = Blend < target ? Math.Min(target, Blend + step) : Math.Max(target, Blend - step);
        }

        /// <summary>
        /// Player setting. When false the game always uses the classic look.
        /// </summary>
        public static bool SeasonalEnabled
        {
            get { return seasonalEnabled; }
            set
            {
                seasonalEnabled = value;
                SaveSetting();
            }
        }

        /// <summary>
        /// The season the system date falls in, ignoring the player setting.
        /// </summary>
        public static Season DateSeason
        {
            get { return FromDate(DateTime.Now); }
        }

        /// <summary>
        /// The season the game should look like right now.
        /// </summary>
        public static Season Current
        {
            get { return forced ?? (seasonalEnabled ? DateSeason : Season.None); }
        }

        public static bool IsHalloween
        {
            get { return Current == Season.Halloween; }
        }

        /// <summary>
        /// True when the menu should offer the on/off switch.
        /// </summary>
        public static bool SeasonAvailable
        {
            get { return forced.HasValue ? forced.Value != Season.None : DateSeason != Season.None; }
        }

        public static Season FromDate(DateTime d)
        {
            return InWindow(d, 10, 24, 11, 1) ? Season.Halloween : Season.None;
        }

        /// <summary>
        /// Inclusive month/day window. A window may run over the new year.
        /// </summary>
        static bool InWindow(DateTime d, int m1, int d1, int m2, int d2)
        {
            int n = d.Month * 100 + d.Day, a = m1 * 100 + d1, b = m2 * 100 + d2;
            return a <= b ? n >= a && n <= b : n >= a || n <= b;
        }

        static void ParseOverride()
        {
            string value = Environment.GetEnvironmentVariable("CATAPULT_THEME");

            try
            {
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg.StartsWith("--theme=", StringComparison.OrdinalIgnoreCase))
                        value = arg.Substring("--theme=".Length);
                }
            }
            catch (Exception)
            {
                // Some platforms have no command line; the environment variable still works.
            }

            if (string.IsNullOrWhiteSpace(value))
                return;

            Season season;
            if (Enum.TryParse(value.Trim(), true, out season))
                forced = season;
            else if (value.Trim().Equals("classic", StringComparison.OrdinalIgnoreCase)
                || value.Trim().Equals("default", StringComparison.OrdinalIgnoreCase))
                forced = Season.None;
        }

        static void LoadSetting()
        {
            try
            {
                if (File.Exists(settingsPath))
                    seasonalEnabled = File.ReadAllText(settingsPath).Trim() != "off";
            }
            catch (Exception)
            {
                // Unreadable settings just mean the default (on).
            }
        }

        static void SaveSetting()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                File.WriteAllText(settingsPath, seasonalEnabled ? "on" : "off");
            }
            catch (Exception)
            {
                // Not being able to save is fine; the choice still holds for this session.
            }
        }
    }
}
