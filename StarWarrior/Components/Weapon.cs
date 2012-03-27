using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;

namespace StarWarrior.Components
{
    class Weapon : Component
    {
        private long shotAt;

        public Weapon()
        {
        }

        public void SetShotAt(long shotAt)
        {
            this.shotAt = shotAt;
        }

        public long GetShotAt()
        {
            return shotAt;
        }
    }
}
