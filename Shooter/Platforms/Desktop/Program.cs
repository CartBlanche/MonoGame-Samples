using Shooter;

internal class Program
{
    private static void Main(string[] args)
    {
        using var game = new ShooterGame(args);
        game.Run();
    }
}
