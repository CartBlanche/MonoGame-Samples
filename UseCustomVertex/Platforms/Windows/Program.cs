using System;
using System.Windows.Forms;

namespace UseCustomVertex.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new UseCustomVertexGame())
                game.Run();
        }
    }
}
