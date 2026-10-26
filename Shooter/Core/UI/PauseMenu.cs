using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter.UI
{
    /// <summary>
    /// The in-game "Menu" (InGameMenu prefab + InGameMenuManager): Tab / P (or Cancel) opens it, the game stops (Time.timeScale 0), the cursor is
    /// freed and the master volume drops to 0.5. Panel 400 x 300 on the 960 x 540 canvas (scale to width): a title, then a grid of rows (cell 390 x 22.19,
    /// spacing 5, padding 5): Look Sensitivity slider (0.1 - 3), Shadows, Invincibility and Framerate Counter toggles, a Controls "Show" button (the
    /// ControlScheme pop-up), a Screenshot "Take new" button with a preview. Selectable colours: normal white, highlighted / selected
    /// (0.282, 0.725, 0.953), pressed 0.784. The logic works without a GraphicsDevice (self-tests); <see cref="Draw"/> needs the content constructor.
    /// Also draws the FramerateCounter text of the HUD (top right, size 16, refreshed every 0.5 s) when it is switched on.
    /// </summary>
    public sealed class PauseMenu
    {
        public const float ReferenceWidth = 960f;
        public const float MinSensitivity = 0.1f, MaxSensitivity = 3f, VolumeWhenMenuOpen = 0.5f;
        private const float FontPixelFactor = 0.75f;
        private const float PanelW = 400f, PanelH = 300f, RowW = 390f, RowH = 22.19f, RowGap = 5f, Pad = 5f, TitleH = 40f;
        private const float FramerateRefresh = 0.5f;

        public enum Row { Sensitivity = 0, Shadows, Invincibility, Framerate, Controls, Screenshot }
        public const int RowCount = 6;

        private static readonly Vector4 Normal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 Highlighted = new Vector4(0.28235295f, 0.7254902f, 0.9529412f, 1f);
        private static readonly Vector4 Pressed = new Vector4(0.78431374f, 0.78431374f, 0.78431374f, 1f);
        private static readonly string[] Labels = { "Look Sensitivity", "Shadows", "Invincibility", "Framerate Counter", "Controls", "Screenshot" };

        public bool IsOpen { get; private set; }
        public bool ControlsOpen { get; private set; }
        public float LookSensitivity { get; set; } = 1f;
        public bool ShadowsOn { get; set; } = true;
        public bool Invincible { get; set; }
        public bool FramerateOn { get; set; }
        /// <summary>Keyboard selection (EventSystem.currentSelectedGameObject), -1 = none.</summary>
        public int Selected { get; private set; } = -1;
        public Texture2D Preview { get; set; }
        public float Scale { get; private set; } = 1f;

        public event Action Opened, Closed, ScreenshotRequested;
        public event Action<float> LookSensitivityChanged;
        public event Action<bool> ShadowsChanged, InvincibleChanged, FramerateChanged;

        // layout (pixels), rebuilt when the window size changes
        private int _width, _height;
        public Rectangle Panel { get; private set; }
        public Rectangle[] Controls { get; } = new Rectangle[RowCount];   // slider / toggle / button rect per row
        public Rectangle ControlsImageRect { get; private set; }
        public Rectangle CloseRect { get; private set; }
        private Rectangle[] _rows = new Rectangle[RowCount];
        private Rectangle _previewRect;

        private int _pressed = -2;      // -1 close button, 0..5 row, -2 none
        private bool _draggingSlider;
        private readonly Vector4[] _tint = { Normal, Normal, Normal, Normal, Normal, Normal };
        private Vector4 _closeTint = Normal;
        private float _fpsTime, _fpsValue;
        private int _fpsFrames;

        // content (null in the logic-only constructor)
        private SpriteBatch _sb;
        private SpriteFont _font14, _font16, _font26;
        private Texture2D _white, _corner, _controlsPopup, _closeIcon;

        public PauseMenu() { }

        public PauseMenu(GraphicsDevice device, ContentManager content)
        {
            _sb = new SpriteBatch(device);
            _font14 = content.Load<SpriteFont>("Fonts/LiberationSans_14");
            _font16 = content.Load<SpriteFont>("Fonts/LiberationSans_16");
            _font26 = content.Load<SpriteFont>("Fonts/LiberationSans_26");
            _white = content.Load<Texture2D>("Textures/UI/TEX_White_16x16");
            _controlsPopup = content.Load<Texture2D>("Textures/UI/ControlScheme_PopUp");
            _closeIcon = content.Load<Texture2D>("Textures/UI/CloseBtn_Icon");
            // a quarter disc for the rounded corners of the default UI sprite (32 x 32 texels, anti-aliased)
            const int n = 32;
            var data = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = n - 0.5f - x, dy = n - 0.5f - y;   // distance from the disc centre at the bottom-right corner
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float a = MathHelper.Clamp(n - d + 0.5f, 0f, 1f);
                    data[y * n + x] = new Color(1f, 1f, 1f, a);   // straight alpha: the sprite batch blends NonPremultiplied
                }
            _corner = new Texture2D(device, n, n);
            _corner.SetData(data);
        }

        // ------------------------------------------------------------------ open / close

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            ControlsOpen = false;
            Selected = -1;
            _pressed = -2;
            _draggingSlider = false;
            if (Opened != null) Opened();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            ControlsOpen = false;
            _draggingSlider = false;
            if (Closed != null) Closed();
        }

        // ------------------------------------------------------------------ layout

        private void Layout(int width, int height)
        {
            _width = width;
            _height = height;
            Scale = width / ReferenceWidth;
            float canvasH = height / Scale;
            float px = (ReferenceWidth - PanelW) * 0.5f, py = (canvasH - PanelH) * 0.5f;
            Panel = P(px, py, PanelW, PanelH);
            for (int i = 0; i < RowCount; i++)
            {
                float top = py + TitleH + Pad + i * (RowH + RowGap);   // InnerPanel starts below the title; grid padding 5
                float left = px + Pad;
                _rows[i] = P(left, top, RowW, RowH);
                float cx = left + RowW * 0.5f;
                switch ((Row)i)
                {
                    case Row.Sensitivity:
                    case Row.Shadows:
                    case Row.Invincibility:
                    case Row.Framerate:
                        Controls[i] = P(cx + 25f - 80f, top + RowH * 0.5f - 10f, 160f, 20f);   // 160 x 20 at the row centre + 25
                        break;
                    case Row.Controls:
                        Controls[i] = P(cx - 24.59f - 61.53f * 0.5f, top, 61.53f, RowH);
                        break;
                    default:
                        Controls[i] = P(cx - 12.97f - 84.75f * 0.5f, top, 84.75f, RowH);
                        break;
                }
            }
            _previewRect = P(px + PanelW * 0.5f + 115.8f - 150.24f * 0.5f, py + PanelH * 0.5f + 30.16f - 84.51f * 0.5f, 150.24f, 84.51f);
            // ControlsImage: centre anchor, pivot (0.5, 1), +109.9 up from the centre, 687.1 x 322.8
            float ix = (ReferenceWidth - 687.1f) * 0.5f, iy = canvasH * 0.5f - 109.9f;
            ControlsImageRect = P(ix, iy, 687.1f, 322.8f);
            CloseRect = P(ix + 687.1f - 15f - 20f, iy + 15f, 20f, 20f);
        }

        private Rectangle P(float x, float y, float w, float h)
        {
            return new Rectangle((int)Math.Round(x * Scale), (int)Math.Round(y * Scale), (int)Math.Round(w * Scale), (int)Math.Round(h * Scale));
        }

        // ------------------------------------------------------------------ input

        private static bool Edge(KeyboardState now, KeyboardState before, Keys k) { return now.IsKeyDown(k) && !before.IsKeyDown(k); }

        /// <summary>One frame. Call it every frame while playing: it also opens the menu (Tab / P).</summary>
        public void Update(float dt, int width, int height, MouseState mouse, MouseState prevMouse, KeyboardState keys, KeyboardState prevKeys)
        {
            if (width != _width || height != _height)
                Layout(width, height);

            bool menuKey = Edge(keys, prevKeys, Keys.Tab) || Edge(keys, prevKeys, Keys.P);
            bool cancel = Edge(keys, prevKeys, Keys.Escape);
            if (!IsOpen)
            {
                if (menuKey) Open();
                return;
            }
            if (menuKey || cancel)
            {
                if (ControlsOpen) { ControlsOpen = false; return; }   // m_MenuAction: the controls image closes first
                Close();
                return;
            }

            var point = new Point(mouse.X, mouse.Y);
            bool down = mouse.LeftButton == ButtonState.Pressed, wasDown = prevMouse.LeftButton == ButtonState.Pressed;

            // hit test
            int hover = -2;
            if (ControlsOpen)
            {
                if (CloseRect.Contains(point)) hover = -1;
            }
            else
            {
                for (int i = 0; i < RowCount; i++)
                    if (Controls[i].Contains(point)) { hover = i; break; }
            }

            if (down && !wasDown)
            {
                _pressed = hover;
                if (hover >= 0) Selected = hover;
                if (hover == (int)Row.Sensitivity) _draggingSlider = true;
            }
            if (_draggingSlider && down)
                SetSensitivity(SliderValue(point.X));
            if (!down && wasDown)
            {
                if (_pressed != -2 && _pressed == hover)
                    Activate(_pressed, point);
                _pressed = -2;
                _draggingSlider = false;
            }

            // keyboard: the first up / down selects the slider, then Selectable navigation (no wrap)
            if (!ControlsOpen)
            {
                bool up = Edge(keys, prevKeys, Keys.Up) || Edge(keys, prevKeys, Keys.W);
                bool dn = Edge(keys, prevKeys, Keys.Down) || Edge(keys, prevKeys, Keys.S);
                bool left = Edge(keys, prevKeys, Keys.Left) || Edge(keys, prevKeys, Keys.A);
                bool right = Edge(keys, prevKeys, Keys.Right) || Edge(keys, prevKeys, Keys.D);
                bool submit = Edge(keys, prevKeys, Keys.Enter) || Edge(keys, prevKeys, Keys.Space);
                if (Selected < 0)
                {
                    if (up || dn) Selected = (int)Row.Sensitivity;
                }
                else
                {
                    if (dn) Selected = Math.Min(RowCount - 1, Selected + 1);
                    else if (up) Selected = Math.Max(0, Selected - 1);
                    else if (Selected == (int)Row.Sensitivity && (left || right))
                        SetSensitivity(LookSensitivity + (right ? 1f : -1f) * (MaxSensitivity - MinSensitivity) * 0.1f);   // Slider keyboard step: a tenth of the range
                    else if (submit) Activate(Selected, new Point(-1, -1));
                }
            }
            else if (Edge(keys, prevKeys, Keys.Enter) || Edge(keys, prevKeys, Keys.Space))
            {
                ControlsOpen = false;
            }

            // tints
            for (int i = 0; i < RowCount; i++)
            {
                Vector4 goal = Normal;
                if (!ControlsOpen)
                {
                    if (_pressed == i && hover == i && down) goal = Pressed;
                    else if (hover == i || Selected == i) goal = Highlighted;
                }
                _tint[i] = Vector4.Lerp(_tint[i], goal, Math.Min(1f, dt / 0.1f));
            }
            Vector4 cg = hover == -1 ? (_pressed == -1 && down ? Pressed : Highlighted) : Normal;
            _closeTint = Vector4.Lerp(_closeTint, cg, Math.Min(1f, dt / 0.1f));
        }

        private float SliderValue(int mouseX)
        {
            Rectangle r = Controls[(int)Row.Sensitivity];
            float inset = 10f * Scale;   // the handle slide area is 20 narrower than the slider
            float t = MathHelper.Clamp((mouseX - (r.X + inset)) / Math.Max(1f, r.Width - 2f * inset), 0f, 1f);
            return MinSensitivity + t * (MaxSensitivity - MinSensitivity);
        }

        private void SetSensitivity(float value)
        {
            value = MathHelper.Clamp(value, MinSensitivity, MaxSensitivity);
            if (Math.Abs(value - LookSensitivity) < 1e-6f) return;
            LookSensitivity = value;
            if (LookSensitivityChanged != null) LookSensitivityChanged(value);
        }

        private void Activate(int index, Point at)
        {
            switch (index)
            {
                case -1: ControlsOpen = false; break;   // CloseButton
                case (int)Row.Sensitivity: if (at.X >= 0) SetSensitivity(SliderValue(at.X)); break;
                case (int)Row.Shadows: ShadowsOn = !ShadowsOn; if (ShadowsChanged != null) ShadowsChanged(ShadowsOn); break;
                case (int)Row.Invincibility: Invincible = !Invincible; if (InvincibleChanged != null) InvincibleChanged(Invincible); break;
                case (int)Row.Framerate: FramerateOn = !FramerateOn; if (FramerateChanged != null) FramerateChanged(FramerateOn); break;
                case (int)Row.Controls: ControlsOpen = true; break;
                case (int)Row.Screenshot: if (ScreenshotRequested != null) ScreenshotRequested(); break;
            }
        }

        // ------------------------------------------------------------------ framerate counter

        /// <summary>FramerateCounter.Update: the frames over PollingTime 0.5 s as an integer.</summary>
        public void CountFrame(float realDt)
        {
            _fpsTime += realDt;
            _fpsFrames++;
            if (_fpsTime >= FramerateRefresh)
            {
                _fpsValue = (float)Math.Round(_fpsFrames / _fpsTime);
                _fpsTime = 0f;
                _fpsFrames = 0;
            }
        }

        public int Framerate { get { return (int)_fpsValue; } }

        // ------------------------------------------------------------------ draw

        private void Rounded(Rectangle r, Color color, float radiusCanvas = 5f)
        {
            int rad = (int)Math.Max(1, Math.Round(radiusCanvas * Scale));
            rad = Math.Min(rad, Math.Min(r.Width, r.Height) / 2);
            _sb.Draw(_white, new Rectangle(r.X + rad, r.Y, r.Width - 2 * rad, r.Height), color);
            _sb.Draw(_white, new Rectangle(r.X, r.Y + rad, rad, r.Height - 2 * rad), color);
            _sb.Draw(_white, new Rectangle(r.Right - rad, r.Y + rad, rad, r.Height - 2 * rad), color);
            _sb.Draw(_corner, new Rectangle(r.Right - rad, r.Bottom - rad, rad, rad), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            _sb.Draw(_corner, new Rectangle(r.X, r.Bottom - rad, rad, rad), null, color, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
            _sb.Draw(_corner, new Rectangle(r.Right - rad, r.Y, rad, rad), null, color, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0f);
            _sb.Draw(_corner, new Rectangle(r.X, r.Y, rad, rad), null, color, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f);
        }

        private static Color C(Vector4 v) { return new Color(v.X, v.Y, v.Z, v.W); }

        private void Text(SpriteFont font, int nominal, string text, Rectangle r, float unitySize, Color color, bool left)
        {
            float s = Scale * unitySize / nominal * FontPixelFactor;
            Vector2 size = font.MeasureString(text) * s;
            float x = left ? r.X : r.X + (r.Width - size.X) * 0.5f;
            _sb.DrawString(font, text, new Vector2((float)Math.Round(x), (float)Math.Round(r.Y + (r.Height - size.Y) * 0.5f)), color, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
        }

        /// <summary>The framerate counter (HUD) and, while open, the menu over the already drawn frame.</summary>
        public void Draw(int width, int height)
        {
            if (_sb == null) return;
            if (width != _width || height != _height) Layout(width, height);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            if (FramerateOn)
            {
                // FramerateCounter: top right, pivot (1, 1), pos (-22, -10), 200 x 26.7, size 16, left aligned
                var r = P(ReferenceWidth - 22f - 200f, 10f, 200f, 26.7f);
                Text(_font16, 16, ((int)_fpsValue).ToString(), r, 16f, Color.White, true);
            }
            if (IsOpen)
            {
                _sb.Draw(_white, new Rectangle(0, 0, width, height), new Color(0f, 0f, 0f, 0.5176471f));
                Rounded(Panel, Color.Black, 6f);
                Text(_font26, 26, "Menu", new Rectangle(Panel.X, Panel.Y, Panel.Width, (int)Math.Round(TitleH * Scale)), 26f, Color.White, false);
                for (int i = 0; i < RowCount; i++)
                {
                    var label = new Rectangle(_rows[i].X + (int)Math.Round(10f * Scale), _rows[i].Y, (int)Math.Round(160f * Scale), _rows[i].Height);
                    Text(_font14, 14, Labels[i], label, 14f, Color.White, true);
                    Rectangle c = Controls[i];
                    Color tint = C(_tint[i]);
                    switch ((Row)i)
                    {
                        case Row.Sensitivity:
                        {
                            Rounded(new Rectangle(c.X, c.Y + c.Height / 4, c.Width, c.Height / 2), Color.White, 3f);
                            float t = (LookSensitivity - MinSensitivity) / (MaxSensitivity - MinSensitivity);
                            float hx = c.X + 10f * Scale + t * (c.Width - 20f * Scale);
                            int d = (int)Math.Round(20f * Scale);
                            Rounded(new Rectangle((int)Math.Round(hx - d * 0.5f), c.Y + c.Height / 2 - d / 2, d, d), tint, 10f);
                            break;
                        }
                        case Row.Shadows:
                        case Row.Invincibility:
                        case Row.Framerate:
                        {
                            int d = (int)Math.Round(20f * Scale);
                            var box = new Rectangle(c.X, c.Y, d, d);
                            Rounded(box, tint, 3f);
                            bool on = i == (int)Row.Shadows ? ShadowsOn : i == (int)Row.Invincibility ? Invincible : FramerateOn;
                            if (on) Check(box);
                            break;
                        }
                        default:
                            Rounded(c, tint, 3f);
                            Text(_font14, 14, i == (int)Row.Controls ? "Show" : "Take new", c, 14f, new Color(0.19607843f, 0.19607843f, 0.19607843f), false);
                            break;
                    }
                }
                if (Preview != null)
                    _sb.Draw(Preview, _previewRect, Color.White);
                if (ControlsOpen)
                {
                    _sb.Draw(_controlsPopup, ControlsImageRect, Color.White);
                    Rounded(CloseRect, C(_closeTint), 3f);
                    float side = Math.Min(CloseRect.Width, CloseRect.Height);
                    float aspect = _closeIcon.Width / (float)_closeIcon.Height;
                    float w = aspect >= 1f ? side : side * aspect, h = aspect >= 1f ? side / aspect : side;
                    _sb.Draw(_closeIcon, new Rectangle((int)Math.Round(CloseRect.Center.X - w * 0.5f), (int)Math.Round(CloseRect.Center.Y - h * 0.5f), (int)Math.Round(w), (int)Math.Round(h)), Color.Black);
                }
            }
            _sb.End();
        }

        // a check mark in the toggle box (the prefab's Checkmark graphic is white on white; drawn dark so the state reads)
        private void Check(Rectangle box)
        {
            var dark = new Color(0.19607843f, 0.19607843f, 0.19607843f);
            float t = Math.Max(2f, box.Width * 0.14f);
            Vector2 a = new Vector2(box.X + box.Width * 0.22f, box.Y + box.Height * 0.52f), b = new Vector2(box.X + box.Width * 0.42f, box.Y + box.Height * 0.74f), c = new Vector2(box.X + box.Width * 0.80f, box.Y + box.Height * 0.26f);
            Line(a, b, t, dark);
            Line(b, c, t, dark);
        }

        private void Line(Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 d = b - a;
            float len = d.Length();
            _sb.Draw(_white, a, null, color, (float)Math.Atan2(d.Y, d.X), new Vector2(0f, 0.5f * _white.Height), new Vector2(len / _white.Width, thickness / _white.Height), SpriteEffects.None, 0f);
        }
    }
}
