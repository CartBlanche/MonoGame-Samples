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
        /// Create a test deck for a player with diverse cards
        /// </summary>
        private void CreateTestDeck(WarlordsPlayer player)
        {
            // ===== TERRAIN CARDS (5 different terrains) =====
            
            // Sacred Ground - High regen terrain
            var sacredGround = new TerrainCard
            {
                Name = "Sacred Ground",
                LoreDescription = "Holy land blessed by ancient spirits",
                HomeBaseEffectDescription = "+800 SE regen per turn",
                BattlefieldEffectDescription = "Characters gain +300 SE",
                SEBonus = 300,
                AttackBonus = 0,
                RegenBonus = 800,
                SoulEssenceCost = 900,
                Rarity = CardRarity.Rare
            };
            sacredGround.Tags.Add("Terrain");
            player.Deck.Add(sacredGround);
            
            // Dark Wasteland - Attack boost terrain
            var darkWasteland = new TerrainCard
            {
                Name = "Dark Wasteland",
                LoreDescription = "Cursed land that amplifies aggression",
                HomeBaseEffectDescription = "+300 SE regen per turn",
                BattlefieldEffectDescription = "Characters gain +400 ATK",
                SEBonus = 0,
                AttackBonus = 400,
                RegenBonus = 300,
                SoulEssenceCost = 800,
                Rarity = CardRarity.Uncommon
            };
            darkWasteland.Tags.Add("Terrain");
            player.Deck.Add(darkWasteland);
            
            // Mystic Forest - Balanced terrain
            var mysticForest = new TerrainCard
            {
                Name = "Mystic Forest",
                LoreDescription = "Ancient woods filled with magic",
                HomeBaseEffectDescription = "+500 SE regen per turn",
                BattlefieldEffectDescription = "Characters gain +200 SE and +200 ATK",
                SEBonus = 200,
                AttackBonus = 200,
                RegenBonus = 500,
                SoulEssenceCost = 1000,
                Rarity = CardRarity.Uncommon
            };
            mysticForest.Tags.Add("Terrain");
            player.Deck.Add(mysticForest);
            
            // Volcanic Crater - High attack terrain
            var volcanicCrater = new TerrainCard
            {
                Name = "Volcanic Crater",
                LoreDescription = "Molten earth that fuels fury",
                HomeBaseEffectDescription = "+200 SE regen per turn",
                BattlefieldEffectDescription = "Characters gain +600 ATK",
                SEBonus = 0,
                AttackBonus = 600,
                RegenBonus = 200,
                SoulEssenceCost = 1100,
                Rarity = CardRarity.Rare
            };
            volcanicCrater.Tags.Add("Terrain");
            player.Deck.Add(volcanicCrater);
            
            // Crystal Sanctuary - SE boost terrain
            var crystalSanctuary = new TerrainCard
            {
                Name = "Crystal Sanctuary",
                LoreDescription = "Shimmering crystals that store souls",
                HomeBaseEffectDescription = "+600 SE regen per turn",
                BattlefieldEffectDescription = "Characters gain +600 SE",
                SEBonus = 600,
                AttackBonus = 0,
                RegenBonus = 600,
                SoulEssenceCost = 1000,
                Rarity = CardRarity.Rare
            };
            crystalSanctuary.Tags.Add("Terrain");
            player.Deck.Add(crystalSanctuary);
            
            // ===== CHARACTER CARDS (12 diverse characters) =====
            
            // Scout - Cheap, weak
            var scout = new CharacterCard
            {
                Name = "Soul Scout",
                LoreDescription = "Swift reconnaissance unit",
                MaxSoulEssence = 3000,
                CurrentSoulEssence = 3000,
                AttackPower = 600,
                Classification = Classification.Human,
                SoulEssenceCost = 400,
                Rarity = CardRarity.Common
            };
            scout.Tags.Add("Character");
            player.Deck.Add(scout);
            
            // Soldier - Basic unit
            var soldier = new CharacterCard
            {
                Name = "Soul Soldier",
                LoreDescription = "Standard infantry warrior",
                MaxSoulEssence = 5000,
                CurrentSoulEssence = 5000,
                AttackPower = 1000,
                Classification = Classification.Human,
                SoulEssenceCost = 600,
                Rarity = CardRarity.Common
            };
            soldier.Tags.Add("Character");
            player.Deck.Add(soldier);
            
            // Knight - Mid-tier unit
            var knight = new CharacterCard
            {
                Name = "Soul Knight",
                LoreDescription = "Armored warrior of honor",
                MaxSoulEssence = 7000,
                CurrentSoulEssence = 7000,
                AttackPower = 1400,
                Classification = Classification.Human,
                SoulEssenceCost = 900,
                Rarity = CardRarity.Uncommon
            };
            knight.Tags.Add("Character");
            player.Deck.Add(knight);
            
            // Paladin - Tanky unit
            var paladin = new CharacterCard
            {
                Name = "Soul Paladin",
                LoreDescription = "Holy defender with immense vitality",
                MaxSoulEssence = 10000,
                CurrentSoulEssence = 10000,
                AttackPower = 1200,
                Classification = Classification.Human,
                SoulEssenceCost = 1100,
                Rarity = CardRarity.Uncommon
            };
            paladin.Tags.Add("Character");
            player.Deck.Add(paladin);
            
            // Berserker - High attack unit
            var berserker = new CharacterCard
            {
                Name = "Soul Berserker",
                LoreDescription = "Raging warrior of pure fury",
                MaxSoulEssence = 6000,
                CurrentSoulEssence = 6000,
                AttackPower = 2000,
                Classification = Classification.Human,
                SoulEssenceCost = 1200,
                Rarity = CardRarity.Rare
            };
            berserker.Tags.Add("Character");
            player.Deck.Add(berserker);
            
            // Champion - Powerful balanced unit
            var champion = new CharacterCard
            {
                Name = "Soul Champion",
                LoreDescription = "Elite warrior of legendary skill",
                MaxSoulEssence = 9000,
                CurrentSoulEssence = 9000,
                AttackPower = 1800,
                Classification = Classification.Human,
                SoulEssenceCost = 1400,
                Rarity = CardRarity.Rare
            };
            champion.Tags.Add("Character");
            player.Deck.Add(champion);
            
            // Add 6 more basic warriors for deck padding
            for (int i = 0; i < 6; i++)
            {
                var warrior = new CharacterCard
                {
                    Name = $"Warrior {i + 1}",
                    LoreDescription = "Trained combatant",
                    MaxSoulEssence = 4000 + (i * 500),
                    CurrentSoulEssence = 4000 + (i * 500),
                    AttackPower = 800 + (i * 100),
                    Classification = Classification.Human,
                    SoulEssenceCost = 500 + (i * 100),
                    Rarity = i > 3 ? CardRarity.Uncommon : CardRarity.Common
                };
                warrior.Tags.Add("Character");
                player.Deck.Add(warrior);
            }
            
            // ===== ITEM CARDS (4 different items) =====
            
            // Soul Blade - Attack boost
            var soulBlade = new ItemCard
            {
                Name = "Soul Blade",
                LoreDescription = "Weapon forged from crystallized souls",
                RequiresCharacter = true,
                SoulEssenceCost = 700,
                Rarity = CardRarity.Uncommon
            };
            soulBlade.Tags.Add("Item");
            soulBlade.Tags.Add("Weapon");
            soulBlade.EquipRestrictions.Add("Human");
            player.Deck.Add(soulBlade);
            
            // Soul Shield - SE boost
            var soulShield = new ItemCard
            {
                Name = "Soul Shield",
                LoreDescription = "Defensive barrier of pure essence",
                RequiresCharacter = true,
                SoulEssenceCost = 600,
                Rarity = CardRarity.Common
            };
            soulShield.Tags.Add("Item");
            soulShield.Tags.Add("Armor");
            soulShield.EquipRestrictions.Add("Human");
            player.Deck.Add(soulShield);
            
            // Soul Amulet - Regen boost
            var soulAmulet = new ItemCard
            {
                Name = "Soul Amulet",
                LoreDescription = "Mystical charm that enhances recovery",
                RequiresCharacter = true,
                SoulEssenceCost = 500,
                Rarity = CardRarity.Common
            };
            soulAmulet.Tags.Add("Item");
            soulAmulet.Tags.Add("Accessory");
            soulAmulet.EquipRestrictions.Add("Human");
            player.Deck.Add(soulAmulet);
            
            // Exodei Ring - Powerful legendary item
            var exodeiRing = new ItemCard
            {
                Name = "Exodei Ring",
                LoreDescription = "Ancient artifact of immense power",
                RequiresCharacter = true,
                SoulEssenceCost = 1000,
                Rarity = CardRarity.Rare
            };
            exodeiRing.Tags.Add("Item");
            exodeiRing.Tags.Add("Accessory");
            exodeiRing.EquipRestrictions.Add("Human");
            player.Deck.Add(exodeiRing);
            
            // ===== EVENT CARDS (4 different events) =====
            
            // Soul Storm - AOE damage
            var soulStorm = new EventCard
            {
                Name = "Soul Storm",
                LoreDescription = "Devastating blast of soul energy",
                SoulEssenceCost = 1500,
                Rarity = CardRarity.Rare
            };
            soulStorm.Tags.Add("Event");
            player.Deck.Add(soulStorm);
            
            // Soul Harvest - Heal
            var soulHarvest = new EventCard
            {
                Name = "Soul Harvest",
                LoreDescription = "Drain essence from the void",
                SoulEssenceCost = 800,
                Rarity = CardRarity.Uncommon
            };
            soulHarvest.Tags.Add("Event");
            player.Deck.Add(soulHarvest);
            
            // Soul Strike - Single target damage
            var soulStrike = new EventCard
            {
                Name = "Soul Strike",
                LoreDescription = "Focused blast of destructive power",
                SoulEssenceCost = 1000,
                Rarity = CardRarity.Uncommon
            };
            soulStrike.Tags.Add("Event");
            player.Deck.Add(soulStrike);
            
            // Soul Blessing - Buff allies
            var soulBlessing = new EventCard
            {
                Name = "Soul Blessing",
                LoreDescription = "Empower your forces with divine energy",
                SoulEssenceCost = 900,
                Rarity = CardRarity.Uncommon
            };
            soulBlessing.Tags.Add("Event");
            player.Deck.Add(soulBlessing);
            
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
                // Check if player has enough SE to play the card
                if (!CurrentPlayer.SEManager.SpendSE(card.SoulEssenceCost))
                    return; // Not enough SE
                
                CurrentPlayer.Hand.Remove(card);
                zone.AddCharacter(card);
                
                // Apply terrain bonuses if terrain is active in this zone
                if (zone.ActiveTerrain != null && zone.ActiveTerrain.SEBonus > 0)
                {
                    card.CurrentSoulEssence = Math.Min(
                        card.CurrentSoulEssence + zone.ActiveTerrain.SEBonus,
                        card.MaxSoulEssence
                    );
                }
            }
        }
        
        /// <summary>
        /// Play a terrain card to a zone
        /// </summary>
        public void PlayTerrain(TerrainCard terrain, GameZone zone)
        {
            if (CurrentPlayer.Hand.Contains(terrain))
            {
                // Check if player has enough SE to play the card
                if (!CurrentPlayer.SEManager.SpendSE(terrain.SoulEssenceCost))
                    return; // Not enough SE
                
                CurrentPlayer.Hand.Remove(terrain);
                zone.ActiveTerrain = terrain;
                
                // Apply terrain bonuses to all characters in this zone
                ApplyTerrainBonusesToZone(zone);
            }
        }
        
        /// <summary>
        /// Apply terrain bonuses to all characters in a zone
        /// </summary>
        private void ApplyTerrainBonusesToZone(GameZone zone)
        {
            if (zone.ActiveTerrain == null) return;
            
            foreach (var character in zone.Characters)
            {
                // Apply SE bonus
                if (zone.ActiveTerrain.SEBonus > 0)
                {
                    character.CurrentSoulEssence = Math.Min(
                        character.CurrentSoulEssence + zone.ActiveTerrain.SEBonus,
                        character.MaxSoulEssence
                    );
                }
                
                // Attack bonus is applied during combat calculation
                // (we don't modify base stats, just calculate with bonus)
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
            else if (card is ItemCard itemCard)
            {
                // Items need to be equipped to a character, not played to a zone
                // This will be handled separately via PlayItem method
            }
            else if (card is EventCard eventCard)
            {
                PlayEvent(eventCard);
            }
        }
        
        /// <summary>
        /// Equip an item to a character
        /// </summary>
        public void PlayItem(ItemCard item, CharacterCard target)
        {
            // Check if player has the card
            if (!CurrentPlayer.Hand.Contains(item)) return;
            
            // Check if item requires a character and target is valid
            if (item.RequiresCharacter && target == null) return;
            
            // Check equipment restrictions
            if (item.EquipRestrictions != null && item.EquipRestrictions.Count > 0)
            {
                if (!item.EquipRestrictions.Contains(target.Classification.ToString()))
                {
                    return; // Character doesn't meet requirements
                }
            }
            
            // Check if player has enough SE to play the card
            if (!CurrentPlayer.SEManager.SpendSE(item.SoulEssenceCost))
                return; // Not enough SE
            
            // Remove from hand
            CurrentPlayer.Hand.Remove(item);
            
            // Equip to character
            target.EquippedItem = item;
            
            // Apply item effects based on item name (simple prototype implementation)
            ApplyItemEffect(item, target);
        }
        
        /// <summary>
        /// Apply item effects to character
        /// </summary>
        private void ApplyItemEffect(ItemCard item, CharacterCard target)
        {
            // Simple prototype effects based on item name
            if (item.Name == "Soul Blade")
            {
                // Soul Blade: +500 Attack Power
                target.AttackPower += 500;
            }
            else if (item.Name == "Soul Shield")
            {
                // Soul Shield: +2000 Max SE and Current SE
                target.MaxSoulEssence += 2000;
                target.CurrentSoulEssence += 2000;
            }
            else if (item.Name == "Soul Amulet")
            {
                // Soul Amulet: +300 Attack Power and +1000 SE
                target.AttackPower += 300;
                target.MaxSoulEssence += 1000;
                target.CurrentSoulEssence += 1000;
            }
            else if (item.Name == "Exodei Ring")
            {
                // Exodei Ring: +800 Attack Power, +3000 SE (legendary item)
                target.AttackPower += 800;
                target.MaxSoulEssence += 3000;
                target.CurrentSoulEssence += 3000;
            }
        }
        
        /// <summary>
        /// Play an event card (instant effect)
        /// </summary>
        public void PlayEvent(EventCard eventCard)
        {
            // Check if player has the card
            if (!CurrentPlayer.Hand.Contains(eventCard)) return;
            
            // Check if player has enough SE to play the card
            if (!CurrentPlayer.SEManager.SpendSE(eventCard.SoulEssenceCost))
                return; // Not enough SE
            
            // Remove from hand
            CurrentPlayer.Hand.Remove(eventCard);
            
            // Apply event effect
            ApplyEventEffect(eventCard);
            
            // Events go to The Void after use (permanent removal)
            MoveToVoid(eventCard);
        }
        
        /// <summary>
        /// Apply event card effects
        /// </summary>
        private void ApplyEventEffect(EventCard eventCard)
        {
            // Simple prototype effects based on event name
            if (eventCard.Name == "Soul Storm")
            {
                // Soul Storm: Deal 2000 damage to all enemy characters
                var opponentZones = (CurrentPlayer == Player) ? 
                    new[] { Field.OpponentBase, Field.OpponentBattlefield } :
                    new[] { Field.PlayerHomeBase, Field.PlayerBattlefield };
                
                foreach (var zone in opponentZones)
                {
                    // Create a copy of the list to avoid modification during iteration
                    var characters = zone.Characters.ToList();
                    foreach (var character in characters)
                    {
                        character.TakeDamage(2000);
                        
                        // Remove defeated characters
                        if (character.IsDefeated)
                        {
                            zone.RemoveCharacter(character);
                            MoveToNexus(character);
                        }
                    }
                }
            }
            else if (eventCard.Name == "Soul Harvest")
            {
                // Soul Harvest: Heal current player for 3000 SE
                CurrentPlayer.SEManager.GainSE(3000);
            }
            else if (eventCard.Name == "Soul Strike")
            {
                // Soul Strike: Deal 3000 damage to a random enemy character
                var opponentZones = (CurrentPlayer == Player) ? 
                    new[] { Field.OpponentBase, Field.OpponentBattlefield } :
                    new[] { Field.PlayerHomeBase, Field.PlayerBattlefield };
                
                // Collect all enemy characters
                var allEnemies = new List<CharacterCard>();
                foreach (var zone in opponentZones)
                {
                    allEnemies.AddRange(zone.Characters);
                }
                
                // Damage a random enemy if any exist
                if (allEnemies.Count > 0)
                {
                    var random = new Random();
                    var target = allEnemies[random.Next(allEnemies.Count)];
                    target.TakeDamage(3000);
                    
                    // Remove if defeated
                    if (target.IsDefeated)
                    {
                        foreach (var zone in opponentZones)
                        {
                            if (zone.Characters.Contains(target))
                            {
                                zone.RemoveCharacter(target);
                                MoveToNexus(target);
                                break;
                            }
                        }
                    }
                }
            }
            else if (eventCard.Name == "Soul Blessing")
            {
                // Soul Blessing: Give all friendly characters +500 ATK and +1000 SE
                var friendlyZones = (CurrentPlayer == Player) ? 
                    new[] { Field.PlayerHomeBase, Field.PlayerBattlefield } :
                    new[] { Field.OpponentBase, Field.OpponentBattlefield };
                
                foreach (var zone in friendlyZones)
                {
                    foreach (var character in zone.Characters)
                    {
                        character.AttackPower += 500;
                        character.MaxSoulEssence += 1000;
                        character.CurrentSoulEssence += 1000;
                    }
                }
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
            
            // Apply terrain bonuses when moving to new zone
            if (toZone.ActiveTerrain != null && toZone.ActiveTerrain.SEBonus > 0)
            {
                card.CurrentSoulEssence = Math.Min(
                    card.CurrentSoulEssence + toZone.ActiveTerrain.SEBonus,
                    card.MaxSoulEssence
                );
            }
        }
        
        /// <summary>
        /// Attack another character
        /// </summary>
        /// <summary>
        /// Get effective attack power considering terrain bonuses
        /// </summary>
        private int GetEffectiveAttackPower(CharacterCard character)
        {
            int attackPower = character.AttackPower;
            
            // Find which zone the character is in
            GameZone zone = Field.GetZoneContaining(character);
            
            // Add terrain attack bonus if present
            if (zone?.ActiveTerrain != null)
            {
                attackPower += zone.ActiveTerrain.AttackBonus;
            }
            
            return attackPower;
        }
        
        public void Attack(CharacterCard attacker, CharacterCard target)
        {
            // Calculate attack with terrain bonuses
            int effectiveAttack = GetEffectiveAttackPower(attacker);
            
            // Deal damage using effective attack
            target.TakeDamage(effectiveAttack);
            attacker.HasActedThisTurn = true;
            
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
            
            // Use effective attack power with terrain bonuses
            int effectiveAttack = GetEffectiveAttackPower(attacker);
            target.SEManager.TakeDamage(effectiveAttack);
            attacker.HasActedThisTurn = true;
            
            CheckWinCondition();
        }
        
        /// <summary>
        /// Apply SE regen with terrain bonuses
        /// </summary>
        private void ApplyRegenWithTerrainBonus()
        {
            // Start with base regen
            CurrentPlayer.SEManager.ApplyRegen();
            
            // Add terrain regen bonuses from Home Base
            GameZone homeBase = (CurrentPlayer == Player) ? Field.PlayerHomeBase : Field.OpponentBase;
            if (homeBase.ActiveTerrain != null && homeBase.ActiveTerrain.RegenBonus > 0)
            {
                CurrentPlayer.SEManager.GainSE(homeBase.ActiveTerrain.RegenBonus);
            }
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
            
            // Apply regen (with terrain bonuses)
            ApplyRegenWithTerrainBonus();
            
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
            int currentSE = Opponent.SEManager.CurrentSE;
            
            // Play event cards first (instant effects) - check affordability
            var eventCard = Opponent.Hand.OfType<EventCard>()
                .Where(e => e.SoulEssenceCost <= currentSE)
                .OrderByDescending(e => e.SoulEssenceCost) // Prioritize expensive events
                .FirstOrDefault();
            if (eventCard != null)
            {
                PlayEvent(eventCard);
                return true;
            }
            
            // Play terrain card if we don't have one on battlefield - check affordability
            var terrainCard = Opponent.Hand.OfType<TerrainCard>()
                .Where(t => t.SoulEssenceCost <= currentSE)
                .OrderByDescending(t => t.AttackBonus + t.RegenBonus) // Prioritize valuable terrains
                .FirstOrDefault();
            if (terrainCard != null && Field.OpponentBattlefield.ActiveTerrain == null)
            {
                PlayTerrain(terrainCard, Field.OpponentBattlefield);
                return true;
            }
            
            // Play item cards on battlefield characters - check affordability
            var itemCard = Opponent.Hand.OfType<ItemCard>()
                .Where(i => i.SoulEssenceCost <= currentSE)
                .OrderByDescending(i => i.SoulEssenceCost) // Prioritize expensive items
                .FirstOrDefault();
            if (itemCard != null && Field.OpponentBattlefield.HasCharacters)
            {
                // Find a character without an item
                var targetChar = Field.OpponentBattlefield.Characters.FirstOrDefault(c => c.EquippedItem == null);
                if (targetChar != null)
                {
                    PlayItem(itemCard, targetChar);
                    return true;
                }
            }
            
            // Play character cards to home base - check affordability, prioritize stronger characters
            var characterCard = Opponent.Hand.OfType<CharacterCard>()
                .Where(c => c.SoulEssenceCost <= currentSE)
                .OrderByDescending(c => c.AttackPower) // Prioritize high attack characters
                .FirstOrDefault();
            if (characterCard != null)
            {
                PlayCard(characterCard, Field.OpponentBase);
                return true;
            }
            
            // Try to move a character forward
            if (Field.OpponentBase.HasCharacters)
            {
                var charToMove = Field.OpponentBase.Characters.FirstOrDefault(c => !c.HasActedThisTurn);
                if (charToMove != null)
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
