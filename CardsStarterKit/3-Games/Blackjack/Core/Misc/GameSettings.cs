//-----------------------------------------------------------------------------
// GameSettings.cs
//
// Manages game settings with JSON persistence
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using CardsFramework;
using Microsoft.Xna.Framework;

namespace Blackjack
{
    /// <summary>
    /// Manages all game settings with automatic persistence to JSON file.
    /// </summary>
    public class GameSettings
    {
        private static GameSettings instance;
        private static readonly string settingsFileName = "blackjack_settings.json";
        private static string settingsFilePath;

        private string language = "English";

        // Display settings
        public string Language
        {
            get => language;
            set
            {
                language = value;
                ApplyLanguage(value);
            }
        }
        // Store theme as invariant English value ("Red" or "Blue") to avoid asset loading issues
        public string Theme { get; set; } = "Red";
        public string Currency { get; set; } = "$";

        // NPC settings
        public byte MaxNPCPlayers { get; set; } = GetPlatformMaxNPCPlayers();
        public bool FillEmptySlotsWithNPC { get; set; } = true;

        // Audio settings
        public float SoundVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 1.0f;

        // Gameplay settings
        public AnimationSpeed AnimationSpeed { get; set; } = AnimationSpeed.Normal;
        public bool AutoStandOn21 { get; set; } = false;
        public bool ShowCardCount { get; set; } = true;
        public bool ShowHints { get; set; } = true;
        public bool PersistWinnings { get; set; } = false;
        public float SavedPlayerBalance { get; set; } = 500f;

        // TODO v2.x: Add strict casino rules setting
        // public bool StrictCasinoRules { get; set; } = true;
        // When true: Only allow splitting identical ranks (J-J, Q-Q, K-K, 10-10)
        // When false: Allow splitting any 10-value cards (J-Q, J-K, Q-K, 10-J, etc.)

