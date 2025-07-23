//-----------------------------------------------------------------------------
// MenuComponent.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace XnaGraphicsDemo
{
    /// <summary>
    /// Base class for all the different screens used in the demo. This provides
    /// a simple touch menu which can display a list of options, and detect when
    /// a menu item is clicked.
    /// </summary>
    class MenuComponent : DrawableGameComponent
    {
        // Properties.
        /// <summary>
        /// Gets the game instance as a DemoGame.
        /// </summary>
        new public DemoGame Game { get { return (DemoGame)base.Game; } }
        /// <returns>The game instance as a DemoGame.</returns>

        /// <summary>
        /// Gets the SpriteBatch used for drawing.
        /// </summary>
        public SpriteBatch SpriteBatch { get { return Game.SpriteBatch; } }
        /// <returns>The SpriteBatch used for drawing.</returns>
        /// <summary>
        /// Gets the default font for menu text.
        /// </summary>
        public SpriteFont Font { get { return Game.Font; } }
        /// <returns>The default font for menu text.</returns>
        /// <summary>
        /// Gets the large font for menu titles.
        /// </summary>
        public SpriteFont BigFont { get { return Game.BigFont; } }
        /// <returns>The large font for menu titles.</returns>

        /// <summary>
        /// Gets the list of menu entries.
        /// </summary>
        protected List<MenuEntry> Entries { get; private set; }
        /// <returns>The list of menu entries.</returns>

        /// <summary>
        /// Gets the last touch point position.
        /// </summary>
        protected Vector2 LastTouchPoint { get; private set; }
        /// <returns>The last touch point position.</returns>


        // Fields.
        /// <summary>
        /// Indicates whether a touch is currently active.
        /// </summary>
        bool touchDown = true;

        /// <summary>
        /// The index of the currently selected menu entry for navigation.
        /// Also used in attract mode to keep everything in sync.
        /// </summary>
        protected int selectedEntry = 0;

        // Static field to track the last selected menu item across all menus
        /// <summary>
        /// Tracks the last selected menu item across all menus.
        /// </summary>
        static int lastSelectedMenuItem = 0;

        /// <summary>
        /// Timer for attract (demo) mode inactivity.
        /// </summary>
        static TimeSpan attractTimer;
        /// <summary>
        /// Stores the last mouse input state.
        /// </summary>
        static MouseState lastInputState = new MouseState(-1, -1, -1, 0, 0, 0, 0, 0);
        /// <summary>
        /// Stores the last keyboard input state.
        /// </summary>
        static KeyboardState lastKeyboardState;


        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuComponent(DemoGame game)
            : base(game)
        {
            Entries = new List<MenuEntry>();
        }


        /// <summary>
        /// Initializes the menu, computing the screen position of each entry.
        /// </summary>
        public override void Initialize()
        {
            Vector2 pos = new Vector2(MenuEntry.Border, 800 - MenuEntry.Border - Entries.Count * MenuEntry.Height);

            foreach (MenuEntry entry in Entries)
            {
                entry.Position = pos;

                pos.Y += MenuEntry.Height;
            }

            base.Initialize();
        }


        /// <summary>
        /// Resets the menu, whenever we transition to or from a different screen.
        /// </summary>
        virtual public void Reset()
        {
            if (selectedEntry >= 0)
                Entries[selectedEntry].IsFocused = false;

            touchDown = true;

            // Restore the last selected menu item if valid, otherwise select the first item
            selectedEntry = (lastSelectedMenuItem >= 0 && lastSelectedMenuItem < Entries.Count)
                              ? lastSelectedMenuItem
                              : 0;

            // Set initial keyboard focus
            UpdateMenuFocus();
        }


        /// <summary>
        /// Updates the menu state, processing user input.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // We read input using the mouse API, which will report the first touch point
            // when run on the phone, but also works on Windows using a regular mouse.
            MouseState input = Game.IsActive ? Mouse.GetState() : new MouseState();
            KeyboardState keyboardInput = Game.IsActive ? Keyboard.GetState() : new KeyboardState();

            // Handle keyboard input
            HandleKeyboardInput(keyboardInput);

            // Scale input if we are running in an unusual screen resolution.
            int touchX = input.X * 480 / Game.Graphics.PreferredBackBufferWidth;
            int touchY = input.Y * 800 / Game.Graphics.PreferredBackBufferHeight;

            // Process the input.
            if (input.LeftButton == ButtonState.Pressed)
            {
                HandleTouchDown(touchX, touchY);
            }
            else
            {
                HandleTouchUp();
            }

            HandleAttractMode(gameTime, input, keyboardInput);
        }


        /// <summary>
        /// Handles input while a touch is occurring.
        /// </summary>
        /// <summary>
        /// Handles input while a touch is occurring, updating selection and focus.
        /// </summary>
        void HandleTouchDown(int touchX, int touchY)
        {
            // Hit test the touch position against the list of menu items.
            int currentEntry = -1;

            for (int i = 0; i < Entries.Count; i++)
            {
                if ((touchY >= Entries[i].Position.Y) && (touchY < Entries[i].Position.Y + MenuEntry.Height))
                {
                    currentEntry = i;
                    break;
                }
            }

            if (touchDown)
            {
                // Are we already processing a touch?
                if (selectedEntry >= 0)
                {
                    if (currentEntry == selectedEntry || Entries[selectedEntry].IsDraggable)
                    {
                        // Pass drag input to the currently selected item.
                        Entries[selectedEntry].IsFocused = true;

                        Entries[selectedEntry].OnDragged(touchX - LastTouchPoint.X);
                    }
                    else
                    {
                        // If the drag moves off the selected item, unfocus it.
                        Entries[selectedEntry].IsFocused = false;
                    }
                }
                else
                {
                    // If the touch was not on any menu item, process a backgroun drag.
                    OnDrag(new Vector2(touchX, touchY) - LastTouchPoint);
                }
            }
            else
            {
                // We are not currently processing a touch.
                touchDown = true;
                selectedEntry = currentEntry;

                // Clear keyboard focus when using touch
                foreach (MenuEntry entry in Entries)
                    entry.IsFocused = false;

                if (selectedEntry >= 0)
                {
                    // Focus the menu item that has just been touched.
                    Entries[selectedEntry].IsFocused = true;
                }
            }

            // Store the most recent touch location.
            LastTouchPoint = new Vector2(touchX, touchY);
        }


        /// <summary>
        /// Handles input when the touch is released.
        /// </summary>
        /// <summary>
        /// Handles input when the touch is released, triggering click actions if needed.
        /// </summary>
        void HandleTouchUp()
        {
            if (touchDown && selectedEntry >= 0 && Entries[selectedEntry].IsFocused)
            {
                // Save the current selection as the last selected menu item
                lastSelectedMenuItem = selectedEntry;

                // If we were touching a menu item, and just released it, process the click action.
                Entries[selectedEntry].IsFocused = false;
                Entries[selectedEntry].OnClicked();
            }

            touchDown = false;
        }


        /// <summary>
        /// Checks if there's any actual keyboard activity between two keyboard states.
        /// </summary>
        /// <summary>
        /// Checks if there is any keyboard activity between two keyboard states.
        /// </summary>
        /// <param name="current">The current keyboard state.</param>
        /// <param name="previous">The previous keyboard state.</param>
        /// <returns>True if there is keyboard activity; otherwise, false.</returns>
        bool HasKeyboardActivity(KeyboardState current, KeyboardState previous)
        {
            // Check if any key was pressed or released
            Keys[] currentKeys = current.GetPressedKeys();
            Keys[] previousKeys = previous.GetPressedKeys();

            // If the number of pressed keys changed, there's activity
            if (currentKeys.Length != previousKeys.Length)
                return true;

            // Check if any different keys are pressed
            foreach (Keys key in currentKeys)
            {
                if (!previous.IsKeyDown(key))
                    return true;
            }

            foreach (Keys key in previousKeys)
            {
                if (!current.IsKeyDown(key))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// If no input is provided, we go into an automatic attract mode, which cycles
        /// through the various options. This was great for leaving the demo unattended
        /// at the kiosk during the MIX10 conference!
        /// </summary>
        /// <param name="gameTime">The current game time.</param>
        /// <param name="input">The current mouse state.</param>
        /// <param name="keyboardInput">The current keyboard state.</param>
        void HandleAttractMode(GameTime gameTime, MouseState input, KeyboardState keyboardInput)
        {
            // Check if there's any actual keyboard activity
            bool keyboardActivity = HasKeyboardActivity(keyboardInput, lastKeyboardState);

            if (input != lastInputState || keyboardActivity || touchDown)
            {
                // If input has changed, reset the timer.
                attractTimer = TimeSpan.FromSeconds(-15);
                lastInputState = input;
                lastKeyboardState = keyboardInput;
            }
            else
            {
                // If no input occurs, increment the timer.
                attractTimer += gameTime.ElapsedGameTime;

                if (attractTimer > AttractDelay)
                {
                    // Timeout! Run the attract action.
                    attractTimer = TimeSpan.Zero;
                    OnAttract();
                }
            }
        }


        /// <summary>
        /// Allows subclasses to customize their attract behavior. The default is
        /// to simulate a click on the last menu entry, which is usually "back".
        /// </summary>
        protected virtual void OnAttract()
        {
            Entries[Entries.Count - 1].OnClicked();
        }


        /// <summary>
        /// Allows subclasses to customize how long they wait before cycling through the attract sequence.
        /// </summary>
        protected virtual TimeSpan AttractDelay { get { return TimeSpan.FromSeconds(10); } }


        /// <summary>
        /// Draws the list of menu entries.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(0, null, null, null, null, null, Game.ScaleMatrix);

            foreach (MenuEntry entry in Entries)
            {
                entry.Draw(SpriteBatch, Font, Game.BlankTexture);
            }

            SpriteBatch.End();
        }


        /// <summary>
        /// Draws the menu title at the top of the screen, optionally with a background color.
        /// </summary>
        /// <param name="title">The title text to display.</param>
        /// <param name="backgroundColor">The background color to use, or null for none.</param>
        /// <param name="titleColor">The color to use for the title text.</param>
        protected void DrawTitle(string title, Color? backgroundColor, Color titleColor)
        {
            if (backgroundColor.HasValue)
                GraphicsDevice.Clear(backgroundColor.Value);

            SpriteBatch.Begin(0, null, null, null, null, null, Game.ScaleMatrix);
            SpriteBatch.DrawString(BigFont, title, new Vector2(480, 24), titleColor, MathHelper.PiOver2, Vector2.Zero, 1, 0, 0);
            SpriteBatch.End();
        }


        /// <summary>
        /// Handles a drag on the background of the screen. Subclasses can override to implement custom drag behavior.
        /// </summary>
        /// <param name="delta">The amount the pointer has moved since the last drag event.</param>
        protected virtual void OnDrag(Vector2 delta)
        {
        }

        /// <summary>
        /// Handles keyboard input for menu navigation and selection.
        /// </summary>
        /// <param name="keyboardInput">The current keyboard state.</param>
        void HandleKeyboardInput(KeyboardState keyboardInput)
        {
            // Check for new key presses
            bool upPressed = keyboardInput.IsKeyDown(Keys.Up) && !lastKeyboardState.IsKeyDown(Keys.Up);
            bool downPressed = keyboardInput.IsKeyDown(Keys.Down) && !lastKeyboardState.IsKeyDown(Keys.Down);
            bool enterPressed = (keyboardInput.IsKeyDown(Keys.Enter) && !lastKeyboardState.IsKeyDown(Keys.Enter)) ||
                               (keyboardInput.IsKeyDown(Keys.Space) && !lastKeyboardState.IsKeyDown(Keys.Space));
            bool escapePressed = keyboardInput.IsKeyDown(Keys.Escape) && !lastKeyboardState.IsKeyDown(Keys.Escape);

            // Handle navigation
            if (upPressed && Entries.Count > 0)
            {
                // Clear touch selection and focus when using keyboard
                if (selectedEntry >= 0)
                    Entries[selectedEntry].IsFocused = false;


                selectedEntry--;
                if (selectedEntry < 0)
                    selectedEntry = Entries.Count - 1;

                UpdateMenuFocus();
            }
            else if (downPressed && Entries.Count > 0)
            {
                // Clear touch selection and focus when using keyboard
                if (selectedEntry >= 0)
                    Entries[selectedEntry].IsFocused = false;

                selectedEntry++;
                if (selectedEntry >= Entries.Count)
                    selectedEntry = 0;

                UpdateMenuFocus();
            }
            else if (enterPressed && Entries.Count > 0)
            {
                // Save the current selection as the last selected menu item
                lastSelectedMenuItem = selectedEntry;

                // Execute the currently selected menu item
                Entries[selectedEntry].OnClicked();
            }
            else if (escapePressed)
            {
                // Go back - simulate clicking the last menu entry (usually "back" or "quit")
                if (Entries.Count > 0)
                    Entries[Entries.Count - 1].OnClicked();
            }

            lastKeyboardState = keyboardInput;
        }

        /// <summary>
        /// Updates the focus state for keyboard navigation, ensuring only the selected entry is focused.
        /// </summary>
        protected void UpdateMenuFocus()
        {
            // Clear all focus first
            foreach (MenuEntry entry in Entries)
                entry.IsFocused = false;

            // Set focus on the keyboard-selected item
            if (selectedEntry >= 0 && selectedEntry < Entries.Count)
                Entries[selectedEntry].IsFocused = true;
        }

        /// <summary>
        /// Gets the currently selected menu item index for keyboard navigation.
        /// </summary>
        /// <returns>The index of the selected menu item.</returns>
        public int GetSelectedIndex()
        {
            return selectedEntry;
        }

        /// <summary>
        /// Sets the selected menu item index for keyboard navigation, updating focus accordingly.
        /// </summary>
        /// <param name="index">The index of the menu item to select.</param>
        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < Entries.Count)
            {
                selectedEntry = index;
                lastSelectedMenuItem = index;
                UpdateMenuFocus();
            }
        }
    }
}