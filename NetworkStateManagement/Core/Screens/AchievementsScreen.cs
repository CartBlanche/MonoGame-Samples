using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

namespace NetworkStateManagement
{
    /// <summary>
    /// Fetches and displays achievements from the active platform backend
    /// (Steam when signed in, local persistent storage otherwise).
    /// Demonstrates the full AchievementService.GetAchievementsAsync call chain.
    /// </summary>
    class AchievementsScreen : MenuScreen
    {
        private enum LoadState { Idle, Loading, Loaded, Error }

        private LoadState loadState = LoadState.Idle;
        private string errorMessage;
        private AchievementCollection achievements;
        private int scrollOffset;
        private CancellationTokenSource cts;

        public AchievementsScreen()
            : base("Achievements")
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
            achievements = null;
            errorMessage = null;
            scrollOffset = 0;
            loadState = LoadState.Loading;

            var token    = cts.Token;
            var gamer    = SignedInGamer.Current;
            // AchievementService.Provider returns LiveProvider (Steam) when wired up,
            // otherwise falls back to LocalProvider.
            var provider = AchievementService.Provider;

            // Achievements require a gamer identity. If no one is signed in
            // (e.g. itch.io / non-Steam build), show a prompt instead of fetching.
            if (gamer == null)
            {
                loadState    = LoadState.Loaded;
                achievements = new AchievementCollection(Array.Empty<Achievement>());
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    // GetAchievementsAsync returns all achievements with progress/earned state.
                    // Steam returns the full achievement list for the app (App 480 returns 0).
                    // Local provider returns only achievements registered in AchievementCatalog.
                    var result = await provider.GetAchievementsAsync(
                        gamer,
                        cancellationToken: token);

                    if (!token.IsCancellationRequested)
                    {
                        achievements = result;
                        loadState    = LoadState.Loaded;
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
            if (loadState == LoadState.Loaded && achievements != null)
            {
                if (input.IsMenuUp(ControllingPlayer) && scrollOffset > 0)
                    scrollOffset--;
                if (input.IsMenuDown(ControllingPlayer) && scrollOffset < achievements.Count - 1)
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
            string backend = isLive
                ? $"Backend: Steam ({SignedInGamer.Current.Gamertag})"
                : "Backend: Local (not signed in to Steam)";
            spriteBatch.DrawString(font, backend,
                new Vector2(x, y),
                (isLive ? Color.LimeGreen : Color.Yellow) * alpha);
            y += lineH * 1.5f;

            switch (loadState)
            {
                case LoadState.Loading:
                    spriteBatch.DrawString(font, "Fetching achievements...",
                        new Vector2(x, y), Color.White * alpha);
                    break;

                case LoadState.Error:
                    spriteBatch.DrawString(font, "Error fetching achievements:",
                        new Vector2(x, y), Color.OrangeRed * alpha);
                    y += lineH;
                    spriteBatch.DrawString(font, errorMessage ?? "unknown error",
                        new Vector2(x + 20, y), Color.OrangeRed * alpha);
                    break;

                case LoadState.Loaded:
                    if (achievements == null || achievements.Count == 0)
                    {
                        spriteBatch.DrawString(font, "No achievements found.",
                            new Vector2(x, y), Color.Gray * alpha);
                        spriteBatch.DrawString(font,
                            "(App 480 has no Steam achievements — register definitions",
                            new Vector2(x, y + lineH), Color.DimGray * alpha);
                        spriteBatch.DrawString(font,
                            " via AchievementCatalog.Register() to show local progress.)",
                            new Vector2(x, y + lineH * 2), Color.DimGray * alpha);
                    }
                    else
                    {
                        // Header
                        spriteBatch.DrawString(font,
                            $"Achievements ({achievements.Count} total)",
                            new Vector2(x, y), Color.White * alpha);
                        y += lineH;

                        // Rows (scrollable)
                        for (int i = scrollOffset; i < achievements.Count; i++)
                        {
                            if (y > ScreenManager.BackbufferHeight - 80) break;

                            var a = achievements[i];

                            // Earned = gold, in-progress = white, locked = gray
                            Color nameColor = a.IsEarned
                                ? Color.Gold
                                : a.PercentComplete > 0 ? Color.White : Color.Gray;

                            string earnedLabel = a.IsEarned
                                ? $" ✓ {a.EarnedDate:yyyy-MM-dd}"
                                : $" {a.PercentComplete:F0}%";

                            spriteBatch.DrawString(font,
                                $"{a.DisplayName}{earnedLabel}",
                                new Vector2(x, y), nameColor * alpha);
                            y += lineH;

                            // Description on the next line, slightly indented
                            if (!string.IsNullOrEmpty(a.Description))
                            {
                                spriteBatch.DrawString(font,
                                    $"  {a.Description}",
                                    new Vector2(x, y), Color.DimGray * alpha);
                                y += lineH;
                            }

                            y += 4; // small gap between entries
                        }

                        if (achievements.Count > 3)
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
