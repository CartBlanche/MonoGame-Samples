//----------------------------------------------------------------------------- 
// Button.cs
//
// Shared reusable button control for CardsStarterKit games.
// Follows the framework's resolution-independent input model and keeps all
// button logic generic so it can be used by Blackjack, Blank, Gin Rummy,
// and future card games.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace CardsFramework
{
    /// <summary>
    /// Generic gameplay button that works across desktop and mobile platforms.
    /// All hit testing uses the framework's transformed input coordinates so the
    /// button remains resolution-independent.
    /// </summary>
    public class Button : AnimatedGameComponent
    {
        private readonly InputState input;
        private readonly SpriteBatch spriteBatch;
        private readonly Matrix globalTransformation;

        private Texture2D regularTexture;
        private Texture2D pressedTexture;
        private Texture2D hoverTexture;
        private Texture2D blankTexture;

        private readonly string regularTextureName;
        private readonly string pressedTextureName;
        private readonly string hoverTextureName;

        private bool isPressed;
        private bool isHovered;

        protected InputState Input => input;
        protected SpriteBatch SpriteBatch => spriteBatch;
        protected Matrix GlobalTransformation => globalTransformation;
        protected bool IsPressedState => isPressed;
        protected bool IsHoveredState => isHovered;

        public event EventHandler Click;

        public Texture2D RegularTexture => regularTexture;
        public Texture2D PressedTexture => pressedTexture;
        public Texture2D HoverTexture => hoverTexture;

        public Rectangle Bounds { get; set; }
        public SpriteFont Font { get; set; }
        public Color HoverBorderColor { get; set; } = Color.Gold;

        public Button(
            string regularTextureName,
            string pressedTextureName,
            InputState input,
            CardsGame game,
            SpriteBatch spriteBatch,
            Matrix globalTransformation)
            : this(regularTextureName, pressedTextureName, null, input, game, spriteBatch, globalTransformation)
        {
        }

        public Button(
            string regularTextureName,
            string pressedTextureName,
            string hoverTextureName,
            InputState input,
            CardsGame game,
            SpriteBatch spriteBatch,
            Matrix globalTransformation)
            : base(game, null, spriteBatch, globalTransformation)
        {
            this.regularTextureName = regularTextureName;
            this.pressedTextureName = pressedTextureName;
            this.hoverTextureName = hoverTextureName;
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            this.globalTransformation = globalTransformation;
        }

        protected override void LoadContent()
        {
            if (!string.IsNullOrEmpty(regularTextureName))
            {
                regularTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", regularTextureName));
            }

            if (!string.IsNullOrEmpty(pressedTextureName))
            {
                pressedTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", pressedTextureName));
            }

            if (!string.IsNullOrEmpty(hoverTextureName))
            {
                hoverTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", hoverTextureName));
            }
            else
            {
                hoverTexture = regularTexture;
            }

            blankTexture = Game.Content.Load<Texture2D>(Path.Combine("Images", "blank"));
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible || !Enabled)
                return;

            UpdateInteraction();
            base.Update(gameTime);
        }

        protected virtual void UpdateInteraction()
        {
            bool clicked = false;

            if (UIUtility.IsDesktop)
            {
                bool hit = HitTest(input.CurrentCursorLocation);
                isHovered = hit;

                if (hit)
                {
                    PlayerIndex playerIndex;
                    if (input.IsMenuSelect(null, out playerIndex) || input.IsLeftMouseButtonClicked())
                    {
                        clicked = true;
                    }
                }

                isPressed = hit && (
                    input.CurrentMouseState.LeftButton == ButtonState.Pressed ||
                    (input.CurrentGamePadStates.Length > 0 &&
                     input.CurrentGamePadStates[0].IsButtonDown(Buttons.A)));
            }
            else if (UIUtility.IsMobile)
            {
                isHovered = false;

                foreach (var gesture in input.Gestures)
                {
                    if (gesture.GestureType == GestureType.Tap)
                    {
                        Vector2 transformed = input.TransformCursorLocation(gesture.Position);
                        if (HitTest(transformed))
                        {
                            clicked = true;
                            break;
                        }
                    }
                }

                isPressed = false;
                foreach (var touch in input.CurrentTouchState)
                {
                    if ((touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved) &&
                        HitTest(input.TransformCursorLocation(touch.Position)))
                    {
                        isPressed = true;
                        break;
                    }
                }
            }

            if (clicked)
            {
                OnClick();
            }
        }

        protected virtual bool HitTest(Vector2 position)
        {
            var tap = new Rectangle((int)position.X - 1, (int)position.Y - 1, 2, 2);
            return Bounds.Intersects(tap);
        }

        public virtual void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);

            var texture = isPressed ? pressedTexture ?? regularTexture : regularTexture;
            if (texture != null)
            {
                spriteBatch.Draw(texture, Bounds, Color.White);
            }

            if (Font != null && !string.IsNullOrEmpty(Text))
            {
                Vector2 textSize = Font.MeasureString(Text);
                Vector2 textPosition = new Vector2(
                    Bounds.X + (Bounds.Width - textSize.X) / 2f,
                    Bounds.Y + (Bounds.Height - textSize.Y) / 2f);

                if (isPressed)
                {
                    textPosition += new Vector2(0, 2);
                }

                spriteBatch.DrawString(Font, Text, textPosition, TextColor);
            }

            if (isHovered && !isPressed && blankTexture != null)
            {
                int borderThickness = 3;
                spriteBatch.Draw(blankTexture, new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, borderThickness), HoverBorderColor);
                spriteBatch.Draw(blankTexture, new Rectangle(Bounds.X, Bounds.Y + Bounds.Height - borderThickness, Bounds.Width, borderThickness), HoverBorderColor);
                spriteBatch.Draw(blankTexture, new Rectangle(Bounds.X, Bounds.Y, borderThickness, Bounds.Height), HoverBorderColor);
                spriteBatch.Draw(blankTexture, new Rectangle(Bounds.X + Bounds.Width - borderThickness, Bounds.Y, borderThickness, Bounds.Height), HoverBorderColor);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            Click = null;
            base.Dispose(disposing);
        }
    }
}
