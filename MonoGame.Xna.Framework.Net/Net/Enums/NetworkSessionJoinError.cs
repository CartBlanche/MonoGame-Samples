using System;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Net
{

    /// <summary>
    /// Types of network session join errors.
    /// </summary>
    public enum NetworkSessionJoinError
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        Unknown,

        /// <summary>
        /// Session is full.
        /// </summary>
        SessionFull,

        /// <summary>
        /// Session not found.
        /// </summary>
        SessionNotFound,

        /// <summary>
        /// Session not joinable.
        /// </summary>
        SessionNotJoinable
    }
}
