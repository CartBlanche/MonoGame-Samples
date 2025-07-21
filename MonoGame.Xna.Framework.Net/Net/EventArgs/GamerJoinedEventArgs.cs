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
        public NetworkGamer Gamer { get; internal set; }

        internal GamerJoinedEventArgs(NetworkGamer gamer)
        {
            Gamer = gamer;
        }
    }
}
