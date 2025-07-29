using System;

namespace RockRain.Desktop
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new RockRainGame())
                game.Run();
        }
    }
}
