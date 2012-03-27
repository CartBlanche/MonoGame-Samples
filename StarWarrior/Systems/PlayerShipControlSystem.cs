using System;
using Artemis;
using StarWarrior.Components;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
namespace StarWarrior.Systems
{
	public class PlayerShipControlSystem : TagSystem {
		private SpriteBatch spriteBatch;
		private bool moveRight;
		private bool moveLeft;
		private bool shoot;
		private ComponentMapper<Transform> transformMapper;
        private KeyboardState oldState;
        
		public PlayerShipControlSystem(SpriteBatch spriteBatch) : base("PLAYER") {
			this.spriteBatch = spriteBatch;
		}
	
		public override void Initialize() {
            transformMapper = new ComponentMapper<Transform>(world);
            oldState = Keyboard.GetState();
		}

        public override void Process(Entity e)
        {
            Transform transform = transformMapper.Get(e);
            UpdateInput();
            if (moveLeft)
            {
                transform.AddX(world.GetDelta() * -0.3f);
            }
            if (moveRight)
            {
                transform.AddX(world.GetDelta() * 0.3f);
            }

            if (shoot)
            {
                Entity missile = EntityFactory.CreateMissile(world);
                missile.GetComponent<Transform>().SetLocation(transform.GetX()+30, transform.GetY() - 20);
                missile.GetComponent<Velocity>().SetVelocity(-0.5f);
                missile.GetComponent<Velocity>().SetAngle(90);
                missile.Refresh();

                shoot = false;
            }
		}
	
		public void UpdateInput() {
            KeyboardState ks = Keyboard.GetState();
			if (ks.IsKeyDown(Keys.A)) {
				moveLeft = true;
				moveRight = false;
            }
            else if (oldState.IsKeyDown(Keys.A)) {
                moveLeft = false;
            }
            if (ks.IsKeyDown(Keys.D)) {
                moveRight = true;
                moveLeft = false;
            }
            else if (oldState.IsKeyDown(Keys.D))
            {
                moveRight = false;
            }
            if (ks.IsKeyDown(Keys.Space) == true && oldState.IsKeyDown(Keys.Space) == false)
            {
                shoot = true;
            }
            else if (oldState.IsKeyDown(Keys.Space))
            {
                shoot = false;
            }
            oldState = ks;
		}
    }
}