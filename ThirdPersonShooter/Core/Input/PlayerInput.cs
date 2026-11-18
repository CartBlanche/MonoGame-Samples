using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>One frame of player intent (what player_input.gd's _process/_input produce).</summary>
    public struct PlayerInput
    {
        /// <summary>player_input.gd:49-51: (right-left, back-forward), each axis analog -1..1. Forward key => Y = -1.</summary>
        public Vector2 Motion;
        /// <summary>Camera rotation to apply this frame, radians: X = yaw right (+), Y = pitch down (+). Already scaled by sensitivity.</summary>
        public Vector2 Look;
        /// <summary>Edge: jump pressed this frame (consumed by the controller).</summary>
        public bool Jump;
        /// <summary>Held states for part B (aim RMB/LT, shoot LMB/RT).</summary>
        public bool Aim, Shoot;
    }
}
