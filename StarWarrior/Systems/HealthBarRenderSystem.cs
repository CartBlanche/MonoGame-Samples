using System;
using Artemis;
using StarWarrior.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace StarWarrior.Systems
{
	public class HealthBarRenderSystem : EntityProcessingSystem {
		private SpriteBatch spriteBatch;
		private ComponentMapper<Health> healthMapper;
		private ComponentMapper<Transform> transformMapper;
        private SpriteFont font;
	
		public HealthBarRenderSystem(SpriteBatch spriteBatch,SpriteFont font) : base(typeof(Health), typeof(Transform)) {
			this.spriteBatch = spriteBatch;
            this.font = font;
		}
	
		public override void Initialize() {
			healthMapper = new ComponentMapper<Health>(world);
			transformMapper = new ComponentMapper<Transform>(world);
		}
	
		public override void Process(Entity e) {
			Health health = healthMapper.Get(e);
			Transform transform = transformMapper.Get(e);
			Vector2 textPosition = new Vector2((float)transform.GetX()-10, (float)transform.GetY()-30);
			spriteBatch.DrawString(font,health.GetHealthPercentage() + "%",textPosition,Color.White);
		}
    }
}

