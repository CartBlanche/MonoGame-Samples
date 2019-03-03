#region Using Statements
using System;
#endregion

namespace ShipGame
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new ShipGameGame())
            {
                game.Run();
            }
        }
    }
}

