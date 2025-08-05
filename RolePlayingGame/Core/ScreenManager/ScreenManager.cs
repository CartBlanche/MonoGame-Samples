//-----------------------------------------------------------------------------
// ScreenManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using Microsoft.Xna.Framework.Input.Touch;

namespace RolePlaying
{
    /// <summary>
    /// The screen manager is a component which manages one or more GameScreen
    /// instances. It maintains a stack of screens, calls their Update and Draw
    /// methods at the appropriate times, and automatically routes input to the
    /// topmost active screen.
    /// </summary>
    /// <remarks>
    /// Similar to a class found in the Game State Management sample on the 
    /// XNA Creators Club Online website (http://creators.xna.com).
    /// </remarks>
    public class ScreenManager : DrawableGameComponent
    {
        List<GameScreen> screens = new List<GameScreen>();
        List<GameScreen> screensToUpdate = new List<GameScreen>();

        SpriteBatch spriteBatch;
        private Texture2D blankTexture;

        bool isInitialized;
        bool traceEnabled;

        private int backbufferWidth;
        /// <summary>Gets or sets the current backbuffer width.</summary>
        public int BackbufferWidth { get => backbufferWidth; set => backbufferWidth = value; }

        private int backbufferHeight;
        /// <summary>Gets or sets the current backbuffer height.</summary>
        public int BackbufferHeight { get => backbufferHeight; set => backbufferHeight = value; }

        private Vector2 baseScreenSize = new Vector2(Session.BACK_BUFFER_WIDTH, Session.BACK_BUFFER_HEIGHT);
        /// <summary>Gets or sets the base screen size used for scaling calculations.</summary>
        public Vector2 BaseScreenSize { get => baseScreenSize; set => baseScreenSize = value; }

        private Matrix globalTransformation;
        /// <summary>Gets or sets the global transformation matrix for scaling and positioning.</summary>
        public Matrix GlobalTransformation { get => globalTransformation; set => globalTransformation = value; }

        /// <summary>
        /// A default SpriteBatch shared by all the screens. This saves
        /// each screen having to bother creating their own local instance.
        /// </summary>
        public SpriteBatch SpriteBatch
        {
            get { return spriteBatch; }
        }

        /// <summary>
        /// If true, the manager prints out a list of all the screens
        /// each time it is updated. This can be useful for making sure
        /// everything is being added and removed at the right times.
        /// </summary>
        public bool TraceEnabled
        {
            get { return traceEnabled; }
            set { traceEnabled = value; }
        }

        /// <summary>
        /// Constructs a new screen manager component.
        /// </summary>
        public ScreenManager(Game game)
            : base(game)
        {
            TouchPanel.EnabledGestures = GestureType.None;
        }

        /// <summary>
        /// Initializes the screen manager component.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            isInitialized = true;
        }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load content belonging to the screen manager.
            ContentManager content = Game.Content;

            spriteBatch = new SpriteBatch(GraphicsDevice);

            blankTexture = content.Load<Texture2D>("Textures/GameScreens/blank");

