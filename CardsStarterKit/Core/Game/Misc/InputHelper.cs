//-----------------------------------------------------------------------------
// InputHelper.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blackjack
{
    /// <summary>
    /// Used to simulate a cursor on the Xbox.
    /// </summary>
    public class InputHelper : DrawableGameComponent
    {
        //public static Game Game;
        //static InputHelper instance;

        public bool IsEscape;
        public bool IsPressed;

        Vector2 drawPosition;
        private ScreenManager screenManager;
        Texture2D texture;
        SpriteBatch spriteBatch;
        float maxVelocity;


        public InputHelper(ScreenManager screenManager)
            : base(screenManager.Game)
        {
            this.screenManager = screenManager;
            texture = screenManager.Game.Content.Load<Texture2D>(Path.Combine("Images", "GamePadCursor"));
            spriteBatch = screenManager.SpriteBatch;
            maxVelocity = (float)(ScreenManager.BACK_BUFFER_WIDTH +
                                  ScreenManager.BACK_BUFFER_HEIGHT) / 3000f;

            drawPosition = new Vector2(ScreenManager.BACK_BUFFER_WIDTH / 2,
                ScreenManager.BACK_BUFFER_HEIGHT / 2);
        }

        //public static InputHelper Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //            instance = new InputHelper();
        //        return instance;
        //    }
        //}

        public Vector2 PointPosition
        {
            get
            {
                return drawPosition + new Vector2(texture.Width / 2f,
                    texture.Height / 2f);
            }
        }

        /// <summary>
        /// Updates itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

            IsPressed = gamePadState.Buttons.A == ButtonState.Pressed;

            IsEscape = gamePadState.Buttons.Back == ButtonState.Pressed;

            drawPosition += gamePadState.ThumbSticks.Left
                * new Vector2(1, -1)
                * gameTime.ElapsedGameTime.Milliseconds
                * maxVelocity;
            drawPosition = Vector2.Clamp(drawPosition, Vector2.Zero,
                new Vector2(ScreenManager.BACK_BUFFER_WIDTH, ScreenManager.BACK_BUFFER_HEIGHT)
                - new Vector2(texture.Bounds.Width, texture.Bounds.Height));
        }

        /// <summary>
        /// Draws cursor.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, screenManager.GlobalTransformation);
            spriteBatch.Draw(texture, drawPosition, null, Color.White, 0, Vector2.Zero, 1,
                SpriteEffects.None, 0);
            spriteBatch.End();
        }

    }
}