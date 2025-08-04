using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace InputSample
{
    public class InputGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont font;
        Color clearColor = Color.CornflowerBlue;

        KeyboardState currentKeyboardState;
        GamePadState currentGamePadState;
        TouchCollection currentTouchState;
        MouseState currentMouseState;
        MouseState previousMouseState;

        TimeSpan lastClickTime;

        public InputGame()
        {
            graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            graphics.PreferMultiSampling = true;

            // Only use fullscreen on mobile platforms
#if MOBILE
			graphics.IsFullScreen = true;
#endif
            graphics.PreferredBackBufferHeight = 640;

            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight
                | DisplayOrientation.Portrait;

            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.DoubleTap;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            font = Content.Load<SpriteFont>("spriteFont1");

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            currentGamePadState = GamePad.GetState(PlayerIndex.One);
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            currentTouchState = TouchPanel.GetState();

            // Handle ESC key to exit (for desktop platforms)
            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
            // Handle gamepad back button (for mobile/console platforms)
            currentGamePadState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            // Replicate TouchPanel tap/double-tap with mouse for non-touch platforms
            // Detect left mouse button click (tap)
            if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                clearColor = clearColor == Color.MonoGameOrange ? Color.Black : Color.MonoGameOrange;
            }
            // Detect double-click (double-tap)
            if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                // Simple double-click detection: check if click happened within 300ms
                if ((gameTime.TotalGameTime - lastClickTime).TotalMilliseconds < 300)
                {
                    clearColor = clearColor == Color.MonoGameOrange ? Color.Red : Color.MonoGameOrange;
                }
                lastClickTime = gameTime.TotalGameTime;
            }

            while (TouchPanel.IsGestureAvailable)
            {
                var gesture = TouchPanel.ReadGesture();
                switch (gesture.GestureType)
                {
                    case GestureType.DoubleTap:
                        clearColor = clearColor == Color.MonoGameOrange ? Color.Red : Color.MonoGameOrange;
                        break;
                    case GestureType.Tap:
                        clearColor = clearColor == Color.MonoGameOrange ? Color.Black : Color.MonoGameOrange;
                        break;
                }
            }

            previousMouseState = currentMouseState;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(clearColor);

            // Won't be visible until we hide the movie
            spriteBatch.Begin();

            Vector2 center = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            Vector2 s1 = font.MeasureString("Touch the Screen");
            center.X -= (s1.X / 2);
            spriteBatch.DrawString(font, "Touch the Screen", center, Color.Red);
            center.Y += 20;
            spriteBatch.DrawString(font, GraphicsDevice.Viewport.Width.ToString() + " x " + GraphicsDevice.Viewport.Height.ToString(), center, Color.Red);
            center.Y += 20;
            spriteBatch.DrawString(font, GraphicsDevice.PresentationParameters.DisplayOrientation.ToString(), center, Color.Red);


            spriteBatch.DrawString(font, "0,0", new Vector2(0, 0), Color.Red);
            string s = GraphicsDevice.Viewport.Width.ToString() + ",0";
            Vector2 v = font.MeasureString(s);
            spriteBatch.DrawString(font, s, new Vector2(GraphicsDevice.Viewport.Width - v.X, 0), Color.Red);
            s = "0," + GraphicsDevice.Viewport.Height.ToString();
            v = font.MeasureString(s);
            spriteBatch.DrawString(font, s, new Vector2(0, GraphicsDevice.Viewport.Height - v.Y), Color.Red);
            string wandh = GraphicsDevice.Viewport.Width.ToString() + ", " + GraphicsDevice.Viewport.Height.ToString();
            Vector2 wh = font.MeasureString(wandh);
            spriteBatch.DrawString(font, wandh, new Vector2(GraphicsDevice.Viewport.Width - wh.X, GraphicsDevice.Viewport.Height - wh.Y), Color.Red);


            if (currentTouchState.Count > 0)
            {
                for (int i = 0; i < currentTouchState.Count; i++)
                {
                    center.Y += s1.Y;
                    Vector2 p = currentTouchState[i].Position;
                    spriteBatch.DrawString(font, "+", p, Color.Red);
                    spriteBatch.DrawString(font, p.ToString(), center, Color.Red);
                }
            }

            spriteBatch.End();
        }
    }
}