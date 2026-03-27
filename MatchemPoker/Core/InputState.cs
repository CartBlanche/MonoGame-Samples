using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace MatchemPoker
{
    /// <summary>
    /// Unified input handler for touch, mouse, keyboard, and gamepad.
    /// Maintains a single authoritative cursor position across all input devices.
    ///
    /// Priority / update rules:
    ///   1. Touch — always wins, updates cursor directly from finger position.
    ///   2. Mouse — updates cursor only when the OS pointer physically moves.
    ///              This lets gamepad keep ownership when the mouse is at rest.
    ///   3. GamePad left-stick — *delta* based (accumulates pixels/second) so
    ///              releasing the stick leaves the cursor where it is, not snapping
    ///              it back to the deadzone center.
    /// </summary>
    public class InputState
    {
        // ── Raw states ────────────────────────────────────────────────────────
        public KeyboardState CurrentKeyboard { get; private set; }
        public KeyboardState LastKeyboard    { get; private set; }

        public MouseState CurrentMouse { get; private set; }
        public MouseState LastMouse    { get; private set; }

        public TouchCollection CurrentTouch { get; private set; }

        public GamePadState CurrentGamePad { get; private set; }
        public GamePadState LastGamePad    { get; private set; }

        // ── Unified cursor ────────────────────────────────────────────────────
        private Vector2 _cursorPosition;

        /// <summary>Current cursor position in screen pixels.</summary>
        public Vector2 CursorPosition => _cursorPosition;

        /// <summary>Cursor position when a press/tap began (screen pixels).</summary>
        public Vector2 PressDownPosition { get; private set; }

        /// <summary>Cursor position when a press/tap ended (screen pixels).</summary>
        public Vector2 PressUpPosition { get; private set; }

        // ── Derived intent flags (set fresh every Update) ─────────────────────

        /// <summary>True on the exact frame a press/tap/button-down is detected.</summary>
        public bool IsNewPress { get; private set; }

        /// <summary>True on the exact frame a release/tap-up/button-up is detected.</summary>
        public bool IsNewRelease { get; private set; }

        /// <summary>True while a press is held but not the first frame (i.e. drag).</summary>
        public bool IsDragging { get; private set; }

        /// <summary>True on any frame where a press is active (including the first).</summary>
        public bool IsPressed { get; private set; }

        /// <summary>True on the exact frame ESC key or gamepad Back is pressed.</summary>
        public bool IsEscapeTriggered { get; private set; }

        /// <summary>True on the exact frame Space/Enter fires a centre-screen confirm.</summary>
        public bool IsConfirmTriggered { get; private set; }

        /// <summary>True on the exact frame Space/Enter is released after a confirm.</summary>
        public bool IsConfirmReleased { get; private set; }

        // ── Gamepad cursor speed ───────────────────────────────────────────────
        private const float GamepadCursorSpeed = 400f; // game-pixels per second

        // ── Game viewport (letterbox rect on screen + canonical game size) ────
        // CursorPosition is always expressed in game-space (0..GameWidth, 0..GameHeight).
        // SetGameViewport teaches InputState how to map raw screen coords into that space.
        private Rectangle _gameViewport;   // dest rect in screen pixels
        private int _gameWidth;
        private int _gameHeight;

        // ─────────────────────────────────────────────────────────────────────

        public InputState()
        {
            _cursorPosition = Vector2.Zero;
        }

        /// <summary>
        /// Tell InputState where the game is letterboxed on screen and what the
        /// canonical game resolution is. Call this from LoadContent after computing
        /// the letterbox rectangle. CursorPosition will then be in game-space.
        /// </summary>
        public void SetGameViewport(Rectangle screenRect, int gameWidth, int gameHeight)
        {
            _gameViewport = screenRect;
            _gameWidth    = gameWidth;
            _gameHeight   = gameHeight;
        }

        /// <summary>Maps a raw screen-pixel position into game-space coordinates.</summary>
        private Vector2 ScreenToGame(float sx, float sy)
        {
            if (_gameWidth == 0) return new Vector2(sx, sy);
            float gx = (sx - _gameViewport.X) / _gameViewport.Width  * _gameWidth;
            float gy = (sy - _gameViewport.Y) / _gameViewport.Height * _gameHeight;
            return new Vector2(gx, gy);
        }

        /// <summary>
        /// Call once per frame from Game.Update().
        /// </summary>
        public void Update(GameTime gameTime, int screenWidth, int screenHeight)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ── Snapshot previous states ──────────────────────────────────────
            LastKeyboard = CurrentKeyboard;
            LastMouse    = CurrentMouse;
            LastGamePad  = CurrentGamePad;

            CurrentKeyboard = Keyboard.GetState();
            CurrentMouse    = Mouse.GetState();
            CurrentTouch    = TouchPanel.GetState();
            CurrentGamePad  = GamePad.GetState(PlayerIndex.One);

            // ── Accumulate press intent from all devices ──────────────────────
            bool anyNewPress    = false;
            bool anyNewRelease  = false;
            bool anyHeld        = false;
            bool touchHandled   = false;

            // 1. Touch (highest priority) ─────────────────────────────────────
            foreach (TouchLocation touch in CurrentTouch)
            {
                switch (touch.State)
                {
                    case TouchLocationState.Pressed:
                        _cursorPosition   = ScreenToGame(touch.Position.X, touch.Position.Y);
                        PressDownPosition = _cursorPosition;
                        anyNewPress  = true;
                        touchHandled = true;
                        break;

                    case TouchLocationState.Moved:
                        _cursorPosition = ScreenToGame(touch.Position.X, touch.Position.Y);
                        anyHeld      = true;
                        touchHandled = true;
                        break;

                    case TouchLocationState.Released:
                        _cursorPosition  = ScreenToGame(touch.Position.X, touch.Position.Y);
                        PressUpPosition  = _cursorPosition;
                        anyNewRelease = true;
                        touchHandled  = true;
                        break;
                }
                if (touchHandled) break;  // only process first active touch
            }

            if (!touchHandled)
            {
                // 2. Mouse ────────────────────────────────────────────────────
                bool mouseMoved = CurrentMouse.X != LastMouse.X ||
                                  CurrentMouse.Y != LastMouse.Y;
                if (mouseMoved)
                    _cursorPosition = ScreenToGame(CurrentMouse.X, CurrentMouse.Y);

                bool mouseDown    = CurrentMouse.LeftButton == ButtonState.Pressed;
                bool mouseWasDown = LastMouse.LeftButton    == ButtonState.Pressed;

                if (mouseDown && !mouseWasDown)
                {
                    PressDownPosition = _cursorPosition;
                    anyNewPress = true;
                }
                else if (!mouseDown && mouseWasDown)
                {
                    PressUpPosition = _cursorPosition;
                    anyNewRelease = true;
                }
                else if (mouseDown)
                {
                    anyHeld = true;
                }

                // 3. GamePad thumbstick — delta accumulation ──────────────────
                // Only runs when mouse hasn't moved, so mouse ownership is
                // respected; releasing the stick leaves the cursor in place.
                if (!mouseMoved && CurrentGamePad.IsConnected)
                {
                    Vector2 stick = CurrentGamePad.ThumbSticks.Left;
                    if (stick.LengthSquared() > 0.01f)
                    {
                        int clampW = _gameWidth  > 0 ? _gameWidth  : screenWidth;
                        int clampH = _gameHeight > 0 ? _gameHeight : screenHeight;
                        float speed = GamepadCursorSpeed * dt;
                        _cursorPosition.X += stick.X * speed;
                        _cursorPosition.Y -= stick.Y * speed;  // stick Y is inverted vs screen Y
                        _cursorPosition.X = MathHelper.Clamp(_cursorPosition.X, 0, clampW);
                        _cursorPosition.Y = MathHelper.Clamp(_cursorPosition.Y, 0, clampH);
                    }
                }

                // 4. GamePad buttons (A or X) ──────────────────────────────────
                if (CurrentGamePad.IsConnected)
                {
                    bool padDown    = CurrentGamePad.IsButtonDown(Buttons.A) ||
                                      CurrentGamePad.IsButtonDown(Buttons.X);
                    bool padWasDown = LastGamePad.IsButtonDown(Buttons.A) ||
                                      LastGamePad.IsButtonDown(Buttons.X);

                    if (padDown && !padWasDown)
                    {
                        PressDownPosition = _cursorPosition;
                        anyNewPress = true;
                    }
                    else if (!padDown && padWasDown)
                    {
                        PressUpPosition = _cursorPosition;
                        anyNewRelease = true;
                    }
                    else if (padDown)
                    {
                        anyHeld = true;
                    }
                }
            }

            // ── Publish intent flags ──────────────────────────────────────────
            IsNewPress   = anyNewPress;
            IsNewRelease = anyNewRelease;
            IsPressed    = anyNewPress || anyHeld;
            IsDragging   = anyHeld;   // held but not the first press frame

            // ── ESC / Back ────────────────────────────────────────────────────
            IsEscapeTriggered =
                (CurrentKeyboard.IsKeyDown(Keys.Escape) && LastKeyboard.IsKeyUp(Keys.Escape)) ||
                (CurrentGamePad.IsButtonDown(Buttons.Back) && LastGamePad.IsButtonUp(Buttons.Back));

            // ── Space / Enter confirm (centre-screen click) ───────────────────
            bool confirmKey    = CurrentKeyboard.IsKeyDown(Keys.Space) ||
                                 CurrentKeyboard.IsKeyDown(Keys.Enter);
            bool confirmWasKey = LastKeyboard.IsKeyDown(Keys.Space) ||
                                 LastKeyboard.IsKeyDown(Keys.Enter);

            IsConfirmTriggered = confirmKey  && !confirmWasKey;
            IsConfirmReleased  = !confirmKey && confirmWasKey;
        }
    }
}
