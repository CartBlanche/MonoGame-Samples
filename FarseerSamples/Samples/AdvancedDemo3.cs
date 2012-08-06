using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FarseerPhysics.SamplesFramework
{
    internal class AdvancedDemo3 : PhysicsGameScreen, IDemoScreen
    {
        private Border _border;
        private TheoJansenWalker _walker;

        #region IDemoScreen Members

        public string GetTitle()
        {
            return "Theo Jansen's walker";
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TODO: Add sample description!");
            sb.AppendLine(string.Empty);
            sb.AppendLine("GamePad:");
            sb.AppendLine("  - Switch walker direction: B button");
            sb.AppendLine("  - Exit to menu: Back button");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Keyboard:");
            sb.AppendLine("  - Switch walker direction: Space");
            sb.AppendLine("  - Exit to menu: Escape");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Mouse / Touchscreen");
            sb.AppendLine("  - Switch walker direction: Right click");
            return sb.ToString();
        }

        #endregion

        public override void LoadContent()
        {
            base.LoadContent();

            HasCursor = false;

            World.Gravity = new Vector2(0, 9.82f);

            _border = new Border(World, this, ScreenManager.GraphicsDevice.Viewport);

            _walker = new TheoJansenWalker(World, this, Vector2.Zero);
        }

        public override void HandleInput(InputHelper input, GameTime gameTime)
        {
            if (input.IsNewButtonPress(Buttons.B) ||
                input.IsNewMouseButtonPress(MouseButtons.RightButton) ||
                input.IsNewKeyPress(Keys.Space))
            {
                _walker.Reverse();
            }

            base.HandleInput(input, gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _walker.Draw();
            _border.Draw();
            base.Draw(gameTime);
        }
    }
}