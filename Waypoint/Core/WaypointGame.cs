//-----------------------------------------------------------------------------
// WaypointSample.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------


using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
//using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;


namespace Waypoint
{
    /// <summary>
    /// This sample shows how an AI can navigate from point to point using 
    /// several different behaviors
    /// </summary>
    public class WaypointGame : Game
    {

        /// <summary>
        /// Screen width in pixels
        /// </summary>
        const int screenWidth = 640;
        /// <summary>
        /// Screen height in pixels
        /// </summary>
        const int screenHeight = 480;

        /// <summary>
        /// Cursor move speed in pixels per second
        /// </summary>
        const float cursorMoveSpeed = 250.0f;



        // Graphics data
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        
        // Cursor data
        Texture2D cursorTexture;
        Vector2 cursorCenter;
        Vector2 cursorLocation;

        // HUD data
        SpriteFont hudFont;
        // Where the HUD draws on the screen
        Vector2 hudLocation;

        // Input data
        KeyboardState previousKeyboardState;
        GamePadState previousGamePadState;
        MouseState previousMouseState;
        KeyboardState currentKeyboardState;
        GamePadState currentGamePadState;
        MouseState currentMouseState;
        TouchCollection currentTouchCollection;

        // The waypoint-following tank
        Tank tank;



        /// <summary>
        /// Construct a WaypointSample object
        /// </summary>
        public WaypointGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;

            // Make the mouse cursor visible (for Desktop platforms)
            this.IsMouseVisible = true;

            tank = new Tank(this);
            Components.Add(tank);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting
        /// to run. This is where it can query for any required services and load and
        /// non-graphic related content.  Calling base.Initialize will enumerate 
        /// through any components and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {

            // This places the HUD near the upper left corner of the screen
            hudLocation = new Vector2(
                (float)Math.Floor(screenWidth * .01f), 
                (float)Math.Floor(screenHeight * .01f));

            // places the cursor in the center of the screen
            cursorLocation = 
                new Vector2((float)screenWidth / 2, (float)screenHeight / 2);
            
            // places the tank halfway between the center of the screen and the
            // upper left corner
            tank.Reset(
                new Vector2((float)screenWidth / 4, (float)screenHeight / 4));

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

            cursorTexture = Content.Load<Texture2D>("cursor");
            cursorCenter = 
                new Vector2(cursorTexture.Width / 2, cursorTexture.Height / 2);

            hudFont = Content.Load<SpriteFont>("HUDFont");
        }



        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleInput(elapsedTime);
            
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.MonoGameOrange);

            base.Draw(gameTime);

            string HudString = "Behavior : " + tank.BehaviorType.ToString();

            spriteBatch.Begin();

            // Draw the cursor
            spriteBatch.Draw(cursorTexture, cursorLocation, null, Color.White, 0f,
                cursorCenter, 1f, SpriteEffects.None, 0f);

            // Draw the string for current behavior
            spriteBatch.DrawString(hudFont, HudString, hudLocation, Color.White);

            // Draw the string for current behavior
            spriteBatch.DrawString(hudFont, "Press B to change behavior", hudLocation + new Vector2(0, 25), Color.White);
            spriteBatch.DrawString(hudFont, "Press A to add a waypoint", hudLocation + new Vector2(0, 50), Color.White);
            spriteBatch.DrawString(hudFont, "Press X to reset the tank", hudLocation + new Vector2(0, 75), Color.White);
            spriteBatch.DrawString(hudFont, "Use the left thumbstick or\n  arrow keys to move the cursor", hudLocation + new Vector2(0, 100), Color.White);
            spriteBatch.End();
        }



        /// <summary>
        /// Read keyboard and gamepad input
        /// </summary>
        private void HandleInput(float elapsedTime)
        {
            previousGamePadState = currentGamePadState;
            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;
            currentGamePadState = GamePad.GetState(PlayerIndex.One);
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            
            currentTouchCollection = TouchPanel.GetState();
            
            //bool touched = false;
            int touchCount = 0;
                
            // tap the screen to select				
            foreach (TouchLocation location in currentTouchCollection)
            {
                switch (location.State)
                {
                    case TouchLocationState.Pressed:	
                        //touched = true;	
                        touchCount++;
                        cursorLocation = location.Position;
                        break;
                    case TouchLocationState.Moved:
                        break;
                    case TouchLocationState.Released:
                        break;
                }	
            }

            if ( currentMouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed)
            {
                cursorLocation.X = currentMouseState.X;
                cursorLocation.Y = currentMouseState.Y;
                touchCount = 1;
            }

            if (currentMouseState.MiddleButton == ButtonState.Released && previousMouseState.MiddleButton == ButtonState.Pressed) {
                touchCount = 2;
            }

            if (currentMouseState.RightButton == ButtonState.Released && previousMouseState.RightButton == ButtonState.Pressed) {
                touchCount = 3;
            }

            // Allows the game to exit
            if (currentGamePadState.Buttons.Back == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.Escape))
