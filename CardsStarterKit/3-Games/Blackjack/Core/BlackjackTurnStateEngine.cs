using System;

namespace Blackjack
{
    internal static class BlackjackTurnStateEngine
    {
        public static void AdvanceAfterStandLikeAction(
            BlackjackPlayer player,
            bool[] turnFinishedByPlayer,
            int playerIndex,
            bool throwOnUnsupportedHandType)
        {
            if (!player.IsSplit)
            {
                turnFinishedByPlayer[playerIndex] = true;
                return;
            }

            switch (player.CurrentHandType)
            {
                case HandTypes.First:
                    if (player.SecondBlackJack)
                    {
                        turnFinishedByPlayer[playerIndex] = true;
                    }
                    else
                    {
                        player.CurrentHandType = HandTypes.Second;
                    }

                    break;

                case HandTypes.Second:
                    turnFinishedByPlayer[playerIndex] = true;
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