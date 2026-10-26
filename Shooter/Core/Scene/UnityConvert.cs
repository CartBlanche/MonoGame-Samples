using System;
using Microsoft.Xna.Framework;

namespace Shooter.Scene
{
    /// <summary>
    /// The ONE place where Unity (left-handed, +Z forward) data is converted to the project's MonoGame
    /// right-handed convention. See Core/Content/ASSET-CONVENTIONS.md section 1.
    ///
    ///   F = diag(1, 1, -1)
    ///   position   (x, y, z)    -> (x, y, -z)
    ///   quaternion (x, y, z, w) -> (-x, -y, z, w)     (F R F)
    ///   scale      (x, y, z)    -> (x, y, z)
    ///
    /// Every node's LOCAL TRS is converted independently and then composed as usual (child * parent), because
    /// conjugation by F distributes over the hierarchy. Mesh vertex data is already baked by the content pipeline
    /// (RotationY 180 of the file data == Unity mesh, converted) and must NOT be converted again.
    /// Linked into Tools/SceneBake so the baker and the runtime share this code.
    /// </summary>
    public static class UnityConvert
    {
        public static Vector3 Position(Vector3 u)
        {
            return new Vector3(u.X, u.Y, -u.Z);
        }

        public static Vector3 Direction(Vector3 u)
        {
            return new Vector3(u.X, u.Y, -u.Z);
        }

        public static Quaternion Rotation(Quaternion u)
        {
            return new Quaternion(-u.X, -u.Y, u.Z, u.W);
        }

        /// <summary>World (or local) matrix from Unity TRS values: Scale * Rotation * Translation (XNA row-vector order).</summary>
        public static Matrix TrsToMatrix(Vector3 unityPos, Quaternion unityRot, Vector3 unityScale)
        {
            return Matrix.CreateScale(unityScale)
                 * Matrix.CreateFromQuaternion(Rotation(unityRot))
                 * Matrix.CreateTranslation(Position(unityPos));
        }

        /// <summary>Hamilton product a*b (b is applied first), matching Unity's Quaternion operator*.</summary>
        public static Quaternion Hamilton(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
        }

        /// <summary>
        /// Unity Quaternion.Euler(x, y, z) (degrees): rotate about Z first, then X, then Y (q = qY * qX * qZ),
        /// result in UNITY space. Positive X pitches DOWN (+Z forward rotates toward -Y).
        /// </summary>
        public static Quaternion UnityEuler(Vector3 eulerDegrees)
        {
            float hx = MathHelper.ToRadians(eulerDegrees.X) * 0.5f;
            float hy = MathHelper.ToRadians(eulerDegrees.Y) * 0.5f;
            float hz = MathHelper.ToRadians(eulerDegrees.Z) * 0.5f;
            var qx = new Quaternion((float)Math.Sin(hx), 0, 0, (float)Math.Cos(hx));
            var qy = new Quaternion(0, (float)Math.Sin(hy), 0, (float)Math.Cos(hy));
            var qz = new Quaternion(0, 0, (float)Math.Sin(hz), (float)Math.Cos(hz));
            return Quaternion.Normalize(Hamilton(qy, Hamilton(qx, qz)));
        }

        /// <summary>Camera-to-world matrix in MonoGame space for a Unity camera pose (position + eulerAngles in degrees).</summary>
        public static Matrix CameraWorldFromUnityPose(Vector3 unityPos, Vector3 unityEulerDegrees)
        {
            return TrsToMatrix(unityPos, UnityEuler(unityEulerDegrees), Vector3.One);
        }

        /// <summary>Unity vertical FOV (degrees) projection, right-handed, looking down -Z.</summary>
        public static Matrix Perspective(float fovYDegrees, float aspect, float near, float far)
        {
            return Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fovYDegrees), aspect, near, far);
        }
    }
}
