using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VectorRumble
{
    class Program
    {
        static void Main(string[] args)
        {
            using (VectorRumbleGame game = new VectorRumbleGame())
            {
                game.Run();
            }

        }
    }
}
