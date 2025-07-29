using System;
using System.Windows.Forms;

namespace CollisionSample
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new CollisionGame())
                game.Run();
        }
    }
}
