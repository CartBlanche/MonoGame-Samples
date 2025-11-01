//-----------------------------------------------------------------------------
// WarlordsCardGame.cs
//
// Main game logic for Warlords
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using CardsFramework;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WarlordsFramework;

namespace Warlords
{
    /// <summary>
    /// Main game logic for Warlords card game
    /// </summary>
    public class WarlordsCardGame : CardsGame
    {
        public PlayingField Field { get; private set; }
        public WarlordsPlayer Player { get; private set; }
        public WarlordsPlayer Opponent { get; private set; }
        public WarlordsPlayer CurrentPlayer { get; private set; }
        
        // Card tracking
        public List<WarlordsCard> TheVoid { get; private set; }
        public List<WarlordsCard> TheNexus { get; private set; }
        
        private ScreenManager screenManager;
        public WarlordsGameState State { get; set; }
        
        // AI delay for visibility
        private TimeSpan aiDelay = TimeSpan.FromSeconds(2);
        private DateTime nextAIAction = DateTime.Now;
        private int aiActionsThisTurn = 0;
        private const int MaxAIActionsPerTurn = 3;
        
        public bool WaitingForPlayer { get; set; }
        
        public WarlordsCardGame(Rectangle tableBounds, ScreenManager screenManager, string theme)
            : base(
                decks: 0,
                jokersInDeck: 0,
                suits: CardSuit.Club,
                cardValues: CardsFramework.CardValue.Ace,
                minimumPlayers: 2,
                maximumPlayers: 2,
                gameTable: new WarlordsTable(tableBounds, theme, screenManager.Game, 
                                           screenManager.SpriteBatch, screenManager.GlobalTransformation),
                theme: theme,
                game: screenManager.Game)
        {
            this.screenManager = screenManager;
            Field = new PlayingField();
            TheVoid = new List<WarlordsCard>();
            TheNexus = new List<WarlordsCard>();
            State = WarlordsGameState.Setup;
        }
        
        /// <summary>
        /// Initialize the game
        /// </summary>
        public void Initialize()
        {
            // Skip base.LoadContent() for prototype - we don't need card graphics yet
            // Just load the font we need
            Font = screenManager.Game.Content.Load<SpriteFont>("Fonts/Regular");
            
            // Initialize the game table
            GameTable.Initialize();
            
            // Create players
            Player = new WarlordsPlayer("Player", this);
            Opponent = new WarlordsPlayer("AI", this);
            
            CurrentPlayer = Player;
            
            // Create test decks (minimal prototype - just character cards)
            CreateTestDeck(Player);
            CreateTestDeck(Opponent);
            
            // Deal opening hands
            StartGame();
            
            State = WarlordsGameState.Playing;
        }
        
        /// <summary>
        /// Create a simple test deck for prototype
        /// </summary>
        private void CreateTestDeck(WarlordsPlayer player)
        {
            // Create 2 terrain cards
            for (int i = 0; i < 2; i++)
            {
                var terrain = new TerrainCard
                {
                    Name = i == 0 ? "Sacred Ground" : "Dark Wasteland",
                    LoreDescription = i == 0 ? "Holy land that heals allies" : "Cursed land that drains life",
                    HomeBaseEffectDescription = i == 0 ? "+500 SE regen per turn" : "+1000 SE degen reduction",
                    BattlefieldEffectDescription = i == 0 ? "Characters gain +500 SE" : "Characters gain +200 ATK",
                    Rarity = CardRarity.Uncommon
                };
                terrain.Tags.Add("Terrain");
                player.Deck.Add(terrain);
            }
            
            // Create 8 character cards with varying power
            for (int i = 0; i < 8; i++)
            {
                var card = new CharacterCard
                {
                    Name = $"Warrior {i + 1}",
                    MaxSoulEssence = 5000 + (i * 1000),
                    CurrentSoulEssence = 5000 + (i * 1000),
                    AttackPower = 1000 + (i * 100),
                    Classification = Classification.Human,
                    Rarity = i > 6 ? CardRarity.Rare : CardRarity.Common
                };
                card.Tags.Add("Character");
                player.Deck.Add(card);
            }
            
            // Shuffle deck
            player.Deck = player.Deck.OrderBy(x => Guid.NewGuid()).ToList();
        }
        
