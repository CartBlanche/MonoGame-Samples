using System.Text;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.SamplesFramework
{
    public class SimpleDemo6 : PhysicsGameScreen, IDemoScreen
    {
        private Agent _agent;
        private Border _border;
        private Spider[] _spiders;

        #region IDemoScreen Members

        public string GetTitle()
        {
            return "Dynamic Angle Joints";
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("This demo demonstrates the use of revolute joints combined");
            sb.AppendLine("with angle joints that have a dynamic target angle.");
            sb.AppendLine(string.Empty);
            sb.AppendLine("GamePad:");
            sb.AppendLine("  - Rotate agent: left and right triggers");
            sb.AppendLine("  - Move agent: right thumbstick");
            sb.AppendLine("  - Move cursor: left thumbstick");
            sb.AppendLine("  - Grab object (beneath cursor): A button");
            sb.AppendLine("  - Drag grabbed object: left thumbstick");
            sb.AppendLine("  - Exit to menu: Back button");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Keyboard:");
            sb.AppendLine("  - Rotate agent: left and right arrows");
            sb.AppendLine("  - Move agent: A,S,D,W");
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

            World.Gravity = new Vector2(0f, 20f);

            _border = new Border(World, this, ScreenManager.GraphicsDevice.Viewport);

            _agent = new Agent(World, this, new Vector2(0f, 10f));
            _spiders = new Spider[8];

            for (int i = 0; i < _spiders.Length; i++)
            {
                _spiders[i] = new Spider(World, this, new Vector2(0f, 8f - (i + 1) * 2f));
            }

            SetUserAgent(_agent.Body, 1000f, 400f);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (IsActive)
            {
                for (int i = 0; i < _spiders.Length; i++)
                {
                    _spiders[i].Update(gameTime);
                }
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(0, null, null, null, null, null, Camera.View);
            _agent.Draw();
            for (int i = 0; i < _spiders.Length; i++)
            {
                _spiders[i].Draw();
            }
            ScreenManager.SpriteBatch.End();
            _border.Draw();
            base.Draw(gameTime);
        }
    }
}