using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platformer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (PlatformerGame game = new PlatformerGame())
                {
                    game.Run();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
    }
}
