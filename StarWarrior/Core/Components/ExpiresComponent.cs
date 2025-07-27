#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExpiresComponent.cs" company="GAMADU.COM">
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
//   The expires.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace StarWarrior.Components
{
    #region Using statements

    using Artemis.Interface;

    #endregion

    /// <summary>The expires.</summary>
    internal class ExpiresComponent : IComponent
    {
        /// <summary>Initializes a new instance of the <see cref="ExpiresComponent" /> class.</summary>
        public ExpiresComponent() 
            : this(0.0f)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ExpiresComponent" /> class.</summary>
        /// <param name="lifeTime">The life time.</param>
        public ExpiresComponent(float lifeTime)
        {
            this.LifeTime = lifeTime;
        }

        /// <summary>Gets a value indicating whether is expired.</summary>
        /// <value><see langword="true" /> if this instance is expired; otherwise, <see langword="false" />.</value>
        public bool IsExpired
        {
            get
            {
                return this.LifeTime <= 0;
            }
        }

        /// <summary>Gets or sets the life time.</summary>
        /// <value>The life time.</value>
        public float LifeTime { get; set; }

        /// <summary>The reduce life time.</summary>
        /// <param name="lifeTimeDelta">The life time.</param>
        public void ReduceLifeTime(float lifeTimeDelta)
        {
            this.LifeTime -= lifeTimeDelta;
            if (this.LifeTime < 0)
            {
                this.LifeTime = 0;
            }
        }
    }
}