//-----------------------------------------------------------------------------
// BlackjackCardGame.UIOrchestration.cs
//
// Partial class containing UI setup, button layout, and UI event orchestration.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        /// <summary>
        /// Performs necessary initializations.
        /// </summary>
        public void Initialize()
        {
            base.LoadContent();
            // Initialize a new bet component
            // You may need to pass input state from elsewhere
            betGameComponent = new BetGameComponent(players, screenManager.InputState, Theme, this,
                screenManager.SpriteBatch, screenManager.GlobalTransformation);
            Game.Components.Add(betGameComponent);

            // Calculate proportional dimensions
            int screenWidth = screenManager.SafeArea.Width;
            int screenHeight = screenManager.SafeArea.Height;
            Rectangle bounds = new Rectangle(0, 0, ScreenManager.BASE_BUFFER_WIDTH, ScreenManager.BASE_BUFFER_HEIGHT);
            int smallPadding = UIConstants.GetSmallPadding(bounds.Width);
            int mediumPadding = UIConstants.GetMediumPadding(screenHeight);
            int buttonWidth = UIConstants.GetButtonWidth(screenWidth);
            int buttonHeight = UIConstants.GetButtonHeight(screenHeight);
            int buttonSpacing = UIConstants.GetButtonSpacing(screenWidth);
            int wideButtonWidth = UIConstants.GetWideButtonWidth(screenWidth);

            // Initialize the game buttons - position them centered above betting chips
            // All action and betting buttons will be in a single row above the chips
            int chipHeight = 50; // Approximate chip texture height
            int buttonY = ScreenManager.BASE_BUFFER_HEIGHT - chipHeight - (smallPadding * 3) - buttonHeight;

            // All possible buttons in a single row (Deal, Clear, Hit, Stand, Double, Split, Insurance, New Hand)
            // Using resource strings for localization
            var buttonData = new[]
            {
                new { Key = "Deal", Text = Resources.Deal },
                new { Key = "Clear", Text = Resources.Clear },
                new { Key = "Hit", Text = Resources.Hit },
                new { Key = "Stand", Text = Resources.Stand },
                new { Key = "Double", Text = Resources.Double },
                new { Key = "Split", Text = Resources.Split },
                new { Key = "Insurance", Text = Resources.Insurance },
                new { Key = "NewHand", Text = Resources.NewHand }
            };

            foreach (var btn in buttonData)
            {
                int minWidth = (btn.Key == "NewHand")
                    ? UIConstants.GetWideButtonWidth(screenManager.SafeArea.Width)
                    : buttonWidth;
                int width = UIConstants.CalculateButtonWidth(btn.Text, this.Font, minWidth, screenWidth);
                // X=0 is a placeholder; RepositionButtons() will centre the row after the loop
                Button button = new Button("ButtonRegular", "ButtonPressed",
                    screenManager.InputState, this, screenManager.SpriteBatch, screenManager.GlobalTransformation)
                {
                    Text = btn.Text,
                    Bounds = new Rectangle(0, buttonY, width, buttonHeight),
                    Font = this.Font,
                    Visible = false,
                    Enabled = false
                };
                if (btn.Key == "Deal") dealButton = button;
                if (btn.Key == "Clear") clearButton = button;
                if (btn.Key != "Deal" && btn.Key != "Clear" && btn.Key != "NewHand")
                    buttons.Add(btn.Key, button);
                if (btn.Key == "NewHand")
                    newGame = button;
                Game.Components.Add(button);
            }

            // Register to click event for gameplay buttons
            buttons["Hit"].Click += Hit_Click;
            buttons["Stand"].Click += Stand_Click;
            buttons["Double"].Click += Double_Click;
            buttons["Split"].Click += Split_Click;
            buttons["Insurance"].Click += Insurance_Click;
            newGame.Click += newGame_Click;

            // Set button colors - green for positive actions, red for negative actions
            buttons["Hit"].Color = Color.Lime;
            buttons["Stand"].Color = Color.Red;
            buttons["Double"].Color = Color.Cyan;
            buttons["Split"].Color = Color.Yellow;
            newGame.Color = Color.Lime;

            // Centre the button row now that all widths are set
            RepositionButtons();

            // Create back button (X) in top-left corner
            int backButtonSize = (int)(50 * (screenHeight / 720f)); // Scale proportionally
            int backButtonPadding = (int)(20 * (screenHeight / 720f));
            backButton = new Button("ButtonRegular", "ButtonPressed",
                screenManager.InputState, this, screenManager.SpriteBatch, screenManager.GlobalTransformation)
            {
                Text = "X",
                Bounds = new Rectangle(backButtonPadding, backButtonPadding, backButtonSize, backButtonSize),
                Font = this.Font,
                Visible = true,
                Enabled = true,
                Color = Color.Red
            };
            backButton.Click += BackButton_Click;
            Game.Components.Add(backButton);
        }

        /// <summary>
        /// Handler for back button click - opens pause menu
        /// </summary>
        private void BackButton_Click(object sender, EventArgs e)
        {
            // Leverage the existing pause functionality from GameplayScreen
            var gameplayScreen = screenManager.GetScreens().OfType<GameplayScreen>().FirstOrDefault();
            gameplayScreen?.PauseCurrentGame();
        }

        /// <summary>
        /// Updates button text and fonts after language change to match current Resources
        /// Also resizes buttons to fit the new text
        /// </summary>
        public void UpdateButtonText()
        {
            // First update the game's font from content manager
            bool useCJKFont = Blackjack.GameSettings.Instance.Language == "日本語" ||
                              Blackjack.GameSettings.Instance.Language == "中文";
            string fontPath = useCJKFont ? "Fonts/Regular_CJK" : "Fonts/Regular";
            this.Font = Game.Content.Load<SpriteFont>(fontPath);

            // Collect all buttons that need updating with their new text
            var buttonUpdates = new List<(Button button, string text, bool isWide)>();

            if (dealButton != null)
                buttonUpdates.Add((dealButton, Resources.Deal, false));
            if (clearButton != null)
                buttonUpdates.Add((clearButton, Resources.Clear, false));
            if (buttons.ContainsKey("Hit"))
                buttonUpdates.Add((buttons["Hit"], Resources.Hit, false));
            if (buttons.ContainsKey("Stand"))
                buttonUpdates.Add((buttons["Stand"], Resources.Stand, false));
            if (buttons.ContainsKey("Double"))
                buttonUpdates.Add((buttons["Double"], Resources.Double, false));
            if (buttons.ContainsKey("Split"))
                buttonUpdates.Add((buttons["Split"], Resources.Split, false));
            if (buttons.ContainsKey("Insurance"))
                buttonUpdates.Add((buttons["Insurance"], Resources.Insurance, false));
            if (newGame != null)
                buttonUpdates.Add((newGame, Resources.NewHand, true));

            // Update text, font, and calculate new widths
            int screenWidth = screenManager.SafeArea.Width;
            int minButtonWidth = UIConstants.GetButtonWidth(screenWidth);
            int minWideWidth = UIConstants.GetWideButtonWidth(screenWidth);

            foreach (var (button, text, isWide) in buttonUpdates)
            {
                button.Text = text;
                button.Font = this.Font;

                // Calculate new width based on text using centralized UIConstants method
                int newWidth = UIConstants.CalculateButtonWidth(text, this.Font,
                    isWide ? minWideWidth : minButtonWidth, screenWidth);

                // Update button bounds with new width (keep same X, Y, and height)
                button.Bounds = new Rectangle(
                    button.Bounds.X,
                    button.Bounds.Y,
                    newWidth,
                    button.Bounds.Height);
            }

            // Recalculate button positions to maintain proper spacing
            RepositionButtons();
        }

        /// <summary>
        /// Repositions all buttons to maintain even spacing after width changes
        /// </summary>
        private void RepositionButtons()
        {
            int smallPadding = UIConstants.GetSmallPadding(screenManager.SafeArea.Width);

            // Calculate total width of all buttons
            int totalWidth = 0;
            var allButtons = new List<Button>();

            // Add all buttons in order
            if (dealButton != null) allButtons.Add(dealButton);
            if (clearButton != null) allButtons.Add(clearButton);
            if (buttons.ContainsKey("Hit")) allButtons.Add(buttons["Hit"]);
            if (buttons.ContainsKey("Stand")) allButtons.Add(buttons["Stand"]);
            if (buttons.ContainsKey("Double")) allButtons.Add(buttons["Double"]);
            if (buttons.ContainsKey("Split")) allButtons.Add(buttons["Split"]);
            if (buttons.ContainsKey("Insurance")) allButtons.Add(buttons["Insurance"]);
            if (newGame != null) allButtons.Add(newGame);

            foreach (var button in allButtons)
            {
                totalWidth += button.Bounds.Width;
            }

            totalWidth += smallPadding * (allButtons.Count - 1); // Add spacing between buttons

            // Center the button row
            int startX = (ScreenManager.BASE_BUFFER_WIDTH - totalWidth) / 2;
            int currentX = startX;

            // Reposition each button
            foreach (var button in allButtons)
            {
                button.Bounds = new Rectangle(
                    currentX,
                    button.Bounds.Y,
                    button.Bounds.Width,
                    button.Bounds.Height);

                currentX += button.Bounds.Width + smallPadding;
            }
        }

        /// <summary>
        /// Sets the button availability according to the options available to the 
        /// current player.
        /// </summary>
        private void SetButtonAvailability()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            // Hide all buttons if no player is in play or the player is an NPC player
            if (player == null || player is BlackjackNPCPlayer)
            {
                ChangeButtonsEnablement(false);
                ChangeButtonsVisiblility(false);
                return;
            }

            // Show all buttons
            ChangeButtonsEnablement(true);
            ChangeButtonsVisiblility(true);

            // Set insurance button availability
            buttons["Insurance"].Visible = showInsurance;
            buttons["Insurance"].Enabled = showInsurance;

            if (player.IsSplit == false)
            {
                // Remember that the bet amount was already reduced from the balance,
                // so we only need to check if the player has more money than the
                // current bet when trying to double/split

                // Set double button availability
                if (player.BetAmount > player.Balance || player.Hand.Count != 2)
                {
                    buttons["Double"].Visible = false;
                    buttons["Double"].Enabled = false;
                }

                // TODO v2.x: Add "Strict Casino Rules" setting to allow splitting any 10-value cards (10/J/Q/K)
                // Currently only allows splitting identical ranks (strict casino rules)
                // For lenient rules, check Blackjack point values instead: GetBlackJackValue(card1) == GetBlackJackValue(card2) && both == 10
                if (player.Hand.Count != 2 ||
                    player.Hand[0].Value != player.Hand[1].Value ||
                    player.BetAmount > player.Balance)
                {
                    buttons["Split"].Visible = false;
                    buttons["Split"].Enabled = false;
                }
            }
            else
            {
                // We've performed a split. Get the initial bet amount to check whether
                // or not we can double the current bet.
                float initialBet = player.BetAmount /
                                   ((player.Double ? 2f : 1f) + (player.SecondDouble ? 2f : 1f));

                // Set double button availability.
                if (initialBet > player.Balance || player.CurrentHand.Count != 2)
                {
                    buttons["Double"].Visible = false;
                    buttons["Double"].Enabled = false;
                }

                // Once you've split, you can't split again
                buttons["Split"].Visible = false;
                buttons["Split"].Enabled = false;
            }

            LayoutVisibleButtons();
        }

        /// <summary>
        /// Changes the visiblility of most game buttons.
        /// </summary>
        /// <param name="visible">True to make the buttons visible, false to make
        /// them invisible.</param>
        void ChangeButtonsVisiblility(bool visible)
        {
            buttons["Hit"].Visible = visible;
            buttons["Stand"].Visible = visible;
            buttons["Double"].Visible = visible;
            buttons["Split"].Visible = visible;
            buttons["Insurance"].Visible = visible;

            LayoutVisibleButtons();
        }

        /// <summary>
        /// Enables or disable most game buttons.
        /// </summary>
        /// <param name="enabled">True to enable the buttons , false to 
        /// disable them.</param>
        void ChangeButtonsEnablement(bool enabled)
        {
            buttons["Hit"].Enabled = enabled;
            buttons["Stand"].Enabled = enabled;
            buttons["Double"].Enabled = enabled;
            buttons["Split"].Enabled = enabled;
            buttons["Insurance"].Enabled = enabled;

            LayoutVisibleButtons();
        }

        /// <summary>
        /// Handles the Click event of the insurance button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Insurance_Click(object sender, EventArgs e)
        {
            ExecuteInputAction(Networking.BlackjackAction.Insurance);
        }

        /// <summary>
        /// Handles the Click event of the new game button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void newGame_Click(object sender, EventArgs e)
        {
            FinishTurn();
            StartRound();
            newGame.Enabled = false;
            newGame.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the hit button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Hit_Click(object sender, EventArgs e)
        {
            ExecuteInputAction(Networking.BlackjackAction.Hit);
        }

        /// <summary>
        /// Handles the Click event of the stand button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Stand_Click(object sender, EventArgs e)
        {
            ExecuteInputAction(Networking.BlackjackAction.Stand);
        }

        /// <summary>
        /// Handles the Click event of the double button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Double_Click(object sender, EventArgs e)
        {
            ExecuteInputAction(Networking.BlackjackAction.Double);
        }

        /// <summary>
        /// Handles the Click event of the split button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The 
        /// <see cref="System.EventArgs"/> instance containing the event data.</param>
        void Split_Click(object sender, EventArgs e)
        {
            ExecuteInputAction(Networking.BlackjackAction.Split);
        }

        private void LayoutVisibleButtons()
        {
            // Gather all visible buttons (including "New Hand" if it's not in the dictionary)
            List<Button> visibleButtons = new List<Button>();
            foreach (var btn in buttons.Values)
            {
                if (btn.Visible)
                    visibleButtons.Add(btn);
            }

            if (newGame != null && newGame.Visible && !visibleButtons.Contains(newGame))
                visibleButtons.Add(newGame);

            if (visibleButtons.Count == 0)
                return;

            // Get UI constants
            int screenWidth = screenManager.SafeArea.Width;
            int screenHeight = screenManager.SafeArea.Height;
            int smallPadding = UIConstants.GetSmallPadding(screenWidth);
            int buttonHeight = UIConstants.GetButtonHeight(screenHeight);

            // Get actual chip height from bet component for accurate positioning
            int chipHeight = betGameComponent?.ChipHeight ?? 50;

            // Calculate button Y position using shared helper for consistency with betting phase
            int buttonY = UIConstants.GetGameplayButtonYPosition(chipHeight, ScreenManager.BASE_BUFFER_WIDTH,
                ScreenManager.BASE_BUFFER_HEIGHT);

            // Calculate total width of all visible buttons using their existing widths
            // (widths were set correctly by CalculateButtonWidth at creation / UpdateButtonText)
            int totalWidth = 0;
            for (int i = 0; i < visibleButtons.Count; i++)
            {
                totalWidth += visibleButtons[i].Bounds.Width;
                if (i < visibleButtons.Count - 1)
                    totalWidth += smallPadding;
            }

            // Center the row horizontally
            int startX = (ScreenManager.BASE_BUFFER_WIDTH - totalWidth) / 2;

            // Position each button, preserving its existing width
            int currentX = startX;
            foreach (var btn in visibleButtons)
            {
                btn.Bounds = new Rectangle(currentX, buttonY, btn.Bounds.Width, buttonHeight);
                currentX += btn.Bounds.Width + smallPadding;
            }
        }
    }
}
