//-----------------------------------------------------------------------------
// Program.cs
//
// MonoGame Foundation Game Platform
// Copyright (C) MonoGame Foundation. All rights reserved.
//-----------------------------------------------------------------------------

#region Using Statements
using System;

using Foundation;
using UIKit;

using GameStateManagement.Core;

#endregion

namespace GameStateManagement.iOS;

[Register("AppDelegate")]
internal class Program : UIApplicationDelegate
{
    private static GameStateManagementGame game;

    internal static void RunGame()
    {
        game = new GameStateManagementGame();
        game.Run();
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    public override void FinishedLaunching(UIApplication app)
    {
        RunGame();
    }
}