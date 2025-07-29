namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Base class for all gamer types.
	/// </summary>
	public abstract class Gamer
    {
        /// <summary>
        /// Gets the gamertag for this gamer.
        /// </summary>
        public abstract string Gamertag { get; }

        /// <summary>
        /// Gets the display name for this gamer.
        /// </summary>
        public virtual string DisplayName => Gamertag;

        /// <summary>
        /// Gets custom data associated with this gamer.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Gets the signed-in gamers.
        /// </summary>
        public static GamerCollection<SignedInGamer> SignedInGamers => signedInGamers;

        private static readonly GamerCollection<SignedInGamer> signedInGamers;

        static Gamer()
        {
            // Initialize with current signed-in gamer
            var gamers = new List<SignedInGamer> { SignedInGamer.Current };
            signedInGamers = new GamerCollection<SignedInGamer>(gamers);
        }
    }
}
