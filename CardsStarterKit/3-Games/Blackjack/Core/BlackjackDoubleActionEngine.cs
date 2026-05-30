using System;

namespace Blackjack
{
    internal static class BlackjackDoubleActionEngine
    {
        public static void ApplyDoubleWager(
            BlackjackPlayer player,
            int playerIndex,
            BetGameComponent betGameComponent,
            bool throwOnUnsupportedHandType)
        {
            switch (player.CurrentHandType)
            {
                case HandTypes.First:
                    player.Double = true;
                    float betAmount = player.BetAmount;

                    if (player.IsSplit)
                    {
                        betAmount /= 2f;
                    }

                    betGameComponent.AddChips(playerIndex, betAmount, false, false);
                    break;

                case HandTypes.Second:
                    player.SecondDouble = true;
                    if (!player.Double)
                    {
                        betGameComponent.AddChips(playerIndex, player.BetAmount / 2f, false, true);
                    }
                    else
                    {
                        betGameComponent.AddChips(playerIndex, player.BetAmount / 3f, false, true);
                    }

                    break;

                default:
                    if (throwOnUnsupportedHandType)
                    {
                        throw new Exception("Player has an unsupported hand type.");
                    }

                    break;
            }
        }
    }
}