#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CollisionSystem.cs" company="GAMADU.COM">
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
//   The collision system.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Systems
{
    #region Using statements

    using System.Collections.Generic;

    using Artemis;
    using Artemis.Attributes;
    using Artemis.Manager;
    using Artemis.System;
    using Artemis.Utils;

    using Microsoft.Xna.Framework;

    using StarWarrior.Components;
    using StarWarrior.Templates;

    #endregion

    /// <summary>The collision system.</summary>
    [ArtemisEntitySystem(GameLoopType = GameLoopType.Update, Layer = 1)]
    internal class CollisionSystem : EntitySystem
    {
        /// <summary>Initializes a new instance of the <see cref="CollisionSystem" /> class.</summary>
        public CollisionSystem()
            : base(Aspect.All(typeof(TransformComponent)))
        {
        }

        /// <summary>Override to implement code that gets executed when systems are initialized.</summary>
        public override void LoadContent()
        {
        }

        /// <summary>Processes the entities.</summary>
        /// <param name="entities">The entities.</param>
        protected override void ProcessEntities(IDictionary<int, Entity> entities)
        {
            Bag<Entity> bullets = this.EntityWorld.GroupManager.GetEntities("BULLETS");
            Bag<Entity> ships = this.EntityWorld.GroupManager.GetEntities("SHIPS");
            if (bullets != null && ships != null)
            {
                // being brutal !!!
                for (int shipIndex = 0; ships.Count > shipIndex; ++shipIndex)
                {
                    Entity ship = ships.Get(shipIndex);
                    for (int bulletIndex = 0; bullets.Count > bulletIndex; ++bulletIndex)
                    {
                        Entity bullet = bullets.Get(bulletIndex);
                        if (this.CollisionExists(bullet, ship))
                        {
                            var bulletTransform = bullet.GetComponent<TransformComponent>();
                            Entity bulletExplosion = this.EntityWorld.CreateEntityFromTemplate(BulletExplosionTemplate.Name);
                            bulletExplosion.GetComponent<TransformComponent>().Position = bulletTransform.Position;
                            bulletExplosion.Refresh();
                            bullet.Delete();
                            var healthComponent = ship.GetComponent<HealthComponent>();
                            healthComponent.AddDamage(4);
                            if (!healthComponent.IsAlive)
                            {
                                var shipTransform = ship.GetComponent<TransformComponent>();
                                Entity shipExplosion = this.EntityWorld.CreateEntityFromTemplate(ShipExplosionTemplate.Name);
                                shipExplosion.GetComponent<TransformComponent>().Position = shipTransform.Position;
                                shipExplosion.Refresh();
                                ship.Delete();
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>The collision exists.</summary>
        /// <param name="entity1">The entity 1.</param>
        /// <param name="entity2">The entity 2.</param>
        /// <returns>The <see cref="bool" />.</returns>
        private bool CollisionExists(Entity entity1, Entity entity2)
        {
            return Vector2.Distance(entity1.GetComponent<TransformComponent>().Position, entity2.GetComponent<TransformComponent>().Position) < 20;
        }
    }
}