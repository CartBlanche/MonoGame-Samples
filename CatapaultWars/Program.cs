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

#if IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

#endregion

namespace CatapultGame
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (CatapultGame game = new CatapultGame())
            {
                game.Run();
            }
        }
    }
#elif __MACOS__


	static class Program
	{	
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			AppKit.NSApplication.Init ();
			
			using (var p = new Foundation.NSAutoreleasePool ()) {
				AppKit.NSApplication.SharedApplication.Delegate = new AppDelegate();
				AppKit.NSApplication.Main(args);
			}
		}
	}
	
	class AppDelegate : AppKit.NSApplicationDelegate
	{
		CatapultGame game;
		public override void DidFinishLaunching (NSNotification notification)
		{
			CatapultGame game = new CatapultGame();
			game.Run ();

		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (AppKit.NSApplication sender)
		{
			return true;
		}
	}
#elif IPHONE
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
		private CatapultGame game;

		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new CatapultGame();
			game.Run();
		}
		
		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "AppDelegate");
		}
	}
#endif

}

