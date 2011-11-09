using System;
using Artemis;
using StarWarrior.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
namespace StarWarrior.Systems
{
	public class HudRenderSystem : TagSystem {
		private SpriteBatch spriteBatch;
		private ComponentMapper<Health> healthMapper;
        private SpriteFont font;
       
		public HudRenderSystem(SpriteBatch spriteBatch,SpriteFont font) : base("PLAYER") {
			this.spriteBatch = spriteBatch;
            this.font = font;
		}
	
		public override void Initialize() {
            healthMapper = new ComponentMapper<Health>(world);
		}
	
        public override void Process(Entity e) {
            Health health = healthMapper.Get(e);
            Vector2 textPosition = new Vector2(20, spriteBatch.GraphicsDevice.Viewport.Height);
            spriteBatch.DrawString(font, "Health: " + health.GetHealthPercentage() + "%", textPosition, Color.White);
		}
    }
}

