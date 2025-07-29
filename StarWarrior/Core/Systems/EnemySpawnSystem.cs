#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnemySpawnSystem.cs" company="GAMADU.COM">
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
//   The enemy spawn system.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Systems
{
    #region Using statements

    using System;
    using System.Collections.Generic;

    using Artemis;
    using Artemis.Attributes;
    using Artemis.Manager;
    using Artemis.System;

    using Microsoft.Xna.Framework.Graphics;

    using StarWarrior.Components;
    using StarWarrior.Templates;

    #endregion

    /// <summary>The enemy spawn system.</summary>
    [ArtemisEntitySystem(GameLoopType = GameLoopType.Update, Layer = 1)]
    public class EnemySpawnSystem : IntervalEntitySystem
    {
        /// <summary>The random.</summary>
        private Random random;

        /// <summary>The sprite batch.</summary>
        private SpriteBatch spriteBatch;

        /// <summary>Initializes a new instance of the <see cref="EnemySpawnSystem" /> class.</summary>
        public EnemySpawnSystem()
            : base(
                new TimeSpan(0, 0, 0, 0, BlackBoard.GetEntry<int>("EnemyInterval")),
                Aspect.All(typeof(TransformComponent), typeof(VelocityComponent), typeof(EnemyComponent)))
        {
        }

        /// <summary>Override to implement code that gets executed when systems are initialized.</summary>
        public override void LoadContent()
        {
            this.spriteBatch = BlackBoard.GetEntry<SpriteBatch>("SpriteBatch");
            this.random = new Random();
        }

        /// <summary>Processes the entities.</summary>
        /// <param name="entities">The entities.</param>
        protected override void ProcessEntities(IDictionary<int, Entity> entities)
        {
            Entity entity = this.EntityWorld.CreateEntityFromTemplate(EnemyShipTemplate.Name);

            entity.GetComponent<TransformComponent>().X = this.random.Next(this.spriteBatch.GraphicsDevice.Viewport.Width);
            entity.GetComponent<TransformComponent>().Y = this.random.Next(400) + 50;

            entity.GetComponent<VelocityComponent>().Speed = 0.05f;
            entity.GetComponent<VelocityComponent>().Angle = this.random.Next() % 2 == 0 ? 0 : 180;

            entity.Refresh();
        }
    }
}