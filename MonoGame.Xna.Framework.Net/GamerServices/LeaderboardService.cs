namespace Microsoft.Xna.Framework.GamerServices
{
    /// <summary>
    /// Contract for leaderboard backends.
    /// </summary>
    public interface ILeaderboardProvider
    {
        Task SubmitAsync(LeaderboardWriter writer, CancellationToken cancellationToken = default);
        Task<LeaderboardReader> ReadAsync(
            LeaderboardIdentity identity,
            int pageStart,
            int pageSize,
            SignedInGamer pivotGamer = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Global leaderboard provider hook used by GamerServices APIs.
    /// </summary>
    public static class LeaderboardService
    {
        private const string DefaultLocalStorageFolder = "MonoGame.Xna.Framework.Net";
        private static ILeaderboardProvider liveProvider;
        private static ILeaderboardProvider localProvider = new PersistentLocalLeaderboardProvider();

        /// <summary>
        /// Gets or sets the online/live provider used when a gamer is signed in.
        /// </summary>
        public static ILeaderboardProvider LiveProvider
        {
            get => liveProvider;
            set => liveProvider = value;
        }

        /// <summary>
        /// Gets or sets the local fallback provider used when a gamer is not signed in.
        /// </summary>
        public static ILeaderboardProvider LocalProvider
        {
            get => localProvider;
            set => localProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Backward-compatible alias for the live provider.
        /// </summary>
        public static ILeaderboardProvider Provider
        {
            get => LiveProvider ?? LocalProvider;
            set => LiveProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Configures local persistent leaderboard storage for a specific game folder.
        /// Call this once during game startup to keep leaderboard data near your game settings files.
        /// </summary>
        /// <param name="gameName">Folder name used under platform-local app data.</param>
        public static void UsePersistentLocalStorage(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
                throw new ArgumentException("Game name cannot be empty.", nameof(gameName));

            LocalProvider = PersistentLocalLeaderboardProvider.CreateForGame(gameName.Trim());
        }

        internal static string LocalStorageFolderName => DefaultLocalStorageFolder;

        internal static ILeaderboardProvider ResolveProvider(SignedInGamer gamer)
        {
            if (gamer != null && gamer.IsSignedInToLive && LiveProvider != null)
                return LiveProvider;

            return LocalProvider;
        }
    }

    internal sealed class InMemoryLeaderboardProvider : ILeaderboardProvider
    {
        private sealed class Submission
        {
            public string GamerKey = string.Empty;
            public string Gamertag = string.Empty;
            public long Score;
            public DateTime UpdatedUtc;
        }

        private static readonly object Gate = new();
        private static readonly Dictionary<string, Dictionary<string, Submission>> Tables = new(StringComparer.Ordinal);

        public Task SubmitAsync(LeaderboardWriter writer, CancellationToken cancellationToken = default)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            cancellationToken.ThrowIfCancellationRequested();

            lock (Gate)
            {
                if (!Tables.TryGetValue(writer.LeaderboardIdentity.Key, out var table))
                {
                    table = new Dictionary<string, Submission>(StringComparer.Ordinal);
                    Tables[writer.LeaderboardIdentity.Key] = table;
                }

                var gamerKey = writer.Gamer.Gamertag;
                if (!table.TryGetValue(gamerKey, out var existing))
                {
                    table[gamerKey] = new Submission
                    {
                        GamerKey = gamerKey,
                        Gamertag = writer.Gamer.Gamertag,
                        Score = writer.Score,
                        UpdatedUtc = DateTime.UtcNow
                    };
                    return Task.CompletedTask;
                }

                if (writer.Score > existing.Score)
                {
                    existing.Score = writer.Score;
                    existing.UpdatedUtc = DateTime.UtcNow;
                    existing.Gamertag = writer.Gamer.Gamertag;
                }
            }

            return Task.CompletedTask;
        }

        public Task<LeaderboardReader> ReadAsync(
            LeaderboardIdentity identity,
            int pageStart,
            int pageSize,
            SignedInGamer pivotGamer = null,
            CancellationToken cancellationToken = default)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (pageStart < 0)
                throw new ArgumentOutOfRangeException(nameof(pageStart));
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize));

            cancellationToken.ThrowIfCancellationRequested();

            lock (Gate)
            {
                if (!Tables.TryGetValue(identity.Key, out var table) || table.Count == 0)
                {
                    return Task.FromResult(new LeaderboardReader(identity.Key, pageStart, 0, new List<LeaderboardEntry>()));
                }

                var ordered = table.Values
                    .OrderByDescending(s => s.Score)
                    .ThenBy(s => s.UpdatedUtc)
                    .ThenBy(s => s.GamerKey, StringComparer.Ordinal)
                    .ToList();

                if (pivotGamer != null)
                {
                    var pivotIndex = ordered.FindIndex(s => s.GamerKey == pivotGamer.Gamertag);
                    if (pivotIndex >= 0)
                    {
                        var half = pageSize / 2;
                        pageStart = Math.Max(0, pivotIndex - half);
                    }
                }

                var rows = ordered
                    .Select((s, i) => new { Submission = s, Rank = i + 1 })
                    .Skip(pageStart)
                    .Take(pageSize)
                    .Select(x => new LeaderboardEntry(
                        x.Rank,
                        x.Submission.Gamertag,
                        x.Submission.Score,
                        pivotGamer != null && x.Submission.GamerKey == pivotGamer.Gamertag))
                    .ToList();

                return Task.FromResult(new LeaderboardReader(identity.Key, pageStart, ordered.Count, rows));
            }
        }
    }

    internal sealed class PersistentLocalLeaderboardProvider : ILeaderboardProvider
    {
        private sealed class Submission
        {
            public string GamerKey { get; set; } = string.Empty;
            public string Gamertag { get; set; } = string.Empty;
            public long Score { get; set; }
            public DateTime UpdatedUtc { get; set; }
        }

        private sealed class StorageModel
        {
            public Dictionary<string, List<Submission>> Tables { get; set; } = new(StringComparer.Ordinal);
        }

        private readonly object gate = new();
        private readonly string storagePath;
        private bool isLoaded;
        private Dictionary<string, Dictionary<string, Submission>> tables = new(StringComparer.Ordinal);

        internal string StoragePath => storagePath;

        public PersistentLocalLeaderboardProvider()
            : this(GetDefaultStoragePath(LeaderboardService.LocalStorageFolderName))
        {
        }

        internal static PersistentLocalLeaderboardProvider CreateForGame(string gameName)
        {
            return new PersistentLocalLeaderboardProvider(GetDefaultStoragePath(gameName));
        }

        internal PersistentLocalLeaderboardProvider(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                throw new ArgumentException("Storage path cannot be empty.", nameof(storagePath));

            this.storagePath = storagePath;
        }

        public Task SubmitAsync(LeaderboardWriter writer, CancellationToken cancellationToken = default)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                EnsureLoaded();

                if (!tables.TryGetValue(writer.LeaderboardIdentity.Key, out var table))
                {
                    table = new Dictionary<string, Submission>(StringComparer.Ordinal);
                    tables[writer.LeaderboardIdentity.Key] = table;
                }

                var gamerKey = writer.Gamer.Gamertag;
                if (!table.TryGetValue(gamerKey, out var existing))
                {
                    table[gamerKey] = new Submission
                    {
                        GamerKey = gamerKey,
                        Gamertag = writer.Gamer.Gamertag,
                        Score = writer.Score,
                        UpdatedUtc = DateTime.UtcNow
                    };

                    SaveLocked();
                    return Task.CompletedTask;
                }

                if (writer.Score > existing.Score)
                {
                    existing.Score = writer.Score;
                    existing.UpdatedUtc = DateTime.UtcNow;
                    existing.Gamertag = writer.Gamer.Gamertag;
                    SaveLocked();
                }
            }

            return Task.CompletedTask;
        }

        public Task<LeaderboardReader> ReadAsync(
            LeaderboardIdentity identity,
            int pageStart,
            int pageSize,
            SignedInGamer pivotGamer = null,
            CancellationToken cancellationToken = default)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (pageStart < 0)
                throw new ArgumentOutOfRangeException(nameof(pageStart));
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize));

            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                EnsureLoaded();

                if (!tables.TryGetValue(identity.Key, out var table) || table.Count == 0)
                {
                    return Task.FromResult(new LeaderboardReader(identity.Key, pageStart, 0, new List<LeaderboardEntry>()));
                }

                var ordered = table.Values
                    .OrderByDescending(s => s.Score)
                    .ThenBy(s => s.UpdatedUtc)
                    .ThenBy(s => s.GamerKey, StringComparer.Ordinal)
                    .ToList();

                if (pivotGamer != null)
                {
                    var pivotIndex = ordered.FindIndex(s => s.GamerKey == pivotGamer.Gamertag);
                    if (pivotIndex >= 0)
                    {
                        var half = pageSize / 2;
                        pageStart = Math.Max(0, pivotIndex - half);
                    }
                }

                var rows = ordered
                    .Select((s, i) => new { Submission = s, Rank = i + 1 })
                    .Skip(pageStart)
                    .Take(pageSize)
                    .Select(x => new LeaderboardEntry(
                        x.Rank,
                        x.Submission.Gamertag,
                        x.Submission.Score,
                        pivotGamer != null && x.Submission.GamerKey == pivotGamer.Gamertag))
                    .ToList();

                return Task.FromResult(new LeaderboardReader(identity.Key, pageStart, ordered.Count, rows));
            }
        }

        private static string GetDefaultStoragePath(string appFolderName)
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = AppContext.BaseDirectory;

            if (OperatingSystem.IsAndroid())
                root = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            else if (OperatingSystem.IsIOS())
                root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            return Path.Combine(root, appFolderName, "leaderboards.json");
        }

        private void EnsureLoaded()
        {
            if (isLoaded)
                return;

            isLoaded = true;

            if (!File.Exists(storagePath))
                return;

            try
            {
                var json = File.ReadAllText(storagePath);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var model = System.Text.Json.JsonSerializer.Deserialize<StorageModel>(json);
                if (model?.Tables == null)
                    return;

                tables = model.Tables.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.ToDictionary(s => s.GamerKey, s => s, StringComparer.Ordinal),
                    StringComparer.Ordinal);
            }
            catch
            {
                // Corrupt or unreadable local cache should not block gameplay.
                tables = new Dictionary<string, Dictionary<string, Submission>>(StringComparer.Ordinal);
            }
        }

        private void SaveLocked()
        {
            var dir = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var model = new StorageModel
            {
                Tables = tables.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Values.ToList(),
                    StringComparer.Ordinal)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(model);
            File.WriteAllText(storagePath, json);
        }
    }
}
