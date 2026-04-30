using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.GamerServices;
using Steamworks;

namespace Microsoft.Xna.Framework.Net.Steam
{
    /// <summary>
    /// ILeaderboardProvider backed by the Steam leaderboard API (SteamUserStats).
    ///
    /// Prerequisites:
    ///   - SteamAPI.Init() called before use (composition root responsibility).
    ///   - SteamAPI.RunCallbacks() called each frame so CallResult callbacks fire.
    ///   - Leaderboard names in Steam App Admin must match the LeaderboardIdentity.Key values.
    ///   - Scores are submitted as best-score (Steam keeps the player's personal best automatically).
    ///
    /// Note: Steam leaderboard scores are 32-bit signed integers. Scores larger than
    /// int.MaxValue are clamped before upload.
    /// </summary>
    public sealed class SteamLeaderboardProvider : ILeaderboardProvider
    {
        private readonly Dictionary<string, SteamLeaderboard_t> handleCache = new(StringComparer.Ordinal);
        private readonly object cacheLock = new();

        // ------------------------------------------------------------------ SubmitAsync

        public async Task SubmitAsync(LeaderboardWriter writer, CancellationToken cancellationToken = default)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            cancellationToken.ThrowIfCancellationRequested();

            var handle = await FindLeaderboardAsync(writer.LeaderboardIdentity.Key, cancellationToken).ConfigureAwait(false);

            // Steam scores are 32-bit; clamp rather than overflow.
            var score = (int)Math.Clamp(writer.Score, int.MinValue, int.MaxValue);

            var tcs = new TaskCompletionSource<LeaderboardScoreUploaded_t>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callResult = CallResult<LeaderboardScoreUploaded_t>.Create((result, ioFailure) =>
            {
                if (ioFailure)
                    tcs.TrySetException(new InvalidOperationException("Steam leaderboard upload failed (IO failure)."));
                else
                    tcs.TrySetResult(result);
            });

            var apiCall = SteamUserStats.UploadLeaderboardScore(
                handle,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score,
                null,
                0);

            callResult.Set(apiCall);

            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            await tcs.Task.ConfigureAwait(false);
        }

        // ------------------------------------------------------------------ ReadAsync

        public async Task<LeaderboardReader> ReadAsync(
            LeaderboardIdentity identity,
            int pageStart,
            int pageSize,
            SignedInGamer pivotGamer = null,
            CancellationToken cancellationToken = default)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize));

            cancellationToken.ThrowIfCancellationRequested();

            var handle = await FindLeaderboardAsync(identity.Key, cancellationToken).ConfigureAwait(false);

            ELeaderboardDataRequest request;
            int rangeStart;
            int rangeEnd;

            if (pivotGamer != null)
            {
                // Centre the window around the current user's rank.
                // Steam uses signed relative offsets for GlobalAroundUser:
                //   0 = user's own rank; negative = above; positive = below.
                request = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser;
                int half = pageSize / 2;
                rangeStart = -half;
                rangeEnd = pageSize - half - 1;
            }
            else
            {
                // Steam ranks are 1-based; pageStart is 0-based.
                request = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal;
                rangeStart = pageStart + 1;
                rangeEnd = pageStart + pageSize;
            }

            var tcs = new TaskCompletionSource<LeaderboardScoresDownloaded_t>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callResult = CallResult<LeaderboardScoresDownloaded_t>.Create((result, ioFailure) =>
            {
                if (ioFailure)
                    tcs.TrySetException(new InvalidOperationException("Steam leaderboard download failed (IO failure)."));
                else
                    tcs.TrySetResult(result);
            });

            var dlApiCall = SteamUserStats.DownloadLeaderboardEntries(handle, request, rangeStart, rangeEnd);
            callResult.Set(dlApiCall);

            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            var downloaded = await tcs.Task.ConfigureAwait(false);

            var totalCount = SteamUserStats.GetLeaderboardEntryCount(handle);
            var currentUserId = SteamUser.GetSteamID();

            var entries = new List<LeaderboardEntry>(downloaded.m_cEntryCount);
            for (int i = 0; i < downloaded.m_cEntryCount; i++)
            {
                SteamUserStats.GetDownloadedLeaderboardEntry(
                    downloaded.m_hSteamLeaderboardEntries, i, out var entry, null, 0);

                var gamertag = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
                if (string.IsNullOrWhiteSpace(gamertag))
                    gamertag = entry.m_steamIDUser.ToString();

                var isCurrentGamer = entry.m_steamIDUser == currentUserId;

                entries.Add(new LeaderboardEntry(entry.m_nGlobalRank, gamertag, entry.m_nScore, isCurrentGamer));
            }

            // pageStart for the reader: derive from first entry's rank if available (pivot shifts it).
            int readerPageStart = entries.Count > 0 ? entries[0].Rank - 1 : pageStart;

            return new LeaderboardReader(identity.Key, readerPageStart, totalCount, entries);
        }

        // ------------------------------------------------------------------ FindLeaderboardAsync

        private async Task<SteamLeaderboard_t> FindLeaderboardAsync(string key, CancellationToken cancellationToken)
        {
            lock (cacheLock)
            {
                if (handleCache.TryGetValue(key, out var cached))
                    return cached;
            }

            var tcs = new TaskCompletionSource<LeaderboardFindResult_t>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callResult = CallResult<LeaderboardFindResult_t>.Create((result, ioFailure) =>
            {
                if (ioFailure)
                    tcs.TrySetException(new InvalidOperationException($"Steam FindLeaderboard failed for '{key}' (IO failure)."));
                else if (result.m_bLeaderboardFound == 0)
                    tcs.TrySetException(new InvalidOperationException($"Leaderboard '{key}' not found in Steam App Admin. Ensure the name matches exactly."));
                else
                    tcs.TrySetResult(result);
            });

            var apiCall = SteamUserStats.FindLeaderboard(key);
            callResult.Set(apiCall);

            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            var findResult = await tcs.Task.ConfigureAwait(false);

            lock (cacheLock)
            {
                handleCache[key] = findResult.m_hSteamLeaderboard;
            }

            return findResult.m_hSteamLeaderboard;
        }
    }
}
