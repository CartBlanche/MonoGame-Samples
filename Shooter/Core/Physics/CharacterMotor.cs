using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Shooter.Physics
{
    /// <summary>
    /// Stand-in for Unity's CharacterController (PhysX): a capsule whose feet are at <see cref="Position"/>
    /// (Unity keeps center = height/2, so the transform sits at the base). Move() collides and slides against a
    /// <see cref="CollisionWorld"/> using small sub-steps followed by overlap recovery, which is what makes the
    /// capsule slide along walls. Unity behaviours reproduced: slope limit (steeper up-facing surfaces cannot be
    /// climbed; the capsule is pushed off sideways), step offset (lift, move, drop, only when standing on
    /// ground and only onto walkable ground), skin width, overlap recovery.
    /// </summary>
    public sealed class CharacterMotor
    {
        private const float MaxSubStep = 0.08f;   // < radius/4, so thin (even zero-thickness) walls cannot be skipped
        private const int RecoveryIterations = 8;

        private readonly CollisionWorld _world;
        private readonly List<CollisionWorld.Contact> _contacts = new List<CollisionWorld.Contact>();
        private bool _touchedSteep;
        private readonly float _cosSlope;

        public Vector3 Position;
        public float Height;
        public readonly float Radius;
        public readonly float SlopeLimit;
        public readonly float StepOffset;
        public readonly float SkinWidth;

        public CharacterMotor(CollisionWorld world, float height, float radius, float slopeLimitDegrees, float stepOffset, float skinWidth)
        {
            _world = world;
            Height = height;
            Radius = radius;
            SlopeLimit = slopeLimitDegrees;
            StepOffset = stepOffset;
            SkinWidth = skinWidth;
            _cosSlope = (float)Math.Cos(MathHelper.ToRadians(slopeLimitDegrees));
        }

        public CollisionWorld World { get { return _world; } }

        public Vector3 BottomHemisphere(Vector3 position) { return position + Vector3.Up * Radius; }
        public Vector3 TopHemisphere(Vector3 position, float height) { return position + Vector3.Up * (height - Radius); }

        /// <summary>True when the surface normal is walkable (angle to up within the slope limit).</summary>
        public bool IsWalkable(Vector3 normal)
        {
            return normal.Y >= _cosSlope - 1e-5f;
        }

        public bool OverlapCapsule(Vector3 position, float height)
        {
            return _world.Overlaps(BottomHemisphere(position), TopHemisphere(position, height), Radius);
        }

        /// <summary>Physics.CapsuleCast of the controller capsule from its current position.</summary>
        public bool CapsuleCast(Vector3 dir, float distance, out float hitDistance, out Vector3 hitNormal)
        {
            return CapsuleCastFrom(Position, Height, dir, distance, out hitDistance, out hitNormal);
        }

        public bool CapsuleCastFrom(Vector3 position, float height, Vector3 dir, float distance, out float hitDistance, out Vector3 hitNormal)
        {
            return _world.CapsuleCast(BottomHemisphere(position), TopHemisphere(position, height), Radius, dir, distance, out hitDistance, out hitNormal);
        }

        /// <summary>
        /// Pushes a capsule that starts inside geometry out of it (Unity enableOverlapRecovery), shallowest contact first.
        /// Calibrated on one PhysX observation: the baked player start sits inside a 0.5 m floor slab and PhysX moved it
        /// +0.5 up and 0.1 sideways off the neighbouring cell's internal face; deepest-first would only go up.
        /// </summary>
        public void Recover()
        {
            Position = Depenetrate(Position, true);
        }

        /// <param name="preventClimbing">
        /// Set while grounded. Unity's velocity projection (ProjectOnPlane) gives an upward velocity when the player runs into a
        /// slope steeper than the slope limit; PhysX's controller does not let that carry the player up the slope, so the upward
        /// part of the motion is dropped when this move touched such a slope.
        /// </param>
        public void Move(Vector3 motion, bool preventClimbing = false)
        {
            Position = Depenetrate(Position);
            Vector3 start = Position;
            _touchedSteep = false;
            Vector3 plain = SlideMove(start, motion);
            if (preventClimbing && _touchedSteep && motion.Y > 0f)
            {
                motion.Y = 0f;
                plain = SlideMove(start, motion);
            }
            Vector3 result = plain;

            var horizontal = new Vector3(motion.X, 0f, motion.Z);
            float hLen = horizontal.Length();
            if (StepOffset > 0f && hLen > 1e-5f && motion.Y <= 1e-4f)
            {
                Vector3 hDir = horizontal / hLen;
                float progress = Vector3.Dot(plain - start, hDir);
                if (progress < hLen - 1e-3f && StandingOnWalkableGround(start))
                {
                    if (TryStep(start, motion, out Vector3 stepped) && Vector3.Dot(stepped - start, hDir) > progress + 0.005f)
                    {
                        result = stepped;
                    }
                }
            }
            Position = result;
        }

        private bool StandingOnWalkableGround(Vector3 pos)
        {
            return CapsuleCastFrom(pos, Height, Vector3.Down, SkinWidth + 0.1f, out _, out Vector3 n) && IsWalkable(n);
        }

        // lift by the step offset, move sideways, drop back onto walkable ground
        private bool TryStep(Vector3 start, Vector3 motion, out Vector3 result)
        {
            result = start;
            Vector3 lifted = SlideMove(start, Vector3.Up * (StepOffset + 2f * SkinWidth));
            float lift = lifted.Y - start.Y;
            if (lift < 1e-3f)
            {
                return false;
            }
            Vector3 side = SlideMove(lifted, new Vector3(motion.X, 0f, motion.Z));
            float drop = lift + Math.Max(0f, -motion.Y) + SkinWidth;
            if (!CapsuleCastFrom(side, Height, Vector3.Down, drop, out float dist, out Vector3 n) || !IsWalkable(n))
            {
                return false;
            }
            result = Depenetrate(side + Vector3.Down * dist);
            return true;
        }

        private Vector3 SlideMove(Vector3 pos, Vector3 motion)
        {
            float len = motion.Length();
            if (len < 1e-7f)
            {
                return pos;
            }
            int steps = Math.Max(1, (int)Math.Ceiling(len / MaxSubStep));
            Vector3 sub = motion / steps;
            for (int i = 0; i < steps; i++)
            {
                pos = Depenetrate(pos + sub);
            }
            return pos;
        }

        private Vector3 Depenetrate(Vector3 pos, bool shallowestFirst = false)
        {
            for (int i = 0; i < RecoveryIterations; i++)
            {
                CollisionWorld.Contact c;
                if (shallowestFirst)
                {
                    _contacts.Clear();
                    _world.Contacts(BottomHemisphere(pos), TopHemisphere(pos, Height), Radius, _contacts);
                    if (_contacts.Count == 0)
                    {
                        break;
                    }
                    c = _contacts[0];
                    for (int k = 1; k < _contacts.Count; k++)
                    {
                        if (_contacts[k].Depth < c.Depth) c = _contacts[k];
                    }
                }
                else if (!_world.DeepestContact(BottomHemisphere(pos), TopHemisphere(pos, Height), Radius, out c))
                {
                    break;
                }
                float push = c.Depth + SkinWidth;
                Vector3 n = c.Normal;
                if (n.Y > 1e-4f && n.Y < _cosSlope)
                {
                    // too steep to stand on: leave sideways instead of being lifted up the slope
                    _touchedSteep = true;
                    float hl = (float)Math.Sqrt(n.X * n.X + n.Z * n.Z);
                    pos += new Vector3(n.X, 0f, n.Z) / hl * (push / hl);
                }
                else
                {
                    pos += n * push;
                }
            }
            return pos;
        }
    }
}
