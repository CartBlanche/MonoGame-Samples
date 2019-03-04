
#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;


#if __MACOS__
using Foundation;
using AppKit;
using ObjCRuntime;
#endif

#endregion

namespace GooCursor
{
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
		Game1 game;
		public override void DidFinishLaunching (NSNotification notification)
		{
			game = new Game1();
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
        private static Game1 game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new Game1();
            game.Run();
        }
    }
#endif
}