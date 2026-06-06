using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework.Core
{
    /// <summary>
    /// Describes a single toast notification item rendered by <see cref="ToastNotificationManager"/>.
    /// </summary>
    public sealed class ToastNotification
    {
        /// <summary>
        /// Stable identifier for this toast, typically a domain key (for example an achievement id).
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// Category name used to pick a style override (for example "achievement" or "network").
        /// </summary>
        public string Category { get; set; } = "default";

        /// <summary>
        /// Optional short header line shown above the title.
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// Main toast text.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional secondary line shown beneath the title when present.
        /// </summary>
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Optional icon texture. When null, callers can provide bytes via <see cref="PendingIconBytes"/>.
        /// </summary>
        public Texture2D IconTexture { get; set; }

        /// <summary>
        /// Optional encoded image bytes to be promoted to a <see cref="Texture2D"/> during Update on the game thread.
        /// </summary>
        public byte[] PendingIconBytes { get; set; }

        /// <summary>
        /// Optional caller-owned payload for activation handlers.
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// Optional deduplication key used when key deduplication is enabled.
        /// If empty, <see cref="Identifier"/> is used as a fallback key.
        /// </summary>
        public string DeduplicationKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Visual style for a toast category.
    /// </summary>
    public sealed class ToastVisualStyle
    {
        public Color FrameColor { get; set; } = new Color(172, 138, 68);
        public Color TopFillColor { get; set; } = new Color(15, 56, 43, 230);
        public Color BottomFillColor { get; set; } = new Color(10, 34, 27, 220);
        public Color AccentColor { get; set; } = Color.Transparent;
        public Color AccentHighlightColor { get; set; } = Color.Transparent;
        public bool ShowAccentBar { get; set; }
    }

    public sealed class ToastNotificationActivatedEventArgs : EventArgs
    {
        public ToastNotificationActivatedEventArgs(ToastNotification notification)
        {
            Notification = notification;
        }

        /// <summary>
        /// Notification that was activated (clicked/tapped).
        /// </summary>
        public ToastNotification Notification { get; }
    }

    /// <summary>
    /// Shared queue/update/draw manager for lightweight toast notifications.
    /// Supports category styling, optional subtitles, and async icon-byte promotion.
    /// </summary>
    public sealed class ToastNotificationManager : IDisposable
    {
        private sealed class ActiveToast
        {
            public ToastNotification Notification;
            public float Time;
        }

        private const float SlideInSeconds = 0.22f;
        private const float HoldSeconds = 3.00f;
        private const float FadeOutSeconds = 0.40f;
        private const float ToastDuration = SlideInSeconds + HoldSeconds + FadeOutSeconds;
        private const int DefaultToastWidth = 330;
        private const int DefaultToastHeight = 82;
        private const int ToastGap = 10;

        private readonly ScreenManager screenManager;
        private readonly Texture2D pixel;
        private readonly List<Texture2D> runtimeIconTextures = new List<Texture2D>();
        private readonly Queue<ToastNotification> pending = new Queue<ToastNotification>();
        private readonly List<ActiveToast> active = new List<ActiveToast>();
        private readonly Dictionary<string, ToastVisualStyle> stylesByCategory = new Dictionary<string, ToastVisualStyle>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> recentDeduplicationKeys = new Dictionary<string, float>(StringComparer.Ordinal);
        private float managerClockSeconds;

        /// <summary>
        /// Raised when the user clicks or taps an active toast.
        /// </summary>
        public event EventHandler<ToastNotificationActivatedEventArgs> ToastActivated;

        /// <summary>
        /// Vertical offset in pixels from safe-area top where the first toast is rendered.
        /// </summary>
        public int ToastTopOffset { get; set; } = 140;

        /// <summary>
        /// Toast width in pixels. Defaults to legacy width.
        /// </summary>
        public int ToastWidth { get; set; } = DefaultToastWidth;

        /// <summary>
        /// Toast height in pixels. Defaults to legacy height.
        /// </summary>
        public int ToastHeight { get; set; } = DefaultToastHeight;

        /// <summary>
        /// Enables suppression of duplicate keys seen within <see cref="KeyDeduplicationWindowSeconds"/>.
        /// Disabled by default to preserve existing behavior.
        /// </summary>
        public bool EnableKeyDeduplication { get; set; }

        /// <summary>
        /// Window in seconds for key deduplication when enabled.
        /// </summary>
        public float KeyDeduplicationWindowSeconds { get; set; } = 1.0f;

        public ToastNotificationManager(GraphicsDevice graphicsDevice, ScreenManager screenManager)
        {
            this.screenManager = screenManager ?? throw new ArgumentNullException(nameof(screenManager));
            pixel = new Texture2D(graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice)), 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Queues a notification for display.
        /// </summary>
        public void Enqueue(ToastNotification notification)
        {
            if (!TryPrepareNotification(notification))
                return;

            if (ShouldSuppressByDeduplication(notification))
                return;

            pending.Enqueue(notification);
        }

        /// <summary>
        /// Shows a notification immediately, optionally clearing pending queued notifications first.
        /// This method is additive and does not change existing <see cref="Enqueue"/> behavior.
        /// </summary>
        public void EnqueueImmediate(ToastNotification notification, bool clearPending = false)
        {
            if (!TryPrepareNotification(notification))
                return;

            if (ShouldSuppressByDeduplication(notification))
                return;

            if (clearPending)
                pending.Clear();

            active.Insert(0, new ActiveToast
            {
                Notification = notification,
                Time = 0f,
            });

            while (active.Count > 2)
                active.RemoveAt(active.Count - 1);

            PromotePendingIcon(notification);
        }

        /// <summary>
        /// Clears active and/or pending notifications.
        /// </summary>
        public void ClearAll(bool includeActive = true, bool includePending = true)
        {
            if (includeActive)
                active.Clear();

            if (includePending)
                pending.Clear();

            if (includeActive || includePending)
                recentDeduplicationKeys.Clear();
        }

        /// <summary>
        /// Sets or replaces style settings for a category.
        /// </summary>
        public void SetStyle(string category, ToastVisualStyle style)
        {
            if (string.IsNullOrWhiteSpace(category) || style == null)
                return;

            stylesByCategory[category] = style;
        }

        /// <summary>
        /// Advances animations, promotes pending icon bytes to textures, and moves queued toasts into active slots.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (gameTime == null)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            managerClockSeconds += Math.Max(0f, dt);

            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].Time += dt;
                if (active[i].Time >= ToastDuration)
                    active.RemoveAt(i);
            }

            while (active.Count < 2 && pending.Count > 0)
            {
                active.Add(new ActiveToast
                {
                    Notification = pending.Dequeue(),
                    Time = 0f,
                });
            }

            PromotePendingIconsToTextures();
            PruneDeduplicationHistory();
        }

        /// <summary>
        /// Handles pointer activation for desktop input.
        /// </summary>
        public bool HandleMouseRelease(Point location)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (!GetToastBounds(i, active[i]).Contains(location))
                    continue;

                ToastActivated?.Invoke(this, new ToastNotificationActivatedEventArgs(active[i].Notification));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles tap activation for touch input.
        /// </summary>
        public bool HandleTap(Vector2 location)
        {
            return HandleMouseRelease(new Point((int)location.X, (int)location.Y));
        }

        /// <summary>
        /// Draws active toasts.
        /// </summary>
        public void Draw()
        {
            if (active.Count == 0)
                return;

            SpriteBatch sb = screenManager.SpriteBatch;
            SpriteFont titleFont = screenManager.BoldFont;
            SpriteFont subtitleFont = screenManager.RegularFont;
            if (sb == null || titleFont == null || subtitleFont == null)
                return;

            Rectangle safe = screenManager.SafeArea;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            for (int i = 0; i < active.Count; i++)
                DrawToast(sb, titleFont, subtitleFont, safe, i, active[i]);
            sb.End();
        }

        private void DrawToast(SpriteBatch sb, SpriteFont titleFont, SpriteFont subtitleFont, Rectangle safe, int stackIndex, ActiveToast toast)
        {
            float t = toast.Time;
            float alpha;
            float slideT;

            if (t < SlideInSeconds)
            {
                float p = MathHelper.Clamp(t / SlideInSeconds, 0f, 1f);
                alpha = p;
                slideT = 1f - p;
            }
            else if (t < SlideInSeconds + HoldSeconds)
            {
                alpha = 1f;
                slideT = 0f;
            }
            else
            {
                float p = MathHelper.Clamp((t - SlideInSeconds - HoldSeconds) / FadeOutSeconds, 0f, 1f);
                alpha = 1f - p;
                slideT = p * 0.15f;
            }

            int toastWidth = Math.Max(220, ToastWidth);
            int toastHeight = Math.Max(64, ToastHeight);
            int baseX = safe.Right - toastWidth - 16;
            int baseY = safe.Top + ToastTopOffset + stackIndex * (toastHeight + ToastGap);
            int slideX = (int)(26f * slideT);
            Rectangle rect = new Rectangle(baseX + slideX, baseY, toastWidth, toastHeight);

            ToastVisualStyle style = ResolveStyle(toast.Notification?.Category);
            Color frame = style.FrameColor * alpha;
            Color topFill = style.TopFillColor * alpha;
            Color bottomFill = style.BottomFillColor * alpha;

            sb.Draw(pixel, rect, bottomFill);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 2), topFill);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), frame);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), frame);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), frame);
            sb.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), frame);

            bool hasIcon = toast.Notification?.IconTexture != null;
            const int iconSize = 64;
            int iconX = rect.X + 26;
            int iconY = rect.Y + (rect.Height - iconSize) / 2;

            if (style.ShowAccentBar)
            {
                sb.Draw(pixel, new Rectangle(rect.X + 14, iconY, 6, iconSize), style.AccentColor * alpha);
                sb.Draw(pixel, new Rectangle(rect.X + 14, iconY, 6, 18), style.AccentHighlightColor * alpha);
            }

            if (hasIcon)
                sb.Draw(toast.Notification.IconTexture, new Rectangle(iconX, iconY, iconSize, iconSize), Color.White * alpha);

            float textLeft = hasIcon ? iconX + iconSize + 16 : rect.X + 28;
            float maxTextWidth = rect.Right - textLeft - 10;

            string fittedHeader = FitTextToWidth(subtitleFont, toast.Notification?.Header, maxTextWidth, 0.65f);
            string[] titleLines = WrapTextToWidth(titleFont, toast.Notification?.Title, maxTextWidth, 2, 0.65f);
            string fittedSubtitle = FitTextToWidth(subtitleFont, toast.Notification?.Subtitle, maxTextWidth, 0.60f);

            float headerScale = ComputeScale(subtitleFont, fittedHeader, maxTextWidth, 0.65f);
            float titleScale = ComputeScale(titleFont, string.Join(" ", titleLines), maxTextWidth, 0.64f);
            float subtitleScale = ComputeScale(subtitleFont, fittedSubtitle, maxTextWidth, 0.60f);

            bool hasSubtitle = !string.IsNullOrWhiteSpace(fittedSubtitle);
            bool hasHeader = !string.IsNullOrWhiteSpace(fittedHeader);
            float headerY = hasSubtitle ? rect.Y + 7 : rect.Y + 12;
            float titleY = (!hasHeader && !hasSubtitle) ? rect.Y + 12 : (hasSubtitle ? rect.Y + 24 : rect.Y + 34);
            float titleLineHeight = titleFont.LineSpacing * titleScale;

            if (hasHeader)
            {
                sb.DrawString(subtitleFont, fittedHeader,
                    new Vector2(textLeft, headerY),
                    new Color(211, 202, 164) * alpha,
                    0f, Vector2.Zero, headerScale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < titleLines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(titleLines[i]))
                    continue;

                sb.DrawString(titleFont, titleLines[i],
                    new Vector2(textLeft, titleY + (i * titleLineHeight)),
                    Color.White * alpha,
                    0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
            }

            if (hasSubtitle)
            {
                sb.DrawString(subtitleFont, fittedSubtitle,
                    new Vector2(textLeft, rect.Y + 50),
                    new Color(225, 205, 168) * alpha,
                    0f, Vector2.Zero, subtitleScale, SpriteEffects.None, 0f);
            }
        }

        private ToastVisualStyle ResolveStyle(string category)
        {
            if (!string.IsNullOrWhiteSpace(category) && stylesByCategory.TryGetValue(category, out var style))
                return style;

            return new ToastVisualStyle();
        }

        private static float ComputeScale(SpriteFont font, string text, float maxWidth, float minScale)
        {
            if (font == null || string.IsNullOrEmpty(text) || maxWidth <= 0f)
                return 1f;

            float width = font.MeasureString(text).X;
            if (width <= 0f)
                return 1f;

            float scale = maxWidth / width;
            if (scale > 1f)
                return 1f;

            return MathHelper.Clamp(scale, minScale, 1f);
        }

        private static string FitTextToWidth(SpriteFont font, string text, float maxWidth, float minScale)
        {
            if (font == null || string.IsNullOrWhiteSpace(text) || maxWidth <= 0f)
                return string.Empty;

            string candidate = text.Trim();
            if (font.MeasureString(candidate).X * minScale <= maxWidth)
                return candidate;

            const string ellipsis = "...";
            for (int len = candidate.Length - 1; len > 0; len--)
            {
                string trimmed = candidate.Substring(0, len).TrimEnd() + ellipsis;
                if (font.MeasureString(trimmed).X * minScale <= maxWidth)
                    return trimmed;
            }

            return ellipsis;
        }

        private static string[] WrapTextToWidth(SpriteFont font, string text, float maxWidth, int maxLines, float minScale)
        {
            if (font == null || string.IsNullOrWhiteSpace(text) || maxWidth <= 0f || maxLines <= 0)
                return Array.Empty<string>();

            string candidate = text.Trim();
            if (font.MeasureString(candidate).X * minScale <= maxWidth)
                return new[] { candidate };

            string[] words = candidate.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return Array.Empty<string>();

            if (maxLines == 1)
                return new[] { FitTextToWidth(font, candidate, maxWidth, minScale) };

            var lines = new List<string>();
            string currentLine = words[0];

            for (int i = 1; i < words.Length; i++)
            {
                string word = words[i];
                string trial = currentLine + " " + word;

                if (font.MeasureString(trial).X * minScale <= maxWidth)
                {
                    currentLine = trial;
                    continue;
                }

                lines.Add(currentLine);

                if (lines.Count >= maxLines - 1)
                {
                    string remaining = string.Join(" ", words, i, words.Length - i);
                    lines.Add(FitTextToWidth(font, remaining, maxWidth, minScale));
                    return lines.ToArray();
                }

                if (font.MeasureString(word).X * minScale <= maxWidth)
                {
                    currentLine = word;
                }
                else
                {
                    currentLine = FitTextToWidth(font, word, maxWidth, minScale);
                }
            }

            lines.Add(currentLine);

            if (lines.Count > maxLines)
                lines.RemoveRange(maxLines, lines.Count - maxLines);

            return lines.ToArray();
        }

        private Rectangle GetToastBounds(int stackIndex, ActiveToast toast)
        {
            Rectangle safe = screenManager.SafeArea;
            int toastWidth = Math.Max(220, ToastWidth);
            int toastHeight = Math.Max(64, ToastHeight);
            int baseX = safe.Right - toastWidth - 16;
            int baseY = safe.Top + ToastTopOffset + stackIndex * (toastHeight + ToastGap);
            float t = toast?.Time ?? 0f;

            if (t < SlideInSeconds)
            {
                float p = MathHelper.Clamp(t / SlideInSeconds, 0f, 1f);
                baseX += (int)(26f * (1f - p));
            }
            else if (t >= SlideInSeconds + HoldSeconds)
            {
                float p = MathHelper.Clamp((t - SlideInSeconds - HoldSeconds) / FadeOutSeconds, 0f, 1f);
                baseX += (int)(26f * p * 0.15f);
            }

            return new Rectangle(baseX, baseY, toastWidth, toastHeight);
        }

        private void PromotePendingIconsToTextures()
        {
            PromotePendingIconsInNotifications(pending);

            for (int i = 0; i < active.Count; i++)
            {
                PromotePendingIcon(active[i].Notification);
            }
        }

        private void PromotePendingIconsInNotifications(IEnumerable<ToastNotification> notifications)
        {
            foreach (var notification in notifications)
            {
                PromotePendingIcon(notification);
            }
        }

        private void PromotePendingIcon(ToastNotification notification)
        {
            if (notification == null || notification.IconTexture != null)
                return;

            var bytes = notification.PendingIconBytes;
            if (bytes == null || bytes.Length == 0)
                return;

            notification.PendingIconBytes = null;

            try
            {
                using var ms = new MemoryStream(bytes, writable: false);
                var tex = Texture2D.FromStream(screenManager.Game.GraphicsDevice, ms);
                notification.IconTexture = tex;
                runtimeIconTextures.Add(tex);
            }
            catch
            {
                // Keep fallback icon path on decode failures.
            }
        }

        private bool TryPrepareNotification(ToastNotification notification)
        {
            if (notification == null || string.IsNullOrWhiteSpace(notification.Title))
                return false;

            notification.Category ??= "default";
            notification.Header ??= string.Empty;
            notification.Identifier ??= string.Empty;
            notification.DeduplicationKey ??= string.Empty;
            notification.Title = notification.Title.Trim();
            notification.Subtitle ??= string.Empty;
            return true;
        }

        private bool ShouldSuppressByDeduplication(ToastNotification notification)
        {
            if (!EnableKeyDeduplication)
                return false;

            float windowSeconds = Math.Max(0f, KeyDeduplicationWindowSeconds);
            if (windowSeconds <= 0f)
                return false;

            string key = ResolveDeduplicationKey(notification);
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (recentDeduplicationKeys.TryGetValue(key, out float lastSeenAt) &&
                managerClockSeconds - lastSeenAt <= windowSeconds)
            {
                return true;
            }

            recentDeduplicationKeys[key] = managerClockSeconds;
            return false;
        }

        private static string ResolveDeduplicationKey(ToastNotification notification)
        {
            if (notification == null)
                return string.Empty;

            string explicitKey = (notification.DeduplicationKey ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(explicitKey))
                return explicitKey;

            string identifier = (notification.Identifier ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(identifier))
                return identifier;

            return string.Empty;
        }

        private void PruneDeduplicationHistory()
        {
            if (recentDeduplicationKeys.Count == 0)
                return;

            float windowSeconds = Math.Max(0f, KeyDeduplicationWindowSeconds);
            if (!EnableKeyDeduplication || windowSeconds <= 0f)
            {
                recentDeduplicationKeys.Clear();
                return;
            }

            float staleBefore = managerClockSeconds - (windowSeconds * 4f);
            if (staleBefore <= 0f)
                return;

            List<string> staleKeys = null;
            foreach (KeyValuePair<string, float> pair in recentDeduplicationKeys)
            {
                if (pair.Value >= staleBefore)
                    continue;

                staleKeys ??= new List<string>();
                staleKeys.Add(pair.Key);
            }

            if (staleKeys == null)
                return;

            for (int i = 0; i < staleKeys.Count; i++)
                recentDeduplicationKeys.Remove(staleKeys[i]);
        }

        public void Dispose()
        {
            for (int i = 0; i < runtimeIconTextures.Count; i++)
            {
                runtimeIconTextures[i]?.Dispose();
            }

            runtimeIconTextures.Clear();
            pixel?.Dispose();
        }
    }
}