namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Current state of a network session.
	/// </summary>
	public enum NetworkSessionState
    {
        /// <summary>
        /// Session is being created.
        /// </summary>
        Creating,

        /// <summary>
        /// Session is in the lobby, waiting for players.
        /// </summary>
        Lobby,

        /// <summary>
        /// Session is in an active game.
        /// </summary>
        Playing,

        /// <summary>
        /// Session has ended.
        /// </summary>
        Ended
    }
}
