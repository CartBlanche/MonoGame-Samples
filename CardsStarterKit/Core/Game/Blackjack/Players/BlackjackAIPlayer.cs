//-----------------------------------------------------------------------------
// BlackjackNPCPlayer.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CardsFramework;

namespace Blackjack
{
    class BlackjackNPCPlayer : BlackjackPlayer
    {
        static Random random = new Random();

        public event EventHandler Hit;
        public event EventHandler Stand;

        /// <summary>
        /// Creates a new instance of the <see cref="BlackjackNPCPlayer"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="game">The game.</param>
        public BlackjackNPCPlayer(string name, CardsGame game)
            : base(name, game)
        {
        }

        /// <summary>
        /// Performs a move during a round.
        /// </summary>
        public void NPCPlay()
        {
            int value = FirstValue;
            if (FirstValueConsiderAce && value + 10 <= 21)
            {
                value += 10;
            }

            if (value < 17 && Hit != null)
            {
                Hit(this, EventArgs.Empty);
            }
            else if (Stand != null)
            {
                Stand(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Returns the amount which the NPC player decides to bet.
        /// </summary>
        /// <returns>The NPC player's bet.</returns>
        public int NPCBet()
        {
            int[] chips = { 0, 5, 25, 100, 500 };
            int bet = chips[random.Next(0, chips.Length)];

            if (bet < Balance)
            {
                return bet;
            }

            return 0;
        }
    }
}
