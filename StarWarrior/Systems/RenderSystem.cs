using System;
using Artemis;
using StarWarrior.Components;
using StarWarrior.Spatials;
using Microsoft.Xna.Framework.Graphics;
using StarWarrior.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
namespace StarWarrior.Systems	
{
	public class RenderSystem : EntityProcessingSystem {
		private ComponentMapper<SpatialForm> spatialFormMapper;
		private ComponentMapper<Transform> transformMapper;
        private SpriteBatch spriteBatch;
        private Transform transform;
        private string spatialName;
        private ContentManager contentManager;
        GraphicsDevice device;
	
		public RenderSystem(GraphicsDevice device,SpriteBatch spriteBatch,ContentManager contentManager) : base(typeof(Transform), typeof(SpatialForm)) {
            this.spriteBatch = spriteBatch;
            this.device = device;
            this.contentManager = contentManager;
		}
	
		public override void Initialize() {
			spatialFormMapper = new ComponentMapper<SpatialForm>(world);
			transformMapper = new ComponentMapper<Transform>(world);
        }
	
		public override void Process(Entity e) {
			transform = transformMapper.Get(e);
            SpatialForm spatialForm = spatialFormMapper.Get(e);
            spatialName = spatialForm.GetSpatialFormFile();
	
			if (transform.GetX() >= 0 && transform.GetY() >= 0 && transform.GetX() < spriteBatch.GraphicsDevice.Viewport.Width && transform.GetY() < spriteBatch.GraphicsDevice.Viewport.Height && spatialForm != null) {
                CreateSpatial(e); 
			}
		}

		private void CreateSpatial(Entity e) {
			if (String.Compare("PlayerShip",spatialName,StringComparison.InvariantCultureIgnoreCase) == 0) {
                PlayerShip.Render(spriteBatch, contentManager,transform);
            }
            else if (String.Compare("Missile", spatialName, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                Missile.Render(spriteBatch, contentManager, transform);
            }
            else if (String.Compare("EnemyShip", spatialName, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                EnemyShip.Render(spriteBatch, contentManager, transform);
            }
            else if (String.Compare("BulletExplosion", spatialName, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                Explosion.Render(spriteBatch, contentManager, transform,Color.Red,10);
            }
            else if (String.Compare("ShipExplosion", spatialName, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                ShipExplosion.Render(spriteBatch, contentManager, transform, Color.Yellow, 30);
			}
		}
    }
}