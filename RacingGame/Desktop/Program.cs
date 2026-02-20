#region Using Statements
using System;
#endregion

namespace RacingGame
{
	static class Program
	{
		[STAThread]
		static void Main (string [] args)
		{
			using (var game = new RacingGameManager ()) {
				game.Run ();
			}
		}
	}
}
