using System;
using System.Threading.Tasks;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

namespace YourGameNamespace
{
    /// <summary>
    /// Template adapter for wiring achievement events into the shared toast manager.
    /// Copy into your game and replace placeholders.
    /// </summary>
    internal sealed class AchievementToastAdapter : IDisposable
    {
        private const string AchievementCategory = "achievement";

        private readonly ToastNotificationManager toasts;
        private readonly Texture2D fallbackIcon;

        internal event EventHandler<string> AchievementToastActivated;

        internal AchievementToastAdapter(GraphicsDevice graphicsDevice, ScreenManager screenManager, Texture2D fallbackIcon)
        {
            toasts = new ToastNotificationManager(graphicsDevice, screenManager)
            {
                ToastTopOffset = 140,
            };

            this.fallbackIcon = fallbackIcon;

            toasts.SetStyle(AchievementCategory, new ToastVisualStyle
            {
                FrameColor = new Color(244, 202, 112),
                TopFillColor = new Color(33, 24, 12, 230),
                BottomFillColor = new Color(17, 12, 8, 220),
                ShowAccentBar = true,
                AccentColor = new Color(255, 198, 104),
                AccentHighlightColor = new Color(255, 224, 146),
            });

            toasts.ToastActivated += OnToastActivated;
        }

        internal void EnqueueAchievement(string achievementKey, string title, string subtitle, Texture2D contentIcon = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;

            var notification = new ToastNotification
            {
                Identifier = achievementKey ?? string.Empty,
                Category = AchievementCategory,
                Header = "Achievement Unlocked",
                Title = title.Trim(),
                Subtitle = subtitle?.Trim() ?? string.Empty,
                IconTexture = contentIcon ?? fallbackIcon,
            };

            toasts.Enqueue(notification);

            // Optional platform icon fetch path. Keep texture creation in shared manager.
            if (notification.IconTexture == null && !string.IsNullOrWhiteSpace(achievementKey))
            {
                _ = ResolvePlatformIconAsync(notification, achievementKey);
            }
        }

        internal void Update(GameTime gameTime)
        {
            toasts.Update(gameTime);
        }

        internal bool HandleMouseRelease(Point location)
        {
            return toasts.HandleMouseRelease(location);
        }

        internal bool HandleTap(Vector2 location)
        {
            return toasts.HandleTap(location);
        }

        internal void Draw()
        {
            toasts.Draw();
        }

        private void OnToastActivated(object sender, ToastNotificationActivatedEventArgs e)
        {
            if (e?.Notification == null)
                return;

            if (!string.Equals(e.Notification.Category, AchievementCategory, StringComparison.OrdinalIgnoreCase))
                return;

            AchievementToastActivated?.Invoke(this, e.Notification.Identifier);
        }

        private static async Task ResolvePlatformIconAsync(ToastNotification notification, string achievementKey)
        {
            try
            {
                var icon = await SignedInGamer.Current.GetAchievementIconAsync(achievementKey).ConfigureAwait(false);
                if (icon?.Data != null && icon.Data.Length > 0)
                {
                    notification.PendingIconBytes = icon.Data;
                }
            }
            catch
            {
                // Keep fallback behavior when platform icons are unavailable.
            }
        }

        public void Dispose()
        {
            toasts.ToastActivated -= OnToastActivated;
            toasts.Dispose();
        }
    }
}
