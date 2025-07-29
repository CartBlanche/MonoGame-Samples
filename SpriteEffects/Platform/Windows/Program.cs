using System;
using System.Windows.Forms;
using SpriteEffects; // Add this to reference the shared Core namespace

namespace SpriteEffects.Platform.Windows
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var game = new SpriteEffectsGame();
            game.Run();
        }
    }
}
