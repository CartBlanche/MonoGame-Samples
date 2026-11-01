//-----------------------------------------------------------------------------
// LobbyScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Input.Touch;


namespace CatapultGame
{
	/// <summary>
	/// The lobby screen provides a place for gamers to congregate before starting
	/// the actual gameplay. It displays a list of all the gamers in the session,
	/// and indicates which ones are currently talking. Each gamer can press a button
	/// to mark themselves as ready: gameplay will begin after everyone has done this.
	/// </summary>
	class LobbyScreen : GameScreen
	{

		NetworkSession networkSession;
		Texture2D isReadyTexture;
		Texture2D hasVoiceTexture;
		Texture2D isTalkingTexture;
		Texture2D voiceMutedTexture;

		// The points-to-win row. The host changes it; the other player is sent it.
		const int LobbySettingsMessage = 100;
		const float SettingsResendSeconds = .5f;
		static readonly Vector2 winScorePosition = new Vector2(470, 335);

		// Players are listed low on the screen, below the castle in the background art.
		const float GamerListTop = 135f;

		// On-screen buttons, so phones can ready up and leave without a keyboard.
		static readonly Rectangle readyButton = new Rectangle(470, 412, 150, 52);
		static readonly Rectangle leaveButton = new Rectangle(630, 412, 150, 52);
		Texture2D buttonPixel;
		float settingsResendTimer;

		/// <summary>
		/// Constructs a new lobby screen.
		/// </summary>
		public LobbyScreen(NetworkSession networkSession)
		{
			this.networkSession = networkSession;
			MatchSettings.WinScore = MatchSettings.DefaultWinScore;
			EnabledGestures = GestureType.Tap;

			TransitionOnTime = TimeSpan.FromSeconds(0.5);
			TransitionOffTime = TimeSpan.FromSeconds(0.5);

		}

		/// <summary>
		/// Loads graphics content used by the lobby screen.
		/// </summary>
		public override void LoadContent()
		{
			ContentManager content = ScreenManager.Game.Content;

			isReadyTexture = content.Load<Texture2D>("chat_ready");
			hasVoiceTexture = content.Load<Texture2D>("chat_able");
			isTalkingTexture = content.Load<Texture2D>("chat_talking");
			voiceMutedTexture = content.Load<Texture2D>("chat_mute");

			buttonPixel = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
			buttonPixel.SetData(new[] { Color.White });
		}

		/// <summary>
		/// Updates the lobby screen.
		/// </summary>
		public override void Update(GameTime gameTime, bool otherScreenHasFocus,
							bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

			if (!IsExiting && networkSession.SessionState == NetworkSessionState.Lobby)
				SyncMatchSettings((float)gameTime.ElapsedGameTime.TotalSeconds);

			if (!IsExiting)
			{
				if (networkSession.SessionState == NetworkSessionState.Playing)
				{
					// Check if we should leave the lobby and begin gameplay.
					// We pass null as the controlling player, because the networked
					// gameplay screen accepts input from any local players who
					// are in the session, not just a single controlling player.
					LoadingScreen.Load(ScreenManager, true, null,
					new GameplayScreen());
				}
				else if (networkSession.IsHost && networkSession.IsEveryoneReady && networkSession.RemoteGamers.Count > 0)
				{
					// The host checks whether everyone has marked themselves
					// as ready, and starts the game in response.
					networkSession.StartGame();
				}
			}
		}

		/// <summary>
		/// Handles user input for all the local gamers in the session. Unlike most
		/// screens, which use the InputState class to combine input data from all
		/// gamepads, the lobby needs to individually mark specific players as ready,
		/// so it loops over all the local gamers and reads their inputs individually.
		/// </summary>
		public override void HandleInput(InputState input)
		{
			if (Tapped(input, leaveButton))
			{
				NetworkSessionComponent.LeaveSession(ScreenManager, PlayerIndex.One);
				return;
			}

			if (Tapped(input, readyButton))
			{
				foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
					gamer.IsReady = !LocalReady();
				return;
			}

			if (networkSession.IsHost && WinScoreChangeRequested(input))
			{
				MatchSettings.WinScore = MatchSettings.NextWinScore(MatchSettings.WinScore);
				settingsResendTimer = 0f;
			}

			foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
			{
				PlayerIndex playerIndex = (PlayerIndex)gamer.SignedInGamer.PlayerIndex;

				PlayerIndex unwantedOutput;

				if (input.IsMenuSelect(playerIndex, out unwantedOutput))
				{
					HandleMenuSelect(gamer);
				}
				else if (input.IsMenuCancel(playerIndex, out unwantedOutput))
				{
					HandleMenuCancel(gamer);
				}
			}
		}

