using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    /// <summary>
    /// The background screen sits behind all the other menu screens.
    /// It draws a background image that remains fixed in place regardless
    /// of whatever transitions the screens on top of it may be doing.
    /// </summary>
    public class BackgroundScreen : GameScreen
    {
        private const float LogoScreenHeightRatio = 0.25f;
        private const float LogoScreenBorderRatio = 0.0375f;
        private const float LogoWidthHeightRatio = 1.4f;

        private Texture2D _backgroundTexture;
        private Rectangle _logoDestination;
        private Texture2D _logoTexture;
        private Rectangle _viewport;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BackgroundScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            _logoTexture = ScreenManager.Content.Load<Texture2D>("Common/logo");
            _backgroundTexture = ScreenManager.Content.Load<Texture2D>("Common/gradient");

            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            Vector2 logoSize = new Vector2();
            logoSize.Y = viewport.Height * LogoScreenHeightRatio;
            logoSize.X = logoSize.Y * LogoWidthHeightRatio;

            float border = viewport.Height * LogoScreenBorderRatio;
            Vector2 logoPosition = new Vector2(viewport.Width - border - logoSize.X,
                                               viewport.Height - border - logoSize.Y);
            _logoDestination = new Rectangle((int)logoPosition.X, (int)logoPosition.Y, (int)logoSize.X,
                                             (int)logoSize.Y);
            _viewport = viewport.Bounds;
        }

        /// <summary>
        /// Updates the background screen. Unlike most screens, this should not
        /// transition off even if it has been covered by another screen: it is
        /// supposed to be covered, after all! This overload forces the
        /// coveredByOtherScreen parameter to false in order to stop the base
        /// Update method wanting to transition off.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                    bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);
        }

        /// <summary>
        /// Draws the background screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin();
            ScreenManager.SpriteBatch.Draw(_backgroundTexture, _viewport, Color.White);
            ScreenManager.SpriteBatch.Draw(_logoTexture, _logoDestination, Color.White * 0.6f);
            ScreenManager.SpriteBatch.End();
        }
    }
}