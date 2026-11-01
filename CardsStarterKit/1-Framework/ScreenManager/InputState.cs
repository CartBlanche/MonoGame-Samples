//-----------------------------------------------------------------------------
// InputState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;

namespace CardsFramework
{
    /// <summary>
    /// Helper for reading input from keyboard, gamepad, and touch input. This class 
    /// tracks both the current and previous state of the input devices, and implements 
    /// query methods for high level input actions such as "move up through the menu"
    /// or "pause the game".
    /// </summary>
    public class InputState
    {

        public const int MaxInputs = 4; // Maximum number of supported input devices (e.g., players)

        // Current Inputstates - Tracks the latest state of all input devices
        public readonly GamePadState[] CurrentGamePadStates;
        public readonly KeyboardState[] CurrentKeyboardStates;
        public MouseState CurrentMouseState;
        private int touchCount; // Number of active touch inputs
        public TouchCollection CurrentTouchState;

        // Last Inputstates - Stores the previous frame's input states for detecting changes
        public readonly GamePadState[] LastGamePadStates;
        public readonly KeyboardState[] LastKeyboardStates;
        public MouseState LastMouseState;
        public TouchCollection LastTouchState;

        public readonly List<GestureSample> Gestures = new List<GestureSample>(); // Stores touch gestures

        public readonly bool[] GamePadWasConnected;

        /// <summary>
        /// Cursor move speed in pixels per second
        /// </summary>
        private const float cursorMoveSpeed = 250.0f;

        private Vector2 currentCursorLocation;
        /// <summary>
        /// Current location of our Cursor
        /// </summary>
        public Vector2 CurrentCursorLocation => currentCursorLocation;

        private Vector2 lastCursorLocation;
        /// <summary>
        /// Current location of our Cursor
        /// </summary>
        public Vector2 LastCursorLocation => lastCursorLocation;

        private bool isMouseWheelScrolledDown;
        /// <summary>
        /// Has the user scrolled the mouse wheel down?
        /// </summary>
        public bool IsMouseWheelScrolledDown => isMouseWheelScrolledDown;

        private bool isMouseWheelScrolledUp;
        private Matrix inputTransformation; // Used to transform input coordinates between screen and game space
        private float baseBufferWidth;
        private float baseBufferHeight;

        /// <summary>
        /// Has the user scrolled the mouse wheel up?
        /// </summary>
        public bool IsMouseWheelScrolledUp => isMouseWheelScrolledUp;

        // ── Unified pointer and gestures ─────────────────────────────────
        // One pointer from the mouse and the primary touch, plus the gestures recognized from it
        // (click, double-click, drag, long-press). Additive: nothing above changes. Positions are in game
        // (base resolution) space, like CurrentCursorLocation, but unlike it the pointer follows a finger
        // while it moves, which a drag needs.

        private readonly PointerTracker pointerTracker = new PointerTracker();
        private readonly List<TouchSample> touchSamples = new List<TouchSample>();

        /// <summary>The recognizer behind <see cref="PointerGestureEvents"/>; its <see cref="PointerGestureRecognizer.Options"/> hold the thresholds.</summary>
        public readonly PointerGestureRecognizer PointerGestures = new PointerGestureRecognizer();

        /// <summary>Where the pointer is: the primary touch while a finger is down, otherwise the mouse (every frame, so hover works).</summary>
        public Vector2 PointerPosition => pointerTracker.Position;

        /// <summary>True while the primary touch or the left mouse button is held.</summary>
        public bool PointerDown => pointerTracker.Down;

        /// <summary>True only on the frame the pointer goes down.</summary>
        public bool PointerPressed => pointerTracker.Pressed;

        /// <summary>True only on the frame the pointer comes up.</summary>
        public bool PointerReleased => pointerTracker.Released;

        /// <summary>Where the pointer was when it last went down.</summary>
        public Vector2 PointerPressPosition => pointerTracker.PressPosition;

        /// <summary>Where the pointer was when it last came up.</summary>
        public Vector2 PointerReleasePosition => pointerTracker.ReleasePosition;

