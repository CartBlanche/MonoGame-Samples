namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Options for sending data over the network.
	/// </summary>
	public enum SendDataOptions
    {
        /// <summary>
        /// No special options - unreliable delivery.
        /// </summary>
        None,

        /// <summary>
        /// Reliable delivery - guarantees the data will arrive.
        /// </summary>
        Reliable,

        /// <summary>
        /// In-order delivery - data arrives in the order sent.
        /// </summary>
        InOrder,

        /// <summary>
        /// Reliable and in-order delivery.
        /// </summary>
        ReliableInOrder = Reliable | InOrder
    }
}
