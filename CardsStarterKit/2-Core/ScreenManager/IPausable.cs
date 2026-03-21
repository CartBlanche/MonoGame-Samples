namespace CardsFramework.Core
{
    /// <summary>
    /// Implemented by any GameScreen that can be paused and resumed.
    /// PauseScreen calls ReturnFromPause() on the screen that implements this
    /// interface when the player chooses to resume the game.
    ///
    /// Example:
    ///   public class MyGameplayScreen : GameScreen, IPausable
    ///   {
    ///       public void ReturnFromPause() => ResumeMusic();
    ///   }
    /// </summary>
    public interface IPausable
    {
        void ReturnFromPause();
    }
}