        /// <summary>The device behind the pointer this frame (mouse or touch).</summary>
        public PointerSource PointerSource => pointerTracker.Source;

        /// <summary>The click, double-click, drag and long-press events of this frame, in order.</summary>
        public IReadOnlyList<PointerGesture> PointerGestureEvents => PointerGestures.Events;

        /// <summary>
        /// Abandons a drag or press in progress (a dialog opened over it, the window lost focus). A drag reports a
        /// <see cref="PointerGestureKind.DragCancel"/> event, readable in <see cref="PointerGestureEvents"/> until the next update.
        /// </summary>
        public void CancelPointerGesture(GameTime gameTime) => PointerGestures.Cancel(gameTime.TotalGameTime, pointerTracker.Position);

        private TimeSpan lastPointerTime;

        /// <summary>The same, for code that is not handed a <see cref="GameTime"/> (an input handler): it uses the time of the last update.</summary>
        public void CancelPointerGesture() => PointerGestures.Cancel(lastPointerTime, pointerTracker.Position);

        /// <summary>
        /// Constructs a new input state.
        /// </summary>
        public InputState(float baseBufferWidth, float baseBufferHeight)
        {
            this.baseBufferWidth = baseBufferWidth;
            this.baseBufferHeight = baseBufferHeight;

            // Initialize arrays for multiple controller/keyboard states
            CurrentKeyboardStates = new KeyboardState[MaxInputs];
            CurrentGamePadStates = new GamePadState[MaxInputs];

            LastKeyboardStates = new KeyboardState[MaxInputs];
            LastGamePadStates = new GamePadState[MaxInputs];

            GamePadWasConnected = new bool[MaxInputs];

            // Configure platform-specific input options
            if (UIUtility.IsMobile)
            {
                TouchPanel.EnabledGestures = GestureType.Tap;
            }
            else if (UIUtility.IsDesktop)
            {
                // No desktop-specific initialization needed
            }
            else
            {
                // For now, we'll throw an exception if we don't know the platform
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Reads the latest state of the keyboard and gamepad.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Update keyboard and gamepad states for all players
            for (int i = 0; i < MaxInputs; i++)
            {
                LastKeyboardStates[i] = CurrentKeyboardStates[i];
                LastGamePadStates[i] = CurrentGamePadStates[i];

                CurrentKeyboardStates[i] = Keyboard.GetState();
                CurrentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);

                // Keep track of whether a gamepad has ever been
                // connected, so we can detect if it is unplugged.
                if (CurrentGamePadStates[i].IsConnected)
                {
                    GamePadWasConnected[i] = true;
                }
            }

            // Update mouse state
            LastMouseState = CurrentMouseState;
            CurrentMouseState = Mouse.GetState();

            // Update mouse cursor location every frame (like touch does)
            if (UIUtility.IsDesktop)
            {
                // Always track the current mouse cursor position
                // Note: lastCursorLocation is NOT updated here - it's only updated on actual clicks/taps
                currentCursorLocation = TransformCursorLocation(new Vector2(CurrentMouseState.X, CurrentMouseState.Y));
            }

            // Update touch state
            touchCount = 0;
            LastTouchState = CurrentTouchState;
            CurrentTouchState = TouchPanel.GetState();

            // Process all available gestures
            Gestures.Clear();
            while (TouchPanel.IsGestureAvailable)
            {
                GestureSample gesture = TouchPanel.ReadGesture();
                Gestures.Add(gesture);
                
                // Update cursor location from gesture position (for tap and other gestures)
                if (gesture.GestureType == GestureType.Tap)
                {
                    lastCursorLocation = currentCursorLocation;
                    // Transform gesture position to game coordinates
                    currentCursorLocation = TransformCursorLocation(gesture.Position);
                }
            }

            // Process touch inputs
            foreach (TouchLocation location in CurrentTouchState)
            {
                switch (location.State)
                {
                    case TouchLocationState.Pressed:
                        touchCount++;
                        lastCursorLocation = currentCursorLocation;
                        // Transform touch position to game coordinates
                        currentCursorLocation = TransformCursorLocation(location.Position);
                        break;
                    case TouchLocationState.Moved:
                        break;
                    case TouchLocationState.Released:
                        break;
                }
            }

            // Handle mouse clicks as touch equivalents
            if (IsLeftMouseButtonClicked())
            {
                lastCursorLocation = currentCursorLocation;
                touchCount = 1;
            }

            if (IsMiddleMouseButtonClicked())
            {
                lastCursorLocation = currentCursorLocation;
                touchCount = 2; // Treat middle mouse click as double touch
            }

            if (IsRightMouseButtonClicked())
            {
                lastCursorLocation = currentCursorLocation;
                touchCount = 3; // Treat right mouse click as triple touch
            }

            // Reset mouse wheel flags
            isMouseWheelScrolledUp = false;
            isMouseWheelScrolledDown = false;

            // Detect mouse wheel scrolling
            if (CurrentMouseState.ScrollWheelValue != LastMouseState.ScrollWheelValue)
            {
                int scrollWheelDelta = CurrentMouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue;

                // Handle the scroll wheel event based on the delta
                if (scrollWheelDelta > 0)
                {
                    // Mouse wheel scrolled up
                    isMouseWheelScrolledUp = true;
                }
                else if (scrollWheelDelta < 0)
                {
                    // Mouse wheel scrolled down
                    isMouseWheelScrolledDown = true;
                }
            }

            // Update the cursor location using gamepad and keyboard
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Move cursor with 1st gamepad thumbstick
            if (CurrentGamePadStates[0].IsConnected)
            {
                lastCursorLocation = currentCursorLocation;

                currentCursorLocation.X += CurrentGamePadStates[0].ThumbSticks.Left.X * elapsedTime * cursorMoveSpeed;
                currentCursorLocation.Y -= CurrentGamePadStates[0].ThumbSticks.Left.Y * elapsedTime * cursorMoveSpeed;
            }

            // Keep cursor within bounds
            currentCursorLocation.X = MathHelper.Clamp(currentCursorLocation.X, 0f, baseBufferWidth);
            currentCursorLocation.Y = MathHelper.Clamp(currentCursorLocation.Y, 0f, baseBufferHeight);

            UpdatePointer(gameTime);
        }