        /// <summary>
        /// Start the game - deal opening hands
        /// </summary>
        public void StartGame()
        {
            // Deal 5 cards to each player (simplified from 7 for prototype)
            for (int i = 0; i < 5; i++)
            {
                Player.DrawCard();
                Opponent.DrawCard();
            }
        }
        
        /// <summary>
        /// Play a character card to a zone
        /// </summary>
        public void PlayCard(CharacterCard card, GameZone zone)
        {
            if (CurrentPlayer.Hand.Contains(card))
            {
                CurrentPlayer.Hand.Remove(card);
                zone.AddCharacter(card);
            }
        }
        
        /// <summary>
        /// Play a terrain card to a zone
        /// </summary>
        public void PlayTerrain(TerrainCard terrain, GameZone zone)
        {
            if (CurrentPlayer.Hand.Contains(terrain))
            {
                CurrentPlayer.Hand.Remove(terrain);
                zone.ActiveTerrain = terrain;
            }
        }
        
        /// <summary>
        /// Play any card (routes to appropriate handler)
        /// </summary>
        public void PlayCardGeneric(WarlordsCard card, GameZone zone)
        {
            if (card is CharacterCard charCard)
            {
                PlayCard(charCard, zone);
            }
            else if (card is TerrainCard terrainCard)
            {
                PlayTerrain(terrainCard, zone);
            }
        }
        
        /// <summary>
        /// Move a character from one zone to another
        /// </summary>
        public void MoveCharacter(CharacterCard card, GameZone fromZone, GameZone toZone)
        {
            if (!Field.CanAdvance(card, fromZone, toZone)) return;
            
            fromZone.RemoveCharacter(card);
            toZone.AddCharacter(card);
            card.HasActedThisTurn = true;
        }
        
        /// <summary>
        /// Attack another character
        /// </summary>
        public void Attack(CharacterCard attacker, CharacterCard target)
        {
            attacker.Attack(target);
            
            // If target defeated, move to Nexus
            if (target.IsDefeated)
            {
                var zone = Field.GetZoneContaining(target);
                zone?.RemoveCharacter(target);
                MoveToNexus(target);
            }
        }
        
        /// <summary>
        /// Attack the opposing player directly
        /// </summary>
        public void AttackPlayer(CharacterCard attacker, WarlordsPlayer target)
        {
            if (attacker.HasActedThisTurn) return;
            
            target.SEManager.TakeDamage(attacker.AttackPower);
            attacker.HasActedThisTurn = true;
            
            CheckWinCondition();
        }
        
        /// <summary>
        /// End current turn
        /// </summary>
        public void EndTurn()
        {
            // Reset all character actions
            foreach (var zone in Field.GetAllZones())
            {
                foreach (var character in zone.Characters)
                {
                    character.ResetTurnState();
                }
            }
            
            // Apply regen
            CurrentPlayer.SEManager.ApplyRegen();
            
            // Reset turn flags
            CurrentPlayer.ResetTurnFlags();
            
            // Switch player
            CurrentPlayer = (CurrentPlayer == Player) ? Opponent : Player;
            
            // Draw card at start of turn (if deck has cards)
            if (CurrentPlayer.Deck.Count > 0)
            {
                CurrentPlayer.DrawCard();
            }
            
            // Check win condition
            CheckWinCondition();
        }
        
        /// <summary>
        /// Check if someone has won
        /// </summary>
        public void CheckWinCondition()
        {
            if (Player.SEManager.IsDefeated)
            {
                State = WarlordsGameState.OpponentWins;
            }
            else if (Opponent.SEManager.IsDefeated)
            {
                State = WarlordsGameState.PlayerWins;
            }
        }
        
