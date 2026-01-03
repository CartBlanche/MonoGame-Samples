using System;

namespace Tetris.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new TetrisGame())
                game.Run();
        }
    }
}
