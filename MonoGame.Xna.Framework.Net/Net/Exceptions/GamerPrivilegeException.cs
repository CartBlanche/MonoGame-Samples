namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Exception thrown when gamer privilege operations fail.
	/// </summary>
	public class GamerPrivilegeException : Exception
    {
        public GamerPrivilegeException() : base() { }
        public GamerPrivilegeException(string message) : base(message) { }
        public GamerPrivilegeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
