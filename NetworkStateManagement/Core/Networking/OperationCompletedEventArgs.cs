//-----------------------------------------------------------------------------
// OperationCompletedEventArgs.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace NetworkStateManagement
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
        /// Constructs a new event arguments class.
        /// </summary>
        public OperationCompletedEventArgs(object result)
        {
            this.Result = result;
        }
    }
}