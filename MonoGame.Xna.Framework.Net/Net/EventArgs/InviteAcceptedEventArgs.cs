using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Event arguments for when an invite is accepted.
	/// </summary>
	public class InviteAcceptedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the gamer who accepted the invite.
        /// </summary>
        public SignedInGamer Gamer { get; internal set; }

        /// <summary>
        /// Gets whether the operation completed successfully.
        /// </summary>
        public bool IsSignedInGamer { get; internal set; }

        internal InviteAcceptedEventArgs(SignedInGamer gamer, bool isSignedInGamer)
        {
            Gamer = gamer;
            IsSignedInGamer = isSignedInGamer;
        }
    }
}
