using System;
using BatteryStatus;

namespace BatteryStatus.Windows
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