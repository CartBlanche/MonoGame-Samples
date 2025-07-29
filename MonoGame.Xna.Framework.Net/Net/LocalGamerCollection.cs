using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Collection of local network gamers in a session.
	/// </summary>
	public class LocalGamerCollection : ReadOnlyCollection<LocalNetworkGamer>
    {
        internal LocalGamerCollection(IList<LocalNetworkGamer> list) : base(list) { }

        /// <summary>
        /// Finds a local gamer by their ID.
        /// </summary>
        /// <param name="id">The ID to search for.</param>
        /// <returns>The local gamer with the specified ID, or null if not found.</returns>
        public LocalNetworkGamer FindGamerById(string id)
        {
            foreach (var gamer in this)
            {
                if (gamer.Id == id)
                    return gamer;
            }
            return null;
        }

        /// <summary>
        /// Gets the host gamer of the session (if local).
        /// </summary>
        public LocalNetworkGamer Host
        {
            get
            {
                foreach (var gamer in this)
                {
                    if (gamer.IsHost)
                        return gamer;
                }
                return null;
            }
        }
    }
}