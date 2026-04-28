using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net.Tests
{
    [TestFixture]
    public class LeaderboardTests
    {
        private ILeaderboardProvider originalLiveProvider;
        private ILeaderboardProvider originalLocalProvider;

        [SetUp]
        public void SetUp()
        {
            originalLiveProvider = LeaderboardService.LiveProvider;
            originalLocalProvider = LeaderboardService.LocalProvider;

            LeaderboardService.LiveProvider = null;
            LeaderboardService.LocalProvider = new InMemoryLeaderboardProvider();

            SignedInGamer.Current.SetSignedInToLive(false);
        }

        [TearDown]
        public void TearDown()
        {
            LeaderboardService.LiveProvider = originalLiveProvider;
            LeaderboardService.LocalProvider = originalLocalProvider;
            SignedInGamer.Current.SetSignedInToLive(false);
        }

        [Test]
        public async Task WriteAndReadLeaderboardAsync_ReturnsRankedEntries()
        {
            var gamer = SignedInGamer.Current;
            var leaderboard = new LeaderboardIdentity($"test.async.{Guid.NewGuid():N}");

            var writer = new LeaderboardWriter(leaderboard, gamer)
            {
                Score = 1234
            };

            await gamer.WriteLeaderboardAsync(writer);

            using var reader = await gamer.GetLeaderboardAsync(leaderboard, pageStart: 0, pageSize: 10);

            Assert.That(reader.Count, Is.EqualTo(1));
            Assert.That(reader[0].Rank, Is.EqualTo(1));
            Assert.That(reader[0].Score, Is.EqualTo(1234));
            Assert.That(reader[0].Gamertag, Is.EqualTo(gamer.Gamertag));
        }

        [Test]
        public void WriteAndReadLeaderboard_SyncWrappersWork()
        {
            var gamer = SignedInGamer.Current;
            var leaderboard = new LeaderboardIdentity($"test.sync.{Guid.NewGuid():N}");

            var writer = new LeaderboardWriter(leaderboard, gamer)
            {
                Score = 2500
            };

            gamer.WriteLeaderboard(writer);

            using var reader = gamer.GetLeaderboard(leaderboard, pageStart: 0, pageSize: 5);

            Assert.That(reader.Count, Is.EqualTo(1));
            Assert.That(reader[0].Score, Is.EqualTo(2500));
        }

        [Test]
        public void WriteLeaderboardAsync_RespectsCancellation()
        {
            var gamer = SignedInGamer.Current;
            var leaderboard = new LeaderboardIdentity($"test.cancel.{Guid.NewGuid():N}");
            var writer = new LeaderboardWriter(leaderboard, gamer) { Score = 10 };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(() =>
                gamer.WriteLeaderboardAsync(writer, cts.Token));
        }

        [Test]
        public async Task WriteAndRead_WhenSignedOut_UsesLocalProvider()
        {
            var gamer = SignedInGamer.Current;
            gamer.SetSignedInToLive(false);

            var localProvider = new RecordingLeaderboardProvider();
            var liveProvider = new RecordingLeaderboardProvider();

            LeaderboardService.LocalProvider = localProvider;
            LeaderboardService.LiveProvider = liveProvider;

            var leaderboard = new LeaderboardIdentity($"test.route.local.{Guid.NewGuid():N}");
            var writer = new LeaderboardWriter(leaderboard, gamer) { Score = 99 };

            await gamer.WriteLeaderboardAsync(writer);
            using var _ = await gamer.GetLeaderboardAsync(leaderboard, 0, 10);

            Assert.That(localProvider.SubmitCallCount, Is.EqualTo(1));
            Assert.That(localProvider.ReadCallCount, Is.EqualTo(1));
            Assert.That(liveProvider.SubmitCallCount, Is.EqualTo(0));
            Assert.That(liveProvider.ReadCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task WriteAndRead_WhenSignedIn_UsesLiveProvider()
        {
            var gamer = SignedInGamer.Current;
            gamer.SetSignedInToLive(true);

            var localProvider = new RecordingLeaderboardProvider();
            var liveProvider = new RecordingLeaderboardProvider();

            LeaderboardService.LocalProvider = localProvider;
            LeaderboardService.LiveProvider = liveProvider;

            var leaderboard = new LeaderboardIdentity($"test.route.live.{Guid.NewGuid():N}");
            var writer = new LeaderboardWriter(leaderboard, gamer) { Score = 199 };

            await gamer.WriteLeaderboardAsync(writer);
            using var _ = await gamer.GetLeaderboardAsync(leaderboard, 0, 10);

            Assert.That(localProvider.SubmitCallCount, Is.EqualTo(0));
            Assert.That(localProvider.ReadCallCount, Is.EqualTo(0));
            Assert.That(liveProvider.SubmitCallCount, Is.EqualTo(1));
            Assert.That(liveProvider.ReadCallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task PersistentLocalProvider_PersistsAcrossProviderInstances()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), $"mgnet.lb.{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempRoot);
            var storagePath = Path.Combine(tempRoot, "leaderboards.json");

            try
            {
                var gamer = SignedInGamer.Current;
                var leaderboard = new LeaderboardIdentity($"test.persist.{Guid.NewGuid():N}");

                var providerA = new PersistentLocalLeaderboardProvider(storagePath);
                await providerA.SubmitAsync(new LeaderboardWriter(leaderboard, gamer) { Score = 4200 });

                var providerB = new PersistentLocalLeaderboardProvider(storagePath);
                using var reader = await providerB.ReadAsync(leaderboard, 0, 10, null);

                Assert.That(reader.Count, Is.EqualTo(1));
                Assert.That(reader[0].Score, Is.EqualTo(4200));
                Assert.That(reader[0].Gamertag, Is.EqualTo(gamer.Gamertag));
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void UsePersistentLocalStorage_UsesGameFolderName()
        {
            var gameName = $"RoyalFlushRush.Test.{Guid.NewGuid():N}";

            LeaderboardService.UsePersistentLocalStorage(gameName);

            var provider = LeaderboardService.LocalProvider as PersistentLocalLeaderboardProvider;
            Assert.That(provider, Is.Not.Null);
            Assert.That(provider.StoragePath, Does.Contain(gameName));
        }

        private sealed class RecordingLeaderboardProvider : ILeaderboardProvider
        {
            private readonly List<LeaderboardEntry> entries = new();

            public int SubmitCallCount { get; private set; }
            public int ReadCallCount { get; private set; }

            public Task SubmitAsync(LeaderboardWriter writer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SubmitCallCount++;

                entries.Add(new LeaderboardEntry(
                    rank: entries.Count + 1,
                    gamertag: writer.Gamer.Gamertag,
                    score: writer.Score,
                    isCurrentGamer: false));

                return Task.CompletedTask;
            }

            public Task<LeaderboardReader> ReadAsync(
                LeaderboardIdentity identity,
                int pageStart,
                int pageSize,
                SignedInGamer pivotGamer = null,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReadCallCount++;

                var page = entries.Skip(pageStart).Take(pageSize).ToList();
                return Task.FromResult(new LeaderboardReader(identity.Key, pageStart, entries.Count, page));
            }
        }
    }
}
