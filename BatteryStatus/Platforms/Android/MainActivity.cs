using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace BatteryStatusDemo
{
	[Activity(Label = "BatteryStatus",
		Name = "com.cartblanche.batterystatus.MainActivity",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
	public class MainActivity : AndroidGameActivity
	{
		private Game1 _game;
		private View _view;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			_game = new Game1();
			//Game1.SetInstance(_game);
			_view = _game.Services.GetService(typeof(View)) as View;

			SetContentView(_view);
			_game.Run();
		}
	}
}
