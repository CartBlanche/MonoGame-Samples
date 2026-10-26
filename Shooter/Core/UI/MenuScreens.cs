using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shooter.UI
{
    public enum MenuScreen { None, Intro, Win, Lose }

    public enum MenuAction { None, Play, PlayAgain, Menu }

    /// <summary>
    /// The IntroMenu, WinScene and LoseScene canvases (960 x 540 reference, CanvasScaler match width): a stretched background image, the
    /// VignetteImage (black 50 %, Transparent_Center sprite), the title text and white Image buttons (Selectable colours: normal white,
    /// highlighted / selected (0.282, 0.725, 0.953), pressed 0.784 grey, fade 0.1 s) with black Roboto-Black text. IntroMenu also has the
    /// Controls pop-up (ControlScheme_PopUp with a close button). Win and Lose are identical but for the title; their MenuButton goes to the intro,
    /// RestartButton ("Play Again") reloads MainScene. MenuNavigation: the first navigation / submit key selects the default button, then the
    /// arrow keys move the selection and Enter / Space press it.
    /// The logic (layout, mouse, keys) works without a GraphicsDevice so the self-tests can drive it; <see cref="Draw"/> needs the content constructor.
    /// </summary>
    public class MenuScreens
    {
        public const float ReferenceWidth = 960f, ReferenceHeight = 540f;
        private const float FontPixelFactor = 0.75f;   // same as the HUD: the spritefont size is in points
        private const float FadeDuration = 0.1f;

        private static readonly Vector4 Normal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 Highlighted = new Vector4(0.28235295f, 0.7254902f, 0.9529412f, 1f);
        private static readonly Vector4 Pressed = new Vector4(0.78431374f, 0.78431374f, 0.78431374f, 1f);

        public class Button
        {
            public string Id;
            public string Text;
            public float FontSize;
            public MenuAction Action;
            public Rectangle Bounds;
            public Vector4 Color = Normal;
        }

        public MenuScreen Screen { get; private set; }
        public bool ControlsOpen { get; private set; }
        public List<Button> Buttons { get; } = new List<Button>();
        public Button CloseButton { get; private set; }
        public Rectangle ControlsRect { get; private set; }
        /// <summary>Index into <see cref="Buttons"/> (top to bottom) of the keyboard selection, -1 = none (EventSystem.currentSelectedGameObject).</summary>
        public int Selected { get; private set; } = -1;
        public string Title { get; private set; } = "";
        public float Scale { get; private set; } = 1f;

        private int _pressed = -1;     // button the mouse went down on
        private int _hover = -1;
        private int _width, _height;

        // ---- content (null in the logic-only constructor)
        private readonly ContentManager _content;
        private readonly GraphicsDevice _device;
        private SpriteBatch _sb;
        private SpriteFont _font72, _font45, _font25;
        private Texture2D _white, _vignette, _controls, _closeIcon, _bgIntro, _bgEnd;

        public MenuScreens() { }

        public MenuScreens(GraphicsDevice device, ContentManager content)
        {
            _device = device;
            _content = content;
            _sb = new SpriteBatch(device);
            _font72 = content.Load<SpriteFont>("Fonts/Roboto-Black_72");
            _font45 = content.Load<SpriteFont>("Fonts/Roboto-Black_45");
            _font25 = content.Load<SpriteFont>("Fonts/Roboto-Black_25");
            _white = content.Load<Texture2D>("Textures/UI/TEX_White_16x16");
            _vignette = content.Load<Texture2D>("Textures/UI/Transparent_Center");
            _controls = content.Load<Texture2D>("Textures/UI/ControlScheme_PopUp");
            _closeIcon = content.Load<Texture2D>("Textures/UI/CloseBtn_Icon");
            _bgIntro = content.Load<Texture2D>("Textures/Backgrounds/Unity_skyboxBG01");
            _bgEnd = content.Load<Texture2D>("Textures/Backgrounds/Unity_skyboxBG02");
        }

        public void Show(MenuScreen screen)
        {
            Screen = screen;
            ControlsOpen = false;
            Selected = -1;
            _pressed = -1;
            _hover = -1;
            Buttons.Clear();
            CloseButton = null;
            Title = screen == MenuScreen.Win ? "You WIN" : screen == MenuScreen.Lose ? "You LOSE" : "";
            _width = 0;   // layout is rebuilt on the next Update
        }

        public void Hide() { Show(MenuScreen.None); }

        /// <summary>Open / close the Controls pop-up (the Controls button or the close button do the same on click).</summary>
        public void SetControlsOpen(bool open) { ControlsOpen = open && Screen == MenuScreen.Intro; }

        // ------------------------------------------------------------------ layout

        /// <summary>Canvas (y-up, bottom-left origin) rect of an anchored element to pixels: anchors (0.5, 0), pivot (0.5, 0).</summary>
        private Rectangle BottomCentre(float posY, float w, float h)
        {
            float canvasH = _height / Scale;
            float x = ReferenceWidth * 0.5f - w * 0.5f;
            float top = canvasH - posY - h;
            return ToPixels(x, top, w, h);
        }

        private Rectangle ToPixels(float x, float y, float w, float h)
        {
            return new Rectangle((int)Math.Round(x * Scale), (int)Math.Round(y * Scale), (int)Math.Round(w * Scale), (int)Math.Round(h * Scale));
        }

        private void Layout(int width, int height)
        {
            _width = width;
            _height = height;
            Scale = width / ReferenceWidth;   // CanvasScaler: scale with screen size, match width 0
            float canvasH = height / Scale;
            Buttons.Clear();
            if (Screen == MenuScreen.Intro)
            {
                // Selectable order = top to bottom on screen: Controls (above), Play
                Buttons.Add(new Button { Id = "ControlsButton", Text = "Controls", FontSize = 25f, Action = MenuAction.None, Bounds = BottomCentre(105f, 380f, 30f) });
                Buttons.Add(new Button { Id = "StartButton", Text = "Play", FontSize = 45f, Action = MenuAction.Play, Bounds = BottomCentre(25f, 380f, 70f) });
            }
            else if (Screen == MenuScreen.Win || Screen == MenuScreen.Lose)
            {
                Buttons.Add(new Button { Id = "MenuButton", Text = "Menu", FontSize = 25f, Action = MenuAction.Menu, Bounds = BottomCentre(105f, 380f, 30f) });
                Buttons.Add(new Button { Id = "RestartButton", Text = "Play Again", FontSize = 45f, Action = MenuAction.PlayAgain, Bounds = BottomCentre(25f, 380f, 70f) });
            }
            if (Screen == MenuScreen.Intro)
            {
                // ControlsImage: stretched, sizeDelta (-100, -155), anchored y +27.5 -> 50 from the top, 105 from the bottom, 50 each side
                ControlsRect = ToPixels(50f, 50f, ReferenceWidth - 100f, canvasH - 155f);
                // CloseButton: top right of it, pivot (1, 1), pos (-15, -15), 20 x 20
                var r = ControlsRect;
                float cs = 20f * Scale, off = 15f * Scale;
                CloseButton = new Button { Id = "CloseButton", Text = "", Bounds = new Rectangle((int)Math.Round(r.Right - off - cs), (int)Math.Round(r.Y + off), (int)Math.Round(cs), (int)Math.Round(cs)) };
            }
        }

        private int DefaultSelection { get { return Buttons.Count - 1; } }   // RestartButton / StartButton (Selectable DefaultSelection)

        // ------------------------------------------------------------------ input

        /// <summary>One frame of input; returns the action the screen asks for (Play, PlayAgain, Menu).</summary>
        public MenuAction Update(float dt, int width, int height, MouseState mouse, MouseState prevMouse, KeyboardState keys, KeyboardState prevKeys)
        {
            if (Screen == MenuScreen.None)
            {
                return MenuAction.None;
            }
            if (width != _width || height != _height)
            {
                Layout(width, height);
            }

            var point = new Point(mouse.X, mouse.Y);
            bool down = mouse.LeftButton == ButtonState.Pressed, wasDown = prevMouse.LeftButton == ButtonState.Pressed;
            MenuAction result = MenuAction.None;

            // hit test: while the pop-up is open it blocks the buttons behind it (its image is a raycast target) except its own close button
            int hover = -1;
            bool overClose = false;
            if (ControlsOpen)
            {
                overClose = CloseButton != null && CloseButton.Bounds.Contains(point);
            }
            else
            {
                for (int i = 0; i < Buttons.Count; i++)
                {
                    if (Buttons[i].Bounds.Contains(point)) { hover = i; break; }
                }
            }
            _hover = hover;

            // mouse: press on a button, release over the same one = onClick
            int target = ControlsOpen ? (overClose ? -2 : -1) : hover;
            if (down && !wasDown)
            {
                _pressed = target;
                if (hover >= 0) Selected = hover;   // a click selects the button
            }
            else if (!down && wasDown)
            {
                if (_pressed != -1 && _pressed == target)
                {
                    result = Activate(_pressed);
                }
                _pressed = -1;
            }

            // keyboard (MenuNavigation + Selectable automatic navigation, no wrap)
            bool navDown = Edge(keys, prevKeys, Keys.Down) || Edge(keys, prevKeys, Keys.S) || (Edge(keys, prevKeys, Keys.Tab) && !keys.IsKeyDown(Keys.LeftShift) && !keys.IsKeyDown(Keys.RightShift));
            bool navUp = Edge(keys, prevKeys, Keys.Up) || Edge(keys, prevKeys, Keys.W) || (Edge(keys, prevKeys, Keys.Tab) && (keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift)));
            bool submit = Edge(keys, prevKeys, Keys.Enter) || Edge(keys, prevKeys, Keys.Space);
            if (!ControlsOpen && Buttons.Count > 0)
            {
                if (Selected < 0)
                {
                    if (submit || navDown || navUp || Edge(keys, prevKeys, Keys.Left) || Edge(keys, prevKeys, Keys.Right))
                    {
                        Selected = DefaultSelection;
                    }
                }
                else if (submit)
                {
                    result = Activate(Selected);
                }
                else if (navDown)
                {
                    Selected = Math.Min(Buttons.Count - 1, Selected + 1);
                }
                else if (navUp)
                {
                    Selected = Math.Max(0, Selected - 1);
                }
            }
            else if (ControlsOpen && (submit || Edge(keys, prevKeys, Keys.Escape)))
            {
                ControlsOpen = false;   // the close button is the only selectable thing in the pop-up
            }

            // colour tint transition (Selectable ColorTint, fade 0.1 s)
            for (int i = 0; i < Buttons.Count; i++)
            {
                Vector4 goal = Normal;
                if (!ControlsOpen)
                {
                    if (_pressed == i && i == hover) goal = Pressed;
                    else if (i == hover || i == Selected) goal = Highlighted;
                }
                Buttons[i].Color = Vector4.Lerp(Buttons[i].Color, goal, Math.Min(1f, dt / FadeDuration));
            }
            if (CloseButton != null)
            {
                Vector4 goal = overClose ? (_pressed == -2 && down ? Pressed : Highlighted) : Normal;
                CloseButton.Color = Vector4.Lerp(CloseButton.Color, goal, Math.Min(1f, dt / FadeDuration));
            }
            return result;
        }

        private static bool Edge(KeyboardState now, KeyboardState before, Keys k) { return now.IsKeyDown(k) && !before.IsKeyDown(k); }

        private MenuAction Activate(int index)
        {
            if (index == -2)
            {
                ControlsOpen = false;   // CloseButton -> ToggleGameObjectButton
                return MenuAction.None;
            }
            Button b = Buttons[index];
            if (b.Id == "ControlsButton")
            {
                ControlsOpen = !ControlsOpen;
                return MenuAction.None;
            }
            return b.Action;
        }

        // ------------------------------------------------------------------ draw

        /// <summary>Draws the current screen over the whole back buffer (or the bound render target) of the given size.</summary>
        public void Draw(int width, int height)
        {
            if (Screen == MenuScreen.None || _sb == null)
            {
                return;
            }
            if (width != _width || height != _height)
            {
                Layout(width, height);
            }
            var full = new Rectangle(0, 0, width, height);
            _sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            _sb.Draw(Screen == MenuScreen.Intro ? _bgIntro : _bgEnd, full, Color.White);
            _sb.Draw(_vignette, full, new Color(0f, 0f, 0f, 0.5019608f));

            if (Screen != MenuScreen.Intro)
            {
                // Text (TMP): centre anchor, pos (0, 31.7), 643.8 x 304.4, size 72, white, centred both ways
                float canvasH = height / Scale;
                Rectangle r = ToPixels(ReferenceWidth * 0.5f - 643.8f * 0.5f, canvasH * 0.5f - 31.7f - 304.4f * 0.5f, 643.8f, 304.4f);
                Text(_font72, 72, Title, r, 72f, Color.White);
            }
            foreach (Button b in Buttons)
            {
                _sb.Draw(_white, b.Bounds, ToColor(b.Color));
                SpriteFont f = b.FontSize >= 45f ? _font45 : _font25;
                Text(f, b.FontSize >= 45f ? 45 : 25, b.Text, b.Bounds, b.FontSize, Color.Black);
            }
            if (ControlsOpen)
            {
                _sb.Draw(_controls, ControlsRect, Color.White);
                _sb.Draw(_white, CloseButton.Bounds, ToColor(CloseButton.Color));
                // Image child: stretched, black, preserve aspect
                float side = Math.Min(CloseButton.Bounds.Width, CloseButton.Bounds.Height);
                float aspect = _closeIcon.Width / (float)_closeIcon.Height;
                float w = aspect >= 1f ? side : side * aspect, h = aspect >= 1f ? side / aspect : side;
                var icon = new Rectangle((int)Math.Round(CloseButton.Bounds.Center.X - w * 0.5f), (int)Math.Round(CloseButton.Bounds.Center.Y - h * 0.5f), (int)Math.Round(w), (int)Math.Round(h));
                _sb.Draw(_closeIcon, icon, Color.Black);
            }
            _sb.End();
        }

        private void Text(SpriteFont font, int nominal, string text, Rectangle r, float unitySize, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            float s = Scale * unitySize / nominal * FontPixelFactor;
            Vector2 size = font.MeasureString(text) * s;
            var pos = new Vector2((float)Math.Round(r.X + (r.Width - size.X) * 0.5f), (float)Math.Round(r.Y + (r.Height - size.Y) * 0.5f));
            _sb.DrawString(font, text, pos, color, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
        }

        private static Color ToColor(Vector4 v) { return new Color(v.X, v.Y, v.Z, v.W); }

        /// <summary>The fade-to-black canvas (EndGameFadeCanvas Image, black) over the already drawn frame.</summary>
        public void DrawFade(int width, int height, float alpha)
        {
            if (_sb == null || alpha <= 0f)
            {
                return;
            }
            _sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            _sb.Draw(_white, new Rectangle(0, 0, width, height), new Color(0f, 0f, 0f, Math.Min(1f, alpha)));
            _sb.End();
        }
    }
}
