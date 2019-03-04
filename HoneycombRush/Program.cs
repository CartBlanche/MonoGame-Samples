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

#endregion

namespace HoneycombRush
{
#if WINDOWSS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (HoneycombRush game = new HoneycombRush())

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
		HoneycombRush game;
		public override void DidFinishLaunching (NSNotification notification)
		{
			game = new HoneycombRush();
			game.Run();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (AppKit.NSApplication sender)
		{
			return true;
		}
	}
#endif
}

