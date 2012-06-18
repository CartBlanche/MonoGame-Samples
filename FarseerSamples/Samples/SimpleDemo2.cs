using System.Collections.Generic;
using System.Text;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    internal class SimpleDemo2 : PhysicsGameScreen, IDemoScreen
    {
        private Border _border;
        private Sprite _rectangleSprite;
        private Body _rectangles;
        private Vector2 _offset;

        #region IDemoScreen Members

        public string GetTitle()
        {
            return "Body with two fixtures";
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("This demo shows a single body with two attached fixtures and shapes.");
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

            Vertices rect1 = PolygonTools.CreateRectangle(2f, 2f);
            Vertices rect2 = PolygonTools.CreateRectangle(2f, 2f);

            Vector2 trans = new Vector2(-2f, 0f);
            rect1.Translate(ref trans);
            trans = new Vector2(2f, 0f);
            rect2.Translate(ref trans);

            List<Vertices> vertices = new List<Vertices>(2);
            vertices.Add(rect1);
            vertices.Add(rect2);

            _rectangles = BodyFactory.CreateCompoundPolygon(World, vertices, 1f);
            _rectangles.BodyType = BodyType.Dynamic;

            SetUserAgent(_rectangles, 200f, 200f);

            // create sprite based on rectangle fixture
            _rectangleSprite = new Sprite(ScreenManager.Assets.TextureFromVertices(rect1, MaterialType.Squares,
                                                                                   Color.Orange, 1f));
            _offset = new Vector2(ConvertUnits.ToDisplayUnits(2f), 0f);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(0, null, null, null, null, null, Camera.View);
            // draw first rectangle
            ScreenManager.SpriteBatch.Draw(_rectangleSprite.Texture,
                                           ConvertUnits.ToDisplayUnits(_rectangles.Position), null,
                                           Color.White, _rectangles.Rotation,
                                           _rectangleSprite.Origin + _offset, 1f, SpriteEffects.None, 0f);
            // draw second rectangle
            ScreenManager.SpriteBatch.Draw(_rectangleSprite.Texture,
                                           ConvertUnits.ToDisplayUnits(_rectangles.Position), null,
                                           Color.White, _rectangles.Rotation,
                                           _rectangleSprite.Origin - _offset, 1f, SpriteEffects.None, 0f);
            ScreenManager.SpriteBatch.End();
            _border.Draw();
            base.Draw(gameTime);
        }
    }
}