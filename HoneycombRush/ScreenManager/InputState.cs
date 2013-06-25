#region File Description
//-----------------------------------------------------------------------------
// InputState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System.Collections.Generic;
#endregion

namespace HoneycombRush
{
    /// <summary>
    /// Helper for reading input from keyboard, gamepad, and touch input. This class 
    /// tracks both the current and previous state of the input devices, and implements 
    /// query methods for high level input actions such as "move up through the menu"
    /// or "pause the game".
    /// </summary>
    public class InputState
    {
		public enum MouseButton
		{
			Left,
			Middle,
			Right
		}
        #region Fields

        public const int MaxInputs = 4;

        public readonly KeyboardState[] CurrentKeyboardStates;
		public readonly MouseState[] CurrentMouseStates;
        public readonly GamePadState[] CurrentGamePadStates;

        public readonly KeyboardState[] LastKeyboardStates;
		public readonly MouseState[] LastMouseStates;
        public readonly GamePadState[] LastGamePadStates;

        public readonly bool[] GamePadWasConnected;

        public TouchCollection TouchState;

        public readonly List<GestureSample> Gestures = new List<GestureSample>();

        #endregion

        #region Initialization


        /// <summary>
        /// Constructs a new input state.
        /// </summary>
        public InputState()
        {
            CurrentKeyboardStates = new KeyboardState[MaxInputs];
			CurrentMouseStates = new MouseState[MaxInputs];
            CurrentGamePadStates = new GamePadState[MaxInputs];

            LastKeyboardStates = new KeyboardState[MaxInputs];
			LastMouseStates = new MouseState[MaxInputs];
            LastGamePadStates = new GamePadState[MaxInputs];

            GamePadWasConnected = new bool[MaxInputs];
        }


        #endregion

        #region Public Methods


        /// <summary>
        /// Reads the latest state of the keyboard and gamepad.
        /// </summary>
        public void Update()
        {
            for (int i = 0; i < MaxInputs; i++)
            {
                LastKeyboardStates[i] = CurrentKeyboardStates[i];
				LastMouseStates[i] = CurrentMouseStates[i];
                LastGamePadStates[i] = CurrentGamePadStates[i];

                CurrentKeyboardStates[i] = Keyboard.GetState((PlayerIndex)i);
				CurrentMouseStates[i] = Mouse.GetState();
                CurrentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);

                // Keep track of whether a gamepad has ever been
                // connected, so we can detect if it is unplugged.
                if (CurrentGamePadStates[i].IsConnected)
                {
                    GamePadWasConnected[i] = true;
                }
            }

            TouchState = TouchPanel.GetState();

            Gestures.Clear();
            while (TouchPanel.IsGestureAvailable)
            {
                Gestures.Add(TouchPanel.ReadGesture());
            }
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
        /// Helper for checking if a key is currently pressed during the update. The
        /// controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When a pressed key
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsKeyDown(Keys key, PlayerIndex? controllingPlayer,
                                            out PlayerIndex playerIndex)
        {
            if (controllingPlayer.HasValue)
            {
                // Read input from the specified player.
                playerIndex = controllingPlayer.Value;

                int i = (int)playerIndex;

                return (CurrentKeyboardStates[i].IsKeyDown(key));
            }
            else
            {
                // Accept input from any player.
                return (IsKeyDown(key, PlayerIndex.One, out playerIndex) ||
                        IsKeyDown(key, PlayerIndex.Two, out playerIndex) ||
                        IsKeyDown(key, PlayerIndex.Three, out playerIndex) ||
                        IsKeyDown(key, PlayerIndex.Four, out playerIndex));
            }
        }

		/// <summary>
		/// Helper for checking if a mouse button was newly pressed during this update. The
		/// controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When a click
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsNewMouseClick(MouseButton mouseButton , PlayerIndex? controllingPlayer,
		                          out PlayerIndex playerIndex)
		{
			if (controllingPlayer.HasValue)
			{
				// Read input from the specified player.
				playerIndex = controllingPlayer.Value;

				int i = (int)playerIndex;
				switch( mouseButton){
					case MouseButton.Left:
						return (CurrentMouseStates[i].LeftButton == ButtonState.Pressed && 
					            LastMouseStates[i].LeftButton == ButtonState.Released);
					case MouseButton.Right:
						return (CurrentMouseStates[i].RightButton == ButtonState.Pressed && 
						        LastMouseStates[i].RightButton == ButtonState.Released);
					case MouseButton.Middle:
						return (CurrentMouseStates[i].MiddleButton == ButtonState.Pressed && 
						        LastMouseStates[i].MiddleButton == ButtonState.Released);
					default:
						return IsNewMouseClick(MouseButton.Left, controllingPlayer, out playerIndex) ||
							   IsNewMouseClick(MouseButton.Middle, controllingPlayer, out playerIndex) ||
							   IsNewMouseClick(MouseButton.Right, controllingPlayer, out playerIndex);
				}

			}
			else
			{
				// Accept input from any player.
				return (IsNewMouseClick(mouseButton, PlayerIndex.One, out playerIndex) ||
				        IsNewMouseClick(mouseButton, PlayerIndex.Two, out playerIndex) ||
				        IsNewMouseClick(mouseButton, PlayerIndex.Three, out playerIndex) ||
				        IsNewMouseClick(mouseButton, PlayerIndex.Four, out playerIndex));
			}

		}

		/// <summary>
		/// Helper for checking if a mouse button is currently pressed during the update. The
		/// controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When a pressed button
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsMouseDown(MouseButton mouseButton , PlayerIndex? controllingPlayer,
		                            out PlayerIndex playerIndex)
		{
			if (controllingPlayer.HasValue)
			{
				// Read input from the specified player.
				playerIndex = controllingPlayer.Value;

				int i = (int)playerIndex;
				switch( mouseButton  ) {
					case MouseButton.Left:
						return CurrentMouseStates[i].LeftButton == ButtonState.Pressed;
					case MouseButton.Right:
						return CurrentMouseStates[i].RightButton == ButtonState.Pressed;
					case MouseButton.Middle:
						return CurrentMouseStates[i].MiddleButton == ButtonState.Pressed;
					default:
						return IsMouseDown(MouseButton.Left, controllingPlayer, out playerIndex) ||
							   IsMouseDown(MouseButton.Middle, controllingPlayer, out playerIndex) ||
							   IsMouseDown(MouseButton.Right, controllingPlayer, out playerIndex);
				}

			}
			else
			{
				// Accept input from any player.
				return (IsMouseDown(mouseButton, PlayerIndex.One, out playerIndex) ||
				        IsMouseDown(mouseButton, PlayerIndex.Two, out playerIndex) ||
				        IsMouseDown(mouseButton, PlayerIndex.Three, out playerIndex) ||
				        IsMouseDown(mouseButton, PlayerIndex.Four, out playerIndex));
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
			   	   IsNewMouseClick(MouseButton.Left, controllingPlayer, out playerIndex) ||
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


        #endregion
    }
}
