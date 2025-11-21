namespace Blackjack
{
    public static class BlackjackConstants
    {
        public const int MaxPlayers = 7;
        public const int MinPlayers = 1;

        /// <summary>
        /// Default AI player names used to fill empty player slots.
        /// Now uses localized names from Resources.
        /// </summary>
        public static string[] DefaultAINames => new string[]
        {
            Resources.AIPlayer1,
            Resources.AIPlayer2,
            Resources.AIPlayer3,
            Resources.AIPlayer4,
            Resources.AIPlayer5,
            Resources.AIPlayer6
        };

        // Add other game logic constants here as needed
    }
}
