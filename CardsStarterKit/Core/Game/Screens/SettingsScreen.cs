//-----------------------------------------------------------------------------
// SettingsScreen.cs
//
// Settings screen with custom layout for adjusting game options
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using System.IO;
using CardsFramework;

namespace Blackjack
{
    class SettingsScreen : GameScreen
    {
        private Texture2D background;
        private Texture2D cardBackTexture;
        private Texture2D buttonRegularTexture;
        private Texture2D buttonPressedTexture;
        // Fonts are accessed directly from ScreenManager to ensure we always use current font
        private GameSettings settings;
        private Rectangle safeArea;

        // Card preview
        private Vector2 cardPreviewPosition;
        private Vector2 cardSize;

        // Setting item areas
        private List<SettingItem> settingItems = new List<SettingItem>();
        private int selectedIndex = -1;
        private int hoveredIndex = -1;
        private int pressedButtonIndex = -1; // Which button is being pressed (-1 for none, 0 for left, 1 for right)

        // Pagination
        private int currentPage = 0;
        private const int TotalPages = 2;
        private Rectangle nextButtonBounds;
        private Rectangle prevButtonBounds;
        private bool isNextButtonPressed = false;
        private bool isPrevButtonPressed = false;

        private Rectangle backButtonBounds;
        private bool isBackButtonPressed = false;

        private bool isMouseDown = false;

        private InputHelper inputHelper;
        private float itemSpacing;
        private float groupSpacing;
        private float leftMargin;
        private float buttonSize;
        private float rightButtonMargin;
        private float buttonSpacing;

        public SettingsScreen()
        {
            // Settings screen needs Tap gestures for mobile
            EnabledGestures = GestureType.Tap;
            
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            settings = GameSettings.Instance;
        }

        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            background = content.Load<Texture2D>("Images/UI/table");

            // Fonts are accessed directly from ScreenManager properties (no caching)

            buttonRegularTexture = content.Load<Texture2D>("Images/ButtonRegular");
            buttonPressedTexture = content.Load<Texture2D>("Images/ButtonPressed");

            // Load current theme card back
            string themeCardBack = settings.Theme == "Red" ? "CardBack_Red" : "CardBack_Blue";
            cardBackTexture = content.Load<Texture2D>($"Images/Cards/{themeCardBack}");

            // Initialize input helper
            inputHelper = new InputHelper(ScreenManager);

            safeArea = ScreenManager.SafeArea;

            // Calculate proportional spacing and sizes based on screen height
            float heightScale = safeArea.Height / 720f; // Scale based on 720p baseline
            itemSpacing = 45f * heightScale;
            groupSpacing = 60f * heightScale;
            leftMargin = 80f * heightScale;
            buttonSize = 32f * heightScale;
            rightButtonMargin = 10f * heightScale;
            buttonSpacing = 168f * heightScale; // Space between left and right buttons

            // Calculate card preview position and size (proportional to screen)
            cardSize = new Vector2(80 * heightScale, 120 * heightScale);
            cardPreviewPosition = new Vector2(
                safeArea.Center.X - cardSize.X / 2,
                safeArea.Top + 50 * heightScale
            );

            BuildSettingItems();

            base.LoadContent();
        }

        /// <summary>
        /// Reloads fonts when language changes between CJK and non-CJK languages.
        /// </summary>
        public void ReloadFonts(bool useCJKFont)
        {
            // No need to cache fonts - they're accessed directly from ScreenManager
            // Don't rebuild items here - will be done after language is set to avoid
            // measuring text in new language with old fonts
        }

