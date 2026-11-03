#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

using Tile_Engine;

using Microsoft.Xna.Framework.GamerServices;

using Gorillas;
#endregion

namespace Gorillas
{
	/// <summary>
	/// This screen implements the actual game logic. It is just a
	/// placeholder to get the idea across: you'll probably want to
	/// put some more interesting gameplay in here!
	/// </summary>
	public class GameplayScreen : GameScreen
	{
	#region Fields
		
		// Networking
		NetworkSession networkSession;
		PacketReader packetReader = new PacketReader();
		
		enum MessageType : byte
		{
			NewGame = 1,
			ThrowBanana = 2,
		}
		
		ContentManager content;
		SpriteFont gameFont;
		Vector2 playerPosition = new Vector2 (100, 100);
		Vector2 enemyPosition = new Vector2 (100, 100);
		Random random = new Random ();
		float pauseAlpha;
		internal CityScape cityScape;
		internal Gorilla gorilla1;
		internal Gorilla gorilla2;
		internal Banana banana;
		HeadsUpTrajectory headsUp;
		Score score;
		GameOver gameOver;
		GameState gameState = GameState.Playing;

		// Death timer variables
		float deathTimer = 0.0f;
		float deathDelay = 5.0f;	

		// Switch player variables
		float switchTimer = 0.0f;
		float switchDelay = 2.0f;

		// Game over variables
		float gameOverTimer = 0.0f;
		float gameOverDelay = 5.0f;	
		internal PlayerManager players;
		int defaultLives = 3;

	#endregion

	#region Initialization


		/// <summary>
		/// Constructor.
		/// </summary>
		public GameplayScreen (NetworkSession networkSession)
		{
			this.networkSession = networkSession;
			TransitionOnTime = TimeSpan.FromSeconds (1.5);
			TransitionOffTime = TimeSpan.FromSeconds (0.5);
			
			defaultLives = OptionsMenuScreen.numberOfLives;
			
			if (this.networkSession != null) {
				defaultLives = networkSession.SessionProperties.TryGetValue (nameof (SessionProperty.ScoreToWin), out object lives) && lives != null
					? Convert.ToInt32 (lives)
					: defaultLives;
			}
			
		}

		private ContentManager Content {
			get {
				if (content == null)
					content = new ContentManager (ScreenManager.Game.Services, "Content");				
				return content;
			}
		}

