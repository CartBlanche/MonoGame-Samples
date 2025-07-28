using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.Devices.Sensors;

namespace VirtualGamePad
{
    public class VirtualGamePadGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D gamepadTexture, caracter;
        Vector2 position = new Vector2();
        Color caracterColor = Color.White;
        SpriteFont font;

        public VirtualGamePadGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            gamepadTexture = Content.Load<Texture2D>("gamepad");
            caracter = Content.Load<Texture2D>("monogameicon");
            font = Content.Load<SpriteFont>("font");
            
            // Set the virtual GamePad
			/* TODO ButtonDefinition BButton = new ButtonDefinition();
			BButton.Texture = texture;
			BButton.Position = new Vector2(200,150);
			BButton.Type = Buttons.B;
			BButton.TextureRect = new Rectangle(72,77,36,36);
			
			ButtonDefinition AButton = new ButtonDefinition();
			AButton.Texture = texture;
			AButton.Position = new Vector2(150,150);
			AButton.Type = Buttons.A;
			AButton.TextureRect = new Rectangle(73,114,36,36);
			
			GamePad.ButtonsDefinitions.Add(BButton);
			GamePad.ButtonsDefinitions.Add(AButton);
			
			ThumbStickDefinition thumbStick = new ThumbStickDefinition();
			thumbStick.Position = new Vector2(200,200);
			thumbStick.Texture = texture;
			thumbStick.TextureRect = new Rectangle(2,2,68,68);
			
			GamePad.LeftThumbStickDefinition = thumbStick;*/
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed)
                caracterColor = Color.Green;

            if (GamePad.GetState(PlayerIndex.One).Buttons.B == ButtonState.Pressed)
                caracterColor = Color.Red;

            GamePadState gamepadStatus = GamePad.GetState(PlayerIndex.One);
            position.Y += (int)(gamepadStatus.ThumbSticks.Left.Y * -4);
            position.X += (int)(gamepadStatus.ThumbSticks.Left.X * 4);

            //  right
            if (position.X + caracter.Width > Window.ClientBounds.Width)
            {
                position.X = Window.ClientBounds.Width - caracter.Width;
            }

            //  bottom
            if (position.Y + caracter.Height > Window.ClientBounds.Height)
            {
                position.Y = Window.ClientBounds.Height - caracter.Height;
            }

            //  left
            if (position.X < 0)
            {
                position.X = 0;
            }

            //  top
            if (position.Y < 0)
            {
                position.Y = 0;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.MonoGameOrange);

            spriteBatch.Begin();
			spriteBatch.Draw(caracter, position, caracterColor);
			spriteBatch.DrawString(font, GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.ToString(),Vector2.One,Color.Black);
			//spriteBatch.DrawString(font,Accelerometer.GetState().Acceleration.ToString(),new Vector2(1,40),Color.Black);
			
			// Draw the virtual GamePad
			// TODO GamePad.Draw(gameTime,spriteBatch);
			
			spriteBatch.End();
            
            base.Draw(gameTime);
        }
    }
}
