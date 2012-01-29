#region Using Statements
using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

#endregion

namespace Blackjack
{
    [Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
		private BlackjackGame game;
		
		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new BlackjackGame();
            game.Run();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
}
