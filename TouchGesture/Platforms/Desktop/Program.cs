using System;

namespace TouchGesture.DesktopGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new TouchGestureGame())
                game.Run();
        }
    }
}
