namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Provides access to platform services.
	/// </summary>
	public static class Guide
    {
        /// <summary>
        /// Gets whether the current game is running in trial mode.
        /// </summary>
        public static bool IsTrialMode => false; // Mock implementation - not in trial mode

        /// <summary>
        /// Gets whether the Guide is visible.
        /// </summary>
        public static bool IsVisible => false; // Mock implementation

        /// <summary>
        /// Gets whether screen saver is enabled.
        /// </summary>
        public static bool IsScreenSaverEnabled
        {
            get => false;
            set { /* Mock implementation */ }
        }

        /// <summary>
        /// Shows a message box to the user.
        /// </summary>
        public static IAsyncResult BeginShowMessageBox(
            string title,
            string text,
            IEnumerable<string> buttons,
            int focusButton,
            MessageBoxIcon icon,
            AsyncCallback callback,
            object state)
        {
            // Mock implementation - for now just return a completed result
            var result = new MockAsyncResult(state, true);
            callback?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Ends the message box operation.
        /// </summary>
        public static int? EndShowMessageBox(IAsyncResult result)
        {
            return 0; // Mock implementation - first button selected
        }

        /// <summary>
        /// Shows the sign-in interface.
        /// </summary>
        public static IAsyncResult BeginShowSignIn(int paneCount, bool onlineOnly, AsyncCallback callback, object state)
        {
            var result = new MockAsyncResult(state, true);
            callback?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Ends the sign-in operation.
        /// </summary>
        public static void EndShowSignIn(IAsyncResult result)
        {
            // Mock implementation
        }

        /// <summary>
        /// Shows the sign-in interface (synchronous version).
        /// </summary>
        public static void ShowSignIn(int paneCount, bool onlineOnly)
        {
            // Mock implementation
        }

        /// <summary>
        /// Shows the marketplace.
        /// </summary>
        public static void ShowMarketplace(PlayerIndex playerIndex)
        {
            // Mock implementation
        }
    }
}
