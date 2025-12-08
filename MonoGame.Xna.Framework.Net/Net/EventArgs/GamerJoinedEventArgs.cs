namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Event arguments for when a gamer joins the session.
    /// </summary>
    public class GamerJoinedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the gamer who joined.
        /// </summary>
        public INetworkGamer Gamer { get; internal set; }

        /// <summary>
        /// Initializes a new instance of GamerJoinedEventArgs.
        /// </summary>
        internal GamerJoinedEventArgs(INetworkGamer gamer)
        {
            Gamer = gamer;
        }

        /// <summary>
        /// Initializes a new instance of GamerJoinedEventArgs with no arguments.
        /// </summary>
        internal GamerJoinedEventArgs() { }
    }
}
