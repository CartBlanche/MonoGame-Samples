using System;
using System.Collections.Generic;
using System.Linq;


#if __MACOS__
using AppKit;
using Foundation;
#endif

namespace Tetris
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
		Engine game;
		public override void DidFinishLaunching (NSNotification notification)
		{
			game = new Engine();
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
        private static Engine game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new Engine();
            game.Run();
        }
    }
#endif
}

