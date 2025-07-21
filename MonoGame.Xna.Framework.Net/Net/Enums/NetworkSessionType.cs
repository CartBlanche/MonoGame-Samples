namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Types of network sessions that can be created or joined.
	/// </summary>
	public enum NetworkSessionType
    {
        /// <summary>
        /// Local session for single-machine multiplayer.
        /// </summary>
        Local,

        /// <summary>
        /// System link session for LAN multiplayer.
        /// </summary>
        SystemLink,

        /// <summary>
        /// Player match session for online multiplayer.
        /// </summary>
        PlayerMatch,

        /// <summary>
        /// Ranked session for competitive online multiplayer.
        /// </summary>
        Ranked
    }
}
