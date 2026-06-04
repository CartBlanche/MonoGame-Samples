using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace CardsFramework.Core
{
    /// <summary>
    /// Reusable achievements screen foundation with common input, scrolling,
    /// clipping, and row projection behavior.
    /// </summary>
    public abstract class AchievementsScreenBase : GameScreen
    {
        protected enum UiAction
        {
            Close,
            ScrollUp,
            ScrollDown,
        }

        private sealed class AchievementRowEntry
        {
            public Achievement Achievement;
            public Texture2D Icon;
        }

        private static readonly RasterizerState ScissorRasterizer = new RasterizerState
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None,
        };

        private readonly List<AchievementRowEntry> _rows = new();
        private Texture2D _pixel;
        private Rectangle _closeButtonBounds;
        private Rectangle _listViewport;
        private Rectangle _scrollUpButtonBounds;
        private Rectangle _scrollDownButtonBounds;
        private bool _isCloseHover;
        private bool _isClosePressed;
        private bool _isScrollUpHover;
        private bool _isScrollDownHover;
        private bool _isScrollUpPressed;
        private bool _isScrollDownPressed;
        private bool _showScrollButtons;
        private float _scrollOffset;
        private float _maxScrollOffset;

        protected virtual int LeftMargin => 20;
        protected virtual int TopMargin => 18;
        protected virtual int HeaderHeight => 92;
        protected virtual int RowHeight => 86;
        protected virtual int RowGap => 8;
        protected virtual int CloseButtonWidth => 54;
        protected virtual int CloseButtonHeight => 44;
        protected virtual int ListBottomMargin => 12;
        protected virtual int ListTopPadding => 8;
        protected virtual int ScrollLaneWidth => 34;
        protected virtual int ScrollButtonHeight => 42;
        protected virtual int ScrollButtonMargin => 8;
        protected virtual float MouseWheelStep => 42f;

        protected Texture2D PixelTexture => _pixel;
        protected Rectangle ListViewport => _listViewport;
        protected Rectangle CloseButtonBounds => _closeButtonBounds;
        protected Rectangle ScrollUpButtonBounds => _scrollUpButtonBounds;
        protected Rectangle ScrollDownButtonBounds => _scrollDownButtonBounds;
        protected bool IsCloseHover => _isCloseHover;
        protected bool IsClosePressed => _isClosePressed;
        protected bool IsScrollUpHover => _isScrollUpHover;
        protected bool IsScrollDownHover => _isScrollDownHover;
        protected bool IsScrollUpPressed => _isScrollUpPressed;
        protected bool IsScrollDownPressed => _isScrollDownPressed;

        protected AchievementsScreenBase()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.35);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            EnabledGestures = GestureType.FreeDrag | GestureType.Tap;
        }

        public override void LoadContent()
        {
            _pixel = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            BuildRows();
            RebuildLayout();
            OnLoadContentCore();

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            OnUnloadContentCore();
            _pixel?.Dispose();
            _pixel = null;
            base.UnloadContent();
        }

        public override void HandleInput(InputState inputState)
        {
            if (inputState == null)
                return;

            PlayerIndex player;
            if (inputState.IsNewButtonPress(Buttons.Back, ControllingPlayer, out player) ||
                inputState.IsNewKeyPress(Keys.Escape, ControllingPlayer, out player))
            {
                TriggerClose();
                return;
            }

            if (inputState.IsNewKeyPress(Keys.Up, ControllingPlayer, out player))
                ScrollBy(-RowHeight * 0.8f);
            if (inputState.IsNewKeyPress(Keys.Down, ControllingPlayer, out player))
                ScrollBy(RowHeight * 0.8f);
            if (inputState.IsNewKeyPress(Keys.PageUp, ControllingPlayer, out player))
                ScrollBy(-_listViewport.Height * 0.85f);
            if (inputState.IsNewKeyPress(Keys.PageDown, ControllingPlayer, out player))
                ScrollBy(_listViewport.Height * 0.85f);
            if (inputState.IsNewKeyPress(Keys.Home, ControllingPlayer, out player))
                _scrollOffset = 0f;
            if (inputState.IsNewKeyPress(Keys.End, ControllingPlayer, out player))
                _scrollOffset = _maxScrollOffset;

            if (!UIUtility.IsDesktop)
            {
                _isCloseHover = false;
                _isClosePressed = false;
                _isScrollUpHover = false;
                _isScrollDownHover = false;
                _isScrollUpPressed = false;
                _isScrollDownPressed = false;

                foreach (var gesture in inputState.Gestures)
                {
                    Vector2 point = inputState.TransformCursorLocation(gesture.Position);

                    if (gesture.GestureType == GestureType.FreeDrag)
                    {
                        if (_listViewport.Contains((int)point.X, (int)point.Y))
                            ScrollBy(-gesture.Delta.Y);
                        continue;
                    }

                    if (gesture.GestureType != GestureType.Tap)
                        continue;

                    if (_closeButtonBounds.Contains((int)point.X, (int)point.Y))
                    {
                        TriggerClose();
                        return;
                    }

                    if (_showScrollButtons && _scrollUpButtonBounds.Contains((int)point.X, (int)point.Y))
                    {
                        TriggerScrollUp();
                        return;
                    }

                    if (_showScrollButtons && _scrollDownButtonBounds.Contains((int)point.X, (int)point.Y))
                    {
                        TriggerScrollDown();
                        return;
                    }
                }

                return;
            }

            Point mousePos = new Point((int)inputState.CurrentCursorLocation.X, (int)inputState.CurrentCursorLocation.Y);
            _isCloseHover = _closeButtonBounds.Contains(mousePos);
            _isScrollUpHover = _showScrollButtons && _scrollUpButtonBounds.Contains(mousePos);
            _isScrollDownHover = _showScrollButtons && _scrollDownButtonBounds.Contains(mousePos);

            bool curDown = inputState.CurrentMouseState.LeftButton == ButtonState.Pressed;
            bool wasDown = inputState.LastMouseState.LeftButton == ButtonState.Pressed;
            _isClosePressed = _isCloseHover && curDown;
            _isScrollUpPressed = _isScrollUpHover && curDown;
            _isScrollDownPressed = _isScrollDownHover && curDown;

            int wheelDelta = inputState.CurrentMouseState.ScrollWheelValue - inputState.LastMouseState.ScrollWheelValue;
            if (wheelDelta != 0 && _listViewport.Contains(mousePos))
                ScrollBy(-(wheelDelta / 120f) * MouseWheelStep);

            if (!curDown && wasDown)
            {
                if (_isCloseHover)
                {
                    TriggerClose();
                    return;
                }

                if (_isScrollUpHover)
                {
                    TriggerScrollUp();
                    return;
                }

                if (_isScrollDownHover)
                {
                    TriggerScrollDown();
                    return;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (_pixel == null)
                return;

            RebuildLayout();

            Rectangle safe = ScreenManager.SafeArea;
            SpriteBatch sb = ScreenManager.SpriteBatch;
            Matrix xform = ScreenManager.GlobalTransformation;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, xform);
            DrawBackdrop(sb, safe, TransitionAlpha);
            DrawTitle(sb, safe, TransitionAlpha);
            DrawListBackground(sb, _listViewport, TransitionAlpha);
            sb.End();

            Rectangle oldScissor = ScreenManager.GraphicsDevice.ScissorRectangle;
            ScreenManager.GraphicsDevice.ScissorRectangle = _listViewport;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, ScissorRasterizer, null, xform);

            float rowY = _listViewport.Y + ListTopPadding - _scrollOffset;
            float rowWidth = _listViewport.Width;

            for (int i = 0; i < _rows.Count; i++)
            {
                AchievementRowEntry row = _rows[i];
                Rectangle rowRect = new Rectangle(_listViewport.Left, (int)rowY, (int)rowWidth, RowHeight);

                if (rowRect.Bottom <= _listViewport.Top || rowRect.Top >= _listViewport.Bottom)
                {
                    rowY += RowHeight + RowGap;
                    continue;
                }

                DrawRow(sb, row.Achievement, row.Icon, rowRect, TransitionAlpha);
                rowY += RowHeight + RowGap;
            }

            sb.End();
            ScreenManager.GraphicsDevice.ScissorRectangle = oldScissor;

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, xform);
            if (_showScrollButtons)
            {
                DrawScrollControls(sb, TransitionAlpha,
                    _scrollOffset > 0f,
                    _scrollOffset < _maxScrollOffset - 0.5f);
            }

            DrawCloseButton(sb, gameTime, TransitionAlpha);
            sb.End();
        }

        protected virtual void OnLoadContentCore()
        {
        }

        protected virtual void OnUnloadContentCore()
        {
        }

        protected virtual string GetTitleText() => "Achievements";

        protected virtual string GetHiddenLabelText() => "Hidden";

        protected virtual Texture2D ResolveIconTexture(Achievement achievement) => null;

        protected virtual void OnUiAction(UiAction action)
        {
        }

        protected virtual void DrawBackdrop(SpriteBatch sb, Rectangle safe, float alpha)
        {
            sb.Draw(_pixel, safe, Color.Black * 0.62f * alpha);
        }

        protected virtual void DrawTitle(SpriteBatch sb, Rectangle safe, float alpha)
        {
            string title = GetTitleText();
            Vector2 titleSize = ScreenManager.Font.MeasureString(title);
            sb.DrawString(ScreenManager.Font, title,
                new Vector2(safe.Center.X - titleSize.X / 2f, safe.Top + TopMargin + 4),
                Color.White * alpha);
        }

        protected virtual void DrawListBackground(SpriteBatch sb, Rectangle viewport, float alpha)
        {
            sb.Draw(_pixel, viewport, new Color(20, 20, 20, 132) * alpha);
        }

        protected virtual void DrawRow(SpriteBatch sb, Achievement achievement, Texture2D icon, Rectangle rowRect, float alpha)
        {
            sb.Draw(_pixel, rowRect, new Color(36, 36, 36, 220) * alpha);

            if (icon != null)
                sb.Draw(icon, new Rectangle(rowRect.X + 12, rowRect.Y + 12, 42, 42), Color.White * alpha);

            GetRowDisplayContent(achievement, out string title, out string description, out _, out _);

            float textLeft = rowRect.X + 64;
            float maxTextWidth = rowRect.Right - textLeft - 12;
            float titleScale = FitTextToWidth(ScreenManager.BoldFont, title, maxTextWidth, 0.7f);
            float descScale = FitTextToWidth(ScreenManager.RegularFont, description, maxTextWidth, 0.6f);

            sb.DrawString(ScreenManager.BoldFont, title,
                new Vector2(textLeft, rowRect.Y + 12),
                Color.White * alpha,
                0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            if (!string.IsNullOrWhiteSpace(description))
            {
                sb.DrawString(ScreenManager.RegularFont, description,
                    new Vector2(textLeft, rowRect.Y + 38),
                    Color.LightGray * alpha,
                    0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
            }
        }

        protected void DrawDetailedRowText(
            SpriteBatch sb,
            Achievement achievement,
            Rectangle rowRect,
            float alpha,
            float textLeft,
            float maxTextWidth,
            Color titleColor,
            Color descriptionColor,
            Color hintColor,
            Color progressColor,
            float titleScaleMin = 0.78f,
            float descriptionScaleMin = 0.62f,
            float hintScaleMin = 0.58f)
        {
            GetRowDisplayContent(achievement, out string title, out string description, out string hint, out string progress);

            float titleScale = FitTextToWidth(ScreenManager.BoldFont, title, maxTextWidth, titleScaleMin);
            float descScale = FitTextToWidth(ScreenManager.RegularFont, description, maxTextWidth, descriptionScaleMin);
            float hintScale = FitTextToWidth(ScreenManager.RegularFont, hint, maxTextWidth, hintScaleMin);

            sb.DrawString(ScreenManager.BoldFont, title,
                new Vector2(textLeft, rowRect.Y + 14),
                titleColor * alpha,
                0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            if (!string.IsNullOrWhiteSpace(description))
            {
                sb.DrawString(ScreenManager.RegularFont, description,
                    new Vector2(textLeft, rowRect.Y + 40),
                    descriptionColor * alpha,
                    0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
            }

            if (!string.IsNullOrWhiteSpace(hint))
            {
                sb.DrawString(ScreenManager.RegularFont, hint,
                    new Vector2(textLeft, rowRect.Y + 60),
                    hintColor * alpha,
                    0f, Vector2.Zero, hintScale, SpriteEffects.None, 0f);
            }

            Vector2 progressSize = ScreenManager.BoldFont.MeasureString(progress);
            sb.DrawString(ScreenManager.BoldFont, progress,
                new Vector2(rowRect.Right - progressSize.X - 16, rowRect.Y + 16),
                progressColor * alpha);
        }

        protected void GetRowDisplayContent(
            Achievement achievement,
            out string title,
            out string description,
            out string hint,
            out string progress)
        {
            GetDisplayRowText(achievement, out title, out description, out hint);
            progress = GetProgressText(achievement);
        }

        protected virtual string GetProgressText(Achievement achievement)
        {
            if (achievement == null)
                return "0%";

            return achievement.IsEarned ? "100%" : $"{achievement.PercentComplete:0}%";
        }

        protected virtual void DrawScrollControls(SpriteBatch sb, float alpha, bool canScrollUp, bool canScrollDown)
        {
            DrawArrowButton(sb, _scrollUpButtonBounds, "^", _isScrollUpHover, _isScrollUpPressed, canScrollUp, alpha);
            DrawArrowButton(sb, _scrollDownButtonBounds, "v", _isScrollDownHover, _isScrollDownPressed, canScrollDown, alpha);
        }

        protected virtual void DrawCloseButton(SpriteBatch sb, GameTime gameTime, float alpha)
        {
            DrawArrowButton(sb, _closeButtonBounds, "X", _isCloseHover, _isClosePressed, true, alpha);
        }

        protected void GetDisplayRowText(Achievement achievement, out string title, out string description, out string hint)
        {
            if (achievement == null)
            {
                title = string.Empty;
                description = string.Empty;
                hint = string.Empty;
                return;
            }

            title = achievement.DisplayName;
            description = achievement.Description;
            hint = achievement.HowToEarn;

            if (achievement.IsHidden && !achievement.IsEarned)
            {
                title = GetHiddenLabelText();
                description = string.Empty;
                hint = string.Empty;
            }
        }

        protected static float FitTextToWidth(SpriteFont font, string text, float maxWidth, float minScale)
        {
            if (font == null || string.IsNullOrWhiteSpace(text) || maxWidth <= 0f)
                return 1f;

            float width = font.MeasureString(text).X;
            if (width <= 0f)
                return 1f;

            float scale = maxWidth / width;
            if (scale > 1f)
                return 1f;

            return MathHelper.Clamp(scale, minScale, 1f);
        }

        private void BuildRows()
        {
            _rows.Clear();

            foreach (Achievement achievement in AchievementRowsProjector.BuildRows(SignedInGamer.Current))
            {
                _rows.Add(new AchievementRowEntry
                {
                    Achievement = achievement,
                    Icon = ResolveIconTexture(achievement),
                });
            }

            UpdateScrollRange();
        }

        private void DrawArrowButton(
            SpriteBatch sb,
            Rectangle bounds,
            string label,
            bool hover,
            bool pressed,
            bool enabled,
            float alpha)
        {
            Color baseFill = enabled ? new Color(84, 84, 84) : new Color(52, 52, 52);
            Color fill = pressed ? baseFill * 0.75f : (hover ? baseFill * 1.08f : baseFill);
            Color frame = enabled ? new Color(196, 196, 196) : new Color(120, 120, 120);

            sb.Draw(_pixel, bounds, fill * alpha);
            sb.Draw(_pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), frame * alpha);
            sb.Draw(_pixel, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), frame * alpha);
            sb.Draw(_pixel, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), frame * alpha);
            sb.Draw(_pixel, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), frame * alpha);

            Vector2 textSize = ScreenManager.BoldFont.MeasureString(label);
            sb.DrawString(ScreenManager.BoldFont, label,
                new Vector2(bounds.Center.X - textSize.X * 0.5f, bounds.Center.Y - textSize.Y * 0.5f - 1f),
                enabled ? Color.White * alpha : new Color(174, 174, 174) * alpha);
        }

        private void TriggerClose()
        {
            OnUiAction(UiAction.Close);
            ExitScreen();
        }

        private void TriggerScrollUp()
        {
            OnUiAction(UiAction.ScrollUp);
            ScrollBy(-RowHeight * 0.8f);
        }

        private void TriggerScrollDown()
        {
            OnUiAction(UiAction.ScrollDown);
            ScrollBy(RowHeight * 0.8f);
        }

        private void RebuildLayout()
        {
            Rectangle safe = ScreenManager.SafeArea;

            _closeButtonBounds = new Rectangle(safe.Left + LeftMargin, safe.Top + TopMargin, CloseButtonWidth, CloseButtonHeight);

            int viewportX = safe.Left + LeftMargin;
            int viewportY = safe.Top + HeaderHeight;
            int viewportWidth = safe.Width - LeftMargin * 2 - ScrollLaneWidth - ScrollButtonMargin;
            int viewportHeight = safe.Height - HeaderHeight - ListBottomMargin;
            _listViewport = new Rectangle(viewportX, viewportY, Math.Max(120, viewportWidth), Math.Max(120, viewportHeight));

            int laneX = _listViewport.Right + ScrollButtonMargin;
            _scrollUpButtonBounds = new Rectangle(laneX, _listViewport.Y + ScrollButtonMargin, ScrollLaneWidth, ScrollButtonHeight);
            _scrollDownButtonBounds = new Rectangle(laneX, _listViewport.Bottom - ScrollButtonMargin - ScrollButtonHeight, ScrollLaneWidth, ScrollButtonHeight);

            UpdateScrollRange();
        }

        private void UpdateScrollRange()
        {
            float contentHeight = _rows.Count == 0
                ? 0f
                : (_rows.Count * (RowHeight + RowGap)) - RowGap + (ListTopPadding * 2);

            _maxScrollOffset = Math.Max(0f, contentHeight - _listViewport.Height);
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0f, _maxScrollOffset);
            _showScrollButtons = _maxScrollOffset > 0.5f;
        }

        private void ScrollBy(float delta)
        {
            if (_maxScrollOffset <= 0f)
                return;

            _scrollOffset = MathHelper.Clamp(_scrollOffset + delta, 0f, _maxScrollOffset);
        }
    }
}