        /// <summary>
        /// Builds the unified pointer from the mouse and the touch panel (both moved into game space), then runs the
        /// gesture recognizer on it. Reads only the device states already captured above.
        /// </summary>
        private void UpdatePointer(GameTime gameTime)
        {
            touchSamples.Clear();
            foreach (TouchLocation location in CurrentTouchState)
                touchSamples.Add(new TouchSample(location.Id, TransformCursorLocation(location.Position), location.State));

            Vector2? mousePosition = UIUtility.IsDesktop
                ? TransformCursorLocation(new Vector2(CurrentMouseState.X, CurrentMouseState.Y))
                : (Vector2?)null;

            lastPointerTime = gameTime.TotalGameTime;
            pointerTracker.Update(mousePosition, CurrentMouseState.LeftButton == ButtonState.Pressed, touchSamples);
            PointerGestures.Update(gameTime.TotalGameTime, pointerTracker.Position, pointerTracker.Down,
                                   pointerTracker.Pressed, pointerTracker.Released, pointerTracker.Source);
        }

        /// <summary>
        /// Checks if left mouse button was clicked (pressed and then released)
        /// </summary>
        /// <returns>True if left mouse button was clicked, false otherwise.</returns>
        public bool IsLeftMouseButtonClicked()
        {
            return CurrentMouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed;
        }

        /// <summary>
        /// Checks if middle mouse button was clicked (pressed and then released)
        /// </summary>
        /// <returns>True if middle mouse button was clicked, false otherwise.</returns>
        internal bool IsMiddleMouseButtonClicked()
        {
            return CurrentMouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed;
        }

        /// <summary>
        /// Checks if right mouse button was clicked (pressed and then released)
        /// </summary>
        /// <returns>True if right mouse button was clicked, false otherwise.</returns>
        internal bool IsRightMouseButtonClicked()
        {
            return CurrentMouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed;
        }

