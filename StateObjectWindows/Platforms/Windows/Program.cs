using System;

namespace StateObject.Windows
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using (var game = new StateObjectGame())
            {
                game.Run();
            }
        }
    }
}
