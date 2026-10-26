using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Audio;
using Shooter.Gameplay.Enemies;
using Shooter.Gameplay.Weapons;
using Shooter.Rendering.Particles;

namespace Shooter.Gameplay.Effects
{
    /// <summary>
    /// What the enemies show and sound like (3d-3), driven by the events of the AI classes:
    ///   EnemyController.OnDie: Instantiate(DeathVfx, DeathVfxSpawnPoint, identity), destroyed after 5 s; the prefab root's AudioSource (PlayOnAwake) plays its death clip.
    ///   EnemyController.OnDamaged (player damage): DamageTick through AudioUtility.CreateSFX (2D), once per frame.
    ///   EnemyMobile / EnemyTurret.OnDetectedTarget: OnDetectSfx through CreateSFX (3D, EnemyDetection group).
    ///   WeaponController.HandleShoot on an enemy: ShootSfx through the weapon's AudioSource (volume, blend 0.5, max distance 20, EnemyAttack group).
    ///   ProjectileStandard.OnHit on an enemy projectile: ImpactVfx at the hit point + normal x offset, LookRotation(normal).
    ///   EnemyMobile / EnemyTurret.OnDetectedTarget / OnLostTarget: OnDetectVfx[i].Play() / Stop() (VFX_Alert, the AngrySteam systems);
    ///   OnDamaged: RandomHitSparks[Random.Range(0, Length - 1)].Play() (the last entry is never picked, Range's maximum is exclusive).
    /// These systems are children of the enemy's bones; their GameObjects are inactive in the prefabs, yet a Unity capture shows the "!", the steam and
    /// the sparks playing, so the active flag is ignored for them (3d-5). Each follows its bone. The enemy muzzle flash is not played: neither weapon
    /// has a MuzzleFlashPrefab and nothing references that object.
    /// </summary>
    public sealed class EnemyEffects : IDisposable
    {
        private const float DeathVfxLifetime = 5f;   // Destroy(vfx, 5f)

        // AudioMixer snapshot (MainAudioMixer.mixer, group volumes in dB; parents are all 0 dB) - read from the asset, not baked
        public const float DamageTickDb = -6.5566154f;      // group DamageTick
        public const float EnemyDetectionDb = 15.101714f;   // group EnemyDetection
        // groups EnemyAttack, EnemyDeath, Pickup and PlayerMovement are 0 dB

        // AudioSource components of the prefabs (Enemy_HoverBot / Enemy_Turret weapon objects, VFX_*Explosion roots), not in the baked scene
        private static readonly Dictionary<string, string> DeathSfx = new Dictionary<string, string>
        {
            { "VFX_HoverBot_Explosion", "Audio/SFX/Enemies/Hoverbot_Death" },
            { "VFX_Explosion", "Audio/SFX/Enemies/Turret_Death" }
        };
        private const float DeathBlend = 0.8f, DeathMin = 1f, DeathMax = 500f;        // volume 1
        private const float AttackBlend = 0.5f, AttackMin = 1f, AttackMax = 20f;      // volume 0.6 (HoverBot) / 0.15 (Turret)
        private const float DetectBlend = 1f, DetectMin = 1f, DetectMax = 500f;       // AudioUtility.CreateSFX(clip, position, group, 1f)

        public static float DbToLinear(float db) { return (float)Math.Pow(10.0, db / 20.0); }
        public static float AttackVolume(string kind) { return kind == "Turret" ? 0.15f : 0.6f; }

        private readonly EnemyDirector _director;
        private readonly VfxLibrary _vfx;
        private readonly SfxPlayer _sfx;
        private readonly Func<Matrix> _listener;
        private readonly List<VfxLibrary.Instance> _live = new List<VfxLibrary.Instance>();
        private readonly List<Action> _unsubscribe = new List<Action>();
        private readonly Dictionary<Enemy, int> _lastTickFrame = new Dictionary<Enemy, int>();
        private int _frame;
        private readonly Random _random = new Random(1234);

        /// <summary>A prefab particle system that belongs to an enemy bone (OnDetectVfx / RandomHitSparks entries).</summary>
        private sealed class Attached
        {
            public ParticleSystemInstance Ps;
            public string Node;       // the system's GameObject in the enemy's baked hierarchy (its local pose under the bone, bone animated)
            public bool Sparks;       // RandomHitSparks (else OnDetectVfx)
        }

