#region File Description
//-----------------------------------------------------------------------------
// PlayerIndexEventArgs.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;

#endregion

namespace Gorillas
{
	/// <summary>
	/// Custom event argument which includes the index of the player who
	/// triggered the event. This is used by the MenuEntry.Selected event.
	/// </summary>
	class MenuEntryEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MenuEntryEventArgs (MenuEntry menuEntry)
		{
			this.menuEntry = menuEntry;
		}


		/// <summary>
		/// Gets the index of the player who triggered this event.
		/// </summary>
		public MenuEntry MenuEntry {
			get { return menuEntry; }
		}

		MenuEntry menuEntry;
	}
}
