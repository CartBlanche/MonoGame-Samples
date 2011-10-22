/*
 * Created by SharpDevelop.
 * User: d_ellis
 * Date: 06/10/2011
 * Time: 19:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace RenderTarget2DSample
{
	class Program
	{
		private static Game1 game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new Game1();
            game.Run();
        }
	}
}