        private readonly Dictionary<Enemy, List<Attached>> _attached = new Dictionary<Enemy, List<Attached>>();

        public int LiveEffects { get { return _live.Count; } }

        public EnemyEffects(EnemyDirector director, VfxLibrary vfx, SfxPlayer sfx, Func<Matrix> listenerWorld)
        {
            _director = director;
            _vfx = vfx;
            _sfx = sfx;
            _listener = listenerWorld;
            foreach (Enemy e in director.AllEnemies)
            {
                Enemy enemy = e;
                Action<Enemy, Vector3> dying = (x, p) => OnDying(x, p);
                Action damaged = () => OnDamaged(enemy);
                Action detected = () => OnDetected(enemy);
                Action attacked = () => OnAttacked(enemy);
                Action lost = () => OnLost(enemy);
                enemy.Dying += dying; enemy.Damaged += damaged; enemy.DetectedTarget += detected; enemy.Attacked += attacked; enemy.LostTarget += lost;
                _unsubscribe.Add(() => { enemy.Dying -= dying; enemy.Damaged -= damaged; enemy.DetectedTarget -= detected; enemy.Attacked -= attacked; enemy.LostTarget -= lost; });
                AttachSystems(enemy);
            }
            Action<ProjectileImpact> impact = OnImpact;
            director.EnemyProjectiles.Impact += impact;
            _unsubscribe.Add(() => director.EnemyProjectiles.Impact -= impact);
        }

        public void Dispose()
        {
            foreach (Action u in _unsubscribe) u();
            _unsubscribe.Clear();
            if (_vfx != null)
            {
                foreach (VfxLibrary.Instance i in _live) _vfx.Destroy(i);
            }
            _live.Clear();
            foreach (List<Attached> list in _attached.Values)
            {
                foreach (Attached a in list) _vfx?.Remove(a.Ps);
            }
            _attached.Clear();
        }

        // EnemyMobile / EnemyTurret: the Alert, AngrySteam and HitSparks systems under the enemy's bones, stopped until played
        private void AttachSystems(Enemy e)
        {
            if (_vfx == null)
            {
                return;
            }
            var list = new List<Attached>();
            var placements = new List<ParticleSystemData.Placement>(_vfx.Placements(e.Data.Kind == "Turret" ? "Enemy_Turret" : "Enemy_HoverBot"));
            placements.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            foreach (ParticleSystemData.Placement p in placements)
            {
                bool sparks = p.Data != null && p.Path.Contains("VFX_HitSparks");
                bool detect = p.Path.Contains("VFX_Alert") || p.Path.Contains("VFX_AngrySteam");
                if (!sparks && !detect)
                {
                    continue;   // VFX_EnemyMuzzleFlash
                }
                // the baked Placement.World does not know the bones (the skeleton is inside the FBX), the enemy's hierarchy does
                string node = p.Path.Substring(p.Path.LastIndexOf('/') + 1);
                ParticleSystemInstance ps = _vfx.AddSystem(p, e.NodeWorld(node));
                ps.Stop(true);
                ps.KeepWhenFinished = true;   // not looping and not playing yet: the manager would drop it before the enemy ever plays it
                list.Add(new Attached { Ps = ps, Node = node, Sparks = sparks });
            }
            _attached[e] = list;
        }

        /// <summary>Shot harness: play (or stop) the detect systems of every enemy.</summary>
        public void DebugAlert(bool on)
        {
            foreach (List<Attached> list in _attached.Values)
            {
                foreach (Attached a in list)
                {
                    if (a.Sparks) continue;
                    if (on) a.Ps.Play(); else a.Ps.Stop();
                }
            }
        }

        // OnDetectVfx[i].Stop()
        private void OnLost(Enemy e)
        {
            if (_attached.TryGetValue(e, out List<Attached> list))
            {
                foreach (Attached a in list) if (!a.Sparks) a.Ps.Stop();
            }
        }

        private static JsonElement Component(Enemy e, string name)
        {
            JsonElement c = default(JsonElement);
            if (e.Data.Components != null)
            {
                e.Data.Components.TryGetValue(name, out c);
            }
            return c;
        }

