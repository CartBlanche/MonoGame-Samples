namespace CatapultGame
{
    /// <summary>
    /// Rules for the match about to be played. In a LAN game the host chooses them in the
    /// lobby and sends them to the other player.
    /// </summary>
    public static class MatchSettings
    {
        public const int DefaultWinScore = 5;

        /// <summary>
        /// Hits needed to win.
        /// </summary>
        public static int WinScore = DefaultWinScore;

        /// <summary>
        /// The next choice in the cycle 5, 10, 15, then back to 5.
        /// </summary>
        public static int NextWinScore(int current)
        {
            return current >= 15 ? DefaultWinScore : current + 5;
        }
    }
}
