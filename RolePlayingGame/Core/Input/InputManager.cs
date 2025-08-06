//-----------------------------------------------------------------------------
// InputManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace RolePlaying
{
    /// <summary>
    /// This class handles all keyboard and gamepad actions in the game.
    /// </summary>
    public static partial class InputManager
    {
        /// <summary>
        /// Readable names of each action.
        /// </summary>
        private static readonly string[] actionNames =
            {
                "Main Menu",
                "Ok",
                "Back",
                "Character Management",
                "Exit Game",
                "Take / View",
                "Drop / Unequip",
                "Move Character - Up",
                "Move Character - Down",
                "Move Character - Left",
                "Move Character - Right",
                "Move Cursor - Up",
                "Move Cursor - Down",
                "Decrease Amount",
                "Increase Amount",
                "Page Screen Left",
                "Page Screen Right",
                "Select Target -Up",
                "Select Target - Down",
                "Select Active Character - Left",
                "Select Active Character - Right",
            };

        /// <summary>
        /// Returns the readable name of the given action.
        /// </summary>
        public static string GetActionName(InputAction action)
        {
            int index = (int)action;

            if ((index < 0) || (index > actionNames.Length))
            {
                throw new ArgumentException("action");
            }

            return actionNames[index];
        }

        /// <summary>
        /// The value of an analog control that reads as a "pressed button".
        /// </summary>
        const float analogLimit = 0.5f;

        /// <summary>
        /// The state of the keyboard as of the last update.
        /// </summary>
        private static KeyboardState currentKeyboardState;

        /// <summary>
        /// The state of the keyboard as of the last update.
        /// </summary>
        public static KeyboardState CurrentKeyboardState
        {
            get { return currentKeyboardState; }
        }

        /// <summary>
        /// The state of the keyboard as of the previous update.
        /// </summary>
        private static KeyboardState previousKeyboardState;

        /// <summary>
        /// Check if a key is pressed.
        /// </summary>
        public static bool IsKeyPressed(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key);
        }

        /// <summary>
        /// Check if a key was just pressed in the most recent update.
        /// </summary>
        public static bool IsKeyTriggered(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key)
                && !previousKeyboardState.IsKeyDown(key);
        }

        /// <summary>
        /// The state of the gamepad as of the last update.
        /// </summary>
        private static GamePadState currentGamePadState;

        /// <summary>
        /// The state of the gamepad as of the last update.
        /// </summary>
        public static GamePadState CurrentGamePadState
        {
            get { return currentGamePadState; }
        }

        /// <summary>
        /// The state of the gamepad as of the previous update.
        /// </summary>
        private static GamePadState previousGamePadState;

        /// <summary>
        /// Check if the gamepad's Start button is pressed.
        /// </summary>
        public static bool IsGamePadStartPressed()
        {
            return (currentGamePadState.Buttons.Start == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if the gamepad's Back button is pressed.
        /// </summary>
        public static bool IsGamePadBackPressed()
        {
            return (currentGamePadState.Buttons.Back == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if the gamepad's A button is pressed.
        /// </summary>
        public static bool IsGamePadAPressed()
        {
            return (currentGamePadState.Buttons.A == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if the gamepad's B button is pressed.
        /// </summary>
        public static bool IsGamePadBPressed()
        {
            return (currentGamePadState.Buttons.B == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if the gamepad's X button is pressed.
        /// </summary>
        public static bool IsGamePadXPressed()
        {
            return (currentGamePadState.Buttons.X == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if the gamepad's Y button is pressed.
        /// </summary>
        public static bool IsGamePadYPressed()
        {
            return (currentGamePadState.Buttons.Y == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if the gamepad's LeftShoulder button is pressed.
        /// </summary>
        public static bool IsGamePadLeftShoulderPressed()
        {
            return (currentGamePadState.Buttons.LeftShoulder == ButtonState.Pressed);
        }

        /// <summary>
        /// <summary>
        /// Check if the gamepad's RightShoulder button is pressed.
        /// </summary>
        public static bool IsGamePadRightShoulderPressed()
        {
            return (currentGamePadState.Buttons.RightShoulder == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if Up on the gamepad's directional pad is pressed.
        /// </summary>
        public static bool IsGamePadDPadUpPressed()
        {
            return (currentGamePadState.DPad.Up == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if Down on the gamepad's directional pad is pressed.
        /// </summary>
        public static bool IsGamePadDPadDownPressed()
        {
            return (currentGamePadState.DPad.Down == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if Left on the gamepad's directional pad is pressed.
        /// </summary>
        public static bool IsGamePadDPadLeftPressed()
        {
            return (currentGamePadState.DPad.Left == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if Right on the gamepad's directional pad is pressed.
        /// </summary>
        public static bool IsGamePadDPadRightPressed()
        {
            return (currentGamePadState.DPad.Right == ButtonState.Pressed);
        }

        /// <summary>
        /// Check if the gamepad's left trigger is pressed.
        /// </summary>
        public static bool IsGamePadLeftTriggerPressed()
        {
            return (currentGamePadState.Triggers.Left > analogLimit);
        }

        /// <summary>
        /// Check if the gamepad's right trigger is pressed.
        /// </summary>
        public static bool IsGamePadRightTriggerPressed()
        {
            return (currentGamePadState.Triggers.Right > analogLimit);
        }

        /// <summary>
        /// Check if Up on the gamepad's left analog stick is pressed.
        /// </summary>
        public static bool IsGamePadLeftStickUpPressed()
        {
            return (currentGamePadState.ThumbSticks.Left.Y > analogLimit);
        }

        /// <summary>
        /// Check if Down on the gamepad's left analog stick is pressed.
        /// </summary>
        public static bool IsGamePadLeftStickDownPressed()
        {
            return (-1f * currentGamePadState.ThumbSticks.Left.Y > analogLimit);
        }

        /// <summary>
        /// Check if Left on the gamepad's left analog stick is pressed.
        /// </summary>
        public static bool IsGamePadLeftStickLeftPressed()
        {
            return (-1f * currentGamePadState.ThumbSticks.Left.X > analogLimit);
        }

        /// <summary>
        /// Check if Right on the gamepad's left analog stick is pressed.
        /// </summary>
        public static bool IsGamePadLeftStickRightPressed()
        {
            return (currentGamePadState.ThumbSticks.Left.X > analogLimit);
        }

        /// <summary>
        /// Check if the GamePadKey value specified is pressed.
        /// </summary>
        private static bool IsGamePadButtonPressed(GamePadButtons gamePadKey)
        {
            switch (gamePadKey)
            {
                case GamePadButtons.Start:
                    return IsGamePadStartPressed();

                case GamePadButtons.Back:
                    return IsGamePadBackPressed();

                case GamePadButtons.A:
                    return IsGamePadAPressed();

                case GamePadButtons.B:
                    return IsGamePadBPressed();

                case GamePadButtons.X:
                    return IsGamePadXPressed();

                case GamePadButtons.Y:
                    return IsGamePadYPressed();

                case GamePadButtons.LeftShoulder:
                    return IsGamePadLeftShoulderPressed();

                case GamePadButtons.RightShoulder:
                    return IsGamePadRightShoulderPressed();

                case GamePadButtons.LeftTrigger:
                    return IsGamePadLeftTriggerPressed();

                case GamePadButtons.RightTrigger:
                    return IsGamePadRightTriggerPressed();

                case GamePadButtons.Up:
                    return IsGamePadDPadUpPressed() ||
                        IsGamePadLeftStickUpPressed();

                case GamePadButtons.Down:
                    return IsGamePadDPadDownPressed() ||
                        IsGamePadLeftStickDownPressed();

                case GamePadButtons.Left:
                    return IsGamePadDPadLeftPressed() ||
                        IsGamePadLeftStickLeftPressed();

                case GamePadButtons.Right:
                    return IsGamePadDPadRightPressed() ||
                        IsGamePadLeftStickRightPressed();
            }

            return false;
        }

        /// <summary>
        /// Check if the gamepad's Start button was just pressed.
        /// </summary>
        public static bool IsGamePadStartTriggered()
        {
            return ((currentGamePadState.Buttons.Start == ButtonState.Pressed) &&
              (previousGamePadState.Buttons.Start == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's Back button was just pressed.
        /// </summary>
        public static bool IsGamePadBackTriggered()
        {
            return ((currentGamePadState.Buttons.Back == ButtonState.Pressed) &&
              (previousGamePadState.Buttons.Back == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's A button was just pressed.
        /// </summary>
        public static bool IsGamePadATriggered()
        {
            return ((currentGamePadState.Buttons.A == ButtonState.Pressed) &&
              (previousGamePadState.Buttons.A == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's B button was just pressed.
        /// </summary>
        public static bool IsGamePadBTriggered()
        {
            return ((currentGamePadState.Buttons.B == ButtonState.Pressed) &&
              (previousGamePadState.Buttons.B == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's X button was just pressed.
        /// </summary>
        public static bool IsGamePadXTriggered()
        {
            return ((currentGamePadState.Buttons.X == ButtonState.Pressed) &&
              (previousGamePadState.Buttons.X == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's Y button was just pressed.
        /// </summary>
        public static bool IsGamePadYTriggered()
        {
            return ((currentGamePadState.Buttons.Y == ButtonState.Pressed) &&
              (previousGamePadState.Buttons.Y == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's LeftShoulder button was just pressed.
        /// </summary>
        public static bool IsGamePadLeftShoulderTriggered()
        {
            return (
                (currentGamePadState.Buttons.LeftShoulder == ButtonState.Pressed) &&
                (previousGamePadState.Buttons.LeftShoulder == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's RightShoulder button was just pressed.
        /// </summary>
        public static bool IsGamePadRightShoulderTriggered()
        {
            return (
                (currentGamePadState.Buttons.RightShoulder == ButtonState.Pressed) &&
                (previousGamePadState.Buttons.RightShoulder == ButtonState.Released));
        }

        /// <summary>
        /// Check if Up on the gamepad's directional pad was just pressed.
        /// </summary>
        public static bool IsGamePadDPadUpTriggered()
        {
            return ((currentGamePadState.DPad.Up == ButtonState.Pressed) &&
              (previousGamePadState.DPad.Up == ButtonState.Released));
        }

        /// <summary>
        /// Check if Down on the gamepad's directional pad was just pressed.
        /// </summary>
        public static bool IsGamePadDPadDownTriggered()
        {
            return ((currentGamePadState.DPad.Down == ButtonState.Pressed) &&
              (previousGamePadState.DPad.Down == ButtonState.Released));
        }

        /// <summary>
        /// Check if Left on the gamepad's directional pad was just pressed.
        /// </summary>
        public static bool IsGamePadDPadLeftTriggered()
        {
            return ((currentGamePadState.DPad.Left == ButtonState.Pressed) &&
              (previousGamePadState.DPad.Left == ButtonState.Released));
        }

        /// <summary>
        /// Check if Right on the gamepad's directional pad was just pressed.
        /// </summary>
        public static bool IsGamePadDPadRightTriggered()
        {
            return ((currentGamePadState.DPad.Right == ButtonState.Pressed) &&
              (previousGamePadState.DPad.Right == ButtonState.Released));
        }

        /// <summary>
        /// Check if the gamepad's left trigger was just pressed.
        /// </summary>
        public static bool IsGamePadLeftTriggerTriggered()
        {
            return ((currentGamePadState.Triggers.Left > analogLimit) &&
                (previousGamePadState.Triggers.Left < analogLimit));
        }

        /// <summary>
        /// Check if the gamepad's right trigger was just pressed.
        /// </summary>
        public static bool IsGamePadRightTriggerTriggered()
        {
            return ((currentGamePadState.Triggers.Right > analogLimit) &&
                (previousGamePadState.Triggers.Right < analogLimit));
        }

        /// <summary>
        /// Check if Up on the gamepad's left analog stick was just pressed.
        /// </summary>
        public static bool IsGamePadLeftStickUpTriggered()
        {
            return ((currentGamePadState.ThumbSticks.Left.Y > analogLimit) &&
                (previousGamePadState.ThumbSticks.Left.Y < analogLimit));
        }

        /// <summary>
        /// Check if Down on the gamepad's left analog stick was just pressed.
        /// </summary>
        public static bool IsGamePadLeftStickDownTriggered()
        {
            return ((-1f * currentGamePadState.ThumbSticks.Left.Y > analogLimit) &&
                (-1f * previousGamePadState.ThumbSticks.Left.Y < analogLimit));
        }

        /// <summary>
        /// Check if Left on the gamepad's left analog stick was just pressed.
        /// </summary>
        public static bool IsGamePadLeftStickLeftTriggered()
        {
            return ((-1f * currentGamePadState.ThumbSticks.Left.X > analogLimit) &&
                (-1f * previousGamePadState.ThumbSticks.Left.X < analogLimit));
        }

        /// <summary>
        /// Check if Right on the gamepad's left analog stick was just pressed.
        /// </summary>
        public static bool IsGamePadLeftStickRightTriggered()
        {
            return ((currentGamePadState.ThumbSticks.Left.X > analogLimit) &&
                (previousGamePadState.ThumbSticks.Left.X < analogLimit));
        }

        /// <summary>
        /// Check if the GamePadKey value specified was pressed this frame.
        /// </summary>
        private static bool IsGamePadButtonTriggered(GamePadButtons gamePadKey)
        {
            switch (gamePadKey)
            {
                case GamePadButtons.Start:
                    return IsGamePadStartTriggered();

                case GamePadButtons.Back:
                    return IsGamePadBackTriggered();

                case GamePadButtons.A:
                    return IsGamePadATriggered();

                case GamePadButtons.B:
                    return IsGamePadBTriggered();

                case GamePadButtons.X:
                    return IsGamePadXTriggered();

                case GamePadButtons.Y:
                    return IsGamePadYTriggered();

                case GamePadButtons.LeftShoulder:
                    return IsGamePadLeftShoulderTriggered();

                case GamePadButtons.RightShoulder:
                    return IsGamePadRightShoulderTriggered();

                case GamePadButtons.LeftTrigger:
                    return IsGamePadLeftTriggerTriggered();

                case GamePadButtons.RightTrigger:
                    return IsGamePadRightTriggerTriggered();

                case GamePadButtons.Up:
                    return IsGamePadDPadUpTriggered() ||
                        IsGamePadLeftStickUpTriggered();

                case GamePadButtons.Down:
                    return IsGamePadDPadDownTriggered() ||
                        IsGamePadLeftStickDownTriggered();

                case GamePadButtons.Left:
                    return IsGamePadDPadLeftTriggered() ||
                        IsGamePadLeftStickLeftTriggered();

                case GamePadButtons.Right:
                    return IsGamePadDPadRightTriggered() ||
                        IsGamePadLeftStickRightTriggered();
            }

            return false;
        }

        /// <summary>
        /// The state of the keyboard as of the last update.
        /// </summary>
        private static MouseState currentMouseState;

        /// <summary>
        /// The state of the mouse as of the last update.
        /// </summary>
        public static MouseState CurrentMouseState
        {
            get { return currentMouseState; }
        }

        /// <summary>
        /// The state of the mouse as of the previous update.
        /// </summary>
        private static MouseState previousMouseState;

        /// <summary>
        /// Check if the left mouse button was just clicked.
        /// </summary>
        public static bool IsMouseLeftButtonClick()
        {
            return currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released;
        }

        /// <summary>
        /// Check if the right mouse button was just clicked.
        /// </summary>
        public static bool IsMouseRightButtonClick()
        {
            return currentMouseState.RightButton == ButtonState.Pressed &&
                previousMouseState.RightButton == ButtonState.Released;
        }

        /// <summary>
        /// Check if the middle mouse button was just clicked.
        /// </summary>
        public static bool IsMouseMiddleButtonClick()
        {
            return currentMouseState.MiddleButton == ButtonState.Pressed &&
                previousMouseState.MiddleButton == ButtonState.Released;
        }

        /// <summary>
        /// Check if the mouse wheel was scrolled up.
        /// </summary>
        public static bool IsMouseWheelScrolledUp()
        {
            return currentMouseState.ScrollWheelValue > previousMouseState.ScrollWheelValue;
        }

        /// <summary>
        /// Check if the mouse wheel was scrolled down.
        /// </summary>
        public static bool IsMouseWheelScrolledDown()
        {
            return currentMouseState.ScrollWheelValue < previousMouseState.ScrollWheelValue;
        }

        private static int lastClickTime;
        private static ButtonState lastClickButtonState;

        /// <summary>
        /// Check if the left mouse button was double-clicked.
        /// </summary>
        public static bool IsMouseLeftButtonDoubleClick()
        {
            int currentTime = Environment.TickCount;
            bool isDoubleClick = currentMouseState.LeftButton == ButtonState.Pressed &&
                                 previousMouseState.LeftButton == ButtonState.Released &&
                                 lastClickButtonState == ButtonState.Pressed &&
                                 (currentTime - lastClickTime <= 500); // 500ms threshold

            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                lastClickTime = currentTime;
                lastClickButtonState = ButtonState.Pressed;
            }
            else if (currentMouseState.LeftButton == ButtonState.Released)
            {
                lastClickButtonState = ButtonState.Released;
            }

            return isDoubleClick;
        }

        /// <summary>
        /// Check if a specific mouse button is pressed.
        /// </summary>
        public static bool IsMouseButtonPressed(MouseButtons button)
        {
            switch (button)
            {
                case MouseButtons.LeftButton:
                    return IsMouseLeftButtonPressed();
                case MouseButtons.RightButton:
                    return IsMouseRightButtonPressed();
                case MouseButtons.MiddleButton:
                    return IsMouseMiddleButtonPressed();
                default:
                    return false;
            }
        }

        private static bool IsMouseMiddleButtonPressed()
        {
            return currentMouseState.MiddleButton == ButtonState.Pressed;
        }

        private static bool IsMouseRightButtonPressed()
        {
            return currentMouseState.RightButton == ButtonState.Pressed;
        }

        private static bool IsMouseLeftButtonPressed()
        {
            return currentMouseState.LeftButton == ButtonState.Pressed;
        }

        public static void HandleMouseDown(Point playerPosition)
        {
            var mouseDown = IsMouseButtonPressed(MouseButtons.LeftButton);
            var touchDown = IsTouchPressed();
            if (mouseDown
            || touchDown)
            {
                // Get the current mouse/touch position
                Point mouseDownPosition = mouseDown
                    ? new Point(currentMouseState.X, currentMouseState.Y)
                    : currentTouchPanelState.Count > 0 ? new Point((int)currentTouchPanelState[0].Position.X, (int)currentTouchPanelState[0].Position.Y) : Point.Zero;

                // Determine which keys to press based on the mouse down position relative to the player position
                Keys[] keysToPress = GetKeysForQuadrant(playerPosition, mouseDownPosition);

                // Combine existing pressed keys with new keys
                var pressedKeys = new HashSet<Keys>(currentKeyboardState.GetPressedKeys());
                foreach (Keys key in keysToPress)
                {
                    pressedKeys.Add(key);
                }

                // Update currentKeyboardState with the combined keys
                currentKeyboardState = new KeyboardState(pressedKeys.ToArray());

            }
        }

        /// <summary>
        /// The state of the keyboard as of the last update.
        /// </summary>
        private static TouchCollection currentTouchPanelState;

        /// <summary>
        /// The state of the mouse as of the last update.
        /// </summary>
        public static TouchCollection CurrentTouchPanelState
        {
            get { return currentTouchPanelState; }
        }

        /// <summary>
        /// The state of the mouse as of the previous update.
        /// </summary>
        private static TouchCollection previousTouchPanelState;

        // Update methods to work with TouchCollection
        public static bool IsTouchPressed()
        {
            foreach (var touch in currentTouchPanelState)
            {
                if (touch.State == TouchLocationState.Pressed)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsTouchReleased()
        {
            foreach (var touch in currentTouchPanelState)
            {
                if (touch.State == TouchLocationState.Released)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsTouchMoved()
        {
            foreach (var touch in currentTouchPanelState)
            {
                if (touch.State == TouchLocationState.Moved)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsGestureDetected(GestureType gestureType)
        {
            // Gesture detection is not supported directly by TouchCollection
            return false;
        }

        private static Keys[] GetKeysForQuadrant(Point playerPosition, Point mouseDownPosition)
        {
            int deltaX = mouseDownPosition.X - playerPosition.X;
            int deltaY = mouseDownPosition.Y - playerPosition.Y;

            if (deltaY < 0 && Math.Abs(deltaX) <= Math.Abs(deltaY)) // Up
            {
                return new Keys[] { Keys.Up };
            }
            else if (deltaY > 0 && Math.Abs(deltaX) <= Math.Abs(deltaY)) // Down
            {
                return new Keys[] { Keys.Down };
            }
            else if (deltaX > 0 && Math.Abs(deltaY) <= Math.Abs(deltaX)) // Right
            {
                return new Keys[] { Keys.Right };
            }
            else if (deltaX < 0 && Math.Abs(deltaY) <= Math.Abs(deltaX)) // Left
            {
                return new Keys[] { Keys.Left };
            }
            else if (deltaX > 0 && deltaY < 0) // Up-Right
            {
                return new Keys[] { Keys.Up, Keys.Right };
            }
            else if (deltaX > 0 && deltaY > 0) // Down-Right
            {
                return new Keys[] { Keys.Down, Keys.Right };
            }
            else if (deltaX < 0 && deltaY < 0) // Up-Left
            {
                return new Keys[] { Keys.Up, Keys.Left };
            }
            else if (deltaX < 0 && deltaY > 0) // Down-Left
            {
                return new Keys[] { Keys.Down, Keys.Left };
            }

            return new Keys[] { }; // Default case
        }

        /// <summary>
        /// The action mappings for the game.
        /// </summary>
        private static ActionMap[] actionMaps;

        public static ActionMap[] ActionMaps
        {
            get { return actionMaps; }
        }

        /// <summary>
        /// Reset the action maps to their default values.
        /// </summary>
        private static void ResetActionMaps()
        {
            actionMaps = new ActionMap[(int)InputAction.TotalActionCount];

            actionMaps[(int)InputAction.MainMenu] = new ActionMap();
            actionMaps[(int)InputAction.MainMenu].keyboardKeys.Add(Keys.Tab);
            actionMaps[(int)InputAction.MainMenu].gamePadButtons.Add(GamePadButtons.Start);

            actionMaps[(int)InputAction.Ok] = new ActionMap();
            actionMaps[(int)InputAction.Ok].keyboardKeys.Add(Keys.Enter);
            actionMaps[(int)InputAction.Ok].gamePadButtons.Add(GamePadButtons.A);

            actionMaps[(int)InputAction.Back] = new ActionMap();
            actionMaps[(int)InputAction.Back].keyboardKeys.Add(Keys.Escape);
            actionMaps[(int)InputAction.Back].gamePadButtons.Add(GamePadButtons.B);

            actionMaps[(int)InputAction.CharacterManagement] = new ActionMap();
            actionMaps[(int)InputAction.CharacterManagement].keyboardKeys.Add(Keys.Space);
            actionMaps[(int)InputAction.CharacterManagement].gamePadButtons.Add(GamePadButtons.Y);

            actionMaps[(int)InputAction.ExitGame] = new ActionMap();
            actionMaps[(int)InputAction.ExitGame].keyboardKeys.Add(Keys.Escape);
            actionMaps[(int)InputAction.ExitGame].gamePadButtons.Add(GamePadButtons.Back);

            actionMaps[(int)InputAction.TakeView] = new ActionMap();
            actionMaps[(int)InputAction.TakeView].keyboardKeys.Add(Keys.LeftControl);
            actionMaps[(int)InputAction.TakeView].gamePadButtons.Add(GamePadButtons.Y);

            actionMaps[(int)InputAction.DropUnEquip] = new ActionMap();
            actionMaps[(int)InputAction.DropUnEquip].keyboardKeys.Add(Keys.D);
            actionMaps[(int)InputAction.DropUnEquip].gamePadButtons.Add(GamePadButtons.X);

            actionMaps[(int)InputAction.MoveCharacterUp] = new ActionMap();
            actionMaps[(int)InputAction.MoveCharacterUp].keyboardKeys.Add(Keys.Up);
            actionMaps[(int)InputAction.MoveCharacterUp].gamePadButtons.Add(GamePadButtons.Up);

            actionMaps[(int)InputAction.MoveCharacterDown] = new ActionMap();
            actionMaps[(int)InputAction.MoveCharacterDown].keyboardKeys.Add(Keys.Down);
            actionMaps[(int)InputAction.MoveCharacterDown].gamePadButtons.Add(GamePadButtons.Down);

            actionMaps[(int)InputAction.MoveCharacterLeft] = new ActionMap();
            actionMaps[(int)InputAction.MoveCharacterLeft].keyboardKeys.Add(Keys.Left);
            actionMaps[(int)InputAction.MoveCharacterLeft].gamePadButtons.Add(GamePadButtons.Left);

            actionMaps[(int)InputAction.MoveCharacterRight] = new ActionMap();
            actionMaps[(int)InputAction.MoveCharacterRight].keyboardKeys.Add(Keys.Right);
            actionMaps[(int)InputAction.MoveCharacterRight].gamePadButtons.Add(GamePadButtons.Right);

            actionMaps[(int)InputAction.CursorUp] = new ActionMap();
            actionMaps[(int)InputAction.CursorUp].keyboardKeys.Add(Keys.Up);
            actionMaps[(int)InputAction.CursorUp].gamePadButtons.Add(GamePadButtons.Up);

            actionMaps[(int)InputAction.CursorDown] = new ActionMap();
            actionMaps[(int)InputAction.CursorDown].keyboardKeys.Add(Keys.Down);
            actionMaps[(int)InputAction.CursorDown].gamePadButtons.Add(GamePadButtons.Down);

            actionMaps[(int)InputAction.DecreaseAmount] = new ActionMap();
            actionMaps[(int)InputAction.DecreaseAmount].keyboardKeys.Add(Keys.Left);
            actionMaps[(int)InputAction.DecreaseAmount].gamePadButtons.Add(GamePadButtons.Left);

            actionMaps[(int)InputAction.IncreaseAmount] = new ActionMap();
            actionMaps[(int)InputAction.IncreaseAmount].keyboardKeys.Add(Keys.Right);
            actionMaps[(int)InputAction.IncreaseAmount].gamePadButtons.Add(GamePadButtons.Right);

            actionMaps[(int)InputAction.PageLeft] = new ActionMap();
            actionMaps[(int)InputAction.PageLeft].keyboardKeys.Add(Keys.LeftShift);
            actionMaps[(int)InputAction.PageLeft].gamePadButtons.Add(GamePadButtons.LeftTrigger);

            actionMaps[(int)InputAction.PageRight] = new ActionMap();
            actionMaps[(int)InputAction.PageRight].keyboardKeys.Add(Keys.RightShift);
            actionMaps[(int)InputAction.PageRight].gamePadButtons.Add(GamePadButtons.RightTrigger);

            actionMaps[(int)InputAction.TargetUp] = new ActionMap();
            actionMaps[(int)InputAction.TargetUp].keyboardKeys.Add(Keys.Up);
            actionMaps[(int)InputAction.TargetUp].gamePadButtons.Add(GamePadButtons.Up);

            actionMaps[(int)InputAction.TargetDown] = new ActionMap();
            actionMaps[(int)InputAction.TargetDown].keyboardKeys.Add(Keys.Down);
            actionMaps[(int)InputAction.TargetDown].gamePadButtons.Add(GamePadButtons.Down);

            actionMaps[(int)InputAction.ActiveCharacterLeft] = new ActionMap();
            actionMaps[(int)InputAction.ActiveCharacterLeft].keyboardKeys.Add(Keys.Left);
            actionMaps[(int)InputAction.ActiveCharacterLeft].gamePadButtons.Add(GamePadButtons.Left);

            actionMaps[(int)InputAction.ActiveCharacterRight] = new ActionMap();
            actionMaps[(int)InputAction.ActiveCharacterRight].keyboardKeys.Add(Keys.Right);
            actionMaps[(int)InputAction.ActiveCharacterRight].gamePadButtons.Add(GamePadButtons.Right);
        }

        /// <summary>
        /// Check if an action has been pressed.
        /// </summary>
        public static bool IsActionPressed(InputAction action)
        {
            return IsActionMapPressed(actionMaps[(int)action]);
        }

        /// <summary>
        /// Check if an action was just performed in the most recent update.
        /// </summary>
        public static bool IsActionTriggered(InputAction action)
        {
            return IsActionMapTriggered(actionMaps[(int)action]);
        }

        /// <summary>
        /// Check if an action map has been pressed.
        /// </summary>
        private static bool IsActionMapPressed(ActionMap actionMap)
        {
            for (int i = 0; i < actionMap.keyboardKeys.Count; i++)
            {
                if (IsKeyPressed(actionMap.keyboardKeys[i]))
                {
                    return true;
                }
            }
            if (currentGamePadState.IsConnected)
            {
                for (int i = 0; i < actionMap.gamePadButtons.Count; i++)
                {
                    if (IsGamePadButtonPressed(actionMap.gamePadButtons[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if an action map has been triggered this frame.
        /// </summary>
        private static bool IsActionMapTriggered(ActionMap actionMap)
        {
            for (int i = 0; i < actionMap.keyboardKeys.Count; i++)
            {
                if (IsKeyTriggered(actionMap.keyboardKeys[i]))
                {
                    return true;
                }
            }

            if (currentGamePadState.IsConnected)
            {
                for (int i = 0; i < actionMap.gamePadButtons.Count; i++)
                {
                    if (IsGamePadButtonTriggered(actionMap.gamePadButtons[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Initializes the default control keys for all actions.
        /// </summary>
        public static void Initialize()
        {
            ResetActionMaps();
        }

        /// <summary>
        /// Updates the keyboard and gamepad control states.
        /// </summary>
        public static void Update()
        {
            // update the keyboard state
            previousKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            // update the gamepad state
            previousGamePadState = currentGamePadState;
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // update the mouse state
            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            // update the touch panel state
            previousTouchPanelState = currentTouchPanelState;
            currentTouchPanelState = TouchPanel.GetState();
        }
    }
}