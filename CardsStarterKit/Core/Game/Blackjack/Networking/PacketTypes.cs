namespace Blackjack.Networking
{
    public enum PacketType : byte
    {
        PlayerAction = 1,
        BetPlaced = 2,
        PlayerListSync = 3,
        ChipAdded = 4,
        GameStateUpdate = 10,
        CardDealt = 11,
        ShuffleSeed = 12,
        RoundStarted = 13,
        RoundEnded = 14,
        TurnChanged = 15,
        BalanceUpdate = 16,
        BettingPhaseStarted = 17,
        // Phase 5: Gameplay actions
        HitAction = 20,
        StandAction = 21,
        DoubleAction = 22,
        SplitAction = 23,
        InsuranceAction = 24,
    }

    public enum BlackjackAction : byte
    {
        Hit = 1,
        Stand = 2,
        Double = 3,
        Split = 4,
        Insurance = 5,
        Pass = 6
    }
}
