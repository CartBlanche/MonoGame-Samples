using System;
using XnaGraphicsDemo;

namespace XnaGraphicsDemo
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new DemoGame())
                game.Run();
        }
    }
}
