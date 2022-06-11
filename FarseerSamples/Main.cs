using System;
using System.Collections.Generic;
using System.Linq;

using AppKit;
using Foundation;

namespace FarseerPhysics.SamplesFramework
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
		FarseerPhysicsGame game;

		public override void DidFinishLaunching (NSNotification notification)
		{
			game = new FarseerPhysicsGame ();
			game.Run ();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}  
}


