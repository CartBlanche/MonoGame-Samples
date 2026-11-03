using System;

namespace Gorillas.DesktopGL
{
	public static class Program
	{
		[STAThread]
		static void Main (string[] args)
		{
			using (var game = new Engine ()) {
				game.Run ();
			}
		}
	}
}
