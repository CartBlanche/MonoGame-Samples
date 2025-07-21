namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Defines gamer presence modes.
	/// </summary>
	public enum GamerPresenceMode
    {
        /// <summary>
        /// Not online.
        /// </summary>
        None,

        /// <summary>
        /// Online and available.
        /// </summary>
        Online,

        /// <summary>
        /// Away from keyboard.
        /// </summary>
        Away,

        /// <summary>
        /// Busy playing a game.
        /// </summary>
        Busy,

        /// <summary>
        /// Playing a specific game.
        /// </summary>
        PlayingGame,

        /// <summary>
        /// At the main menu.
        /// </summary>
        AtMenu,

        /// <summary>
        /// Waiting for other players.
        /// </summary>
        WaitingForPlayers,

        /// <summary>
        /// Waiting in lobby.
        /// </summary>
        WaitingInLobby,

        /// <summary>
        /// Currently winning.
        /// </summary>
        Winning,

        /// <summary>
        /// Currently losing.
        /// </summary>
        Losing,

        /// <summary>
        /// Score is tied.
        /// </summary>
        ScoreIsTied
    }
}
