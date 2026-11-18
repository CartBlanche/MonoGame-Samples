using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// menu.tscn "Loading" row: the logo stays, a "Loading..." label and a progress bar sit at the bottom (HBoxContainer anchored bottom,
    /// offset_left 100, offset_right -130, 135..100 px above the bottom). The bar uses the button look: dark slanted track, blue fill.
    /// </summary>
    public sealed class LoadingScreen : UiScreen
    {
        readonly Texture2D _logo; int _w = UiTheme.VirtualW, _h = UiTheme.VirtualH;
        public float Progress;
        public LoadingScreen(Texture2D logo) { _logo = logo; }
        public void Layout(int w, int h) { _w = w; _h = h; }
        public override void Update(UiInput input, float dt) { } // no input: Esc/clicks are ignored while loading

        public override void Draw(UiRenderer r)
        {
            if (_logo != null) r.Image(_logo, new Vector2(20, 5), Color.White);
            float vw = _w / r.Scale, vh = _h / r.Scale;                    // virtual size of the window (UI scale is min(w/1920,h/1080))
            const int barH = 35; float top = vh - 135f, cy = top + barH * 0.5f;
            r.TextMid(UiTheme.Font30, "Loading...", 100, cy, UiTheme.Text);
            float bx = 100 + UiTheme.Font30.MeasureString("Loading...").X + 24, bw = vw - 130 - bx; // right offset -130 from the right edge
            if (bw < 50) return;
            float p = Math.Clamp(Progress, 0f, 1f);
            r.Slanted(new Rectangle((int)bx, (int)top, (int)bw, barH), UiTheme.C(0f, 0f, 0f, 0.5f), 3, UiTheme.HoverBorder);      // track
            int fw = (int)((bw - 8) * p);
            if (fw > 0) r.Slanted(new Rectangle((int)bx + 4, (int)top + 4, fw, barH - 8), UiTheme.SliderFill, 0, Color.Transparent, radius: 3); // fill
            r.TextMidCentre(UiTheme.Font22, $"{(int)MathF.Round(p * 100f)}%", bx + bw * 0.5f, cy, UiTheme.Text);
        }
    }
}
