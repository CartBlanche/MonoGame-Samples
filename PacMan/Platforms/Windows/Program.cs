
using System;

namespace PacMan.Windows
{
	public static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			using (var game = new PacManGame())
			{
				game.Run();
			}
		}
	}
}

