using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;

namespace StarWarrior.Components
{
    class Health : Component
    {
        private float health = 0;
        private float maximumHealth = 0;

        public Health() { }

        public Health(float health)
        {
            this.health = this.maximumHealth = health;
        }

        public float GetHealth()
        {
            return health;
        }

        public void SetHealth(float health)
        {
            this.health = this.maximumHealth = health;
        }

        public float GetMaximumHealth()
        {
            return maximumHealth;
        }

        public double GetHealthPercentage()
        {
            return Math.Round(health / maximumHealth * 100f);
        }

        public void AddDamage(int damage)
        {
            health -= damage;
            if (health < 0)
                health = 0;
        }

        public bool IsAlive()
        {
            return health > 0;
        }
    }
}
