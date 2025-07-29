#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HealthComponent.cs" company="GAMADU.COM">
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
//   The health.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Components
{
    #region Using statements

    using System;

    using Artemis.Interface;

    #endregion

    /// <summary>The health.</summary>
    internal class HealthComponent : IComponent
    {
        /// <summary>Initializes a new instance of the <see cref="HealthComponent"/> class.</summary>
        public HealthComponent() 
            : this(0.0f)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="HealthComponent"/> class.</summary>
        /// <param name="points">The points.</param>
        public HealthComponent(float points)
        {
            this.Points = this.MaximumHealth = points;
        }

        /// <summary>Gets or sets the health points.</summary>
        /// <value>The Points.</value>
        public float Points { get; set; }

        /// <summary>Gets the health percentage.</summary>
        /// <value>The health percentage.</value>
        public double HealthPercentage
        {
            get
            {
                return Math.Round(this.Points / this.MaximumHealth * 100f);
            }
        }

        /// <summary>Gets a value indicating whether is alive.</summary>
        /// <value><see langword="true" /> if this instance is alive; otherwise, <see langword="false" />.</value>
        public bool IsAlive
        {
            get
            {
                return this.Points > 0;
            }
        }

        /// <summary>Gets the maximum health.</summary>
        /// <value>The maximum health.</value>
        public float MaximumHealth { get; private set; }

        /// <summary>The add damage.</summary>
        /// <param name="damage">The damage.</param>
        public void AddDamage(int damage)
        {
            this.Points -= damage;
            if (this.Points < 0)
            {
                this.Points = 0;
            }
        }
    }
}