namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Quality of service information for a network session.
	/// </summary>
	public class QualityOfService
    {
        /// <summary>
        /// Gets the average round trip time in milliseconds.
        /// </summary>
        public TimeSpan AverageRoundTripTime { get; internal set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Gets the minimum round trip time in milliseconds.
        /// </summary>
        public TimeSpan MinimumRoundTripTime { get; internal set; } = TimeSpan.FromMilliseconds(20);

        /// <summary>
        /// Gets the maximum round trip time in milliseconds.
        /// </summary>
        public TimeSpan MaximumRoundTripTime { get; internal set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets the bytes per second being sent.
        /// </summary>
        public int BytesPerSecondSent { get; internal set; } = 0;

        /// <summary>
        /// Gets the bytes per second being received.
        /// </summary>
        public int BytesPerSecondReceived { get; internal set; } = 0;

        /// <summary>
        /// Gets whether the connection is available.
        /// </summary>
        public bool IsAvailable { get; internal set; } = true;
    }
}
