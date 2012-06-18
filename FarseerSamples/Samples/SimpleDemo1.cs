using System.Text;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    internal class SimpleDemo1 : PhysicsGameScreen, IDemoScreen
    {
        private Border _border;
        private Body _rectangle;
        private Sprite _rectangleSprite;

        #region IDemoScreen Members

        public string GetTitle()
        {
            return "Body with a single fixture";
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("This demo shows a single body with one attached fixture and shape.");
            sb.AppendLine("A fixture binds a shape to a body and adds material");
            sb.AppendLine("properties such as density, friction, and restitution.");
            sb.AppendLine(string.Empty);
            sb.AppendLine("GamePad:");
            sb.AppendLine("  - Rotate object: left and right triggers");
            sb.AppendLine("  - Move object: right thumbstick");
            sb.AppendLine("  - Move cursor: left thumbstick");
            sb.AppendLine("  - Grab object (beneath cursor): A button");
            sb.AppendLine("  - Drag grabbed object: left thumbstick");
            sb.AppendLine("  - Exit to menu: Back button");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Keyboard:");
            sb.AppendLine("  - Rotate Object: left and right arrows");
            sb.AppendLine("  - Move Object: A,S,D,W");
            sb.AppendLine("  - Exit to menu: Escape");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Mouse / Touchscreen");
            sb.AppendLine("  - Grab object (beneath cursor): Left click");
            sb.AppendLine("  - Drag grabbed object: move mouse / finger");
            return sb.ToString();
        }

        #endregion

        public override void LoadContent()
        {
            base.LoadContent();

            World.Gravity = Vector2.Zero;

            _border = new Border(World, this, ScreenManager.GraphicsDevice.Viewport);

            _rectangle = BodyFactory.CreateRectangle(World, 5f, 5f, 1f);
            _rectangle.BodyType = BodyType.Dynamic;

            SetUserAgent(_rectangle, 100f, 100f);

            // create sprite based on body
            _rectangleSprite = new Sprite(ScreenManager.Assets.TextureFromShape(_rectangle.FixtureList[0].Shape,
                                                                                MaterialType.Squares,
                                                                                Color.Orange, 1f));
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(0, null, null, null, null, null, Camera.View);
            ScreenManager.SpriteBatch.Draw(_rectangleSprite.Texture, ConvertUnits.ToDisplayUnits(_rectangle.Position),
                                           null,
                                           Color.White, _rectangle.Rotation, _rectangleSprite.Origin, 1f,
                                           SpriteEffects.None, 0f);
            ScreenManager.SpriteBatch.End();
            _border.Draw();
            base.Draw(gameTime);
        }
    }
}