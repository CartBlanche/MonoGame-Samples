//-----------------------------------------------------------------------------
// OperationCompletedEventArgs.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace CatapultGame
{
    /// <summary>
    /// Custom EventArgs class used by the NetworkBusyScreen.OperationCompleted event.
    /// </summary>
    class OperationCompletedEventArgs : EventArgs
    {


        /// <summary>
        /// Gets or sets the IAsyncResult associated with
        /// the network operation that has just completed.
        /// </summary>
        public IAsyncResult AsyncResult
        {
            get { return asyncResult; }
            set { asyncResult = value; }
        }

        IAsyncResult asyncResult;





        /// <summary>
        /// Constructs a new event arguments class.
        /// </summary>
        public OperationCompletedEventArgs(IAsyncResult asyncResult)
        {
            this.asyncResult = asyncResult;
        }


    }
}
