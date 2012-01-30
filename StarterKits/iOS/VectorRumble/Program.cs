#region File Description
//-----------------------------------------------------------------------------
// Program.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;

#if IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif


#endregion

namespace VectorRumble
{
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
		private VectorRumbleGame game;
		
		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game= new VectorRumbleGame();
            game.Run();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
}

