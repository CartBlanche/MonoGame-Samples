using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;
using StarWarrior.Components;
using Microsoft.Xna.Framework.Graphics;

namespace StarWarrior.Systems
{
    class EnemyShipMovementSystem : EntityProcessingSystem
    {
        private SpriteBatch spriteBatch;
	    private ComponentMapper<Transform> transformMapper;
	    private ComponentMapper<Velocity> velocityMapper;
        
	    public EnemyShipMovementSystem(SpriteBatch spriteBatch) : base(typeof(Transform), typeof(Velocity),typeof(Enemy)) {
		    this.spriteBatch = spriteBatch;
	    }

	    public override void Initialize() {
		    transformMapper = new ComponentMapper<Transform>(world);
		    velocityMapper = new ComponentMapper<Velocity>(world);
	    }

	    public override void Process(Entity e) {
            Transform transform = transformMapper.Get(e);
            Velocity velocity = velocityMapper.Get(e);

            if (transform.GetX() > spriteBatch.GraphicsDevice.Viewport.Width || transform.GetX() < 0)
            {
                velocity.AddAngle(180);
            }
	    }
    }
}
