using System;

namespace NetworkStateManagement.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new NetworkStateManagementGame())
            {
                game.Run();
            }
        }
    }
}
