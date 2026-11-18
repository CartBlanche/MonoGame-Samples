using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Owns a screen's widgets and the focus model (Godot grab_focus + ui_* actions): Up/Down spatial navigation with wrap that skips disabled widgets,
    /// Left/Right adjusts a radio/slider or moves sideways between buttons, Enter/A activates, Esc/B raises <see cref="BackPressed"/>, mouse hover moves
    /// focus and click activates on release (sliders also drag). Pure logic (no GL), see UiSelfTest.
    /// </summary>
    public sealed class UiFocus
    {
        public readonly List<UiWidget> Widgets = new();
        public UiWidget Focused { get; private set; }
        public Action BackPressed;
        UiWidget _pressed;

        public T Add<T>(T w) where T : UiWidget { Widgets.Add(w); return w; }

        public void SetFocus(UiWidget w) { if (w == null || w.CanFocus) Focused = w; }
        public void FocusFirst() { Focused = null; foreach (var w in Widgets) if (w.CanFocus) { Focused = w; break; } }

        /// <summary>Move focus; returns true if it changed. Up/Down wrap, Left/Right do not.</summary>
        public bool Move(int dx, int dy)
        {
            if (Focused == null || !Focused.CanFocus) { FocusFirst(); return Focused != null; }
            var cur = Focused; UiWidget best = null; float bestScore = float.MaxValue;
            foreach (var w in Widgets)
            {
                if (w == cur || !w.CanFocus) continue;
                float ddx = w.Center.X - cur.Center.X, ddy = w.Center.Y - cur.Center.Y, score;
                if (dy != 0) { if (ddy * dy <= 1) continue; score = Math.Abs(ddy) + 0.5f * Math.Abs(w.Bounds.X - cur.Bounds.X); }
                else { if (ddx * dx <= 1) continue; score = Math.Abs(ddx) + 4f * Math.Abs(ddy); }
                if (score < bestScore) { bestScore = score; best = w; }
            }
            if (best == null && dy != 0) // wrap: the farthest widget on the other side
            {
                float far = -1f;
                foreach (var w in Widgets)
                {
                    if (w == cur || !w.CanFocus) continue;
                    float score = -dy * (w.Center.Y - cur.Center.Y) * 1000f - Math.Abs(w.Bounds.X - cur.Bounds.X);
                    if (score > far || best == null) { far = score; best = w; }
                }
            }
            if (best == null) return false;
            Focused = best; return true;
        }

        public void Update(UiInput i)
        {
            if (i.Back) BackPressed?.Invoke();
            if (i.Up) Move(0, -1);
            if (i.Down) Move(0, 1);
            if (i.Left) { if (Focused != null && Focused.HandlesHorizontal) Focused.Adjust(-1); else Move(-1, 0); }
            if (i.Right) { if (Focused != null && Focused.HandlesHorizontal) Focused.Adjust(1); else Move(1, 0); }
            if (i.Confirm && Focused != null && Focused.CanFocus) Focused.Activate();

            var mp = new Point((int)i.Mouse.X, (int)i.Mouse.Y); var hit = HitTest(mp);
            if (i.MouseMoved && hit != null) Focused = hit;
            if (i.MousePressed && hit != null) { _pressed = hit; Focused = hit; hit.MouseDown(mp); }
            else if (i.MouseDown && _pressed != null) _pressed.MouseDrag(mp);
            if (i.MouseReleased) { if (_pressed != null && hit == _pressed && !_pressed.HandlesHorizontal) _pressed.Activate(); _pressed = null; }
        }

        public UiWidget HitTest(Point p)
        {
            for (int k = Widgets.Count - 1; k >= 0; k--)
            {
                var w = Widgets[k]; if (!w.CanFocus) continue;
                // slanted boxes: test against the box sheared at the clicked row
                float shift = UiTheme.SkewVisual * w.Bounds.Height * (0.5f - (p.Y - w.Bounds.Y) / (float)Math.Max(w.Bounds.Height, 1));
                if (p.Y >= w.Bounds.Y && p.Y < w.Bounds.Bottom && p.X - shift >= w.Bounds.X && p.X - shift < w.Bounds.Right) return w;
            }
            return null;
        }

        public void Draw(UiRenderer r, Point mouse)
        {
            var hover = HitTest(mouse);
            foreach (var w in Widgets) w.Draw(r, new UiState { Focused = w == Focused, Hover = w == hover, Pressed = w == _pressed && w == hover });
        }
    }
}
