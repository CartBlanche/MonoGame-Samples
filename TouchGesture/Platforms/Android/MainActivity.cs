using Android.App;
using Android.OS;

namespace TouchGesture.Android
{
    [Activity(Label = "TouchGesture", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var game = new TouchGestureGame();
            game.Run();
        }
    }
}
