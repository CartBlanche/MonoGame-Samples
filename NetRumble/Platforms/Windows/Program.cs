using System;

namespace NetRumble.Windows
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
