#region File Description
//-----------------------------------------------------------------------------
// Program.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.IO;

#endregion

namespace Blackjack
{
	class Program
	{
		private static BlackjackGame game;
		
		/// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
		{
			game = new BlackjackGame();
			game.Run ();
		}
	}
}

