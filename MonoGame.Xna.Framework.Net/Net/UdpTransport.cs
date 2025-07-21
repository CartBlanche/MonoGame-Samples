using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Provides a UDP-based implementation of the <see cref="INetworkTransport"/> interface for sending and receiving network data.
    /// </summary>
    public class UdpTransport : INetworkTransport
    {
        private readonly UdpClient udpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpTransport"/> class.
        /// </summary>
        public UdpTransport()
        {
            udpClient = new UdpClient();
        }

        /// <summary>
        /// Receives data from the network in a blocking manner.
        /// </summary>
        /// <returns>A tuple containing the received data and the sender's endpoint.</returns>
        public (byte[] data, IPEndPoint sender) Receive()
        {
            // Call the async version and block until it completes
            return ReceiveAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Receives data from the network asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The result contains the received data and the sender's endpoint.</returns>
        public async Task<(byte[] data, IPEndPoint sender)> ReceiveAsync()
        {
            var result = await udpClient.ReceiveAsync();
            return (result.Buffer, result.RemoteEndPoint);
        }

        /// <summary>
        /// Sends data to the specified endpoint in a blocking manner.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send the data to.</param>
        public void Send(byte[] data, IPEndPoint endpoint)
        {
            // Call the async version and block until it completes
            SendAsync(data, endpoint).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends data to the specified endpoint asynchronously.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="endpoint">The endpoint to send the data to.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SendAsync(byte[] data, IPEndPoint endpoint)
        {
            await udpClient.SendAsync(data, data.Length, endpoint);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="UdpTransport"/> class.
        /// </summary>
        public void Dispose()
        {
            udpClient?.Close();
            udpClient?.Dispose();
        }
    }
}
