using System;
using System.Collections.Generic;
using System.Linq;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#endif

namespace TiledSprites
{
#if MONOMAC
	class Program
	{
		static void Main (string [] args)
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
		private TiledSpritesSample game;
		
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new TiledSpritesSample();
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
        private static TiledSpritesSample game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new TiledSpritesSample();
            game.Run();
        }
    }
#endif
}
