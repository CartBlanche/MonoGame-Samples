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

        // AI settings
        public byte MaxAIPlayers { get; set; } = GetPlatformMaxAIPlayers();
        public bool FillEmptySlotsWithAI { get; set; } = true;

        // Audio settings
        public float SoundVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 1.0f;

        // Gameplay settings
        public AnimationSpeed AnimationSpeed { get; set; } = AnimationSpeed.Normal;
        public bool AutoStandOn21 { get; set; } = false;
        public bool ShowCardCount { get; set; } = true;

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
        /// Gets the maximum allowed AI players based on the current platform.
        /// </summary>
        public static byte GetPlatformMaxAIPlayers()
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
                }
                else
                {
                    instance = new GameSettings();
                    System.Console.WriteLine("[Settings] Created default settings");
                    Save(); // Save default settings
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Settings] Error loading: {ex.Message}");
                instance = new GameSettings(); // Use defaults on error
            }

            // Ensure MaxAIPlayers doesn't exceed platform limit
            byte platformMax = GetPlatformMaxAIPlayers();
            if (instance.MaxAIPlayers > platformMax)
            {
                instance.MaxAIPlayers = platformMax;
            }

            // Apply language setting
            ApplyLanguage(instance.Language);
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

            // Clamp MaxAIPlayers to platform limit
            byte platformMax = GetPlatformMaxAIPlayers();
            if (MaxAIPlayers > platformMax)
            {
                MaxAIPlayers = platformMax;
            }
            if (MaxAIPlayers < 0)
            {
                MaxAIPlayers = 0;
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