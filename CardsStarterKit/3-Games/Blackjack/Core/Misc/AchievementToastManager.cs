using System;
using System.Collections.Generic;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    internal sealed class AchievementToastManager : IDisposable
    {
        private const string AchievementCategory = "achievement";
        private const string NetworkCategory = "network";

        internal sealed class ToastActivatedEventArgs : EventArgs
        {
            public ToastActivatedEventArgs(string achievementKey, string title)
            {
                AchievementKey = achievementKey ?? string.Empty;
                Title = title ?? string.Empty;
            }

            public string AchievementKey { get; }

            public string Title { get; }
        }

        private readonly ToastNotificationManager _toasts;
        private readonly Texture2D _fallbackIcon;

        internal event EventHandler<ToastActivatedEventArgs> ToastActivated;

        internal AchievementToastManager(GraphicsDevice graphicsDevice, ScreenManager screenManager)
        {
            _ = screenManager ?? throw new ArgumentNullException(nameof(screenManager));
            _toasts = new ToastNotificationManager(graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice)), screenManager);
            _toasts.ToastActivated += OnToastActivated;

            _toasts.SetStyle(AchievementCategory, new ToastVisualStyle
            {
                FrameColor = new Color(172, 138, 68),
                TopFillColor = new Color(15, 56, 43, 230),
                BottomFillColor = new Color(10, 34, 27, 220),
            });

            _toasts.SetStyle(NetworkCategory, new ToastVisualStyle
            {
                FrameColor = new Color(95, 113, 125),
                TopFillColor = new Color(26, 37, 49, 230),
                BottomFillColor = new Color(19, 28, 39, 220),
            });

            try
            {
                _fallbackIcon = screenManager.Game.Content.Load<Texture2D>("Images/UI/ring");
            }
            catch
            {
                _fallbackIcon = null;
            }
        }

        internal void Enqueue(string achievementKey, string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;

            _toasts.Enqueue(new ToastNotification
            {
                Identifier = achievementKey ?? string.Empty,
                Category = AchievementCategory,
                Header = AchievementLocalization.GetAchievementUnlockedHeaderText(),
                Title = title.Trim(),
                IconTexture = _fallbackIcon,
            });
        }

        internal void EnqueueNetworkMessage(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return;

            _toasts.Enqueue(new ToastNotification
            {
                Category = NetworkCategory,
                Header = Resources.NetworkBusy,
                Title = title.Trim(),
            });
        }

        internal void Update(GameTime gameTime)
        {
            _toasts.Update(gameTime);
        }

        internal bool HandleMouseRelease(Point location)
        {
            return _toasts.HandleMouseRelease(location);
        }

        internal bool HandleTap(Vector2 location)
        {
            return _toasts.HandleTap(location);
        }

        internal void Draw()
        {
            _toasts.Draw();
        }

        private void OnToastActivated(object sender, ToastNotificationActivatedEventArgs e)
        {
            if (e?.Notification == null)
                return;

            if (string.Equals(e.Notification.Category, AchievementCategory, StringComparison.OrdinalIgnoreCase))
            {
                ToastActivated?.Invoke(this, new ToastActivatedEventArgs(e.Notification.Identifier, e.Notification.Title));
            }
        }

        public void Dispose()
        {
            _toasts.ToastActivated -= OnToastActivated;
            _toasts.Dispose();
        }
    }
}