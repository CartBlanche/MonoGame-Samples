#region File Description
//-----------------------------------------------------------------------------
// MenuEntry.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

#endregion

namespace Gorillas
{
	/// <summary>
	/// Helper class represents a single entry in a MenuScreen. By default this
	/// just draws the entry text string, but it can be customized to display menu
	/// entries in different ways. This also provides an event that will be raised
	/// when the menu entry is selected.
	/// </summary>
	class MenuEntry
	{
	#region Fields

		/// <summary>
		/// The text rendered for this entry.
		/// </summary>
		string text;

		/// <summary>
		/// Tracks a fading selection effect on the entry.
		/// </summary>
		/// <remarks>
		/// The entries transition out of the selection effect when they are deselected.
		/// </remarks>
		float selectionFade;

		/// <summary>
		/// The position at which the entry is drawn. This is set by the MenuScreen
		/// each frame in Update.
		/// </summary>
		Vector2 position;
		
		
		bool isOverMe = false;
	#endregion

	#region Properties


		/// <summary>
		/// Gets or sets the text of this menu entry.
		/// </summary>
		public string Text {
			get { return text; }
			set { text = value; }
		}


		/// <summary>
		/// Gets or sets the position at which to draw this menu entry.
		/// </summary>
		public Vector2 Position {
			get { return position; }
			set { position = value; }
		}


	#endregion

	#region Events


		/// <summary>
		/// Event raised when the menu entry is selected.
		/// </summary>
		public event EventHandler<PlayerIndexEventArgs> Selected;


		/// <summary>
		/// Method for raising the Selected event.
		/// </summary>
		protected internal virtual void OnSelectEntry (PlayerIndex playerIndex)
		{
			if (Selected != null)
				Selected (this, new PlayerIndexEventArgs (playerIndex));
		}

		/// <summary>
		/// Event raised when the menu entry is to be incremented.
		/// </summary>
		public event EventHandler<PlayerIndexEventArgs> Incremented;


		/// <summary>
		/// Method for raising the Increment event.
		/// </summary>
		protected internal virtual void OnIncrementEntry (PlayerIndex playerIndex)
		{
			if (Incremented != null)
				Incremented (this, new PlayerIndexEventArgs (playerIndex));
		}

		/// <summary>
		/// Event raised when the menu entry is to be decremented.
		/// </summary>
		public event EventHandler<PlayerIndexEventArgs> Decremented;


		/// <summary>
		/// Method for raising the Decrement event.
		/// </summary>
		protected internal virtual void OnDecrementEntry (PlayerIndex playerIndex)
		{
			if (Decremented != null)
				Decremented (this, new PlayerIndexEventArgs (playerIndex));
		}		

		/// <summary>
		/// Event raised when the menu entry is Entered over.
		/// </summary>
		public event EventHandler<MenuEntryEventArgs> MouseEntered;


		/// <summary>
		/// Method for raising the Decrement event.
		/// </summary>
		protected internal virtual void OnMouseEntered ()
		{
			if (MouseEntered != null)
				MouseEntered (this, new MenuEntryEventArgs(this));
		}		
		
		/// <summary>
		/// Event raised when the menu entry is Entered over.
		/// </summary>
		public event EventHandler<MenuEntryEventArgs> MouseExited;


		/// <summary>
		/// Method for raising the Decrement event.
		/// </summary>
		protected internal virtual void OnMouseExited ()
		{
			if (MouseExited != null)
				MouseExited (this, new MenuEntryEventArgs(this));
		}		
		
		/// <summary>
		/// Event raised when the menu entry is Entered over.
		/// </summary>
		public event EventHandler<MenuEntryEventArgs> MouseClicked;


		/// <summary>
		/// Method for raising the Decrement event.
		/// </summary>
		protected internal virtual void OnMouseClicked ()
		{
			if (MouseClicked!= null)
				MouseClicked (this, new MenuEntryEventArgs(this));
		}			
	#endregion

	#region Initialization


		/// <summary>
		/// Constructs a new menu entry with the specified text.
		/// </summary>
		public MenuEntry (string text)
		{
			this.text = text;
		}


	#endregion

	#region Update and Draw
		
		internal bool HasMouseEnteredAttached
		{
			get { return MouseEntered != null; }
		}
		
		internal bool HasMouseExitedAttached
		{
			get { return MouseExited != null; }
		}
		
		internal bool HasMouseClickedAttached
		{
			get { return MouseClicked != null; }
		}
		
		/// <summary>
		/// Updates the menu entry.
		/// </summary>
		public virtual void Update (MenuScreen screen, bool isSelected, GameTime gameTime)
		{
			// there is no such thing as a selected item on Windows Phone, so we always
			// force isSelected to be false
#if WINDOWS_PHONE
		isSelected = false;
#endif
			// Mouse and touch both arrive as a pointer in virtual game coordinates.
			InputState input = screen.ScreenManager.Input;
			Vector2 pointer = input.PointerPosition;

			Rectangle menuRect = new Rectangle((int)position.X, (int)(position.Y - GetHeight(screen) / 2f),
				(int)GetWidth(screen), (int)GetHeight(screen));

			if (screen.IsActive && menuRect.Contains((int)pointer.X, (int)pointer.Y)) {
				if (!isOverMe) {
					isOverMe = true;
					OnMouseEntered();
				}

				// A tap or click on the entry selects it.
				if (input.PointerPressed)
					OnMouseClicked();
			}
			else if (isOverMe) {
				isOverMe = false;
				OnMouseExited();
			}

			// When the menu selection changes, entries gradually fade between
			// their selected and deselected appearance, rather than instantly
			// popping to the new state.
			float fadeSpeed = (float)gameTime.ElapsedGameTime.TotalSeconds * 4;

			if (isSelected)
				selectionFade = Math.Min (selectionFade + fadeSpeed, 1);
			else
				selectionFade = Math.Max (selectionFade - fadeSpeed, 0);
		}


		/// <summary>
		/// Draws the menu entry. This can be overridden to customize the appearance.
		/// </summary>
		public virtual void Draw (MenuScreen screen, bool isSelected, GameTime gameTime)
		{
			// there is no such thing as a selected item on Windows Phone, so we always
			// force isSelected to be false
#if WINDOWS_PHONE
		isSelected = false;
#endif

			// Draw the selected entry in yellow, otherwise white.
			Color color = isSelected ? Color.Yellow : Color.White;

			// Pulsate the size of the selected menu entry.
			double time = gameTime.TotalGameTime.TotalSeconds;

			float pulsate = (float)Math.Sin (time * 6) + 1;

			float scale = 1 + pulsate * 0.05f * selectionFade;

			// Modify the alpha to fade text out during transitions.
			color *= screen.TransitionAlpha;

			// Draw text, centered on the middle of each line.
			ScreenManager screenManager = screen.ScreenManager;
			SpriteBatch spriteBatch = screenManager.SpriteBatch;
			SpriteFont font = screenManager.Font;

			Vector2 origin = new Vector2 (0, font.LineSpacing / 2);

			spriteBatch.DrawString (font, text, position, color, 0, 
				origin, scale, SpriteEffects.None, 0);
		}


		/// <summary>
		/// Queries how much space this menu entry requires.
		/// </summary>
		public virtual int GetHeight (MenuScreen screen)
		{
			return screen.ScreenManager.Font.LineSpacing;
		}


		/// <summary>
		/// Queries how wide the entry is, used for centering on the screen.
		/// </summary>
		public virtual int GetWidth (MenuScreen screen)
		{
			return (int)screen.ScreenManager.Font.MeasureString (Text).X;
		}


	#endregion
	}
}
