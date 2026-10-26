using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Scene;

namespace Shooter.Gameplay.Pickups
{
    /// <summary>One Pickup / HealthPickup / JetpackPickup instance (Game/Gameplay/Pickup.cs, HealthPickup.cs, JetpackPickup.cs): bobbing and spinning root with a box trigger.</summary>
    public sealed class Pickup
    {
        public string Name;
        public string Kind = "Health";     // "Health" | "Jetpack" (PickupDto.Kind)
        public Vector3 Start;              // Pickup.m_StartPosition (the root's position when it started)
        public float VerticalBobFrequency = 1f, BobbingAmount = 1f, RotatingSpeed = 360f, HealAmount;
        public Vector3 TriggerCenter;      // root-local, scale applied (BoxCollider.center x lossy scale)
        public Vector3 TriggerHalfSize;
        public string SfxName, VfxPrefab;
        public float AngleDegrees;         // accumulated Rotate(up, RotatingSpeed * dt)
        public float BobOffset;            // current height above Start
        public bool Destroyed;
        public int DynamicIndex = -1;      // the render item (set by the presentation)
        public Matrix MeshRelative = Matrix.Identity;   // the mesh's pose relative to the root (set by the presentation)
        internal bool Inside;              // OnTriggerEnter fires once per entry

        /// <summary>Root transform without scale: the spin about the root's own up axis (clockwise from above like Unity, so negative in the mirrored world), then the position.</summary>
        public Matrix Root
        {
            get { return Matrix.CreateRotationY(-MathHelper.ToRadians(AngleDegrees)) * Matrix.CreateTranslation(Start + Vector3.Up * BobOffset); }
        }

        public Vector3 Position { get { return Start + Vector3.Up * BobOffset; } }
    }

    /// <summary>
    /// The scene's health and jetpack pickups and the loot enemies drop: bobbing and rotation like Pickup.Update, OnTriggerEnter with the player's capsule,
    /// HealthPickup.OnPicked (heal if the Health can take it, then destroyed) and JetpackPickup.OnPicked (unlock the Jetpack unless it already is, then
    /// destroyed). Presentation (mesh, particles, sound) listens to the events.
    /// </summary>
    public sealed class PickupManager
    {
        private readonly List<Pickup> _all = new List<Pickup>();
        private float _time;

        public IReadOnlyList<Pickup> All { get { return _all; } }
        /// <summary>Time.time: bobbing uses the global clock, so a pickup spawned later starts at its own phase.</summary>
        public float Time { get { return _time; } }

        public event Action<Pickup> Spawned;
        /// <summary>The health was healed and the pickup is gone (PlayPickupFeedback: sound and VFX at the pickup's position).</summary>
        public event Action<Pickup> Picked;

        /// <summary>The first health pickup of the scene, kept even after it was picked: the pose and values loot is made from.</summary>
        public Pickup Template { get; private set; }

        public Func<Capsule?> GetPlayerCapsule;
        public Func<Health> GetPlayerHealth;
        public Func<Jetpack> GetPlayerJetpack;

        /// <summary>The Pickup_Health and Pickup_Jetpack instances of the scene (trigger centre / size x the root scale).</summary>
        public void AddFromScene(SceneFile data)
        {
            foreach (PickupDto pd in data.Pickups)
            {
                JsonElement hp = default(JsonElement);
                if (pd.Components != null)
                {
                    pd.Components.TryGetValue(pd.Kind == "Jetpack" ? "JetpackPickup" : "HealthPickup", out hp);
                }
                Matrix m = SceneLoader.ToMatrix(pd.M);
                Vector3 scale = new Vector3(new Vector3(m.M11, m.M12, m.M13).Length(), new Vector3(m.M21, m.M22, m.M23).Length(), new Vector3(m.M31, m.M32, m.M33).Length());
                Vector3 c = pd.TriggerCenter != null ? new Vector3(pd.TriggerCenter[0], pd.TriggerCenter[1], pd.TriggerCenter[2]) : Vector3.Zero;
                Vector3 s = pd.TriggerSize != null ? new Vector3(pd.TriggerSize[0], pd.TriggerSize[1], pd.TriggerSize[2]) : Vector3.One;
                var pickup = new Pickup
                {
                    Name = pd.Name,
                    Kind = pd.Kind ?? "Health",
                    Start = new Vector3(pd.Position[0], pd.Position[1], pd.Position[2]),
                    VerticalBobFrequency = JsonFields.Float(hp, "VerticalBobFrequency", 1f),
                    BobbingAmount = JsonFields.Float(hp, "BobbingAmount", 1f),
                    RotatingSpeed = JsonFields.Float(hp, "RotatingSpeed", 360f),
                    HealAmount = JsonFields.Float(hp, "HealAmount", pd.HealAmount),
                    TriggerCenter = c * scale,
                    TriggerHalfSize = s * scale * 0.5f,
                    SfxName = JsonFields.AssetName(hp, "PickupSfx"),
                    VfxPrefab = JsonFields.AssetFileName(hp, "PickupVfxPrefab")
                };
                if (Template == null && pickup.Kind == "Health")
                {
                    Template = pickup;
                }
                Add(pickup);
            }
        }

