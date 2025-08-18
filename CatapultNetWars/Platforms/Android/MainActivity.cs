using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;
using Android.Content.PM;

namespace CatapultGame
{
    public class MainActivity : AndroidGameActivity
    {
        private CatapultGame _game;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new CatapultGame();
            SetContentView((View)_game.Services.GetService(typeof(View)));
            _game.Run();
        }
    }
}


