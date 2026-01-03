using System;

namespace BatteryStatus.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            var powerStatus = new PowerStatus();
            using (var game = new BatteryStatusGame(powerStatus))
                game.Run();
        }
    }
}
