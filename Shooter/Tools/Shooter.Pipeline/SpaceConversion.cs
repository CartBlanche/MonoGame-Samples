using Microsoft.Xna.Framework;

namespace Shooter.Pipeline
{
    /// <summary>Which coordinate system imported data is delivered in.</summary>
    public enum CoordinateSpace
    {
        /// <summary>
        /// Project-wide MonoGame world (right-handed, Y up, -Z forward; see ASSET-CONVENTIONS.md section 1/2).
        /// FBX native -> Unity import (X mirror, scale) -> F = diag(1,1,-1), which nets to RotationY(180) of the native data:
        /// position (-x, y, -z) * scale. A proper rotation, so triangle winding is unchanged.
        /// </summary>
        MonoGame,

        /// <summary>Unity import space (left-handed): position (-x, y, z) * scale. Only for tools that need Unity values.</summary>
        Unity,

        /// <summary>Raw FBX space (right-handed), only scaled.</summary>
        Native
    }

    /// <summary>
    /// THE one place where FBX space is converted to the game's space. Every conversion in the pipeline (mesh vertices,
    /// bind poses, animation tracks) and the skeleton JSON exporter goes through this struct.
    /// The conversion is a diagonal sign matrix S (per-axis +-1) plus a uniform scale:
    ///   position  p  -> S p * scale
    ///   direction n  -> S n
    ///   rotation  q  -> (det(S) * S * q.xyz, q.w)       (conjugation S R S)
    ///   local TRS matrix A (row-vector) -> S A S with translation * scale
    /// MonoGame: S = diag(-1,1,-1) (det +1);  Unity: diag(-1,1,1) (det -1);  Native: identity.
    /// To get Unity-space values from MonoGame-space ones apply F = diag(1,1,-1) again: (x, y, -z), quaternion (-x,-y,z,w).
    /// </summary>
    public readonly struct SpaceConversion
    {
        public readonly float SignX;
        public readonly float SignY;
        public readonly float SignZ;
        public readonly float Scale;
        public readonly CoordinateSpace Space;

        public SpaceConversion(CoordinateSpace space, float scale)
        {
            Space = space;
            Scale = scale;
            SignY = 1f;
            switch (space)
            {
                case CoordinateSpace.MonoGame:
                    SignX = -1f;
                    SignZ = -1f;
                    break;
                case CoordinateSpace.Unity:
                    SignX = -1f;
                    SignZ = 1f;
                    break;
                default:
                    SignX = 1f;
                    SignZ = 1f;
                    break;
            }
        }

        public bool MirrorX
        {
            get { return SignX * SignY * SignZ < 0f; }
        }

        public Vector3 Position(Vector3 p)
        {
            return new Vector3(p.X * SignX, p.Y * SignY, p.Z * SignZ) * Scale;
        }

        public Vector3 Direction(Vector3 d)
        {
            return new Vector3(d.X * SignX, d.Y * SignY, d.Z * SignZ);
        }

        public Quaternion Rotation(Quaternion q)
        {
            float det = SignX * SignY * SignZ;
            return new Quaternion(q.X * SignX * det, q.Y * SignY * det, q.Z * SignZ * det, q.W);
        }

        /// <summary>Converts a node-local (parent-relative) transform: S * A * S, translation scaled.</summary>
        public Matrix Local(Matrix m)
        {
            float[] s = { SignX, SignY, SignZ };
            float[,] a =
            {
                { m.M11, m.M12, m.M13 },
                { m.M21, m.M22, m.M23 },
                { m.M31, m.M32, m.M33 },
                { m.M41, m.M42, m.M43 }
            };
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    float si = i < 3 ? s[i] : 1f;
                    a[i, j] *= si * s[j];
                }
            }

            Matrix r = m;
            r.M11 = a[0, 0]; r.M12 = a[0, 1]; r.M13 = a[0, 2];
            r.M21 = a[1, 0]; r.M22 = a[1, 1]; r.M23 = a[1, 2];
            r.M31 = a[2, 0]; r.M32 = a[2, 1]; r.M33 = a[2, 2];
            r.M41 = a[3, 0] * Scale; r.M42 = a[3, 1] * Scale; r.M43 = a[3, 2] * Scale;
            return r;
        }
    }
}
