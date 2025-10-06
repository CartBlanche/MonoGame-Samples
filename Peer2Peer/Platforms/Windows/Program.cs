//-----------------------------------------------------------------------------
// PeerToPeerGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace PeerToPeer.Windows
{
	class Program
	{
		public static void Main(string[] args)
		{
			using (var game = new PeerToPeerGame())
			{
				game.Run();
			}
		}
	}
}