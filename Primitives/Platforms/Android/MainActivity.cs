using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace PrimitivesSample.Android
{
    [Activity(Label = "Primitives Sample",
              MainLauncher = true,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
              ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle? bundle)
        {
            base.OnCreate(bundle);
            var game = new PrimitivesSampleGame();
            SetContentView((View)game.Services.GetService(typeof(View))!);
            game.Run();
        }
    }
}
