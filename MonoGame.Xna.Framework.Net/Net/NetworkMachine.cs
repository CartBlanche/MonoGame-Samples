namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Represents a machine in a network session.
	/// </summary>
	public class NetworkMachine
    {
        /// <summary>
        /// Gets the gamers on this machine.
        /// </summary>
        public GamerCollection Gamers { get; } = new GamerCollection(new List<NetworkGamer>());
    }
}