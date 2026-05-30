using System;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    internal sealed class GameplayPacketProcessingPolicy
    {
        private readonly Func<NetworkSession> sessionProvider;

        public GameplayPacketProcessingPolicy(Func<NetworkSession> sessionProvider)
        {
            this.sessionProvider = sessionProvider ?? throw new ArgumentNullException(nameof(sessionProvider));
        }

        public void ProcessOnClient(Action processAction)
        {
            var session = sessionProvider();
            if (session != null && !session.IsHost)
            {
                processAction();
            }
        }

        public void ProcessOnClientFromHost(NetworkGamer sender, Action processAction)
        {
            var session = sessionProvider();
            if (session != null && !session.IsHost && sender != null && sender.IsHost)
            {
                processAction();
            }
        }

        public void ProcessOnHost(Action processAction)
        {
            var session = sessionProvider();
            if (session != null && session.IsHost)
            {
                processAction();
            }
        }

        public void ProcessHostRelayPacket(
            NetworkGamer sender,
            Action hostApply,
            Action hostBroadcast,
            Action clientApply)
        {
            var session = sessionProvider();
            if (session == null)
                return;

            if (session.IsHost)
            {
                if (!sender.IsLocal)
                {
                    hostApply();
                    hostBroadcast();
                }
            }
            else
            {
                if (sender != null && sender.IsHost)
                {
                    clientApply();
                }
            }
        }

        public void HandleClientPlayerAction(NetworkGamer sender, byte playerIndex, Action<byte> applyAction)
        {
            ProcessOnClientFromHost(sender, () => applyAction(playerIndex));
        }
    }
}
