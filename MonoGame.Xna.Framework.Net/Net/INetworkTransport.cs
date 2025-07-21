using System;
using System.Net;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Defines the contract for a network transport layer, providing methods for sending and receiving data.
    /// </summary>
    public interface INetworkTransport : IDisposable
    {
        /// <summary>
        /// Receives data from the network in a blocking manner.
        /// </summary>
        /// <returns>A tuple containing the received data and the sender's endpoint.</returns>
        (byte[] data, IPEndPoint sender) Receive();

        /// <summary>
        /// Receives data from the network asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The result contains the received data and the sender's endpoint.</returns>
        Task<(byte[] data, IPEndPoint sender)> ReceiveAsync();

        /// <summary>
        /// Sends data to the specified endpoint in a blocking manner.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send the data to.</param>
        void Send(byte[] data, IPEndPoint endpoint);

        /// <summary>
        /// Sends data to the specified endpoint asynchronously.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send the data to.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendAsync(byte[] data, IPEndPoint endpoint);
    }
}