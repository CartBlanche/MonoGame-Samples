using System;
using NetworkPrediction;

namespace NetworkPrediction.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new NetworkPredictionGame())
                game.Run();
        }
    }
}
