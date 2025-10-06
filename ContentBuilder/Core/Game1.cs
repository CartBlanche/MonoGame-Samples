using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SamplesContentBuilder
{
    /// <summary>
    /// This is a sample MonoGame project demonstrating content building and loading.
    /// It showcases how to build and load various types of content assets.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _backgroundTexture;
        private SpriteFont _font;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Try to load sample content if available
            try
            {
                _backgroundTexture = Content.Load<Texture2D>("Textures/background");
            }
            catch
            {
                // Create a simple colored texture if background is not available
                _backgroundTexture = new Texture2D(GraphicsDevice, 1, 1);
                _backgroundTexture.SetData(new[] { Color.CornflowerBlue });
            }

            try
            {
                _font = Content.Load<SpriteFont>("Fonts/Font");
            }
            catch
            {
                // Font will be null if not available
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.MonoGameOrange);

            _spriteBatch.Begin();

            // Draw background if available
            if (_backgroundTexture != null)
            {
                _spriteBatch.Draw(_backgroundTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);
            }

            // Draw text if font is available
            if (_font != null)
            {
                _spriteBatch.DrawString(_font, "MonoGame Samples Content Builder", new Vector2(10, 10), Color.White);
                _spriteBatch.DrawString(_font, "This project demonstrates content building with MonoGame 3.8.*", new Vector2(10, 40), Color.White);
                _spriteBatch.DrawString(_font, "Press Escape to exit", new Vector2(10, 70), Color.White);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
