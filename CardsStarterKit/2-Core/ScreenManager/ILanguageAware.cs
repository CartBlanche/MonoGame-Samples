namespace CardsFramework.Core
{
    /// <summary>
    /// Implemented by any GameScreen that needs to react when the player changes
    /// the game language. ScreenManager calls OnLanguageChanged() on all screens
    /// that implement this interface after fonts have been reloaded.
    ///
    /// Example — a gameplay screen that has localised button labels:
    ///   public class MyGameplayScreen : GameScreen, ILanguageAware
    ///   {
    ///       public void OnLanguageChanged() => RebuildButtonLabels();
    ///   }
    /// </summary>
    public interface ILanguageAware
    {
        void OnLanguageChanged();
    }
}
