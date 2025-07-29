using System;

namespace Tetris.Windows
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var game = new TetrisGame();
            game.Run();
        }
    }
}
