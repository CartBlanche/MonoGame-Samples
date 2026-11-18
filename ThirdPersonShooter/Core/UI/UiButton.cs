using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>Godot Button: slanted dark box, label left-aligned (menu.tscn alignment 0, margin 20). Disabled draws dimmed with an optional note beside it.</summary>
    public sealed class UiButton : UiWidget
    {
        public string Text, Note;           // Note: small text to the right of a disabled button ("COMING SOON")
        public Action Clicked;
        public UiButton(string text, int x, int y, int w = UiTheme.ButtonW, int h = UiTheme.ButtonH, Action clicked = null) { Text = text; Bounds = new Rectangle(x, y, w, h); Clicked = clicked; }
        public override void Activate() { if (Enabled) Clicked?.Invoke(); }
        public override void Draw(UiRenderer r, UiState s)
        {
            if (!Visible) return;
            r.ButtonBox(Bounds, Enabled, s.Hover && Enabled, s.Pressed && Enabled, s.Focused && Enabled);
            r.TextMid(UiTheme.Font30, Text, Bounds.X + UiTheme.MarginX, Center.Y, Enabled ? UiTheme.Text : UiTheme.TextDisabled);
            if (!Enabled && !string.IsNullOrEmpty(Note)) r.TextMid(UiTheme.Font22, Note, Bounds.Right + 40, Center.Y, UiTheme.TextDim);
        }
    }
}
