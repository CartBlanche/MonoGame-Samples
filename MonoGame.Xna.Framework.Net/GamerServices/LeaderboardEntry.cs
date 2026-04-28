namespace Microsoft.Xna.Framework.GamerServices
{
    /// <summary>
    /// Represents a single leaderboard row.
    /// </summary>
    public sealed class LeaderboardEntry
    {
        public LeaderboardEntry(int rank, string gamertag, long score, bool isCurrentGamer)
        {
            Rank = rank;
            Gamertag = gamertag;
            Score = score;
            IsCurrentGamer = isCurrentGamer;
        }

        public int Rank { get; }
        public string Gamertag { get; }
        public long Score { get; }
        public bool IsCurrentGamer { get; }
    }
}
