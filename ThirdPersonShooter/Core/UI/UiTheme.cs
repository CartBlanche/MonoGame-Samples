using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Look constants of the Godot menu theme, read from tps-demo-master/menu/button_*.tres, menu.tscn and button_font.tres. The UI lays out in a
    /// virtual 1920x1080 space (Godot stretch canvas_items) anchored top-left and scaled by min(w/1920, h/1080). All colours are
    /// straight alpha here; <see cref="C"/> premultiplies them for the SpriteBatch.
    /// </summary>
    public static class UiTheme
    {
        public const int VirtualW = 1920, VirtualH = 1080;

        // menu.tscn: buttons 300x50 at x=100, y = 390 + 100 n, theme_override_font_sizes/font_size = 30, alignment = left (0)
        public const int ButtonX = 100, ButtonY0 = 390, ButtonW = 300, ButtonH = 50, ButtonStep = 100, ButtonFontPx = 30;
        // button_normal.tres: content_margin_left/right 20, top/bottom 4; skew (0.5, 0); corner radius 6
        public const int MarginX = 20, Radius = 6;
        /// <summary>Godot skew.x = 0.5 renders a horizontal shear of about 0.45 x height in godot-01-main-menu.png (measured), centred on the box.</summary>
        public const float SkewVisual = 0.45f;

        // button_normal.tres: bg white a0.031, border 4, border_color (0.145,0.145,0.145,0.5)
        public static readonly Color NormalFill = C(1, 1, 1, 0.0314f), NormalBorder = C(0.145f, 0.145f, 0.145f, 0.502f); public const int NormalBorderW = 4;
        // button_hover.tres: bg white a0, border 3, border_color (1,1,1,0.251)
        public static readonly Color HoverFill = C(1, 1, 1, 0f), HoverBorder = C(1, 1, 1, 0.251f); public const int HoverBorderW = 3;
        // button_pressed.tres: bg black a0.502, border 3 (default border colour 0.8 grey)
        public static readonly Color PressedFill = C(0, 0, 0, 0.502f), PressedBorder = C(0.8f, 0.8f, 0.8f, 1f); public const int PressedBorderW = 3;
        // button_focus.tres: draw_center false, border 5, border_color (0.376,0.863,1,1)
        public static readonly Color FocusBorder = C(0.3764706f, 0.8627451f, 1f, 1f); public const int FocusBorderW = 5;
        // menu.tscn Theme: font_color white; font_color_disabled (0.9,0.9,0.9,0.2)
        public static readonly Color Text = Color.White, TextDisabled = C(0.9f, 0.9f, 0.9f, 0.2f);
        // not in Godot (our additions): secondary text, slider track
        public static readonly Color TextDim = C(1, 1, 1, 0.55f), SliderTrack = C(1, 1, 1, 0.18f), SliderFill = C(0.3764706f, 0.8627451f, 1f, 0.9f);
        public static readonly Color Dim = C(0, 0, 0, 0.6f);

        // Fonts: PT Sans Bold (button_font.tres), baked at 22 / 30 / 44 px by the content pipeline
        public static SpriteFont Font22, Font30, Font44;

        public static void LoadFonts(ContentManager c)
        {
            Font22 = c.Load<SpriteFont>("Fonts/Font22"); Font30 = c.Load<SpriteFont>("Fonts/Font30"); Font44 = c.Load<SpriteFont>("Fonts/Font44");
        }

        /// <summary>Straight-alpha colour to the premultiplied colour SpriteBatch.AlphaBlend expects.</summary>
        public static Color C(float r, float g, float b, float a) => new Color(r * a, g * a, b * a, a);
    }

    /// <summary>Per-frame UI input snapshot (edges, plus key repeat for the directions) in virtual UI coordinates.</summary>
    public struct UiInput
    {
        public bool Up, Down, Left, Right, Confirm, Back;
        public Vector2 Mouse; public bool MouseMoved, MouseDown, MousePressed, MouseReleased;
    }

    /// <summary>Builds <see cref="UiInput"/> from keyboard (arrows/WASD, Enter/Space, Esc), gamepad (d-pad/left stick, A, B/Back) and mouse. Call every frame.</summary>
    public sealed class UiInputReader
    {
        readonly Game _game;
        KeyboardState _pk; GamePadState _pp; MouseState _pm;
        float _rep; int _repDir;

        public UiInputReader(Game game) { _game = game; _pk = Keyboard.GetState(); _pp = GamePad.GetState(PlayerIndex.One); _pm = Mouse.GetState(); }

        public UiInput Poll(float dt, float uiScale)
        {
            var k = Keyboard.GetState(); var p = GamePad.GetState(PlayerIndex.One); var m = Mouse.GetState();
            var r = new UiInput();
            if (_game.IsActive)
            {
                bool up = k.IsKeyDown(Keys.Up) || k.IsKeyDown(Keys.W) || p.DPad.Up == ButtonState.Pressed || p.ThumbSticks.Left.Y > 0.6f;
                bool dn = k.IsKeyDown(Keys.Down) || k.IsKeyDown(Keys.S) || p.DPad.Down == ButtonState.Pressed || p.ThumbSticks.Left.Y < -0.6f;
                bool lf = k.IsKeyDown(Keys.Left) || k.IsKeyDown(Keys.A) || p.DPad.Left == ButtonState.Pressed || p.ThumbSticks.Left.X < -0.6f;
                bool rt = k.IsKeyDown(Keys.Right) || k.IsKeyDown(Keys.D) || p.DPad.Right == ButtonState.Pressed || p.ThumbSticks.Left.X > 0.6f;
                int dir = up ? 1 : dn ? 2 : lf ? 3 : rt ? 4 : 0;
                if (dir == 0) { _repDir = 0; _rep = 0; }
                else if (dir != _repDir) { _repDir = dir; _rep = 0.4f; Set(ref r, dir); }           // first press, then repeat after 0.4 s every 0.08 s
                else { _rep -= dt; if (_rep <= 0f) { _rep += 0.08f; Set(ref r, dir); } }
                r.Confirm = Pressed(k, _pk, Keys.Enter) || Pressed(k, _pk, Keys.Space) || (p.Buttons.A == ButtonState.Pressed && _pp.Buttons.A != ButtonState.Pressed);
                r.Back = Pressed(k, _pk, Keys.Escape) || (p.Buttons.B == ButtonState.Pressed && _pp.Buttons.B != ButtonState.Pressed) || (p.Buttons.Back == ButtonState.Pressed && _pp.Buttons.Back != ButtonState.Pressed);
                r.Mouse = new Vector2(m.X, m.Y) / Math.Max(uiScale, 0.01f);
                r.MouseMoved = m.X != _pm.X || m.Y != _pm.Y;
                r.MouseDown = m.LeftButton == ButtonState.Pressed;
                r.MousePressed = r.MouseDown && _pm.LeftButton != ButtonState.Pressed;
                r.MouseReleased = !r.MouseDown && _pm.LeftButton == ButtonState.Pressed;
            }
            _pk = k; _pp = p; _pm = m;
            return r;
        }

        static void Set(ref UiInput r, int dir) { if (dir == 1) r.Up = true; else if (dir == 2) r.Down = true; else if (dir == 3) r.Left = true; else r.Right = true; }
        static bool Pressed(KeyboardState k, KeyboardState prev, Keys key) => k.IsKeyDown(key) && !prev.IsKeyDown(key);
    }

    /// <summary>SpriteBatch wrapper for the virtual 1920x1080 UI space: solid/slanted rects (drawn row by row from a 1x1 pixel, so no texture assets) and text.</summary>
    public sealed class UiRenderer
    {
        readonly SpriteBatch _sb; readonly Texture2D _px;
        public float Scale { get; private set; } = 1f;

        public UiRenderer(GraphicsDevice gd) { _sb = new SpriteBatch(gd); _px = new Texture2D(gd, 1, 1); _px.SetData(new[] { Color.White }); }

        public static float ScaleFor(int w, int h) => Math.Min(w / (float)UiTheme.VirtualW, h / (float)UiTheme.VirtualH);

        public void Begin(int w, int h)
        {
            Scale = ScaleFor(w, h);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(Scale, Scale, 1f));
        }
        public void End() => _sb.End();

        public void Rect(float x, float y, float w, float h, Color premul)
        {
            if (w <= 0 || h <= 0 || premul.A == 0) return;
            _sb.Draw(_px, new Vector2(x, y), null, premul, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0f);
        }
        public void Rect(Rectangle r, Color premul) => Rect(r.X, r.Y, r.Width, r.Height, premul);
        public void Image(Texture2D t, Vector2 pos, Color premul) => _sb.Draw(t, pos, premul);
        /// <summary>Texture stretched into a w x h virtual-pixel box (TextureRect with expand_mode = ignore size).</summary>
        public void Image(Texture2D t, float x, float y, float w, float h, Color premul) => _sb.Draw(t, new Vector2(x, y), null, premul, 0f, Vector2.Zero, new Vector2(w / t.Width, h / t.Height), SpriteEffects.None, 0f);
        public void Text(SpriteFont f, string s, Vector2 pos, Color premul) { if (!string.IsNullOrEmpty(s) && premul.A > 0) _sb.DrawString(f, s, new Vector2(MathF.Round(pos.X), MathF.Round(pos.Y)), premul); }
        /// <summary>Text with its vertical centre at <paramref name="cy"/>, left edge at x.</summary>
        public void TextMid(SpriteFont f, string s, float x, float cy, Color premul) => Text(f, s, new Vector2(x, cy - f.MeasureString("Ag").Y * 0.5f), premul);
        public void TextMidRight(SpriteFont f, string s, float right, float cy, Color premul) => TextMid(f, s, right - f.MeasureString(s).X, cy, premul);
        public void TextMidCentre(SpriteFont f, string s, float cx, float cy, Color premul) => TextMid(f, s, cx - f.MeasureString(s).X * 0.5f, cy, premul);

        /// <summary>
        /// Slanted (parallelogram, StyleBoxFlat skew) rect with rounded corners: one 1-unit row at a time. Rows are shifted by
        /// <c>SkewVisual*h*(0.5 - y/h)</c> so the top leans right like button_*.tres. Fill and border segments never overlap (alpha-safe).
        /// </summary>
        public void Slanted(Rectangle b, Color fill, int border, Color borderCol, bool drawFill = true, int radius = UiTheme.Radius)
        {
            int h = b.Height;
            for (int y = 0; y < h; y++)
            {
                float shift = UiTheme.SkewVisual * h * (0.5f - (y + 0.5f) / h);
                float inset = 0f; float e = Math.Min(y + 0.5f, h - y - 0.5f);
                if (e < radius) inset = radius - MathF.Sqrt(Math.Max(0f, radius * radius - (radius - e) * (radius - e)));
                float xl = b.X + shift + inset, xr = b.Right + shift - inset, ry = b.Y + y;
                if (border > 0 && (y < border || y >= h - border)) { Rect(xl, ry, xr - xl, 1f, borderCol); continue; }
                if (border > 0) { Rect(xl, ry, border, 1f, borderCol); Rect(xr - border, ry, border, 1f, borderCol); }
                if (drawFill) Rect(xl + border, ry, xr - xl - 2 * border, 1f, fill);
            }
        }

        /// <summary>The Godot Button look (button_normal/hover/pressed/focus.tres): base box by state, then the blue focus outline on top.</summary>
        public void ButtonBox(Rectangle b, bool enabled, bool hover, bool pressed, bool focused)
        {
            if (!enabled) { Slanted(b, UiTheme.NormalFill * 0.6f, UiTheme.NormalBorderW, UiTheme.NormalBorder * 0.6f); return; }
            if (pressed) Slanted(b, UiTheme.PressedFill, UiTheme.PressedBorderW, UiTheme.PressedBorder);
            else if (hover) Slanted(b, UiTheme.HoverFill, UiTheme.HoverBorderW, UiTheme.HoverBorder);
            else Slanted(b, UiTheme.NormalFill, UiTheme.NormalBorderW, UiTheme.NormalBorder);
            if (focused) Slanted(b, Color.Transparent, UiTheme.FocusBorderW, UiTheme.FocusBorder, drawFill: false);
        }

        /// <summary>Full-screen dim (pause / modal backdrop).</summary>
        public void DimScreen(int w, int h) => Rect(0, 0, w / Scale + 2, h / Scale + 2, UiTheme.Dim);
    }
}
