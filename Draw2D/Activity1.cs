using Android.App;
using Android.OS;
using Android.Content.PM;

namespace Microsoft.Xna.Samples.Draw2D
{
    [Activity(Label = "Draw2D", MainLauncher = true, Icon = "@drawable/icon"
	          , Theme = "@style/Theme.Splash"
	          ,ScreenOrientation=ScreenOrientation.Portrait
	          ,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]	
    public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Game1.Activity = this;
            var g = new Game1();
            SetContentView(g.Window);
            g.Run();
        }
    }
}

