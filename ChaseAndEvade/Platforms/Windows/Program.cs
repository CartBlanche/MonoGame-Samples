using System;
using ChaseAndEvade;

namespace ChaseAndEvade.Windows
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new ChaseAndEvadeGame())
                game.Run();
        }
    }
}