            // Tell each of the screens to load their content.
            foreach (GameScreen screen in screens)
            {
                screen.LoadContent();
            }
        }

        /// <summary>
        /// Unload your graphics content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Tell each of the screens to unload their content.
            foreach (GameScreen screen in screens)
            {
                screen.UnloadContent();
            }
        }

        /// <summary>
        /// Allows each screen to run logic.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Make a copy of the master screen list, to avoid confusion if
            // the process of updating one screen adds or removes others.
            screensToUpdate.Clear();

            foreach (GameScreen screen in screens)
                screensToUpdate.Add(screen);

            bool otherScreenHasFocus = !Game.IsActive;
            bool coveredByOtherScreen = false;

            // Loop as long as there are screens waiting to be updated.
            while (screensToUpdate.Count > 0)
            {
                // Pop the topmost screen off the waiting list.
                GameScreen screen = screensToUpdate[screensToUpdate.Count - 1];

                screensToUpdate.RemoveAt(screensToUpdate.Count - 1);

                // Update the screen.
                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                if (screen.ScreenState == ScreenState.TransitionOn ||
                    screen.ScreenState == ScreenState.Active)
                {
                    // If this is the first active screen we came across,
                    // give it a chance to handle input.
                    if (!otherScreenHasFocus)
                    {
                        screen.HandleInput();

                        otherScreenHasFocus = true;
                    }

                    // If this is an active non-popup, inform any subsequent
                    // screens that they are covered by it.
                    if (!screen.IsPopup)
                        coveredByOtherScreen = true;
                }
            }

            // Print debug trace?
            if (traceEnabled)
                TraceScreens();
        }

        /// <summary>
        /// Prints a list of all the screens, for debugging.
        /// </summary>
        void TraceScreens()
        {
            List<string> screenNames = new List<string>();

            foreach (GameScreen screen in screens)
                screenNames.Add(screen.GetType().Name);

#if DEBUG
            Trace.WriteLine(string.Join(", ", screenNames.ToArray()));
#endif
        }


        /// <summary>
        /// Tells each screen to draw itself.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            foreach (GameScreen screen in screens)
            {
                if (screen.ScreenState == ScreenState.Hidden)
                    continue;

                screen.Draw(gameTime);
            }
        }

        /// <summary>
        /// Adds a new screen to the screen manager.
        /// </summary>
        public void AddScreen(GameScreen screen)
        {
            screen.ScreenManager = this;
            screen.IsExiting = false;

            // If we have a graphics device, tell the screen to load content.
            if (isInitialized)
            {
                screen.LoadContent();
            }

            screens.Add(screen);
            TouchPanel.EnabledGestures = screen.EnabledGestures;
        }


        /// <summary>
        /// Removes a screen from the screen manager. You should normally
        /// use GameScreen.ExitScreen instead of calling this directly, so
        /// the screen can gradually transition off rather than just being
        /// instantly removed.
        /// </summary>
        public void RemoveScreen(GameScreen screen)
        {
            // If we have a graphics device, tell the screen to unload content.
            if (isInitialized)
            {
                screen.UnloadContent();
            }

            screens.Remove(screen);
            screensToUpdate.Remove(screen);
        }


        /// <summary>
        /// Expose an array holding all the screens. We return a copy rather
        /// than the real master list, because screens should only ever be added
        /// or removed using the AddScreen and RemoveScreen methods.
        /// </summary>
        public GameScreen[] GetScreens()
        {
            return screens.ToArray();
        }

        /// <summary>
        /// Draws a translucent black fullscreen sprite. This is used for fading
        /// screens in and out, or for darkening the background behind popups.
        /// </summary>
        /// <param name="alpha">The opacity level of the fade (0 = fully transparent, 1 = fully opaque).</param>
        public void FadeBackBufferToBlack(float alpha)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, GlobalTransformation);

            spriteBatch.Draw(blankTexture,
                             new Rectangle(0, 0, Session.BACK_BUFFER_WIDTH, Session.BACK_BUFFER_HEIGHT),
                             Color.Black * alpha);

            spriteBatch.End();
        }

        /// <summary>
        /// Scales the game presentation area to match the screen's aspect ratio.
        /// </summary>
        public void ScalePresentationArea()
        {
            // Validate parameters before calculation
            if (GraphicsDevice == null || baseScreenSize.X <= 0 || baseScreenSize.Y <= 0)
            {
                throw new InvalidOperationException("Invalid graphics configuration");
            }

            // Fetch screen dimensions
            backbufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            backbufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

            // Prevent division by zero
            if (backbufferHeight == 0 || baseScreenSize.Y == 0)
            {
                return;
            }

            // Calculate aspect ratios
            float baseAspectRatio = baseScreenSize.X / baseScreenSize.Y;
            float screenAspectRatio = backbufferWidth / (float)backbufferHeight;

            // Determine uniform scaling factor
            float scalingFactor;
            float horizontalOffset = 0;
            float verticalOffset = 0;

            if (screenAspectRatio > baseAspectRatio)
            {
                // Wider screen: scale by height
                scalingFactor = backbufferHeight / baseScreenSize.Y;

                // Centre things horizontally.
                horizontalOffset = (backbufferWidth - baseScreenSize.X * scalingFactor) / 2;
            }
            else
            {
                // Taller screen: scale by width
                scalingFactor = backbufferWidth / baseScreenSize.X;

                // Centre things vertically.
                verticalOffset = (backbufferHeight - baseScreenSize.Y * scalingFactor) / 2;
            }

            // Update the transformation matrix
            globalTransformation = Matrix.CreateScale(scalingFactor) *
                                   Matrix.CreateTranslation(horizontalOffset, verticalOffset, 0);

            // Update the inputTransformation with the Inverted globalTransformation
            // TODO inputState.UpdateInputTransformation(Matrix.Invert(globalTransformation));

            // Debug info
            Debug.WriteLine($"Screen Size - Width[{backbufferWidth}] Height[{backbufferHeight}] ScalingFactor[{scalingFactor}]");
        }
    }
}