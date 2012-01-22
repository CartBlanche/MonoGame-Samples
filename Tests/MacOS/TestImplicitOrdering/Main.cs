using System;
using System.Drawing;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace TestImplicitOrdering
{
    class MainClass
    {
        static void Main(string[] args)
        {
            NSApplication.Init();
            using (var pool = new NSAutoreleasePool())
            {
                NSApplication.SharedApplication.Delegate = new AppDelegate();
                NSApplication.Main(args);
            }
        }
    }

    class AppDelegate : NSApplicationDelegate
    {
        private ImplicitOrderingGame _game;
        public override void DidFinishLaunching(NSNotification notification)
        {
            _game = new ImplicitOrderingGame();
            _game.Run();
        }

        public override void WillTerminate(NSNotification notification)
        {
            if (_game != null)
            {
                _game.Dispose();
                _game = null;
            }
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            return true;
        }
    }
}

