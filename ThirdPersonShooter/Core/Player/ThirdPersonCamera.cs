using System;
using Microsoft.Xna.Framework;
using ThirdPersonShooter.Physics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Camera rig of player.tscn:621-632: CameraBase (yaw, pivot 1.6 m above the feet) -> CameraRot (pitch) -> SpringArm3D (2.4 m, 180 deg yaw + 20 deg pitch,
    /// ray vs layer 1 with 0.01 margin) -> Camera3D (FOV 75, near 0.05, far 300). Mouse/stick rotation from player_input.gd:116-120.
    /// The port uses XNA's Forward (-Z) as the view axis (Godot's arm basis already contains a 180 deg yaw), so Yaw is XNA RotationY of the view direction.
    /// </summary>
    public sealed class ThirdPersonCamera
    {
        readonly CollisionWorld _level;
        public float Yaw;     // XNA RotationY, radians; view direction at pitch 0 = Forward rotated by yaw
        public float Pitch;   // Godot camera_rot.rotation.x: positive = looking down, clamped [-89.9, 70] deg
        public Vector3 Position, ViewDir, Pivot;
        public float ArmLength = PlayerConstants.SpringLength; // after collision
        public Vector3 ArmOffset; // SpringArm3D:position in CameraRot space (aim shoulder tween, part B); zero by default

        public ThirdPersonCamera(CollisionWorld levelWorld, float yaw, float pitch = 0f) { _level = levelWorld; Yaw = yaw; Pitch = pitch; }

        /// <summary>look = (yaw right +, pitch down +) in radians (player_input.gd:rotate_camera: rotate_y(-x), rotation.x += y).</summary>
        public void Rotate(Vector2 look)
        {
            Yaw -= look.X;
            Pitch = MathHelper.Clamp(Pitch + look.Y, PlayerConstants.PitchMin, PlayerConstants.PitchMax);
        }

        public Matrix CameraRotation => Matrix.CreateRotationX(-Pitch) * Matrix.CreateRotationY(Yaw);

        /// <summary>Recompute camera position for a player at <paramref name="feet"/>.</summary>
        public void Update(Vector3 feet)
        {
            Pivot = feet + Vector3.Up * PlayerConstants.CameraPivotHeight;
            float a = MathHelper.ToRadians(PlayerConstants.ArmPitchDeg);
            Vector3 arm = new Vector3(0, MathF.Sin(a), MathF.Cos(a)); // from pivot toward the camera in CameraRot space (behind and above)
            Matrix r = CameraRotation;
            Vector3 origin = Pivot + Vector3.TransformNormal(ArmOffset, r);
            Vector3 dir = Vector3.Normalize(Vector3.TransformNormal(arm, r));
            ViewDir = -dir;
            float len = PlayerConstants.SpringLength;
            if (_level.Raycast(origin, dir, len, out var hit)) len = Math.Max(0f, hit.Distance - PlayerConstants.SpringMargin);
            ArmLength = len;
            Position = origin + dir * len;
        }

        public Matrix View => Matrix.CreateLookAt(Position, Position + ViewDir, Vector3.Up);
        public Matrix Projection(float aspect) => Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(PlayerConstants.Fov), aspect, PlayerConstants.Near, PlayerConstants.Far);
    }
}
