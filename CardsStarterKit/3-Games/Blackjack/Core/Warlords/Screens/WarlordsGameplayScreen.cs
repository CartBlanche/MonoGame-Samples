//-----------------------------------------------------------------------------
// WarlordsGameplayScreen.cs
//
// Main gameplay screen for Warlords
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WarlordsFramework;

namespace Warlords
{
    /// <summary>
    /// Main gameplay screen for Warlords
    /// </summary>
    public class WarlordsGameplayScreen : GameScreen
    {
        private WarlordsCardGame game;
        private ScreenManager screenManager;
        private string theme;
        private SpriteFont font;
        private MouseState previousMouseState;
        private Rectangle endTurnButton;
        private Rectangle advancePhaseButton;
        private Rectangle drawCardButton;
        private Rectangle sacrificeButton;
        
        // Card selection
        private WarlordsCard selectedCard;
        private CharacterCard selectedCharacterOnField; // Track selected character from field
        private Rectangle[] handCardRects;
        private Rectangle[] zoneRects;
        private Rectangle enemyBattlefieldRect;

        // Mulligan
        private System.Collections.Generic.List<WarlordsCard> mulliganSelected =
            new System.Collections.Generic.List<WarlordsCard>();
        private Rectangle mulliganConfirmButton;
        private Rectangle mulliganSkipButton;

        // Terrain contest
        private Rectangle contestPassButton;

        // Modal dialog (terrain counter notification)
        private Rectangle dialogCloseButton;
        
        // Feedback system
        private string feedbackMessage = "";
        private float feedbackTimer = 0f;
        private const float FeedbackDuration = 2.0f;
        
        public WarlordsGameplayScreen(string theme)
        {
            this.theme = theme;
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }
        
        public override void LoadContent()
        {
            screenManager = ScreenManager;
            Rectangle safeArea = screenManager.SafeArea;
            
            // Load font
            font = ScreenManager.Game.Content.Load<SpriteFont>("Fonts/Regular");
            
            // Create and initialize game.
            // Initialize() already calls StartGame() internally — do NOT call it again here.
            game = new WarlordsCardGame(safeArea, screenManager, theme);
            game.Initialize();
            
            // Get screen dimensions
            int screenHeight = screenManager.GraphicsDevice.Viewport.Height;
            int screenWidth = screenManager.GraphicsDevice.Viewport.Width;
            
            // Calculate proportional dimensions
            int handAreaHeight = UIConstants.GetHandAreaHeight(screenHeight);
            int topBarHeight = UIConstants.GetTopBarHeight(screenHeight);
            int buttonWidth = UIConstants.GetButtonWidth(screenWidth);
            int buttonHeight = UIConstants.GetButtonHeight(screenHeight);
            int padding = UIConstants.GetPadding(screenWidth);
            
            // Define end turn button - in hand area on right
            int handAreaY = screenHeight - handAreaHeight;
            
            endTurnButton = new Rectangle(
                screenManager.SafeArea.Right - buttonWidth - padding,
                handAreaY + (handAreaHeight - buttonHeight) / 2,
                buttonWidth,
                buttonHeight
            );

            // Phase and utility buttons on the left side of hand area
            drawCardButton = new Rectangle(
                padding,
                handAreaY + (handAreaHeight - buttonHeight) / 2,
                buttonWidth,
                buttonHeight);

            advancePhaseButton = new Rectangle(
                padding + buttonWidth + padding,
                handAreaY + (handAreaHeight - buttonHeight) / 2,
                buttonWidth,
                buttonHeight);

            sacrificeButton = new Rectangle(
                padding + (buttonWidth + padding) * 2,
                handAreaY + (handAreaHeight - buttonHeight) / 2,
                buttonWidth,
                buttonHeight);
            
            // Initialize card and zone rectangles
            handCardRects = new Rectangle[10]; // Max 10 cards in hand
            zoneRects = new Rectangle[2]; // Player's two zones (Home Base and Battlefield)
            
            // Define zone click areas
            int playAreaHeight = screenHeight - handAreaHeight - topBarHeight;
            int zoneHeight = playAreaHeight / 4;
            
            // Your Battlefield (purple zone) - 3rd zone
            zoneRects[0] = new Rectangle(0, topBarHeight + (2 * zoneHeight), screenWidth, zoneHeight);
            
            // Your Home Base (dark blue zone) - 4th zone
            zoneRects[1] = new Rectangle(0, topBarHeight + (3 * zoneHeight), screenWidth, zoneHeight);

            // Enemy Battlefield (dark red zone) - 2nd zone
            enemyBattlefieldRect = new Rectangle(0, topBarHeight + (1 * zoneHeight), screenWidth, zoneHeight);
            
            previousMouseState = Mouse.GetState();

            // Mulligan buttons — centred near bottom of screen
            int btnW = UIConstants.GetButtonWidth(screenWidth) * 2;
            int btnH = UIConstants.GetButtonHeight(screenHeight);
            int btnY = screenHeight - btnH - UIConstants.GetPadding(screenHeight);
            mulliganConfirmButton = new Rectangle((screenWidth / 2) - btnW - UIConstants.GetPadding(screenWidth), btnY, btnW, btnH);
            mulliganSkipButton    = new Rectangle((screenWidth / 2) + UIConstants.GetPadding(screenWidth), btnY, btnW, btnH);

            // Terrain contest pass button — same row as mulligan buttons
            int contestBtnW = UIConstants.GetButtonWidth(screenWidth) * 2;
            contestPassButton = new Rectangle((screenWidth / 2) - contestBtnW / 2, btnY, contestBtnW, btnH);

            // Modal close button (centered near bottom)
            dialogCloseButton = new Rectangle((screenWidth / 2) - btnW / 2, btnY, btnW, btnH);

            base.LoadContent();
        }
        
