//-----------------------------------------------------------------------------
// BlackjackGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

namespace Blackjack
{
    /// <summary>
    /// The various possible game states.
    /// </summary>
    public enum BlackjackGameState
    {
        Shuffling,
        Betting,
        Playing,
        Dealing,
        RoundEnd,
        GameOver,
    }
}