		/// <summary>
		/// Load graphics content for the game.
		/// </summary>
		public override void LoadContent ()
		{
			if (content == null)
				content = new ContentManager (ScreenManager.Game.Services, "Content");

			gameFont = content.Load<SpriteFont> ("Fonts/gamefont");

			// A real game would probably have more content than this sample, so
			// it would take longer to load. We simulate that by delaying for a
			// while, giving you a chance to admire the beautiful loading screen.
			//Thread.Sleep (1000);

			// once the load has finished, we use ResetElapsedTime to tell the game's
			// timing mechanism that we have just finished a very long frame, and that
			// it should not try to catch up.
			ScreenManager.Game.ResetElapsedTime ();

			cityScape = new CityScape (this);

			Camera.WorldRectangle = new Rectangle (0, 0, 640, 350);
			Camera.Position = Vector2.Zero;
			Camera.ViewPortWidth = 640;
			Camera.ViewPortHeight = 350;

			banana = new Banana (Content, this);
			
			gorilla1 = new Gorilla (Content, Gorilla.GorillaPlayerClass.Player1, banana);
			gorilla1.WorldLocation = new Vector2 (0, 0);
			gorilla1.Name = Gamer.SignedInGamers[0].Gamertag;
			if (networkSession != null) { // && !networkSession.IsHost) {
				if (networkSession.IsHost) {
					gorilla1.Name = networkSession.LocalGamers[0].Gamertag + " (host)";
				}
				else {
					gorilla1.IsRemote = true;
					gorilla1.Name = networkSession.RemoteGamers[0].Gamertag;
				}
			}

			gorilla2 = new Gorilla (Content, Gorilla.GorillaPlayerClass.Player2, banana);
			gorilla2.WorldLocation = new Vector2 (200, 100);
			
			gorilla2.Name = "Killa Gorilla";
			if (networkSession == null || networkSession.AllGamers.Count < 2) {
				// the AI plays Player 2 when asked to, when a network host is alone in the session
				// (it starts at once if nobody has joined), or when there is no second signed-in gamer
				if (OptionsMenuScreen.currentPlayerMode == 0 || networkSession != null || SignedInGamer.SignedInGamers.Count < 2) {
					gorilla2.IsAI = true;
				}
				else {
					gorilla2.Name = SignedInGamer.SignedInGamers[1].Gamertag;
				}
			}
			else {
				if (networkSession.IsHost) {
					gorilla2.IsRemote = true;
					gorilla2.Name = networkSession.RemoteGamers[0].Gamertag;
				}
				else {
					gorilla2.Name = networkSession.LocalGamers[0].Gamertag;
				}
			}
			
			if (OptionsMenuScreen.currentEntryMode == OptionsMenuScreen.EntryMode.KeyBoard)
				headsUp = new HeadsUpTrajectory (this, networkSession, true);
			else
				headsUp = new HeadsUpTrajectory (this, networkSession, false);
			headsUp.Visible = true;

			players = new PlayerManager (this, banana, headsUp);
			players.AddGorilla (gorilla1);
			players.AddGorilla (gorilla2);
			players.NewGame (OptionsMenuScreen.numberOfLives);

			score = new Score (this, players);
			score.Visible = true;

			gameOver = new GameOver (this);
			gameOver.Visible = false;
			
			SoundManager.Initialize(Content);
			SoundManager.GameStart();
			CreateCityScape ();			
		}


		/// <summary>
		/// Unload graphics content used by the game.
		/// </summary>
		public override void UnloadContent ()
		{
			content.Unload ();
		}

		/// <summary>
		/// The logic for deciding whether the game is paused depends on whether
		/// this is a networked or single player game. If we are in a network session,
		/// we should go on updating the game even when the user tabs away from us or
		/// brings up the pause menu, because even though the local player is not
		/// responding to input, other remote players may not be paused. In single
		/// player modes, however, we want everything to pause if the game loses focus.
		/// </summary>
		new bool IsActive {
			get {
				if (networkSession == null) {
					// Pause behavior for single player games.
					return base.IsActive;
				} else {
					// Pause behavior for networked games.
					return !IsExiting;
				}
			}
		}

	#endregion

	#region Update and Draw

