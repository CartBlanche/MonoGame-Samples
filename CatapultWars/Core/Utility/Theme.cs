using System;
using System.IO;

namespace CatapultGame
{
    /// <summary>
    /// Seasonal looks. Each one has a <see cref="ThemeDefinition"/> registered in <see cref="Themes"/>.
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
    /// Override for testing with <c>--theme=halloween</c> (or <c>--theme=none</c>; any name in <see cref="Themes"/>) on the
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
            shown = Active;
            Blend = Active.IsStyled ? 1f : 0f;
        }

        static ThemeDefinition shown;
        static long dateCheckedAt;
        static bool dateChecked;
        static ThemeDefinition dateTheme = Themes.Classic;

        const float FadeSeconds = .6f;

        /// <summary>
        /// How much of the themed look to show on screens that can change live (the title
        /// screen): 0 is classic, 1 is fully themed. Eases toward the current setting.
        /// </summary>
        public static float Blend { get; private set; }

        /// <summary>
        /// Moves <see cref="Blend"/> toward the current look; call once a frame.
        /// </summary>
        public static void Fade(float elapsed)
        {
            if (Active.IsStyled)
                shown = Active;
            float target = Active.IsStyled ? 1f : 0f;
            float step = elapsed / FadeSeconds;
            Blend = Blend < target ? Math.Min(target, Blend + step) : Math.Max(target, Blend - step);
        }

        /// <summary>
        /// The styled theme the title screen is showing or fading out. Stays on the last styled
        /// theme while the blend eases back to classic.
        /// </summary>
        public static ThemeDefinition Shown
        {
            get { return shown; }
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
            get { return DateTheme.Id; }
        }

        /// <summary>
        /// The theme the system date falls in. The date is looked up at most once a second, since
        /// the title screen asks every frame.
        /// </summary>
        public static ThemeDefinition DateTheme
        {
            get
            {
                long now = Environment.TickCount64;
                if (!dateChecked || now - dateCheckedAt >= 1000)
                {
                    dateTheme = Themes.ForDate(DateTime.Now);
                    dateCheckedAt = now;
                    dateChecked = true;
                }
                return dateTheme;
            }
        }

        /// <summary>
        /// The season the game should look like right now.
        /// </summary>
        public static Season Current
        {
            get { return forced ?? (seasonalEnabled ? DateSeason : Season.None); }
        }

        /// <summary>
        /// The definition of the look the game should have right now.
        /// </summary>
        public static ThemeDefinition Active
        {
            get { return Themes.For(Current); }
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
            return Themes.ForDate(d).Id;
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

            ThemeDefinition theme;
            if (Themes.TryParse(value, out theme))
                forced = theme.Id;
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
