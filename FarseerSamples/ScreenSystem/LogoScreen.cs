using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FarseerPhysics.SamplesFramework
{
    public class LogoScreen : GameScreen
    {
        private const float LogoScreenHeightRatio = 4f / 6f;
        private const float LogoWidthHeightRatio = 1.4f;

        private ContentManager _content;
        private Rectangle _destination;
        private TimeSpan _duration;
        private Texture2D _farseerLogoTexture;

        public LogoScreen(TimeSpan duration)
        {
            _duration = duration;
            TransitionOffTime = TimeSpan.FromSeconds(2.0);
        }

        /// <summary>
        /// Loads graphics content for this screen. The background texture is quite
        /// big, so we use our own local ContentManager to load it. This allows us
        /// to unload before going from the menus into the game itself, wheras if we
        /// used the shared ContentManager provided by the Game class, the content
        /// would remain loaded forever.
        /// </summary>
        public override void LoadContent()
        {
            if (_content == null)
            {
                _content = new ContentManager(ScreenManager.Game.Services, "Content");
            }

            _farseerLogoTexture = _content.Load<Texture2D>("Common/logo");

            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            int rectHeight = (int)(viewport.Height * LogoScreenHeightRatio);
            int rectWidth = (int)(rectHeight * LogoWidthHeightRatio);
            int posX = viewport.Bounds.Center.X - rectWidth / 2;
            int posY = viewport.Bounds.Center.Y - rectHeight / 2;

            _destination = new Rectangle(posX, posY, rectWidth, rectHeight);
        }

        /// <summary>
        /// Unloads graphics content for this screen.
        /// </summary>
        public override void UnloadContent()
        {
            _content.Unload();
        }

        public override void HandleInput(InputHelper input, GameTime gameTime)
        {
            if (input.KeyboardState.GetPressedKeys().Length > 0 ||
                input.GamePadState.IsButtonDown(Buttons.A | Buttons.Start | Buttons.Back) ||
                input.MouseState.LeftButton == ButtonState.Pressed)
            {
                _duration = TimeSpan.Zero;
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                    bool coveredByOtherScreen)
        {
            _duration -= gameTime.ElapsedGameTime;
            if (_duration <= TimeSpan.Zero)
            {
                ExitScreen();
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(Color.White);

            ScreenManager.SpriteBatch.Begin();
            ScreenManager.SpriteBatch.Draw(_farseerLogoTexture, _destination, Color.White);
            ScreenManager.SpriteBatch.End();
        }
    }
}