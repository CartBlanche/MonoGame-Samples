#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnemyShooterSystem.cs" company="GAMADU.COM">
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
//   The enemy shooter system.
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

    using StarWarrior.Components;
    using StarWarrior.Templates;

    #endregion

    /// <summary>The enemy shooter system.</summary>
    [ArtemisEntitySystem(GameLoopType = GameLoopType.Update, Layer = 1)]
    public class EnemyShooterSystem : EntityProcessingSystem
    {
        /// <summary>The two seconds ticks.</summary>
        private static readonly long TwoSecondsTicks = TimeSpan.FromSeconds(2).Ticks;

        /// <summary>Initializes a new instance of the <see cref="EnemyShooterSystem" /> class.</summary>
        public EnemyShooterSystem()
            : base(Aspect.All(typeof(TransformComponent), typeof(WeaponComponent), typeof(EnemyComponent)))
        {
        }

        /// <summary>Override to implement code that gets executed when systems are initialized.</summary>
        public override void LoadContent()
        {
        }

        /// <summary>Processes the specified entity.</summary>
        /// <param name="entity">The entity.</param>
        public override void Process(Entity entity)
        {
            var weaponComponent = entity.GetComponent<WeaponComponent>();
            if (weaponComponent != null)
            {
                if ((weaponComponent.ShotAt + TwoSecondsTicks) < FastDateTime.Now.Ticks)
                {
                    var transformComponent = entity.GetComponent<TransformComponent>();
                    Entity missile = this.EntityWorld.CreateEntityFromTemplate(MissileTemplate.Name);
                    missile.GetComponent<TransformComponent>().X = transformComponent.X;
                    missile.GetComponent<TransformComponent>().Y = transformComponent.Y + 20;
                    missile.GetComponent<VelocityComponent>().Speed = -0.5f;
                    missile.GetComponent<VelocityComponent>().Angle = 270;
                    missile.Refresh();
                    weaponComponent.ShotAt = FastDateTime.Now.Ticks;
                }
            }
        }
    }
}