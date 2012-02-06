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
#elif MACOS


	static class Program
	{	
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			MonoMac.AppKit.NSApplication.Init ();
			
			using (var p = new MonoMac.Foundation.NSAutoreleasePool ()) {
				MonoMac.AppKit.NSApplication.SharedApplication.Delegate = new AppDelegate();
				MonoMac.AppKit.NSApplication.Main(args);
			}
		}
	}
	
	class AppDelegate : MonoMac.AppKit.NSApplicationDelegate
	{
		CatapultGame game;
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			CatapultGame game = new CatapultGame();
			game.Run ();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (MonoMac.AppKit.NSApplication sender)
		{
			return true;
		}
	}
#endif

}

