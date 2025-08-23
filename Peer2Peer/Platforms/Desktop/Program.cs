using System;

namespace PeerToPeer.Desktop
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new PeerToPeerGame())
                game.Run();
        }
    }
}
