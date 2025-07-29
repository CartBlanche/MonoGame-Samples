namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Properties used when creating or searching for network sessions.
	/// </summary>
	public class NetworkSessionProperties : Dictionary<string, object>
    {
        /// <summary>
        /// Initializes a new NetworkSessionProperties.
        /// </summary>
        public NetworkSessionProperties() : base() { }

        /// <summary>
        /// Initializes a new NetworkSessionProperties with a specified capacity.
        /// </summary>
        public NetworkSessionProperties(int capacity) : base(capacity) { }
    }
}