        private void BuildSettingItems()
        {
            settingItems.Clear();
            float yPos = cardPreviewPosition.Y + cardSize.Y + 20; // Start below card preview

            if (currentPage == 0)
            {
                // Page 1: DISPLAY and AUDIO
                AddHeader(Resources.SettingsDisplay, ref yPos);
                AddCycleSetting(Resources.SettingsLanguage, GetLanguageDisplay,
                    CycleToPreviousLanguage,
                    CycleToNextLanguage,
                    null, ref yPos);
                AddCycleSetting(Resources.SettingsCardBackTheme, () => settings.Theme,
                    () => settings.Theme = settings.Theme == "Red" ? "Blue" : "Red",
                    () => settings.Theme = settings.Theme == "Red" ? "Blue" : "Red",
                    () =>
                    {
                        // Reload card back texture
                        string themeCardBack = settings.Theme == "Red" ? "CardBack_Red" : "CardBack_Blue";
                        cardBackTexture = ScreenManager.Game.Content.Load<Texture2D>($"Images/Cards/{themeCardBack}");
                        MainMenuScreen.Theme = settings.Theme;
                    },
                    ref yPos);
                AddCycleSetting(Resources.SettingsCurrency, () => settings.Currency,
                    CycleToPreviousCurrency,
                    CycleToNextCurrency,
                    null, ref yPos);

                yPos += groupSpacing - itemSpacing; // Extra space before next group

                // AUDIO section
                AddHeader(Resources.SettingsAudio, ref yPos);
                AddSliderSetting(Resources.SettingsSoundVolume, () => settings.SoundVolume,
                    (v) => settings.SoundVolume = v, ref yPos);
                AddSliderSetting(Resources.SettingsMusicVolume, () => settings.MusicVolume,
                    (v) => settings.MusicVolume = v, ref yPos);
            }
            else if (currentPage == 1)
            {
                // Page 2: GAMEPLAY and AI PLAYERS
                AddHeader(Resources.SettingsGameplay, ref yPos);
                AddCycleSetting(Resources.SettingsAnimationSpeed, () => settings.AnimationSpeed.ToString(),
                    () =>
                    {
                        settings.AnimationSpeed = settings.AnimationSpeed switch
                        {
                            AnimationSpeed.Fast => AnimationSpeed.Normal,
                            AnimationSpeed.Normal => AnimationSpeed.Slow,
                            _ => AnimationSpeed.Fast
                        };
                    },
                    () =>
                    {
                        settings.AnimationSpeed = settings.AnimationSpeed switch
                        {
                            AnimationSpeed.Slow => AnimationSpeed.Normal,
                            AnimationSpeed.Normal => AnimationSpeed.Fast,
                            _ => AnimationSpeed.Slow
                        };
                    },
                    null, ref yPos);
                AddCheckboxSetting(Resources.SettingsAutoStandOn21, () => settings.AutoStandOn21,
                    (v) => settings.AutoStandOn21 = v, ref yPos);
                AddCheckboxSetting(Resources.SettingsShowCardCount, () => settings.ShowCardCount,
                    (v) => settings.ShowCardCount = v, ref yPos);
                AddCheckboxSetting(Resources.SettingsPersistWinnings, () => settings.PersistWinnings,
                    (v) => settings.PersistWinnings = v, ref yPos);

                yPos += groupSpacing - itemSpacing; // Extra space before next group

                // AI PLAYERS section
                AddHeader(Resources.SettingsAIPlayers, ref yPos);
                AddCounterSetting(Resources.SettingsMaxAIPlayers, () => settings.MaxAIPlayers, 0, GameSettings.GetPlatformMaxAIPlayers(),
                    (v) => settings.MaxAIPlayers = (byte)v, ref yPos);
                AddCheckboxSetting(Resources.SettingsFillEmptySlots, () => settings.FillEmptySlotsWithAI,
                    (v) => settings.FillEmptySlotsWithAI = v, ref yPos);
            }

            // Calculate navigation button bounds at bottom (proportional to screen)
            float heightScale = safeArea.Height / 720f;
            int navButtonY = safeArea.Bottom - (int)(60 * heightScale);
            int navButtonHeight = (int)(40 * heightScale);

            // Calculate button widths based on text size with padding
            Vector2 prevTextSize = ScreenManager.RegularFont.MeasureString(Resources.Previous);
            Vector2 nextTextSize = ScreenManager.RegularFont.MeasureString(Resources.Next);
            int prevButtonWidth = (int)(prevTextSize.X + 40 * heightScale);
            int nextButtonWidth = (int)(nextTextSize.X + 40 * heightScale);

            prevButtonBounds = new Rectangle(safeArea.Left + (int)(100 * heightScale), navButtonY, prevButtonWidth, navButtonHeight);
            nextButtonBounds = new Rectangle(safeArea.Right - nextButtonWidth - (int)(100 * heightScale), navButtonY, nextButtonWidth, navButtonHeight);

            // Calculate back button bounds in top-left corner
            int backButtonSize = (int)(50 * heightScale);
            int backButtonPadding = (int)(20 * heightScale);
            backButtonBounds = new Rectangle(
                safeArea.Left + backButtonPadding,
                safeArea.Top + backButtonPadding,
                backButtonSize,
                backButtonSize);
        }

