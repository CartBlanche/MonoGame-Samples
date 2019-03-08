using System;
using System.Collections.Generic;
using System.Linq;

#if __MACOS__
using AppKit;
using Foundation;
#endif

namespace NetworkStateManagement
{
	#region Entry Point
#if __MACOS__
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
		
		public override void DidFinishLaunching (NSNotification notification)
		{
			NetworkStateManagementGame game = new NetworkStateManagementGame ();
			game.Run ();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}	
#else
    static class Program
    {
        private static NetworkStateManagementGame game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new NetworkStateManagementGame();
            game.Run();
        }
    }
#endif
	#endregion
}