        /// <summary>
        /// Move a card to The Void (permanent removal)
        /// </summary>
        public void MoveToVoid(WarlordsCard card)
        {
            TheVoid.Add(card);
        }
        
        /// <summary>
        /// Move a card to The Nexus (discard pile)
        /// </summary>
        public void MoveToNexus(WarlordsCard card)
        {
            TheNexus.Add(card);
        }
        
        /// <summary>
        /// Update game logic
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // AI turn
            if (CurrentPlayer == Opponent && State == WarlordsGameState.Playing)
            {
                // Add delay so we can see what's happening
                if (DateTime.Now >= nextAIAction)
                {
                    bool actionTaken = SimpleAI();
                    
                    if (actionTaken)
                    {
                        aiActionsThisTurn++;
                        nextAIAction = DateTime.Now + aiDelay;
                    }
                    
                    // End AI turn after max actions or if no action was taken
                    if (!actionTaken || aiActionsThisTurn >= MaxAIActionsPerTurn)
                    {
                        aiActionsThisTurn = 0;
                        PlayerEndTurn();
                    }
                }
            }
            else if (CurrentPlayer == Player && State == WarlordsGameState.Playing)
            {
                WaitingForPlayer = true;
            }
        }
        
        /// <summary>
        /// Very simple AI that plays and attacks
        /// Returns true if an action was taken, false otherwise
        /// </summary>
        private bool SimpleAI()
        {
            // Play terrain card first if we don't have one on battlefield
            var terrainCard = Opponent.Hand.OfType<TerrainCard>().FirstOrDefault();
            if (terrainCard != null && Field.OpponentBattlefield.ActiveTerrain == null)
            {
                PlayTerrain(terrainCard, Field.OpponentBattlefield);
                return true;
            }
            
            // Play first character card in hand to home base
            var characterCard = Opponent.Hand.OfType<CharacterCard>().FirstOrDefault();
            if (characterCard != null)
            {
                PlayCard(characterCard, Field.OpponentBase);
                return true;
            }
            
            // Try to move a character forward
            if (Field.OpponentBase.HasCharacters)
            {
                var charToMove = Field.OpponentBase.Characters.FirstOrDefault(c => !c.HasActedThisTurn);
                if (charToMove != null && Field.OpponentBattlefield.ActiveTerrain != null)
                {
                    MoveCharacter(charToMove, Field.OpponentBase, Field.OpponentBattlefield);
                    return true;
                }
            }
            
            // Attack if possible
            if (Field.OpponentBattlefield.HasCharacters)
            {
                var attacker = Field.OpponentBattlefield.Characters.FirstOrDefault(c => !c.HasActedThisTurn);
                if (attacker != null)
                {
                    if (Field.PlayerBattlefield.HasCharacters)
                    {
                        // Attack player's character
                        Attack(attacker, Field.PlayerBattlefield.Characters[0]);
                        return true;
                    }
                    else if (!Field.PlayerBattlefield.HasCharacters)
                    {
                        // Attack player directly
                        AttackPlayer(attacker, Player);
                        return true;
                    }
                }
            }
            
            // No action taken
            return false;
        }
        
        /// <summary>
        /// Manually end turn (called by player or after AI delay)
        /// </summary>
        public void PlayerEndTurn()
        {
            WaitingForPlayer = false;
            EndTurn();
        }
        
        /// <summary>
        /// Draw game state info
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            // Drawing is handled by WarlordsTable and GameplayScreen
        }
        
        // CardsGame abstract method implementations
        public override void AddPlayer(Player player)
        {
            // Not used in this implementation
        }
        
        public override Player GetCurrentPlayer()
        {
            return CurrentPlayer;
        }
        
        public override void Deal()
        {
            StartGame();
        }
        
        public override void StartPlaying()
        {
            State = WarlordsGameState.Playing;
        }
    }
    
    public enum WarlordsGameState
    {
        Setup,
        Playing,
        PlayerWins,
        OpponentWins
    }
}