        private string GetLanguageDisplay()
        {
            // Language names are already stored in native format
            return settings.Language;
        }

        private void CycleToNextLanguage()
        {
            // Determine next language
            string nextLanguage = settings.Language switch
            {
                "English" => "Français",
                "Français" => "Español",
                "Español" => "Italiano",
                "Italiano" => "Русский",
                "Русский" => "日本語",
                "日本語" => "中文",
                "中文" => "English",
                _ => "English"
            };

            // Phase 1: Reload fonts BEFORE changing language
            ScreenManager.ReloadFontForLanguage(nextLanguage);

            // Phase 2: Set the language (this will change CurrentUICulture)
            settings.Language = nextLanguage;

            // Phase 3: Refresh all screens now that language and fonts are both updated
            BuildSettingItems();
            ScreenManager.RefreshScreensAfterLanguageChange();
        }

        private void CycleToPreviousLanguage()
        {
            // Determine previous language
            string previousLanguage = settings.Language switch
            {
                "English" => "中文",
                "中文" => "日本語",
                "日本語" => "Русский",
                "Русский" => "Italiano",
                "Italiano" => "Español",
                "Español" => "Français",
                "Français" => "English",
                _ => "中文" // Default to last active language
            };

            // Phase 1: Reload fonts BEFORE changing language
            ScreenManager.ReloadFontForLanguage(previousLanguage);

            // Phase 2: Set the language (this will change CurrentUICulture)
            settings.Language = previousLanguage;

            // Phase 3: Refresh all screens now that language and fonts are both updated
            BuildSettingItems();
            ScreenManager.RefreshScreensAfterLanguageChange();
        }

        private void CycleToNextCurrency()
        {
            settings.Currency = settings.Currency switch
            {
                "$" => "€",
                "€" => "£",
                "£" => "¥",
                "¥" => "R",
                "R" => "$",
                _ => "$"
            };
            GameSettings.Save();
        }

        private void CycleToPreviousCurrency()
        {
            settings.Currency = settings.Currency switch
            {
                "$" => "R",
                "R" => "¥",
                "¥" => "£",
                "£" => "€",
                "€" => "$",
                _ => "$"
            };
            GameSettings.Save();
        }

        private void AddHeader(string text, ref float yPos)
        {
            settingItems.Add(new SettingItem
            {
                Type = SettingType.Header,
                Label = text,
                Bounds = new Rectangle((int)leftMargin, (int)yPos, safeArea.Width - (int)leftMargin * 2, (int)(40 * (safeArea.Height / 720f)))
            });
            yPos += itemSpacing;
        }

        private void AddCounterSetting(string label, Func<int> getValue, int min, int max,
            Action<int> setValue, ref float yPos)
        {
            settingItems.Add(new SettingItem
            {
                Type = SettingType.Counter,
                Label = label,
                GetValue = () => getValue().ToString(),
                OnDecrease = () =>
                {
                    int val = getValue() - 1;
                    if (val < min) val = max;
                    setValue(val);
                    GameSettings.Save();
                },
                OnIncrease = () =>
                {
                    int val = getValue() + 1;
                    if (val > max) val = min;
                    setValue(val);
                    GameSettings.Save();
                },
                Bounds = new Rectangle((int)leftMargin, (int)yPos, safeArea.Width - (int)leftMargin * 2, (int)(40 * (safeArea.Height / 720f)))
            });
            yPos += itemSpacing;
        }

