namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Represents a local network gamer.
	/// </summary>
	public class LocalNetworkGamer : NetworkGamer
    {
        internal LocalNetworkGamer(NetworkSession session, string id, bool isHost, string gamertag)
            : base(session, id, true, isHost, gamertag)
        {
        }

        /// <summary>
        /// Gets whether this local gamer is sending data.
        /// </summary>
        public bool IsSendingData => false; // Mock implementation

        /// <summary>
        /// Enables or disables sending data for this local gamer.
        /// </summary>
        /// <param name="options">Send data options.</param>
        public void EnableSendData(SendDataOptions options)
        {
            // Mock implementation
        }
    }
}