using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    internal static class AchievementLocalization
    {
        internal static string GetAchievementsText() => GetString("Achievements", "Achievements");

        internal static string GetHiddenText() => GetString("Hidden", "Hidden");

        internal static string GetAchievementUnlockedHeaderText() => GetString("AchievementUnlockedHeader", "ACHIEVEMENT UNLOCKED");

        private static string GetString(string key, string fallback)
        {
            string text = Resources.ResourceManager.GetString(key, Resources.Culture);
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }
    }

    internal sealed class BlackjackAchievementsScreen : AchievementsScreenBase
    {
        private Texture2D _fallbackIcon;

        protected override void OnLoadContentCore()
        {
            _fallbackIcon = Load<Texture2D>("Images/UI/ring");
        }

        protected override string GetTitleText() => AchievementLocalization.GetAchievementsText();

        protected override string GetHiddenLabelText() => AchievementLocalization.GetHiddenText();

        protected override void OnUiAction(UiAction action)
        {
            if (action == UiAction.Close)
                AudioManager.PlaySound("Click");
        }

        protected override Texture2D ResolveIconTexture(Achievement achievement) => _fallbackIcon;

        protected override void DrawBackdrop(SpriteBatch sb, Rectangle safe, float alpha)
        {
            sb.Draw(PixelTexture, safe, new Color(6, 31, 24) * 0.92f * alpha);
        }

        protected override void DrawListBackground(SpriteBatch sb, Rectangle viewport, float alpha)
        {
            sb.Draw(PixelTexture, viewport, new Color(10, 42, 32, 210) * alpha);
            sb.Draw(PixelTexture, new Rectangle(viewport.X, viewport.Y, viewport.Width, 2), new Color(172, 138, 68) * alpha);
            sb.Draw(PixelTexture, new Rectangle(viewport.X, viewport.Bottom - 2, viewport.Width, 2), new Color(172, 138, 68) * alpha);
        }

        protected override void DrawRow(SpriteBatch sb, Achievement achievement, Texture2D icon, Rectangle rowRect, float alpha)
        {
            bool earned = achievement?.IsEarned == true;
            Color frame = earned ? new Color(216, 180, 86) : new Color(92, 110, 94);
            Color fill = earned ? new Color(21, 60, 46, 236) : new Color(16, 36, 29, 228);

            sb.Draw(PixelTexture, rowRect, fill * alpha);
            sb.Draw(PixelTexture, new Rectangle(rowRect.X, rowRect.Y, rowRect.Width, 2), frame * alpha);
            sb.Draw(PixelTexture, new Rectangle(rowRect.X, rowRect.Bottom - 2, rowRect.Width, 2), frame * alpha);
            sb.Draw(PixelTexture, new Rectangle(rowRect.X, rowRect.Y, 2, rowRect.Height), frame * alpha);
            sb.Draw(PixelTexture, new Rectangle(rowRect.Right - 2, rowRect.Y, 2, rowRect.Height), frame * alpha);

            Texture2D rowIcon = icon ?? _fallbackIcon;
            if (rowIcon != null)
            {
                sb.Draw(rowIcon, new Rectangle(rowRect.X + 18, rowRect.Y + 18, 48, 48),
                    (earned ? Color.White : new Color(205, 214, 208)) * alpha);
            }

            float textLeft = rowRect.X + 18 + 48 + 14;
            float maxTextWidth = rowRect.Right - textLeft - 18;
            DrawDetailedRowText(
                sb,
                achievement,
                rowRect,
                alpha,
                textLeft,
                maxTextWidth,
                earned ? Color.White : new Color(222, 230, 224),
                new Color(201, 214, 206),
                new Color(151, 178, 164),
                earned ? new Color(243, 214, 127) : new Color(171, 188, 178));
        }
    }
}