using System;

namespace ShaderTest.Windows
{
    /// <summary>
    /// The main entry point for the Windows application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new ShaderTestGame())
                game.Run();
        }
    }
}
