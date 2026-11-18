using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ThirdPersonShooter.Physics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Red robot explosion debris: red_robot.tscn Death/PartShield1, PartShield2, PartHead (RigidBody3D, enemies/red_robot/parts/part.gd) as three small rigid bodies.
    /// Godot facts (red_robot.tscn:10833-10985, part.gd): explode() -> linear_velocity (0,3,0), angular_velocity = (normalize(rand3)*2-1)*10, collision shapes enabled;
    /// layer 3 / mask 3 (Level + Player, so the level world here); PhysicsMaterial = defaults (friction 1, bounce 0); project damping defaults (linear 0.1, angular 0.1) + body angular_damp 0.3
    /// (PartShield1, PartHead); gravity 9.8; a part lives 3 + 3*rand s, then fades for 0.3 s (fade = (t/0.5)^2, part.gd:34-39), spawns the disappear puff, and is freed 0.2 s later.
    /// Simplified: each shape is a set of collision spheres (box corners / edge midpoints, sphere) against the static level triangles with an impulse solver (restitution, Coulomb friction,
    /// sleeping); part-player / part-robot contact as static capsule/sphere obstacles (<see cref="Obstacles"/>); no part-part contact (collides in Godot: layer 3 vs mask 3). Pure simulation, stepped at the fixed 60 Hz tick.
    /// </summary>
    public sealed class DebrisSystem
    {
        public const int Capacity = 64;
        public const float Gravity = 9.8f;                  // Godot default 3d gravity (project.godot sets none)
        public const float LinearDamp = 0.1f;               // project default_linear_damp 0.1 (+ body 0)
        public const float Bounce = 0f, Friction = 1f;      // default PhysicsMaterial
        public const float Lifetime = 3f, LifetimeRandom = 3f, DisappearingTime = 0.5f, PuffDelay = 0.2f;   // part.gd:8-10, :42, :48
        public const float LaunchSpeed = 3f, SpinScale = 10f;   // part.gd:31-32
        const int SubSteps = 4;
        const float SleepLinear = 0.1f, SleepAngular = 0.14f, SleepTime = 0.5f;   // Godot defaults (0.1 m/s, 8 deg/s, 0.5 s)
        public static readonly Vector3 DeathOffset = new Vector3(0, 0.5f, 0);    // red_robot.tscn:10829 Death node

        public struct Sphere { public Vector3 C; public float R; }
        struct Contact { public Vector3 R, N; public float Depth, Target, AccN, AccT; }

        public sealed class Part
        {
            public int Type;                       // 0 PartShield1, 1 PartShield2, 2 PartHead
            public bool IsHead => Type == 2;
            public bool Alive, Fading, Dying, Sleeping;
            public Vector3 Pos, PrevPos, V, W;
            public Quaternion Rot, PrevRot;
            public Sphere[] Spheres;
            public Vector3 InvInertia;             // diagonal, body frame, unit mass
            public float AngDamp, Wait, Elapsed, Counter, Fade, DieTimer, SleepT;
            public int Touching;
        }

        readonly CollisionWorld _world;
        readonly Part[] _parts = new Part[Capacity];
        readonly List<CollisionWorld.Contact> _tmp = new List<CollisionWorld.Contact>();
        Contact[] _c = new Contact[256];
        /// <summary>Test hook: restitution used by the solver (Godot: 0).</summary>
        public float Restitution = Bounce;
        public int TotalSpawned { get; private set; }
        public int Count { get { int n = 0; for (int i = 0; i < Capacity; i++) if (_parts[i] != null && _parts[i].Alive) n++; return n; } }
        public ReadOnlySpan<Part> All => _parts;
        /// <summary>DetachSpark1/2 sites at the explosion (world position).</summary>
        public event Action<Vector3> SparkBurst;
        /// <summary>part_disappear.tscn puff at a part's position when it vanishes.</summary>
        public event Action<Vector3> Puff;

        /// <summary>Kinematic bodies the parts collide with (layer 3 vs the player's layer 2/3 and the robots' layer 3): capsules A-B radius R (a sphere when A == B), refilled by the simulation every tick.</summary>
        public readonly List<(Vector3 A, Vector3 B, float R)> Obstacles = new List<(Vector3, Vector3, float)>();

        /// <summary>World-space collision spheres of every live part (what the player and the robots must not walk through).</summary>
        public void AppendSpheres(Action<Vector3, float> add)
        {
            for (int i = 0; i < Capacity; i++)
            {
                Part p = _parts[i]; if (!p.Alive) continue;
                foreach (var s in p.Spheres) add(p.Pos + Vector3.Transform(s.C, p.Rot), s.R);
            }
        }

        public DebrisSystem(CollisionWorld levelWorld) { _world = levelWorld; for (int i = 0; i < Capacity; i++) _parts[i] = new Part(); }

        static Matrix G(float a0, float a1, float a2, float a3, float a4, float a5, float a6, float a7, float a8, float ox, float oy, float oz)
            => new Matrix(a0, a3, a6, 0, a1, a4, a7, 0, a2, a5, a8, 0, ox, oy, oz, 1);   // Godot Transform3D text (row-major basis) -> XNA row-vector matrix

        static void BoxSpheres(List<Sphere> l, Matrix local, Vector3 size)
        {
            float r = size.Z * 0.5f; float hx = size.X * 0.5f - r, hy = size.Y * 0.5f - r;   // thin plate: spheres of half the thickness at corners + edge midpoints
            foreach (var (x, y) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f), (0f, -1f), (0f, 1f), (-1f, 0f), (1f, 0f) })
                l.Add(new Sphere { C = Vector3.Transform(new Vector3(x * hx, y * hy, 0), local), R = r });
        }

        static Sphere[] BuildShapes(int type, out Vector3 invI)
        {
            var l = new List<Sphere>();
            if (type == 2)
            {   // PartHead: SphereShape3D r 1.0 (Col1) + BoxShape3D plate (Col2, rotated 90 deg about X)
                l.Add(new Sphere { C = Vector3.Zero, R = 1f });
                BoxSpheres(l, G(1, 0, 0, 0, -4.37114e-08f, 1, 0, -1, -4.37114e-08f, -0.0753293f, 0.680746f, 0.945143f), new Vector3(0.177054f, 1.02904f, 0.0948846f));
                invI = Vector3.One / 0.4f;                  // solid sphere r 1, unit mass
                return l.ToArray();
            }
            Matrix a = G(1, 0, 0, 0, 0.908425f, 0.418048f, 0, -0.418048f, 0.908425f, 0, 0, 0);
            if (type == 0)
            {
                a.Translation = new Vector3(-0.0167698f, 0.5788f, -0.122978f); BoxSpheres(l, a, new Vector3(0.890932f, 1.24566f, 0.0948846f));
                BoxSpheres(l, G(1, 0, 0, 0, 0.994888f, 0.100984f, 0, -0.100984f, 0.994888f, 0.0804193f, -0.63062f, 0.179012f), new Vector3(0.864586f, 1.02904f, 0.0948846f));
            }
            else
            {
                a.Translation = new Vector3(-0.0150018f, 0.599786f, -0.170673f); BoxSpheres(l, a, new Vector3(0.822392f, 1.02904f, 0.0948846f));
                BoxSpheres(l, G(1, 0, 0, 0, 0.999275f, -0.038063f, 0, 0.038063f, 0.999275f, 0.0207567f, -0.47267f, 0.0351334f), new Vector3(0.806126f, 1.02904f, 0.0948846f));
            }
            Vector3 lo = new Vector3(1e9f), hi = new Vector3(-1e9f);
            foreach (var s in l) { lo = Vector3.Min(lo, s.C - new Vector3(s.R)); hi = Vector3.Max(hi, s.C + new Vector3(s.R)); }
            Vector3 d = hi - lo;                           // inertia of the union AABB as a unit-mass box
            invI = new Vector3(12f / (d.Y * d.Y + d.Z * d.Z), 12f / (d.X * d.X + d.Z * d.Z), 12f / (d.X * d.X + d.Y * d.Y));
            return l.ToArray();
        }

        static readonly Vector3[] PartLocal = { new Vector3(-1.37822f, 1.11034f, 1.09121f), new Vector3(1.75083f, 1.02975f, 0.162546f), new Vector3(-0.0374503f, 1.02975f, 0.162546f) };
        static readonly float[] PartAngDamp = { 0.3f + 0.1f, 0.1f, 0.3f + 0.1f };   // tscn angular_damp (0 = unset for PartShield2) + project default 0.1

        /// <summary>explode() of PartShield1, PartShield2, PartHead of a robot standing at <paramref name="feet"/> facing <paramref name="yaw"/> (Death node frame = the robot's). Deterministic for a given seed.</summary>
        public void Explode(Vector3 feet, float yaw, int seed)
        {
            var rng = new Random(seed); Matrix R = Matrix.CreateRotationY(yaw); Quaternion q = Quaternion.CreateFromAxisAngle(Vector3.Up, yaw);
            for (int t = 0; t < 3; t++)
            {
                var v = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
                if (v.LengthSquared() < 1e-12f) v = Vector3.One; v.Normalize();
                Vector3 w = (v * 2f - Vector3.One) * SpinScale;                              // part.gd:32
                float wait = Lifetime + LifetimeRandom * (float)rng.NextDouble();           // part.gd:33
                Spawn(t, feet + Vector3.Transform(PartLocal[t] + DeathOffset, R), q, new Vector3(0, LaunchSpeed, 0), w, wait);
            }
            foreach (float sx in new[] { 0.803977f, -1.11386f }) SparkBurst?.Invoke(feet + Vector3.Transform(new Vector3(sx, 0.987653f, 0) + DeathOffset, R));   // DetachSpark1/2
        }

        /// <summary>One part (test hook and <see cref="Explode"/>).</summary>
        public Part Spawn(int type, Vector3 pos, Quaternion rot, Vector3 v, Vector3 w, float wait)
        {
            Part p = null; for (int i = 0; i < Capacity; i++) if (!_parts[i].Alive) { p = _parts[i]; break; }
            if (p == null) return null;
            TotalSpawned++;
            p.Type = type; p.Spheres = BuildShapes(type, out p.InvInertia); p.AngDamp = PartAngDamp[type];
            p.Alive = true; p.Fading = p.Dying = p.Sleeping = false; p.Pos = p.PrevPos = pos; p.Rot = p.PrevRot = rot; p.V = v; p.W = w;
            p.Wait = wait; p.Elapsed = p.Counter = p.Fade = p.DieTimer = p.SleepT = 0f; p.Touching = 0;
            return p;
        }

        Vector3 InvIWorld(Part p, Vector3 v)
        {
            Vector3 l = Vector3.Transform(v, Quaternion.Conjugate(p.Rot));
            return Vector3.Transform(l * p.InvInertia, p.Rot);
        }

        public void Update(float dt)
        {
            for (int i = 0; i < Capacity; i++)
            {
                Part p = _parts[i]; if (!p.Alive) continue;
                p.PrevPos = p.Pos; p.PrevRot = p.Rot;
                p.Elapsed += dt;
                if (!p.Fading && p.Elapsed >= p.Wait) { p.Fading = true; p.Counter = 0f; }                // part.gd:36 timer -> set_process(true)
                if (p.Dying) { p.DieTimer -= dt; if (p.DieTimer <= 0f) { p.Alive = false; continue; } }   // part.gd:50-51 queue_free after 0.2 s
                else if (p.Fading)
                {
                    p.Fade = MathF.Pow(p.Counter / DisappearingTime, 2f); p.Counter += dt;                 // part.gd:39-40
                    if (p.Counter >= DisappearingTime - PuffDelay) { p.Dying = true; p.DieTimer = PuffDelay; Puff?.Invoke(p.Pos); }
                }
                if (!p.Sleeping) for (int k = 0; k < SubSteps && !p.Sleeping; k++) Simulate(p, dt / SubSteps);   // 4 x 240 Hz: shallower penetration, better restitution
            }
        }

        void Simulate(Part p, float dt)
        {
            p.V.Y -= Gravity * dt;
            p.V *= Math.Max(0f, 1f - LinearDamp * dt); p.W *= Math.Max(0f, 1f - p.AngDamp * dt);
            p.Pos += p.V * dt;
            float wl = p.W.Length();
            if (wl > 1e-6f) { Quaternion dq = Quaternion.CreateFromAxisAngle(p.W / wl, wl * dt); p.Rot = Quaternion.Normalize(dq * p.Rot); }   // world-space spin: q' = dq * q
            // contacts of every sphere with the level
            int n = 0;
            foreach (var s in p.Spheres)
            {
                Vector3 c = p.Pos + Vector3.Transform(s.C, p.Rot);
                _tmp.Clear(); _world.Contacts(c, c, s.R, _tmp); if (_tmp.Count == 0) continue;
                int best = 0; for (int k = 1; k < _tmp.Count; k++) if (_tmp[k].Depth > _tmp[best].Depth) best = k;
                for (int k = 0; k < _tmp.Count; k++)
                {
                    if (k != best && Vector3.Dot(_tmp[k].Normal, _tmp[best].Normal) > 0.95f) continue;   // coplanar neighbour triangle: same constraint
                    if (n == _c.Length) Array.Resize(ref _c, n * 2);
                    Vector3 nrm = _tmp[k].Normal; Vector3 point = c - nrm * (s.R - _tmp[k].Depth);
                    _c[n++] = new Contact { R = point - p.Pos, N = nrm, Depth = _tmp[k].Depth };
                }
            }
            foreach (var s in p.Spheres)
            {
                if (Obstacles.Count == 0) break;
                Vector3 c = p.Pos + Vector3.Transform(s.C, p.Rot);
                foreach (var o in Obstacles)
                {
                    Vector3 ab = o.B - o.A; float l2 = ab.LengthSquared();
                    float t = l2 > 1e-12f ? MathHelper.Clamp(Vector3.Dot(c - o.A, ab) / l2, 0f, 1f) : 0f;
                    Vector3 d = c - (o.A + ab * t); float dist = d.Length(), depth = o.R + s.R - dist;
                    if (depth <= 0f) continue;
                    Vector3 nrm = dist > 1e-6f ? d / dist : Vector3.Up;
                    if (n == _c.Length) Array.Resize(ref _c, n * 2);
                    _c[n++] = new Contact { R = c - nrm * s.R - p.Pos, N = nrm, Depth = depth };
                }
            }
            p.Touching = n;
            if (n > 0)
            {
                Vector3 push = Vector3.Zero;
                for (int k = 0; k < n; k++)
                {
                    float d = Math.Max(0f, _c[k].Depth - 0.002f);
                    push += _c[k].N * d * 0.25f;
                    Vector3 vr = p.V + Vector3.Cross(p.W, _c[k].R); float vn = Vector3.Dot(vr, _c[k].N);
                    _c[k].Target = vn < -1f ? -Restitution * vn : 0f;      // bounce only for impacts above 1 m/s (resting contact stays at rest)
                }
                if (push.Length() > 0.3f) push *= 0.3f / push.Length();
                p.Pos += push;
                for (int it = 0; it < 8; it++)
                    for (int k = 0; k < n; k++) Solve(p, ref _c[k]);
            }
            // sleeping (Godot thresholds): slow and touching for 0.5 s
            if (n > 0 && p.V.Length() < SleepLinear && p.W.Length() < SleepAngular) { p.SleepT += dt; if (p.SleepT >= SleepTime) { p.Sleeping = true; p.V = p.W = Vector3.Zero; } }
            else p.SleepT = 0f;
        }

        void Solve(Part p, ref Contact c)
        {
            Vector3 r = c.R, nrm = c.N;
            Vector3 vr = p.V + Vector3.Cross(p.W, r); float vn = Vector3.Dot(vr, nrm);
            float denom = 1f + Vector3.Dot(nrm, Vector3.Cross(InvIWorld(p, Vector3.Cross(r, nrm)), r));
            float j = (c.Target - vn) / denom, acc = Math.Max(c.AccN + j, 0f); j = acc - c.AccN; c.AccN = acc;
            Apply(p, nrm * j, r);
            vr = p.V + Vector3.Cross(p.W, r); Vector3 vt = vr - nrm * Vector3.Dot(vr, nrm); float vtl = vt.Length();
            if (vtl < 1e-6f) return;
            Vector3 tdir = vt / vtl;
            float dt2 = 1f + Vector3.Dot(tdir, Vector3.Cross(InvIWorld(p, Vector3.Cross(r, tdir)), r));
            float jt = -vtl / dt2, maxT = Friction * c.AccN, accT = MathHelper.Clamp(c.AccT + jt, -maxT, maxT); jt = accT - c.AccT; c.AccT = accT;
            Apply(p, tdir * jt, r);
        }

        void Apply(Part p, Vector3 impulse, Vector3 r) { p.V += impulse; p.W += InvIWorld(p, Vector3.Cross(r, impulse)); }

        /// <summary>Render transform (body frame -> world) interpolated between the last two ticks.</summary>
        public static Matrix BodyWorld(Part p, float alpha) =>
            Matrix.CreateFromQuaternion(Quaternion.Slerp(p.PrevRot, p.Rot, alpha)) * Matrix.CreateTranslation(Vector3.Lerp(p.PrevPos, p.Pos, alpha));
    }
}
