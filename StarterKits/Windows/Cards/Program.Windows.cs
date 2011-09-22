using System;
using System.Collections.Generic;
using System.Linq;

namespace Blackjack
{
    static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (BlackjackGame game = new BlackjackGame())
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
