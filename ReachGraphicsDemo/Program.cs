#region File Description
//-----------------------------------------------------------------------------
// Program.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#endif


using System;

namespace XnaGraphicsDemo
{
#if MONOMAC
	class Program
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate ();

				// Set our Application Icon
				//NSImage appIcon = NSImage.ImageNamed ("GameThumbnail.png");
				//NSApplication.SharedApplication.ApplicationIconImage = appIcon;

				NSApplication.Main (args);
			}
		}
	}

	class AppDelegate : NSApplicationDelegate
	{
		private DemoGame game;

		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new DemoGame();
			game.Run();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}	
#else
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (DemoGame game = new DemoGame())
            {
                game.Run();
            }
        }
    }
#endif
}

