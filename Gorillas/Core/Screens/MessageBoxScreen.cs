#region File Description
//-----------------------------------------------------------------------------
// MessageBoxScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace Gorillas
{
	/// <summary>
	/// A popup message box screen, used to display "are you sure?"
	/// confirmation messages.
	/// </summary>
	class MessageBoxScreen : GameScreen
	{
	#region Fields

		string message;
		Texture2D gradientTexture;

		// Confirmation boxes get tappable OK / Cancel buttons (keyboard and gamepad still work).
		bool hasButtons;
		Rectangle okButton;
		Rectangle cancelButton;
		const int buttonHeight = 30;
		const int buttonWidth = 90;
		const int buttonSpacing = 20;

	#endregion

	#region Events

		public event EventHandler<PlayerIndexEventArgs> Accepted;
		public event EventHandler<PlayerIndexEventArgs> Cancelled;

	#endregion

	#region Initialization


		/// <summary>
		/// Constructor automatically includes the standard "A=ok, B=cancel"
		/// usage text prompt.
		/// </summary>
		public MessageBoxScreen (string message)
		: this(message, true)
		{
		}


		/// <summary>
		/// Constructor lets the caller specify whether to include the standard
		/// "A=ok, B=cancel" usage text prompt.
		/// </summary>
		public MessageBoxScreen (string message,bool includeUsageText)
			{
			this.message = message;
			hasButtons = includeUsageText;

			IsPopup = true;

			TransitionOnTime = TimeSpan.FromSeconds (0.2);
			TransitionOffTime = TimeSpan.FromSeconds (0.2);
		}


		/// <summary>
		/// Loads graphics content for this screen. This uses the shared ContentManager
		/// provided by the Game class, so the content will remain loaded forever.
		/// Whenever a subsequent MessageBoxScreen tries to load this same content,
		/// it will just get back another reference to the already loaded data.
		/// </summary>
		public override void LoadContent ()
		{
			ContentManager content = ScreenManager.Game.Content;

			gradientTexture = content.Load<Texture2D> ("gradient");
		}


	#endregion

	#region Handle Input


		/// <summary>
		/// Responds to user input, accepting or cancelling the message box.
		/// </summary>
		public override void HandleInput (InputState input)
		{
			PlayerIndex playerIndex;

			// Tap / click: on the OK or Cancel button for confirmations, anywhere for plain notices.
			if (input.PointerPressed) {
				Point pointer = new Point ((int)input.PointerPosition.X, (int)input.PointerPosition.Y);
				if (!hasButtons || okButton.Contains (pointer)) {
					if (Accepted != null)
						Accepted (this, new PlayerIndexEventArgs (PlayerIndex.One));
					ExitScreen ();
					return;
				}
				if (cancelButton.Contains (pointer)) {
					if (Cancelled != null)
						Cancelled (this, new PlayerIndexEventArgs (PlayerIndex.One));
					ExitScreen ();
					return;
				}
			}

			// We pass in our ControllingPlayer, which may either be null (to
			// accept input from any player) or a specific index. If we pass a null
			// controlling player, the InputState helper returns to us which player
			// actually provided the input. We pass that through to our Accepted and
			// Cancelled events, so they can tell which player triggered them.
			if (input.IsMenuSelect (ControllingPlayer, out playerIndex)) {
				// Raise the accepted event, then exit the message box.
				if (Accepted != null)
					Accepted (this, new PlayerIndexEventArgs (playerIndex));

				ExitScreen ();
			} else if (input.IsMenuCancel (ControllingPlayer, out playerIndex)) {
				// Raise the cancelled event, then exit the message box.
				if (Cancelled != null)
					Cancelled (this, new PlayerIndexEventArgs (playerIndex));

				ExitScreen ();
			}
		}


	#endregion

	#region Draw


		/// <summary>
		/// Draws the message box.
		/// </summary>
		public override void Draw (GameTime gameTime)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont font = ScreenManager.Font;

			// Darken down any other screens that were drawn beneath the popup.
			ScreenManager.FadeBackBufferToBlack (TransitionAlpha * 2 / 3);

			// Center the message text in the viewport.
			Viewport viewport = ScreenManager.BaseViewport;
			Vector2 viewportSize = new Vector2 (viewport.Width, viewport.Height);
			Vector2 textSize = font.MeasureString (message);
			Vector2 textPosition = (viewportSize - textSize) / 2;

			// The background includes a border somewhat larger than the text itself.
			const int hPad = 32 ; 
			const int vPad = 16 ; 

			int extra = hasButtons ? buttonHeight + vPad : 0;
			textPosition.Y -= extra / 2f;

			Rectangle backgroundRectangle = new Rectangle ((int)textPosition.X - hPad,
							(int)textPosition.Y - vPad,
							(int)textSize.X + hPad * 2,
							(int)textSize.Y + vPad * 2 + extra);

			int buttonY = (int)(textPosition.Y + textSize.Y + vPad / 2);
			int centreX = backgroundRectangle.Center.X;
			okButton = new Rectangle (centreX - buttonSpacing / 2 - buttonWidth, buttonY, buttonWidth, buttonHeight);
			cancelButton = new Rectangle (centreX + buttonSpacing / 2, buttonY, buttonWidth, buttonHeight);

			// Fade the popup alpha during transitions.
			Color color = Color.White * TransitionAlpha;

			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

			// Draw the background rectangle.
			spriteBatch.Draw (gradientTexture, backgroundRectangle, color);

			// Draw the message box text.
			spriteBatch.DrawString (font, message, textPosition, color);

			if (hasButtons) {
				DrawButton (spriteBatch, font, okButton, "OK", color);
				DrawButton (spriteBatch, font, cancelButton, "Cancel", color);
			}

			spriteBatch.End ();
		}


		void DrawButton (SpriteBatch spriteBatch, SpriteFont font, Rectangle bounds, string label, Color color)
		{
			spriteBatch.Draw (gradientTexture, bounds, color);
			Vector2 size = font.MeasureString (label);
			spriteBatch.DrawString (font, label,
				new Vector2 (bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color);
		}

	#endregion
	}
}
