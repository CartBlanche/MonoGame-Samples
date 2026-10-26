using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Physics;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>What an enemy knows about its target: Unity keeps a GameObject, which is the actor's AimPoint when it was seen, or the damage source's root when it was shot.</summary>
    public class TargetRef
    {
        public Actor Actor;
        public bool IsAimPoint;
        public Vector3 Position { get { return IsAimPoint ? Actor.GetAimPoint() : Actor.GetPosition(); } }
    }

    /// <summary>Port of AI/DetectionModule.cs. There is no field of view: detection is a range check plus a line-of-sight ray, as in the script.</summary>
    public class DetectionModule
    {
        public float DetectionRange = 20f, AttackRange = 10f, KnownTargetTimeout = 4f;

        public event Action DetectedTarget;
        public event Action LostTarget;

        public TargetRef KnownDetectedTarget { get; private set; }
        public bool IsTargetInAttackRange { get; private set; }
        public bool IsSeeingTarget { get; private set; }
        public bool HadKnownTarget { get; private set; }

        private float _timeLastSeenTarget = float.NegativeInfinity;

        public DetectionModule(JsonElement fields)
        {
            DetectionRange = JsonFields.Float(fields, "DetectionRange", DetectionRange);
            AttackRange = JsonFields.Float(fields, "AttackRange", AttackRange);
            KnownTargetTimeout = JsonFields.Float(fields, "KnownTargetTimeout", KnownTargetTimeout);
        }

        public void HandleTargetDetection(Enemy self, ActorsManager actors, CollisionWorld world, float time)
        {
            // known target timeout
            if (KnownDetectedTarget != null && !IsSeeingTarget && (time - _timeLastSeenTarget) > KnownTargetTimeout)
            {
                KnownDetectedTarget = null;
            }

            // closest visible hostile actor
            Vector3 source = self.DetectionSourcePosition;
            float sqrRange = DetectionRange * DetectionRange;
            IsSeeingTarget = false;
            float closest = float.PositiveInfinity;
            foreach (Actor other in actors.Actors)
            {
                if (other.Affiliation == self.Actor.Affiliation)
                {
                    continue;
                }
                float sqr = Vector3.DistanceSquared(other.GetPosition(), source);
                if (sqr < sqrRange && sqr < closest && HasLineOfSight(self, source, other, world))
                {
                    IsSeeingTarget = true;
                    closest = sqr;
                    _timeLastSeenTarget = time;
                    KnownDetectedTarget = new TargetRef { Actor = other, IsAimPoint = true };
                }
            }

            IsTargetInAttackRange = KnownDetectedTarget != null && Vector3.Distance(self.Position, KnownDetectedTarget.Position) <= AttackRange;

            if (!HadKnownTarget && KnownDetectedTarget != null)
            {
                OnDetect();
            }
            if (HadKnownTarget && KnownDetectedTarget == null)
            {
                OnLost();
            }
            HadKnownTarget = KnownDetectedTarget != null;
        }

        /// <summary>
        /// RaycastAll from the source towards the other actor's AimPoint over the detection range; the closest hit that is not one of the
        /// enemy's own colliders must belong to the other actor. The player's CharacterController capsule is not in the triangle world, so
        /// the world hit must lie beyond where the ray enters that capsule.
        /// </summary>
        private bool HasLineOfSight(Enemy self, Vector3 source, Actor other, CollisionWorld world)
        {
            Vector3 aim = other.GetAimPoint();
            Vector3 toAim = aim - source;
            float len = toAim.Length();
            if (len < 1e-6f)
            {
                return true;
            }
            Vector3 dir = toAim / len;
            CollisionWorld.RayHit hit = default(CollisionWorld.RayHit);
            bool worldHit = world != null && world.Raycast(source, dir, DetectionRange, out hit, self.HitTags);
            if (other.GetCapsule == null)
            {
                return !worldHit || hit.Distance >= len;
            }
            Capsule cap = other.GetCapsule();
            if (!RayShapes.RayCapsule(source, dir, DetectionRange, cap.A, cap.B, cap.Radius, out float tCap, out _))
            {
                return false;   // the ray misses the collider within the range (or starts inside it: then it hits "nothing" and the closest valid hit is not the actor)
            }
            return !worldHit || hit.Distance > tCap;
        }

        public void OnDamaged(Actor damageSource, float time)
        {
            _timeLastSeenTarget = time;
            KnownDetectedTarget = new TargetRef { Actor = damageSource, IsAimPoint = false };
        }

        private void OnDetect() { if (DetectedTarget != null) DetectedTarget(); }
        private void OnLost() { if (LostTarget != null) LostTarget(); }
    }
}
