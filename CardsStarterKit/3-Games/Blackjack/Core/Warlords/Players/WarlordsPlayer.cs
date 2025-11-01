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
        
        // Turn tracking (for Phase 1 expansion)
        public bool HasPlayedCharacter { get; set; }
        public bool HasPlayedItem { get; set; }
        public bool HasPlayedEvent { get; set; }
        public bool HasDrawn { get; set; }
        
        public WarlordsPlayer(string name, CardsFramework.CardsGame game) 
            : base(name, game)
        {
            SEManager = new SoulEssenceManager(10000);
            Deck = new List<WarlordsCard>();
            Hand = new List<WarlordsCard>();
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
        /// Reset turn flags at end of turn
        /// </summary>
        public void ResetTurnFlags()
        {
            HasPlayedCharacter = false;
            HasPlayedItem = false;
            HasPlayedEvent = false;
            HasDrawn = false;
        }
    }
}
