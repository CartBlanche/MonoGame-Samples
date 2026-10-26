using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Shooter.Rendering.Particles
{
    /// <summary>
    /// Runtime simulation of one Unity ParticleSystem (CPU). Works in the MonoGame world: Unity shape / force / velocity vectors are
    /// converted with F = diag(1, 1, -1) when they are read from the data (see ParticleSystemData). Supported Unity modules:
    /// Main, Emission (rate over time, rate over distance, bursts), Shape (sphere, hemisphere, cone, box, circle, rectangle, edge),
    /// Velocity over lifetime (linear), Force over lifetime, Limit velocity over lifetime, Inherit velocity, Noise (single frequency
    /// value noise), Color over lifetime, Size over lifetime, Rotation over lifetime, Texture sheet animation (grid).
    /// </summary>
    public sealed class ParticleSystemInstance
    {
        private struct Particle
        {
            public Vector3 Position;      // in simulation space (local or world)
            public Vector3 Velocity;      // in simulation space
            public Vector3 StartSize;
            public Vector3 Rotation;      // radians (z is the billboard roll)
            public Vector4 StartColor;    // gamma
            public float Age;
            public float Lifetime;
            public uint Seed;
            public float StartFrame;
            public int Trail;             // index into _trails, -1 = none
        }

        /// <summary>Trails module: the ribbon of one particle. It outlives the particle (unless dieWithParticles) and fades out from its tail.</summary>
        private sealed class Trail
        {
            public readonly List<TrailPoint> Points = new List<TrailPoint>();
            public Vector3 Head;
            public float HeadWidth;
            public bool Alive = true;     // the particle still exists: the ribbon ends at its current position
            public float Age01;           // particle age when it last moved (colour over lifetime)
            public float Rand;
            public float Lifetime = 1f;   // seconds a vertex lives
        }

        private struct TrailPoint
        {
            public Vector3 Pos;           // trail space: world when the module's worldSpace is on, else the system's local space
            public float Birth;
            public float Width;
        }

        private readonly List<Trail> _trails = new List<Trail>();   // free slots are null
        private int _liveTrails;
        private float _trailClock;

        /// <summary>The renderer's trail material (second entry of the renderer's materials); resolved by the manager.</summary>
        public Material TrailMaterial { get; set; }
        public bool HasTrails => _liveTrails > 0;

        private Vector3 TrailToWorld(Vector3 p) { return Data.TrailWorldSpace ? p : Vector3.Transform(p, World); }

        private Particle[] _particles;
        private int _count;
        private readonly Random _rng;
        private uint _nextSeed;

        public ParticleSystemData Data { get; }
        public Material Material { get; set; }
        public string Name => Data.Name;

        /// <summary>System-to-world matrix (MonoGame space).</summary>
        public Matrix World { get; private set; } = Matrix.Identity;

        public bool Visible { get; set; } = true;

        /// <summary>Part of the first-person weapon: drawn in the weapon pass (weapon camera, over the scene and the viewmodel) like a Unity object on the weapon layer.</summary>
        public bool WeaponLayer { get; set; }

        /// <summary>Weapon-layer systems only: draw without a depth test, over the viewmodel (the muzzle flash sits at the barrel tip, inside the geometry it should light up).</summary>
        public bool DrawOverWeapon { get; set; }

        /// <summary>Script-driven systems that are Play()ed again and again (an enemy's alert, steam and sparks): never auto-removed by the manager when idle.</summary>
        public bool KeepWhenFinished { get; set; }

        /// <summary>ParticleSystem.emission.rateOverTimeMultiplier set from script: replaces a constant rate, scales a curve (relative to its scalar). Null = authored.</summary>
        public float? RateOverTimeMultiplier { get; set; }
        public bool IsPlaying { get; private set; }
        public bool IsEmitting { get; private set; }
        public float SystemTime { get; private set; }
        public int ParticleCount => _count;

        /// <summary>World-space bounds of the live particles (valid when ParticleCount &gt; 0).</summary>
        public BoundingBox Bounds { get; private set; }

        /// <summary>True while the system is emitting or still has particles alive.</summary>
        public bool IsAlive => IsEmitting || _count > 0 || _liveTrails > 0;

        private float _delay;
        private float _emitAccumulator;
        private float _distanceAccumulator;
        private float _prevBurstTime;
        private Vector3 _lastWorldPos;
        private Vector3 _emitterVelocity;
        private bool _hasLastPos;

        public ParticleSystemInstance(ParticleSystemData data, Material material, Matrix world, int seed = 0)
        {
            Data = data;
            Material = material;
            World = ApplyScalingMode(world);
            _rng = new Random(seed != 0 ? seed : Environment.TickCount ^ data.Name.GetHashCode());
            _nextSeed = (uint)_rng.Next();
            _particles = new Particle[Math.Min(data.MaxParticles, 4096)];
            if (data.PlayOnAwake)
                Play();
        }

        // ------------------------------------------------------------------ control

        public void SetWorld(Matrix world)
        {
            World = ApplyScalingMode(world);
        }

        /// <summary>
        /// ScalingMode Local (Unity "Local"): only the system's own transform scale applies, the parents' scale is ignored: positions, speeds and
        /// sizes are all in the system's own units (the blaster's steam puffs are 0.8 units, not 0.8 x the weapon's 0.45 scale). Hierarchy (the
        /// default) and Shape leave the matrix as it is.
        /// </summary>
        private Matrix ApplyScalingMode(Matrix m)
        {
            if (Data.ScalingMode != 1)
                return m;
            Vector3 x = new Vector3(m.M11, m.M12, m.M13), y = new Vector3(m.M21, m.M22, m.M23), z = new Vector3(m.M31, m.M32, m.M33);
            if (x.LengthSquared() < 1e-12f || y.LengthSquared() < 1e-12f || z.LengthSquared() < 1e-12f)
                return m;
            x = Vector3.Normalize(x) * Data.LocalScale.X;
            y = Vector3.Normalize(y) * Data.LocalScale.Y;
            z = Vector3.Normalize(z) * Data.LocalScale.Z;
            return new Matrix(x.X, x.Y, x.Z, 0f, y.X, y.Y, y.Z, 0f, z.X, z.Y, z.Z, 0f, m.M41, m.M42, m.M43, 1f);
        }

        public void Play()
        {
            IsPlaying = true;
            IsEmitting = true;
            SystemTime = 0f;
            _prevBurstTime = -1e-5f;
            _emitAccumulator = 0f;
            _distanceAccumulator = 0f;
            _delay = Data.StartDelay.Evaluate(0f, (float)_rng.NextDouble());
            _hasLastPos = false;
            if (Data.Prewarm && Data.Looping)
            {
                float step = 1f / 30f;
                for (float t = 0; t < Data.Duration; t += step)
                    Update(step);
            }
        }

        /// <summary>
        /// Unity's <c>emission.enabled</c> set from a script every frame: new particles stop or start, the system keeps its time (no burst fires again)
        /// and the live particles carry on.
        /// </summary>
        public void SetEmissionEnabled(bool enabled)
        {
            if (enabled && !IsEmitting)
            {
                IsEmitting = true;
                IsPlaying = true;
                _hasLastPos = false;
            }
            else if (!enabled)
            {
                IsEmitting = false;
            }
        }

        public void Stop(bool clear = false)
        {
            IsEmitting = false;
            if (clear)
            {
                _count = 0;
                ClearTrails();
            }
        }

        public void Clear()
        {
            _count = 0;
            ClearTrails();
        }

        private void ClearTrails()
        {
            _trails.Clear();
            _liveTrails = 0;
        }

        /// <summary>Emit <paramref name="count"/> particles immediately (Unity ParticleSystem.Emit).</summary>
        public void Emit(int count)
        {
            Spawn(count, SystemTime / Data.Duration);
        }

        // ------------------------------------------------------------------ helpers

        private float Rand()
        {
            return (float)_rng.NextDouble();
        }

        private static float Hash01(uint seed, int salt)
        {
            uint h = seed ^ (uint)(salt * 0x9E3779B1u);
            h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        private static Vector3 FlipZ(Vector3 v)
        {
            return new Vector3(v.X, v.Y, -v.Z);
        }

        private Vector3 RandomUnit()
        {
            float z = Rand() * 2f - 1f;
            float a = Rand() * MathHelper.TwoPi;
            float r = (float)Math.Sqrt(Math.Max(0f, 1f - z * z));
            return new Vector3(r * (float)Math.Cos(a), r * (float)Math.Sin(a), z);
        }

        private Matrix WorldRotationOnly()
        {
            Matrix m = World;
            Vector3 sx = new Vector3(m.M11, m.M12, m.M13);
            Vector3 sy = new Vector3(m.M21, m.M22, m.M23);
            Vector3 sz = new Vector3(m.M31, m.M32, m.M33);
            sx = sx.LengthSquared() > 1e-12f ? Vector3.Normalize(sx) : Vector3.UnitX;
            sy = sy.LengthSquared() > 1e-12f ? Vector3.Normalize(sy) : Vector3.UnitY;
            sz = sz.LengthSquared() > 1e-12f ? Vector3.Normalize(sz) : Vector3.UnitZ;
            return new Matrix(sx.X, sx.Y, sx.Z, 0, sy.X, sy.Y, sy.Z, 0, sz.X, sz.Y, sz.Z, 0, 0, 0, 0, 1);
        }

        private float AverageScale()
        {
            Matrix m = World;
            float sx = new Vector3(m.M11, m.M12, m.M13).Length();
            float sy = new Vector3(m.M21, m.M22, m.M23).Length();
            float sz = new Vector3(m.M31, m.M32, m.M33).Length();
            return (sx + sy + sz) / 3f;
        }

        // ------------------------------------------------------------------ shape

        /// <summary>Samples the shape in Unity local axes (+Z is the emission axis), returns position and direction.</summary>
        private void SampleShape(out Vector3 pos, out Vector3 dir)
        {
            var d = Data;
            pos = Vector3.Zero;
            dir = Vector3.UnitZ;
            if (!d.ShapeEnabled)
                return;

            float radius = d.ShapeRadius;
            float thickness = MathHelper.Clamp(d.ShapeRadiusThickness, 0f, 1f);
            float arc = MathHelper.ToRadians(d.ShapeArc);

            switch (d.Shape)
            {
                case ParticleShapeType.Sphere:
                case ParticleShapeType.SphereShell:
                case ParticleShapeType.Hemisphere:
                case ParticleShapeType.HemisphereShell:
                {
                    Vector3 u = RandomUnit();
                    bool hemi = d.Shape == ParticleShapeType.Hemisphere || d.Shape == ParticleShapeType.HemisphereShell;
                    if (hemi) u.Z = Math.Abs(u.Z);
                    bool shell = d.Shape == ParticleShapeType.SphereShell || d.Shape == ParticleShapeType.HemisphereShell;
                    float rr = shell ? 1f : MathHelper.Lerp(1f, (float)Math.Pow(Rand(), 1.0 / 3.0), thickness);
                    pos = u * (radius * rr);
                    dir = u;
                    break;
                }
                case ParticleShapeType.Cone:
                case ParticleShapeType.ConeShell:
                case ParticleShapeType.ConeVolume:
                case ParticleShapeType.ConeVolumeShell:
                {
                    float phi = Rand() * arc;
                    bool shell = d.Shape == ParticleShapeType.ConeShell || d.Shape == ParticleShapeType.ConeVolumeShell;
                    float frac = shell ? 1f : MathHelper.Lerp(1f, (float)Math.Sqrt(Rand()), thickness);
                    float r = radius * frac;
                    float c = (float)Math.Cos(phi), s = (float)Math.Sin(phi);
                    pos = new Vector3(r * c, r * s, 0f);
                    float ang = MathHelper.ToRadians(d.ShapeAngle) * frac;
                    float sa = (float)Math.Sin(ang), ca = (float)Math.Cos(ang);
                    dir = new Vector3(c * sa, s * sa, ca);
                    if (d.Shape == ParticleShapeType.ConeVolume || d.Shape == ParticleShapeType.ConeVolumeShell)
                        pos += dir * (Rand() * d.ShapeLength);
                    break;
                }
                case ParticleShapeType.Box:
                case ParticleShapeType.BoxShell:
                case ParticleShapeType.BoxEdge:
                    pos = new Vector3(Rand() - 0.5f, Rand() - 0.5f, Rand() - 0.5f);
                    dir = Vector3.UnitZ;
                    break;
                case ParticleShapeType.Circle:
                case ParticleShapeType.CircleEdge:
                {
                    float phi = Rand() * arc;
                    float frac = d.Shape == ParticleShapeType.CircleEdge ? 1f : MathHelper.Lerp(1f, (float)Math.Sqrt(Rand()), thickness);
                    float r = radius * frac;
                    pos = new Vector3(r * (float)Math.Cos(phi), r * (float)Math.Sin(phi), 0f);
                    dir = new Vector3((float)Math.Cos(phi), (float)Math.Sin(phi), 0f);
                    break;
                }
                case ParticleShapeType.SingleSidedEdge:
                    pos = new Vector3((Rand() * 2f - 1f) * radius, 0f, 0f);
                    dir = Vector3.UnitY;
                    break;
                case ParticleShapeType.Rectangle:
                    pos = new Vector3(Rand() - 0.5f, Rand() - 0.5f, 0f);
                    dir = Vector3.UnitZ;
                    break;
                case ParticleShapeType.Donut:
                {
                    float phi = Rand() * arc;
                    float theta = Rand() * MathHelper.TwoPi;
                    float rr = d.DonutRadius * (float)Math.Sqrt(Rand());
                    Vector3 ring = new Vector3((float)Math.Cos(phi), (float)Math.Sin(phi), 0f);
                    pos = ring * (radius + rr * (float)Math.Cos(theta)) + new Vector3(0, 0, rr * (float)Math.Sin(theta));
                    dir = ring;
                    break;
                }
                default:
                    break; // mesh / sprite shapes: emit from the transform origin along +Z
            }

            // shape transform (Unity: position/rotation/scale of the shape module)
            pos *= d.ShapeScale;
            if (d.ShapeRotation != Vector3.Zero)
            {
                Quaternion q = UnityEuler(d.ShapeRotation);
                pos = Vector3.Transform(pos, q);
                dir = Vector3.Transform(dir, q);
            }
            pos += d.ShapePosition;

            if (d.SphericalDirectionAmount > 0f && pos.LengthSquared() > 1e-10f)
                dir = Vector3.Lerp(dir, Vector3.Normalize(pos), d.SphericalDirectionAmount);
            if (d.RandomDirectionAmount > 0f)
                dir = Vector3.Lerp(dir, RandomUnit(), d.RandomDirectionAmount);
            if (d.RandomPositionAmount > 0f)
                pos += RandomUnit() * (d.RandomPositionAmount * Rand());
            if (dir.LengthSquared() > 1e-10f)
                dir = Vector3.Normalize(dir);
        }

        /// <summary>Unity Quaternion.Euler (degrees, applied Z then X then Y) in Unity space.</summary>
        private static Quaternion UnityEuler(Vector3 deg)
        {
            Vector3 r = new Vector3(MathHelper.ToRadians(deg.X), MathHelper.ToRadians(deg.Y), MathHelper.ToRadians(deg.Z));
            Quaternion qx = new Quaternion((float)Math.Sin(r.X / 2), 0, 0, (float)Math.Cos(r.X / 2));
            Quaternion qy = new Quaternion(0, (float)Math.Sin(r.Y / 2), 0, (float)Math.Cos(r.Y / 2));
            Quaternion qz = new Quaternion(0, 0, (float)Math.Sin(r.Z / 2), (float)Math.Cos(r.Z / 2));
            return Hamilton(qy, Hamilton(qx, qz));
        }

        private static Quaternion Hamilton(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
        }

        // ------------------------------------------------------------------ spawning

        private void Spawn(int count, float t01)
        {
            var d = Data;
            Matrix rotOnly = d.WorldSpace ? WorldRotationOnly() : Matrix.Identity;
            float scale = AverageScale();
            for (int n = 0; n < count; n++)
            {
                if (_count >= _particles.Length)
                    break;

                SampleShape(out Vector3 pos, out Vector3 dir);
                pos = FlipZ(pos);
                dir = FlipZ(dir);

                var p = new Particle();
                p.Seed = _nextSeed;
                _nextSeed = _nextSeed * 1664525u + 1013904223u;

                float rand = Rand();
                p.Lifetime = Math.Max(0.001f, d.StartLifetime.Evaluate(t01, Rand()));
                float speed = d.StartSpeed.Evaluate(t01, Rand());
                float sx = d.StartSizeX.Evaluate(t01, rand);
                p.StartSize = d.Size3D
                    ? new Vector3(sx, d.StartSizeY.Evaluate(t01, Rand()), d.StartSizeZ.Evaluate(t01, Rand()))
                    : new Vector3(sx, sx, sx);
                float rz = d.StartRotationZ.Evaluate(t01, Rand());
                if (d.RandomizeRotationDirection > 0f && Rand() < d.RandomizeRotationDirection)
                    rz = -rz;
                p.Rotation = d.Rotation3D
                    ? new Vector3(d.StartRotationX.Evaluate(t01, Rand()), d.StartRotationY.Evaluate(t01, Rand()), rz)
                    : new Vector3(0, 0, rz);
                p.StartColor = d.StartColor.Evaluate(t01, Rand());
                p.StartFrame = d.UvEnabled ? d.UvStartFrame.Evaluate(t01, Rand()) : 0f;

                Vector3 vel = dir * speed;
                if (d.InheritVelocityEnabled && d.InheritVelocityMode == 0)
                    vel += InheritedVelocity(t01, rand);

                if (d.WorldSpace)
                {
                    pos = Vector3.Transform(pos, World);
                    vel = Vector3.TransformNormal(vel, rotOnly);
                }
                else
                {
                    // local space: shapes are authored in the system's own units
                }
                p.Position = pos;
                p.Velocity = vel;
                p.Trail = -1;
                if (d.TrailsEnabled && Rand() <= d.TrailRatio)
                    p.Trail = NewTrail(Rand());
                _particles[_count++] = p;
            }
        }

        private Vector3 InheritedVelocity(float t01, float rand)
        {
            float k = Data.InheritVelocity.Evaluate(t01, rand);
            Vector3 v = _emitterVelocity * k;
            if (!Data.WorldSpace)
            {
                Matrix inv = Matrix.Invert(WorldRotationOnly());
                v = Vector3.TransformNormal(v, inv);
            }
            return v;
        }

        // ------------------------------------------------------------------ update

        /// <summary>Advances the simulation after moving the system to <paramref name="world"/>.</summary>
        public void Update(float dt, Matrix world)
        {
            World = ApplyScalingMode(world);
            Update(dt);
        }

        public void Update(float dtReal)
        {
            var d = Data;
            if (!IsPlaying && _count == 0 && _liveTrails == 0)
                return;

            float dt = dtReal * d.SimulationSpeed;
            if (dt <= 0f)
                return;
            _trailClock += dt;

            Vector3 worldPos = World.Translation;
            if (_hasLastPos)
                _emitterVelocity = (worldPos - _lastWorldPos) / Math.Max(dt, 1e-5f);
            float moved = _hasLastPos ? (worldPos - _lastWorldPos).Length() : 0f;
            _lastWorldPos = worldPos;
            _hasLastPos = true;

            // ---- emission
            if (IsEmitting && d.EmissionEnabled)
            {
                if (_delay > 0f)
                {
                    _delay -= dt;
                }
                else
                {
                    float prev = SystemTime;
                    SystemTime += dt;
                    float t01 = SystemTime / d.Duration;

                    float rate = d.RateOverTime.Evaluate(Math.Min(t01, 1f), Rand());
                    if (RateOverTimeMultiplier.HasValue)
                    {
                        float scalar = d.RateOverTime.Scalar;
                        rate = d.RateOverTime.Mode == CurveMode.Constant ? RateOverTimeMultiplier.Value : (Math.Abs(scalar) > 1e-6f ? rate * RateOverTimeMultiplier.Value / scalar : 0f);
                    }
                    _emitAccumulator += rate * dt;
                    if (d.RateOverDistance.Mode != CurveMode.Constant || d.RateOverDistance.Scalar != 0f)
                        _distanceAccumulator += d.RateOverDistance.Evaluate(Math.Min(t01, 1f), Rand()) * moved;

                    int emit = (int)_emitAccumulator;
                    if (emit > 0)
                        _emitAccumulator -= emit;
                    int emitDist = (int)_distanceAccumulator;
                    if (emitDist > 0)
                        _distanceAccumulator -= emitDist;

                    // bursts: every burst time (time + k * repeatInterval) crossed during this step fires once
                    foreach (ParticleBurst b in d.Bursts)
                    {
                        int cycles = b.CycleCount <= 0 ? 1024 : b.CycleCount;
                        for (int k = 0; k < cycles; k++)
                        {
                            float tk = b.Time + k * b.RepeatInterval;
                            if (tk > SystemTime)
                                break;
                            if (tk > _prevBurstTime && Rand() <= b.Probability)
                                emit += (int)Math.Round(b.Count.Evaluate(Math.Min(t01, 1f), Rand()));
                        }
                    }
                    _prevBurstTime = SystemTime;

                    if (emit + emitDist > 0)
                        Spawn(emit + emitDist, Math.Min(t01, 1f));

                    if (SystemTime >= d.Duration)
                    {
                        if (d.Looping)
                        {
                            SystemTime -= d.Duration;
                            _prevBurstTime = -1e-5f;
                        }
                        else
                        {
                            IsEmitting = false;
                        }
                    }
                }
            }

            if (_count == 0)
            {
                AgeTrails();
                if (_liveTrails > 0)
                    ExpandBoundsForTrails();
                if (!IsEmitting && _liveTrails == 0)
                    IsPlaying = false;
                return;
            }

            // ---- simulation of live particles
            Matrix rotOnly = WorldRotationOnly();
            Matrix invRot = Matrix.Invert(rotOnly);
            Vector3 gravityWorld = new Vector3(0f, -9.81f, 0f);
            float gm = d.GravityModifier.Evaluate(Math.Min(SystemTime / d.Duration, 1f), 0.5f);
            Vector3 gravity = d.WorldSpace ? gravityWorld * gm : Vector3.TransformNormal(gravityWorld * gm, invRot);
            float sizeScale = AverageScale();
            Matrix invWorld = d.TrailsEnabled && !d.TrailWorldSpace && d.WorldSpace ? Matrix.Invert(World) : Matrix.Identity;

            Vector3 bmin = new Vector3(float.MaxValue), bmax = new Vector3(float.MinValue);
            int alive = 0;
            for (int i = 0; i < _count; i++)
            {
                Particle p = _particles[i];
                p.Age += dt;
                if (p.Age >= p.Lifetime)
                {
                    EndTrail(p.Trail);
                    continue;
                }

                float t = p.Age / p.Lifetime;
                float r0 = Hash01(p.Seed, 1);

                Vector3 accel = gravity;
                if (d.ForceEnabled)
                {
                    Vector3 f = FlipZ(new Vector3(d.ForceX.Evaluate(t, Hash01(p.Seed, 2)), d.ForceY.Evaluate(t, Hash01(p.Seed, 3)), d.ForceZ.Evaluate(t, Hash01(p.Seed, 4))));
                    if (d.ForceWorldSpace != d.WorldSpace)
                        f = d.ForceWorldSpace ? Vector3.TransformNormal(f, invRot) : Vector3.TransformNormal(f, rotOnly);
                    accel += f;
                }
                p.Velocity += accel * dt;

                if (d.ClampEnabled)
                    p.Velocity = LimitVelocity(p.Velocity, t, p.Seed, dt, p.StartSize.X, rotOnly, invRot);

                Vector3 move = p.Velocity;
                if (d.VelocityEnabled)
                {
                    Vector3 v = FlipZ(new Vector3(d.VelocityX.Evaluate(t, Hash01(p.Seed, 5)), d.VelocityY.Evaluate(t, Hash01(p.Seed, 6)), d.VelocityZ.Evaluate(t, Hash01(p.Seed, 7))));
                    if (d.VelocityWorldSpace != d.WorldSpace)
                        v = d.VelocityWorldSpace ? Vector3.TransformNormal(v, invRot) : Vector3.TransformNormal(v, rotOnly);
                    move += v;
                    move *= d.SpeedModifier.Evaluate(t, Hash01(p.Seed, 8));
                }
                p.Position += move * dt;

                if (d.NoiseEnabled)
                    p.Position += Noise(p, t) * dt;

                if (d.RotationEnabled)
                {
                    float w = d.RotationZ.Evaluate(t, Hash01(p.Seed, 9));
                    p.Rotation.Z += w * dt;
                    if (d.RotationSeparateAxes)
                    {
                        p.Rotation.X += d.RotationX.Evaluate(t, Hash01(p.Seed, 10)) * dt;
                        p.Rotation.Y += d.RotationY.Evaluate(t, Hash01(p.Seed, 11)) * dt;
                    }
                }

                _particles[alive++] = p;

                Vector3 wp = d.WorldSpace ? p.Position : Vector3.Transform(p.Position, World);
                if (p.Trail >= 0)
                    UpdateTrailHead(p, wp, t, sizeScale, invWorld);
                float ext = Math.Max(p.StartSize.X, Math.Max(p.StartSize.Y, p.StartSize.Z)) * sizeScale;
                bmin = Vector3.Min(bmin, wp - new Vector3(ext));
                bmax = Vector3.Max(bmax, wp + new Vector3(ext));
            }
            _count = alive;
            AgeTrails();
            if (_count > 0)
            {
                Bounds = new BoundingBox(bmin, bmax);
                if (_liveTrails > 0)
                    ExpandBoundsForTrails();
            }
            else if (_liveTrails > 0)
                ExpandBoundsForTrails();
            else if (!IsEmitting)
                IsPlaying = false;
        }

        // ------------------------------------------------------------------ trails

        private int NewTrail(float rand)
        {
            var tr = new Trail { Rand = rand };
            _liveTrails++;
            for (int i = 0; i < _trails.Count; i++)
            {
                if (_trails[i] == null)
                {
                    _trails[i] = tr;
                    return i;
                }
            }
            _trails.Add(tr);
            return _trails.Count - 1;
        }

        private void FreeTrail(int index)
        {
            if (index >= 0 && index < _trails.Count && _trails[index] != null)
            {
                _trails[index] = null;
                _liveTrails--;
            }
        }

        // the particle died: the ribbon stays and fades (dieWithParticles removes it at once)
        private void EndTrail(int index)
        {
            if (index < 0 || index >= _trails.Count || _trails[index] == null)
                return;
            if (Data.TrailDieWithParticles)
                FreeTrail(index);
            else
                _trails[index].Alive = false;
        }

        private void UpdateTrailHead(Particle p, Vector3 worldPos, float t, float sizeScale, Matrix invWorld)
        {
            var d = Data;
            Trail tr = _trails[p.Trail];
            float size = p.StartSize.X;
            if (d.SizeEnabled)
                size *= d.SizeX.Evaluate(t, Hash01(p.Seed, 20));
            size *= d.WorldSpace ? 1f : sizeScale;
            // trail space: world, or the system's local space (a particle simulated in world space is brought back with the inverse matrix)
            Vector3 at = d.TrailWorldSpace ? worldPos : (d.WorldSpace ? Vector3.Transform(worldPos, invWorld) : p.Position);
            tr.Head = at;
            tr.HeadWidth = d.TrailSizeAffectsWidth ? size : 1f;
            tr.Age01 = t;
            tr.Lifetime = Math.Max(0.001f, d.TrailLifetime.Evaluate(t, tr.Rand) * (d.TrailSizeAffectsLifetime ? size : 1f));
            if (tr.Points.Count == 0 || Vector3.Distance(TrailToWorld(at), TrailToWorld(tr.Points[tr.Points.Count - 1].Pos)) >= d.TrailMinVertexDistance)
                tr.Points.Add(new TrailPoint { Pos = at, Birth = _trailClock, Width = tr.HeadWidth });
        }

        // vertices older than the trail lifetime go; a ribbon whose particle is gone and has no vertices left is freed
        private void AgeTrails()
        {
            for (int i = 0; i < _trails.Count; i++)
            {
                Trail tr = _trails[i];
                if (tr == null)
                    continue;
                int expired = 0;
                while (expired < tr.Points.Count && _trailClock - tr.Points[expired].Birth > tr.Lifetime)
                    expired++;
                if (expired > 0)
                    tr.Points.RemoveRange(0, expired);
                if (!tr.Alive && tr.Points.Count < 2)
                    FreeTrail(i);
            }
        }

        private void ExpandBoundsForTrails()
        {
            Vector3 bmin = new Vector3(float.MaxValue), bmax = new Vector3(float.MinValue);
            bool any = false;
            foreach (Trail tr in _trails)
            {
                if (tr == null)
                    continue;
                foreach (TrailPoint pt in tr.Points)
                {
                    float w = Math.Max(0.05f, pt.Width);
                    Vector3 wpos = TrailToWorld(pt.Pos);
                    bmin = Vector3.Min(bmin, wpos - new Vector3(w));
                    bmax = Vector3.Max(bmax, wpos + new Vector3(w));
                    any = true;
                }
                if (tr.Alive)
                {
                    Vector3 head = TrailToWorld(tr.Head);
                    bmin = Vector3.Min(bmin, head - new Vector3(0.05f));
                    bmax = Vector3.Max(bmax, head + new Vector3(0.05f));
                    any = true;
                }
            }
            if (!any)
                return;
            Bounds = _count > 0 ? new BoundingBox(Vector3.Min(Bounds.Min, bmin), Vector3.Max(Bounds.Max, bmax)) : new BoundingBox(bmin, bmax);
        }

        /// <summary>
        /// Camera-facing ribbon strips of every trail (two vertices per ribbon point, newest at the head). Colour = colour over lifetime (the particle's
        /// age) x colour over trail (0 head .. 1 tail), width = the point's width x width over trail.
        /// </summary>
        internal void BuildTrailGeometry(Vector3 camPos, List<ParticleVertex> verts, List<short> indices)
        {
            var d = Data;
            var pos = new List<Vector3>();
            var wid = new List<float>();
            foreach (Trail tr in _trails)
            {
                if (tr == null)
                    continue;
                pos.Clear();
                wid.Clear();
                foreach (TrailPoint pt in tr.Points)
                {
                    pos.Add(TrailToWorld(pt.Pos));
                    wid.Add(pt.Width);
                }
                Vector3 headWorld = TrailToWorld(tr.Head);
                if (tr.Alive && (pos.Count == 0 || Vector3.DistanceSquared(pos[pos.Count - 1], headWorld) > 1e-8f))
                {
                    pos.Add(headWorld);
                    wid.Add(tr.HeadWidth);
                }
                int n = pos.Count;
                if (n < 2 || verts.Count + n * 2 > short.MaxValue)
                    continue;

                float total = 0f;
                for (int i = 1; i < n; i++)
                    total += Vector3.Distance(pos[i], pos[i - 1]);
                if (total < 1e-6f)
                    continue;

                Vector4 life = d.TrailColorOverLifetime.Evaluate(tr.Age01, tr.Rand);
                int baseIndex = verts.Count;
                float cum = 0f;   // distance from the head
                for (int i = n - 1; i >= 0; i--)
                {
                    if (i < n - 1)
                        cum += Vector3.Distance(pos[i], pos[i + 1]);
                    float u = MathHelper.Clamp(cum / total, 0f, 1f);

                    Vector3 tangent = pos[Math.Min(n - 1, i + 1)] - pos[Math.Max(0, i - 1)];
                    tangent = tangent.LengthSquared() > 1e-10f ? Vector3.Normalize(tangent) : Vector3.UnitY;
                    Vector3 toCam = camPos - pos[i];
                    Vector3 side = Vector3.Cross(tangent, toCam);
                    side = side.LengthSquared() > 1e-10f ? Vector3.Normalize(side) : Vector3.UnitX;

                    Vector4 c = life * d.TrailColorOverTrail.Evaluate(u, tr.Rand);
                    if (d.ApplyActiveColorSpace)
                        c = new Vector4(PostProcess.PostProcessor.GammaToLinear(c.X), PostProcess.PostProcessor.GammaToLinear(c.Y), PostProcess.PostProcessor.GammaToLinear(c.Z), c.W);
                    float half = wid[i] * Math.Max(0f, d.TrailWidth.Evaluate(u, tr.Rand)) * 0.5f;
                    verts.Add(new ParticleVertex { Position = pos[i] - side * half, Color = c, UV = new Vector2(u, 0f) });
                    verts.Add(new ParticleVertex { Position = pos[i] + side * half, Color = c, UV = new Vector2(u, 1f) });
                }
                // vertices run head -> tail: 2 per point
                for (int i = 0; i < n - 1; i++)
                {
                    short a = (short)(baseIndex + i * 2), b = (short)(a + 1), c2 = (short)(a + 2), d2 = (short)(a + 3);
                    indices.Add(a); indices.Add(b); indices.Add(c2);
                    indices.Add(b); indices.Add(d2); indices.Add(c2);
                }
            }
        }

        private Vector3 LimitVelocity(Vector3 v, float t, uint seed, float dt, float size, Matrix rotOnly, Matrix invRot)
        {
            var d = Data;
            // drag
            float drag = d.ClampDrag.Evaluate(t, Hash01(seed, 12));
            if (drag > 0f)
            {
                float factor = drag;
                if (d.ClampDragBySize) factor *= size * size;
                if (d.ClampDragByVelocity) factor *= v.Length();
                v *= Math.Max(0f, 1f - factor * dt);
            }

            if (d.ClampSeparateAxis)
            {
                Vector3 lv = v;
                bool toWorld = d.ClampWorldSpace != d.WorldSpace;
                if (toWorld) lv = d.ClampWorldSpace ? Vector3.TransformNormal(v, rotOnly) : Vector3.TransformNormal(v, invRot);
                Vector3 lim = new Vector3(d.ClampX.Evaluate(t, Hash01(seed, 13)), d.ClampY.Evaluate(t, Hash01(seed, 14)), d.ClampZ.Evaluate(t, Hash01(seed, 15)));
                Vector3 c = new Vector3(Math.Sign(lv.X) * Math.Min(Math.Abs(lv.X), lim.X), Math.Sign(lv.Y) * Math.Min(Math.Abs(lv.Y), lim.Y), Math.Sign(lv.Z) * Math.Min(Math.Abs(lv.Z), lim.Z));
                lv = Vector3.Lerp(lv, c, d.ClampDampen);
                if (toWorld) lv = d.ClampWorldSpace ? Vector3.TransformNormal(lv, invRot) : Vector3.TransformNormal(lv, rotOnly);
                return lv;
            }

            float limit = d.ClampMagnitude.Evaluate(t, Hash01(seed, 16));
            float speed = v.Length();
            if (speed > limit && speed > 1e-6f)
                v = Vector3.Lerp(v, v * (limit / speed), d.ClampDampen);
            return v;
        }

        // ------------------------------------------------------------------ noise

        private static float ValueNoise(float x, float y, float z, uint salt)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y), zi = (int)Math.Floor(z);
            float fx = x - xi, fy = y - yi, fz = z - zi;
            float ux = fx * fx * (3 - 2 * fx), uy = fy * fy * (3 - 2 * fy), uz = fz * fz * (3 - 2 * fz);
            float C(int a, int b, int c)
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(c * 83492791) ^ salt;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return (h & 0xFFFF) / 32767.5f - 1f;
            }
            float x00 = MathHelper.Lerp(C(xi, yi, zi), C(xi + 1, yi, zi), ux);
            float x10 = MathHelper.Lerp(C(xi, yi + 1, zi), C(xi + 1, yi + 1, zi), ux);
            float x01 = MathHelper.Lerp(C(xi, yi, zi + 1), C(xi + 1, yi, zi + 1), ux);
            float x11 = MathHelper.Lerp(C(xi, yi + 1, zi + 1), C(xi + 1, yi + 1, zi + 1), ux);
            return MathHelper.Lerp(MathHelper.Lerp(x00, x10, uy), MathHelper.Lerp(x01, x11, uy), uz);
        }

        private Vector3 Noise(Particle p, float t)
        {
            var d = Data;
            float scroll = d.NoiseScrollSpeed.Evaluate(SystemTime / d.Duration, 0.5f) * SystemTime;
            float freq = d.NoiseFrequency;
            float amp = 1f, sum = 0f;
            Vector3 n = Vector3.Zero;
            float scale = freq;
            for (int o = 0; o < d.NoiseOctaves; o++)
            {
                float px = p.Position.X * scale + scroll, py = p.Position.Y * scale + scroll, pz = p.Position.Z * scale + scroll;
                n += new Vector3(ValueNoise(px, py, pz, 11), ValueNoise(px + 17.3f, py - 5.1f, pz + 3.7f, 23), ValueNoise(px - 9.2f, py + 11.9f, pz - 4.4f, 37)) * amp;
                sum += amp;
                amp *= d.NoiseOctaveMultiplier;
                scale *= d.NoiseOctaveScale;
            }
            n /= Math.Max(sum, 1e-4f);
            float sx = d.NoiseStrengthX.Evaluate(t, 0.5f);
            Vector3 strength = d.NoiseSeparateAxes ? new Vector3(sx, d.NoiseStrengthY.Evaluate(t, 0.5f), d.NoiseStrengthZ.Evaluate(t, 0.5f)) : new Vector3(sx);
            return n * strength;
        }

        // ------------------------------------------------------------------ per-particle render state

        /// <summary>Per-particle data prepared for rendering.</summary>
        internal struct RenderParticle
        {
            public Vector3 Position;   // world
            public Vector3 Velocity;   // world
            public Vector3 Size;       // world units (full width)
            public Vector3 Rotation;
            public Vector4 Color;      // linear rgb + alpha
            public Vector2 FrameUv;    // top-left uv of the frame
            public Vector2 FrameSize;  // uv extent of the frame
            public float Age01;
        }

        internal int GetRenderParticles(List<RenderParticle> output)
        {
            var d = Data;
            output.Clear();
            float scale = d.WorldSpace ? 1f : AverageScale();
            Matrix rotOnly = WorldRotationOnly();
            for (int i = 0; i < _count; i++)
            {
                Particle p = _particles[i];
                float t = MathHelper.Clamp(p.Age / p.Lifetime, 0f, 1f);

                var rp = new RenderParticle();
                rp.Age01 = t;
                if (d.WorldSpace)
                {
                    rp.Position = p.Position;
                    rp.Velocity = p.Velocity;
                }
                else
                {
                    rp.Position = Vector3.Transform(p.Position, World);
                    rp.Velocity = Vector3.TransformNormal(p.Velocity, rotOnly);
                }

                Vector3 size = p.StartSize;
                if (d.SizeEnabled)
                {
                    float sx = d.SizeX.Evaluate(t, Hash01(p.Seed, 20));
                    if (d.SizeSeparateAxes)
                        size *= new Vector3(sx, d.SizeY.Evaluate(t, Hash01(p.Seed, 21)), d.SizeZ.Evaluate(t, Hash01(p.Seed, 22)));
                    else
                        size *= sx;
                }
                rp.Size = size * scale;
                rp.Rotation = p.Rotation;

                Vector4 c = p.StartColor;
                if (d.ColorEnabled)
                    c *= d.ColorOverLifetime.Evaluate(t, Hash01(p.Seed, 23));
                if (d.ApplyActiveColorSpace)
                    c = new Vector4(PostProcess.PostProcessor.GammaToLinear(c.X), PostProcess.PostProcessor.GammaToLinear(c.Y), PostProcess.PostProcessor.GammaToLinear(c.Z), c.W);
                rp.Color = c;

                // texture sheet animation
                rp.FrameUv = Vector2.Zero;
                rp.FrameSize = Vector2.One;
                if (d.UvEnabled && d.UvMode == 0)
                {
                    int tx = d.UvTilesX, ty = d.UvTilesY;
                    float frameCount = d.UvAnimationType == 1 ? tx : tx * ty;
                    float u;
                    if (d.UvTimeMode == 2)
                    {
                        u = p.StartFrame + p.Age * d.UvFps / frameCount;          // FPS mode: loops through the sheet
                        u -= (float)Math.Floor(u);
                    }
                    else
                    {
                        float time = t;
                        if (d.UvTimeMode == 1)
                        {
                            float range = Math.Max(1e-4f, d.UvSpeedRange.Y - d.UvSpeedRange.X);
                            time = MathHelper.Clamp((rp.Velocity.Length() - d.UvSpeedRange.X) / range, 0f, 1f);
                        }
                        float curve = d.UvFrameOverTime.Evaluate(time, Hash01(p.Seed, 24));
                        u = p.StartFrame + curve * d.UvCycles;
                        if (d.UvCycles > 1.0001f || p.StartFrame > 0f)
                            u -= (float)Math.Floor(u);
                        else
                            u = MathHelper.Clamp(u, 0f, 0.99999f);
                    }
                    float f = u;
                    int frame = (int)Math.Floor(f * frameCount);
                    frame = Math.Max(0, Math.Min((int)frameCount - 1, frame));
                    int col, row;
                    if (d.UvAnimationType == 1)
                    {
                        col = frame;
                        row = d.UvRandomRow ? (int)(Hash01(p.Seed, 25) * ty) % ty : d.UvRowIndex;
                    }
                    else
                    {
                        col = frame % tx;
                        row = frame / tx;
                    }
                    rp.FrameSize = new Vector2(1f / tx, 1f / ty);
                    rp.FrameUv = new Vector2(col * rp.FrameSize.X, row * rp.FrameSize.Y);
                }
                output.Add(rp);
            }
            return output.Count;
        }
    }
}