		/// <summary>
		/// Updates the state of the game. This method checks the GameScreen.IsActive
		/// property, so the game will stop updating when the pause menu is active,
		/// or if you tab away to a different application.
		/// </summary>
		public override void Update (GameTime gameTime, bool otherScreenHasFocus, 
							bool coveredByOtherScreen)
		{
			base.Update (gameTime, otherScreenHasFocus, false);

			// Gradually fade in or out depending on whether we are covered by the pause screen.
			if (coveredByOtherScreen)
				pauseAlpha = Math.Min (pauseAlpha + 1f / 32, 1);
			else
				pauseAlpha = Math.Max (pauseAlpha - 1f / 32, 0);

			if (IsActive) {
				
				UpdateNetworkSession();
				
				float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;				

				if (gameState == GameState.Playing || 
					gameState == GameState.PlayerSwitch) {
					
					if (CheckInsideBoundaries ()) {
						CheckForCollisions ();
					}

					cityScape.Update (gameTime);

					gorilla1.Update (gameTime);
					gorilla2.Update (gameTime);

					banana.Update (gameTime);

					headsUp.Update (gameTime);

					score.Update (gameTime);
				}	

				if (gameState == GameState.PlayerDead) {

					gorilla1.Update (gameTime);
					gorilla2.Update (gameTime);
					deathTimer += elapsed;

					if (deathTimer > deathDelay) {
						CreateCityScape ();
						gameState = GameState.Playing;
					}
				}

				if (gameState == GameState.PlayerSwitch) {
					switchTimer += elapsed;
					if (switchTimer > switchDelay) {
						gameState = GameState.Playing;
						players.SwitchTurns ();
					}
				}

				if (gameState == GameState.PlayAgain) {
					players.NewGame (defaultLives);					
					CreateCityScape ();
					gameState = GameState.Playing;
					gameOver.Visible = false;
					gameOverTimer = 0;
					
					// This is here so that we do not get 
					// extra key strokes in HeadsUpTrajectory
					Thread.Sleep(500);  // This needs to be looked into
								
				}	

				if (gorilla1.LivesRemaining == 0 || gorilla2.LivesRemaining == 0) {
					gameState = GameState.GameOver;
					gameOver.Visible = true;
					gameOver.Update (gameTime);
					
				}


				if (gameState == GameState.GameOver) {
					gorilla1.Update (gameTime);
					gorilla2.Update (gameTime);

					gameOverTimer += elapsed;

					if (gameOverTimer > gameOverDelay) {
						gameState = GameState.PlayAgain;
						gameOver.Visible = false;
						gameOverTimer = 0;
								
						ScreenManager.AddScreen (new GameOverMenuScreen (), ControllingPlayer);
					}				
				}
			}
			// If we are in a network game, check if we should return to the lobby.
			if ((networkSession != null) && !IsExiting) {
				if (networkSession.SessionState == NetworkSessionState.Lobby) {
					LoadingScreen.Load(ScreenManager, true, null,
						new BackgroundScreen(),
						new LobbyScreen(networkSession));
				}
			}
			
		}
		
		void UpdateNetworkSession () 
		{
			if (networkSession == null)
				return;
			
			// Pump the underlying session object.
			networkSession.Update ();

			// Make sure the session has not ended.
			if (networkSession == null)
				return; 
			
			// Read any packets telling us the positions of remotely controlled tanks.
			foreach (LocalNetworkGamer gamer in networkSession.LocalGamers) {
				ReadIncomingPackets (gamer);
			}			
		}
		
		/// <summary>
		/// Helper for reading incoming network packets.
		/// </summary>
		void ReadIncomingPackets (LocalNetworkGamer gamer)
		{
			// Keep reading as long as incoming packets are available.
			while (gamer.IsDataAvailable) {
				NetworkGamer sender;

				// Read a single packet from the network.
				gamer.ReceiveData (packetReader, out sender);

				// Discard packets sent by local gamers: we already know their state!
				if (sender.IsLocal)
					continue;
				
				MessageType msgType = (MessageType)packetReader.ReadInt32();
				switch (msgType) {
				case MessageType.NewGame:
					ReceiveNewNetworkedGame();
					break;
				case MessageType.ThrowBanana:
					ReadBananaThrown();
					break;
				}
			}
		}
		
		void ReceiveNewNetworkedGame() 
		{

			cityScape.ReadCityScape(packetReader);
			
			Vector2 position = packetReader.ReadVector2();
			gorilla1.Position = position;
			position = packetReader.ReadVector2();
			gorilla2.Position = position;
			score.ScoreBoardBackground = CityScapeBackground.backGroundColor;
			
			players.RevivePlayers ();			
			players.SwitchTurns ();
			
		}
		
		/// <summary>
		/// Lets the game respond to player input. Unlike the Update method,
		/// this will only be called when the gameplay screen is active.
		/// </summary>
		public override void HandleInput (InputState input)
		{
			if (input == null)
				throw new ArgumentNullException ("input");

			if (ControllingPlayer.HasValue) {
				// In single player games, handle input for the controlling player.
				HandlePlayerInput (input, ControllingPlayer.Value);
			} else if (networkSession != null) {
				// In network game modes, handle input for all the
				// local players who are participating in the session.
				foreach (LocalNetworkGamer gamer in networkSession.LocalGamers) {
					if (!HandlePlayerInput (input, gamer.SignedInGamer.PlayerIndex))
						break;
				}
			}
			
		}

