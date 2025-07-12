//----------------------------------------------------------------------------- 
// AimingGame.cs (moved to Core)
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;

namespace Aiming
{
    /// <summary>
    /// This sample shows how to aim one object towards another. In this sample, a
    /// spotlight turns to aim towards a cat that the player controls.
    /// </summary>
    public class AimingGame : Game
    {
        // how fast can the cat move?  this is in terms of pixels per frame.
        const float CatSpeed = 10.0f;
        // how fast can the spot light turn? this is in terms of radians per frame.
        const float SpotlightTurnSpeed = 0.025f;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D spotlightTexture;
        Vector2 spotlightPosition = new Vector2();
        Vector2 spotlightOrigin = new Vector2();
        float spotlightAngle = 0.0f;
        Texture2D catTexture;
        Vector2 catPosition = new Vector2();
        Vector2 catOrigin = new Vector2();
        public AimingGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 480;
            graphics.PreferredBackBufferHeight = 640;
#if MOBILE
            graphics.IsFullScreen = true;
#endif
        }
        protected override void Initialize()
        {
            base.Initialize();
            Viewport vp = graphics.GraphicsDevice.Viewport;
            spotlightPosition.X = vp.X + vp.Width / 2;
            spotlightPosition.Y = vp.Y + vp.Height / 2;
            catPosition.X = vp.X + vp.Width / 4;
            catPosition.Y = vp.Y + vp.Height / 2;
        }
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spotlightTexture = Content.Load<Texture2D>("spotlight");
            spotlightOrigin.X = spotlightTexture.Width / 2;
            spotlightOrigin.Y = spotlightTexture.Height / 2;
            catTexture = Content.Load<Texture2D>("cat");
            catOrigin.X = catTexture.Width / 2;
            catOrigin.Y = catTexture.Height / 2;
        }
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            // Move the cat with arrow keys or touch
            Vector2 move = Vector2.Zero;
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Left)) move.X -= 1;
            if (keyState.IsKeyDown(Keys.Right)) move.X += 1;
            if (keyState.IsKeyDown(Keys.Up)) move.Y -= 1;
            if (keyState.IsKeyDown(Keys.Down)) move.Y += 1;
            if (move.Length() > 1)
                move.Normalize();
            catPosition += move * CatSpeed;
            // Touch input (for mobile)
            TouchCollection touches = TouchPanel.GetState();
            if (touches.Count > 0)
            {
                Vector2 touch = touches[0].Position;
                Vector2 diff = touch - catPosition;
                if (diff.Length() > CatSpeed)
                {
                    diff.Normalize();
                    catPosition += diff * CatSpeed;
                }
                else
                {
                    catPosition = touch;
                }
            }
            // Aim the spotlight at the cat
            Vector2 toCat = catPosition - spotlightPosition;
            float desiredAngle = (float)Math.Atan2(toCat.Y, toCat.X);
            float delta = MathHelper.WrapAngle(desiredAngle - spotlightAngle);
            if (Math.Abs(delta) < SpotlightTurnSpeed)
                spotlightAngle = desiredAngle;
            else
                spotlightAngle += Math.Sign(delta) * SpotlightTurnSpeed;
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            spriteBatch.Draw(spotlightTexture, spotlightPosition, null, Color.White, spotlightAngle, spotlightOrigin, 1.0f, SpriteEffects.None, 0f);
            spriteBatch.Draw(catTexture, catPosition, null, Color.White, 0f, catOrigin, 1.0f, SpriteEffects.None, 0f);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