        /// <summary>Instantiate the same kind of pickup somewhere else (Loot_Health has the HealthPickup values and trigger of Pickup_Health).</summary>
        public Pickup SpawnLike(Pickup template, Vector3 position, string name)
        {
            var p = new Pickup
            {
                Name = name,
                Kind = template.Kind,
                Start = position,
                VerticalBobFrequency = template.VerticalBobFrequency,
                BobbingAmount = template.BobbingAmount,
                RotatingSpeed = template.RotatingSpeed,
                HealAmount = template.HealAmount,
                TriggerCenter = template.TriggerCenter,
                TriggerHalfSize = template.TriggerHalfSize,
                SfxName = template.SfxName,
                VfxPrefab = template.VfxPrefab
            };
            Add(p);
            return p;
        }

        private void Add(Pickup p)
        {
            _all.Add(p);
            Update(p, 0f);   // the first Update happens in the frame it appears
            if (Spawned != null) Spawned(p);
        }

        public void Update(float dt)
        {
            _time += dt;
            foreach (Pickup p in _all.ToArray())
            {
                if (!p.Destroyed)
                {
                    Update(p, dt);
                }
            }
            Capsule? capsule = GetPlayerCapsule != null ? GetPlayerCapsule() : null;
            Health health = GetPlayerHealth != null ? GetPlayerHealth() : null;
            if (capsule == null)
            {
                return;
            }
            foreach (Pickup p in _all.ToArray())
            {
                if (p.Destroyed)
                {
                    continue;
                }
                bool inside = Overlaps(p, capsule.Value);
                if (inside && !p.Inside)
                {
                    OnTriggerEnter(p, health, GetPlayerJetpack != null ? GetPlayerJetpack() : null);   // OnTriggerEnter: once per entry, even if nothing was picked
                }
                p.Inside = inside;
            }
            _all.RemoveAll(p => p.Destroyed);
        }

        // Pickup.Update: bob, then spin
        private void Update(Pickup p, float dt)
        {
            p.BobOffset = ((float)Math.Sin(_time * p.VerticalBobFrequency) * 0.5f + 0.5f) * p.BobbingAmount;
            p.AngleDegrees += p.RotatingSpeed * dt;
        }

        // HealthPickup.OnPicked / JetpackPickup.OnPicked
        private void OnTriggerEnter(Pickup p, Health health, Jetpack jetpack)
        {
            if (p.Kind == "Jetpack")
            {
                if (jetpack != null && jetpack.TryUnlock())
                {
                    p.Destroyed = true;
                    if (Picked != null) Picked(p);
                }
            }
            else if (health != null && health.CanPickup())
            {
                health.Heal(p.HealAmount);
                p.Destroyed = true;
                if (Picked != null) Picked(p);
            }
        }

        /// <summary>Capsule against the pickup's box trigger, which spins with the root: the capsule axis is sampled in the box's frame.</summary>
        public static bool Overlaps(Pickup p, Capsule capsule)
        {
            Matrix toLocal = Matrix.Invert(Matrix.CreateRotationY(-MathHelper.ToRadians(p.AngleDegrees)) * Matrix.CreateTranslation(p.Position));
            Vector3 a = Vector3.Transform(capsule.A, toLocal) - p.TriggerCenter;
            Vector3 b = Vector3.Transform(capsule.B, toLocal) - p.TriggerCenter;
            const int samples = 24;
            for (int i = 0; i <= samples; i++)
            {
                Vector3 q = Vector3.Lerp(a, b, i / (float)samples);
                Vector3 d = new Vector3(
                    Math.Max(Math.Abs(q.X) - p.TriggerHalfSize.X, 0f),
                    Math.Max(Math.Abs(q.Y) - p.TriggerHalfSize.Y, 0f),
                    Math.Max(Math.Abs(q.Z) - p.TriggerHalfSize.Z, 0f));
                if (d.LengthSquared() <= capsule.Radius * capsule.Radius)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