		/// <summary>
		/// Host sends the points-to-win to everyone; everyone else reads it.
		/// </summary>
		void SyncMatchSettings(float elapsed)
		{
			if (networkSession.IsHost)
			{
				settingsResendTimer -= elapsed;
				if (settingsResendTimer > 0f || networkSession.LocalGamers.Count == 0)
					return;

				settingsResendTimer = SettingsResendSeconds;
				using (var writer = new PacketWriter())
				{
					writer.Write(LobbySettingsMessage);
					writer.Write(MatchSettings.WinScore);
					networkSession.LocalGamers[0].SendData(writer, SendDataOptions.ReliableInOrder);
				}
				return;
			}

			PacketReader reader = new PacketReader();
			foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
			{
				while (gamer.IsDataAvailable)
				{
					NetworkGamer sender;
					gamer.ReceiveData(reader, out sender);
					if (sender.IsLocal)
						continue;

					if (reader.ReadInt32() == LobbySettingsMessage)
						MatchSettings.WinScore = reader.ReadInt32();
				}
			}
		}

		Rectangle WinScoreBounds()
		{
			Vector2 size = ScreenManager.Font.MeasureString(WinScoreText());
			return new Rectangle((int)winScorePosition.X - 10, (int)winScorePosition.Y - 8,
				(int)size.X + 20, (int)size.Y + 16);
		}

		string WinScoreText()
		{
			return networkSession.IsHost
				? "Points to win:  < " + MatchSettings.WinScore + " >"
				: "Points to win:  " + MatchSettings.WinScore;
		}

		/// <summary>
		/// A tap or click on the row, or an arrow key, steps the host through 5, 10 and 15.
		/// </summary>
		bool WinScoreChangeRequested(InputState input)
		{
			PlayerIndex ignored;
			if (input.IsNewKeyPress(Keys.Left, null, out ignored) || input.IsNewKeyPress(Keys.Right, null, out ignored) ||
				input.IsNewKeyPress(Keys.Up, null, out ignored) || input.IsNewKeyPress(Keys.Down, null, out ignored))
				return true;

			return Tapped(input, WinScoreBounds());
		}

		/// <summary>
		/// True when a tap or mouse click landed inside the rectangle.
		/// </summary>
		static bool Tapped(InputState input, Rectangle bounds)
		{
			if (input.MouseGesture.HasFlag(MouseGestureType.LeftClick) &&
				bounds.Contains(new Point((int)input.CurrentMousePosition.X, (int)input.CurrentMousePosition.Y)))
				return true;

			foreach (GestureSample gesture in input.Gestures)
			{
				if (gesture.GestureType == GestureType.Tap &&
					bounds.Contains(new Point((int)gesture.Position.X, (int)gesture.Position.Y)))
					return true;
			}
			return false;
		}

		bool LocalReady()
		{
			return networkSession.LocalGamers.Count > 0 && networkSession.LocalGamers[0].IsReady;
		}

