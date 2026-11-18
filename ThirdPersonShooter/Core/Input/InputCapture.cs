using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Mouse capture (Godot MOUSE_MODE_CAPTURED, player_input.gd:41): hide the cursor and recentre it every frame, reading the
    /// delta from the centre. Set(false) on pause/menu/focus loss (plan 05 calls this).
    /// </summary>
    public static class InputCapture
    {
        public static bool Captured { get; private set; }
        static Game _game;
        static bool _fresh;

        public static void Attach(Game game) { _game = game; }

        public static void Set(bool captured)
        {
            Captured = captured;
            _fresh = true; // swallow the first delta after (re)capturing: the cursor jumps to the centre
            if (_game != null) _game.IsMouseVisible = !captured;
        }

        /// <summary>Mouse movement since last call in pixels; zero when not captured or the window is not active.</summary>
        public static Vector2 ReadDelta()
        {
            if (!Captured || _game == null || !_game.IsActive) { _fresh = true; return Vector2.Zero; }
            var b = _game.Window.ClientBounds;
            var c = new Point(b.Width / 2, b.Height / 2);
            var m = Mouse.GetState();
            Vector2 d = new Vector2(m.X - c.X, m.Y - c.Y);
            Mouse.SetPosition(c.X, c.Y);
            if (_fresh) { _fresh = false; return Vector2.Zero; }
            return d;
        }
    }
}
