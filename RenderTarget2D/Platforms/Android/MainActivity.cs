using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace RenderTarget2DSample.Android
{
    [Activity(Label = "RenderTarget2D", MainLauncher = true, Icon = "@drawable/icon",
        Theme = "@style/Theme.Splash", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class MainActivity : AndroidGameActivity
    {
        private RenderTarget2DSampleGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            _game = new RenderTarget2DSampleGame();
            _view = _game.Services.GetService(typeof(View)) as View;
            
            SetContentView(_view);
            _game.Run();
        }
    }
}


