namespace Microsoft.Xna.Framework.Net
{
	/// <summary>
	/// Wrapper for async operations to provide XNA-compatible IAsyncResult interface.
	/// </summary>
	internal class AsyncResultWrapper<T> : IAsyncResult
    {
        private readonly Task<T> task;
        private readonly object asyncState;

        public AsyncResultWrapper(Task<T> task, AsyncCallback callback, object asyncState)
        {
            this.task = task;
            this.asyncState = asyncState;

            if (callback != null)
            {
                task.ContinueWith(t => callback(this));
            }
        }

        public object AsyncState => asyncState;

        public System.Threading.WaitHandle AsyncWaitHandle => ((IAsyncResult)task).AsyncWaitHandle;

        public bool CompletedSynchronously => task.IsCompletedSuccessfully;

        public bool IsCompleted => task.IsCompleted;

        public T GetResult()
        {
            return task.Result;
        }
    }
}
