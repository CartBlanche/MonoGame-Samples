using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;

namespace StarWarrior.Components
{
    class Velocity : Component
    {
        private float velocity;
        private float angle;

        public Velocity()
        {
        }

        public Velocity(float vector)
        {
            this.velocity = vector;
        }

        public Velocity(float velocity, float angle)
        {
            this.velocity = velocity;
            this.angle = angle;
        }

        public float GetVelocity()
        {
            return velocity;
        }

        public void SetVelocity(float velocity)
        {
            this.velocity = velocity;
        }

        public void SetAngle(float angle)
        {
            this.angle = angle;
        }

        public float GetAngle()
        {
            return angle;
        }

        public void AddAngle(float a)
        {
            angle = (angle + a) % 360;
        }

        public float GetAngleAsRadians()
        {
            return (float)Math.PI * angle / 180.0f; ;
        }
    }
}
