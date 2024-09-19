//-----------------------------------------------------------------------------
// MainActivity.cs
//
// MonoGame Foundation Game Platform
// Copyright (C) MonoGame Foundation. All rights reserved.
//-----------------------------------------------------------------------------

#region Using Statements
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using Microsoft.Xna.Framework;

using GameStateManagement.Core;
#endregion

namespace GameStateManagement.Android;

[Activity(
    Label = "GameStateManagement",
    MainLauncher = true,
    Icon = "@drawable/icon",
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
)]
public class MainActivity : AndroidGameActivity
{
    private GameStateManagementGame _game;
    private View _view;

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        _game = new GameStateManagementGame();
        _view = _game.Services.GetService(typeof(View)) as View;

        SetContentView(_view);
        _game.Run();
    }
}
