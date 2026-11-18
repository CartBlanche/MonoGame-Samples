using System;
using System.IO;

namespace ThirdPersonShooter
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (Array.IndexOf(args, "--test-physics") >= 0)
                return PhysicsSelfTest.Run(Path.Combine(AppContext.BaseDirectory, "Content")); // headless, exit code = failure count
            if (Array.IndexOf(args, "--test-ui") >= 0)
                return UiSelfTest.Run(); // headless UI kit + settings checks, exit code = failure count
            if (Array.IndexOf(args, "--test-audio") >= 0)
                return AudioSelfTest.Run(Path.Combine(AppContext.BaseDirectory, "Content")); // headless audio maths/table checks, exit code = failure count
            using var game = new ThirdPersonShooterGame(args);
            game.Run();
            return 0;
        }
    }
}
