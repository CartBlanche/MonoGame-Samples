using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

namespace NetworkStateManagement
{
    /// <summary>
    /// Fetches and displays leaderboard entries from the active platform backend
    /// (Steam when signed in, local persistent storage otherwise).
    /// Demonstrates the full LeaderboardService.ReadAsync call chain.
    /// </summary>
    class LeaderboardsScreen : MenuScreen
    {
        // Leaderboard key — replace with your real leaderboard name.
        // App 480 (Steamworks test app) has no leaderboards, so Steam will return 0 rows.
        private const string LeaderboardKey = "HighScores";
        private const int PageSize = 10;

        private enum LoadState { Idle, Loading, Loaded, Error }

        private LoadState loadState = LoadState.Idle;
        private string errorMessage;
        private LeaderboardReader reader;
        private int scrollOffset;
        private CancellationTokenSource cts;

        public LeaderboardsScreen()
            : base("Leaderboards")
        {
            MenuEntry refreshMenuEntry = new MenuEntry("Refresh");
            MenuEntry backMenuEntry    = new MenuEntry("Back");
            refreshMenuEntry.Selected += (s, e) => BeginFetch();
            backMenuEntry.Selected    += OnCancel;
            MenuEntries.Add(refreshMenuEntry);
            MenuEntries.Add(backMenuEntry);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            BeginFetch();
        }

        public override void UnloadContent()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            base.UnloadContent();
        }

        private void BeginFetch()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            reader = null;
            errorMessage = null;
            scrollOffset = 0;
            loadState = LoadState.Loading;

            var token  = cts.Token;
            var gamer  = SignedInGamer.Current;
            // LeaderboardService.Provider returns LiveProvider (Steam) when it has been
            // wired up by SteamPlatformBootstrap, otherwise falls back to LocalProvider.
            var provider = LeaderboardService.Provider;
            var identity = new LeaderboardIdentity(LeaderboardKey);

            Task.Run(async () =>
            {
                try
                {
                    // ReadAsync(identity, pageStart, pageSize, pivotGamer)
                    // pageStart = 0  → top of the leaderboard
                    // pivotGamer     → pass SignedInGamer.Current to include local rank
                    var result = await provider.ReadAsync(
                        identity,
                        pageStart: 0,
                        pageSize:  PageSize,
                        pivotGamer: gamer,
                        cancellationToken: token);

                    if (!token.IsCancellationRequested)
                    {
                        reader    = result;
                        loadState = LoadState.Loaded;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        errorMessage = ex.Message;
                        loadState    = LoadState.Error;
                    }
                }
            }, token);
        }

        public override void HandleInput(InputState input)
        {
            // Scroll loaded results with up/down
            if (loadState == LoadState.Loaded && reader != null)
            {
                if (input.IsMenuUp(ControllingPlayer) && scrollOffset > 0)
                    scrollOffset--;
                if (input.IsMenuDown(ControllingPlayer) && scrollOffset < reader.Count - 1)
                    scrollOffset++;
            }

            base.HandleInput(input);
        }

        public override void Draw(GameTime gameTime)
        {
            var spriteBatch = ScreenManager.SpriteBatch;
            var font        = ScreenManager.Font;
            float alpha     = TransitionAlpha;

            spriteBatch.Begin();

            float x = 80f;
            float y = 80f;
            float lineH = font.LineSpacing + 4;

            // -- Backend status line --
            bool isLive = SignedInGamer.Current?.IsSignedInToLive == true;
            string backend  = isLive
                ? $"Backend: Steam ({SignedInGamer.Current.Gamertag})"
                : "Backend: Local (not signed in to Steam)";
            spriteBatch.DrawString(font, backend,
                new Vector2(x, y),
                (isLive ? Color.LimeGreen : Color.Yellow) * alpha);
            y += lineH * 1.5f;

            switch (loadState)
            {
                case LoadState.Loading:
                    spriteBatch.DrawString(font, "Fetching leaderboard...",
                        new Vector2(x, y), Color.White * alpha);
                    break;

                case LoadState.Error:
                    spriteBatch.DrawString(font, "Error fetching leaderboard:",
                        new Vector2(x, y), Color.OrangeRed * alpha);
                    y += lineH;
                    spriteBatch.DrawString(font, errorMessage ?? "unknown error",
                        new Vector2(x + 20, y), Color.OrangeRed * alpha);
                    break;

                case LoadState.Loaded:
                    if (reader == null || reader.Count == 0)
                    {
                        spriteBatch.DrawString(font,
                            $"No entries found for '{LeaderboardKey}'.",
                            new Vector2(x, y), Color.Gray * alpha);
                        spriteBatch.DrawString(font,
                            "(App 480 has no Steam leaderboards — local scores will appear here.)",
                            new Vector2(x, y + lineH), Color.DimGray * alpha);
                    }
                    else
                    {
                        // Header
                        spriteBatch.DrawString(font,
                            $"{LeaderboardKey}  ({reader.TotalRowCount} total entries)",
                            new Vector2(x, y), Color.White * alpha);
                        y += lineH;

                        // Column headers
                        spriteBatch.DrawString(font, "Rank  Gamertag                Score",
                            new Vector2(x, y), Color.Gray * alpha);
                        y += lineH;

                        // Rows (scrollable)
                        for (int i = scrollOffset; i < reader.Count; i++)
                        {
                            if (y > ScreenManager.BackbufferHeight - 80) break;

                            var entry = reader[i];
                            Color rowColor = entry.IsCurrentGamer
                                ? Color.Yellow
                                : Color.White;
                            string row = $"{entry.Rank,4}  {entry.Gamertag,-22} {entry.Score,8:N0}";
                            spriteBatch.DrawString(font, row,
                                new Vector2(x, y), rowColor * alpha);
                            y += lineH;
                        }

                        if (reader.Count > 1)
                            spriteBatch.DrawString(font, "Up/Down to scroll",
                                new Vector2(x, ScreenManager.BackbufferHeight - 60f),
                                Color.DimGray * alpha);
                    }
                    break;
            }

            spriteBatch.End();

            // Draw menu entries on top
            base.Draw(gameTime);
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ExitScreen();
        }
    }
}
