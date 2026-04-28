using System.Collections;

namespace Microsoft.Xna.Framework.GamerServices
{
    /// <summary>
    /// Reads leaderboard entries for a range.
    /// </summary>
    public sealed class LeaderboardReader : IDisposable, IEnumerable<LeaderboardEntry>
    {
        private readonly List<LeaderboardEntry> entries;

        internal LeaderboardReader(string leaderboardKey, int pageStart, int totalRowCount, List<LeaderboardEntry> entries)
        {
            LeaderboardKey = leaderboardKey;
            PageStart = pageStart;
            TotalRowCount = totalRowCount;
            this.entries = entries;
        }

        public string LeaderboardKey { get; }
        public int PageStart { get; }
        public int TotalRowCount { get; }
        public int Count => entries.Count;

        public LeaderboardEntry this[int index] => entries[index];

        public IEnumerator<LeaderboardEntry> GetEnumerator() => entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            // Included for API compatibility.
        }
    }
}
