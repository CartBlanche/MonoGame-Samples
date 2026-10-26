using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Pickups;
using Shooter.Gameplay.Weapons;
using Shooter.Scene;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>
    /// What the Unity scene wires together at load: GameEvents, ActorsManager, EnemyManager, the enemies with their hitboxes, the
    /// player's Actor and Health, and ObjectiveKillEnemies. <see cref="Reset"/> rebuilds all of it (reloading the scene): enemies are
    /// back at full health, their render items and colliders restored, the objective counts from zero again.
    /// </summary>
    public class EnemyDirector
    {
        private readonly LoadedScene _scene;

        public GameEvents Events { get; private set; }
        public ActorsManager Actors { get; private set; }
        public EnemyManager Enemies { get; private set; }
        public ObjectiveKillEnemies Objective { get; private set; }
        public List<Enemy> AllEnemies { get; private set; }
        /// <summary>GameFlowManager.GameIsEnding as seen by the enemies; survives <see cref="Reset"/>.</summary>
        public Func<bool> GameIsEnding;
        /// <summary>The player's Actor (affiliation from the Player prefab); it is also the damage source of the player's projectiles.</summary>
        public Actor PlayerActor { get; private set; }
        /// <summary>The enemies' projectiles (Projectile_Hoverbot, Projectile_Turret).</summary>
        public ProjectileManager EnemyProjectiles { get; private set; }
        /// <summary>Projectiles that reached the player's capsule (impact, shooter); each has already been applied to <see cref="PlayerDamageable"/>.</summary>
        public List<KeyValuePair<ProjectileImpact, object>> PlayerHits { get; private set; }
        /// <summary>The Health component of the Player prefab (MaxHealth 100, CriticalHealthRatio 0.3); rebuilt by <see cref="Reset"/>.</summary>
        public Health PlayerHealth { get; private set; }
        /// <summary>The Player prefab's Damageable (multiplier 1, SensibilityToSelfdamage 0.25) over <see cref="PlayerHealth"/>.</summary>
        public Damageable PlayerDamageable { get; private set; }

        /// <summary>The scene's health pickups and the loot dropped by dead enemies (HoverBot: Loot_Health, DropRate 1); rebuilt by <see cref="Reset"/>.</summary>
        public PickupManager Pickups { get; private set; }
        /// <summary>Raised when an enemy drops loot (the pickup is already in <see cref="Pickups"/>).</summary>
        public event Action<Enemy, Pickup> LootDropped;
        private readonly Random _random = new Random();

        private PlayerController _player;
        private PlayerSettings _playerSettings;
        private ProjectileSettings _enemyProjectile, _turretProjectile;

        /// <summary>Binds the player: its Actor reports the controller's position, AimPoint and capsule to the AI.</summary>
        public void SetPlayer(PlayerController player, PlayerSettings settings)
        {
            _player = player;
            _playerSettings = settings;
            BindPlayerActor();
            _player.BindHealth(PlayerHealth);
        }

        private void BindPlayerActor()
        {
            if (_player == null)
            {
                return;
            }
            float[] aim = _scene.Data.Player.AimPointInRoot ?? new[] { 0f, 1.09f, 0f };
            PlayerActor.GetPosition = () => _player.Position;
            PlayerActor.GetAimPoint = () => _player.Position + new Vector3(aim[0], aim[1], aim[2]);
            PlayerActor.GetCapsule = () =>
            {
                float r = _playerSettings.Radius;
                return new Capsule { A = _player.Position + Vector3.Up * r, B = _player.Position + Vector3.Up * (_player.Height - r), Radius = r };
            };
            Actors.Register(PlayerActor);
            Actors.SetPlayer(PlayerActor);
        }

        public EnemyDirector(LoadedScene scene)
        {
            _scene = scene;
            Reset();
        }

        public void Reset()
        {
            if (Objective != null)
            {
                Objective.Detach();
            }
            // restore what the previous run destroyed
            if (AllEnemies != null)
            {
                foreach (Enemy e in AllEnemies)
                {
                    _scene.RestoreEnemyItem(e.Index);
                }
                foreach (HitboxInfo h in _scene.Hitboxes)
                {
                    _scene.Collision.SetOwnerActive(h.Tag, true);
                }
            }

            Events = new GameEvents();
            Actors = new ActorsManager();
            Enemies = new EnemyManager(Events);
            AllEnemies = new List<Enemy>();

            // the player's Actor
            JsonElement ac = default(JsonElement);
            if (_scene.Data.Player.Components != null)
            {
                _scene.Data.Player.Components.TryGetValue("Actor", out ac);
            }
            PlayerActor = new Actor { Affiliation = JsonFields.Int(ac, "Affiliation", 0), Name = "Player" };
            BindPlayerActor();   // without a player the actor is not registered: nothing to detect

            JsonElement hc = default(JsonElement), dc = default(JsonElement);
            if (_scene.Data.Player.Components != null)
            {
                _scene.Data.Player.Components.TryGetValue("Health", out hc);
                _scene.Data.Player.Components.TryGetValue("Damageable", out dc);
            }
            PlayerHealth = new Health(JsonFields.Float(hc, "MaxHealth", 100f), JsonFields.Float(hc, "CriticalHealthRatio", 0.3f));
            PlayerDamageable = new Damageable(PlayerHealth, PlayerActor, JsonFields.Float(dc, "DamageMultiplier", 1f), JsonFields.Float(dc, "SensibilityToSelfdamage", 0.5f));
            if (_player != null)
            {
                _player.BindHealth(PlayerHealth);   // the controller keeps listening to the old Health otherwise
            }
            EnemyProjectiles = new ProjectileManager { DamageableOf = tag => Enemies.DamageableOf(tag) };
            PlayerHits = new List<KeyValuePair<ProjectileImpact, object>>();
            EnemyProjectiles.PlayerHit += (impact, source) =>
            {
                PlayerHits.Add(new KeyValuePair<ProjectileImpact, object>(impact, source));
                // ProjectileStandard.OnHit: the player's Damageable takes the damage with the projectile's owner as source
                PlayerDamageable.InflictDamage(impact.Damage, false, source);
            };
            _enemyProjectile = ProjectileSettingsOf(_scene.Data.EnemyProjectile);
            _turretProjectile = ProjectileSettingsOf(_scene.Data.TurretProjectile);

            for (int i = 0; i < _scene.Data.Enemies.Count; i++)
            {
                EnemyDto ed = _scene.Data.Enemies[i];
                var ctx = new EnemyContext
                {
                    World = _scene.Collision,
                    Projectiles = EnemyProjectiles,
                    ProjectileSettings = ed.Kind == "Turret" ? _turretProjectile : _enemyProjectile,
                    PlayerActor = () => PlayerActor,
                    GameIsEnding = () => GameIsEnding != null && GameIsEnding(),
                    View = _scene.EnemyViews != null ? _scene.EnemyViews[i] : null
                };
                (ctx.View as EnemyRig)?.ResetState();
                var enemy = new Enemy(ed, i, Enemies, Actors, ctx, ed.PatrolPath >= 0 ? _scene.Data.PatrolPaths[ed.PatrolPath] : null);
                AllEnemies.Add(enemy);
                enemy.Destroyed += OnEnemyDestroyed;
                foreach (HitboxInfo h in _scene.Hitboxes)
                {
                    if (h.EnemyIndex == i)
                    {
                        enemy.HitTags.Add(h.Tag);
                        Enemies.RegisterHitbox(h.Tag, new Damageable(enemy.Health, enemy, h.DamageMultiplier, h.SensibilityToSelfdamage), enemy);
                    }
                }
                // hitboxes under an animated bone follow it (the Turret's head)
                foreach (HitboxInfo h in _scene.Hitboxes)
                {
                    if (h.EnemyIndex == i && ed.Hierarchy != null && ed.Hierarchy.TryGetValue(h.Name, out NodeDto node) && node.Parent != null && ctx.View != null && ctx.View.HasBone(node.Parent))
                    {
                        enemy.HitboxBones.Add(new KeyValuePair<int, string>(h.Tag, node.Parent));
                    }
                }
                enemy.FollowBones();   // the Turret's head box starts at its bone, not at the baked placeholder
                if (enemy.Agent != null && enemy.HitTags.Count > 0)
                {
                    _scene.Collision.MoveDynamicSphere(enemy.HitTags[0], enemy.AimPointPosition);   // back at the start pose
                }
            }

            Pickups = new PickupManager();
            Pickups.GetPlayerCapsule = () => _player != null ? PlayerActor.GetCapsule() : (Capsule?)null;
            Pickups.GetPlayerHealth = () => PlayerHealth;
            Pickups.AddFromScene(_scene.Data);

            Objective = null;
            foreach (ObjectiveDto o in _scene.Data.Objectives)
            {
                if (o.Kind == "ObjectiveKillEnemies")
                {
                    Objective = new ObjectiveKillEnemies(o, Events);
                    Objective.Initialize();
                    break;
                }
            }
        }

        private static ProjectileSettings ProjectileSettingsOf(WeaponDto p)
        {
            if (p == null || p.Components == null || !p.Components.TryGetValue("ProjectileStandard", out System.Text.Json.JsonElement ps))
            {
                return null;
            }
            float[] tip = p.TipInRoot ?? new float[3];
            return ProjectileSettings.From(ps, new Vector3(tip[0], tip[1], tip[2]));
        }

        public void Update(float dt)
        {
            Enemies.Update(dt);
            foreach (Enemy e in AllEnemies.ToArray())
            {
                e.Update(dt);
            }
            EnemyProjectiles.Update(dt);
            if (_player != null)
            {
                Pickups.Update(dt);
            }
        }

        private void OnEnemyDestroyed(Enemy e)
        {
            // Destroy(gameObject): renderers and colliders go with it
            _scene.RemoveEnemyItem(e.Index);
            foreach (int tag in e.HitTags)
            {
                _scene.Collision.SetOwnerActive(tag, false);
            }
            TryDropLoot(e);
        }

        /// <summary>EnemyController.Die: if TryDropItem, Instantiate(LootPrefab, transform.position, identity). Loot_Health is the only LootPrefab in the scene.</summary>
        private void TryDropLoot(Enemy e)
        {
            JsonElement ec = default(JsonElement);
            if (e.Data.Components != null)
            {
                e.Data.Components.TryGetValue("EnemyController", out ec);
            }
            string loot = JsonFields.AssetFileName(ec, "LootPrefab");
            float rate = JsonFields.Float(ec, "DropRate", 1f);
            bool drop = !(rate == 0f || loot == null) && (rate == 1f || _random.NextDouble() <= rate);
            if (drop && loot == "Loot_Health" && Pickups.Template != null)
            {
                Pickup p = Pickups.SpawnLike(Pickups.Template, e.Position, loot);
                if (LootDropped != null) LootDropped(e, p);
            }
        }
    }
}
