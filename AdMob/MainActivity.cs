using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;
using Android.Gms.Ads;
using Android.Content.PM;

namespace AdMob.Android
{
    [Activity (
        Label = "AdMob",
        MainLauncher = true,
        Icon = "@drawable/icon",
        Theme = "@style/Theme.Splash",
        ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]	
    public class MainActivity : AndroidGameActivity
    {
        private AdMobGame _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Initialize Google Mobile Ads SDK
            MobileAds.Initialize(this);

            // Create the MonoGame view
            _game = new AdMobGame();
            _view = _game.Services.GetService(typeof(View)) as View;

            // Create a layout to hold both the game and the ad
            var layout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };

            // Add the MonoGame view
            layout.AddView(_view, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0, 1f)); // Weight 1 to fill remaining space

            // Create and add the AdMob banner
            var adView = new AdView(this)
            {
                AdSize = AdSize.Banner,
                AdUnitId = "ca-app-pub-3940256099942544/6300978111" // Test Ad Unit ID
            };
            var adParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
            layout.AddView(adView, adParams);

            // Load an ad
            var adRequest = new AdRequest.Builder().Build();
            adView.LoadAd(adRequest);

            // Set the layout as the content view
            SetContentView(layout);
            _game.Run();
        }
    }
}


