using System;
using Android.App;
using Android.OS;
using Microsoft.Xna.Framework;

namespace Particle3DSample.Android
{
    [Activity(Label = "Particle3DSample", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var game = new Particle3DSampleGame();
            SetContentView((game.Services.GetService(typeof(View)) as View));
            game.Run();
        }
    }
}
