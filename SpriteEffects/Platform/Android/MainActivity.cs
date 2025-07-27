using Android.App;
using Android.OS;
using Android.Content.PM;
using Microsoft.Xna.Framework;

namespace SpriteEffects.Android
{
    [Activity(Label = "SpriteEffects", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var game = new Core.SpriteEffectsGame();
            SetContentView((View)game.Services.GetService(typeof(View)));
            game.Run();
        }
    }
}
