using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Microsoft.Xna.Samples.MultiTouch
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
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
            // Enable multi-touch
            TouchPanel.EnabledGestures = GestureType.None;
            
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>

        private Texture2D Brush;

        private TouchCollection touchStateCollection;
        private bool Cls = true;
        private List<Color> drawColors = new List<Color>();
        private Dictionary<int, Color> LineColors = new Dictionary<int, Color>();

        private int ShakeTime = 0;

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load in a single pixel to use as the brush
            Brush = Content.Load<Texture2D>("sqbrush");

            // Set the random colors for multi touch painting
            drawColors.Add(Color.Orange);
            drawColors.Add(Color.Yellow);
            drawColors.Add(Color.Green);
            drawColors.Add(Color.Cyan);
            drawColors.Add(Color.HotPink);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private Vector2? prevMousePos = null;
        private bool prevMouseDown = false;

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            // Simple shake detection using gamepad or keyboard
            ShakeTime += (int)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (ShakeTime >= 500)
            {
                // Clear screen on Space key press or gamepad button press
                if (Keyboard.GetState().IsKeyDown(Keys.Space) ||
                    GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed)
                {
                    Cls = true;
                }
                ShakeTime = 0;
            }

            // Update touch panel state
            touchStateCollection = TouchPanel.GetState();

            // Mouse input tracking
            var mouseState = Mouse.GetState();
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            if (!mouseDown)
            {
                prevMousePos = null;
            }
            prevMouseDown = mouseDown;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if (Cls)
            {
                Cls = false;
                graphics.GraphicsDevice.Clear(Color.Black);
            }

            spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend);
            // Touch drawing
            foreach (TouchLocation t in touchStateCollection)
            {
                TouchLocation PrevLocation = new TouchLocation();
                if (t.TryGetPreviousLocation(out PrevLocation))
                {
                    if (!LineColors.ContainsKey(t.Id))
                    {
                        if (touchStateCollection.Count > 1)
                        {
                            Random randomizer = new Random();
                            LineColors[t.Id] = drawColors[randomizer.Next(0, 4)];
                        }
                        else
                        {
                            LineColors[t.Id] = Color.White;
                        }
                    }
                    spriteBatch.Draw(Brush, PrevLocation.Position, null, 
                        LineColors[t.Id], (float)Math.Atan2((double)(t.Position.Y - PrevLocation.Position.Y), (double)(t.Position.X - PrevLocation.Position.X)), Vector2.Zero, 
                        new Vector2(Vector2.Distance(PrevLocation.Position, t.Position), 1f), SpriteEffects.None, 0f);
                }
            }
            // Mouse drawing (parity with touch)
            var mouseState = Mouse.GetState();
            bool mouseDown = mouseState.LeftButton == ButtonState.Pressed;
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
            if (mouseDown)
            {
                if (prevMousePos.HasValue)
                {
                    // Use same color logic as touch
                    Color mouseColor = Color.White;
                    if (touchStateCollection.Count > 1)
                    {
                        Random randomizer = new Random();
                        mouseColor = drawColors[randomizer.Next(0, 4)];
                    }
                    spriteBatch.Draw(Brush, prevMousePos.Value, null,
                        mouseColor, (float)Math.Atan2(mousePos.Y - prevMousePos.Value.Y, mousePos.X - prevMousePos.Value.X), Vector2.Zero,
                        new Vector2(Vector2.Distance(prevMousePos.Value, mousePos), 1f), SpriteEffects.None, 0f);
                }
                prevMousePos = mousePos;
            }
            else
            {
                prevMousePos = null;
            }
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