        private void AddSliderSetting(string label, Func<float> getValue, Action<float> setValue, ref float yPos)
        {
            settingItems.Add(new SettingItem
            {
                Type = SettingType.Slider,
                Label = label,
                GetValue = () => $"{(int)(getValue() * 100)}%",
                OnDecrease = () =>
                {
                    float val = getValue() - 0.1f;
                    if (val < 0) val = 1.0f;
                    setValue(val);
                    GameSettings.Save();
                },
                OnIncrease = () =>
                {
                    float val = getValue() + 0.1f;
                    if (val > 1.0f) val = 0f;
                    setValue(val);
                    GameSettings.Save();
                },
                Bounds = new Rectangle((int)leftMargin, (int)yPos, safeArea.Width - (int)leftMargin * 2, (int)(40 * (safeArea.Height / 720f)))
            });
            yPos += itemSpacing;
        }

        private void AddCheckboxSetting(string label, Func<bool> getValue, Action<bool> setValue, ref float yPos)
        {
            settingItems.Add(new SettingItem
            {
                Type = SettingType.Checkbox,
                Label = label,
                GetValue = () => getValue() ? "[X]" : "[ ]",
                OnClick = () =>
                {
                    setValue(!getValue());
                    GameSettings.Save();
                },
                Bounds = new Rectangle((int)leftMargin, (int)yPos, safeArea.Width - (int)leftMargin * 2, (int)(40 * (safeArea.Height / 720f)))
            });
            yPos += itemSpacing;
        }

