using System;

namespace StencilCraters.Windows
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new StencilCratersGame())
            {
                game.Run();
            }
        }
    }
}
