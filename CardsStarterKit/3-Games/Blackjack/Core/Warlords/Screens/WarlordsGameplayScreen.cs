//-----------------------------------------------------------------------------
// WarlordsGameplayScreen.cs
//
// Main gameplay screen for Warlords
//-----------------------------------------------------------------------------

using System;
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
        
        // Card selection
        private WarlordsCard selectedCard;
        private CharacterCard selectedCharacterOnField; // Track selected character from field
        private Rectangle[] handCardRects;
        private Rectangle[] zoneRects;
        
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
            
            // Create and initialize game
            game = new WarlordsCardGame(safeArea, screenManager, theme);
            game.Initialize();
            game.StartGame();
            
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
            
            previousMouseState = Mouse.GetState();
            
            base.LoadContent();
        }
        
        public override void HandleInput(InputState input)
        {
            MouseState currentMouseState = Mouse.GetState();
            
            // Only allow input during player's turn
            if (game.WaitingForPlayer && game.State == WarlordsGameState.Playing)
            {
                if (currentMouseState.LeftButton == ButtonState.Released &&
                    previousMouseState.LeftButton == ButtonState.Pressed)
                {
                    Point mousePos = new Point(currentMouseState.X, currentMouseState.Y);
                    
                    // Check for end turn button click
                    if (endTurnButton.Contains(mousePos))
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
                                    // Can't afford - show feedback
                                    int needed = clickedCard.SoulEssenceCost - game.Player.SEManager.CurrentSE;
                                    feedbackMessage = $"Not enough SE! Need {needed} more.";
                                    feedbackTimer = FeedbackDuration;
                                    selectedCard = null; // Clear selection
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
                                }
                                else
                                {
                                    // Playing from hand to zones
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
                DrawGameInfo(gameTime);
                DrawZonesWithCards(gameTime);
                DrawPlayerHand(gameTime);
                DrawEndTurnButton(gameTime);
                DrawFeedbackMessage(gameTime);
                DrawGameOverScreen(gameTime);
            }
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
                string instruction = selectedCard == null 
                    ? "Click a card to select" 
                    : selectedCard is ItemCard ? "Click a character to equip"
                    : selectedCard is EventCard ? "Event will play instantly"
                    : $"Click a zone to play {selectedCard.Name}";
                    
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
            
            // Only show button when it's player's turn
            if (game.WaitingForPlayer)
            {
                MouseState mouseState = Mouse.GetState();
                Point mousePos = new Point(mouseState.X, mouseState.Y);
                bool isHovering = endTurnButton.Contains(mousePos);
                
                // Button background
                Color buttonColor = isHovering ? Color.Yellow * 0.8f : Color.Green * 0.7f;
                screenManager.SpriteBatch.Draw(screenManager.BlankTexture, endTurnButton, buttonColor);
                
                // Button border
                DrawCardBorder(endTurnButton, Color.White, UIConstants.BorderThicknessMedium);
                
                // Button text - scaled
                string buttonText = "END TURN";
                Vector2 textSize = font.MeasureString(buttonText) * UIConstants.TitleTextScale;
                Vector2 textPos = new Vector2(
                    endTurnButton.X + (endTurnButton.Width - textSize.X) / 2,
                    endTurnButton.Y + (endTurnButton.Height - textSize.Y) / 2
                );
                screenManager.SpriteBatch.DrawString(font, buttonText, textPos, Color.Black,
                    0f, Vector2.Zero, UIConstants.TitleTextScale, SpriteEffects.None, 0f);
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
