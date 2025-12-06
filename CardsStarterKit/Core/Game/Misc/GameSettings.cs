//-----------------------------------------------------------------------------
// GameSettings.cs
//
// Manages game settings with JSON persistence
//-----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
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
        public bool PersistWinnings { get; set; } = false;
        public float SavedPlayerBalance { get; set; } = 500f;

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
                    System.Console.WriteLine($"[Settings] Loaded from {settingsFilePath}");
                    System.Console.WriteLine($"[Settings] PersistWinnings={instance.PersistWinnings}, SavedPlayerBalance={instance.SavedPlayerBalance}");
                }
                else
                {
                    // First launch - detect OS language and set defaults
                    instance = new GameSettings();
                    DetectAndSetOSLanguage();
                    System.Console.WriteLine("[Settings] Created default settings (first launch)");
                    System.Console.WriteLine($"[Settings] Detected language: {instance.Language}, Currency: {instance.Currency}");
                    Save(); // Save default settings
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Settings] Error loading: {ex.Message}");
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
                System.Console.WriteLine($"[Settings] Detected OS language: {osLanguage} ({CultureInfo.CurrentUICulture.Name})");

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
                System.Console.WriteLine($"[Settings] Error detecting OS language: {ex.Message}");
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
                System.Console.WriteLine($"[Settings] Language changed to {languageName} ({cultureCode})");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Settings] Error applying language: {ex.Message}");
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
                System.Console.WriteLine($"[Settings] Saved to {settingsFilePath}");
                System.Console.WriteLine($"[Settings] PersistWinnings={instance.PersistWinnings}, SavedPlayerBalance={instance.SavedPlayerBalance}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Settings] Error saving: {ex.Message}");
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