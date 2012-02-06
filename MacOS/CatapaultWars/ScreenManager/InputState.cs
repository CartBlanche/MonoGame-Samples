#region File Description
//-----------------------------------------------------------------------------
// InputState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
#endregion

namespace GameStateManagement
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

			UpdateMouseStates();

			Gestures.Clear ();
			while (TouchPanel.IsGestureAvailable) {
				Gestures.Add (TouchPanel.ReadGesture ());
			}
		}

		bool dragging = false;
		bool dragComplete = false;
		bool leftMouseDown = false;
		int dragThreshold = 3;
		MouseGestureType mouseGestureType;
		Vector2 currentMousePosition = Vector2.Zero;
		Vector2 prevMousePosition = Vector2.Zero;
		Vector2 dragMouseStart = Vector2.Zero;
		Vector2 dragMouseEnd = Vector2.Zero;

		public MouseGestureType MouseGesture
		{
			get {
				return mouseGestureType;
			}
		}

		public Vector2 CurrentMousePosition
		{
			get {
				return currentMousePosition;
			}
		}

		public Vector2 PrevMousePosition
		{
			get {
				return prevMousePosition;
			}
		}

		public Vector2 MouseDelta
		{
			get {
				return prevMousePosition - currentMousePosition;
			}
		}

		public Vector2 MouseDragDelta
		{
			get {
				return dragMouseStart - dragMouseEnd;
			}
		}

		public Vector2 MouseDragStartPosition
		{
			get {
				return dragMouseStart;
			}
		}

		public Vector2 MouseDragEndPosition
		{
			get {
				return dragMouseEnd;
			}
		}

		void UpdateMouseStates ()
		{
			currentMousePosition.X = CurrentMouseState.X;
			currentMousePosition.Y = CurrentMouseState.Y;

			prevMousePosition.X = LastMouseState.X;
			prevMousePosition.Y = LastMouseState.Y;

			if (mouseGestureType.HasFlag(MouseGestureType.LeftClick))
				mouseGestureType = mouseGestureType ^ MouseGestureType.LeftClick;

			if (mouseGestureType.HasFlag(MouseGestureType.Move))
				mouseGestureType = mouseGestureType ^ MouseGestureType.Move;

			if (MouseDelta.Length() != 0)
				mouseGestureType = mouseGestureType | MouseGestureType.Move;

			// If we were dragging and the left mouse button was released
			// then we are no longer dragging and need to throw the banana.
			if (CurrentMouseState.LeftButton == ButtonState.Released &&
					dragging) {

				leftMouseDown = false;
				dragging = false;
				dragComplete = true;
				dragMouseEnd = currentMousePosition;
				mouseGestureType |= MouseGestureType.DragComplete;
				mouseGestureType = mouseGestureType ^ MouseGestureType.FreeDrag;
				//Console.WriteLine ("Dragging: " + mouseGestureType);

			}

			// Let's set the left mouse down and the mouse origin
			if (!leftMouseDown && CurrentMouseState.LeftButton == ButtonState.Pressed &&
					!CurrentMouseState.Equals (LastMouseState)) {
				//Console.WriteLine ("left down");
				leftMouseDown = true;
				dragComplete = false;
				dragMouseStart = currentMousePosition;
			}

			if (leftMouseDown && CurrentMouseState.LeftButton == ButtonState.Released &&
					!CurrentMouseState.Equals (LastMouseState)) {
				leftMouseDown = false;
				mouseGestureType |= MouseGestureType.LeftClick;
			}

			// Here we test the distance and if over the threshold then we set the dragging to true
			//  Current threshold is 5 pixels.
			if (leftMouseDown && !dragging) {

				Vector2 delta = dragMouseStart - currentMousePosition;

				if (delta.Length() > dragThreshold) {
					dragging = true;
					dragMouseStart = currentMousePosition;
					mouseGestureType = mouseGestureType | MouseGestureType.FreeDrag;
					//Console.WriteLine ("Dragging: " + mouseGestureType);
				}
			}

			//Console.WriteLine(mouseGestureType);
		}

		/// <summary>
		/// Helper for checking if a key was newly pressed during this update. The
		/// controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When a keypress
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsNewKeyPress (Keys key, PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
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
		public bool IsNewButtonPress (Buttons button, PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
		{
			if (controllingPlayer.HasValue) {
				// Read input from the specified player.
				playerIndex = controllingPlayer.Value;

				int i = (int)playerIndex;

				return (CurrentGamePadStates [i].IsButtonDown (button) && LastGamePadStates [i].IsButtonUp (button));
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
		public bool IsMenuSelect (PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
		{
			return IsNewKeyPress (Keys.Space, controllingPlayer, out playerIndex) ||
						IsNewKeyPress (Keys.Enter, controllingPlayer, out playerIndex) ||
						IsNewButtonPress (Buttons.A, controllingPlayer, out playerIndex) ||
						IsNewButtonPress (Buttons.Start, controllingPlayer, out playerIndex);
		}


		/// <summary>
		/// Checks for a "menu cancel" input action.
		/// The controllingPlayer parameter specifies which player to read input for.
		/// If this is null, it will accept input from any player. When the action
		/// is detected, the output playerIndex reports which player pressed it.
		/// </summary>
		public bool IsMenuCancel (PlayerIndex? controllingPlayer, out PlayerIndex playerIndex)
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
