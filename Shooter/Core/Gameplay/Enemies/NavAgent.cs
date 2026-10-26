using System;
using Microsoft.Xna.Framework;
using Shooter.Physics;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>
    /// Stand-in for UnityEngine.AI.NavMeshAgent (no NavMesh exists here). It copies the agent's kinematics from the Inspector values: speed,
    /// acceleration, auto braking, stopping distance, and turning towards the velocity at the angular speed. What it does NOT have is path
    /// finding: it steers in a straight line to the destination and slides along walls (a vertical capsule of the agent's radius and height
    /// against <see cref="CollisionWorld"/>), so it can get stuck behind an obstacle where a NavMesh agent would walk around it. It stays at
    /// its spawn height like a hovering agent on a flat navmesh, moves in the horizontal plane only, and has no avoidance between agents.
    /// </summary>
    public class NavAgent
    {
        public float Speed = 3.5f, Acceleration = 8f, AngularSpeed = 120f, StoppingDistance = 0f, Radius = 0.5f, Height = 2f;
        public bool AutoBraking = true;

        public Vector3 Position;            // root, MonoGame space
        public float YawDegrees;            // Unity yaw degrees (positive = to the right, 0 = Unity +Z)
        public Vector3 Velocity;            // horizontal
        public bool IsStuck { get; private set; }   // the last step was blocked on both axes

        private Vector3 _destination;
        private bool _hasDestination;

        public Vector3 Destination { get { return _destination; } }
        public float VelocityMagnitude { get { return Velocity.Length(); } }

        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
            _hasDestination = true;
        }

        /// <summary>Horizontal distance to the destination.</summary>
        public float RemainingDistance
        {
            get
            {
                if (!_hasDestination)
                {
                    return 0f;
                }
                return new Vector2(_destination.X - Position.X, _destination.Z - Position.Z).Length();
            }
        }

        /// <summary>Unity yaw of a horizontal MonoGame-space direction.</summary>
        public static float YawOf(float dx, float dz)
        {
            return MathHelper.ToDegrees((float)Math.Atan2(dx, -dz));
        }

        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = MathHelper.ToDegrees(MathHelper.WrapAngle(MathHelper.ToRadians(target - current)));
            if (Math.Abs(delta) <= maxDelta)
            {
                return current + delta;
            }
            return current + Math.Sign(delta) * maxDelta;
        }

        public void Update(float dt, CollisionWorld world, int ignoreOwner)
        {
            Vector3 desired = Vector3.Zero;
            if (_hasDestination)
            {
                float dx = _destination.X - Position.X, dz = _destination.Z - Position.Z;
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist > StoppingDistance && dist > 1e-5f)
                {
                    float speed = Speed;
                    if (AutoBraking)
                    {
                        // slow down so that the agent can stop at the stopping distance with its acceleration
                        speed = Math.Min(Speed, (float)Math.Sqrt(2f * Acceleration * (dist - StoppingDistance)));
                    }
                    desired = new Vector3(dx / dist * speed, 0f, dz / dist * speed);
                }
            }

            // accelerate towards the desired velocity
            Vector3 dv = desired - Velocity;
            float maxChange = Acceleration * dt;
            float len = dv.Length();
            Velocity = len <= maxChange || len < 1e-6f ? desired : Velocity + dv / len * maxChange;

            // turn towards the direction of travel
            if (Velocity.LengthSquared() > 0.05f * 0.05f)
            {
                YawDegrees = MoveTowardsAngle(YawDegrees, YawOf(Velocity.X, Velocity.Z), AngularSpeed * dt);
            }

            // move with wall sliding
            IsStuck = false;
            Vector3 step = Velocity * dt;
            if (step.LengthSquared() > 0f)
            {
                Vector3 full = Position + step;
                if (!Blocked(world, full, ignoreOwner))
                {
                    Position = full;
                }
                else
                {
                    var onlyX = new Vector3(Position.X + step.X, Position.Y, Position.Z);
                    var onlyZ = new Vector3(Position.X, Position.Y, Position.Z + step.Z);
                    if (step.X != 0f && !Blocked(world, onlyX, ignoreOwner))
                    {
                        Position = onlyX;
                        Velocity.Z = 0f;
                    }
                    else if (step.Z != 0f && !Blocked(world, onlyZ, ignoreOwner))
                    {
                        Position = onlyZ;
                        Velocity.X = 0f;
                    }
                    else
                    {
                        Velocity = Vector3.Zero;
                        IsStuck = true;
                    }
                }
            }
        }

        private bool Blocked(CollisionWorld world, Vector3 root, int ignoreOwner)
        {
            if (world == null)
            {
                return false;
            }
            // capsule from the root up by Height; the hemisphere centres sit Radius inside the ends
            Vector3 a = root + Vector3.Up * Radius, b = root + Vector3.Up * (Height - Radius);
            return world.OverlapsIgnoring(a, b, Radius, ignoreOwner);
        }
    }
}
