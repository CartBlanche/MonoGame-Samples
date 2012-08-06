using System;
using System.Collections.Generic;
using System.Linq;

using MonoMac.AppKit;
using MonoMac.Foundation;

namespace ShadowMapping
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			NSApplication.Init ();
			
			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate ();
				NSApplication.Main (args);
			}


		}
	}

	class AppDelegate : NSApplicationDelegate
	{
		ShadowMappingGame game;

		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new ShadowMappingGame ();
			game.Run ();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}  
}


