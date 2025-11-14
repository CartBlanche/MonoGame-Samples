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
using Microsoft.Xna.Framework.Content;
using System.IO;

namespace Blackjack
{
    class SettingsScreen : GameScreen
    {
        private Texture2D background;
        private Texture2D cardBackTexture;
        private Texture2D buttonRegularTexture;
        private Texture2D buttonPressedTexture;
        private SpriteFont titleFont;
        private SpriteFont settingsFont;
        private SpriteFont headerFont;
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

        private InputHelper inputHelper;
        private float itemSpacing;
        private float groupSpacing;
        private float leftMargin;
        private float buttonSize;
        private float rightButtonMargin;
        private float buttonSpacing;

        public SettingsScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            settings = GameSettings.Instance;
        }

        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            background = content.Load<Texture2D>("Images/UI/table");
            titleFont = content.Load<SpriteFont>("Fonts/MenuFont");
            settingsFont = content.Load<SpriteFont>("Fonts/Regular");
            headerFont = content.Load<SpriteFont>("Fonts/MenuFont");
            buttonRegularTexture = content.Load<Texture2D>("Images/ButtonRegular");
            buttonPressedTexture = content.Load<Texture2D>("Images/ButtonPressed");

            // Load current theme card back
            string themeCardBack = settings.Theme == "Red" ? "CardBack_Red" : "CardBack_Blue";
            cardBackTexture = content.Load<Texture2D>($"Images/Cards/{themeCardBack}");

            // Initialize input helper
            inputHelper = new InputHelper(ScreenManager);

            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            safeArea = viewport.TitleSafeArea;

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

        private void BuildSettingItems()
        {
            settingItems.Clear();
            float yPos = cardPreviewPosition.Y + cardSize.Y + 20; // Start below card preview

            if (currentPage == 0)
            {
                // Page 1: DISPLAY, AUDIO, and AI PLAYERS
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

                yPos += groupSpacing - itemSpacing; // Extra space before next group

                // AUDIO section
                AddHeader(Resources.SettingsAudio, ref yPos);
                AddSliderSetting(Resources.SettingsSoundVolume, () => settings.SoundVolume,
                    (v) => settings.SoundVolume = v, ref yPos);
                AddSliderSetting(Resources.SettingsMusicVolume, () => settings.MusicVolume,
                    (v) => settings.MusicVolume = v, ref yPos);

                yPos += groupSpacing - itemSpacing;

                // AI PLAYERS section
                AddHeader(Resources.SettingsAIPlayers, ref yPos);
                AddCounterSetting(Resources.SettingsMaxAIPlayers, () => settings.MaxAIPlayers, 0, GameSettings.GetPlatformMaxAIPlayers(),
                    (v) => settings.MaxAIPlayers = (byte)v, ref yPos);
                AddCheckboxSetting(Resources.SettingsFillEmptySlots, () => settings.FillEmptySlotsWithAI,
                    (v) => settings.FillEmptySlotsWithAI = v, ref yPos);
            }
            else if (currentPage == 1)
            {
                // Page 2: GAMEPLAY
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
            }

            // Calculate navigation button bounds at bottom (proportional to screen)
            float heightScale = safeArea.Height / 720f;
            int navButtonY = safeArea.Bottom - (int)(60 * heightScale);
            int navButtonHeight = (int)(40 * heightScale);

            // Calculate button widths based on text size with padding
            Vector2 prevTextSize = settingsFont.MeasureString(Resources.Previous);
            Vector2 nextTextSize = settingsFont.MeasureString(Resources.Next);
            int prevButtonWidth = (int)(prevTextSize.X + 40 * heightScale);
            int nextButtonWidth = (int)(nextTextSize.X + 40 * heightScale);

            prevButtonBounds = new Rectangle(safeArea.Left + (int)(100 * heightScale), navButtonY, prevButtonWidth, navButtonHeight);
            nextButtonBounds = new Rectangle(safeArea.Right - nextButtonWidth - (int)(100 * heightScale), navButtonY, nextButtonWidth, navButtonHeight);
        }

        private string GetLanguageDisplay()
        {
            // Language names are already stored in native format
            return settings.Language;
        }

