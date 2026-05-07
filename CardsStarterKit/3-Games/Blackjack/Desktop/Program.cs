using System;
using Blackjack.Networking;
using Microsoft.Xna.Framework.Net;

namespace Blackjack.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            MultiplayerSessionService.InitializeBackend(MultiplayerBackend.UdpSystemLink);
            LeaderboardPlatformStatus.ConfigureFromBackend(MultiplayerSessionService.ActiveBackend);

            using (var game = new BlackjackGame())
                game.Run();
        }
    }
}
