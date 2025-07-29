namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Event arguments for when a network session ends.
	/// </summary>
	public class NetworkSessionEndedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the reason the session ended.
        /// </summary>
        public NetworkSessionEndReason EndReason { get; internal set; }

        internal NetworkSessionEndedEventArgs(NetworkSessionEndReason endReason)
        {
            EndReason = endReason;
        }
    }
}
