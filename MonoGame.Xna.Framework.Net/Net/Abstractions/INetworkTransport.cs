using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Abstraction for network transport mechanisms.
    /// Allows different transport implementations (UDP, Steam P2P, etc.) to be plugged in.
    /// </summary>
    public interface INetworkTransport : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the transport is bound to a local endpoint.
        /// </summary>
        bool IsBound { get; }

        /// <summary>
        /// Initializes and binds the transport to a local endpoint.
        /// </summary>
        void Bind();

        /// <summary>
        /// Initializes and binds the transport to a specific port.
        /// </summary>
        /// <param name="port">The port to bind to.</param>
        void Bind(int port);

        /// <summary>
        /// Sends data to a specific endpoint synchronously.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send to.</param>
        void Send(byte[] data, IPEndPoint endpoint);

        /// <summary>
        /// Sends a fixed-length prefix of data to a specific endpoint synchronously.
        /// </summary>
        /// <param name="data">The data buffer to send from.</param>
        /// <param name="length">The number of bytes to send from the start of the buffer.</param>
        /// <param name="endpoint">The endpoint to send to.</param>
        void Send(byte[] data, int length, IPEndPoint endpoint);

        /// <summary>
        /// Sends data to a specific endpoint asynchronously.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send to.</param>
        Task SendAsync(byte[] data, IPEndPoint endpoint);

        /// <summary>
        /// Sends a fixed-length prefix of data to a specific endpoint asynchronously.
        /// </summary>
        /// <param name="data">The data buffer to send from.</param>
        /// <param name="length">The number of bytes to send from the start of the buffer.</param>
        /// <param name="endpoint">The endpoint to send to.</param>
        Task SendAsync(byte[] data, int length, IPEndPoint endpoint);

        /// <summary>
        /// Receives data from the network synchronously.
        /// </summary>
        /// <returns>A tuple containing the received data and the sender's endpoint.</returns>
        (byte[] data, IPEndPoint sender) Receive();

        /// <summary>
        /// Receives data from the network asynchronously.
        /// </summary>
        /// <returns>A task containing a tuple with the received data and the sender's endpoint.</returns>
        Task<(byte[] data, IPEndPoint sender)> ReceiveAsync();
    }
}
