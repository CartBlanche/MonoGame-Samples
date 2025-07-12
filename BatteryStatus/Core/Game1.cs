using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BatteryStatusDemo
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
#if __MOBILE__ 
            graphics.IsFullScreen = true;
#endif
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("SpriteFont1");
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            spriteBatch.DrawString(font, "[Battery Status]\n" + PowerStatus.BatteryChargeStatus, new Vector2(10, 100), Color.Black);
            spriteBatch.DrawString(font, "[PowerLine Status]\n" + PowerStatus.PowerLineStatus, new Vector2(10, 200), Color.Black);
            spriteBatch.DrawString(font, "[Charge]\n" + PowerStatus.BatteryLifePercent + "%", new Vector2(10, 300), Color.Black);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
