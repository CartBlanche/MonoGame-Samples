using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>Option cycler row: label on the left, "&lt; value &gt;" on the right. Left/Right (or click on the left/right half of the value) change it, Enter/A cycles forward.</summary>
    public sealed class UiRadioRow : UiWidget
    {
        public string Label; public string[] Options; public int Index; public Action<int> Changed;
        public UiRadioRow(string label, string[] options, int index, Rectangle bounds, Action<int> changed = null) { Label = label; Options = options; Index = index; Bounds = bounds; Changed = changed; }
        public override bool HandlesHorizontal => true;
        public string Value => Options.Length == 0 ? "" : Options[Math.Clamp(Index, 0, Options.Length - 1)];
        public override void Adjust(int dir)
        {
            if (!Enabled || Options.Length == 0) return;
            Index = ((Index + dir) % Options.Length + Options.Length) % Options.Length; Changed?.Invoke(Index);
        }
        public override void Activate() => Adjust(1);
        public override void MouseDown(Point p) { if (p.X < Bounds.X + Bounds.Width * 0.72f) Adjust(-1); else Adjust(1); } // value area: left half of it = previous
        public override void Draw(UiRenderer r, UiState s)
        {
            if (!Visible) return;
            r.ButtonBox(Bounds, Enabled, s.Hover, false, s.Focused);
            var col = Enabled ? UiTheme.Text : UiTheme.TextDisabled; float cy = Center.Y;
            r.TextMid(UiTheme.Font30, Label, Bounds.X + UiTheme.MarginX, cy, col);
            float vx = Bounds.X + Bounds.Width * 0.72f, vr = Bounds.Right - UiTheme.MarginX;
            r.TextMid(UiTheme.Font30, "<", vx - 6, cy, Enabled ? UiTheme.TextDim : UiTheme.TextDisabled);
            r.TextMidRight(UiTheme.Font30, ">", vr, cy, Enabled ? UiTheme.TextDim : UiTheme.TextDisabled);
            r.TextMidCentre(UiTheme.Font30, Value, (vx + 24 + vr - 24) * 0.5f, cy, col);
        }
    }
}
