#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerShipControlSystem.cs" company="GAMADU.COM">
//     Copyright � 2013 GAMADU.COM. All rights reserved.
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
//   The player ship control system.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Systems
{
    #region Using statements

    using System;

    using Artemis;
    using Artemis.Attributes;
    using Artemis.Manager;
    using Artemis.System;
    using Artemis.Utils;

    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using StarWarrior.Components;
    using StarWarrior.Templates;

    #endregion

    /// <summary>The player ship control system.</summary>
    [ArtemisEntitySystem(GameLoopType = GameLoopType.Update)]
    public class PlayerShipControlSystem : TagSystem
    {
        /// <summary>The missile launch timer.</summary>
        private readonly Timer missileLaunchTimer;

        /// <summary>The graphics device.</summary>
        private GraphicsDevice graphicsDevice;

        /// <summary>Initializes a new instance of the <see cref="PlayerShipControlSystem" /> class.</summary>
        public PlayerShipControlSystem()
            : base("PLAYER")
        {
            this.missileLaunchTimer = new Timer(new TimeSpan(0, 0, 0, 0, 150));
        }

        /// <summary>Override to implement code that gets executed when systems are initialized.</summary>
        public override void LoadContent()
        {
            this.graphicsDevice = BlackBoard.GetEntry<GraphicsDevice>("GraphicsDevice");
        }

        /// <summary>Processes the specified entity.</summary>
        /// <param name="entity">The entity.</param>
        public override void Process(Entity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            KeyboardState keyboardState = Keyboard.GetState();
            float keyMoveSpeed = 0.3f * TimeSpan.FromTicks(this.EntityWorld.Delta).Milliseconds;
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            {
                transformComponent.X -= keyMoveSpeed;
                if (transformComponent.X < 32)
                {
                    transformComponent.X = 32;
                }
            }
            else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            {
                transformComponent.X += keyMoveSpeed;
                if (transformComponent.X > this.graphicsDevice.Viewport.Width - 32)
                {
                    transformComponent.X = this.graphicsDevice.Viewport.Width - 32;
                }
            }

            if (keyboardState.IsKeyDown(Keys.Space) || keyboardState.IsKeyDown(Keys.Enter))
            {
                if (this.missileLaunchTimer.IsReached(this.EntityWorld.Delta))
                {
                    this.AddMissile(transformComponent);
                    this.AddMissile(transformComponent, 89, -9);
                    this.AddMissile(transformComponent, 91, +9);
                }
            }
        }

        /// <summary>Adds the missile.</summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="offsetX">The offset X.</param>
        private void AddMissile(TransformComponent transformComponent, float angle = 90.0f, float offsetX = 0.0f)
        {
            Entity missile = this.EntityWorld.CreateEntityFromTemplate(MissileTemplate.Name);

            missile.GetComponent<TransformComponent>().X = transformComponent.X + 1 + offsetX;
            missile.GetComponent<TransformComponent>().Y = transformComponent.Y - 20;

            missile.GetComponent<VelocityComponent>().Speed = -0.5f;
            missile.GetComponent<VelocityComponent>().Angle = angle;

            missile.Refresh();
        }
    }
}