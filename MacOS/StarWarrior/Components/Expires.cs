using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;

namespace StarWarrior.Components
{
    class Expires : Component
    {
        private int lifeTime;

        public Expires() { }

        public Expires(int lifeTime)
        {
            this.lifeTime = lifeTime;
        }

        public int GetLifeTime()
        {
            return lifeTime;
        }

        public void SetLifeTime(int lifeTime)
        {
            this.lifeTime = lifeTime;
        }

        public void ReduceLifeTime(int lifeTime)
        {
            this.lifeTime -= lifeTime;
        }

        public bool IsExpired()
        {
            return lifeTime <= 0;
        }
    }
}
