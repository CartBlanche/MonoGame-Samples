//-----------------------------------------------------------------------------
// WarlordsPlayer.cs
//
// Represents a player in Warlords
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CardsFramework;
using WarlordsFramework;

namespace Warlords
{
    /// <summary>
    /// Represents a player with deck, hand, and Soul Essence
    /// </summary>
    public class WarlordsPlayer : Player
    {
        public SoulEssenceManager SEManager { get; private set; }
        public List<WarlordsCard> Deck { get; set; }
        public new List<WarlordsCard> Hand { get; set; }
        
        public TerrainCard ChosenHomeBase { get; set; }

        // ── Turn tracking ─────────────────────────────────────────────────
        public TurnTracker CurrentTurnTracker { get; private set; }

        // Legacy convenience properties — delegate to TurnTracker.
        public bool HasPlayedCharacter
        {
            get => CurrentTurnTracker.HasPlayedCharacter;
            set => CurrentTurnTracker.HasPlayedCharacter = value;
        }
        public bool HasPlayedItem
        {
            get => CurrentTurnTracker.HasPlayedItem;
            set => CurrentTurnTracker.HasPlayedItem = value;
        }
        public bool HasPlayedEvent
        {
            get => CurrentTurnTracker.HasPlayedEvent;
            set => CurrentTurnTracker.HasPlayedEvent = value;
        }
        public bool HasDrawn
        {
            get => CurrentTurnTracker.HasDrawnThisTurn;
            set => CurrentTurnTracker.HasDrawnThisTurn = value;
        }
        public bool HasPlayedTerrain
        {
            get => CurrentTurnTracker.HasPlayedTerrain;
            set => CurrentTurnTracker.HasPlayedTerrain = value;
        }
        
        public WarlordsPlayer(string name, CardsFramework.CardsGame game) 
            : base(name, game)
        {
            SEManager = new SoulEssenceManager(10000);
            Deck = new List<WarlordsCard>();
            Hand = new List<WarlordsCard>();
            CurrentTurnTracker = new TurnTracker();
        }
        
        /// <summary>
        /// Draw a card from deck to hand
        /// </summary>
        public void DrawCard()
        {
            if (Deck.Count > 0)
            {
                var card = Deck[0];
                Deck.RemoveAt(0);
                Hand.Add(card);
            }
        }
        
        /// <summary>
        /// Reset turn flags at end of turn.
        /// </summary>
        public void ResetTurnFlags()
        {
            CurrentTurnTracker.Reset();
        }
    }
}
