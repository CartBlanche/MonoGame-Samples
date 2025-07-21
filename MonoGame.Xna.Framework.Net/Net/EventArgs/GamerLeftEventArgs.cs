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
        public NetworkGamer Gamer { get; internal set; }

        internal GamerLeftEventArgs(NetworkGamer gamer)
        {
            Gamer = gamer;
        }
    }
}
