using System;

namespace NetworkStateManagement.DesktopGL
{
    // Itch.io entry point. No platform store SDK bootstrap.
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new NetworkStateManagementGame())
            {
                game.Run();
            }
        }
    }
}
