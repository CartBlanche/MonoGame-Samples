using System;
using NetworkPrediction;

namespace NetworkPrediction.iOS
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new NetworkPredictionGame())
                game.Run();
        }
    }
}
