//-----------------------------------------------------------------------------
// Button.cs
//
// Blackjack-specific customization layered on top of the reusable framework
// button primitive.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blackjack
{
    public class Button : CardsFramework.Button
    {
        private readonly string iconTextureName;

        public Texture2D IconTexture { get; set; }
        public float IconScale { get; set; } = 1.0f;
        public Vector2 IconOffset { get; set; } = Vector2.Zero;
        public float TextIconSpacing { get; set; } = 10f;
        public Color IconTint { get; set; } = Color.White;
        public Color IconPressedTint { get; set; } = new Color(200, 200, 200);
        public Rectangle? IconSourceRect { get; set; } = null;

        public Button(string regularTexture, string pressedTexture, InputState input,
            CardsGame cardGame, SpriteBatch sharedSpriteBatch, Matrix globalTransformation)
            : this(regularTexture, pressedTexture, null, input, cardGame, sharedSpriteBatch, globalTransformation)
        {
        }

        public Button(string regularTexture, string pressedTexture, string iconTexture, InputState input,
            CardsGame cardGame, SpriteBatch sharedSpriteBatch, Matrix globalTransformation)
            : base(regularTexture, pressedTexture, input, cardGame, sharedSpriteBatch, globalTransformation)
        {
            iconTextureName = iconTexture;
        }

        protected override void LoadContent()
        {
            if (!string.IsNullOrEmpty(iconTextureName))
            {
                IconTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", iconTextureName));
            }

            base.LoadContent();
        }

        public override void OnClick()
        {
            AudioManager.PlaySound("Click");
            base.OnClick();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (IconTexture == null || !Visible || !Enabled)
                return;

            var bounds = Bounds;
            bool hasText = Font != null && !string.IsNullOrEmpty(Text);

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            Vector2 iconSize = IconSourceRect.HasValue
                ? new Vector2(IconSourceRect.Value.Width * IconScale, IconSourceRect.Value.Height * IconScale)
                : new Vector2(IconTexture.Width * IconScale, IconTexture.Height * IconScale);

            float totalContentHeight = iconSize.Y + (hasText ? TextIconSpacing + Font.MeasureString(Text).Y : 0);
            float startY = bounds.Y + (bounds.Height - totalContentHeight) / 2f;
            Vector2 pressedOffset = IsPressedState ? new Vector2(0, 2) : Vector2.Zero;

            Rectangle sourceRect = IconSourceRect ?? new Rectangle(0, 0, IconTexture.Width, IconTexture.Height);
            Vector2 iconPosition = new Vector2(
                bounds.X + (bounds.Width - iconSize.X) / 2f,
                startY
            );
            iconPosition += IconOffset + pressedOffset;

            Color iconColor = IsPressedState ? IconPressedTint : IconTint;

            SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, GlobalTransformation);
            SpriteBatch.Draw(IconTexture, iconPosition, sourceRect, iconColor, 0f, Vector2.Zero, IconScale, SpriteEffects.None, 0f);
            SpriteBatch.End();
        }
    }
}
