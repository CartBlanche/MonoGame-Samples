using System;
using Artemis;
using StarWarrior.Components;
using Microsoft.Xna.Framework.Graphics;
namespace StarWarrior.Systems
{
	public class MovementSystem : EntityProcessingSystem {
		private SpriteBatch spriteBatch;
		private ComponentMapper<Velocity> velocityMapper;
		private ComponentMapper<Transform> transformMapper;
	
		public MovementSystem(SpriteBatch spriteBatch) : base(typeof(Transform), typeof(Velocity)) {
			this.spriteBatch = spriteBatch;
		}
	
		public override void Initialize() {
			velocityMapper = new ComponentMapper<Velocity>(world);
			transformMapper = new ComponentMapper<Transform>(world);
		}
	
		public override void Process(Entity e) {
			Velocity velocity = velocityMapper.Get(e);
			float v = velocity.GetVelocity();
	
			Transform transform = transformMapper.Get(e);
	
			float r = velocity.GetAngleAsRadians();
	
			float xn = transform.GetX() + (TrigLUT.Cos(r) * v * world.GetDelta());
			float yn = transform.GetY() + (TrigLUT.Sin(r) * v * world.GetDelta());
	
			transform.SetLocation(xn, yn);
		}
    }
}

