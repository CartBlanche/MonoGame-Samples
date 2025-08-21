using System;

namespace NetRumble.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new NetRumbleGame())
                game.Run();
        }
    }
}