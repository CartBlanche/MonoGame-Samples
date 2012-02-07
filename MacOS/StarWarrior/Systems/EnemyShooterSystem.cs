using System;
using Artemis;
using StarWarrior.Components;
namespace StarWarrior.Systems
{
	public class EnemyShooterSystem : EntityProcessingSystem {

		private ComponentMapper<Weapon> weaponMapper;		
		private ComponentMapper<Transform> transformMapper;
        Random rd = new Random();
        long playerId = -1;
	
		public EnemyShooterSystem() : base(typeof(Transform), typeof(Weapon),typeof(Enemy)) {
		}
	
		public override void Initialize() {
			weaponMapper = new ComponentMapper<Weapon>(world);
			transformMapper = new ComponentMapper<Transform>(world);
		}
	
		protected override void Begin() {			
		}
	
		public override void Process(Entity e) {
            Weapon weapon = weaponMapper.Get(e);

            long t = weapon.GetShotAt() + TimeSpan.FromSeconds(2).Ticks;
            if (t < DateTime.Now.Ticks)
            {
                Transform transform = transformMapper.Get(e);

                Entity missile = EntityFactory.CreateMissile(world);
                missile.GetComponent<Transform>().SetLocation(transform.GetX() + 20, transform.GetY() + 20);
                missile.GetComponent<Velocity>().SetVelocity(-0.5f);
                missile.GetComponent<Velocity>().SetAngle(270);
                missile.Refresh();

                weapon.SetShotAt(DateTime.Now.Ticks);
            }
		}
    }
}