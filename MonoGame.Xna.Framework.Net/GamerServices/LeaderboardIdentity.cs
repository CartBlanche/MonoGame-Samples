namespace Microsoft.Xna.Framework.GamerServices
{
    /// <summary>
    /// Identifies a leaderboard by key.
    /// </summary>
    public sealed class LeaderboardIdentity
    {
        public LeaderboardIdentity(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Leaderboard key cannot be empty.", nameof(key));

            Key = key;
        }

        public string Key { get; }
    }
}