		void DrawButton(SpriteBatch spriteBatch, SpriteFont font, Rectangle bounds, string text, Color accent, float offsetX)
		{
			bounds.X += (int)offsetX;
			spriteBatch.Draw(buttonPixel, bounds, Color.Black * (.6f * TransitionAlpha));

			Color edge = accent * TransitionAlpha;
			spriteBatch.Draw(buttonPixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), edge);
			spriteBatch.Draw(buttonPixel, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), edge);
			spriteBatch.Draw(buttonPixel, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), edge);
			spriteBatch.Draw(buttonPixel, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), edge);

			Vector2 size = font.MeasureString(text);
			spriteBatch.DrawString(font, text, new Vector2(bounds.Center.X - size.X / 2f, bounds.Center.Y - size.Y / 2f), edge);
		}

		/// <summary>
		/// Handle MenuSelect inputs by marking ourselves as ready.
		/// </summary>
		void HandleMenuSelect(LocalNetworkGamer localGamer)
		{
			if (!localGamer.IsReady)
			{
				localGamer.IsReady = true;
			}
			else if (localGamer.IsHost && networkSession.RemoteGamers.Count > 0)
			{
				// The host has an option to force starting the game, even if not
				// everyone has marked themselves ready. If they press select twice
				// in a row, the first time marks the host ready, then the second
				// time we ask if they want to force start.
				MessageBoxScreen messageBox = new MessageBoxScreen(
						Resources.ConfirmForceStartGame);

				messageBox.Accepted += ConfirmStartGameMessageBoxAccepted;

				ScreenManager.AddScreen(messageBox, (PlayerIndex?)localGamer.SignedInGamer.PlayerIndex);
			}
		}

		/// <summary>
		/// Event handler for when the host selects ok on the "are you sure
		/// you want to start even though not everyone is ready" message box.
		/// </summary>
		void ConfirmStartGameMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
		{
			if (networkSession.SessionState == NetworkSessionState.Lobby)
			{
				networkSession.StartGame();
			}
		}

		/// <summary>
		/// Handle MenuCancel inputs by clearing our ready status, or if it is
		/// already clear, prompting if the user wants to leave the session.
		/// </summary>
		void HandleMenuCancel(LocalNetworkGamer localGamer)
		{
			if (localGamer.IsReady)
			{
				localGamer.IsReady = false;
			}
			else
			{
				PlayerIndex playerIndex = (PlayerIndex)localGamer.SignedInGamer.PlayerIndex;

				NetworkSessionComponent.LeaveSession(ScreenManager, playerIndex);
			}
		}

		/// <summary>
		/// Draws the lobby screen.
		/// </summary>
		public override void Draw(GameTime gameTime)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont font = ScreenManager.Font;

			Vector2 position = new Vector2(50, GamerListTop);

			// Make the lobby slide into place during transitions.
			float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

			if (ScreenState == ScreenState.TransitionOn)
				position.X -= transitionOffset * 256;
			else
				position.X += transitionOffset * 512;

			// How far the slide-in transition has pushed everything sideways.
			float shiftX = position.X - 50;

			spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

			// Draw all the gamers in the session.
			int gamerCount = 0;

			foreach (NetworkGamer gamer in networkSession.AllGamers)
			{
				DrawGamer(gamer, position);

				// Advance to the next screen position, wrapping into two
				// columns if there are more than 8 gamers in the session.
				if (++gamerCount == 8)
				{
					position.X += 433;
					position.Y = 150;
				}
				else
					position.Y += ScreenManager.Font.LineSpacing;
			}

			// Starting alone just drops the host back to the menu, so say what we are waiting for.
			if (networkSession.IsHost && networkSession.RemoteGamers.Count == 0)
			{
				spriteBatch.DrawString(font, "Waiting for another player to join...",
					new Vector2(position.X + 34 * 2, position.Y + font.LineSpacing / 2f),
					Color.Gray * TransitionAlpha, 0f, Vector2.Zero, .8f, SpriteEffects.None, 0f);
			}

			// Draw the points to win, which the host can change.
			Vector2 rowPosition = winScorePosition;
			rowPosition.X += shiftX;
			spriteBatch.DrawString(font, WinScoreText(), rowPosition,
				(networkSession.IsHost ? Color.Orange : Color.White) * TransitionAlpha);
			if (networkSession.IsHost)
			{
				spriteBatch.DrawString(font, "Tap or click to change",
					rowPosition + new Vector2(0, font.LineSpacing), Color.Gray * TransitionAlpha, 0f,
					Vector2.Zero, .6f, SpriteEffects.None, 0f);
			}

			DrawButton(spriteBatch, font, readyButton, LocalReady() ? "Not Ready" : "Ready",
				LocalReady() ? Color.Orange : Color.Lime, shiftX);
			DrawButton(spriteBatch, font, leaveButton, "Leave", Color.White, shiftX);

			// Draw the screen title.
			string title = Resources.Lobby;

			Vector2 titlePosition = new Vector2(100, 80);
			Vector2 titleOrigin = font.MeasureString(title) / 2;
			Color titleColor = new Color(192, 192, 192) * TransitionAlpha;
			float titleScale = 1.25f;

			titlePosition.Y -= transitionOffset * 100;

			spriteBatch.DrawString(font, title, titlePosition, titleColor, 0,
				titleOrigin, titleScale, SpriteEffects.None, 0);

			spriteBatch.End();
		}

		/// <summary>
		/// Helper draws the gamertag and status icons for a single NetworkGamer.
		/// </summary>
		void DrawGamer(NetworkGamer gamer, Vector2 position)
		{
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont font = ScreenManager.Font;

			Vector2 iconWidth = new Vector2(34, 0);
			Vector2 iconOffset = new Vector2(0, 12);

			Vector2 iconPosition = position + iconOffset;

			// Draw the "is ready" icon.
			if (gamer.IsReady)
			{
				spriteBatch.Draw(isReadyTexture, iconPosition,
				Color.Lime * TransitionAlpha);
			}

			iconPosition += iconWidth;

			// Draw the "is muted", "is talking", or "has voice" icon.
			if (gamer.IsMutedByLocalUser)
			{
				spriteBatch.Draw(voiceMutedTexture, iconPosition,
				Color.Red * TransitionAlpha);
			}
			else if (gamer.IsTalking)
			{
				spriteBatch.Draw(isTalkingTexture, iconPosition,
				Color.Yellow * TransitionAlpha);
			}
			else if (gamer.HasVoice)
			{
				spriteBatch.Draw(hasVoiceTexture, iconPosition,
				Color.White * TransitionAlpha);
			}

			// Draw the gamertag, normally in white, but yellow for local players.
			string text = gamer.Gamertag;

			if (gamer.IsHost)
				text += Resources.HostSuffix;

			Color color = (gamer.IsLocal) ? Color.Yellow : Color.White;

			spriteBatch.DrawString(font, text, position + iconWidth * 2,
				color * TransitionAlpha);
		}
	}
}