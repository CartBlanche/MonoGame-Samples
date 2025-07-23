using System;
using XnaGraphicsDemo;

namespace XnaGraphicsDemo
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new DemoGame())
                game.Run();
        }
    }
}
