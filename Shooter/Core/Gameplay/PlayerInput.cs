using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Shooter.Gameplay
{
    /// <summary>One frame of player input, in the units PlayerInputHandler returns to PlayerCharacterController.</summary>
    public struct PlayerInput
    {
        public Vector2 Move;        // x = right, y = forward, length <= 1
        public float LookX;         // turn right positive; already multiplied by sensitivity (and the 0.001 mouse scale)
        public float LookY;         // look down positive
        public bool JumpDown;
        public bool JumpHeld;       // GetJumpInputHeld: the jetpack thrusts while it is held
        public bool CrouchDown;
        public bool SprintHeld;
        public bool FireDown, FireHeld, FireReleased, AimHeld;
        public int SwitchWeapon;   // +1 next / -1 previous, 0 none
        public int SelectWeapon;   // 1..9 for the number keys, 0 none
    }

    /// <summary>
    /// PlayerInputHandler for keyboard + mouse. Bindings follow Assets/FPS/InputSystem_Actions.inputactions:
    /// Move = WASD/arrows (diagonals normalised), Look = pointer delta x0.001 (y inverted by Unity's -0.001 and
    /// screen axis), Jump = Space, Sprint = Left Shift, Crouch = C, Fire = left mouse, Aim = right mouse, weapon switch = wheel / E / Q / 1-9 (R reload is not bound: the Blaster reloads itself). IJKL look is an addition for machines without a
    /// captured mouse (arrows are Move in Unity), expressed as an equivalent of 8 px per frame.
    /// </summary>
    public class PlayerInputHandler
    {
        public const float MouseScale = 0.001f;
        private KeyboardState _prev;
        private bool _prevFire;
        private int _prevScroll;

        public float LookSensitivity = 1f;
        public bool InvertX, InvertY;

        /// <param name="mouseDelta">Mouse movement since the last frame in pixels (MonoGame axes: +y = down); zero when not captured.</param>
        public PlayerInput Read(KeyboardState keys, MouseState mouse, Point mouseDelta)
        {
            var input = new PlayerInput();
            var move = Vector2.Zero;
            if (keys.IsKeyDown(Keys.W) || keys.IsKeyDown(Keys.Up)) move.Y += 1f;
            if (keys.IsKeyDown(Keys.S) || keys.IsKeyDown(Keys.Down)) move.Y -= 1f;
            if (keys.IsKeyDown(Keys.D) || keys.IsKeyDown(Keys.Right)) move.X += 1f;
            if (keys.IsKeyDown(Keys.A) || keys.IsKeyDown(Keys.Left)) move.X -= 1f;
            if (move.LengthSquared() > 1f) move.Normalize();
            input.Move = move;

            float lx = mouseDelta.X, ly = mouseDelta.Y;
            if (keys.IsKeyDown(Keys.J)) lx -= 8f;
            if (keys.IsKeyDown(Keys.L)) lx += 8f;
            if (keys.IsKeyDown(Keys.I)) ly -= 8f;
            if (keys.IsKeyDown(Keys.K)) ly += 8f;
            lx *= MouseScale * LookSensitivity;
            ly *= MouseScale * LookSensitivity;
            input.LookX = InvertX ? -lx : lx;
            input.LookY = InvertY ? -ly : ly;

            input.JumpDown = keys.IsKeyDown(Keys.Space) && !_prev.IsKeyDown(Keys.Space);
            input.JumpHeld = keys.IsKeyDown(Keys.Space);
            input.CrouchDown = keys.IsKeyDown(Keys.C) && !_prev.IsKeyDown(Keys.C);
            input.SprintHeld = keys.IsKeyDown(Keys.LeftShift);

            bool fire = mouse.LeftButton == ButtonState.Pressed;
            input.FireHeld = fire;
            input.FireDown = fire && !_prevFire;
            input.FireReleased = !fire && _prevFire;
            _prevFire = fire;
            input.AimHeld = mouse.RightButton == ButtonState.Pressed;

            // NextWeapon: scroll up gives -1 (previous), scroll down +1; E / Q cycle as well
            int scroll = mouse.ScrollWheelValue - _prevScroll;
            _prevScroll = mouse.ScrollWheelValue;
            if (scroll > 0) input.SwitchWeapon = -1;
            else if (scroll < 0) input.SwitchWeapon = 1;
            else if (keys.IsKeyDown(Keys.E) && !_prev.IsKeyDown(Keys.E)) input.SwitchWeapon = 1;
            else if (keys.IsKeyDown(Keys.Q) && !_prev.IsKeyDown(Keys.Q)) input.SwitchWeapon = -1;
            for (int d = 0; d < 9; d++)
            {
                Keys k = Keys.D1 + d;
                if (keys.IsKeyDown(k) && !_prev.IsKeyDown(k)) { input.SelectWeapon = d + 1; break; }
            }

            _prev = keys;
            return input;
        }
    }
}
