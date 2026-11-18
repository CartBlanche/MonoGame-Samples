using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Input map from tps-demo-master/project.godot:96-170: move_* = A/D/W/S + arrows + d-pad + left stick (axes 0/1),
    /// view_* = right stick (axes 2/3), jump = Space / pad A (button 0), aim = RMB / LT (axis 4), shoot = LMB / RT (axis 5).
    /// Mouse look = MouseLookSpeed rad/pixel (player_input.gd:5,113); stick look = 3 rad/s * dt (player_input.gd:4,56).
    /// </summary>
    public sealed class InputMap
    {
        KeyboardState _prevKeys; GamePadState _prevPad;

        /// <param name="aiming">player_input.gd:55-57,111-112: look speed x0.5 (stick) / x0.75 (mouse) while aiming.</param>
        public PlayerInput Read(float dt, bool aiming = false)
        {
            var k = Keyboard.GetState(); var pad = GamePad.GetState(PlayerIndex.One);
            var pi = new PlayerInput();
            float right = (k.IsKeyDown(Keys.D) || k.IsKeyDown(Keys.Right) ? 1f : 0f) - (k.IsKeyDown(Keys.A) || k.IsKeyDown(Keys.Left) ? 1f : 0f);
            float back = (k.IsKeyDown(Keys.S) || k.IsKeyDown(Keys.Down) ? 1f : 0f) - (k.IsKeyDown(Keys.W) || k.IsKeyDown(Keys.Up) ? 1f : 0f);
            if (pad.IsConnected)
            {
                right += Dead(pad.ThumbSticks.Left.X) + (pad.DPad.Right == ButtonState.Pressed ? 1f : 0f) - (pad.DPad.Left == ButtonState.Pressed ? 1f : 0f);
                back += -Dead(pad.ThumbSticks.Left.Y) + (pad.DPad.Down == ButtonState.Pressed ? 1f : 0f) - (pad.DPad.Up == ButtonState.Pressed ? 1f : 0f); // XNA stick Y is +up
            }
            pi.Motion = new Vector2(MathHelper.Clamp(right, -1, 1), MathHelper.Clamp(back, -1, 1));

            Vector2 mouse = InputCapture.ReadDelta();
            pi.Look = mouse * (PlayerConstants.MouseLookSpeed * GameSettings.Current.MouseSensitivity * (aiming ? PlayerConstants.AimLookScaleMouse : 1f));
            if (pad.IsConnected)
                pi.Look += new Vector2(Dead(pad.ThumbSticks.Right.X), -Dead(pad.ThumbSticks.Right.Y)) * (PlayerConstants.StickLookSpeed * dt * (aiming ? PlayerConstants.AimLookScaleStick : 1f));

            pi.Jump = (k.IsKeyDown(Keys.Space) && !_prevKeys.IsKeyDown(Keys.Space)) || (pad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A != ButtonState.Pressed);
            var ms = Mouse.GetState();
            bool captured = InputCapture.Captured;
            pi.Aim = (captured && ms.RightButton == ButtonState.Pressed) || pad.Triggers.Left > 0.5f;
            pi.Shoot = (captured && ms.LeftButton == ButtonState.Pressed) || pad.Triggers.Right > 0.5f;
            _prevKeys = k; _prevPad = pad;
            return pi;
        }

        static float Dead(float v) => Math.Abs(v) < PlayerConstants.StickDeadzone ? 0f : v;
    }
}
