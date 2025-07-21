namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Represents a signed-in gamer.
	/// </summary>
	public class SignedInGamer : Gamer
    {
        private static SignedInGamer current;

        /// <summary>
        /// Gets the current signed-in gamer.
        /// </summary>
        public static SignedInGamer Current
        {
            get
            {
                if (current == null)
                {
                    current = new SignedInGamer();
                    current.SetGamertag(Environment.UserName);
                }
                return current;
            }
            internal set => current = value;
        }

        private string gamertag;

        /// <summary>
        /// Gets or sets the gamertag for this gamer.
        /// </summary>
        public override string Gamertag 
        { 
            get => gamertag;
        }

        /// <summary>
        /// Sets the gamertag for this gamer.
        /// </summary>
        internal void SetGamertag(string value)
        {
            gamertag = value;
        }

        /// <summary>
        /// Gets whether this gamer is signed in to a live service.
        /// </summary>
        public bool IsSignedInToLive => false; // Mock implementation

        /// <summary>
        /// Gets whether this gamer is a guest.
        /// </summary>
        public bool IsGuest => false;

        /// <summary>
        /// Gets the display name for this gamer.
        /// </summary>
        public new string DisplayName => Gamertag;

        /// <summary>
        /// Gets the presence information for this gamer.
        /// </summary>
        public GamerPresence Presence { get; } = new GamerPresence();

        /// <summary>
        /// Gets the player index for this gamer.
        /// </summary>
        public PlayerIndex PlayerIndex { get; internal set; } = PlayerIndex.One;

        internal SignedInGamer() { }

		public GamerPrivileges Privileges
		{
			get;
			private set;
		}
	}
}
