using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteFontSample.Core
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpriteFontGame : Game
    {
        private const string MONOGAME_FOUNDATION = "MonoGame Foundation";
        private const float ROTATION_SPEED = 0.03f; // Smaller value for slower rotation
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        float rotation; // Rotation angle in radians
        float rotationDirection = 1f; // 1 for clockwise, -1 for counter-clockwise
        Vector2 textSize;

        public SpriteFontGame(GraphicsDeviceManager graphicsManager = null)
        {
            graphics = graphicsManager ?? new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 480;
            graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("SpriteFont1");
            textSize = font.MeasureString(MONOGAME_FOUNDATION);
            textSize = new Vector2(textSize.X / 2, textSize.Y / 2);
        }

        protected override void Update(GameTime gameTime)
        {
            // Allow ESC to exit on platforms that support it
#if !__IOS__
            if (Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                Exit();
#endif
            rotation += rotationDirection * ROTATION_SPEED;
            if (rotation >= MathHelper.TwoPi)
            {
                rotation = MathHelper.TwoPi;
                rotationDirection = -1f;
            }
            else if (rotation <= 0f)
            {
                rotation = 0f;
                rotationDirection = 1f;
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.MonoGameOrange);
            spriteBatch.Begin();

            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(101, 95), Color.Black);
            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(101, 97), Color.Black);
            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(99, 95), Color.Black);
            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(99, 97), Color.Black);
            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(100, 96), Color.White);

            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(100, 100), Color.Yellow, MathHelper.PiOver2, Vector2.Zero, 1.0f, SpriteEffects.None, 1);
            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(100, 100), Color.Yellow, MathHelper.PiOver4, Vector2.Zero, 1.0f, SpriteEffects.None, 1);
            
            spriteBatch.DrawString(font, MONOGAME_FOUNDATION, new Vector2(160, 360), Color.Black, rotation, textSize, 1.0f, SpriteEffects.None, 1);
            
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
