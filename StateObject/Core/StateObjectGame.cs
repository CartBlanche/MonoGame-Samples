//-----------------------------------------------------------------------------
// Game1.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;

namespace StateObject
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class StateObjectGame : Game
    {
        GraphicsDeviceManager graphicsDeviceManager;

        BasicEffect basicEffect;

        VertexDeclaration vertexDeclaration;
        VertexBuffer vertexBuffer;
        const int number_of_vertices = 6;

        RasterizerState rsCullNone;
        RasterizerState rsCullCounterClockwise;
        RasterizerState rsCullClockwise;

        SpriteBatch spriteBatch;

        // SpriteFont and mode tracking
        SpriteFont instructionFont;

        KeyboardState currentKeyboardState = Keyboard.GetState();
        KeyboardState lastKeyboardState = Keyboard.GetState();
        GamePadState currentGamePadState = GamePad.GetState(PlayerIndex.One);
        GamePadState lastGamePadState = GamePad.GetState(PlayerIndex.One);

        bool changeState = false;

        public StateObjectGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
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
            CreateEffect();
            CreateVertexBuffer();

            rsCullNone = new RasterizerState()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.WireFrame,
                MultiSampleAntiAlias = false
            };

            rsCullCounterClockwise = new RasterizerState()
            {
                CullMode = CullMode.CullCounterClockwiseFace,
                FillMode = FillMode.WireFrame,
                MultiSampleAntiAlias = false
            };

            rsCullClockwise = new RasterizerState()
            {
                CullMode = CullMode.CullClockwiseFace,
                FillMode = FillMode.WireFrame,
                MultiSampleAntiAlias = false
            };

            GraphicsDevice.RasterizerState = rsCullNone;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load the SpriteFont
            instructionFont = Content.Load<SpriteFont>("font");

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Exit the game from a GamePad
            if (currentGamePadState.Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // Exit the game from a Keyboard
            if (currentKeyboardState.IsKeyDown(Keys.Escape))
                this.Exit();

            // Only set changeState when input is detected
            if ((currentGamePadState.Buttons.A == ButtonState.Pressed
                && currentGamePadState.Buttons.A != lastGamePadState.Buttons.A)
                || (currentKeyboardState.IsKeyDown(Keys.A)
                && lastKeyboardState.IsKeyUp(Keys.A)))
            {
                changeState = true;
            }

            if (changeState)
            {
                if (GraphicsDevice.RasterizerState == rsCullNone)
                {
                    GraphicsDevice.RasterizerState = rsCullCounterClockwise;
                }
                else if (GraphicsDevice.RasterizerState == rsCullCounterClockwise)
                {
                    GraphicsDevice.RasterizerState = rsCullClockwise;
                }
                else
                {
                    GraphicsDevice.RasterizerState = rsCullNone;
                }
                changeState = false;
            }

            lastKeyboardState = currentKeyboardState;
            lastGamePadState = currentGamePadState;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.MonoGameOrange);

            GraphicsDevice.SetVertexBuffer(vertexBuffer);

            EffectPass pass = basicEffect.CurrentTechnique.Passes[0];
            if (pass != null)
            {
                pass.Apply();

                GraphicsDevice.DrawPrimitives(
                    PrimitiveType.TriangleList, // primitive type to draw
                    0, // start vertex
                    2 // number of primitives to draw
                );
            }

            // Draw instructions and current mode
            spriteBatch.Begin();
            spriteBatch.DrawString(
                instructionFont,
                "Change Cull Modes:\n  A on the Keyboard or A on the GamePad",
                new Vector2(20, 250),
                Color.White
            );
            spriteBatch.DrawString(
                instructionFont,
                $"Cull Mode:\n  {GraphicsDevice.RasterizerState.CullMode}",
                new Vector2(20, 325),
                Color.Yellow
            );
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void CreateEffect()
        {
            basicEffect = new BasicEffect(GraphicsDevice);
        }

        private void CreateVertexBuffer()
        {
            vertexDeclaration = new VertexDeclaration(new VertexElement[1]
                {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0)
                }
            );

            vertexBuffer = new VertexBuffer(
                GraphicsDevice,
                vertexDeclaration,
                number_of_vertices,
                BufferUsage.None
                );

            Vector3[] vertices = new Vector3[number_of_vertices];
            vertices[0] = new Vector3(-1, 0, 0); // cw
            vertices[1] = new Vector3(0, 1, 0);
            vertices[2] = new Vector3(0, 0, 0);
            vertices[3] = new Vector3(0, 0, 0); // ccw
            vertices[4] = new Vector3(1, 0, 0);
            vertices[5] = new Vector3(0, 1, 0);

            vertexBuffer.SetData(vertices);

        }
    }
}