        private void AddCycleSetting(string label, Func<string> getValue, Action onPrevious, Action onNext,
            Action onChange, ref float yPos)
        {
            settingItems.Add(new SettingItem
            {
                Type = SettingType.Cycle,
                Label = label,
                GetValue = getValue,
                OnDecrease = () =>
                {
                    onPrevious();
                    onChange?.Invoke();
                    GameSettings.Save();
                },
                OnIncrease = () =>
                {
                    onNext();
                    onChange?.Invoke();
                    GameSettings.Save();
                },
                Bounds = new Rectangle((int)leftMargin, (int)yPos, safeArea.Width - (int)leftMargin * 2, (int)(40 * (safeArea.Height / 720f)))
            });
            yPos += itemSpacing;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);


        }

        /// <summary>
        /// Responds to user input.
        /// </summary>
        public override void HandleInput(InputState inputState)
        {
            // Cancel the settings screen if the user presses the back button
            PlayerIndex player;
            if (inputState.IsNewButtonPress(Buttons.Back, ControllingPlayer, out player))
            {
                ExitScreen();
                return;
            }

            // Check for escape key
            if (inputState.IsNewKeyPress(Keys.Escape, ControllingPlayer, out player))
            {
                ExitScreen();
                return;
            }

            // Get transformed cursor position (handles scaling/letterboxing on mobile)
            Vector2 cursorPos = inputState.CurrentCursorLocation;
            Point mousePos = new Point((int)cursorPos.X, (int)cursorPos.Y);

            // Track button press state
            pressedButtonIndex = -1;
            isNextButtonPressed = false;
            isPrevButtonPressed = false;
            isBackButtonPressed = false;

            if (UIUtility.IsDesktop)
            {
                // Handle mouse input - detect click (press + release)
                if (inputState.CurrentMouseState.LeftButton == ButtonState.Released)
                {
                    if (isMouseDown)
                    {
                        isMouseDown = false;
                        HandleSettingItemClick(mousePos);
                    }
                }
                else if (inputState.CurrentMouseState.LeftButton == ButtonState.Pressed)
                {
                    isMouseDown = true;
                    // Track hover on press
                    TrackHoverState(mousePos);
                }
            }
            else if (UIUtility.IsMobile)
            {
                // Handle touch input with gestures (like MenuScreen does)
                foreach (GestureSample gesture in inputState.Gestures)
                {
                    if (gesture.GestureType == GestureType.Tap)
                    {
                        HandleSettingItemClick(mousePos);
                    }
                }
            }

            // Track hover state for visual feedback on desktop
            if (UIUtility.IsDesktop && inputState.CurrentMouseState.LeftButton == ButtonState.Released)
            {
                TrackHoverState(mousePos);
            }
        }

        private void HandleSettingItemClick(Point clickLocation)
        {
            // Check back button
            if (backButtonBounds.Contains(clickLocation))
            {
                isBackButtonPressed = true;
                AudioManager.PlaySound("menu_select");
                ExitScreen();
                return;
            }

            // Check navigation buttons
            if (currentPage < TotalPages - 1 && nextButtonBounds.Contains(clickLocation))
            {
                isNextButtonPressed = true;
                currentPage++;
                BuildSettingItems();
                AudioManager.PlaySound("menu_select");
                return;
            }
            else if (currentPage > 0 && prevButtonBounds.Contains(clickLocation))
            {
                isPrevButtonPressed = true;
                currentPage--;
                BuildSettingItems();
                AudioManager.PlaySound("menu_select");
                return;
            }

            // Check setting items
            for (int i = 0; i < settingItems.Count; i++)
            {
                SettingItem item = settingItems[i];
                if (item.Type == SettingType.Header) continue;

                if (item.Bounds.Contains(clickLocation))
                {
                    // Button bounds
                    Rectangle leftButton = GetLeftButtonBounds(item.Bounds);
                    Rectangle rightButton = GetRightButtonBounds(item.Bounds);

                    if (item.Type == SettingType.Checkbox)
                    {
                        // Get checkbox bounds (centered in value column)
                        Rectangle checkboxBounds = GetCheckboxButtonBounds(item.Bounds);
                        if (checkboxBounds.Contains(clickLocation))
                        {
                            item.OnClick?.Invoke();
                            AudioManager.PlaySound("menu_select");
                        }
                    }
                    else if (leftButton.Contains(clickLocation))
                    {
                        item.OnDecrease?.Invoke();
                        AudioManager.PlaySound("menu_select");
                        pressedButtonIndex = i * 2; // Even indices for left buttons
                    }
                    else if (rightButton.Contains(clickLocation))
                    {
                        item.OnIncrease?.Invoke();
                        AudioManager.PlaySound("menu_select");
                        pressedButtonIndex = i * 2 + 1; // Odd indices for right buttons
                    }
                    break;
                }
            }
        }

        private void TrackHoverState(Point mousePos)
        {
            // Track hover state for visual feedback
            hoveredIndex = -1;
            for (int i = 0; i < settingItems.Count; i++)
            {
                if (settingItems[i].Type != SettingType.Header &&
                    settingItems[i].Bounds.Contains(mousePos))
                {
                    hoveredIndex = i;
                    break;
                }
            }
        }

        private Rectangle GetLeftButtonBounds(Rectangle itemBounds)
        {
            return new Rectangle(
                itemBounds.Right - (int)(buttonSpacing + buttonSize + rightButtonMargin),
                itemBounds.Y + (itemBounds.Height - (int)buttonSize) / 2,
                (int)buttonSize, (int)buttonSize);
        }

        private Rectangle GetRightButtonBounds(Rectangle itemBounds)
        {
            return new Rectangle(
                itemBounds.Right - (int)buttonSize - (int)rightButtonMargin,
                itemBounds.Y + (itemBounds.Height - (int)buttonSize) / 2,
                (int)buttonSize, (int)buttonSize);
        }

        private Rectangle GetCheckboxButtonBounds(Rectangle itemBounds)
        {
            // Center in the value column, matching the position where counter values appear
            int leftButtonRight = itemBounds.Right - (int)(buttonSpacing + buttonSize + rightButtonMargin) + (int)buttonSize;
            int rightButtonLeft = itemBounds.Right - (int)buttonSize - (int)rightButtonMargin;
            int centerX = (leftButtonRight + rightButtonLeft) / 2 - (int)buttonSize / 2;

            return new Rectangle(
                centerX,
                itemBounds.Y + (itemBounds.Height - (int)buttonSize) / 2,
                (int)buttonSize, (int)buttonSize);
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

            // Draw background
            spriteBatch.Draw(background, safeArea, Color.White * TransitionAlpha);

            // Draw title
            string title = Resources.SettingsTitle;
            Vector2 titleSize = ScreenManager.Font.MeasureString(title);
            Vector2 titlePos = new Vector2(safeArea.Center.X - titleSize.X / 2, safeArea.Top + 10);
            spriteBatch.DrawString(ScreenManager.Font, title, titlePos, Color.White * TransitionAlpha);

            // Draw card preview
            Rectangle cardRect = new Rectangle((int)cardPreviewPosition.X, (int)cardPreviewPosition.Y,
                (int)cardSize.X, (int)cardSize.Y);
            spriteBatch.Draw(cardBackTexture, cardRect, Color.White * TransitionAlpha);

            // Draw all setting items
            for (int i = 0; i < settingItems.Count; i++)
            {
                SettingItem item = settingItems[i];
                Color color = Color.White * TransitionAlpha;

                if (item.Type == SettingType.Header)
                {
                    // Draw header with larger font
                    Vector2 headerPos = new Vector2(item.Bounds.X, item.Bounds.Y);
                    spriteBatch.DrawString(ScreenManager.Font, item.Label, headerPos,
                        Color.Gold * TransitionAlpha);
                }
                else
                {
                    // Highlight if hovered
                    if (i == hoveredIndex)
                    {
                        spriteBatch.Draw(background, item.Bounds, Color.White * 0.2f * TransitionAlpha);
                    }

                    // Draw label
                    Vector2 labelPos = new Vector2(item.Bounds.X + 10,
                        item.Bounds.Y + (item.Bounds.Height - ScreenManager.RegularFont.LineSpacing) / 2);
                    spriteBatch.DrawString(ScreenManager.RegularFont, item.Label, labelPos, color);

                    // Draw value and controls
                    string valueText = item.GetValue?.Invoke() ?? "";
                    Vector2 valueSize = ScreenManager.RegularFont.MeasureString(valueText);

                    if (item.Type == SettingType.Checkbox)
                    {
                        // Draw checkbox as a button in the center of the value column
                        bool isChecked = item.GetValue?.Invoke() == "[X]";
                        Rectangle checkboxBounds = GetCheckboxButtonBounds(item.Bounds);

                        Texture2D checkboxTexture = isChecked ? buttonPressedTexture : buttonRegularTexture;
                        spriteBatch.Draw(checkboxTexture, checkboxBounds, Color.White * TransitionAlpha);

                        // Draw checkmark if checked
                        if (isChecked)
                        {
                            Vector2 checkSize = ScreenManager.RegularFont.MeasureString("X");
                            Vector2 checkPos = new Vector2(
                                checkboxBounds.X + (checkboxBounds.Width - checkSize.X) / 2,
                                checkboxBounds.Y + (checkboxBounds.Height - checkSize.Y) / 2);
                            spriteBatch.DrawString(ScreenManager.RegularFont, "X", checkPos, Color.Black * TransitionAlpha);
                        }
                    }
                    else
                    {
                        // Get button bounds
                        Rectangle leftButton = GetLeftButtonBounds(item.Bounds);
                        Rectangle rightButton = GetRightButtonBounds(item.Bounds);

                        // Draw left button [-]
                        bool leftPressed = (pressedButtonIndex == i * 2);
                        Texture2D leftTexture = leftPressed ? buttonPressedTexture : buttonRegularTexture;
                        spriteBatch.Draw(leftTexture, leftButton, Color.White * TransitionAlpha);
                        Vector2 minusSize = ScreenManager.RegularFont.MeasureString("-");
                        Vector2 minusPos = new Vector2(
                            leftButton.X + (leftButton.Width - minusSize.X) / 2,
                            leftButton.Y + (leftButton.Height - minusSize.Y) / 2);
                        spriteBatch.DrawString(ScreenManager.RegularFont, "-", minusPos, Color.Black * TransitionAlpha);

                        // Draw value in center
                        Vector2 valuePos = new Vector2(
                            leftButton.Right + (rightButton.Left - leftButton.Right - valueSize.X) / 2,
                            item.Bounds.Y + (item.Bounds.Height - ScreenManager.RegularFont.LineSpacing) / 2);
                        spriteBatch.DrawString(ScreenManager.RegularFont, valueText, valuePos, color);

                        // Draw right button [+]
                        bool rightPressed = (pressedButtonIndex == i * 2 + 1);
                        Texture2D rightTexture = rightPressed ? buttonPressedTexture : buttonRegularTexture;
                        spriteBatch.Draw(rightTexture, rightButton, Color.White * TransitionAlpha);
                        Vector2 plusSize = ScreenManager.RegularFont.MeasureString("+");
                        Vector2 plusPos = new Vector2(
                            rightButton.X + (rightButton.Width - plusSize.X) / 2,
                            rightButton.Y + (rightButton.Height - plusSize.Y) / 2);
                        spriteBatch.DrawString(ScreenManager.RegularFont, "+", plusPos, Color.Black * TransitionAlpha);
                    }
                }
            }

            // Draw back button
            Texture2D backTexture = isBackButtonPressed ? buttonPressedTexture : buttonRegularTexture;
            spriteBatch.Draw(backTexture, backButtonBounds, Color.Red * TransitionAlpha);
            string backText = "X";
            Vector2 backTextSize = ScreenManager.Font.MeasureString(backText);
            Vector2 backTextPos = new Vector2(
                backButtonBounds.X + (backButtonBounds.Width - backTextSize.X) / 2,
                backButtonBounds.Y + (backButtonBounds.Height - backTextSize.Y) / 2);
            spriteBatch.DrawString(ScreenManager.Font, backText, backTextPos, Color.White * TransitionAlpha);

            // Draw page navigation buttons
            if (currentPage > 0)
            {
                Texture2D prevTexture = isPrevButtonPressed ? buttonPressedTexture : buttonRegularTexture;
                spriteBatch.Draw(prevTexture, prevButtonBounds, Color.White * TransitionAlpha);
                string prevText = Resources.Previous;
                Vector2 prevTextSize = ScreenManager.RegularFont.MeasureString(prevText);
                Vector2 prevTextPos = new Vector2(
                    prevButtonBounds.X + (prevButtonBounds.Width - prevTextSize.X) / 2,
                    prevButtonBounds.Y + (prevButtonBounds.Height - prevTextSize.Y) / 2);
                spriteBatch.DrawString(ScreenManager.RegularFont, prevText, prevTextPos, Color.Black * TransitionAlpha);
            }

            if (currentPage < TotalPages - 1)
            {
                Texture2D nextTexture = isNextButtonPressed ? buttonPressedTexture : buttonRegularTexture;
                spriteBatch.Draw(nextTexture, nextButtonBounds, Color.White * TransitionAlpha);
                string nextText = Resources.Next;
                Vector2 nextTextSize = ScreenManager.RegularFont.MeasureString(nextText);
                Vector2 nextTextPos = new Vector2(
                    nextButtonBounds.X + (nextButtonBounds.Width - nextTextSize.X) / 2,
                    nextButtonBounds.Y + (nextButtonBounds.Height - nextTextSize.Y) / 2);
                spriteBatch.DrawString(ScreenManager.RegularFont, nextText, nextTextPos, Color.Black * TransitionAlpha);
            }

            // Draw page indicator
            string pageText = string.Format(Resources.PageIndicator, currentPage + 1, TotalPages);
            Vector2 pageTextSize = ScreenManager.RegularFont.MeasureString(pageText);
            Vector2 pageTextPos = new Vector2(
                safeArea.Center.X - pageTextSize.X / 2,
                safeArea.Bottom - 55);
            spriteBatch.DrawString(ScreenManager.RegularFont, pageText, pageTextPos, Color.White * TransitionAlpha);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }

    enum SettingType
    {
        Header,
        Counter,
        Slider,
        Cycle,
        Checkbox
    }

    class SettingItem
    {
        public SettingType Type { get; set; }
        public string Label { get; set; }
        public Func<string> GetValue { get; set; }
        public Action OnClick { get; set; }
        public Action OnIncrease { get; set; }
        public Action OnDecrease { get; set; }
        public Rectangle Bounds { get; set; }
    }
}