        /// <summary>
        /// Gets the singleton instance of GameSettings.
        /// </summary>
        public static GameSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    Load();
                }
                return instance;
            }
        }

        /// <summary>
        /// Gets the maximum allowed NPC Players based on the current platform.
        /// </summary>
        public static byte GetPlatformMaxNPCPlayers()
        {
            if (UIUtility.IsMobile)
            {
                return 3; // Mobile platforms have limited screen space
            }
            else
            {
                return 6; // Desktop/Console can handle more players
            }
        }

        /// <summary>
        /// Initializes the settings file path based on platform.
        /// </summary>
        private static void InitializeFilePath()
        {
            if (string.IsNullOrEmpty(settingsFilePath))
            {
                // Get platform-specific storage location
                string storageFolder;
                if (OperatingSystem.IsAndroid())
                {
                    storageFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                }
                else if (OperatingSystem.IsIOS())
                {
                    storageFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                else
                {
                    // Desktop: use AppData/LocalApplicationData
                    storageFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                }
                storageFolder = Path.Combine(storageFolder, "Blackjack");

                // Ensure directory exists
                if (!Directory.Exists(storageFolder))
                {
                    Directory.CreateDirectory(storageFolder);
                }

                settingsFilePath = Path.Combine(storageFolder, settingsFileName);
            }
        }

        /// <summary>
        /// Loads settings from disk. If file doesn't exist, creates default settings.
        /// </summary>
        public static void Load()
        {
            InitializeFilePath();

            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    instance = JsonSerializer.Deserialize<GameSettings>(json);

                    // Migrate old localized theme values to invariant English values
                    MigrateThemeToInvariant();

                    Debug.WriteLine($"[Settings] Loaded from {settingsFilePath}");
                    Debug.WriteLine($"[Settings] PersistWinnings={instance.PersistWinnings}, SavedPlayerBalance={instance.SavedPlayerBalance}");
                }
                else
                {
                    // First launch - detect OS language and set defaults
                    instance = new GameSettings();
                    DetectAndSetOSLanguage();
                    Debug.WriteLine("[Settings] Created default settings (first launch)");
                    Debug.WriteLine($"[Settings] Detected language: {instance.Language}, Currency: {instance.Currency}");
                    Save(); // Save default settings
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] Error loading: {ex.Message}");
                instance = new GameSettings(); // Use defaults on error
            }

            // Ensure MaxNPClayers doesn't exceed platform limit
            byte platformMax = GetPlatformMaxNPCPlayers();
            if (instance.MaxNPCPlayers > platformMax)
            {
                instance.MaxNPCPlayers = platformMax;
            }

            // Apply language setting
            ApplyLanguage(instance.Language);
        }

        /// <summary>
        /// Detects the OS language and sets the language and currency accordingly.
        /// Called only on first launch.
        /// </summary>
        private static void DetectAndSetOSLanguage()
        {
            try
            {
                string osLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                Debug.WriteLine($"[Settings] Detected OS language: {osLanguage} ({CultureInfo.CurrentUICulture.Name})");

                // Map OS language to game language and set appropriate currency
                switch (osLanguage)
                {
                    case "fr":
                        instance.language = "Français";
                        instance.Currency = "€";
                        break;
                    case "es":
                        instance.language = "Español";
                        instance.Currency = "€";
                        break;
                    case "it":
                        instance.language = "Italiano";
                        instance.Currency = "€";
                        break;
                    case "ja":
                        instance.language = "日本語";
                        instance.Currency = "¥";
                        break;
                    case "zh":
                        instance.language = "中文";
                        instance.Currency = "¥";
                        break;
                    case "ru":
                        instance.language = "Русский";
                        instance.Currency = "R";
                        break;
                    default:
                        instance.language = "English";
                        instance.Currency = "$";
                        break;
                }

                // Apply the detected language
                ApplyLanguage(instance.language);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] Error detecting OS language: {ex.Message}");
                instance.language = "English";
                instance.Currency = "$";
            }
        }

        /// <summary>
        /// Applies the language setting by changing the current UI culture.
        /// </summary>
        private static void ApplyLanguage(string languageName)
        {
            try
            {
                string cultureCode = languageName switch
                {
                    "English" => "en-US",
                    "Français" => "fr-FR",
                    "Español" => "es-ES",
                    "Italiano" => "it-IT",
                    "日本語" => "ja-JP",
                    "中文" => "zh-CN",
                    "Русский" => "ru-RU",
                    _ => "en-US" // Default to English
                };

                CultureInfo culture = new CultureInfo(cultureCode);
                CultureInfo.CurrentUICulture = culture;
                Debug.WriteLine($"[Settings] Language changed to {languageName} ({cultureCode})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] Error applying language: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves current settings to disk.
        /// </summary>
        public static void Save()
        {
            InitializeFilePath();

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(instance, options);
                File.WriteAllText(settingsFilePath, json);
                Debug.WriteLine($"[Settings] Saved to {settingsFilePath}");
                Debug.WriteLine($"[Settings] PersistWinnings={instance.PersistWinnings}, SavedPlayerBalance={instance.SavedPlayerBalance}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] Error saving: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates and clamps a setting value.
        /// </summary>
        public void ValidateAndClamp()
        {
            // Clamp volumes to 0-1 range
            SoundVolume = MathHelper.Clamp(SoundVolume, 0f, 1f);
            MusicVolume = MathHelper.Clamp(MusicVolume, 0f, 1f);

            // Clamp MaxNPCPlayers to platform limit
            byte platformMax = GetPlatformMaxNPCPlayers();
            if (MaxNPCPlayers > platformMax)
            {
                MaxNPCPlayers = platformMax;
            }
            if (MaxNPCPlayers < 0)
            {
                MaxNPCPlayers = 0;
            }
        }

        /// <summary>
        /// Migrates old localized theme values (e.g., "Rouge", "Bleu") to invariant English values.
        /// This fixes crashes when changing languages with non-English card back names.
        /// </summary>
        private static void MigrateThemeToInvariant()
        {
            if (instance == null) return;

            // List of known localized values that mean "Red"
            string[] redVariants = { "Red", "Rouge", "Rojo", "Rosso", "Vermelho", "赤", "红色", "Красный" };
            // List of known localized values that mean "Blue"
            string[] blueVariants = { "Blue", "Bleu", "Azul", "Blu", "Azul", "青", "蓝色", "Синий" };

            if (redVariants.Contains(instance.Theme, StringComparer.OrdinalIgnoreCase))
            {
                instance.Theme = "Red";
                Debug.WriteLine($"[Settings] Migrated theme to invariant value: Red");
            }
            else if (blueVariants.Contains(instance.Theme, StringComparer.OrdinalIgnoreCase))
            {
                instance.Theme = "Blue";
                Debug.WriteLine($"[Settings] Migrated theme to invariant value: Blue");
            }
            // If neither matches, default to Red
            else if (instance.Theme != "Red" && instance.Theme != "Blue")
            {
                Debug.WriteLine($"[Settings] Unknown theme value '{instance.Theme}', defaulting to Red");
                instance.Theme = "Red";
            }
        }
    }

    /// <summary>
    /// Animation speed options for card dealing and gameplay.
    /// </summary>
    public enum AnimationSpeed
    {
        Fast,
        Normal,
        Slow
    }
}