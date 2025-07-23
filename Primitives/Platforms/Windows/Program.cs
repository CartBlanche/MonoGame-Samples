using System;
using PrimitivesSample;

namespace PrimitivesSample.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new PrimitivesSampleGame())
                game.Run();
        }
    }
}
