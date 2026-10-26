using System;
using System.Collections.Generic;
using Shooter.Gameplay.Combat;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>Port of AI/EnemyManager.cs plus the hit-tag lookup the collision world needs (Unity finds the Damageable through the collider).</summary>
    public class EnemyManager
    {
        private readonly GameEvents _events;
        private readonly Dictionary<int, Damageable> _hitboxes = new Dictionary<int, Damageable>();
        private readonly Dictionary<int, Enemy> _hitboxEnemies = new Dictionary<int, Enemy>();

        public List<Enemy> Enemies { get; private set; }
        public int NumberOfEnemiesTotal { get; private set; }
        public int NumberOfEnemiesRemaining { get { return Enemies.Count; } }
        /// <summary>Seconds since the scene started (Time.time for the enemy scripts).</summary>
        public float Time { get; private set; }

        public EnemyManager(GameEvents events)
        {
            _events = events;
            Enemies = new List<Enemy>();
        }

        public void Update(float dt)
        {
            Time += dt;
        }

        public void RegisterEnemy(Enemy enemy)
        {
            Enemies.Add(enemy);
            NumberOfEnemiesTotal++;
        }

        public void RegisterHitbox(int tag, Damageable damageable, Enemy enemy)
        {
            _hitboxes[tag] = damageable;
            _hitboxEnemies[tag] = enemy;
        }

        /// <summary>Damageable of a collision tag (null for level geometry or an unknown tag).</summary>
        public Damageable DamageableOf(int tag)
        {
            return tag >= 0 && _hitboxes.TryGetValue(tag, out Damageable d) ? d : null;
        }

        /// <summary>The enemy owning a collision tag (Unity: GetComponentInParent&lt;EnemyController&gt;).</summary>
        public Enemy EnemyOf(int tag)
        {
            return tag >= 0 && _hitboxEnemies.TryGetValue(tag, out Enemy e) ? e : null;
        }

        public void UnregisterEnemy(Enemy killed)
        {
            // the remaining count is announced before the enemy leaves the list, like the script does
            _events.Raise(new EnemyKillEvent { Enemy = killed, RemainingEnemyCount = NumberOfEnemiesRemaining - 1 });
            Enemies.Remove(killed);
        }
    }
}
