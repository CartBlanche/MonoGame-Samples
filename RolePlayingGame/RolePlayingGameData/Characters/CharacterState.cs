//-----------------------------------------------------------------------------
// CharacterState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

namespace RolePlaying.Data
{
    public abstract partial class Character : WorldObject
    {
        /// <summary>
        /// The state of a character.
        /// </summary>
        public enum CharacterState
        {
            /// <summary>
            /// Ready to perform an action, and playing the idle animation
            /// </summary>
            Idle,

            /// <summary>
            /// Walking in the world.
            /// </summary>
            Walking,

            /// <summary>
            /// In defense mode
            /// </summary>
            Defending,

            /// <summary>
            /// Performing Dodge Animation
            /// </summary>
            Dodging,

            /// <summary>
            /// Performing Hit Animation
            /// </summary>
            Hit,

            /// <summary>
            /// Dead, but still playing the dying animation.
            /// </summary>
            Dying,

            /// <summary>
            /// Dead, with the dead animation.
            /// </summary>
            Dead,
        }
    }
}