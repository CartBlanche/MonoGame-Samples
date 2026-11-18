using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Settings: display mode, resolution, VSync, bloom, mouse sensitivity, master/music/SFX volume (plan 05 "Settings mapping"; Godot's GI/SSAO/TAA/FSR/MSAA
    /// rows are not implemented and hidden). Edits a copy; APPLY commits + saves, CANCEL / Esc / B discards. <paramref name="closed"/>(applied) returns to
    /// wherever the screen was opened from (main menu now, pause overlay later), so the return target is the caller's parameter.
    /// </summary>
    public sealed class SettingsScreen : UiScreen
    {
        public readonly SettingsEditor Editor;
        readonly List<Point> _res;

        /// <summary>Volume sliders act live (preview of the working copy); Cancel/Esc restores the committed volumes, Apply commits (the game re-applies the live settings).</summary>
        static void Preview(GameSettings s) => AudioManager.ApplySettings(s);

        public SettingsScreen(Texture2D logo, SettingsEditor editor, Point desktop, Action<bool> closed)
        {
            Editor = editor; var s = editor.Working;
            _res = DisplaySettings.Choices(desktop, s.ResolutionWidth, s.ResolutionHeight);
            if (logo != null) Focus.Add(new UiImage(logo, new Point(20, 5)));
            Focus.Add(new UiLabel("SETTINGS", new Vector2(UiTheme.ButtonX, 244), UiTheme.Font44));
            const int x = UiTheme.ButtonX, w = 780, h = 50, step = 58; int y = 320;
            Rectangle Row() { var r = new Rectangle(x, y, w, h); y += step; return r; }

            var mode = Focus.Add(new UiRadioRow("DISPLAY MODE", new[] { "WINDOWED", "FULLSCREEN" }, s.Fullscreen ? 1 : 0, Row()));
            var res = Focus.Add(new UiRadioRow("RESOLUTION", _res.ConvertAll(p => $"{p.X} x {p.Y}").ToArray(), Math.Max(0, _res.IndexOf(new Point(s.ResolutionWidth, s.ResolutionHeight))), Row(), i => { s.ResolutionWidth = _res[i].X; s.ResolutionHeight = _res[i].Y; }));
            res.Enabled = !s.Fullscreen; // fullscreen always uses the desktop size (RacingGame lesson)
            mode.Changed = i => { s.Fullscreen = i == 1; res.Enabled = !s.Fullscreen; };
            Focus.Add(new UiRadioRow("VSYNC", new[] { "OFF", "ON" }, s.VSync ? 1 : 0, Row(), i => s.VSync = i == 1));
            Focus.Add(new UiRadioRow("BLOOM", new[] { "OFF", "ON" }, s.Bloom ? 1 : 0, Row(), i => s.Bloom = i == 1));
            Focus.Add(new UiRadioRow("SHADOWS", new[] { "OFF", "ON" }, s.Shadows ? 1 : 0, Row(), i => s.Shadows = i == 1));
            Focus.Add(new UiRadioRow("RUMBLE", new[] { "OFF", "ON" }, s.Rumble ? 1 : 0, Row(), i => s.Rumble = i == 1)); // gamepad only
            Focus.Add(new UiSlider("MOUSE SENSITIVITY", GameSettings.MinSensitivity, GameSettings.MaxSensitivity, s.MouseSensitivity, 0.05f, Row(), v => s.MouseSensitivity = v, v => v.ToString("0.00") + "x"));
            Focus.Add(new UiSlider("MASTER VOLUME", 0f, 1f, s.MasterVolume, 0.05f, Row(), v => { s.MasterVolume = v; Preview(s); }));
            Focus.Add(new UiSlider("MUSIC VOLUME", 0f, 1f, s.MusicVolume, 0.05f, Row(), v => { s.MusicVolume = v; Preview(s); }));
            Focus.Add(new UiSlider("SFX VOLUME", 0f, 1f, s.SfxVolume, 0.05f, Row(), v => { s.SfxVolume = v; Preview(s); }));
            y += 14;
            var apply = Focus.Add(new UiButton("APPLY", x, y, clicked: () => { Editor.Apply(); closed?.Invoke(true); }));
            Focus.Add(new UiButton("CANCEL", x + 340, y, clicked: () => { Editor.Cancel(); AudioManager.ApplySettings(GameSettings.Current); closed?.Invoke(false); }));
            Focus.BackPressed = () => { Editor.Cancel(); AudioManager.ApplySettings(GameSettings.Current); closed?.Invoke(false); };
            Focus.SetFocus(mode);
        }
    }
}
