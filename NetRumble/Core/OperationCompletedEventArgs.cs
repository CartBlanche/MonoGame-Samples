//-----------------------------------------------------------------------------
// OperationCompletedEventArgs.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace NetRumble
{
    /// <summary>
    /// Custom EventArgs class used by the NetworkBusyScreen.OperationCompleted event.
    /// </summary>
    class OperationCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the result of the network operation that has just completed.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Gets or sets the exception that caused the operation to fail, if any.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Constructs a new event arguments class.
        /// </summary>
        public OperationCompletedEventArgs(object result)
        {
            this.Result = result;
        }

        /// <summary>
        /// Constructs a new event arguments class with an optional exception.
        /// </summary>
        public OperationCompletedEventArgs(object result, Exception exception)
        {
            this.Result = result;
            this.Exception = exception;
        }
    }
}
