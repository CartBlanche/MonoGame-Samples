namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Exception thrown when network session join operations fail.
	/// </summary>
	public class NetworkSessionJoinException : Exception
    {
        /// <summary>
        /// Gets the join error type.
        /// </summary>
        public NetworkSessionJoinError JoinError { get; }

        public NetworkSessionJoinException() : base() 
        {
            JoinError = NetworkSessionJoinError.Unknown;
        }

        public NetworkSessionJoinException(string message) : base(message) 
        {
            JoinError = NetworkSessionJoinError.Unknown;
        }

        public NetworkSessionJoinException(string message, Exception innerException) : base(message, innerException) 
        {
            JoinError = NetworkSessionJoinError.Unknown;
        }

        public NetworkSessionJoinException(NetworkSessionJoinError joinError) : base() 
        {
            JoinError = joinError;
        }

        public NetworkSessionJoinException(string message, NetworkSessionJoinError joinError) : base(message) 
        {
            JoinError = joinError;
        }
    }
}
