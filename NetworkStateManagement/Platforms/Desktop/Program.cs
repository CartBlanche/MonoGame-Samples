using System;

namespace NetworkStateManagement.DesktopGL
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new NetworkStateManagementGame())
            {
                game.Run();
            }
        }
    }
}