        private void CycleToNextLanguage()
        {
            settings.Language = settings.Language switch
            {
                "English" => "Français",
                "Français" => "Español",
                "Español" => "Italiano",
                "Italiano" => "English",
                //"Italiano" => "日本語",
                //"日本語" => "中文",
                _ => "English"
            };
        }

        private void CycleToPreviousLanguage()
        {
            settings.Language = settings.Language switch
            {
                "中文" => "日本語",
                "日本語" => "Italiano",
                "Italiano" => "Español",
                "Español" => "Français",
                "Français" => "English",
                _ => "Italiano" // Default to last active language
            };
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

            if (IsActive && !coveredByOtherScreen)
            {
                HandleInput();
            }
        }

        private MouseState previousMouseState;
        private KeyboardState previousKeyboardState;

        private void HandleInput()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // Check for escape/back
            if ((keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape)) ||
                inputHelper.IsEscape)
            {
                ExitScreen();
                previousKeyboardState = keyboardState;
                return;
            }

            // Get mouse position
            Point mousePos = new Point(mouseState.X, mouseState.Y);

            // Track button press state
            pressedButtonIndex = -1;
            isNextButtonPressed = false;
            isPrevButtonPressed = false;

            // Check for mouse click (left button just pressed)
            if (mouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                // Check navigation buttons
                if (currentPage < TotalPages - 1 && nextButtonBounds.Contains(mousePos))
                {
                    isNextButtonPressed = true;
                    currentPage++;
                    BuildSettingItems();
                    AudioManager.PlaySound("menu_select");
                    previousMouseState = mouseState;
                    previousKeyboardState = keyboardState;
                    return;
                }
                else if (currentPage > 0 && prevButtonBounds.Contains(mousePos))
                {
                    isPrevButtonPressed = true;
                    currentPage--;
                    BuildSettingItems();
                    AudioManager.PlaySound("menu_select");
                    previousMouseState = mouseState;
                    previousKeyboardState = keyboardState;
                    return;
                }

                for (int i = 0; i < settingItems.Count; i++)
                {
                    SettingItem item = settingItems[i];
                    if (item.Type == SettingType.Header) continue;

                    if (item.Bounds.Contains(mousePos))
                    {
                        // Button bounds
                        Rectangle leftButton = GetLeftButtonBounds(item.Bounds);
                        Rectangle rightButton = GetRightButtonBounds(item.Bounds);

                        if (item.Type == SettingType.Checkbox)
                        {
                            // Get checkbox bounds (centered in value column)
                            Rectangle checkboxBounds = GetCheckboxButtonBounds(item.Bounds);
                            if (checkboxBounds.Contains(mousePos))
                            {
                                item.OnClick?.Invoke();
                                AudioManager.PlaySound("menu_select");
                            }
                        }
                        else if (leftButton.Contains(mousePos))
                        {
                            item.OnDecrease?.Invoke();
                            AudioManager.PlaySound("menu_select");
                            pressedButtonIndex = i * 2; // Even indices for left buttons
                        }
                        else if (rightButton.Contains(mousePos))
                        {
                            item.OnIncrease?.Invoke();
                            AudioManager.PlaySound("menu_select");
                            pressedButtonIndex = i * 2 + 1; // Odd indices for right buttons
                        }
                        break;
                    }
                }
            }

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

            previousMouseState = mouseState;
            previousKeyboardState = keyboardState;
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

            spriteBatch.Begin();

            // Draw background
            spriteBatch.Draw(background, safeArea, Color.White * TransitionAlpha);

            // Draw title
            string title = Resources.SettingsTitle;
            Vector2 titleSize = titleFont.MeasureString(title);
            Vector2 titlePos = new Vector2(safeArea.Center.X - titleSize.X / 2, safeArea.Top + 10);
            spriteBatch.DrawString(titleFont, title, titlePos, Color.White * TransitionAlpha);

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
                    spriteBatch.DrawString(headerFont, item.Label, headerPos,
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
                        item.Bounds.Y + (item.Bounds.Height - settingsFont.LineSpacing) / 2);
                    spriteBatch.DrawString(settingsFont, item.Label, labelPos, color);

                    // Draw value and controls
                    string valueText = item.GetValue?.Invoke() ?? "";
                    Vector2 valueSize = settingsFont.MeasureString(valueText);

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
                            Vector2 checkSize = settingsFont.MeasureString("X");
                            Vector2 checkPos = new Vector2(
                                checkboxBounds.X + (checkboxBounds.Width - checkSize.X) / 2,
                                checkboxBounds.Y + (checkboxBounds.Height - checkSize.Y) / 2);
                            spriteBatch.DrawString(settingsFont, "X", checkPos, Color.Black * TransitionAlpha);
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
                        Vector2 minusSize = settingsFont.MeasureString("-");
                        Vector2 minusPos = new Vector2(
                            leftButton.X + (leftButton.Width - minusSize.X) / 2,
                            leftButton.Y + (leftButton.Height - minusSize.Y) / 2);
                        spriteBatch.DrawString(settingsFont, "-", minusPos, Color.Black * TransitionAlpha);

                        // Draw value in center
                        Vector2 valuePos = new Vector2(
                            leftButton.Right + (rightButton.Left - leftButton.Right - valueSize.X) / 2,
                            item.Bounds.Y + (item.Bounds.Height - settingsFont.LineSpacing) / 2);
                        spriteBatch.DrawString(settingsFont, valueText, valuePos, color);

                        // Draw right button [+]
                        bool rightPressed = (pressedButtonIndex == i * 2 + 1);
                        Texture2D rightTexture = rightPressed ? buttonPressedTexture : buttonRegularTexture;
                        spriteBatch.Draw(rightTexture, rightButton, Color.White * TransitionAlpha);
                        Vector2 plusSize = settingsFont.MeasureString("+");
                        Vector2 plusPos = new Vector2(
                            rightButton.X + (rightButton.Width - plusSize.X) / 2,
                            rightButton.Y + (rightButton.Height - plusSize.Y) / 2);
                        spriteBatch.DrawString(settingsFont, "+", plusPos, Color.Black * TransitionAlpha);
                    }
                }
            }

            // Draw page navigation buttons
            if (currentPage > 0)
            {
                Texture2D prevTexture = isPrevButtonPressed ? buttonPressedTexture : buttonRegularTexture;
                spriteBatch.Draw(prevTexture, prevButtonBounds, Color.White * TransitionAlpha);
                string prevText = Resources.Previous;
                Vector2 prevTextSize = settingsFont.MeasureString(prevText);
                Vector2 prevTextPos = new Vector2(
                    prevButtonBounds.X + (prevButtonBounds.Width - prevTextSize.X) / 2,
                    prevButtonBounds.Y + (prevButtonBounds.Height - prevTextSize.Y) / 2);
                spriteBatch.DrawString(settingsFont, prevText, prevTextPos, Color.Black * TransitionAlpha);
            }

            if (currentPage < TotalPages - 1)
            {
                Texture2D nextTexture = isNextButtonPressed ? buttonPressedTexture : buttonRegularTexture;
                spriteBatch.Draw(nextTexture, nextButtonBounds, Color.White * TransitionAlpha);
                string nextText = Resources.Next;
                Vector2 nextTextSize = settingsFont.MeasureString(nextText);
                Vector2 nextTextPos = new Vector2(
                    nextButtonBounds.X + (nextButtonBounds.Width - nextTextSize.X) / 2,
                    nextButtonBounds.Y + (nextButtonBounds.Height - nextTextSize.Y) / 2);
                spriteBatch.DrawString(settingsFont, nextText, nextTextPos, Color.Black * TransitionAlpha);
            }

            // Draw page indicator
            string pageText = string.Format(Resources.PageIndicator, currentPage + 1, TotalPages);
            Vector2 pageTextSize = settingsFont.MeasureString(pageText);
            Vector2 pageTextPos = new Vector2(
                safeArea.Center.X - pageTextSize.X / 2,
                safeArea.Bottom - 35);
            spriteBatch.DrawString(settingsFont, pageText, pageTextPos, Color.White * TransitionAlpha);

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