        /// <summary>
        /// Helper for checking if a key was newly pressed during this update. The
        /// controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When a keypress
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsNewKeyPress(Keys key, PlayerIndex? controllingPlayer,
                                            out PlayerIndex playerIndex)
        {
            if (controllingPlayer.HasValue)
            {
                // Read input from the specified player.
                playerIndex = controllingPlayer.Value;

                int i = (int)playerIndex;

                return (CurrentKeyboardStates[i].IsKeyDown(key) &&
                        LastKeyboardStates[i].IsKeyUp(key));
            }
            else
            {
                // Accept input from any player.
                return (IsNewKeyPress(key, PlayerIndex.One, out playerIndex) ||
                        IsNewKeyPress(key, PlayerIndex.Two, out playerIndex) ||
                        IsNewKeyPress(key, PlayerIndex.Three, out playerIndex) ||
                        IsNewKeyPress(key, PlayerIndex.Four, out playerIndex));
            }
        }

        /// <summary>
        /// Helper for checking if a button was newly pressed during this update.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When a button press
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsNewButtonPress(Buttons button, PlayerIndex? controllingPlayer,
                                                     out PlayerIndex playerIndex)
        {
            if (controllingPlayer.HasValue)
            {
                // Read input from the specified player.
                playerIndex = controllingPlayer.Value;

                int i = (int)playerIndex;

                return (CurrentGamePadStates[i].IsButtonDown(button) &&
                        LastGamePadStates[i].IsButtonUp(button));
            }
            else
            {
                // Accept input from any player.
                return (IsNewButtonPress(button, PlayerIndex.One, out playerIndex) ||
                        IsNewButtonPress(button, PlayerIndex.Two, out playerIndex) ||
                        IsNewButtonPress(button, PlayerIndex.Three, out playerIndex) ||
                        IsNewButtonPress(button, PlayerIndex.Four, out playerIndex));
            }
        }

        /// <summary>
        /// Checks for a "menu select" input action.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When the action
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsMenuSelect(PlayerIndex? controllingPlayer,
                                 out PlayerIndex playerIndex)
        {
            return IsNewKeyPress(Keys.Space, controllingPlayer, out playerIndex) ||
                   IsNewKeyPress(Keys.Enter, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.A, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex);
        }

        /// <summary>
        /// Checks for a "menu cancel" input action.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When the action
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsMenuCancel(PlayerIndex? controllingPlayer,
                                 out PlayerIndex playerIndex)
        {
            return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.B, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.Back, controllingPlayer, out playerIndex);
        }

        /// <summary>
        /// Checks for a "menu up" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsMenuUp(PlayerIndex? controllingPlayer)
        {
            PlayerIndex playerIndex;

            return IsNewKeyPress(Keys.Up, controllingPlayer, out playerIndex) ||
                   IsNewKeyPress(Keys.Left, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.DPadLeft, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.LeftThumbstickLeft, controllingPlayer, out playerIndex);
        }

        /// <summary>
        /// Checks for a "menu down" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsMenuDown(PlayerIndex? controllingPlayer)
        {
            PlayerIndex playerIndex;

            return IsNewKeyPress(Keys.Down, controllingPlayer, out playerIndex) ||
                   IsNewKeyPress(Keys.Right, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.DPadRight, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.LeftThumbstickRight, controllingPlayer, out playerIndex);
        }

        /// <summary>
        /// Checks for a "pause the game" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsPauseGame(PlayerIndex? controllingPlayer)
        {
            PlayerIndex playerIndex;

            return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.Back, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex);
        }

        /// <summary>
        /// Updates the matrix used to transform input coordinates.
        /// </summary>
        /// <param name="inputTransformation">The transformation matrix to apply.</param>
        public void UpdateInputTransformation(Matrix inputTransformation)
        {
            this.inputTransformation = inputTransformation;
        }

        /// <summary>
        /// Transforms touch/mouse positions from screen space to game space.
        /// </summary>
        /// <param name="mousePosition">The screen-space position to transform.</param>
        /// <returns>The transformed position in game space.</returns>
        public Vector2 TransformCursorLocation(Vector2 mousePosition)
        {
            // Transform back to cursor location
            return Vector2.Transform(mousePosition, inputTransformation);
        }
    }
}