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
using System;
#endregion

namespace Gorillas
{
	/// <summary>
	/// Helper for reading input from keyboard, gamepad, and touch input. This class 
	/// tracks both the current and previous state of the input devices, and implements 
	/// query methods for high level input actions such as "move up through the menu"
	/// or "pause the game".
	/// </summary>
	public class InputState
	{
	#region Fields

		public const int MaxInputs = 4;
		public readonly KeyboardState[] CurrentKeyboardStates;
		public readonly GamePadState[] CurrentGamePadStates;
		public readonly KeyboardState[] LastKeyboardStates;
		public readonly GamePadState[] LastGamePadStates;
		public readonly bool[] GamePadWasConnected;
		public TouchCollection TouchState;
		public MouseState CurrentMouseState;	
		public MouseState LastMouseState;
		public readonly List<GestureSample> Gestures = new List<GestureSample> ();

		Matrix inputTransformation = Matrix.Identity;

		/// <summary>
		/// Pointer (touch or mouse) position in virtual 640x350 game coordinates. Touch wins over
		/// the mouse while a finger is down.
		/// </summary>
		public Vector2 PointerPosition { get; private set; }

		/// <summary>True while a finger or the left mouse button is down.</summary>
		public bool PointerDown { get; private set; }

		/// <summary>True only on the update where the pointer went down.</summary>
		public bool PointerPressed { get; private set; }

		/// <summary>True only on the update where the pointer was released.</summary>
		public bool PointerReleased { get; private set; }

		internal void UpdateInputTransformation (Matrix inverseTransformation)
		{
			inputTransformation = inverseTransformation;
		}

		/// <summary>Maps a back-buffer pixel position to virtual game coordinates.</summary>
		public Vector2 ToGameCoordinates (Vector2 screenPosition)
		{
			return Vector2.Transform (screenPosition, inputTransformation);
		}

	#endregion

	#region Initialization


		/// <summary>
		/// Constructs a new input state.
		/// </summary>
		public InputState ()
		{
			CurrentKeyboardStates = new KeyboardState[MaxInputs];
			CurrentGamePadStates = new GamePadState[MaxInputs];

			LastKeyboardStates = new KeyboardState[MaxInputs];
			LastGamePadStates = new GamePadState[MaxInputs];
			
			GamePadWasConnected = new bool[MaxInputs];
		}


	#endregion

	#region Public Methods


		/// <summary>
		/// Reads the latest state of the keyboard and gamepad.
		/// </summary>
		public void Update ()
		{
			for (int i = 0; i < MaxInputs; i++) {
				LastKeyboardStates [i] = CurrentKeyboardStates [i];
				LastGamePadStates [i] = CurrentGamePadStates [i];

				CurrentKeyboardStates [i] = Keyboard.GetState ((PlayerIndex)i);
				CurrentGamePadStates [i] = GamePad.GetState ((PlayerIndex)i);

				// Keep track of whether a gamepad has ever been
				// connected, so we can detect if it is unplugged.
				if (CurrentGamePadStates [i].IsConnected) {
					GamePadWasConnected [i] = true;
				}
			}

			TouchState = TouchPanel.GetState ();

			LastMouseState = CurrentMouseState;
			CurrentMouseState = Mouse.GetState ();
			
			UpdatePointer ();

			Gestures.Clear ();
			
			while (TouchPanel.IsGestureAvailable) {
				Gestures.Add (TouchPanel.ReadGesture ());
			}
		}


		void UpdatePointer ()
		{
			bool wasDown = PointerDown;
			bool down = false;
			Vector2 screenPosition = PointerPosition;
			bool havePosition = false;

			if (TouchState.Count > 0) {
				TouchLocation touch = TouchState [0];
				if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved) {
					down = true;
					screenPosition = touch.Position;
					havePosition = true;
				}
			}

			if (!down && CurrentMouseState.LeftButton == ButtonState.Pressed) {
				down = true;
				screenPosition = new Vector2 (CurrentMouseState.X, CurrentMouseState.Y);
				havePosition = true;
			}

			if (!havePosition) {
				// Hover: keep following the mouse so menu entries can highlight.
				screenPosition = new Vector2 (CurrentMouseState.X, CurrentMouseState.Y);
			}

			PointerDown = down;
			PointerPressed = down && !wasDown;
			PointerReleased = !down && wasDown;
			if (havePosition || !wasDown)
				PointerPosition = ToGameCoordinates (screenPosition);
		}

