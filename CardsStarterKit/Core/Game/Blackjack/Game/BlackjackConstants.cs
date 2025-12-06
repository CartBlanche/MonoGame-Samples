namespace Blackjack
{
    public static class BlackjackConstants
    {
        public const int MaxPlayers = 7;
        public const int MinPlayers = 1;

        /// <summary>
        /// Default NPC player names used to fill empty player slots.
        /// Now uses localized names from Resources.
        /// </summary>
        public static string[] DefaultAINames => new string[]
        {
            Resources.NPCPlayer1,
            Resources.NPCPlayer2,
            Resources.NPCPlayer3,
            Resources.NPCPlayer4,
            Resources.NPCPlayer5,
            Resources.NPCPlayer6
        };

        // Add other game logic constants here as needed
    }
}
