//-----------------------------------------------------------------------------
// Program.cs
//
//-----------------------------------------------------------------------------

using System;

namespace NetworkPrediction
{
    /// <summary>
    /// The main class for the Windows version.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new NetworkPredictionGame())
                game.Run();
        }
    }
}
