using System;
using Artemis;
using StarWarrior.Components;
namespace StarWarrior.Systems
{
	public class ExpirationSystem : EntityProcessingSystem {

		private ComponentMapper<Expires> expiresMapper;
	
		public ExpirationSystem() : base(typeof(Expires)) {
		}
	
		public override void Initialize() {
			expiresMapper = new ComponentMapper<Expires>(world);
		}
	
		public override void Process(Entity e) {
			Expires expires = expiresMapper.Get(e);
			expires.ReduceLifeTime(world.GetDelta());
	
			if (expires.IsExpired()) {
				world.DeleteEntity(e);
			}
	
		}
    }
}