		/// <summary>
		/// Handles input for the specified player. In local game modes, this is called
		/// just once for the controlling player. In network modes, it can be called
		/// more than once if there are multiple profiles playing on the local machine.
		/// Returns true if we should continue to handle input for subsequent players,
		/// or false if this player has paused the game.
		/// </summary>
		bool HandlePlayerInput (InputState input, PlayerIndex playerIndex)
		{
			// Look up inputs for the specified player profile.
			KeyboardState keyboardState = input.CurrentKeyboardStates [(int)playerIndex];
			KeyboardState lastOne = input.LastKeyboardStates [(int)playerIndex];
			GamePadState gamePadState = input.CurrentGamePadStates [(int)playerIndex];

			// The game pauses either if the user presses the pause button, or if
			// they unplug the active gamepad. This requires us to keep track of
			// whether a gamepad was ever plugged in, because we don't want to pause
			// on PC if they are playing with a keyboard and have no gamepad at all!
			bool gamePadDisconnected = !gamePadState.IsConnected && 
						input.GamePadWasConnected [(int)playerIndex];

			if (input.IsPauseGame (playerIndex) || gamePadDisconnected) {
				ScreenManager.AddScreen (new PauseMenuScreen (networkSession), playerIndex);
				return false;
			}

			// Touch players have no Esc key or gamepad Start button, so they get an on-screen pause
			// button. Handle it before the aim code so pressing it never starts a drag.
			if (ShowPauseButton && input.PointerPressed &&
				pauseButton.Contains ((int)input.PointerPosition.X, (int)input.PointerPosition.Y)) {
				ScreenManager.AddScreen (new PauseMenuScreen (networkSession), playerIndex);
				return false;
			}

			headsUp.HandlePlayerInput(input, ControllingPlayer, playerIndex);
			
			return true;
		}

		// On-screen pause button, shown on touch devices only. It sits just right of the sun and is
		// vertically centred on it (the sun is drawn at y=10 and is 31 units tall, so its centre is y=25.5).
		const int pauseButtonSize = 26;
		static readonly Rectangle pauseButton = new Rectangle (ScreenManager.BaseWidth / 2 + 40,
			(int)(10 + 31 / 2f - pauseButtonSize / 2f), pauseButtonSize, pauseButtonSize);

		static bool ShowPauseButton {
			get { return OperatingSystem.IsAndroid () || OperatingSystem.IsIOS (); }
		}

		void DrawPauseButton (SpriteBatch spriteBatch)
		{
			if (!ShowPauseButton)
				return;

			Texture2D pixel = ScreenManager.BlankTexture;
			Color border = Color.Yellow;
			Color fill = Color.Black * 0.55f;

			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

			// Blocky retro frame with a "pause" glyph (two bars).
			spriteBatch.Draw (pixel, pauseButton, border);
			spriteBatch.Draw (pixel, new Rectangle (pauseButton.X + 2, pauseButton.Y + 2,
				pauseButton.Width - 4, pauseButton.Height - 4), Color.Black);
			spriteBatch.Draw (pixel, new Rectangle (pauseButton.X + 2, pauseButton.Y + 2,
				pauseButton.Width - 4, pauseButton.Height - 4), fill);
			spriteBatch.Draw (pixel, new Rectangle (pauseButton.X + 8, pauseButton.Y + 7, 4, 12), border);
			spriteBatch.Draw (pixel, new Rectangle (pauseButton.X + 15, pauseButton.Y + 7, 4, 12), border);

			spriteBatch.End ();
		}