#if !___IOS___
                this.Exit();
#endif

            // Update the cursor location by listening for left thumbstick input on
            // the GamePad and direction key input on the Keyboard, making sure to
            // keep the cursor inside the screen boundary
            cursorLocation.X += 
                currentGamePadState.ThumbSticks.Left.X * cursorMoveSpeed * elapsedTime;
            cursorLocation.Y -= 
                currentGamePadState.ThumbSticks.Left.Y * cursorMoveSpeed * elapsedTime;

            if (currentKeyboardState.IsKeyDown(Keys.Up))
            {
                cursorLocation.Y -= elapsedTime * cursorMoveSpeed;
            }
            if (currentKeyboardState.IsKeyDown(Keys.Down))
            {
                cursorLocation.Y += elapsedTime * cursorMoveSpeed;
            }
            if (currentKeyboardState.IsKeyDown(Keys.Left))
            {
                cursorLocation.X -= elapsedTime * cursorMoveSpeed;
            }
            if (currentKeyboardState.IsKeyDown(Keys.Right))
            {
                cursorLocation.X += elapsedTime * cursorMoveSpeed;
            }
            cursorLocation.X = MathHelper.Clamp(cursorLocation.X, 0f, screenWidth);
            cursorLocation.Y = MathHelper.Clamp(cursorLocation.Y, 0f, screenHeight);

            // Change the tank move behavior if the user pressed B on
            // the GamePad or on the Keyboard.
            if ((previousGamePadState.Buttons.B == ButtonState.Released &&
                currentGamePadState.Buttons.B == ButtonState.Pressed) ||
                (previousKeyboardState.IsKeyUp(Keys.B) &&
                currentKeyboardState.IsKeyDown(Keys.B)) || ( touchCount == 2 ))
            {
                tank.CycleBehaviorType();
            }

            // Add the cursor's location to the WaypointList if the user pressed A on
            // the GamePad or on the Keyboard.
            if ((previousGamePadState.Buttons.A == ButtonState.Released &&
                currentGamePadState.Buttons.A == ButtonState.Pressed) ||
                (previousKeyboardState.IsKeyUp(Keys.A) &&
                currentKeyboardState.IsKeyDown(Keys.A)) || ( touchCount == 1 ))
            {
                    tank.Waypoints.Enqueue(cursorLocation);
            }

            // Delete all the current waypoints and reset the tanks� location if 
            // the user pressed X on the GamePad or on the Keyboard.
            if ((previousGamePadState.Buttons.X == ButtonState.Released &&
                currentGamePadState.Buttons.X == ButtonState.Pressed) ||
                (previousKeyboardState.IsKeyUp(Keys.X) &&
                currentKeyboardState.IsKeyDown(Keys.X)) || ( touchCount == 3 ))
            {
                tank.Reset(
                    new Vector2((float)screenWidth / 4, (float)screenHeight / 4));
            }
        }

    }
}
