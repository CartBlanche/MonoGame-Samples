namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Gamer presence information.
	/// </summary>
	public class GamerPresence
    {
        /// <summary>
        /// Gets or sets the presence mode.
        /// </summary>
        public GamerPresenceMode PresenceMode { get; set; } = GamerPresenceMode.Online;

        /// <summary>
        /// Gets or sets the presence value.
        /// </summary>
        public int PresenceValue { get; set; }
    }
}
