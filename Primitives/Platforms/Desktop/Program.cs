using System;

namespace PrimitivesSample.Desktop
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
