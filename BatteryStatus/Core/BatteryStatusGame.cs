using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BatteryStatus
{
    public class BatteryStatusGame : Game, IDisposable
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch = null!;
        SpriteFont font = null!;
        private readonly IPowerStatus powerStatus;

        public BatteryStatusGame(IPowerStatus powerStatus)
        {
            this.powerStatus = powerStatus ?? throw new ArgumentNullException(nameof(powerStatus));

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
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

            // Exit the game if the back button is pressed or Escape key is pressed
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.MonoGameOrange);
            spriteBatch.Begin();
            spriteBatch.DrawString(font, "[Battery Status]\n" + powerStatus.BatteryChargeStatus, new Vector2(10, 100), Color.Black);
            spriteBatch.DrawString(font, "[PowerLine Status]\n" + powerStatus.PowerLineStatus, new Vector2(10, 200), Color.Black);
            spriteBatch.DrawString(font, "[Charge]\n" + powerStatus.BatteryLifePercent + "%", new Vector2(10, 300), Color.Black);
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
