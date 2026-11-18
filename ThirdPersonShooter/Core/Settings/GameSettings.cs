using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// User settings, persisted as settings.json under ApplicationData/ThirdPersonShooter/ (mirrors RacingGame's GameSettings.cs).
    /// Only what the port honours is kept (plan 05 section 2 "Settings mapping"): display mode, resolution, vsync, bloom, mouse
    /// sensitivity and the three volumes (stored only, no audio yet). Godot's GI/SSAO/TAA/FSR/MSAA rows are not implemented and hidden.
    /// </summary>
    public sealed class GameSettings
    {
        public bool Fullscreen { get; set; }
        public int ResolutionWidth { get; set; } = 1280;   // windowed client size (fullscreen = desktop size)
        public int ResolutionHeight { get; set; } = 720;
        public bool VSync { get; set; } = true;
        public bool Bloom { get; set; } = true;
        public bool Shadows { get; set; } = true;          // real shadow map for the key light (off = blob shadows only)
        public bool Rumble { get; set; } = true;           // gamepad vibration on shots / laser hits (port-only enhancement; only does anything with a pad)
        public float MouseSensitivity { get; set; } = 1f;  // multiplier on PlayerConstants.MouseLookSpeed
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 1f;
        public float SfxVolume { get; set; } = 1f;

        public const float MinSensitivity = 0.25f, MaxSensitivity = 3f;

        /// <summary>The live settings the game reads (the settings screen edits a copy and Apply commits into this).</summary>
        public static GameSettings Current { get; private set; } = new GameSettings();

        public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ThirdPersonShooter", "settings.json");

        static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = true };

        public GameSettings Clone() => (GameSettings)MemberwiseClone();

        public void CopyFrom(GameSettings o)
        {
            Fullscreen = o.Fullscreen; ResolutionWidth = o.ResolutionWidth; ResolutionHeight = o.ResolutionHeight; VSync = o.VSync; Bloom = o.Bloom; Shadows = o.Shadows; Rumble = o.Rumble;
            MouseSensitivity = o.MouseSensitivity; MasterVolume = o.MasterVolume; MusicVolume = o.MusicVolume; SfxVolume = o.SfxVolume;
        }

        public bool ValueEquals(GameSettings o) =>
            Fullscreen == o.Fullscreen && ResolutionWidth == o.ResolutionWidth && ResolutionHeight == o.ResolutionHeight && VSync == o.VSync && Bloom == o.Bloom && Shadows == o.Shadows && Rumble == o.Rumble &&
            MouseSensitivity == o.MouseSensitivity && MasterVolume == o.MasterVolume && MusicVolume == o.MusicVolume && SfxVolume == o.SfxVolume;

        /// <summary>Clamp hand-edited / corrupt values to sane ranges.</summary>
        public void Sanitize()
        {
            if (ResolutionWidth < 640 || ResolutionHeight < 360 || ResolutionWidth > 16384 || ResolutionHeight > 16384) { ResolutionWidth = 1280; ResolutionHeight = 720; }
            MouseSensitivity = MathHelper.Clamp(MouseSensitivity, MinSensitivity, MaxSensitivity);
            MasterVolume = MathHelper.Clamp(MasterVolume, 0f, 1f); MusicVolume = MathHelper.Clamp(MusicVolume, 0f, 1f); SfxVolume = MathHelper.Clamp(SfxVolume, 0f, 1f);
        }

        public static GameSettings Load(string path = null)
        {
            path ??= DefaultPath;
            try
            {
                if (File.Exists(path))
                {
                    var s = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path), Json);
                    if (s != null) { s.Sanitize(); return s; }
                }
            }
            catch (Exception e) { Console.WriteLine($"[settings] load failed ({e.GetType().Name}): using defaults"); }
            return new GameSettings();
        }

        /// <summary>Load the saved settings into <see cref="Current"/>.</summary>
        public static void LoadCurrent(string path = null) { Current = Load(path); }

        public bool Save(string path = null)
        {
            path ??= DefaultPath;
            try { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path, JsonSerializer.Serialize(this, Json)); return true; }
            catch (Exception e) { Console.WriteLine($"[settings] save failed ({e.GetType().Name})"); return false; }
        }
    }

    /// <summary>
    /// Apply/Cancel semantics of the settings screen: edits go to <see cref="Working"/> (a copy); Apply commits it into the target
    /// (and saves); Cancel leaves the target untouched.
    /// </summary>
    public sealed class SettingsEditor
    {
        public readonly GameSettings Working;
        readonly GameSettings _target; readonly string _path;
        public SettingsEditor(GameSettings target, string path = null) { _target = target; _path = path; Working = target.Clone(); }
        public void Apply() { Working.Sanitize(); _target.CopyFrom(Working); _target.Save(_path); }
        public void Cancel() { /* the working copy is simply dropped */ }
    }

    /// <summary>Display mode / resolution handling, following RacingGame (BaseGame.ApplyResolutionChange and its window-fit lesson).</summary>
    public static class DisplaySettings
    {
        /// <summary>A window as big as the desktop gets shrunk by the system (title bar, dock/taskbar), leaving a back buffer that no
        /// longer matches the window; so windowed sizes must leave this much vertical room (RacingGame BaseGame.WindowDecorationHeight).</summary>
        public const int WindowDecorationHeight = 160;

        static readonly Point[] Standard = { new(1024, 576), new(1280, 720), new(1366, 768), new(1600, 900), new(1920, 1080), new(2560, 1440), new(3840, 2160) };

        public static Point Desktop()
        {
            try { var d = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode; return new Point(d.Width, d.Height); }
            catch { return new Point(1920, 1080); }
        }

        public static bool WindowedSizeFits(Point desktop, int w, int h) => w <= desktop.X && h <= desktop.Y - WindowDecorationHeight;

        /// <summary>Windowed resolutions that fit the desktop, plus the currently saved one if it is not in the list.</summary>
        public static List<Point> Choices(Point desktop, int curW, int curH)
        {
            var l = new List<Point>();
            foreach (var p in Standard) if (WindowedSizeFits(desktop, p.X, p.Y)) l.Add(p);
            var cur = new Point(curW, curH);
            if (!l.Contains(cur)) { l.Add(cur); l.Sort((a, b) => (a.X * a.Y).CompareTo(b.X * b.Y)); }
            return l;
        }

        static Point Fit(Point desktop, int w, int h)
        {
            float s = Math.Min(1f, Math.Min(desktop.X / (float)w, (desktop.Y - WindowDecorationHeight) / (float)h));
            return s >= 1f ? new Point(w, h) : new Point((int)(w * s) & ~1, (int)(h * s) & ~1);
        }

        /// <summary>Set the preferred back buffer / fullscreen / vsync on the manager (no ApplyChanges: usable in the Game constructor).</summary>
        public static void Configure(GraphicsDeviceManager g, GameSettings s)
        {
            var desk = Desktop();
            Point size = s.Fullscreen ? desk : Fit(desk, s.ResolutionWidth, s.ResolutionHeight); // fullscreen = borderless at desktop size
            g.PreferredBackBufferWidth = size.X; g.PreferredBackBufferHeight = size.Y;
            g.HardwareModeSwitch = false;
            g.IsFullScreen = s.Fullscreen;
            g.SynchronizeWithVerticalRetrace = s.VSync;
        }

        public static void Apply(GraphicsDeviceManager g, GameSettings s) { Configure(g, s); g.ApplyChanges(); }
    }
}
