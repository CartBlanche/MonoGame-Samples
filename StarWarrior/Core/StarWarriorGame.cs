#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StarWarriorGame.cs" company="GAMADU.COM">
//     Copyright © 2013 GAMADU.COM. All rights reserved.
//
//     Redistribution and use in source and binary forms, with or without modification, are
//     permitted provided that the following conditions are met:
//
//        1. Redistributions of source code must retain the above copyright notice, this list of
//           conditions and the following disclaimer.
//
//        2. Redistributions in binary form must reproduce the above copyright notice, this list
//           of conditions and the following disclaimer in the documentation and/or other materials
//           provided with the distribution.
//
//     THIS SOFTWARE IS PROVIDED BY GAMADU.COM 'AS IS' AND ANY EXPRESS OR IMPLIED
//     WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//     FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL GAMADU.COM OR
//     CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//     CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//     SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//     ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//     NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//     ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//     The views and conclusions contained in the software and documentation are those of the
//     authors and should not be interpreted as representing official policies, either expressed
//     or implied, of GAMADU.COM.
// </copyright>
// <summary>
//   This is the main type for your game.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior
{
    #region Using statements

    using System;

    using Artemis;
    using Artemis.System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using StarWarrior.Components;
    using StarWarrior.Templates;

    #endregion

    /// <summary>This is the main type for Star Warrior.</summary>
    public class StarWarriorGame : Game
    {
        /// <summary>The one second.</summary>
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        /// <summary>The graphics.</summary>
        private readonly GraphicsDeviceManager graphics;

        /// <summary>The elapsed time.</summary>
        private TimeSpan elapsedTime;

        /// <summary>The font.</summary>
        private SpriteFont font;

        /// <summary>The frame counter.</summary>
        private int frameCounter;

        /// <summary>The frame rate.</summary>
        private int frameRate;

        /// <summary>The sprite batch.</summary>
        private SpriteBatch spriteBatch;

        /// <summary>The entityWorld.</summary>
        private EntityWorld entityWorld;

        /// <summary>Initializes a new instance of the <see cref="StarWarriorGame" /> class.</summary>
        public StarWarriorGame()
        {
            this.elapsedTime = TimeSpan.Zero;

            this.graphics = new GraphicsDeviceManager(this)
                                {
                                    IsFullScreen = false,
                                    PreferredBackBufferHeight = 720,
                                    PreferredBackBufferWidth = 1280,
                                    PreferredBackBufferFormat = SurfaceFormat.Color,
                                    PreferMultiSampling = false,
                                    PreferredDepthStencilFormat = DepthFormat.None
                                };
#if DEBUG
            this.graphics.SynchronizeWithVerticalRetrace = false;
#else
            this.graphics.SynchronizeWithVerticalRetrace = true;
#endif
            this.IsFixedTimeStep = false; //this wont work when vsync is ON
            this.Content.RootDirectory = "Content";
        }

        /// <summary>This is called when the game should draw itself.</summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            string fps = string.Format("fps: {0}", this.frameRate);            
#if DEBUG
            string entityCount = string.Format("Active entities: {0}", this.entityWorld.EntityManager.ActiveEntities.Count);
            string removedEntityCount = string.Format("Removed entities: {0}", this.entityWorld.EntityManager.TotalRemoved);
            string totalEntityCount = string.Format("Total entities: {0}", this.entityWorld.EntityManager.TotalCreated);
#endif

            this.GraphicsDevice.Clear(Color.Black);
            this.spriteBatch.Begin();
            this.entityWorld.Draw();
            this.spriteBatch.DrawString(this.font, fps, new Vector2(32, 32), Color.Yellow);            
#if DEBUG
            this.spriteBatch.DrawString(this.font, entityCount, new Vector2(32, 62), Color.Yellow);
            this.spriteBatch.DrawString(this.font, removedEntityCount, new Vector2(32, 92), Color.Yellow);
            this.spriteBatch.DrawString(this.font, totalEntityCount, new Vector2(32, 122), Color.Yellow);
#endif
            this.spriteBatch.End();
        }

        /// <summary>Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.</summary>
        protected override void Initialize()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);
            this.font = this.Content.Load<SpriteFont>("myFont");

            this.entityWorld = new EntityWorld();

            EntitySystem.BlackBoard.SetEntry("ContentManager", this.Content);
            EntitySystem.BlackBoard.SetEntry("GraphicsDevice", this.GraphicsDevice);
            EntitySystem.BlackBoard.SetEntry("SpriteBatch", this.spriteBatch);
            EntitySystem.BlackBoard.SetEntry("SpriteFont", this.font);
            EntitySystem.BlackBoard.SetEntry("EnemyInterval", 500);

#if XBOX
            this.entityWorld.InitializeAll( System.Reflection.Assembly.GetExecutingAssembly());
#else
            this.entityWorld.InitializeAll(true);
#endif

            this.InitializePlayerShip();
            this.InitializeEnemyShips();

            base.Initialize();
        }

        /// <summary>Allows the game to run logic such as updating the entityWorld,
        /// checking for collisions, gathering input, and playing audio.</summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape) ||
                GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.Back))
            {
                this.Exit();
            }

            this.entityWorld.Update();

            ++this.frameCounter;
            this.elapsedTime += gameTime.ElapsedGameTime;
            if (this.elapsedTime > OneSecond)
            {
                this.elapsedTime -= OneSecond;
                this.frameRate = this.frameCounter;
                this.frameCounter = 0;
            }
        }

        /// <summary>The initialize enemy ships.</summary>
        private void InitializeEnemyShips()
        {
            Random random = new Random();
            for (int index = 0; 2 > index; ++index)
            {
                Entity entity = this.entityWorld.CreateEntityFromTemplate(EnemyShipTemplate.Name);

                entity.GetComponent<TransformComponent>().X = random.Next(this.GraphicsDevice.Viewport.Width - 100) + 50;
                entity.GetComponent<TransformComponent>().Y = random.Next((int)((this.GraphicsDevice.Viewport.Height * 0.75) + 0.5)) + 50;
                entity.GetComponent<VelocityComponent>().Speed = 0.05f;
                entity.GetComponent<VelocityComponent>().Angle = random.Next() % 2 == 0 ? 0 : 180;

                entity.Refresh();
            }
        }

        /// <summary>The initialize player ship.</summary>
        private void InitializePlayerShip()
        {
            Entity entity = this.entityWorld.CreateEntity();
            entity.Group = "SHIPS";

            entity.AddComponentFromPool<TransformComponent>();
            entity.AddComponent(new SpatialFormComponent("PlayerShip"));
            entity.AddComponent(new HealthComponent(30));

            entity.GetComponent<TransformComponent>().X = this.GraphicsDevice.Viewport.Width * 0.5f;
            entity.GetComponent<TransformComponent>().Y = this.GraphicsDevice.Viewport.Height - 50;
            entity.Tag = "PLAYER";

            entity.Refresh();
        }
    }
}