		/// <summary>
		/// Draws the gameplay screen.
		/// </summary>
		public override void Draw (GameTime gameTime)
		{
			// This game has a blue background. Why? Because!
			ScreenManager.GraphicsDevice.Clear (ClearOptions.Target, 
						Color.CornflowerBlue, 0, 0);

			// Our player and enemy are both actually just text strings.
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

			if (gameState == GameState.Playing || 
				gameState == GameState.PlayerDead || 
				gameState == GameState.PlayerSwitch || 
				gameState == GameState.GameOver || 
				gameState == GameState.PlayAgain) {

				cityScape.Draw (gameTime);

				spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

				gorilla1.Draw (spriteBatch);
				gorilla2.Draw (spriteBatch);

				spriteBatch.End ();

				// banana drawing is outside of loop because we need to read the screen
				// screen pixel of the front buffer.
				spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
				banana.Draw (spriteBatch);
				spriteBatch.End ();

				headsUp.Draw (gameTime);
				DrawPauseButton (spriteBatch);
				score.Draw (gameTime);
				gameOver.Draw (gameTime);
			}

			// If the game is transitioning on or off, fade it out to black.
			if (TransitionPosition > 0 || pauseAlpha > 0) {
				float alpha = MathHelper.Lerp (1f - TransitionAlpha, 1f, pauseAlpha / 2);

				ScreenManager.FadeBackBufferToBlack (alpha);
			}
		}

	#endregion

		void CheckForCollisions ()
		{

			if (!banana.BeingThrown)
				return;

			ObjectCollisionEnum whatWasHit = cityScape.CheckBuildingCollision (banana);

			switch (whatWasHit) {
			case ObjectCollisionEnum.Gorilla1:
			case ObjectCollisionEnum.Gorilla2:
				players.KillOffGorilla (whatWasHit);
				gameState = GameState.PlayerDead;
				deathTimer = 0.0f;
				break;
			case ObjectCollisionEnum.Building:
				SwitchPlayers ();
				break;
			}

		}

		bool CheckInsideBoundaries ()
		{
			if (!banana.BeingThrown)
				return false;

			if (banana.WorldLocation.X < 0 || banana.WorldLocation.X > ScreenManager.BaseWidth) {
				SwitchPlayers ();
				return false;
			}

			return true;
		}

		private void SwitchPlayers ()
		{
			gameState = GameState.PlayerSwitch;
			banana.ResetBanana ();
			banana.Visible = false;
			cityScape.ResetScenary ();
			switchTimer = 0;

		}

		void CreateCityScape ()
		{
			if (networkSession == null || (networkSession.IsHost)) {
				cityScape.CityIsDirty = true;
				score.ScoreBoardBackground = CityScapeBackground.backGroundColor;
			
				players.RevivePlayers ();			
				PlaceGorillas ();
				players.SwitchTurns ();				
				
			}
			SendNewGame();

		}
		
		void SendNewGame()
		{
			if (networkSession == null || !networkSession.IsHost)
				return;
			
			// SendData and SendToAll empty the writer once they have sent it, as in XNA, so each local
			// gamer gets its own packet, written from scratch.
			foreach (LocalNetworkGamer gamer in networkSession.LocalGamers) {
				PacketWriter packetWriter = new PacketWriter();
				packetWriter.Write((int)MessageType.NewGame);
				cityScape.WriteScene(packetWriter);

				packetWriter.Write(gorilla1.Position);
				packetWriter.Write(gorilla2.Position);

				gamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);
			}
		}
		
		internal void SendBananaThrown(float angle, float velocity)
		{
			if (networkSession == null || banana.BeingThrown)
				return;
			
			// one packet per local gamer: the writer is emptied by SendData, see SendNewGame
			foreach (LocalNetworkGamer gamer in networkSession.LocalGamers) {
				PacketWriter packetWriter = new PacketWriter();
				packetWriter.Write((int)MessageType.ThrowBanana);
				packetWriter.Write(angle);
				packetWriter.Write(velocity);

				gamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);
			}
		}
		
		internal void ReadBananaThrown()
		{
			if (networkSession == null || banana.BeingThrown)
				return;
			
			float angle = packetReader.ReadSingle();
			float velocity = packetReader.ReadSingle();

			headsUp.SetFields(angle, velocity);
		}
		
		void PlaceGorillas ()
		{
			cityScape.PlaceGorilla (gorilla1);
			cityScape.PlaceGorilla (gorilla2);
		}

	}
}
