namespace Microsoft.Xna.Framework.GamerServices
{
    /// <summary>
    /// Holds data used to write a leaderboard score.
    /// </summary>
    public sealed class LeaderboardWriter
    {
        public LeaderboardWriter(LeaderboardIdentity leaderboardIdentity)
            : this(leaderboardIdentity, SignedInGamer.Current)
        {
        }

        public LeaderboardWriter(LeaderboardIdentity leaderboardIdentity, SignedInGamer gamer)
        {
            LeaderboardIdentity = leaderboardIdentity ?? throw new ArgumentNullException(nameof(leaderboardIdentity));
            Gamer = gamer ?? throw new ArgumentNullException(nameof(gamer));
        }

        public LeaderboardIdentity LeaderboardIdentity { get; }
        public SignedInGamer Gamer { get; }
        public long Score { get; set; }
    }
}
