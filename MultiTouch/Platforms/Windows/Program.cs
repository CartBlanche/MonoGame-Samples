// ...existing code from Program.Windows.cs will be moved here...
using System;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Samples.MultiTouch
{
    /// <summary>
    /// The main entry point for Windows DirectX.
    /// </summary>
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Game1();
            game.Run();
        }
    }
}
