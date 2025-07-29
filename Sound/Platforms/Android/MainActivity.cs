using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;

namespace SoundSample.Android
{
    [Activity(Label = "Sound", MainLauncher = true, Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash",ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // SoundGame.Activity = this; // Not needed in MonoGame 3.8 Android
            var g = new SoundGame();
            g.Run();
               
        }
    }
}
