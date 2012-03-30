using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Artemis;
using StarWarrior.Components;
using StarWarrior.Systems;
using StarWarrior.Primitives;

namespace StarWarrior
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    /// 
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private EntityWorld world;

        private EntitySystem renderSystem;
        private EntitySystem hudRenderSystem;
        private EntitySystem controlSystem;
        private EntitySystem movementSystem;
        private EntitySystem enemyShooterSystem;
        private EntitySystem enemyShipMovementSystem;
        private EntitySystem collisionSystem;
        private EntitySystem healthBarRenderSystem;
        private EntitySystem enemySpawnSystem;
        private EntitySystem expirationSystem;
        private SpriteFont font;
        private GamePool pool;

        int frameRate,frameCounter;
        TimeSpan elapsedTime = TimeSpan.Zero;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = false;
            this.IsFixedTimeStep = false;
            //graphics.IsFullScreen = false;
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        /// 

        private void RemovedComponent(Entity e,Component c)
        {
            if (c != null)
            {
                pool.AddComponent(c.GetType(), c);
            }
        }

        protected override void Initialize()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            Type[] types = new Type[] {typeof(Enemy),typeof(Expires),typeof(Health),typeof(SpatialForm),typeof(Transform),typeof(Velocity),typeof(Weapon)};
            pool = new GamePool(100,types);
            pool.Initialize();

            spriteBatch = new SpriteBatch(GraphicsDevice);
            world = new EntityWorld();
            world.GetEntityManager().RemovedComponentEvent += new RemovedComponentHandler(RemovedComponent);
            world.SetPool(pool);

            font = Content.Load<SpriteFont>("Arial");
            SystemManager systemManager = world.GetSystemManager();
            renderSystem = systemManager.SetSystem(new RenderSystem(GraphicsDevice,spriteBatch,Content),ExecutionType.Draw);
            hudRenderSystem = systemManager.SetSystem(new HudRenderSystem(spriteBatch, font), ExecutionType.Draw);
            controlSystem = systemManager.SetSystem(new MovementSystem(spriteBatch), ExecutionType.Update,1);
            movementSystem = systemManager.SetSystem(new PlayerShipControlSystem(spriteBatch),ExecutionType.Update);
            enemyShooterSystem = systemManager.SetSystem(new EnemyShipMovementSystem(spriteBatch), ExecutionType.Update,1);
            enemyShipMovementSystem = systemManager.SetSystem(new EnemyShooterSystem(), ExecutionType.Update);
            collisionSystem = systemManager.SetSystem(new CollisionSystem(), ExecutionType.Update,1);
            healthBarRenderSystem = systemManager.SetSystem(new HealthBarRenderSystem(spriteBatch, font), ExecutionType.Draw);
            enemySpawnSystem = systemManager.SetSystem(new EnemySpawnSystem(500, spriteBatch), ExecutionType.Update);
            expirationSystem = systemManager.SetSystem(new ExpirationSystem(), ExecutionType.Update);

            systemManager.InitializeAll();

            InitPlayerShip();
            InitEnemyShips();

            base.Initialize();
        }

        private void InitEnemyShips() {
		    Random r = new Random();
		    for (int i = 0; 2 > i; i++) {
			    Entity e = EntityFactory.CreateEnemyShip(world);

			    e.GetComponent<Transform>().SetLocation(r.Next(GraphicsDevice.Viewport.Width), r.Next(400)+50);
			    e.GetComponent<Velocity>().SetVelocity(0.05f);
			    e.GetComponent<Velocity>().SetAngle(r.Next() % 2 == 0 ? 0 : 180);
			
			    e.Refresh();
		    }
	    }

	    private void InitPlayerShip() {
		    Entity e = world.CreateEntity();
		    e.SetGroup("SHIPS");

            e.AddComponent(pool.TakeComponent<Transform>());
		    e.AddComponent(pool.TakeComponent<SpatialForm>());
		    e.AddComponent(pool.TakeComponent<Health>());
            e.GetComponent<SpatialForm>().SetSpatialFormFile("PlayerShip");
            e.GetComponent<Health>().SetHealth(30);
            e.GetComponent<Transform>().SetCoords(new Vector3(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 50, 0));
		    e.Refresh();
            world.GetTagManager().Register("PLAYER", e);
	    }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
        }


        DateTime dt = DateTime.Now;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            TimeSpan elapsed = DateTime.Now - dt;
            dt = DateTime.Now;
            frameCounter++;

            world.LoopStart();
            world.SetDelta(elapsed.Milliseconds);

            world.GetSystemManager().UpdateAsynchronous(ExecutionType.Update);
            //world.GetSystemManager().UpdateSynchronous(ExecutionType.Update);
            
            elapsedTime += elapsed;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {            
            string fps = string.Format("fps: {0}", frameRate);

            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            spriteBatch.DrawString(font, fps, new Vector2(32,32), Color.Yellow);
            world.GetSystemManager().UpdateSynchronous(ExecutionType.Draw);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
