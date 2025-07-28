using System;

namespace TouchGesture.Windows
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
