namespace Microsoft.Xna.Framework.GamerServices
{
	public interface IGuideSignInProvider
    {
        Task<bool> ShowSignInAsync(int paneCount, bool onlineOnly, CancellationToken cancellationToken = default);
    }

	internal sealed class DefaultGuideSignInProvider : IGuideSignInProvider
    {
        public Task<bool> ShowSignInAsync(int paneCount, bool onlineOnly, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }
    }

	/// <summary>
	/// Provides access to platform services.
	/// </summary>
	public static class Guide
    {
        private static readonly object gate = new();
        private static bool isVisible;
        private static IGuideSignInProvider signInProvider = new DefaultGuideSignInProvider();

        /// <summary>
        /// Gets whether the current game is running in trial mode.
        /// </summary>
        public static bool IsTrialMode => false; // Mock implementation - not in trial mode

        /// <summary>
        /// Gets whether the Guide is visible.
        /// </summary>
        public static bool IsVisible
        {
            get
            {
                lock (gate)
                {
                    return isVisible;
                }
            }
        }

        /// <summary>
        /// Platform-auth provider used by sign-in APIs.
        /// Override from the platform composition root.
        /// </summary>
        public static IGuideSignInProvider SignInProvider
        {
            get
            {
                lock (gate)
                {
                    return signInProvider;
                }
            }
            set
            {
                lock (gate)
                {
                    signInProvider = value ?? throw new ArgumentNullException(nameof(value));
                }
            }
        }

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
        public static async Task<int?> ShowMessageBoxAsync(
            string title,
            string text,
            IEnumerable<string> buttons,
            int focusButton,
            MessageBoxIcon icon,
            CancellationToken cancellationToken = default)
        {
            if (buttons == null)
                throw new ArgumentNullException(nameof(buttons));

            var buttonList = buttons as IList<string> ?? buttons.ToList();
            if (buttonList.Count == 0)
                return null;

            cancellationToken.ThrowIfCancellationRequested();

            SetGuideVisible(true);

            try
            {
                await Task.Yield();

                if (focusButton < 0 || focusButton >= buttonList.Count)
                    return 0;

                return focusButton;
            }
            finally
            {
                SetGuideVisible(false);
            }
        }

        /// <summary>
        /// Synchronous wrapper for ShowMessageBoxAsync.
        /// </summary>
        public static int? ShowMessageBox(
            string title,
            string text,
            IEnumerable<string> buttons,
            int focusButton,
            MessageBoxIcon icon)
        {
            return ShowMessageBoxAsync(title, text, buttons, focusButton, icon).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Shows the sign-in interface.
        /// </summary>
        public static async Task ShowSignInAsync(int paneCount, bool onlineOnly, CancellationToken cancellationToken = default)
        {
            if (paneCount < 1)
                throw new ArgumentOutOfRangeException(nameof(paneCount));

            SetGuideVisible(true);

            try
            {
                var provider = SignInProvider;
                var signedIn = await provider.ShowSignInAsync(paneCount, onlineOnly, cancellationToken).ConfigureAwait(false);
                SignedInGamer.Current.SetSignedInToLive(signedIn);
            }
            finally
            {
                SetGuideVisible(false);
            }
        }

        /// <summary>
        /// Shows the sign-in interface (synchronous version).
        /// </summary>
        public static void ShowSignIn(int paneCount, bool onlineOnly)
        {
            ShowSignInAsync(paneCount, onlineOnly).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Shows the marketplace.
        /// </summary>
        public static void ShowMarketplace(PlayerIndex playerIndex)
        {
            // Mock implementation
        }

        private static void SetGuideVisible(bool value)
        {
            lock (gate)
            {
                isVisible = value;
            }
        }
    }
}
