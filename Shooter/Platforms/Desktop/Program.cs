using System;
using Shooter;

// Create and run the game
try
{
    using var game = new ShooterGame();
    game.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL ERROR] Game crashed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
