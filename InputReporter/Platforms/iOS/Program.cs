using System;
using Microsoft.Xna.Framework;

namespace InputReporter
{
    /// <summary>
    /// The main class for iOS platform.
    /// </summary>
    public static class Program
    {
        static void Main(string[] args)
        {
            using var game = new InputReporterGame();
            game.Run();
        }
    }
}
