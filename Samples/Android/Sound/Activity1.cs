using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Samples.Sound
{
    [Activity(Label = "Sound", MainLauncher = true, Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash")]
    public class Activity1 : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var g = new Game1(this);
            SetContentView(g.Window);
            g.Run();
               
        }
    }
}
