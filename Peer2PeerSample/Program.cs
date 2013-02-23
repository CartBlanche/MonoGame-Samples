#region File Description
//-----------------------------------------------------------------------------
// PeerToPeerGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

#if MONOMAC
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

#endregion


namespace PeerToPeer
{
	
	#region Entry Point
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
		
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			PeerToPeerGame game = new PeerToPeerGame ();
			game.Run ();
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
		private PeerToPeerGame game;

		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new PeerToPeerGame();
			game.Run();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
#else
	class Program
	{
		public static void Main(string[] args)
		{
			using ( PeerToPeer.PeerToPeerGame game = new  PeerToPeer.PeerToPeerGame())
			{
				game.Run();
			}
		}
	}
#endif
	
	#endregion
}
