using System;
using StarWarrior.Components;
using Artemis;
using Microsoft.Xna.Framework;
namespace StarWarrior
{
	public class EntityFactory {

        public static Entity CreateMissile(EntityWorld world)
        {
			Entity missile = world.CreateEntity();
            GamePool pool = (GamePool)world.GetPool();
			missile.SetGroup("BULLETS");
			
			missile.AddComponent(pool.TakeComponent<Transform>());
			missile.AddComponent(pool.TakeComponent<SpatialForm>());
			missile.AddComponent(pool.TakeComponent<Velocity>());
			missile.AddComponent(pool.TakeComponent<Expires>());
            missile.GetComponent<SpatialForm>().SetSpatialFormFile("Missile");
            missile.GetComponent<Expires>().SetLifeTime(2000);
	   		return missile;
		}

     	public static Entity CreateEnemyShip(EntityWorld world) {
			Entity e = world.CreateEntity();
			e.SetGroup("SHIPS");
            GamePool pool = (GamePool)world.GetPool();
			e.AddComponent(pool.TakeComponent<Transform>());
			e.AddComponent(pool.TakeComponent<SpatialForm>());
			e.AddComponent(pool.TakeComponent<Health>());
			e.AddComponent(pool.TakeComponent<Weapon>());
            e.AddComponent(pool.TakeComponent<Enemy>());
			e.AddComponent(pool.TakeComponent<Velocity>());
            e.GetComponent<SpatialForm>().SetSpatialFormFile("EnemyShip");
            e.GetComponent<Health>().SetHealth(10);
			return e;
		}

        public static Entity CreateBulletExplosion(EntityWorld world, float x, float y)
        {
			Entity e = world.CreateEntity();
            GamePool pool = (GamePool)world.GetPool();
			e.SetGroup("EFFECTS");
			
			e.AddComponent(pool.TakeComponent<Transform>());
			e.AddComponent(pool.TakeComponent<SpatialForm>());
			e.AddComponent(pool.TakeComponent<Expires>());
            e.GetComponent<SpatialForm>().SetSpatialFormFile("BulletExplosion");
            e.GetComponent<Expires>().SetLifeTime(1000);
            e.GetComponent<Transform>().SetCoords(new Vector3(x, y, 0));
			return e;
		}

        public static Entity CreateShipExplosion(EntityWorld world, float x, float y)
        {
			Entity e = world.CreateEntity();
            GamePool pool = (GamePool)world.GetPool();
			e.SetGroup("EFFECTS");
			
			e.AddComponent(pool.TakeComponent<Transform>());
			e.AddComponent(pool.TakeComponent<SpatialForm>());
			e.AddComponent(pool.TakeComponent<Expires>());
            e.GetComponent<SpatialForm>().SetSpatialFormFile("ShipExplosion");
            e.GetComponent<Transform>().SetCoords(new Vector3(x, y, 0));
            e.GetComponent<Expires>().SetLifeTime(1000);
			return e;
		}
	
	}
}