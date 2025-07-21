namespace Microsoft.Xna.Framework.GamerServices
{
	/// <summary>
	/// Mock implementation of IAsyncResult for testing.
	/// </summary>
	internal class MockAsyncResult : IAsyncResult
    {
        public MockAsyncResult(object asyncState, bool isCompleted)
        {
            AsyncState = asyncState;
            IsCompleted = isCompleted;
            CompletedSynchronously = isCompleted;
        }

        public object AsyncState { get; }
        public WaitHandle AsyncWaitHandle => new ManualResetEvent(IsCompleted);
        public bool CompletedSynchronously { get; }
        public bool IsCompleted { get; }
    }
}
