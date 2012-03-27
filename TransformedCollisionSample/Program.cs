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

#if MONOMAC
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif
#endregion

namespace TransformedCollision
{
#if MONOMAC
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			NSApplication.Init ();
			
			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate();
				NSApplication.Main(args);
			}
		}
	}
	
	class AppDelegate : NSApplicationDelegate
	{
		TransformedCollisionGame game;
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new TransformedCollisionGame();
			game.Run();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}	
#elif IPHONE
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
        TransformedCollisionGame game;
		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new TransformedCollisionGame();
			game.Run ();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
#endif
}