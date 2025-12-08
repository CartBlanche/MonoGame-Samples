namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Event arguments for when a gamer leaves the session.
    /// </summary>
    public class GamerLeftEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the gamer who left.
        /// </summary>
        public INetworkGamer Gamer { get; internal set; }

        /// <summary>
        /// Initializes a new instance of GamerLeftEventArgs.
        /// </summary>
        internal GamerLeftEventArgs(INetworkGamer gamer)
        {
            Gamer = gamer;
        }

        /// <summary>
        /// Initializes a new instance of GamerLeftEventArgs with no arguments.
        /// </summary>
        internal GamerLeftEventArgs() { }
    }
}
