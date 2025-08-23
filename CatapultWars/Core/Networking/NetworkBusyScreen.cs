//-----------------------------------------------------------------------------
// NetworkBusyScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CatapultGame
{
	/// <summary>
	/// When an asynchronous network operation (for instance searching for or joining a
	/// session) is in progress, this screen displays a busy indicator and blocks input.
	/// It monitors a Task&lt;T&gt; returned by the async call, showing the indicator while
	/// the task is running. When the task completes, it raises an event with the
	/// operation result (or null on failure), then automatically dismisses itself.
	/// Because this screen sits on top while the async operation is in progress, it
	/// captures all user input to prevent interaction with underlying screens until
	/// the operation completes.
	/// </summary>
	class NetworkBusyScreen<T> : GameScreen
	{
		readonly Task<T> task;
		readonly CancellationTokenSource cts;
		bool completionRaised;
		Texture2D gradientTexture;
		Texture2D catTexture;

		event EventHandler<OperationCompletedEventArgs> operationCompleted;
		public event EventHandler<OperationCompletedEventArgs> OperationCompleted
		{
			add { operationCompleted += value; }
			remove { operationCompleted -= value; }
		}

		/// <summary>
		/// Constructs a network busy screen for the specified asynchronous operation.
		/// Accepts a Task&lt;T&gt; representing the in-flight operation.
		/// </summary>
		public NetworkBusyScreen(Task<T> task)
		{
			this.task = task;
			this.cts = null;

			IsPopup = true;

			TransitionOnTime = TimeSpan.FromSeconds(0.1);
			TransitionOffTime = TimeSpan.FromSeconds(0.2);
		}


		/// <summary>
		/// Loads graphics content for this screen. This uses the shared ContentManager
		/// provided by the Game class, so the content will remain loaded forever.
		/// Whenever a subsequent NetworkBusyScreen tries to load this same content,
		/// it will just get back another reference to the already loaded data.
		/// </summary>
		public override void LoadContent()
		{
			ContentManager content = ScreenManager.Game.Content;

			gradientTexture = content.Load<Texture2D>("gradient");
			catTexture = content.Load<Texture2D>("cat");
		}

		/// <summary>
		/// Updates the NetworkBusyScreen, checking whether the underlying Task has
		/// completed and raising OperationCompleted when it does.
		/// </summary>
		public override void Update(GameTime gameTime, bool otherScreenHasFocus,
							bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

			// Optional: allow user to cancel (Esc or B)
			if (!completionRaised && cts != null)
			{
				var kb = Keyboard.GetState();
				if (kb.IsKeyDown(Keys.Escape))
				{
					cts.Cancel();
				}
				var gp = GamePad.GetState(PlayerIndex.One);
				if (gp.IsConnected && gp.IsButtonDown(Buttons.B))
				{
					cts.Cancel();
				}
			}

			// Has our asynchronous operation completed?
			if (!completionRaised && task != null && task.IsCompleted)
			{
				object resultObject = default(T);
				Exception error = null;
				try
				{
					// Accessing Result will throw if the task faulted/canceled.
					// We catch here and allow handlers to present error UI.
					resultObject = task.Result;
				}
				catch (Exception ex)
				{
					// Leave result as default (usually null) to signal failure to handlers.
					error = (task?.Exception?.GetBaseException()) ?? ex;
				}

				var handler = operationCompleted;
				if (handler != null)
				{
					handler(this, new OperationCompletedEventArgs(resultObject, error));
				}

				completionRaised = true;
				// Clear handlers to avoid reentry if this screen lingers during transition off
				operationCompleted = null;
				ExitScreen();
			}
		}


		/// <summary>
		/// Draws the NetworkBusyScreen.
		/// </summary>
		public override void Draw(GameTime gameTime)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont font = ScreenManager.Font;

			string message = Resources.NetworkBusy;

			const int hPad = 32;
			const int vPad = 16;

			// Center the message text in the viewport.
			Vector2 viewportSize = new Vector2(ScreenManager.BASE_BUFFER_WIDTH, ScreenManager.BASE_BUFFER_HEIGHT);
			Vector2 textSize = font.MeasureString(message);

			// Add enough room to spin a cat.
			Vector2 catSize = new Vector2(catTexture.Width);

			textSize.X = Math.Max(textSize.X, catSize.X);
			textSize.Y += catSize.Y + vPad;

			Vector2 textPosition = (viewportSize - textSize) / 2;

			// The background includes a border somewhat larger than the text itself.
			Rectangle backgroundRectangle = new Rectangle((int)textPosition.X - hPad,
							(int)textPosition.Y - vPad,
							(int)textSize.X + hPad * 2,
							(int)textSize.Y + vPad * 2);

			// Fade the popup alpha during transitions.
			Color color = Color.White * TransitionAlpha;

			spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

			// Draw the background rectangle.
			spriteBatch.Draw(gradientTexture, backgroundRectangle, color);

			// Draw the message box text.
			spriteBatch.DrawString(font, message, textPosition, color);

			// Draw the spinning cat progress indicator.
			float catRotation = (float)gameTime.TotalGameTime.TotalSeconds * 3;

			Vector2 catPosition = new Vector2(textPosition.X + textSize.X / 2,
						textPosition.Y + textSize.Y -
								catSize.Y / 2);

			spriteBatch.Draw(catTexture, catPosition, null, color, catRotation,
				catSize / 2, 1, SpriteEffects.None, 0);

			spriteBatch.End();
		}
	}
}