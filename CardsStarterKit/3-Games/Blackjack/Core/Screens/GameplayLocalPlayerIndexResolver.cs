using System;
using System.Linq;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    internal static class GameplayLocalPlayerIndexResolver
    {
        public static bool TryAssign(BlackjackCardGame blackJackGame, NetworkSession networkSession, out int localPlayerIndex)
        {
            localPlayerIndex = -1;

            if (blackJackGame == null || networkSession == null || networkSession.LocalGamers.Count <= 0)
                return false;

            string localGamerTag = networkSession.LocalGamers[0].Gamertag;

            for (int i = 0; i < blackJackGame.Players.Count; i++)
            {
                if (blackJackGame.Players[i].Name.Equals(localGamerTag, StringComparison.OrdinalIgnoreCase))
                {
                    var betComponent = blackJackGame.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
                    if (betComponent != null)
                    {
                        betComponent.LocalPlayerIndex = i;
                    }

                    blackJackGame.LocalPlayerIndex = i;
                    localPlayerIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}