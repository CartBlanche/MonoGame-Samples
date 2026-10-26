using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Scene;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>
    /// Port of AI/EnemyTurret.cs. The script works with world rotations of Transforms; this port does the same maths in Unity space
    /// (left-handed, Hamilton products where the right factor is applied first, LookRotation with +Z forward) and converts at the edges,
    /// so that the formulas stay copies of the script. TurretPivot is the Turret_Pivot_C_Jnt bone: after the Animator has been evaluated
    /// (the late update) its world rotation is replaced by the aiming rotation while attacking, and blended back to the animation's after the
    /// target was lost.
    /// </summary>
    public class EnemyTurret
    {
        public enum AIState { Idle, Attack }

        public const string PivotBone = "Turret_Pivot_C_Jnt";

        public float AimRotationSharpness = 5f, LookAtRotationSharpness = 2.5f, DetectionFireDelay = 1f, AimingTransitionBlendTime = 1f;
        public AIState AiState { get; private set; }

        private readonly Enemy _e;
        private readonly Quaternion _rotationWeaponForwardToPivot;   // Unity space
        private float _timeStartedDetection = float.NegativeInfinity;
        private float _timeLostDetection;
        private Quaternion _previousPivotAimingRotation;
        private Quaternion _pivotAimingRotation;                      // default = (0, 0, 0, 0) like the script's uninitialised field
        private bool _pivotAimingSet;
        private float _dt = 1f / 60f;

        public EnemyTurret(Enemy enemy, JsonElement fields)
        {
            _e = enemy;
            AimRotationSharpness = JsonFields.Float(fields, "AimRotationSharpness", AimRotationSharpness);
            LookAtRotationSharpness = JsonFields.Float(fields, "LookAtRotationSharpness", LookAtRotationSharpness);
            DetectionFireDelay = JsonFields.Float(fields, "DetectionFireDelay", DetectionFireDelay);
            AimingTransitionBlendTime = JsonFields.Float(fields, "AimingTransitionBlendTime", AimingTransitionBlendTime);

            _e.DetectedTarget += OnDetectedTarget;
            _e.LostTarget += OnLostTarget;
            _e.OnDamagedAny += OnDamagedAny;

            // Start(): the rotation between the muzzle's forward and the pivot, in the pose the scene holds before the Animator's first update (the bind pose)
            Quaternion muzzle = WorldRotationUnity(_e.NodeWorld("GunMuzzle", true));
            Quaternion pivot = WorldRotationUnity(_e.NodeWorld(PivotBone, true));
            _rotationWeaponForwardToPivot = Mul(Inverse(muzzle), pivot);
            AiState = AIState.Idle;
            _previousPivotAimingRotation = pivot;

            _e.View?.SetLateUpdate(LateUpdate);
        }

        // ------------------------------------------------------------------ Update / LateUpdate

        public void Update(float dt)
        {
            _dt = dt;
            if (AiState != AIState.Attack)
            {
                return;
            }
            float time = _e.Time;
            bool mustShoot = time > _timeStartedDetection + DetectionFireDelay;

            // desired rotation of the turret (aim at the target), all in Unity space
            Vector3 aim = ToUnity(_e.NodeWorld("WeaponRoot").Translation);   // TurretAimPoint
            Vector3 target = ToUnity(_e.Detection.KnownDetectedTarget.Position);
            Vector3 directionToTarget = Vector3.Normalize(target - aim);
            Quaternion offsettedTargetRotation = Mul(LookRotation(directionToTarget), _rotationWeaponForwardToPivot);
            _pivotAimingRotation = Slerp(_previousPivotAimingRotation, offsettedTargetRotation, (mustShoot ? AimRotationSharpness : LookAtRotationSharpness) * dt);
            _pivotAimingSet = true;

            if (mustShoot)
            {
                Vector3 corrected = Rotate(Mul(_pivotAimingRotation, Inverse(_rotationWeaponForwardToPivot)), new Vector3(0f, 0f, 1f));
                _e.TryAttack(ToMonoGame(aim + corrected));
            }
        }

        private void LateUpdate()
        {
            if (_e.View == null || !_e.View.TryGetBoneWorld(PivotBone, false, out Matrix pivotMatrix))
            {
                return;
            }
            Quaternion animated = WorldRotationUnity(pivotMatrix);   // TurretPivot.rotation as the animation left it
            Quaternion rotation;
            if (AiState == AIState.Attack)
            {
                rotation = _pivotAimingRotation;
            }
            else
            {
                // use the turret rotation of the animation, blending from the last aiming rotation after the target was lost
                float t = (_e.Time - _timeLostDetection) / AimingTransitionBlendTime;
                rotation = _pivotAimingSet && t < 1f ? Slerp(_pivotAimingRotation, animated, t) : animated;
            }
            if (rotation != animated)
            {
                _e.View.SetBoneWorldRotation(PivotBone, UnityConvert.Rotation(rotation));
            }
            _previousPivotAimingRotation = rotation;
        }

        // ------------------------------------------------------------------ events

        private void OnDetectedTarget()
        {
            if (AiState == AIState.Idle)
            {
                AiState = AIState.Attack;
            }
            // OnDetectVfx / OnDetectSfx: 3d
            _e.View?.SetBool("IsActive", true);
            _timeStartedDetection = _e.Time;
        }

        private void OnLostTarget()
        {
            if (AiState == AIState.Attack)
            {
                AiState = AIState.Idle;
            }
            _e.View?.SetBool("IsActive", false);
            _timeLostDetection = _e.Time;
        }

        private void OnDamagedAny()
        {
            // EnemyTurret.OnDamaged hooks Health.OnDamaged directly: any damage, from any source, plays the hit animation
            _e.View?.SetTrigger("OnDamaged");
        }

        // ------------------------------------------------------------------ Unity-space maths

        public static Vector3 ToUnity(Vector3 mg) { return new Vector3(mg.X, mg.Y, -mg.Z); }
        public static Vector3 ToMonoGame(Vector3 u) { return new Vector3(u.X, u.Y, -u.Z); }

        private static Quaternion WorldRotationUnity(Matrix world)
        {
            Vector3 x = new Vector3(world.M11, world.M12, world.M13), y = new Vector3(world.M21, world.M22, world.M23), z = new Vector3(world.M31, world.M32, world.M33);
            x.Normalize(); y.Normalize(); z.Normalize();
            Quaternion mg = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(new Matrix(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1)));
            return UnityConvert.Rotation(mg);
        }

        /// <summary>Unity's a * b: b is applied first.</summary>
        public static Quaternion Mul(Quaternion a, Quaternion b) { return UnityConvert.Hamilton(a, b); }

        public static Quaternion Inverse(Quaternion q) { return new Quaternion(-q.X, -q.Y, -q.Z, q.W); }

        /// <summary>q * v for a unit quaternion.</summary>
        public static Vector3 Rotate(Quaternion q, Vector3 v)
        {
            var u = new Vector3(q.X, q.Y, q.Z);
            return v + 2f * Vector3.Cross(u, Vector3.Cross(u, v) + q.W * v);
        }

        /// <summary>Quaternion.Slerp (t clamped to 0..1, shortest arc).</summary>
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return Quaternion.Normalize(Quaternion.Slerp(a, b, t));
        }

        /// <summary>Quaternion.LookRotation(forward, Vector3.up) in Unity's left-handed space.</summary>
        public static Quaternion LookRotation(Vector3 forward)
        {
            Vector3 z = Vector3.Normalize(forward);
            Vector3 x = Vector3.Cross(Vector3.Up, z);
            if (x.LengthSquared() < 1e-12f)
            {
                x = Vector3.Right;
            }
            x.Normalize();
            Vector3 y = Vector3.Cross(z, x);
            // rotation matrix with the basis vectors as columns (local axis -> world): m(row, col)
            float m00 = x.X, m01 = y.X, m02 = z.X, m10 = x.Y, m11 = y.Y, m12 = z.Y, m20 = x.Z, m21 = y.Z, m22 = z.Z;
            float trace = m00 + m11 + m22;
            Quaternion q;
            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1f) * 2f;
                q = new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s);
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = (float)Math.Sqrt(1f + m00 - m11 - m22) * 2f;
                q = new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
            }
            else if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1f + m11 - m00 - m22) * 2f;
                q = new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s);
            }
            else
            {
                float s = (float)Math.Sqrt(1f + m22 - m00 - m11) * 2f;
                q = new Quaternion((m02 + m20) / s, (m12 + m21) / s, 0.25f * s, (m10 - m01) / s);
            }
            return Quaternion.Normalize(q);
        }
    }
}
