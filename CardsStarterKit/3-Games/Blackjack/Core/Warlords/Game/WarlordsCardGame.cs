//-----------------------------------------------------------------------------
// WarlordsCardGame.cs
//
// Main game logic for Warlords
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using CardsFramework;
using CardsFramework.Core;
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

        /// <summary>The phase currently active for the current player's turn.</summary>
        public TurnPhase CurrentPhase => CurrentPlayer.CurrentTurnTracker.CurrentPhase;
        
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

        // Terrain contest state
        /// <summary>The terrain card that has been proposed but not yet confirmed.</summary>
        public TerrainCard PendingTerrain { get; private set; }
        /// <summary>The zone the pending terrain was proposed for.</summary>
        public GameZone PendingTerrainZone { get; private set; }
        /// <summary>The player who proposed the pending terrain.</summary>
        public WarlordsPlayer PendingTerrainProposer { get; private set; }

        // One-shot UI notification state
        public bool IsDialogOpen { get; private set; }
        public string DialogMessage { get; private set; }
        
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
            
            // Enter home terrain selection. Decks are NOT shuffled and hands are NOT
            // dealt yet — that happens once both sides have picked their home terrain.
            State = WarlordsGameState.HomeTerrainSelectionPending;

            // AI immediately picks its best home terrain (highest RegenBonus).
            SelectAIHomeTerrain();
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
            
            // Add 16 more basic warriors to reach deck total of 35
            // (5 terrain + 6 named chars + 16 warriors + 4 items + 4 events = 35)
            for (int i = 0; i < 16; i++)
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
        
        // ── Mulligan ──────────────────────────────────────────────────────

        /// <summary>
        /// Human player swaps <paramref name="cardsToSwap"/> back into their deck,
        /// reshuffles, and redraws the same number of cards.
        /// Calling with an empty list is equivalent to <see cref="SkipMulligan"/>.
        /// </summary>
        public void PerformMulligan(List<WarlordsCard> cardsToSwap)
        {
            if (State != WarlordsGameState.MulliganPending) return;

            int swapCount = 0;
            foreach (var card in cardsToSwap)
            {
                if (Player.Hand.Contains(card))
                {
                    Player.Hand.Remove(card);
                    Player.Deck.Add(card);
                    swapCount++;
                }
            }

            if (swapCount > 0)
                Player.Deck = Player.Deck.OrderBy(_ => Guid.NewGuid()).ToList();

            for (int i = 0; i < swapCount; i++)
                Player.DrawCard();

            FinishMulligan();
        }

        /// <summary>Keep the opening hand as-is and start the game.</summary>
        public void SkipMulligan()
        {
            if (State != WarlordsGameState.MulliganPending) return;
            FinishMulligan();
        }

        /// <summary>
        /// Simple AI mulligan: swap any hand cards that are not Characters or Terrain
        /// when no Characters are present (i.e. unplayable opening hand).
        /// </summary>
        private void PerformAIMulligan()
        {
            bool hasCharacter = Opponent.Hand.Any(c => c is CharacterCard);
            if (!hasCharacter)
            {
                // Swap all non-character cards to try to get at least one character.
                var toSwap = Opponent.Hand.Where(c => !(c is CharacterCard)).ToList();
                foreach (var card in toSwap)
                {
                    Opponent.Hand.Remove(card);
                    Opponent.Deck.Add(card);
                }
                Opponent.Deck = Opponent.Deck.OrderBy(_ => Guid.NewGuid()).ToList();
                for (int i = 0; i < toSwap.Count; i++)
                    Opponent.DrawCard();
            }
        }

        private void FinishMulligan()
        {
            State = WarlordsGameState.Playing;

            // Turn 1: opening hand was just drawn — skip the Draw phase.
            Player.HasDrawn = true;
            Player.CurrentTurnTracker.AdvancePhase();   // Draw → Main
        }

        /// <summary>
        /// Player picks a terrain card from their deck to place on their Home Base.
        /// Called from the UI. When both sides have selected, transitions to MulliganPending.
        /// </summary>
        public void SelectHomeTerrain(TerrainCard terrain)
        {
            if (State != WarlordsGameState.HomeTerrainSelectionPending) return;
            if (!Player.Deck.Contains(terrain)) return;

            Player.Deck.Remove(terrain);
            Field.PlayerHomeBase.ActiveTerrain = terrain;
            Field.PlayerHomeBase.HasTerrainBeenSet = true;

            // If AI has already picked (it does so immediately in Initialize), proceed.
            if (Field.OpponentBase.ActiveTerrain != null)
                FinishHomeTerrainSelection();
        }

        /// <summary>
        /// AI picks its home terrain — the one with the highest RegenBonus, favouring
        /// expensive cards when bonuses are equal (best value for HomeBase regen role).
        /// </summary>
        private void SelectAIHomeTerrain()
        {
            var best = Opponent.Deck.OfType<TerrainCard>()
                .OrderByDescending(t => t.RegenBonus)
                .ThenByDescending(t => t.SoulEssenceCost)
                .FirstOrDefault();

            if (best == null) return;

            Opponent.Deck.Remove(best);
            Field.OpponentBase.ActiveTerrain = best;
            Field.OpponentBase.HasTerrainBeenSet = true;
        }

        /// <summary>
        /// Both home terrains have been chosen. Shuffle both decks, deal opening hands,
        /// then enter the mulligan window.
        /// </summary>
        private void FinishHomeTerrainSelection()
        {
            // Shuffle remaining decks now that home terrain cards have been removed.
            Player.Deck   = Player.Deck.OrderBy(_ => Guid.NewGuid()).ToList();
            Opponent.Deck = Opponent.Deck.OrderBy(_ => Guid.NewGuid()).ToList();

            // Deal opening hands.
            StartGame();

            // Enter mulligan; AI decides immediately.
            State = WarlordsGameState.MulliganPending;
            PerformAIMulligan();
        }

        /// <summary>
        /// Start the game — deal the opening 7-card hand to each player.
        /// Called once from FinishHomeTerrainSelection(); the screen must not call it.
        /// </summary>
        public void StartGame()
        {
            for (int i = 0; i < RulesEngine.OpeningHandSize; i++)
            {
                Player.DrawCard();
                Opponent.DrawCard();
            }
        }
        
        /// <summary>
        /// Play a character card to a zone.
        /// </summary>
        public void PlayCard(CharacterCard card, GameZone zone)
        {
            var result = RulesEngine.CanPlayCharacter(
                card, zone, zone.Owner,
                CurrentPlayer.CurrentTurnTracker,
                CurrentPlayer.SEManager.CurrentSE,
                RulesEngine.EvaluateOverburden(CurrentPlayer.Hand.Count));
            if (!result.IsLegal) return;

            if (!CurrentPlayer.SEManager.SpendSE(card.SoulEssenceCost)) return;

            CurrentPlayer.Hand.Remove(card);
            zone.AddCharacter(card);
            CurrentPlayer.HasPlayedCharacter = true;

            // Overburden Tier-3: characters enter at half MaxSoulEssence
            if (RulesEngine.EvaluateOverburden(CurrentPlayer.Hand.Count + 1) == OverburdenLevel.Tier3_18Plus)
                card.CurrentSoulEssence = card.MaxSoulEssence / 2;

            // Apply terrain SE bonus
            if (zone.ActiveTerrain != null && zone.ActiveTerrain.SEBonus > 0)
            {
                card.CurrentSoulEssence = Math.Min(
                    card.CurrentSoulEssence + zone.ActiveTerrain.SEBonus,
                    card.MaxSoulEssence);
            }
        }
        
        /// <summary>
        /// Play a terrain card to a zone. Deducts cost, removes from hand, then enters
        /// the terrain-contest window (TerrainContestPending). The terrain is NOT placed on
        /// the zone until the opponent passes or fails to counter.
        /// </summary>
        public void PlayTerrain(TerrainCard terrain, GameZone zone)
        {
            var result = RulesEngine.CanAttemptTerrainSet(
                terrain, zone, zone.Owner,
                CurrentPlayer.CurrentTurnTracker,
                CurrentPlayer.SEManager.CurrentSE);
            if (!result.IsLegal) return;

            if (!CurrentPlayer.SEManager.SpendSE(terrain.SoulEssenceCost)) return;

            CurrentPlayer.Hand.Remove(terrain);
            CurrentPlayer.HasPlayedTerrain = true;

            // Store intent; the zone is NOT updated yet.
            PendingTerrain = terrain;
            PendingTerrainZone = zone;
            PendingTerrainProposer = CurrentPlayer;
            State = WarlordsGameState.TerrainContestPending;
            WaitingForPlayer = false;
        }

        /// <summary>
        /// The contesting player plays a terrain from their hand to counter the pending terrain.
        /// Both terrains are returned to their owners' decks (reshuffled) and the zone stays clear.
        /// </summary>
        public void CounterTerrain(TerrainCard counterTerrain)
        {
            if (State != WarlordsGameState.TerrainContestPending) return;
            if (counterTerrain == null) return;

            // Contester is whoever is NOT the proposer
            WarlordsPlayer contester = (PendingTerrainProposer == Player) ? Opponent : Player;
            if (!contester.Hand.Contains(counterTerrain)) return;

            contester.SEManager.SpendSE(counterTerrain.SoulEssenceCost);
            contester.Hand.Remove(counterTerrain);

            // Both terrains return to their owners' decks and are reshuffled
            PendingTerrainProposer.Deck.Add(PendingTerrain);
            ShuffleDeck(PendingTerrainProposer);

            contester.Deck.Add(counterTerrain);
            ShuffleDeck(contester);

            // Zone is untouched — no terrain is placed
            string proposedName = PendingTerrain?.Name ?? "terrain";
            string counterName = counterTerrain.Name;
            bool playerWasProposer = PendingTerrainProposer == Player;
            ClearPendingTerrain();
            State = WarlordsGameState.Playing;

            if (playerWasProposer)
            {
                IsDialogOpen = true;
                DialogMessage =
                    $"Terrain was countered: {proposedName} vs {counterName}. Both were returned to decks and reshuffled.";
            }
        }

        /// <summary>
        /// The contesting player passes (cannot or chooses not to counter).
        /// The pending terrain is confirmed onto the zone.
        /// </summary>
        public void PassTerrainContest()
        {
            if (State != WarlordsGameState.TerrainContestPending) return;

            // Battlefield terrain effects are shared across both battlefield lanes.
            Field.PlayerBattlefield.ActiveTerrain = PendingTerrain;
            Field.PlayerBattlefield.HasTerrainBeenSet = true;
            Field.OpponentBattlefield.ActiveTerrain = PendingTerrain;
            Field.OpponentBattlefield.HasTerrainBeenSet = true;

            ApplyTerrainBonusesToZone(Field.PlayerBattlefield);
            ApplyTerrainBonusesToZone(Field.OpponentBattlefield);

            ClearPendingTerrain();
            State = WarlordsGameState.Playing;
        }

        public void CloseDialog()
        {
            IsDialogOpen = false;
            DialogMessage = string.Empty;
        }

        private void ClearPendingTerrain()
        {
            PendingTerrain = null;
            PendingTerrainZone = null;
            PendingTerrainProposer = null;
        }

        private static void ShuffleDeck(WarlordsPlayer player)
        {
            var rng = new Random();
            var deck = player.Deck;
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
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
        /// Equip an item to a character (or deploy as zone obstacle when IsZoneObstacle is true).
        /// </summary>
        public void PlayItem(ItemCard item, CharacterCard target)
        {
            var result = RulesEngine.CanPlayItem(
                item, target,
                CurrentPlayer.CurrentTurnTracker,
                CurrentPlayer.SEManager.CurrentSE);
            if (!result.IsLegal) return;

            if (!CurrentPlayer.Hand.Contains(item)) return;

            // Check equipment restrictions
            if (target != null && item.EquipRestrictions != null && item.EquipRestrictions.Count > 0)
            {
                if (!item.EquipRestrictions.Contains(target.Classification.ToString()))
                    return;
            }

            if (!CurrentPlayer.SEManager.SpendSE(item.SoulEssenceCost)) return;

            CurrentPlayer.Hand.Remove(item);
            CurrentPlayer.HasPlayedItem = true;

            if (target != null)
            {
                target.EquippedItem = item;
                ApplyItemEffect(item, target);
            }
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
        /// Play an event card (instant effect).
        /// </summary>
        public void PlayEvent(EventCard eventCard)
        {
            var result = RulesEngine.CanPlayEvent(
                eventCard,
                CurrentPlayer.CurrentTurnTracker,
                CurrentPlayer.SEManager.CurrentSE);
            if (!result.IsLegal) return;

            if (!CurrentPlayer.Hand.Contains(eventCard)) return;
            if (!CurrentPlayer.SEManager.SpendSE(eventCard.SoulEssenceCost)) return;

            CurrentPlayer.Hand.Remove(eventCard);
            CurrentPlayer.HasPlayedEvent = true;

            ApplyEventEffect(eventCard);
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
        /// Move a character from one zone to another.
        /// </summary>
        public void MoveCharacter(CharacterCard card, GameZone fromZone, GameZone toZone)
        {
            var result = RulesEngine.CanMoveCharacter(
                card, fromZone, toZone,
                CurrentPlayer.CurrentTurnTracker);
            if (!result.IsLegal) return;

            // Determine advance vs retreat to assign the correct action.
            bool isRetreat =
                (fromZone.Owner == PlayerSide.Player   && toZone.Type == ZoneType.HomeBase) ||
                (fromZone.Owner == PlayerSide.Player   && toZone.Type == ZoneType.Battlefield && fromZone.Type == ZoneType.EnemyBattlefield) ||
                (fromZone.Owner == PlayerSide.Opponent && toZone.Type == ZoneType.EnemyBase) ||
                (fromZone.Owner == PlayerSide.Opponent && toZone.Type == ZoneType.EnemyBattlefield && fromZone.Type == ZoneType.Battlefield);

            fromZone.RemoveCharacter(card);
            toZone.AddCharacter(card);
            card.ActionThisTurn = isRetreat ? CharacterAction.Retreat : CharacterAction.Advance;

            // Apply terrain SE bonus when moving into new zone
            if (toZone.ActiveTerrain != null && toZone.ActiveTerrain.SEBonus > 0)
            {
                card.CurrentSoulEssence = Math.Min(
                    card.CurrentSoulEssence + toZone.ActiveTerrain.SEBonus,
                    card.MaxSoulEssence);
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
        
        /// <summary>
        /// Put a character in Defend stance, granting 25% damage reduction until end of turn.
        /// Requires Main phase; character must not have acted yet.
        /// </summary>
        public void Defend(CharacterCard character)
        {
            var result = RulesEngine.CanDefend(character, CurrentPlayer.CurrentTurnTracker);
            if (!result.IsLegal) return;
            character.ActionThisTurn = CharacterAction.Defend;
        }

        public void Attack(CharacterCard attacker, CharacterCard target)
        {
            var result = RulesEngine.CanAttackCharacter(
                attacker, target, Field,
                CurrentPlayer.CurrentTurnTracker);
            if (!result.IsLegal) return;

            int effectiveAttack = GetEffectiveAttackPower(attacker);

            // Apply defender's damage reduction if in Defend stance
            if (target.ActionThisTurn == CharacterAction.Defend)
                effectiveAttack = (int)(effectiveAttack * (1f - CharacterCard.DefendDamageReduction));

            target.TakeDamage(effectiveAttack);
            attacker.ActionThisTurn = CharacterAction.Attack;

            if (target.IsDefeated)
            {
                var zone = Field.GetZoneContaining(target);
                zone?.RemoveCharacter(target);
                MoveToNexus(target);
            }
        }
        
        /// <summary>
        /// Attack the opposing player directly.
        /// Requires the attacker to be in the Enemy Battlefield and the Enemy
        /// Home Base to be fully clear.
        /// </summary>
        public void AttackPlayer(CharacterCard attacker, WarlordsPlayer target)
        {
            var attackerSide = (CurrentPlayer == Player) ? PlayerSide.Player : PlayerSide.Opponent;
            var result = RulesEngine.CanAttackWarlord(
                attacker, attackerSide, Field,
                CurrentPlayer.CurrentTurnTracker);
            if (!result.IsLegal) return;

            int effectiveAttack = GetEffectiveAttackPower(attacker);
            target.SEManager.TakeDamage(effectiveAttack);
            attacker.ActionThisTurn = CharacterAction.Attack;
        }
        
        /// <summary>
        /// Sacrifice a card from hand. The card is sent to The Void.
        /// If it is a CharacterCard the player gains its current Soul Essence.
        /// </summary>
        public void SacrificeCard(WarlordsCard card)
        {
            var result = RulesEngine.CanSacrifice(card, CurrentPlayer.CurrentTurnTracker);
            if (!result.IsLegal) return;

            if (!CurrentPlayer.Hand.Contains(card)) return;

            CurrentPlayer.Hand.Remove(card);

            if (card is CharacterCard charCard)
                CurrentPlayer.SEManager.GainSE(charCard.CurrentSoulEssence);

            MoveToVoid(card);
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
        /// End current turn: apply overburden, regen/degen, win check, then switch player.
        /// </summary>
        public void EndTurn()
        {
            var tracker = CurrentPlayer.CurrentTurnTracker;

            // ── Overburden tier checks (End phase) ─────────────────────────
            var overburden = RulesEngine.EvaluateOverburden(CurrentPlayer.Hand.Count);
            if (overburden >= OverburdenLevel.Tier1_14_15)
            {
                // Tier-1 degen equals full effective regen (both regen and degen apply;
                // they do NOT cancel each other).
                CurrentPlayer.SEManager.TakeDamage(CurrentPlayer.SEManager.EffectiveRegen);
            }

            // Reset all character actions
            foreach (var zone in Field.GetAllZones())
                foreach (var character in zone.Characters)
                    character.ResetTurnState();

            // ── RegenDegen phase ───────────────────────────────────
            ApplyRegenWithTerrainBonus();

            // ── Win condition (checked only after RegenDegen) ─────────────
            CheckWinCondition();
            if (State != WarlordsGameState.Playing) return;

            // ── Reset and switch ──────────────────────────────────
            CurrentPlayer.ResetTurnFlags();
            CurrentPlayer = (CurrentPlayer == Player) ? Opponent : Player;
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
            // ── Terrain contest: AI responds ─────────────────────────────
            // When a terrain was proposed by the player, give the AI one tick to decide.
            if (State == WarlordsGameState.TerrainContestPending && DateTime.Now >= nextAIAction)
            {
                WarlordsPlayer contester = (PendingTerrainProposer == Player) ? Opponent : Player;

                if (contester == Opponent)
                {
                    // AI counters if it has an affordable terrain in hand
                    var counterTerrain = Opponent.Hand.OfType<TerrainCard>()
                        .Where(t => t.SoulEssenceCost <= Opponent.SEManager.CurrentSE)
                        .OrderByDescending(t => t.AttackBonus + t.RegenBonus)
                        .FirstOrDefault();

                    if (counterTerrain != null)
                        CounterTerrain(counterTerrain);
                    else
                        PassTerrainContest();
                }
                // When the player is the contester, the human decides via UI — nothing to do here.
            }

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
                    // Move character from OpponentBase to OpponentBattlefield — skip rules
                    // engine for AI so terrain gate doesn't block early movement.
                    Field.OpponentBase.RemoveCharacter(charToMove);
                    Field.OpponentBattlefield.AddCharacter(charToMove);
                    charToMove.ActionThisTurn = CharacterAction.Advance;
                    return true;
                }
            }
            
            // Attack if possible
            if (Field.OpponentBattlefield.HasCharacters)
            {
                var attacker = Field.OpponentBattlefield.Characters
                    .FirstOrDefault(c => c.ActionThisTurn == CharacterAction.None);
                if (attacker != null)
                {
                    if (Field.PlayerBattlefield.HasCharacters)
                    {
                        Attack(attacker, Field.PlayerBattlefield.Characters[0]);
                        return true;
                    }
                    else if (!Field.PlayerBattlefield.HasCharacters)
                    {
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

            // Auto-draw for the incoming player's Draw phase (optional, once per turn).
            // The player can skip by advancing the phase; this keeps the prototype playable
            // without a dedicated Draw-phase UI button.
            if (State == WarlordsGameState.Playing && CurrentPlayer.Deck.Count > 0
                && !CurrentPlayer.HasDrawn)
            {
                CurrentPlayer.DrawCard();
                CurrentPlayer.HasDrawn = true;
            }

            // Move into Main phase automatically.
            if (State == WarlordsGameState.Playing)
                CurrentPlayer.CurrentTurnTracker.AdvancePhase(); // Draw → Main
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
        /// <summary>
        /// Both players simultaneously pick one terrain card from their deck to place
        /// on their Home Base before decks are shuffled or hands are dealt.
        /// AI picks automatically; the human player decides via UI.
        /// </summary>
        HomeTerrainSelectionPending,
        /// <summary>
        /// The human player is choosing which opening-hand cards to swap.
        /// AI mulligans automatically.
        /// </summary>
        MulliganPending,
        Playing,
        /// <summary>
        /// A terrain card has been proposed; the opposing player now gets
        /// one chance to counter it by playing a terrain of their own.
        /// Resolves to Playing when either the counter window expires or
        /// a counter is played.
        /// </summary>
        TerrainContestPending,
        PlayerWins,
        OpponentWins
    }
}