        // EnemyController.OnDie
        private void OnDying(Enemy e, Vector3 position)
        {
            if (_attached.TryGetValue(e, out List<Attached> own))
            {
                foreach (Attached a in own) _vfx.Remove(a.Ps);   // the enemy's GameObject is destroyed with its particle systems
                _attached.Remove(e);
            }
            string prefab = JsonFields.AssetFileName(Component(e, "EnemyController"), "DeathVfx");
            if (prefab == null)
            {
                return;
            }
            if (_vfx != null)
            {
                _live.Add(_vfx.Spawn(prefab, Matrix.CreateTranslation(position), DeathVfxLifetime));
            }
            if (_sfx != null && DeathSfx.TryGetValue(prefab, out string clip))
            {
                _sfx.PlayOneShot3D(clip, 1f, DeathBlend, position, _listener(), DeathMin, DeathMax);
            }
        }

        // EnemyController.OnDamaged: only the first damage of a frame ticks
        private void OnDamaged(Enemy e)
        {
            if (_attached.TryGetValue(e, out List<Attached> list))
            {
                var sparks = list.FindAll(a => a.Sparks);
                if (sparks.Count > 0)
                {
                    sparks[_random.Next(0, Math.Max(1, sparks.Count - 1))].Ps.Play();   // Random.Range(0, Length - 1): int maximum is exclusive
                }
            }
            if (_lastTickFrame.TryGetValue(e, out int f) && f == _frame)
            {
                return;
            }
            _lastTickFrame[e] = _frame;
            string clip = JsonFields.AssetName(Component(e, "EnemyController"), "DamageTick");
            if (_sfx != null && clip != null)
            {
                _sfx.PlayOneShot(clip, DbToLinear(DamageTickDb));   // CreateSFX with spatial blend 0
            }
        }

        // EnemyMobile / EnemyTurret.OnDetectedTarget
        private void OnDetected(Enemy e)
        {
            if (_attached.TryGetValue(e, out List<Attached> list))
            {
                foreach (Attached a in list) if (!a.Sparks) a.Ps.Play();
            }
            string clip = JsonFields.AssetName(Component(e, e.Data.Kind == "Turret" ? "EnemyTurret" : "EnemyMobile"), "OnDetectSfx");
            if (_sfx != null && clip != null)
            {
                _sfx.PlayOneShot3D(clip, 1f, DetectBlend, e.Position, _listener(), DetectMin, DetectMax, DbToLinear(EnemyDetectionDb));
            }
        }

        // WeaponController.HandleShoot: m_ShootAudioSource.PlayOneShot(ShootSfx)
        private void OnAttacked(Enemy e)
        {
            string clip = e.Weapon != null ? e.Weapon.Settings.ShootSfx : null;
            if (_sfx != null && clip != null)
            {
                _sfx.PlayOneShot3D(clip, AttackVolume(e.Data.Kind), AttackBlend, e.Weapon.PivotWorld(e), _listener(), AttackMin, AttackMax);
            }
        }

        // ProjectileStandard.OnHit (enemy projectiles, level and player alike)
        private void OnImpact(ProjectileImpact hit)
        {
            ProjectileSettings p = hit.Settings;
            if (p == null || p.ImpactVfx == null || _vfx == null)
            {
                return;
            }
            Vector3 normal = hit.Normal;
            Vector3 up = Math.Abs(Vector3.Dot(normal, Vector3.Up)) > 0.999f ? Vector3.Forward : Vector3.Up;
            Matrix root = Matrix.CreateWorld(hit.Point + normal * p.ImpactVfxSpawnOffset, normal, up);   // LookRotation(normal)
            _live.Add(_vfx.Spawn(p.ImpactVfx, root, p.ImpactVfxLifetime));
        }

        /// <summary>Ages the one-shot effects and starts a new frame for the once-per-frame damage tick.</summary>
        public void Update(float dt)
        {
            _frame++;
            foreach (KeyValuePair<Enemy, List<Attached>> kv in _attached)
            {
                foreach (Attached a in kv.Value)
                {
                    if (a.Ps.IsAlive)
                    {
                        a.Ps.SetWorld(kv.Key.NodeWorld(a.Node));   // follows the animated bone
                    }
                }
            }
            for (int k = _live.Count - 1; k >= 0; k--)
            {
                VfxLibrary.Instance i = _live[k];
                i.Age += dt;
                if (i.Age >= i.Lifetime)
                {
                    _vfx.Destroy(i);
                    _live.RemoveAt(k);
                }
            }
        }
    }
}
