using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;
using Gemstone_Hunter;

namespace GemstoneHunter.Android
{
    [Activity(Label = "Gemstone Hunter", MainLauncher = true, Icon = "@drawable/icon", 
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class Activity1 : AndroidGameActivity
    {
        private Game1 _game;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            _game = new Game1();
            SetContentView((Android.Views.View)_game.Services.GetService(typeof(Android.Views.View)));
            _game.RunOneFrame();
        }
    }
}