        public override void HandleInput(InputState input)
        {
            MouseState currentMouseState = Mouse.GetState();

            bool clicked = currentMouseState.LeftButton == ButtonState.Released &&
                           previousMouseState.LeftButton == ButtonState.Pressed;
            bool rightClicked = currentMouseState.RightButton == ButtonState.Released &&
                                previousMouseState.RightButton == ButtonState.Pressed;

            // Modal notification dialog has top priority and blocks all other input.
            if (game.IsDialogOpen)
            {
                if (clicked)
                {
                    Point mp = new Point(currentMouseState.X, currentMouseState.Y);
                    if (dialogCloseButton.Contains(mp))
                        game.CloseDialog();
                }

                previousMouseState = currentMouseState;
                return;
            }

            // ── Home terrain selection input ──────────────────────────────
            if (game.State == WarlordsGameState.HomeTerrainSelectionPending && clicked)
            {
                Point mp = new Point(currentMouseState.X, currentMouseState.Y);
                var terrains = game.Player.Deck.OfType<TerrainCard>().ToList();

                int sw  = screenManager.GraphicsDevice.Viewport.Width;
                int sh  = screenManager.GraphicsDevice.Viewport.Height;
                int cw  = UIConstants.GetCardWidth(sw);
                int ch  = (int)(cw * 1.6f);
                int cs  = UIConstants.GetCardSpacing(sw);
                int totalW = terrains.Count * cw + Math.Max(0, terrains.Count - 1) * cs;
                int startX = (sw - totalW) / 2;
                int startY = sh / 2 - ch / 2;

                for (int i = 0; i < terrains.Count; i++)
                {
                    var rect = new Rectangle(startX + i * (cw + cs), startY, cw, ch);
                    if (rect.Contains(mp))
                    {
                        game.SelectHomeTerrain(terrains[i]);
                        break;
                    }
                }
            }

            // ── Terrain contest input (only when player is the contester) ──
            if (game.State == WarlordsGameState.TerrainContestPending && clicked &&
                game.PendingTerrainProposer == game.Opponent)
            {
                Point mp = new Point(currentMouseState.X, currentMouseState.Y);

                if (contestPassButton.Contains(mp))
                {
                    game.PassTerrainContest();
                }
                else
                {
                    // Toggle a terrain card from hand for countering
                    int sw  = screenManager.GraphicsDevice.Viewport.Width;
                    int sh  = screenManager.GraphicsDevice.Viewport.Height;
                    int cw  = UIConstants.GetCardWidth(sw);
                    int ch  = (int)(cw * 1.4f);
                    int cs  = UIConstants.GetCardSpacing(sw);
                    var terrains = game.Player.Hand.OfType<TerrainCard>().ToList();
                    int totalW   = terrains.Count * cw + Math.Max(0, terrains.Count - 1) * cs;
                    int startX   = (sw - totalW) / 2;
                    int startY   = sh / 2 - ch / 2;

                    for (int i = 0; i < terrains.Count; i++)
                    {
                        var rect = new Rectangle(startX + i * (cw + cs), startY, cw, ch);
                        if (rect.Contains(mp))
                        {
                            game.CounterTerrain(terrains[i]);
                            break;
                        }
                    }
                }
            }

            // ── Mulligan input ────────────────────────────────────────
            if (game.State == WarlordsGameState.MulliganPending && clicked)
            {
                Point mp = new Point(currentMouseState.X, currentMouseState.Y);

                if (mulliganConfirmButton.Contains(mp))
                {
                    game.PerformMulligan(mulliganSelected);
                    mulliganSelected.Clear();
                }
                else if (mulliganSkipButton.Contains(mp))
                {
                    game.SkipMulligan();
                    mulliganSelected.Clear();
                }
                else
                {
                    // Toggle card selection — reuse the same card rect layout as DrawMulliganScreen.
                    int sw  = screenManager.GraphicsDevice.Viewport.Width;
                    int sh  = screenManager.GraphicsDevice.Viewport.Height;
                    int cw  = UIConstants.GetCardWidth(sw);
                    int ch  = (int)(cw * 1.4f);
                    int cs  = UIConstants.GetCardSpacing(sw);
                    int pad = UIConstants.GetPadding(sw);
                    int totalWidth = game.Player.Hand.Count * cw + (game.Player.Hand.Count - 1) * cs;
                    int startX = (sw - totalWidth) / 2;
                    int startY = sh / 2 - ch / 2;

                    for (int i = 0; i < game.Player.Hand.Count; i++)
                    {
                        var rect = new Rectangle(startX + i * (cw + cs), startY, cw, ch);
                        if (rect.Contains(mp))
                        {
                            var card = game.Player.Hand[i];
                            if (mulliganSelected.Contains(card))
                                mulliganSelected.Remove(card);
                            else
                                mulliganSelected.Add(card);
                            break;
                        }
                    }
                }
            }

            // Only allow normal input during player's turn
            if (game.WaitingForPlayer && game.State == WarlordsGameState.Playing)
            {
                // Right-click: put selected field character into Defend stance
                if (rightClicked && selectedCharacterOnField != null)
                {
                    game.Defend(selectedCharacterOnField);
                    // Keep the selection visible so the DEF indicator is clear
                }

                if (currentMouseState.LeftButton == ButtonState.Released &&
                    previousMouseState.LeftButton == ButtonState.Pressed)
                {
                    Point mousePos = new Point(currentMouseState.X, currentMouseState.Y);
                    
                    // Draw card (Draw phase only)
                    if (drawCardButton.Contains(mousePos))
                    {
                        if (game.CurrentPhase == TurnPhase.Draw)
                        {
                            int beforeHand = game.Player.Hand.Count;
                            game.TryDrawForCurrentPlayer();
                            if (game.Player.Hand.Count == beforeHand)
                            {
                                feedbackMessage = "Cannot draw now.";
                                feedbackTimer = FeedbackDuration;
                            }
                        }
                        else
                        {
                            feedbackMessage = "Draw is only available in Draw phase.";
                            feedbackTimer = FeedbackDuration;
                        }
                    }
                    // Advance phase button
                    else if (advancePhaseButton.Contains(mousePos))
                    {
                        selectedCard = null;
                        selectedCharacterOnField = null;
                        game.AdvanceCurrentPlayerPhase();
                    }
                    // Sacrifice selected hand card
                    else if (sacrificeButton.Contains(mousePos))
                    {
                        if (selectedCard != null && game.Player.Hand.Contains(selectedCard))
                        {
                            game.SacrificeCard(selectedCard);
                            selectedCard = null;
                            selectedCharacterOnField = null;
                        }
                        else
                        {
                            feedbackMessage = "Select a card in hand to sacrifice.";
                            feedbackTimer = FeedbackDuration;
                        }
                    }
                    // Check for end turn button click (force end)
                    else if (endTurnButton.Contains(mousePos))
                    {
                        selectedCard = null; // Clear selection
                        selectedCharacterOnField = null; // Clear character selection
                        game.PlayerEndTurn();
                    }
                    // Check for hand card clicks
                    else
                    {
                        bool cardClicked = false;
                        for (int i = 0; i < game.Player.Hand.Count && i < handCardRects.Length; i++)
                        {
                            if (handCardRects[i].Contains(mousePos))
                            {
                                var clickedCard = game.Player.Hand[i];
                                
                                // Check if player can afford this card
                                if (game.Player.SEManager.CurrentSE >= clickedCard.SoulEssenceCost)
                                {
                                    // Special handling for event cards - they play immediately
                                    if (clickedCard is EventCard eventCard)
                                    {
                                        game.PlayEvent(eventCard);
                                        feedbackMessage = ""; // Clear any previous feedback
                                        selectedCard = null; // Events don't stay selected
                                    }
                                    else
                                    {
                                        selectedCard = clickedCard;
                                        feedbackMessage = ""; // Clear any previous feedback
                                    }
                                }
                                else
                                {
                                    // Can't afford to play now; still allow selecting for sacrifice.
                                    int needed = clickedCard.SoulEssenceCost - game.Player.SEManager.CurrentSE;
                                    feedbackMessage = $"Not enough SE! Need {needed} more.";
                                    feedbackTimer = FeedbackDuration;
                                    selectedCard = clickedCard;
                                    selectedCharacterOnField = null;
                                }
                                
                                cardClicked = true;
                                break;
                            }
                        }
                        
                        // If no card in hand was clicked, check for clicking characters on field
                        if (!cardClicked)
                        {
                            // Check if clicking on a character already on the field for movement
                            CharacterCard clickedCharacter = GetCharacterAtPosition(mousePos);
                            if (clickedCharacter != null)
                            {
                                // If we have an item selected, try to equip it
                                if (selectedCard is ItemCard itemCard)
                                {
                                    game.PlayItem(itemCard, clickedCharacter);
                                    selectedCard = null;
                                    selectedCharacterOnField = null;
                                }
                                // Otherwise, select this character for movement
                                else
                                {
                                    selectedCard = clickedCharacter;
                                    selectedCharacterOnField = clickedCharacter;
                                }
                                cardClicked = true;
                            }
                            else if (selectedCharacterOnField != null)
                            {
                                // Attack targeting: check for enemy character or enemy zone click
                                CharacterCard enemyChar = GetEnemyCharacterAtPosition(mousePos);
                                if (enemyChar != null)
                                {
                                    game.Attack(selectedCharacterOnField, enemyChar);
                                    selectedCard = null;
                                    selectedCharacterOnField = null;
                                    cardClicked = true;
                                }
                                else if (IsMouseInEnemyZone(mousePos))
                                {
                                    game.AttackPlayer(selectedCharacterOnField, game.Opponent);
                                    selectedCard = null;
                                    selectedCharacterOnField = null;
                                    cardClicked = true;
                                }
                            }
                        }
                        
                        // If we have a selected card and no character was clicked, handle zone clicks to play/move it
                        if (!cardClicked && selectedCard != null)
                        {
                            // Skip if we just selected a character above
                            CharacterCard clickedCharacter = GetCharacterAtPosition(mousePos);
                            if (clickedCharacter != null)
                            {
                                // Already handled above
                            }
                            else if (selectedCard is ItemCard itemCard)
                            {
                                // Items can only be equipped to characters (already handled above when clicking character)
                                // If clicking zone with item selected, do nothing
                            }
                            else
                            {
                                // Character or Terrain cards
                                // Check if this is a character already on field (movement)
                                bool isCharacterOnField = selectedCard is CharacterCard charCard &&
                                    (game.Field.PlayerHomeBase.Characters.Contains(charCard) ||
                                     game.Field.PlayerBattlefield.Characters.Contains(charCard));
                                
                                if (isCharacterOnField)
                                {
                                    // Try to move the character
                                    // Determine current zone
                                    GameZone fromZone = game.Field.PlayerHomeBase.Characters.Contains((CharacterCard)selectedCard) 
                                        ? game.Field.PlayerHomeBase 
                                        : game.Field.PlayerBattlefield;
                                    
                                    // Check Your Battlefield
                                    if (zoneRects[0].Contains(mousePos))
                                    {
                                        game.MoveCharacter((CharacterCard)selectedCard, fromZone, game.Field.PlayerBattlefield);
                                        selectedCard = null;
                                        selectedCharacterOnField = null;
                                    }
                                    // Check Your Home Base
                                    else if (zoneRects[1].Contains(mousePos))
                                    {
                                        game.MoveCharacter((CharacterCard)selectedCard, fromZone, game.Field.PlayerHomeBase);
                                        selectedCard = null;
                                        selectedCharacterOnField = null;
                                    }
                                    // Check Enemy Battlefield (advance)
                                    else if (enemyBattlefieldRect.Contains(mousePos))
                                    {
                                        game.MoveCharacter((CharacterCard)selectedCard, fromZone, game.Field.OpponentBattlefield);
                                        selectedCard = null;
                                        selectedCharacterOnField = null;
                                    }
                                }
                                else
                                {
                                    // Playing from hand to zones
                                    // Characters must be deployed to Home Base unless a special skill says otherwise.
                                    if (selectedCard is CharacterCard)
                                    {
                                        if (zoneRects[1].Contains(mousePos))
                                        {
                                            game.PlayCardGeneric(selectedCard, game.Field.PlayerHomeBase);
                                            selectedCard = null;
                                            selectedCharacterOnField = null;
                                        }
                                        else if (zoneRects[0].Contains(mousePos))
                                        {
                                            feedbackMessage = "Characters must be deployed to your Home Base.";
                                            feedbackTimer = FeedbackDuration;
                                        }
                                    }
                                    else
                                    {
                                        // Terrain/Event/Item routing from hand
                                        // Check Your Battlefield
                                        if (zoneRects[0].Contains(mousePos))
                                        {
                                            game.PlayCardGeneric(selectedCard, game.Field.PlayerBattlefield);
                                            selectedCard = null;
                                            selectedCharacterOnField = null;
                                        }
                                        // Check Your Home Base
                                        else if (zoneRects[1].Contains(mousePos))
                                        {
                                            game.PlayCardGeneric(selectedCard, game.Field.PlayerHomeBase);
                                            selectedCard = null;
                                            selectedCharacterOnField = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            previousMouseState = currentMouseState;
            
            // Check for ESC key to pause/menu
            if (input.IsPauseGame(null))
            {
                ScreenManager.AddScreen(new WarlordsMainMenuScreen(), null);
            }
            
            base.HandleInput(input);
        }
        
        /// <summary>
        /// Get character card at mouse position (in player's zones only)
        /// </summary>
        private CharacterCard GetCharacterAtPosition(Point mousePos)
        {
            int screenWidth = ScreenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = ScreenManager.GraphicsDevice.Viewport.Height;
            
            // Calculate proportional dimensions - must match DrawZonesWithCards exactly
            int handAreaHeight = UIConstants.GetHandAreaHeight(screenHeight);
            int topBarHeight = UIConstants.GetTopBarHeight(screenHeight);
            int playAreaHeight = screenHeight - handAreaHeight - topBarHeight;
            int zoneHeight = playAreaHeight / 4;
            
            int cardWidth = UIConstants.GetCardWidth(screenWidth);
            int cardHeight = UIConstants.GetCardHeight(screenHeight);
            int cardSpacing = UIConstants.GetCardSpacing(screenWidth);
            int padding = UIConstants.GetPadding(screenWidth);
            
            // Check player zones (YOUR BATTLEFIELD is index 2, YOUR HOME BASE is index 3)
            GameZone[] playerZones = { game.Field.PlayerBattlefield, game.Field.PlayerHomeBase };
            int[] zoneIndices = { 2, 3 }; // Zone 2 and 3 are player zones
            
            for (int i = 0; i < playerZones.Length; i++)
            {
                var zone = playerZones[i];
                int yPos = topBarHeight + (zoneIndices[i] * zoneHeight);
                
                int cardX = (int)(screenWidth * 0.16f); // 16% from left
                int cardY = yPos + padding;
                int maxCardHeight = zoneHeight - (2 * padding);
                int actualCardHeight = Math.Min(cardHeight, maxCardHeight);
                
                for (int j = 0; j < zone.Characters.Count; j++)
                {
                    int x = cardX + (j * (cardWidth + cardSpacing));
                    Rectangle cardRect = new Rectangle(x, cardY, cardWidth, actualCardHeight);
                    
                    if (cardRect.Contains(mousePos))
                    {
                        return zone.Characters[j];
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Get enemy character card at mouse position (opponent zones only).
        /// Uses the same coordinate math as DrawZonesWithCards (zone indices 0 and 1).
        /// </summary>
        private CharacterCard GetEnemyCharacterAtPosition(Point mousePos)
        {
            int screenWidth  = ScreenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = ScreenManager.GraphicsDevice.Viewport.Height;

            int handAreaHeight = UIConstants.GetHandAreaHeight(screenHeight);
            int topBarHeight   = UIConstants.GetTopBarHeight(screenHeight);
            int playAreaHeight = screenHeight - handAreaHeight - topBarHeight;
            int zoneHeight     = playAreaHeight / 4;

            int cardWidth   = UIConstants.GetCardWidth(screenWidth);
            int cardHeight  = UIConstants.GetCardHeight(screenHeight);
            int cardSpacing = UIConstants.GetCardSpacing(screenWidth);
            int padding     = UIConstants.GetPadding(screenWidth);

            // Opponent zones: OpponentBase = draw index 0, OpponentBattlefield = draw index 1
            GameZone[] enemyZones  = { game.Field.OpponentBase, game.Field.OpponentBattlefield };
            int[]      zoneIndices = { 0, 1 };

            for (int i = 0; i < enemyZones.Length; i++)
            {
                var zone = enemyZones[i];
                int yPos           = topBarHeight + (zoneIndices[i] * zoneHeight);
                int cardX          = (int)(screenWidth * 0.16f);
                int cardY          = yPos + padding;
                int maxCardHeight   = zoneHeight - (2 * padding);
                int actualCardHeight = Math.Min(cardHeight, maxCardHeight);

                for (int j = 0; j < zone.Characters.Count; j++)
                {
                    int x = cardX + (j * (cardWidth + cardSpacing));
                    Rectangle cardRect = new Rectangle(x, cardY, cardWidth, actualCardHeight);
                    if (cardRect.Contains(mousePos))
                        return zone.Characters[j];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true when the mouse is over either enemy zone (OpponentBase or OpponentBattlefield).
        /// </summary>
        private bool IsMouseInEnemyZone(Point mousePos)
        {
            int screenHeight   = ScreenManager.GraphicsDevice.Viewport.Height;
            int screenWidth    = ScreenManager.GraphicsDevice.Viewport.Width;
            int handAreaHeight = UIConstants.GetHandAreaHeight(screenHeight);
            int topBarHeight   = UIConstants.GetTopBarHeight(screenHeight);
            int playAreaHeight = screenHeight - handAreaHeight - topBarHeight;
            int zoneHeight     = playAreaHeight / 4;

            // Enemy zones occupy the top two rows of the play area
            Rectangle enemyArea = new Rectangle(0, topBarHeight, screenWidth, zoneHeight * 2);
            return enemyArea.Contains(mousePos);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (!coveredByOtherScreen && game != null)
            {
                game.Update(gameTime);
                
                // Update feedback timer
                if (feedbackTimer > 0f)
                {
                    feedbackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (feedbackTimer < 0f)
                    {
                        feedbackTimer = 0f;
                        feedbackMessage = "";
                    }
                }
            }
            
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
        
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            
            if (game != null)
            {
                if (game.State == WarlordsGameState.HomeTerrainSelectionPending)
                {
                    DrawHomeTerrainScreen(gameTime);
                }
                else if (game.State == WarlordsGameState.MulliganPending)
                {
                    DrawMulliganScreen(gameTime);
                }
                else if (game.State == WarlordsGameState.TerrainContestPending)
                {
                    DrawGameInfo(gameTime);
                    DrawZonesWithCards(gameTime);
                    DrawTerrainContestScreen(gameTime);
                }
                else
                {
                    DrawGameInfo(gameTime);
                    DrawZonesWithCards(gameTime);
                    DrawPlayerHand(gameTime);
                    DrawEndTurnButton(gameTime);
                    DrawFeedbackMessage(gameTime);
                    DrawGameOverScreen(gameTime);
                }

                if (game.IsDialogOpen)
                    DrawModalDialog(gameTime, game.DialogMessage);
            }
        }
        
        /// <summary>
        /// Draw the mulligan selection screen shown once before turn 1.
        /// The player clicks cards to toggle them for swapping, then confirms or skips.
        /// </summary>
        private void DrawMulliganScreen(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin();

            int sw  = screenManager.GraphicsDevice.Viewport.Width;
            int sh  = screenManager.GraphicsDevice.Viewport.Height;
            int pad = UIConstants.GetPadding(sw);
            int cw  = UIConstants.GetCardWidth(sw);
            int ch  = (int)(cw * 1.4f);
            int cs  = UIConstants.GetCardSpacing(sw);

            // Full-screen dark overlay
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle(0, 0, sw, sh), Color.Black * 0.92f);

            // Title
            string title = "OPENING HAND - Select cards to swap back into your deck";
            Vector2 titleSize = font.MeasureString(title) * UIConstants.RegularTextScale;
            screenManager.SpriteBatch.DrawString(font, title,
                new Vector2((sw - titleSize.X) / 2f, pad * 2),
                Color.White, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

            // Sub-title
            string sub = $"{mulliganSelected.Count} card(s) selected  |  Max hand: {RulesEngine.MaxHandSize}";
            Vector2 subSize = font.MeasureString(sub) * UIConstants.SmallTextScale;
            screenManager.SpriteBatch.DrawString(font, sub,
                new Vector2((sw - subSize.X) / 2f, pad * 2 + titleSize.Y * UIConstants.RegularTextScale + 4),
                Color.LightGray, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

            // Hand cards — centred horizontally, vertically centred on screen
            int handCount  = game.Player.Hand.Count;
            int totalWidth = handCount * cw + Math.Max(0, handCount - 1) * cs;
            int startX     = (sw - totalWidth) / 2;
            int startY     = sh / 2 - ch / 2;

            for (int i = 0; i < handCount; i++)
            {
                var card = game.Player.Hand[i];
                bool selected = mulliganSelected.Contains(card);

                int x = startX + i * (cw + cs);
                Rectangle cardRect = new Rectangle(x, startY, cw, ch);

                // Card colour by type; teal tint when selected for swap
                Color cardColor = card is CharacterCard ? new Color(30, 80, 160) :
                                  card is TerrainCard   ? new Color(20, 100, 40)  :
                                  card is ItemCard      ? new Color(140, 100, 10)  :
                                                          new Color(100, 20, 140);

                if (selected) cardColor = Color.Lerp(cardColor, Color.Teal, 0.55f);

                screenManager.SpriteBatch.Draw(screenManager.BlankTexture, cardRect, cardColor);
                DrawCardBorder(cardRect,
                    selected ? Color.Cyan : Color.White,
                    selected ? UIConstants.BorderThicknessThick : UIConstants.BorderThicknessThin);

                // Card name
                string name = card.Name.Length > 13 ? card.Name.Substring(0, 13) : card.Name;
                screenManager.SpriteBatch.DrawString(font, name,
                    new Vector2(x + 3, startY + 3),
                    Color.White, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

                // Type label
                string typeLabel = card is CharacterCard ? "CHARACTER" :
                                   card is TerrainCard   ? "TERRAIN"   :
                                   card is ItemCard      ? "ITEM"       : "EVENT";
                screenManager.SpriteBatch.DrawString(font, typeLabel,
                    new Vector2(x + 3, startY + ch - 28),
                    Color.LightGray, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

                // SWAP indicator overlay
                if (selected)
                {
                    string swapLabel = "SWAP";
                    Vector2 swapSize = font.MeasureString(swapLabel) * UIConstants.RegularTextScale;
                    screenManager.SpriteBatch.DrawString(font, swapLabel,
                        new Vector2(x + (cw - swapSize.X) / 2f, startY + ch / 2f - swapSize.Y / 2f),
                        Color.Cyan, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
                }
            }

            // Buttons
            MouseState ms = Mouse.GetState();
            Point mousePos = new Point(ms.X, ms.Y);

            bool confirmHover = mulliganConfirmButton.Contains(mousePos);
            bool skipHover    = mulliganSkipButton.Contains(mousePos);

            // Confirm Mulligan (swap selected)
            Color confirmColor = mulliganSelected.Count > 0
                ? (confirmHover ? Color.Cyan * 0.9f   : new Color(0, 160, 160) * 0.85f)
                : (confirmHover ? Color.Gray * 0.7f   : Color.Gray * 0.5f);
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, mulliganConfirmButton, confirmColor);
            DrawCardBorder(mulliganConfirmButton, Color.White, UIConstants.BorderThicknessMedium);

            string confirmText = mulliganSelected.Count > 0
                ? $"SWAP {mulliganSelected.Count} CARD(S)"
                : "SWAP (none selected)";
            Vector2 confirmTextSize = font.MeasureString(confirmText) * UIConstants.RegularTextScale;
            screenManager.SpriteBatch.DrawString(font, confirmText,
                new Vector2(
                    mulliganConfirmButton.X + (mulliganConfirmButton.Width  - confirmTextSize.X) / 2f,
                    mulliganConfirmButton.Y + (mulliganConfirmButton.Height - confirmTextSize.Y) / 2f),
                Color.White, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

            // Keep Hand (skip)
            Color skipColor = skipHover ? Color.LimeGreen * 0.9f : Color.Green * 0.7f;
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, mulliganSkipButton, skipColor);
            DrawCardBorder(mulliganSkipButton, Color.White, UIConstants.BorderThicknessMedium);

            string skipText = "KEEP HAND";
            Vector2 skipTextSize = font.MeasureString(skipText) * UIConstants.RegularTextScale;
            screenManager.SpriteBatch.DrawString(font, skipText,
                new Vector2(
                    mulliganSkipButton.X + (mulliganSkipButton.Width  - skipTextSize.X) / 2f,
                    mulliganSkipButton.Y + (mulliganSkipButton.Height - skipTextSize.Y) / 2f),
                Color.White, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

            screenManager.SpriteBatch.End();
        }

        /// <summary>
        /// Full-screen overlay shown at game start for the player to pick their home terrain.
        /// All terrain cards from the player's deck are displayed. Clicking one selects it,
        /// placing it on the player's Home Base and advancing to the mulligan phase.
        /// </summary>
        private void DrawHomeTerrainScreen(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin();

            int sw  = screenManager.GraphicsDevice.Viewport.Width;
            int sh  = screenManager.GraphicsDevice.Viewport.Height;
            int pad = UIConstants.GetPadding(sw);
            int cw  = UIConstants.GetCardWidth(sw);
            int ch  = (int)(cw * 1.6f); // slightly taller to fit regen/effect text
            int cs  = UIConstants.GetCardSpacing(sw);

            // Background
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle(0, 0, sw, sh), new Color(10, 15, 30));

            // Title
            string title = "SELECT YOUR HOME TERRAIN";
            Vector2 titleSize = font.MeasureString(title) * UIConstants.TitleTextScale;
            screenManager.SpriteBatch.DrawString(font, title,
                new Vector2((sw - titleSize.X) / 2f, pad * 2),
                Color.Yellow, 0f, Vector2.Zero, UIConstants.TitleTextScale, SpriteEffects.None, 0f);

            // Sub-title
            string sub = "This terrain will be placed on your Home Base before the game begins.";
            Vector2 subSize = font.MeasureString(sub) * UIConstants.SmallTextScale;
            screenManager.SpriteBatch.DrawString(font, sub,
                new Vector2((sw - subSize.X) / 2f, pad * 2 + titleSize.Y * UIConstants.TitleTextScale + 6),
                Color.LightGray, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

            // AI status line
            bool aiPicked = game.Field.OpponentBase.ActiveTerrain != null;
            string aiLine = aiPicked
                ? $"Enemy has chosen: {game.Field.OpponentBase.ActiveTerrain.Name}"
                : "Enemy is choosing...";
            Vector2 aiLineSize = font.MeasureString(aiLine) * UIConstants.SmallTextScale;
            screenManager.SpriteBatch.DrawString(font, aiLine,
                new Vector2((sw - aiLineSize.X) / 2f,
                    pad * 2 + titleSize.Y * UIConstants.TitleTextScale + 6 + subSize.Y * UIConstants.SmallTextScale + 4),
                aiPicked ? Color.OrangeRed : Color.Gray,
                0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

            // Terrain cards
            var terrains = game.Player.Deck.OfType<TerrainCard>().ToList();
            int totalW = terrains.Count * cw + Math.Max(0, terrains.Count - 1) * cs;
            int startX = (sw - totalW) / 2;
            int startY = sh / 2 - ch / 2;

            MouseState ms = Mouse.GetState();
            Point mousePos = new Point(ms.X, ms.Y);

            for (int i = 0; i < terrains.Count; i++)
            {
                var card = terrains[i];
                int x = startX + i * (cw + cs);
                Rectangle cardRect = new Rectangle(x, startY, cw, ch);
                bool hover = cardRect.Contains(mousePos);

                Color cardColor = hover
                    ? Color.Lerp(new Color(20, 100, 40), Color.White, 0.15f)
                    : new Color(20, 100, 40);
                screenManager.SpriteBatch.Draw(screenManager.BlankTexture, cardRect, cardColor);
                DrawCardBorder(cardRect,
                    hover ? Color.Yellow : Color.LimeGreen,
                    hover ? UIConstants.BorderThicknessThick : UIConstants.BorderThicknessThin);

                // Card name
                string name = card.Name.Length > 13 ? card.Name.Substring(0, 13) : card.Name;
                screenManager.SpriteBatch.DrawString(font, name,
                    new Vector2(x + 3, startY + 3),
                    Color.White, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

                // Rarity
                screenManager.SpriteBatch.DrawString(font, card.Rarity.ToString(),
                    new Vector2(x + 3, startY + 22),
                    Color.Gold, 0f, Vector2.Zero, UIConstants.TinyTextScale, SpriteEffects.None, 0f);

                // Home Base effect (regen)
                if (card.RegenBonus > 0)
                {
                    string regenText = $"+{card.RegenBonus} Regen";
                    screenManager.SpriteBatch.DrawString(font, regenText,
                        new Vector2(x + 3, startY + ch / 2 - 18),
                        Color.Cyan, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                }

                // Battlefield effect summary
                string bfEffect = card.SEBonus > 0 && card.AttackBonus > 0
                    ? $"+{card.SEBonus}SE +{card.AttackBonus}ATK"
                    : card.SEBonus > 0
                        ? $"+{card.SEBonus} SE"
                        : card.AttackBonus > 0
                            ? $"+{card.AttackBonus} ATK"
                            : "";
                if (!string.IsNullOrEmpty(bfEffect))
                {
                    screenManager.SpriteBatch.DrawString(font, "BF: " + bfEffect,
                        new Vector2(x + 3, startY + ch / 2),
                        Color.Orange, 0f, Vector2.Zero, UIConstants.TinyTextScale, SpriteEffects.None, 0f);
                }

                // Cost
                string costText = $"{card.SoulEssenceCost} SE";
                Vector2 costSize = font.MeasureString(costText) * UIConstants.SmallTextScale;
                screenManager.SpriteBatch.DrawString(font, costText,
                    new Vector2(x + cw - costSize.X - 2, startY + ch - 18),
                    Color.Gold, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

                // "TERRAIN" label bottom-left
                screenManager.SpriteBatch.DrawString(font, "TERRAIN",
                    new Vector2(x + 3, startY + ch - 18),
                    Color.LightGreen, 0f, Vector2.Zero, UIConstants.TinyTextScale, SpriteEffects.None, 0f);

                // Hover prompt
                if (hover)
                {
                    string selectText = "CLICK TO SELECT";
                    Vector2 selSize = font.MeasureString(selectText) * UIConstants.TinyTextScale;
                    screenManager.SpriteBatch.DrawString(font, selectText,
                        new Vector2(x + (cw - selSize.X) / 2f, startY + ch + 4),
                        Color.Yellow, 0f, Vector2.Zero, UIConstants.TinyTextScale, SpriteEffects.None, 0f);
                }
            }

            screenManager.SpriteBatch.End();
        }

        /// <summary>
        /// Overlay shown when a terrain has been proposed and the opposing player must
        /// decide whether to counter it. The board is drawn behind this.
        /// When the AI proposed the terrain, the human player sees their terrain cards
        /// and a Pass button. When the human proposed, a "Waiting for AI..." message shows.
        /// </summary>
        private void DrawTerrainContestScreen(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin();

            int sw  = screenManager.GraphicsDevice.Viewport.Width;
            int sh  = screenManager.GraphicsDevice.Viewport.Height;
            int pad = UIConstants.GetPadding(sw);
            int cw  = UIConstants.GetCardWidth(sw);
            int ch  = (int)(cw * 1.4f);
            int cs  = UIConstants.GetCardSpacing(sw);

            // Semi-transparent overlay
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle(0, 0, sw, sh), Color.Black * 0.80f);

            // Title
            string proposerName = game.PendingTerrainProposer == game.Player ? "YOU" : "ENEMY";
            string terrainName  = game.PendingTerrain?.Name ?? "?";
            string title = $"TERRAIN CONTEST: {proposerName} played {terrainName}";
            Vector2 titleSize = font.MeasureString(title) * UIConstants.RegularTextScale;
            screenManager.SpriteBatch.DrawString(font, title,
                new Vector2((sw - titleSize.X) / 2f, pad * 2),
                Color.Yellow, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

            bool playerIsContester = game.PendingTerrainProposer == game.Opponent;

            if (!playerIsContester)
            {
                // Player proposed — waiting for AI
                string waitMsg = "Waiting for AI response...";
                Vector2 waitSize = font.MeasureString(waitMsg) * UIConstants.RegularTextScale;
                screenManager.SpriteBatch.DrawString(font, waitMsg,
                    new Vector2((sw - waitSize.X) / 2f, sh / 2f),
                    Color.Orange, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            }
            else
            {
                // Player is contester — show their terrain cards
                string sub = "Play a terrain to COUNTER (both return to deck) or PASS to let it land";
                Vector2 subSize = font.MeasureString(sub) * UIConstants.SmallTextScale;
                screenManager.SpriteBatch.DrawString(font, sub,
                    new Vector2((sw - subSize.X) / 2f, pad * 2 + titleSize.Y * UIConstants.RegularTextScale + 4),
                    Color.LightGray, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

                var terrains = game.Player.Hand.OfType<TerrainCard>().ToList();

                if (terrains.Count == 0)
                {
                    string noTerrains = "No terrain cards in hand.";
                    Vector2 noSize = font.MeasureString(noTerrains) * UIConstants.RegularTextScale;
                    screenManager.SpriteBatch.DrawString(font, noTerrains,
                        new Vector2((sw - noSize.X) / 2f, sh / 2f - ch / 2f),
                        Color.Gray, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
                }
                else
                {
                    int totalW = terrains.Count * cw + Math.Max(0, terrains.Count - 1) * cs;
                    int startX = (sw - totalW) / 2;
                    int startY = sh / 2 - ch / 2;

                    for (int i = 0; i < terrains.Count; i++)
                    {
                        var card = terrains[i];
                        bool canAfford = game.Player.SEManager.CurrentSE >= card.SoulEssenceCost;
                        int x = startX + i * (cw + cs);
                        Rectangle cardRect = new Rectangle(x, startY, cw, ch);

                        Color cardColor = canAfford ? new Color(20, 100, 40) : new Color(20, 60, 20) * 0.6f;
                        screenManager.SpriteBatch.Draw(screenManager.BlankTexture, cardRect, cardColor);
                        DrawCardBorder(cardRect, canAfford ? Color.LimeGreen : Color.DarkGreen,
                            UIConstants.BorderThicknessThin);

                        string name = card.Name.Length > 13 ? card.Name.Substring(0, 13) : card.Name;
                        screenManager.SpriteBatch.DrawString(font, name,
                            new Vector2(x + 3, startY + 3),
                            canAfford ? Color.White : Color.Gray,
                            0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

                        string costText = $"{card.SoulEssenceCost}";
                        Vector2 costSize = font.MeasureString(costText) * UIConstants.SmallTextScale;
                        screenManager.SpriteBatch.DrawString(font, costText,
                            new Vector2(x + cw - costSize.X - 2, startY + 3),
                            canAfford ? Color.Gold : Color.Red,
                            0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

                        if (!canAfford)
                        {
                            string cantText = "N/A";
                            Vector2 cantSize = font.MeasureString(cantText) * UIConstants.SmallTextScale;
                            screenManager.SpriteBatch.DrawString(font, cantText,
                                new Vector2(x + (cw - cantSize.X) / 2f, startY + ch / 2f - cantSize.Y / 2f),
                                Color.Gray, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                        }
                    }
                }

                // Pass button
                MouseState ms = Mouse.GetState();
                bool passHover = contestPassButton.Contains(new Point(ms.X, ms.Y));
                Color passColor = passHover ? Color.Crimson * 0.9f : new Color(180, 20, 20) * 0.8f;
                screenManager.SpriteBatch.Draw(screenManager.BlankTexture, contestPassButton, passColor);
                DrawCardBorder(contestPassButton, Color.White, UIConstants.BorderThicknessMedium);

                string passText = "PASS (let terrain land)";
                Vector2 passTextSize = font.MeasureString(passText) * UIConstants.RegularTextScale;
                screenManager.SpriteBatch.DrawString(font, passText,
                    new Vector2(
                        contestPassButton.X + (contestPassButton.Width  - passTextSize.X) / 2f,
                        contestPassButton.Y + (contestPassButton.Height - passTextSize.Y) / 2f),
                    Color.White, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            }

            screenManager.SpriteBatch.End();
        }

        /// <summary>
        /// Draw player info and game state
        /// </summary>
        private void DrawGameInfo(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin();
            
            int screenWidth = screenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = screenManager.GraphicsDevice.Viewport.Height;
            
            // Top bar with SE and turn info
            int topBarHeight = UIConstants.GetTopBarHeight(screenHeight);
            int padding = UIConstants.GetPadding(screenWidth);
            
            Rectangle topBar = new Rectangle(0, 0, screenWidth, topBarHeight);
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, topBar, Color.Black * 0.8f);
            
            // Enemy SE (left)
            string enemySE = $"Enemy SE: {game.Opponent.SEManager.CurrentSE:N0}";
            Vector2 enemySEPos = new Vector2(padding, padding);
            screenManager.SpriteBatch.DrawString(font, enemySE, enemySEPos, Color.Red, 
                0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            
            // Turn indicator (center)
            string turnText = $"Turn: {game.CurrentPlayer.Name}";
            Vector2 turnSize = font.MeasureString(turnText) * UIConstants.RegularTextScale;
            Vector2 turnPos = new Vector2(
                (screenWidth - turnSize.X) / 2, padding);
            Color turnColor = game.CurrentPlayer == game.Player ? Color.Yellow : Color.Orange;
            screenManager.SpriteBatch.DrawString(font, turnText, turnPos, turnColor,
                0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

            // Phase indicator (below turn)
            string phaseText = $"Phase: {game.CurrentPhase}";
            Vector2 phaseSize = font.MeasureString(phaseText) * UIConstants.RegularTextScale;
            Vector2 phasePos = new Vector2(
                (screenWidth - phaseSize.X) / 2, padding + (int)(turnSize.Y * UIConstants.RegularTextScale) + 2);
            screenManager.SpriteBatch.DrawString(font, phaseText, phasePos, Color.LightCyan,
                0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            
            // Your SE (right)
            string yourSE = $"Your SE: {game.Player.SEManager.CurrentSE:N0}";
            Vector2 yourSESize = font.MeasureString(yourSE) * UIConstants.RegularTextScale;
            Vector2 yourSEPos = new Vector2(
                screenWidth - yourSESize.X - padding, padding);
            screenManager.SpriteBatch.DrawString(font, yourSE, yourSEPos, Color.Lime,
                0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            
            screenManager.SpriteBatch.End();
        }
        
        /// <summary>
        /// Draw all zones with their terrain and characters in a clean layout
        /// </summary>
        private void DrawZonesWithCards(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin();
            
            int screenHeight = screenManager.GraphicsDevice.Viewport.Height;
            int screenWidth = screenManager.GraphicsDevice.Viewport.Width;
            
            // Calculate proportional dimensions
            int handAreaHeight = UIConstants.GetHandAreaHeight(screenHeight);
            int topBarHeight = UIConstants.GetTopBarHeight(screenHeight);
            int playAreaHeight = screenHeight - handAreaHeight - topBarHeight;
            int zoneHeight = playAreaHeight / 4;
            int padding = UIConstants.GetPadding(screenWidth);
            
            var zones = new[] 
            { 
                game.Field.OpponentBase, 
                game.Field.OpponentBattlefield, 
                game.Field.PlayerBattlefield, 
                game.Field.PlayerHomeBase 
            };
            
            string[] zoneNames = { "ENEMY BASE", "ENEMY BATTLEFIELD", "YOUR BATTLEFIELD", "YOUR HOME BASE" };
            Color[] zoneColors = { 
                new Color(139, 0, 0, 200),      // Dark red
                new Color(128, 0, 0, 200),      // Darker red
                new Color(75, 0, 130, 200),     // Indigo (purple)
                new Color(25, 25, 112, 200)     // Midnight blue
            };
            
            for (int i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                int yPos = topBarHeight + (i * zoneHeight);
                
                // Zone background overlay
                Rectangle zoneBg = new Rectangle(0, yPos, screenWidth, zoneHeight);
                screenManager.SpriteBatch.Draw(screenManager.BlankTexture, zoneBg, zoneColors[i]);
                
                // Zone label
                Vector2 labelPos = new Vector2(padding, yPos + padding / 2);
                screenManager.SpriteBatch.DrawString(font, zoneNames[i], labelPos, Color.White,
                    0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
                
                // Terrain indicator (if present)
                if (zone.ActiveTerrain != null)
                {
                    string terrainText = $"[{zone.ActiveTerrain.Name}]";
                    Vector2 terrainPos = new Vector2(padding, yPos + padding * 1.5f);
                    screenManager.SpriteBatch.DrawString(font, terrainText, terrainPos, Color.LightGreen,
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    
                    // Show terrain effect for this zone type
                    string effectText = "";
                    if (zone.Type == ZoneType.HomeBase || zone.Type == ZoneType.EnemyBase)
                    {
                        if (zone.ActiveTerrain.RegenBonus > 0)
                            effectText = $"+{zone.ActiveTerrain.RegenBonus} Regen";
                    }
                    else if (zone.Type == ZoneType.Battlefield || zone.Type == ZoneType.EnemyBattlefield)
                    {
                        if (zone.ActiveTerrain.SEBonus > 0)
                            effectText = $"+{zone.ActiveTerrain.SEBonus} SE";
                        else if (zone.ActiveTerrain.AttackBonus > 0)
                            effectText = $"+{zone.ActiveTerrain.AttackBonus} ATK";
                    }
                    
                    if (!string.IsNullOrEmpty(effectText))
                    {
                        Vector2 effectPos = new Vector2(padding, yPos + padding * 2.5f);
                        screenManager.SpriteBatch.DrawString(font, effectText, effectPos, Color.Yellow,
                            0f, Vector2.Zero, UIConstants.TinyTextScale, SpriteEffects.None, 0f);
                    }
                }
                
                // Draw character cards in this zone
                int cardWidth = UIConstants.GetCardWidth(screenWidth);
                int cardHeight = UIConstants.GetCardHeight(screenHeight);
                int cardSpacing = UIConstants.GetCardSpacing(screenWidth);
                int cardX = (int)(screenWidth * 0.16f); // 16% from left
                int cardY = yPos + padding;
                int maxCardHeight = zoneHeight - (2 * padding);
                int actualCardHeight = Math.Min(cardHeight, maxCardHeight);
                
                for (int j = 0; j < zone.Characters.Count; j++)
                {
                    var character = zone.Characters[j];
                    int x = cardX + (j * (cardWidth + cardSpacing));
                    
                    Rectangle cardRect = new Rectangle(x, cardY, cardWidth, actualCardHeight);
                    
                    // Check if this character is selected
                    bool isSelected = (selectedCharacterOnField == character);
                    
                    // Card background
                    Color cardColor = zone.Owner == PlayerSide.Player ? 
                        new Color(30, 144, 255, 220) :  // Dodger blue for player
                        new Color(220, 20, 60, 220);    // Crimson for enemy
                    
                    screenManager.SpriteBatch.Draw(screenManager.BlankTexture, cardRect, cardColor);
                    
                    // Draw border - gold and thicker if selected
                    DrawCardBorder(cardRect, isSelected ? Color.Gold : Color.White, 
                        isSelected ? UIConstants.BorderThicknessThick : UIConstants.BorderThicknessThin);
                    
                    // Card name - smaller
                    string name = character.Name.Length > 12 ? character.Name.Substring(0, 12) : character.Name;
                    Vector2 namePos = new Vector2(x + 3, cardY + 3);
                    screenManager.SpriteBatch.DrawString(font, name, namePos, Color.White, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    
                    // Item indicator if equipped
                    if (character.EquippedItem != null)
                    {
                        string itemIndicator = $"[{character.EquippedItem.Name}]";
                        Vector2 itemSize = font.MeasureString(itemIndicator) * UIConstants.TinyTextScale;
                        Vector2 itemPos = new Vector2(x + 3, cardY + 18);
                        screenManager.SpriteBatch.DrawString(font, itemIndicator, itemPos, Color.Gold, 
                            0f, Vector2.Zero, UIConstants.TinyTextScale, SpriteEffects.None, 0f);
                    }
                    
                    // SE - smaller text
                    string seText = $"SE:{character.CurrentSoulEssence}";
                    Vector2 sePos = new Vector2(x + 3, cardY + actualCardHeight - 30);
                    screenManager.SpriteBatch.DrawString(font, seText, sePos, Color.Cyan, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    
                    // ATK - show with terrain bonus if present
                    int baseAttack = character.AttackPower;
                    int terrainBonus = zone.ActiveTerrain?.AttackBonus ?? 0;
                    string atkText = terrainBonus > 0 ? 
                        $"ATK:{baseAttack}+{terrainBonus}" : 
                        $"ATK:{baseAttack}";
                    Vector2 atkPos = new Vector2(x + 3, cardY + actualCardHeight - 17);
                    Color atkColor = terrainBonus > 0 ? Color.Orange : Color.Red;
                    screenManager.SpriteBatch.DrawString(font, atkText, atkPos, atkColor, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

                    // Action status indicator (top-right corner of card)
                    if (character.ActionThisTurn == CharacterAction.Defend)
                    {
                        string defText = "DEF";
                        Vector2 defSize = font.MeasureString(defText) * UIConstants.SmallTextScale;
                        screenManager.SpriteBatch.DrawString(font, defText,
                            new Vector2(x + cardWidth - defSize.X - 2, cardY + 3),
                            Color.Orange, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    }
                    else if (character.ActionThisTurn != CharacterAction.None)
                    {
                        string actText = "ACT";
                        Vector2 actSize = font.MeasureString(actText) * UIConstants.SmallTextScale;
                        screenManager.SpriteBatch.DrawString(font, actText,
                            new Vector2(x + cardWidth - actSize.X - 2, cardY + 3),
                            Color.Gray, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    }
                }
            }
            
            screenManager.SpriteBatch.End();
        }
        
        /// <summary>
        /// Draw a centered message
        /// </summary>
        private void DrawCenteredMessage(string message, Color color)
        {
            Vector2 messageSize = font.MeasureString(message);
            Vector2 messagePos = new Vector2(
                (screenManager.SafeArea.Width - messageSize.X) / 2,
                (screenManager.SafeArea.Height - messageSize.Y) / 2
            );
            
            // Background
            Rectangle messageBG = new Rectangle(
                (int)messagePos.X - 20,
                (int)messagePos.Y - 20,
                (int)messageSize.X + 40,
                (int)messageSize.Y + 40
            );
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, messageBG, Color.Black * 0.9f);
            
            // Text with shadow
            screenManager.SpriteBatch.DrawString(font, message, 
                new Vector2(messagePos.X + 2, messagePos.Y + 2), Color.Black);
            screenManager.SpriteBatch.DrawString(font, message, messagePos, color);
        }
        
        /// <summary>
        /// Draw player's hand at bottom of screen
        /// </summary>
        private void DrawPlayerHand(GameTime gameTime)
        {
            if (game.State != WarlordsGameState.Playing) return;
            
            screenManager.SpriteBatch.Begin();
            
            int screenHeight = screenManager.GraphicsDevice.Viewport.Height;
            int screenWidth = screenManager.GraphicsDevice.Viewport.Width;
            
            // Hand area at bottom - using proportional sizing
            int handAreaHeight = UIConstants.GetHandAreaHeight(screenHeight);
            int handAreaY = screenHeight - handAreaHeight;
            int padding = UIConstants.GetPadding(screenWidth);
            
            // Draw hand area background
            Rectangle handBg = new Rectangle(0, handAreaY, screenWidth, handAreaHeight);
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, handBg, Color.Black * 0.85f);
            
            // Hand label
            string handLabel = $"YOUR HAND ({game.Player.Hand.Count} cards | Deck: {game.Player.Deck.Count})";
            Vector2 labelPos = new Vector2(padding, handAreaY + padding / 2);
            screenManager.SpriteBatch.DrawString(font, handLabel, labelPos, Color.White,
                0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            
            // Calculate proportional card dimensions
            int cardWidth = UIConstants.GetCardWidth(screenWidth);
            int cardHeight = (int)(cardWidth * 1.2f); // Maintain aspect ratio
            int cardSpacing = UIConstants.GetCardSpacing(screenWidth);
            int startX = padding;
            int startY = handAreaY + (int)(padding * 2.5f);
            
            for (int i = 0; i < game.Player.Hand.Count && i < handCardRects.Length; i++)
            {
                var card = game.Player.Hand[i];
                
                // Calculate card position
                int x = startX + (i * (cardWidth + cardSpacing));
                int y = startY;
                
                // Store rectangle for click detection
                handCardRects[i] = new Rectangle(x, y, cardWidth, cardHeight);
                
                // Determine card color and info based on type
                bool isSelected = (selectedCard == card);
                Color cardColor;
                Color borderColor = isSelected ? Color.Gold : Color.White;
                
                if (card is CharacterCard charCard)
                {
                    // Check if player can afford this card
                    bool canAfford = game.Player.SEManager.CurrentSE >= card.SoulEssenceCost;
                    
                    // Character card - dark gray, dimmed if unaffordable
                    cardColor = isSelected ? Color.Yellow * 0.9f : 
                                canAfford ? Color.DarkGray * 0.8f : Color.DarkGray * 0.4f;
                    
                    // Draw card background
                    screenManager.SpriteBatch.Draw(screenManager.BlankTexture, handCardRects[i], cardColor);
                    DrawCardBorder(handCardRects[i], borderColor, isSelected ? UIConstants.BorderThicknessThick : UIConstants.BorderThicknessThin);
                    
                    // Draw card info with smaller text
                    string cardName = charCard.Name.Length > 14 ? charCard.Name.Substring(0, 14) : charCard.Name;
                    string seText = $"SE:{charCard.CurrentSoulEssence}";
                    string atkText = $"ATK:{charCard.AttackPower}";
                    
                    Vector2 namePos = new Vector2(x + 3, y + 3);
                    Vector2 sePos = new Vector2(x + 3, y + 45);
                    Vector2 atkPos = new Vector2(x + 3, y + 65);
                    
                    // Scale down text
                    Color textColor = canAfford ? Color.White : Color.Gray;
                    screenManager.SpriteBatch.DrawString(font, cardName, namePos, textColor, 
                        0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, seText, sePos, canAfford ? Color.Cyan : Color.DarkCyan, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, atkText, atkPos, canAfford ? Color.Red : Color.DarkRed, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    
                    // Draw cost in top-right corner
                    string costText = $"{card.SoulEssenceCost}";
                    Vector2 costSize = font.MeasureString(costText) * UIConstants.SmallTextScale;
                    Vector2 costPos = new Vector2(x + cardWidth - costSize.X - 3, y + 3);
                    Color costColor = canAfford ? Color.Gold : Color.Red;
                    screenManager.SpriteBatch.DrawString(font, costText, costPos, costColor, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                }
                else if (card is TerrainCard terrainCard)
                {
                    // Check if player can afford this card
                    bool canAfford = game.Player.SEManager.CurrentSE >= card.SoulEssenceCost;
                    
                    // Terrain card - green, dimmed if unaffordable
                    cardColor = isSelected ? Color.LightGreen * 0.9f : 
                                canAfford ? Color.DarkGreen * 0.8f : Color.DarkGreen * 0.4f;
                    
                    // Draw card background
                    screenManager.SpriteBatch.Draw(screenManager.BlankTexture, handCardRects[i], cardColor);
                    DrawCardBorder(handCardRects[i], borderColor, isSelected ? UIConstants.BorderThicknessThick : UIConstants.BorderThicknessThin);
                    
                    // Draw card info with smaller text
                    string cardName = terrainCard.Name.Length > 14 ? terrainCard.Name.Substring(0, 14) : terrainCard.Name;
                    string typeText = "TERRAIN";
                    string effectText = "Click zone";
                    
                    Vector2 namePos = new Vector2(x + 3, y + 3);
                    Vector2 typePos = new Vector2(x + 3, y + 45);
                    Vector2 effectPos = new Vector2(x + 3, y + 65);
                    
                    Color textColor = canAfford ? Color.White : Color.Gray;
                    screenManager.SpriteBatch.DrawString(font, cardName, namePos, textColor, 
                        0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, typeText, typePos, canAfford ? Color.LightGreen : Color.DarkGreen, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, effectText, effectPos, Color.Gray, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    
                    // Draw cost in top-right corner
                    string costText = $"{card.SoulEssenceCost}";
                    Vector2 costSize = font.MeasureString(costText) * UIConstants.SmallTextScale;
                    Vector2 costPos = new Vector2(x + cardWidth - costSize.X - 3, y + 3);
                    Color costColor = canAfford ? Color.Gold : Color.Red;
                    screenManager.SpriteBatch.DrawString(font, costText, costPos, costColor, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                }
                else if (card is ItemCard itemCard)
                {
                    // Check if player can afford this card
                    bool canAfford = game.Player.SEManager.CurrentSE >= card.SoulEssenceCost;
                    
                    // Item card - orange/gold, dimmed if unaffordable
                    cardColor = isSelected ? Color.Orange * 0.9f : 
                                canAfford ? new Color(184, 134, 11) * 0.8f : new Color(184, 134, 11) * 0.4f;
                    
                    // Draw card background
                    screenManager.SpriteBatch.Draw(screenManager.BlankTexture, handCardRects[i], cardColor);
                    DrawCardBorder(handCardRects[i], borderColor, isSelected ? UIConstants.BorderThicknessThick : UIConstants.BorderThicknessThin);
                    
                    // Draw card info
                    string cardName = itemCard.Name.Length > 14 ? itemCard.Name.Substring(0, 14) : itemCard.Name;
                    string typeText = "ITEM";
                    string equipText = "Click char";
                    
                    Vector2 namePos = new Vector2(x + 3, y + 3);
                    Vector2 typePos = new Vector2(x + 3, y + 45);
                    Vector2 equipPos = new Vector2(x + 3, y + 65);
                    
                    Color textColor = canAfford ? Color.White : Color.Gray;
                    screenManager.SpriteBatch.DrawString(font, cardName, namePos, textColor, 
                        0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, typeText, typePos, canAfford ? Color.Gold : new Color(139, 101, 8), 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, equipText, equipPos, new Color(255, 218, 185), 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    
                    // Draw cost in top-right corner
                    string costText = $"{card.SoulEssenceCost}";
                    Vector2 costSize = font.MeasureString(costText) * UIConstants.SmallTextScale;
                    Vector2 costPos = new Vector2(x + cardWidth - costSize.X - 3, y + 3);
                    Color costColor = canAfford ? Color.Gold : Color.Red;
                    screenManager.SpriteBatch.DrawString(font, costText, costPos, costColor, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                }
                else if (card is EventCard eventCard)
                {
                    // Check if player can afford this card
                    bool canAfford = game.Player.SEManager.CurrentSE >= card.SoulEssenceCost;
                    
                    // Event card - purple/magenta, dimmed if unaffordable
                    cardColor = isSelected ? Color.Magenta * 0.9f : 
                                canAfford ? new Color(138, 43, 226) * 0.8f : new Color(138, 43, 226) * 0.4f;
                    
                    // Draw card background
                    screenManager.SpriteBatch.Draw(screenManager.BlankTexture, handCardRects[i], cardColor);
                    DrawCardBorder(handCardRects[i], borderColor, isSelected ? UIConstants.BorderThicknessThick : UIConstants.BorderThicknessThin);
                    
                    // Draw card info
                    string cardName = eventCard.Name.Length > 14 ? eventCard.Name.Substring(0, 14) : eventCard.Name;
                    string typeText = "EVENT";
                    string effectText = "Click to use";
                    
                    Vector2 namePos = new Vector2(x + 3, y + 3);
                    Vector2 typePos = new Vector2(x + 3, y + 45);
                    Vector2 effectPos = new Vector2(x + 3, y + 65);
                    
                    Color textColor = canAfford ? Color.White : Color.Gray;
                    screenManager.SpriteBatch.DrawString(font, cardName, namePos, textColor, 
                        0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, typeText, typePos, canAfford ? new Color(148, 0, 211) : new Color(98, 0, 141), 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    screenManager.SpriteBatch.DrawString(font, effectText, effectPos, new Color(221, 160, 221), 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                    
                    // Draw cost in top-right corner
                    string costText = $"{card.SoulEssenceCost}";
                    Vector2 costSize = font.MeasureString(costText) * UIConstants.SmallTextScale;
                    Vector2 costPos = new Vector2(x + cardWidth - costSize.X - 3, y + 3);
                    Color costColor = canAfford ? Color.Gold : Color.Red;
                    screenManager.SpriteBatch.DrawString(font, costText, costPos, costColor, 
                        0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);
                }
            }
            
            // Draw instructions if waiting for player
            if (game.WaitingForPlayer)
            {
                string instruction;
                if (selectedCharacterOnField != null)
                    instruction = $"{selectedCharacterOnField.Name}: Click zones to move, enemy to attack, R-click to defend";
                else if (selectedCard == null)
                    instruction = "Use DRAW/ADVANCE/SACRIFICE buttons or click a card to select";
                else if (selectedCard is ItemCard)
                    instruction = "Click a character to equip";
                else if (selectedCard is EventCard)
                    instruction = "Event will play instantly";
                else
                    instruction = $"Click a zone to play {selectedCard.Name}";
                    
                Vector2 instructionSize = font.MeasureString(instruction) * UIConstants.RegularTextScale;
                Vector2 instructionPos = new Vector2(screenWidth - instructionSize.X - padding, handAreaY + padding / 2);
                screenManager.SpriteBatch.DrawString(font, instruction, instructionPos, Color.Yellow,
                    0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            }
            
            screenManager.SpriteBatch.End();
        }
        
        /// <summary>
        /// Draw the End Turn button
        /// </summary>
        private void DrawEndTurnButton(GameTime gameTime)
        {
            if (game.State != WarlordsGameState.Playing) return;
            
            screenManager.SpriteBatch.Begin();
            
            // Only show controls when it's player's turn
            if (game.WaitingForPlayer)
            {
                MouseState mouseState = Mouse.GetState();
                Point mousePos = new Point(mouseState.X, mouseState.Y);

                DrawControlButton(drawCardButton, mousePos, "DRAW", game.CurrentPhase == TurnPhase.Draw ? Color.SteelBlue : Color.Gray);
                DrawControlButton(advancePhaseButton, mousePos, "ADVANCE", Color.CadetBlue);
                DrawControlButton(sacrificeButton, mousePos, "SACRIFICE", Color.IndianRed);
                DrawControlButton(endTurnButton, mousePos, "END TURN", Color.Green);
            }
            else if (game.CurrentPlayer == game.Opponent)
            {
                // Show "AI Thinking..." message
                string aiText = "AI Turn...";
                Vector2 textSize = font.MeasureString(aiText) * UIConstants.RegularTextScale;
                Vector2 textPos = new Vector2(
                    endTurnButton.X + (endTurnButton.Width - textSize.X) / 2,
                    endTurnButton.Y + (endTurnButton.Height - textSize.Y) / 2
                );
                screenManager.SpriteBatch.DrawString(font, aiText, textPos, Color.Orange,
                    0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
            }
            
            screenManager.SpriteBatch.End();
        }

        private void DrawControlButton(Rectangle rect, Point mousePos, string text, Color baseColor)
        {
            bool hovering = rect.Contains(mousePos);
            Color buttonColor = hovering ? baseColor * 0.9f : baseColor * 0.7f;
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, rect, buttonColor);
            DrawCardBorder(rect, Color.White, UIConstants.BorderThicknessMedium);

            Vector2 textSize = font.MeasureString(text) * UIConstants.RegularTextScale;
            Vector2 textPos = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2,
                rect.Y + (rect.Height - textSize.Y) / 2
            );
            screenManager.SpriteBatch.DrawString(font, text, textPos, Color.White,
                0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);
        }
        
        /// <summary>
        /// Draw feedback messages (like insufficient SE warnings)
        /// </summary>
        private void DrawFeedbackMessage(GameTime gameTime)
        {
            if (feedbackTimer <= 0f || string.IsNullOrEmpty(feedbackMessage))
                return;
            
            screenManager.SpriteBatch.Begin();
            
            // Calculate fade based on remaining time
            float alpha = Math.Min(1.0f, feedbackTimer / 0.5f); // Fade out in last 0.5 seconds
            
            // Draw message in center of screen with background
            Vector2 messageSize = font.MeasureString(feedbackMessage) * UIConstants.TitleTextScale;
            int messageWidth = (int)messageSize.X + UIConstants.GetPadding(screenManager.GraphicsDevice.Viewport.Width) * 2;
            int messageHeight = (int)messageSize.Y + UIConstants.GetPadding(screenManager.GraphicsDevice.Viewport.Height) * 2;
            int screenWidth = screenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = screenManager.GraphicsDevice.Viewport.Height;
            
            Rectangle messageBox = new Rectangle(
                (screenWidth - messageWidth) / 2,
                (screenHeight - messageHeight) / 2,
                messageWidth,
                messageHeight
            );
            
            // Draw semi-transparent background
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, messageBox, Color.Black * (0.8f * alpha));
            
            // Draw red border
            DrawCardBorder(messageBox, Color.Red * alpha, UIConstants.BorderThicknessMedium);
            
            // Draw message text
            Vector2 textPos = new Vector2(
                messageBox.X + (messageBox.Width - messageSize.X) / 2,
                messageBox.Y + (messageBox.Height - messageSize.Y) / 2
            );
            screenManager.SpriteBatch.DrawString(font, feedbackMessage, textPos, Color.Red * alpha,
                0f, Vector2.Zero, UIConstants.TitleTextScale, SpriteEffects.None, 0f);
            
            screenManager.SpriteBatch.End();
        }

        private void DrawModalDialog(GameTime gameTime, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            screenManager.SpriteBatch.Begin();

            int sw = screenManager.GraphicsDevice.Viewport.Width;
            int sh = screenManager.GraphicsDevice.Viewport.Height;
            int pad = UIConstants.GetPadding(sw);

            // Backdrop
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle(0, 0, sw, sh), Color.Black * 0.65f);

            // Dialog panel
            int panelW = Math.Min(sw - (pad * 8), 900);
            int panelH = Math.Min(sh - (pad * 8), 280);
            Rectangle panel = new Rectangle((sw - panelW) / 2, (sh - panelH) / 2, panelW, panelH);
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, panel, new Color(24, 24, 40));
            DrawCardBorder(panel, Color.White, UIConstants.BorderThicknessMedium);

            string title = "TERRAIN COUNTERED";
            Vector2 titleSize = font.MeasureString(title) * UIConstants.RegularTextScale;
            screenManager.SpriteBatch.DrawString(font, title,
                new Vector2(panel.X + (panel.Width - titleSize.X) / 2f, panel.Y + pad),
                Color.Orange, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

            Vector2 msgPos = new Vector2(panel.X + pad, panel.Y + pad * 3);
            screenManager.SpriteBatch.DrawString(font, message, msgPos,
                Color.White, 0f, Vector2.Zero, UIConstants.SmallTextScale, SpriteEffects.None, 0f);

            // Reposition close button under the panel for current resolution
            dialogCloseButton = new Rectangle(
                panel.X + (panel.Width / 2) - (dialogCloseButton.Width / 2),
                panel.Bottom - dialogCloseButton.Height - pad,
                dialogCloseButton.Width,
                dialogCloseButton.Height);

            MouseState ms = Mouse.GetState();
            bool hover = dialogCloseButton.Contains(new Point(ms.X, ms.Y));
            Color closeColor = hover ? Color.Cyan * 0.9f : Color.SteelBlue * 0.85f;
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, dialogCloseButton, closeColor);
            DrawCardBorder(dialogCloseButton, Color.White, UIConstants.BorderThicknessMedium);

            string closeText = "CLOSE";
            Vector2 closeSize = font.MeasureString(closeText) * UIConstants.RegularTextScale;
            screenManager.SpriteBatch.DrawString(font, closeText,
                new Vector2(
                    dialogCloseButton.X + (dialogCloseButton.Width - closeSize.X) / 2f,
                    dialogCloseButton.Y + (dialogCloseButton.Height - closeSize.Y) / 2f),
                Color.White, 0f, Vector2.Zero, UIConstants.RegularTextScale, SpriteEffects.None, 0f);

            screenManager.SpriteBatch.End();
        }
        
        /// <summary>
        /// Draw a rectangle border
        /// </summary>
        private void DrawCardBorder(Rectangle rect, Color color, int thickness)
        {
            // Top
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, 
                new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, 
                new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, 
                new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, 
                new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
        
        /// <summary>
        /// Draw game over screen when someone wins
        /// </summary>
        private void DrawGameOverScreen(GameTime gameTime)
        {
            if (game.State != WarlordsGameState.PlayerWins && 
                game.State != WarlordsGameState.OpponentWins)
            {
                return;
            }
            
            screenManager.SpriteBatch.Begin();
            
            // Semi-transparent overlay
            Rectangle fullScreen = new Rectangle(0, 0, 
                screenManager.GraphicsDevice.Viewport.Width, 
                screenManager.GraphicsDevice.Viewport.Height);
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture, fullScreen, Color.Black * 0.8f);
            
            // Determine winner text and color
            string winnerText = game.State == WarlordsGameState.PlayerWins ? "YOU WIN!" : "YOU LOSE!";
            Color winnerColor = game.State == WarlordsGameState.PlayerWins ? Color.Gold : Color.Red;
            
            // Draw winner text (large)
            Vector2 textSize = font.MeasureString(winnerText);
            Vector2 textPos = new Vector2(
                (screenManager.GraphicsDevice.Viewport.Width - textSize.X) / 2,
                (screenManager.GraphicsDevice.Viewport.Height / 2) - 50
            );
            
            // Draw with shadow
            screenManager.SpriteBatch.DrawString(font, winnerText, textPos + new Vector2(3, 3), Color.Black);
            screenManager.SpriteBatch.DrawString(font, winnerText, textPos, winnerColor);
            
            // Draw instructions
            string instruction = "Press ESC for menu";
            Vector2 instructionSize = font.MeasureString(instruction);
            Vector2 instructionPos = new Vector2(
                (screenManager.GraphicsDevice.Viewport.Width - instructionSize.X) / 2,
                textPos.Y + textSize.Y + 30
            );
            screenManager.SpriteBatch.DrawString(font, instruction, instructionPos, Color.White);
            
            screenManager.SpriteBatch.End();
        }
    }
}
