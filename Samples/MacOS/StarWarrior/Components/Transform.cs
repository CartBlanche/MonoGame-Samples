using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;
using Microsoft.Xna.Framework;

namespace StarWarrior.Components
{
    class Transform : Component
    {
        private Vector3 coords;

        public Transform()
        {
        }

	    public Transform(Vector3 coords) {
		    this.coords = coords;
	    }

        public void SetCoords(Vector3 coords) {
            this.coords = coords;
        }

	    public void AddX(float x) {
		    this.coords.X += x;
	    }

	    public void AddY(float y) {
		    this.coords.Y += y;
	    }

	    public float GetX() {
		    return this.coords.X;
	    }

	    public void SetX(float x) {
		    this.coords.X = x;
	    }

	    public float GetY() {
		    return this.coords.Y;
	    }

	    public void SetY(float y) {
		    this.coords.Y = y;
	    }

	    public void SetLocation(float x, float y) {
		    this.coords.X = x;
		    this.coords.Y = y;
	    }

	    public float GetRotation() {
		    return this.coords.Z;
	    }

	    public void SetRotation(float rotation) {
		    this.coords.Z = rotation;
	    }

	    public void AddRotation(float angle) {
		    this.coords.Z = (this.coords.Z + angle) % 360;
	    }

	    public float GetRotationAsRadians() {
            return (float)Math.PI * this.coords.Z / 180.0f;
	    }
	
	    public float GetDistanceTo(Transform t) {
		    return Artemis.Utils.Distance(t.GetX(), t.GetY(), GetX(), GetY());
	    }
    }
}
