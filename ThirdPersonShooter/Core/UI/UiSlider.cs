using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>Slider row: label left, track + percentage right. Left/Right step, mouse click/drag sets the value from the x position on the track.</summary>
    public sealed class UiSlider : UiWidget
    {
        public string Label; public float Min, Max, Value, Step; public Action<float> Changed; public Func<float, string> Format;
        public UiSlider(string label, float min, float max, float value, float step, Rectangle bounds, Action<float> changed = null, Func<float, string> format = null)
        { Label = label; Min = min; Max = max; Value = value; Step = step; Bounds = bounds; Changed = changed; Format = format ?? (v => $"{(int)MathF.Round(v * 100)}%"); }
        public override bool HandlesHorizontal => true;
        Rectangle Track => new Rectangle((int)(Bounds.X + Bounds.Width * 0.50f), Center.Y - 4, (int)(Bounds.Width * 0.36f), 8);
        void SetValue(float v) { v = MathHelper.Clamp(v, Min, Max); if (Step > 0) v = Min + MathF.Round((v - Min) / Step) * Step; v = MathHelper.Clamp(v, Min, Max); if (v != Value) { Value = v; Changed?.Invoke(v); } }
        public override void Adjust(int dir) { if (Enabled) SetValue(Value + dir * Step); }
        public override void MouseDown(Point p) => MouseDrag(p);
        public override void MouseDrag(Point p) { if (!Enabled) return; var t = Track; SetValue(Min + (p.X - t.X) / (float)t.Width * (Max - Min)); }
        public override void Draw(UiRenderer r, UiState s)
        {
            if (!Visible) return;
            r.ButtonBox(Bounds, Enabled, s.Hover, false, s.Focused);
            var col = Enabled ? UiTheme.Text : UiTheme.TextDisabled; float cy = Center.Y;
            r.TextMid(UiTheme.Font30, Label, Bounds.X + UiTheme.MarginX, cy, col);
            var t = Track; float f = (Value - Min) / Math.Max(Max - Min, 1e-6f);
            r.Rect(t, UiTheme.SliderTrack); r.Rect(t.X, t.Y, t.Width * f, t.Height, UiTheme.SliderFill);
            r.Rect(t.X + t.Width * f - 4, t.Y - 8, 8, t.Height + 16, col);
            r.TextMidRight(UiTheme.Font30, Format(Value), Bounds.Right - UiTheme.MarginX, cy, col);
        }
    }
}