		/// <summary>
		/// Helper for checking if a key was newly pressed during this update. The
		/// controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When a keypress
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsNewKeyPress (Keys key, PlayerIndex? controllingPlayer, 
					out PlayerIndex playerIndex)
		{
			if (controllingPlayer.HasValue) {
				// Read input from the specified player.
				playerIndex = controllingPlayer.Value;

				int i = (int)playerIndex;

				return (CurrentKeyboardStates [i].IsKeyDown (key) && LastKeyboardStates [i].IsKeyUp (key));
			} else {
				// Accept input from any player.
				return (IsNewKeyPress (key, PlayerIndex.One, out playerIndex) || 
					IsNewKeyPress (key, PlayerIndex.Two, out playerIndex) || 
					IsNewKeyPress (key, PlayerIndex.Three, out playerIndex) || 
					IsNewKeyPress (key, PlayerIndex.Four, out playerIndex));
			}
		}

		/// <summary>
		/// Helper for checking if a button was newly pressed during this update.
		/// The controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When a button press
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsNewButtonPress (Buttons button, PlayerIndex? controllingPlayer,
						out PlayerIndex playerIndex)
		{
			if (controllingPlayer.HasValue) {
				// Read input from the specified player.
				playerIndex = controllingPlayer.Value;

				int i = (int)playerIndex;

				return (CurrentGamePadStates [i].IsButtonDown (button) && 
					LastGamePadStates [i].IsButtonUp (button));
			} else {
				// Accept input from any player.
				return (IsNewButtonPress (button, PlayerIndex.One, out playerIndex) || 
					IsNewButtonPress (button, PlayerIndex.Two, out playerIndex) || 
					IsNewButtonPress (button, PlayerIndex.Three, out playerIndex) || 
					IsNewButtonPress (button, PlayerIndex.Four, out playerIndex));
			}
		}


		/// <summary>
		/// Checks for a "menu select" input action.
		/// The controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When the action
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsMenuSelect (PlayerIndex? controllingPlayer,
				out PlayerIndex playerIndex)
		{
			return IsNewKeyPress (Keys.Space, controllingPlayer, out playerIndex) || 
			IsNewKeyPress (Keys.Enter, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.A, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.Start, controllingPlayer, out playerIndex);
		}
		
		/// <summary>
		/// Checks for a "menu select" input action.
		/// The controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When the action
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsMenuDecrementSelect (PlayerIndex? controllingPlayer,
				out PlayerIndex playerIndex)
		{
			return IsNewKeyPress (Keys.Left, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.DPadLeft, controllingPlayer, out playerIndex);
		}
		
		/// <summary>
		/// Checks for a "menu select" input action.
		/// The controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When the action
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsMenuIncrementSelect (PlayerIndex? controllingPlayer,
				out PlayerIndex playerIndex)
		{
			return IsNewKeyPress (Keys.Right, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.DPadRight, controllingPlayer, out playerIndex);
		}
		
		/// <summary>
		/// Checks for a "menu cancel" input action.
		/// The controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When the action
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsMenuCancel (PlayerIndex? controllingPlayer,
				out PlayerIndex playerIndex)
		{
			return IsNewKeyPress (Keys.Escape, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.B, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.Back, controllingPlayer, out playerIndex);
		}


		/// <summary>
		/// Checks for a "menu up" input action.
		/// The controllingPlayer parameter specifies which player to read
		/// input for. If this is null, it will accept input from any player.
		/// </summary>
		public bool IsMenuUp (PlayerIndex? controllingPlayer)
		{
			PlayerIndex playerIndex;

			return IsNewKeyPress (Keys.Up, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.DPadUp, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.LeftThumbstickUp, controllingPlayer, out playerIndex);
		}


		/// <summary>
		/// Checks for a "menu down" input action.
		/// The controllingPlayer parameter specifies which player to read
		/// input for. If this is null, it will accept input from any player.
		/// </summary>
		public bool IsMenuDown (PlayerIndex? controllingPlayer)
		{
			PlayerIndex playerIndex;

			return IsNewKeyPress (Keys.Down, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.DPadDown, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.LeftThumbstickDown, controllingPlayer, out playerIndex);
		}


		/// <summary>
		/// Checks for a "pause the game" input action.
		/// The controllingPlayer parameter specifies which player to read
		/// input for. If this is null, it will accept input from any player.
		/// </summary>
		public bool IsPauseGame (PlayerIndex? controllingPlayer)
		{
			PlayerIndex playerIndex;

			return IsNewKeyPress (Keys.Escape, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.Back, controllingPlayer, out playerIndex) || 
			IsNewButtonPress (Buttons.Start, controllingPlayer, out playerIndex);
		}


	#endregion
	}
}
