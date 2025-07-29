using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Collection of network gamers in a session.
	/// </summary>
	public class GamerCollection : ReadOnlyCollection<NetworkGamer>
    {
        internal GamerCollection(IList<NetworkGamer> list) : base(list) { }

        /// <summary>
        /// Finds a gamer by their gamertag.
        /// </summary>
        /// <param name="gamertag">The gamertag to search for.</param>
        /// <returns>The gamer with the specified gamertag, or null if not found.</returns>
        public NetworkGamer FindGamerById(string id)
        {
            foreach (var gamer in this)
            {
                if (gamer.Id == id)
                    return gamer;
            }
            return null;
        }

        /// <summary>
        /// Gets the host gamer of the session.
        /// </summary>
        public NetworkGamer Host
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