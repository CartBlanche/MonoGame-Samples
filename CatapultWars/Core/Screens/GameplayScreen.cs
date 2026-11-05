//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

//using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework.Net;

namespace CatapultGame
{
	class GameplayScreen : GameScreen
	{

		enum MessageType : byte
		{
			NewGame = 1,
			CatapultFiring = 2,
			CatapultAiming = 3,
			UpdateEnvironment = 4,
			Crater = 5,
		}

		internal NetworkSession NetworkSession { get; private set; }
		PacketReader packetReader = new PacketReader();
		// Do NOT reuse a single PacketWriter across sends; create a fresh one per packet.
		internal bool IsMultiplayer { get; private set; }

		// Texture Members
		Texture2D foregroundTexture;
		Texture2D cloud1Texture;
		Texture2D cloud2Texture;
		Texture2D mountainTexture;
		Texture2D skyTexture;
		Texture2D hudBackgroundTexture;
		Texture2D ammoTypeTexture;
		HalloweenDecor decor;
		// The crew beside each catapult, and the scores they last reacted to
		CatapultMan manOne;
		CatapultMan manTwo;
		int lastScoreOne;
		int lastScoreTwo;
		bool menToldMatchOver;

		// Slow motion and zoom for a direct hit
		const float SlowMotionScale = 0.25f;
		const float MaxZoom = 1.7f;
		const float SlowMotionHoldAfterHit = 0.9f; // real seconds to stay slow once the shot lands
		float timeScale = 1f;
		float slowMotionHold;
		Vector2 zoomFocus = new Vector2(400, 240);

		// Camera shake when a shot lands. Real time, so slow motion does not stretch it.
		const float ShakeDuration = 0.4f;
		float shakeLeft;
		float shakeStrength;
		float shakeClock;
		bool wasCatapultHit;
		bool wasHit;
		Texture2D windArrowTexture;
		Texture2D defeatTexture;
		Texture2D victoryTexture;
		SpriteFont hudFont;

		// Rendering members
		Vector2 cloud1Position;
		Vector2 cloud2Position;
		Vector2 playerOneHUDPosition;

		// On-screen X for phones and tablets, which have no Esc key. It opens the pause menu.
		static readonly Rectangle exitButton = new Rectangle(7, 7, 40, 40);
		Texture2D buttonPixel;
		Vector2 computerHUDPosition;
		Vector2 windArrowPosition;

		// Gameplay members
		Human playerOne;
		Human playerTwo;

		Vector2 wind;
		bool changeTurn;
		bool isFirstPlayerTurn;
		bool gameOver;

		// a networked game that had two players, and the other one has left
		bool hadOpponent;
		bool opponentLeft;
		Random random;
		const int minWind = 0;
		const int maxWind = 10;

		// Helper members
		bool isDragging;

		public GameplayScreen()
		{
			TransitionOnTime = TimeSpan.FromSeconds(1.5);
			TransitionOffTime = TimeSpan.FromSeconds(0.5);
			
			EnabledGestures = GestureType.FreeDrag |
					GestureType.DragComplete |
					GestureType.Tap;

			random = new Random();
        }

		/// <summary>
		/// Loads the game assets and initializes "players"
		/// </summary>
		public override void LoadContent()
		{
			base.LoadContent();

			LoadAssets();

			// Start the game
			Start();
		}

		public void LoadAssets()
		{

			// A fresh battlefield every game
			Terrain.Reset();

			NetworkSession = ScreenManager.Game.Services.GetService(typeof(NetworkSession)) as NetworkSession;
			IsMultiplayer = NetworkSession != null;
			hadOpponent = IsMultiplayer && NetworkSession.RemoteGamers.Count > 0;

			// Load textures
			foregroundTexture = Load<Texture2D>("Textures/Backgrounds/gameplay_screen");
			cloud1Texture = Load<Texture2D>("Textures/Backgrounds/cloud1");
			cloud2Texture = Load<Texture2D>("Textures/Backgrounds/cloud2");
			mountainTexture = Load<Texture2D>("Textures/Backgrounds/mountain");
			skyTexture = Load<Texture2D>("Textures/Backgrounds/sky");
			defeatTexture = Load<Texture2D>("Textures/Backgrounds/defeat");
			victoryTexture = Load<Texture2D>("Textures/Backgrounds/victory");
			hudBackgroundTexture = Load<Texture2D>("Textures/HUD/hudBackground");
			windArrowTexture = Load<Texture2D>("Textures/HUD/windArrow");
			ammoTypeTexture = Load<Texture2D>("Textures/HUD/ammoType");
			// Seasonal look, decided once per game
			decor = Theme.IsHalloween ? new HalloweenDecor(ScreenManager.GraphicsDevice) : null;
			Terrain.Prepare(foregroundTexture, mountainTexture,
				decor != null ? HalloweenDecor.ForegroundTint : Color.White,
				decor != null ? HalloweenDecor.MountainTint : Color.White);

			// Load font
			hudFont = Load<SpriteFont>("Fonts/HUDFont");

			// Define initial cloud position
			cloud1Position = new Vector2(224 - cloud1Texture.Width, 32);
			cloud2Position = new Vector2(64, 90);

			// Define initial HUD positions
			playerOneHUDPosition = new Vector2(7, 7);
			if (UIUtility.IsMobile)
			{
				// Make room for the exit button
				playerOneHUDPosition.X += exitButton.Right;
				buttonPixel = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
				buttonPixel.SetData(new[] { Color.White });
			}
			computerHUDPosition = new Vector2(613, 7);
			windArrowPosition = new Vector2(345, 46);

			// Initialize human & other players
			if (IsMultiplayer)
			{
				if (NetworkSession.RemoteGamers.Count > 0)
				{
					if (NetworkSession.IsHost)
					{
						playerOne = new Human(ScreenManager.Game, ScreenManager.SpriteBatch, PlayerSide.Left);
						playerOne.Initialize();
						playerOne.Name = NetworkSession.LocalGamers[0].Gamertag + " (host)";

						playerTwo = new Human(ScreenManager.Game, ScreenManager.SpriteBatch, PlayerSide.Right);
						playerTwo.Initialize();
						playerTwo.Name = NetworkSession.RemoteGamers[0].Gamertag;
					}
					else
					{
						playerOne = new Human(ScreenManager.Game, ScreenManager.SpriteBatch, PlayerSide.Left);
						playerOne.Initialize();
						playerOne.Name = NetworkSession.RemoteGamers[0].Gamertag + " (host)";

						playerTwo = new Human(ScreenManager.Game, ScreenManager.SpriteBatch, PlayerSide.Right);
						playerTwo.Initialize();
						playerTwo.Name = NetworkSession.LocalGamers[0].Gamertag;
					}
				}
				else
				{
					playerOne = new Human(ScreenManager.Game, ScreenManager.SpriteBatch, PlayerSide.Left);
					playerOne.Initialize();
					playerOne.Name = NetworkSession.LocalGamers[0].Gamertag + " (host)";

					playerTwo = new Human(ScreenManager.Game, ScreenManager.SpriteBatch, PlayerSide.Right);
					playerTwo.Initialize();
					playerTwo.Name = "Player 2";

					IsMultiplayer = false;
					NetworkSession.Dispose();
					NetworkSession = null;
				}
			}
			else
			{
				playerOne = new Human(ScreenManager.Game, ScreenManager.SpriteBatch, PlayerSide.Left);
				playerOne.Initialize();
				playerOne.Name = "Player 1";

				playerTwo = new AI(ScreenManager.Game, ScreenManager.SpriteBatch);
				playerTwo.Initialize();
				playerTwo.Name = "AI";
			}

			// Identify enemies
			playerOne.Enemy = playerTwo;
			playerTwo.Enemy = playerOne;

			// The crew stand just outside their catapults, facing the enemy
			manOne = new CatapultMan(ScreenManager.GraphicsDevice, Battlefield.LeftMan, true, true);
			manTwo = new CatapultMan(ScreenManager.GraphicsDevice, Battlefield.RightMan, false, false);
			lastScoreOne = lastScoreTwo = 0;
			menToldMatchOver = false;

			// A new layout every game. In a LAN game the host's is sent across in the environment update.
			ApplyLayout(Battlefield.NewSeed());

			// Shots that reach the ground dig craters
			playerOne.Catapult.GroundImpact += (position, speed) => OnGroundImpact(true, position, speed);
			playerTwo.Catapult.GroundImpact += (position, speed) => OnGroundImpact(false, position, speed);
		}

		/// <summary>
		/// Runs one frame of update for the game.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Nobody is left to take the other player's turn, so the game ends and the player who stayed wins
			if (hadOpponent && !gameOver && NetworkSession.RemoteGamers.Count == 0)
			{
				gameOver = true;
				opponentLeft = true;
				AudioManager.PlaySound("gameOver_Win");
				return;
			}

			// Check it one of the players reached 5 and stop the game
			if ((playerOne.Catapult.GameOver || playerTwo.Catapult.GameOver) &&
 				(gameOver == false))
			{

				gameOver = true;
				if (IsMultiplayer)
				{
					if (NetworkSession.IsHost)
					{
						if (playerOne.Score > playerTwo.Score)
						{
							AudioManager.PlaySound("gameOver_Win");
						}
						else
						{
							AudioManager.PlaySound("gameOver_Lose");
						}
					}
					else
					{
						if (playerOne.Score > playerTwo.Score)
						{
							AudioManager.PlaySound("gameOver_Lose");
						}
						else
						{
							AudioManager.PlaySound("gameOver_Win");
						}
					}
				}
				else
				{
					if (playerOne.Score > playerTwo.Score)
					{
						AudioManager.PlaySound("gameOver_Win");
					}
					else
					{
						AudioManager.PlaySound("gameOver_Lose");
					}
				}
				return;
			}

			// If Reset flag raised and both catapults are not animating - 
			// active catapult finished the cycle, new turn!
			if ((playerOne.Catapult.CurrentState == CatapultState.Reset ||
				playerTwo.Catapult.CurrentState == CatapultState.Reset) &&
				!(playerOne.Catapult.AnimationRunning ||
				playerTwo.Catapult.AnimationRunning))
			{

				changeTurn = true;

				if (playerOne.IsActive == true)
				{ //Last turn was a human turn?
					playerOne.IsActive = false;
					playerTwo.IsActive = true;
					isFirstPlayerTurn = false;
					playerOne.Catapult.CurrentState = CatapultState.Idle;
					if (playerTwo.IsAI)
						playerTwo.Catapult.CurrentState = CatapultState.Aiming;
					else
						playerTwo.Catapult.CurrentState = CatapultState.Idle;
				}
				else
				{ //It was an AI turn
					playerOne.IsActive = true;
					playerTwo.IsActive = false;
					isFirstPlayerTurn = true;
					playerTwo.Catapult.CurrentState = CatapultState.Idle;
					playerOne.Catapult.CurrentState = CatapultState.Idle;
				}
			}

			if (changeTurn)
			{
				// Update wind
				wind = new Vector2(random.Next(-1, 2), random.Next(minWind, maxWind + 1));

				// Set new wind value to the players and
				playerOne.Catapult.Wind = playerTwo.Catapult.Wind = wind.X > 0 ? wind.Y : -wind.Y;
				changeTurn = false;
			}

			// Update the players, in slow motion if a direct hit is about to land
			UpdateSlowMotion(elapsed);
			UpdateShake(elapsed);
			var playTime = new GameTime(gameTime.TotalGameTime, TimeSpan.FromSeconds(elapsed * timeScale));
			playerOne.Update(playTime);
			playerTwo.Update(playTime);

			if (decor != null)
			{
				decor.Update(elapsed);

				// Startle the bats when a catapult is hit
				bool hitNow = playerOne.Catapult.CurrentState == CatapultState.Hit
					|| playerTwo.Catapult.CurrentState == CatapultState.Hit;
				if (hitNow && !wasHit)
					decor.Scatter();
				wasHit = hitNow;
			}

			UpdateMen(elapsed);

			// Updates the cloud positions
			UpdateClouds(elapsed);

			UpdateNetworkSession();

			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		/// <summary>
		/// Draw the game world, effects, and HUD
		/// </summary>
		/// <param name="gameTime">The elapsed time since last Draw</param>
		public override void Draw(GameTime gameTime)
		{
			// The battlefield, which zooms in for a direct hit
			ScreenManager.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, WorldTransform());
			DrawBackground();
			manOne.Draw(ScreenManager.SpriteBatch);
			manTwo.Draw(ScreenManager.SpriteBatch);
			DrawComputer(gameTime);
			DrawPlayer(gameTime);
			ScreenManager.SpriteBatch.End();

			// The HUD always stays put
			ScreenManager.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
			DrawHud();
			DrawExitButton();

			ScreenManager.SpriteBatch.End();
		}

		/// <summary>
		/// Input helper method provided by GameScreen.  Packages up the various input
		/// values for ease of use.
		/// </summary>
		/// <param name="input">The state of the gamepads</param>
		public override void HandleInput(InputState input)
		{

			if (input == null)
				throw new ArgumentNullException("input");

			if (!gameOver && UIUtility.IsMobile && ExitButtonPressed(input))
			{
				PauseCurrentGame();
				return;
			}

			if (gameOver)
			{
				if (input.IsPauseGame(null))
				{
					FinishCurrentGame();
				}

				if (input.MouseGesture.HasFlag(MouseGestureType.LeftClick))
					FinishCurrentGame();

				foreach (GestureSample gestureSample in input.Gestures)
				{
					if (gestureSample.GestureType == GestureType.Tap)
					{
						FinishCurrentGame();
					}
				}

				return;
			}
			if (NetworkSession != null)
			{
				if ((isFirstPlayerTurn && !NetworkSession.IsHost)
					|| (!isFirstPlayerTurn && NetworkSession.IsHost))
					return;
			}

			if (input.IsPauseGame(null))
			{
				PauseCurrentGame();
			}
			else if (isFirstPlayerTurn &&
					(playerOne.Catapult.CurrentState == CatapultState.Idle ||
					playerOne.Catapult.CurrentState == CatapultState.Aiming))
			{

				// First we try with mouse input
				playerOne.HandleInput(input);
				isDragging = playerOne.isDragging;

				// Read all available gestures
				foreach (GestureSample gestureSample in input.Gestures)
				{
					if (gestureSample.GestureType == GestureType.FreeDrag)
						isDragging = true;
					else if (gestureSample.GestureType == GestureType.DragComplete)
						isDragging = false;

					playerOne.HandleInput(gestureSample);
				}
			}
			else if (!isFirstPlayerTurn &&
					(playerTwo.Catapult.CurrentState == CatapultState.Idle ||
					playerTwo.Catapult.CurrentState == CatapultState.Aiming))
			{

				// First we try with mouse input
				playerTwo.HandleInput(input);
				isDragging = playerTwo.isDragging;

				// Read all available gestures
				foreach (GestureSample gestureSample in input.Gestures)
				{
					if (gestureSample.GestureType == GestureType.FreeDrag)
						isDragging = true;
					else if (gestureSample.GestureType == GestureType.DragComplete)
						isDragging = false;

					playerTwo.HandleInput(gestureSample);
				}
			}
		}

		Vector3 catapultInfoVector = Vector3.Zero;
		void UpdateNetworkSession()
		{
			if (NetworkSession == null)
				return;

			if (NetworkSession.IsHost)
			{
				// Build a fresh packet for environment update
				using (var writer = new PacketWriter())
				{
					writer.Write((int)MessageType.UpdateEnvironment);
					writer.Write(wind);
					writer.Write(cloud1Position);
					writer.Write(cloud2Position);
					writer.Write(MatchSettings.WinScore);
					writer.Write(Battlefield.Seed);

					// Update our locally controlled catapults, and send their
					// latest position data to everyone in the session.
					foreach (LocalNetworkGamer gamer in NetworkSession.LocalGamers)
					{
						gamer.SendData(writer, SendDataOptions.ReliableInOrder);
					}
				}
			}

			if (playerOne.Catapult.CurrentState == CatapultState.Aiming)
			{
				SendCatapultInfo(MessageType.CatapultAiming, playerOne);
			}

			if (playerOne.Catapult.CurrentState == CatapultState.Firing)
			{

				SendCatapultInfo(MessageType.CatapultFiring, playerOne);
			}

			if (playerTwo.Catapult.CurrentState == CatapultState.Aiming)
			{
				SendCatapultInfo(MessageType.CatapultAiming, playerTwo);
			}

			if (playerTwo.Catapult.CurrentState == CatapultState.Firing)
			{
				SendCatapultInfo(MessageType.CatapultFiring, playerTwo);
			}

			// Pump the underlying session object.
			NetworkSession.Update();

			// Make sure the session has not ended.
			if (NetworkSession == null)
				return;

			// Read any packets
			foreach (LocalNetworkGamer gamer in NetworkSession.LocalGamers)
			{
				ReadIncomingPackets(gamer);
			}
		}

		void SendCatapultInfo(MessageType messageType, Human player)
		{
			// Build a fresh packet for each catapult update
			using (var writer = new PacketWriter())
			{
				writer.Write((int)messageType);
				catapultInfoVector.X = player.Catapult.ShotStrength;
				catapultInfoVector.Y = player.Catapult.ShotVelocity;
				catapultInfoVector.Z = player.ArrowScale;
				writer.Write(catapultInfoVector);
				// The shot angle follows the original three values, so an older build just ignores it
				writer.Write(player.Catapult.ShotAngle);

				// Update our locally controlled catapults, and send their
				// latest position data to everyone in the session.
				foreach (LocalNetworkGamer gamer in NetworkSession.LocalGamers)
				{
					gamer.SendData(writer, SendDataOptions.ReliableInOrder);
				}
			}
		}

		/// <summary>
		/// A shot reached the ground. In a LAN game the machine that fired decides where the crater goes
		/// and tells the other one, so both battlefields stay identical even if the two flights drift a pixel apart.
		/// </summary>
		void OnGroundImpact(bool playerOneFired, Vector2 position, float speed)
		{
			// Every boulder that lands shakes the view a little, harder if it came in fast
			AddShake(1.5f + 2.5f * MathHelper.Clamp((speed - 200f) / 400f, 0f, 1f));

			if (IsMultiplayer)
			{
				bool firedHere = playerOneFired == NetworkSession.IsHost;
				if (!firedHere)
					return; // the crater arrives in a Crater message

				using (var writer = new PacketWriter())
				{
					writer.Write((int)MessageType.Crater);
					writer.Write(position);
					writer.Write(speed);
					foreach (LocalNetworkGamer gamer in NetworkSession.LocalGamers)
						gamer.SendData(writer, SendDataOptions.ReliableInOrder);
				}
			}

			Terrain.Carve(position.X, speed);
		}

		/// <summary>
		/// The shot angle, if the sender included it. An older build does not, and always fired at 45 degrees.
		/// </summary>
		float ReadShotAngle()
		{
			if (packetReader.Position < packetReader.Length)
				return MathHelper.Clamp(packetReader.ReadSingle(), Ballistics.MinAngle, Ballistics.MaxAngle);
			return Ballistics.DefaultAngle;
		}

		/// <summary>
		/// Helper for reading incoming network packets.
		/// </summary>
		void ReadIncomingPackets(LocalNetworkGamer gamer)
		{
			// Keep reading as long as incoming packets are available.
			while (gamer.IsDataAvailable)
			{
				NetworkGamer sender;

				// Read a single packet from the network.
				gamer.ReceiveData(packetReader, out sender);

				// Discard packets sent by local gamers: we already know their state!
				if (sender.IsLocal)
					continue;

				MessageType msgType = (MessageType)packetReader.ReadInt32();
				switch (msgType)
				{
					case MessageType.NewGame:
						//ReceiveNewNetworkedGame();
						break;
					case MessageType.CatapultAiming:

						if (isFirstPlayerTurn && !NetworkSession.IsHost)
						{
							playerOne.Catapult.CurrentState = CatapultState.Aiming;
							playerOne.isDragging = true;

							catapultInfoVector = packetReader.ReadVector3();

							playerOne.Catapult.ShotStrength = catapultInfoVector.X;
							playerOne.Catapult.ShotVelocity = catapultInfoVector.Y;
							playerOne.ArrowScale = catapultInfoVector.Z;
							playerOne.Catapult.ShotAngle = ReadShotAngle();

						}
						if (!isFirstPlayerTurn && NetworkSession.IsHost)
						{

							playerTwo.Catapult.CurrentState = CatapultState.Aiming;
							playerTwo.isDragging = true;

							catapultInfoVector = packetReader.ReadVector3();

							playerTwo.Catapult.ShotStrength = catapultInfoVector.X;
							playerTwo.Catapult.ShotVelocity = catapultInfoVector.Y;
							playerTwo.ArrowScale = catapultInfoVector.Z;
							playerTwo.Catapult.ShotAngle = ReadShotAngle();

						}
						break;
					case MessageType.CatapultFiring:

						if (isFirstPlayerTurn && !NetworkSession.IsHost)
						{
							catapultInfoVector = packetReader.ReadVector3();
							playerOne.Catapult.ShotAngle = ReadShotAngle();
							playerOne.Catapult.Fire(catapultInfoVector.Y, playerOne.Catapult.ShotAngle);
							playerOne.Catapult.CurrentState = CatapultState.Firing;
							playerOne.ResetDragState();
						}
						if (!isFirstPlayerTurn && NetworkSession.IsHost)
						{
							catapultInfoVector = packetReader.ReadVector3();
							playerTwo.Catapult.ShotAngle = ReadShotAngle();
							playerTwo.Catapult.Fire(catapultInfoVector.Y, playerTwo.Catapult.ShotAngle);
							playerTwo.Catapult.CurrentState = CatapultState.Firing;
							playerTwo.ResetDragState();
						}
						break;
					case MessageType.Crater:
						Vector2 craterPosition = packetReader.ReadVector2();
						Terrain.Carve(craterPosition.X, packetReader.ReadSingle());
						break;
					case MessageType.UpdateEnvironment:
						wind = packetReader.ReadVector2();
						cloud1Position = packetReader.ReadVector2();
						cloud2Position = packetReader.ReadVector2();
						// The host's points-to-win rides along; an older host does not send it
						if (packetReader.Position < packetReader.Length)
							MatchSettings.WinScore = packetReader.ReadInt32();
						// So does the layout of the battlefield; build the host's if it is not the one we have
						if (packetReader.Position < packetReader.Length)
						{
							int seed = packetReader.ReadInt32();
							if (seed != Battlefield.Seed)
								ApplyLayout(seed);
						}
						// Set new wind value to the players and
						playerOne.Catapult.Wind = playerTwo.Catapult.Wind = wind.X > 0 ? wind.Y : -wind.Y;
						break;
				}
			}
		}
		// End networking methods

		private void UpdateClouds(float elapsedTime)
		{
			// Move the clouds according to the wind
			int windDirection = wind.X > 0 ? 1 : -1;

			cloud1Position += new Vector2(24.0f, 0.0f) * elapsedTime *
				windDirection * wind.Y;
			if (cloud1Position.X > ScreenManager.BASE_BUFFER_WIDTH)
				cloud1Position.X = -cloud1Texture.Width * 2.0f;
			else if (cloud1Position.X < -cloud1Texture.Width * 2.0f)
				cloud1Position.X = ScreenManager.BASE_BUFFER_WIDTH;

			cloud2Position += new Vector2(16.0f, 0.0f) * elapsedTime *
				windDirection * wind.Y;
			if (cloud2Position.X > ScreenManager.BASE_BUFFER_WIDTH)
				cloud2Position.X = -cloud2Texture.Width * 2.0f;
			else if (cloud2Position.X < -cloud2Texture.Width * 2.0f)
				cloud2Position.X = ScreenManager.BASE_BUFFER_WIDTH;
		}

		/// <summary>
		/// Eases into slow motion while a shot that will hit a catapult makes its last approach, holds it
		/// for a moment after the hit, then eases back to normal speed.
		/// </summary>
		void UpdateSlowMotion(float realElapsed)
		{
			Catapult incoming = null;
			if (playerOne.Catapult.DirectHitIncoming)
				incoming = playerOne.Catapult;
			else if (playerTwo.Catapult.DirectHitIncoming)
				incoming = playerTwo.Catapult;

			float target = 1f;
			if (incoming != null && !gameOver)
			{
				slowMotionHold = SlowMotionHoldAfterHit;
				target = SlowMotionScale;

				// Look between the shot and where it is about to land
				zoomFocus = Vector2.Lerp(incoming.PredictedImpact, incoming.ProjectilePosition, 0.35f);
			}
			else if (slowMotionHold > 0f && !gameOver)
			{
				slowMotionHold -= realElapsed;
				target = SlowMotionScale;
			}

			// Slow down fast, speed up gently
			float rate = target < timeScale ? 8f : 2f;
			timeScale = MathHelper.Clamp(target, timeScale - rate * realElapsed, timeScale + rate * realElapsed);

		}

		/// <summary>Starts a shake of the given size in pixels, unless one that big is still going.</summary>
		void AddShake(float pixels)
		{
			shakeStrength = Math.Max(CurrentShake(), pixels);
			shakeLeft = ShakeDuration;
		}

		// The size of the shake now: full at the start, dying away smoothly
		float CurrentShake()
		{
			if (shakeLeft <= 0f)
				return 0f;
			float k = shakeLeft / ShakeDuration;
			return shakeStrength * k * k;
		}

		void UpdateShake(float realElapsed)
		{
			shakeClock += realElapsed;
			shakeLeft = Math.Max(0f, shakeLeft - realElapsed);

			// A catapult being destroyed shakes the ground harder than a boulder landing
			bool hitNow = playerOne.Catapult.CurrentState == CatapultState.Hit
				|| playerTwo.Catapult.CurrentState == CatapultState.Hit;
			if (hitNow && !wasCatapultHit)
				AddShake(5f);
			wasCatapultHit = hitNow;
		}

		/// <summary>
		/// The view of the battlefield, zoomed in towards the shot in proportion to how slow time is running.
		/// </summary>
		Matrix WorldTransform()
		{
			float slow = MathHelper.Clamp((1f - timeScale) / (1f - SlowMotionScale), 0f, 1f);
			float zoom = MathHelper.Lerp(1f, MaxZoom, slow * slow * (3f - 2f * slow));

			// The shake wobbles the view, with just enough zoom to keep the edges of the screen covered
			float amount = CurrentShake();
			Vector2 shake = new Vector2(
				(float)Math.Sin(shakeClock * 71f) * amount,
				(float)Math.Cos(shakeClock * 53f) * amount);
			zoom *= 1f + amount * 1.2f / (ScreenManager.BASE_BUFFER_HEIGHT / 2f);

			if (zoom <= 1.001f)
				return ScreenManager.GlobalTransformation;

			// Keep the view inside the battlefield
			float viewW = ScreenManager.BASE_BUFFER_WIDTH / zoom;
			float viewH = ScreenManager.BASE_BUFFER_HEIGHT / zoom;
			float focusX = slow > 0f ? zoomFocus.X : ScreenManager.BASE_BUFFER_WIDTH / 2f;
			float focusY = slow > 0f ? zoomFocus.Y : ScreenManager.BASE_BUFFER_HEIGHT / 2f;
			float left = MathHelper.Clamp(focusX - viewW / 2f + shake.X, 0f, ScreenManager.BASE_BUFFER_WIDTH - viewW);
			float top = MathHelper.Clamp(focusY - viewH / 2f + shake.Y, 0f, ScreenManager.BASE_BUFFER_HEIGHT - viewH);

			return Matrix.CreateTranslation(-left, -top, 0f) * Matrix.CreateScale(zoom, zoom, 1f)
				* ScreenManager.GlobalTransformation;
		}

		/// <summary>
		/// Sets out the battlefield for a seed: the hill, the catapults and their crews.
		/// </summary>
		void ApplyLayout(int seed)
		{
			Battlefield.Generate(seed);
			Terrain.Reset();
			playerOne.Catapult.Relocate(Battlefield.LeftCatapult);
			playerTwo.Catapult.Relocate(Battlefield.RightCatapult);
			manOne.MoveTo(Battlefield.LeftMan);
			manTwo.MoveTo(Battlefield.RightMan);
		}

		/// <summary>
		/// The crew cheer when their side scores and shake their fists when the other side does.
		/// When the match is over the winner cheers and the loser shakes fists until the screen is dismissed.
		/// </summary>
		void UpdateMen(float elapsed)
		{
			if (playerOne.Score > lastScoreOne || playerTwo.Score > lastScoreTwo)
			{
				bool oneScored = playerOne.Score > lastScoreOne;
				bool twoScored = playerTwo.Score > lastScoreTwo;
				lastScoreOne = playerOne.Score;
				lastScoreTwo = playerTwo.Score;

				if (oneScored && !twoScored) { manOne.Cheer(2.5f); manTwo.ShakeFists(2.5f); }
				else if (twoScored && !oneScored) { manTwo.Cheer(2.5f); manOne.ShakeFists(2.5f); }
			}

			if (gameOver && !menToldMatchOver)
			{
				menToldMatchOver = true;

				// If the other player walked out, the one who stayed wins whatever the score
				bool oneWins;
				if (opponentLeft)
					oneWins = !IsMultiplayer || NetworkSession == null || NetworkSession.IsHost;
				else
					oneWins = playerOne.Score > playerTwo.Score;

				if (oneWins) { manOne.CheerForever(); manTwo.ShakeFistsForever(); }
				else { manTwo.CheerForever(); manOne.ShakeFistsForever(); }
			}

			manOne.Update(elapsed);
			manTwo.Update(elapsed);
		}

		/// <summary>
		/// Draws the player's catapult
		/// </summary>
		void DrawPlayer(GameTime gameTime)
		{
			if (!gameOver)
				playerOne.Draw(gameTime);
		}

		/// <summary>
		/// Draws the AI's catapult
		/// </summary>
		void DrawComputer(GameTime gameTime)
		{
			if (!gameOver)
				playerTwo.Draw(gameTime);
		}

		/// <summary>
		/// Draw the sky, clouds, mountains, etc. 
		/// </summary>
		/// <summary>
		/// Draws the HUD ammo icon, a pumpkin when themed, at the standard icon size.
		/// </summary>
		private void DrawAmmoIcon(Vector2 position)
		{
			if (decor != null)
				ScreenManager.SpriteBatch.Draw(decor.Pumpkin,
					new Rectangle((int)position.X, (int)position.Y, ammoTypeTexture.Width, ammoTypeTexture.Height), Color.White);
			else
				ScreenManager.SpriteBatch.Draw(ammoTypeTexture, position, Color.White);
		}

		/// <summary>
		/// Angle and power under a player's score. Live while they pull back, otherwise the last shot they fired.
		/// </summary>
		void DrawAimReadout(Human player, Vector2 hudPosition)
		{
			int angle, power;
			string angleText = "Angle: --";
			string powerText = "Power: --";
			if (player.TryGetReadout(out angle, out power))
			{
				angleText = "Angle: " + angle;
				powerText = "Power: " + power + "%";
			}

			Color color = player.isDragging ? Color.Yellow : Color.White;
			DrawString(hudFont, angleText, hudPosition + new Vector2(10, 90), color);
			DrawString(hudFont, powerText, hudPosition + new Vector2(10, 90 + hudFont.LineSpacing), color);
		}

		private void DrawBackground()
		{
			// Clear the background
			ScreenManager.Game.GraphicsDevice.Clear(Color.Black);

			bool night = decor != null;
			Color cloudTint = night ? HalloweenDecor.CloudTint : Color.White;

			// Draw the Sky
			if (night)
				decor.DrawSky(ScreenManager.SpriteBatch);
			else
				ScreenManager.SpriteBatch.Draw(skyTexture, Vector2.Zero, Color.White);

			// Draw Cloud #1
			ScreenManager.SpriteBatch.Draw(cloud1Texture,
				cloud1Position, cloudTint);

			// Draw the Mountain
			ScreenManager.SpriteBatch.Draw(mountainTexture,
				Vector2.Zero, night ? HalloweenDecor.MountainTint : Color.White);

			// Draw Cloud #2
			ScreenManager.SpriteBatch.Draw(cloud2Texture,
				cloud2Position, cloudTint);

			// Draw the Castle, trees, and foreground 
			ScreenManager.SpriteBatch.Draw(foregroundTexture,
				Vector2.Zero, night ? HalloweenDecor.ForegroundTint : Color.White);

			// The hill between the catapults
			Terrain.Draw(ScreenManager.SpriteBatch);

			// Lit windows, lanterns and bats sit behind the catapults and projectiles
			if (night)
			{
				decor.DrawGameLights(ScreenManager.SpriteBatch);
				decor.DrawBats(ScreenManager.SpriteBatch, 1f);
			}
		}

		/// <summary>
		/// Draw the HUD, which consists of the score elements and the GAME OVER tag.
		/// </summary>
		void DrawHud()
		{
			if (gameOver)
			{
				Texture2D texture;
				if (IsMultiplayer)
				{
					if (opponentLeft)
					{
						texture = victoryTexture;
					}
					else if (NetworkSession.IsHost)
					{
						if (playerOne.Score > playerTwo.Score)
						{
							texture = victoryTexture;
						}
						else
						{
							texture = defeatTexture;
						}
					}
					else
					{
						if (playerOne.Score > playerTwo.Score)
						{
							texture = defeatTexture;
						}
						else
						{
							texture = victoryTexture;
						}
					}

					// Simple fallback - show victory for player 1 if they have higher score
					/*if (playerOne.Score > playerTwo.Score)
					{
						texture = victoryTexture;
					}
					else
					{
						texture = defeatTexture;
					}*/
				}
				else
				{
					if (playerOne.Score > playerTwo.Score)
					{
						texture = victoryTexture;
					}
					else
					{
						texture = defeatTexture;
					}
				}

				ScreenManager.SpriteBatch.Draw(texture,
												new Vector2(ScreenManager.BASE_BUFFER_WIDTH / 2 - texture.Width / 2,
							 ScreenManager.BASE_BUFFER_HEIGHT / 2 - texture.Height / 2),
												Color.White);
			}
			else
			{
				// Draw Player Hud
				ScreenManager.SpriteBatch.Draw(hudBackgroundTexture, playerOneHUDPosition, Color.White);
				DrawAmmoIcon(playerOneHUDPosition + new Vector2(33, 35));
				DrawString(hudFont, playerOne.Score.ToString(), playerOneHUDPosition + new Vector2(123, 35), Color.White);
				DrawString(hudFont, playerOne.Name, playerOneHUDPosition + new Vector2(40, 1), Color.Blue);
				DrawAimReadout(playerOne, playerOneHUDPosition);

				// Draw Computer Hud
				ScreenManager.SpriteBatch.Draw(hudBackgroundTexture, computerHUDPosition, Color.White);
				DrawAmmoIcon(computerHUDPosition + new Vector2(33, 35));
				DrawString(hudFont, playerTwo.Score.ToString(), computerHUDPosition + new Vector2(123, 35), Color.White);
				DrawString(hudFont, playerTwo.Name, computerHUDPosition + new Vector2(40, 1), Color.Red);
				DrawAimReadout(playerTwo, computerHUDPosition);

				// Draw Wind direction
				string text = "WIND";
				Vector2 size = hudFont.MeasureString(text);
				Vector2 windarrowScale = new Vector2(wind.Y / 10, 1);
				ScreenManager.SpriteBatch.Draw(windArrowTexture,
												windArrowPosition, null, Color.White, 0, Vector2.Zero,
												windarrowScale, wind.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);

				DrawString(hudFont, text, windArrowPosition - new Vector2(0, size.Y), Color.Black);
				if (wind.Y == 0)
				{
					text = "NONE";
					DrawString(hudFont, text, windArrowPosition, Color.Black);
				}
				if (IsMultiplayer)
				{
					if (isFirstPlayerTurn)
					{
						if (NetworkSession.IsHost)
						{
							// Prepare player one prompt message
							text = !isDragging ? "Drag Anywhere to Fire" : "Release to Fire!";
						}
						else
						{
							text = "Waiting for " + playerOne.Name;
						}
					}
					else
					{
						if (NetworkSession.IsHost)
						{
							// Prepare player one prompt message
							text = "Waiting for " + playerTwo.Name;
						}
						else
						{
							text = !isDragging ? "Drag Anywhere to Fire" : "Release to Fire!";
						}
					}

				}
				else
				{
					if (isFirstPlayerTurn)
					{
						// Prepare player one prompt message
						text = !isDragging ? "Drag Anywhere to Fire" : "Release to Fire!";
					}
					else
					{
						// Prepare other player message
						text = "I'll get you yet!";
					}
				}
				size = hudFont.MeasureString(text);

				DrawString(hudFont, text,
							new Vector2(ScreenManager.BASE_BUFFER_WIDTH / 2 - size.X / 2,
							 ScreenManager.BASE_BUFFER_HEIGHT - size.Y),
							Color.Green);
			}
		}

		/// <summary>
		/// A simple helper to draw shadowed text.
		/// </summary>
		void DrawString(SpriteFont font, string text, Vector2 position, Color color)
		{
			ScreenManager.SpriteBatch.DrawString(font, text,
				new Vector2(position.X + 1, position.Y + 1), Color.Black);
			ScreenManager.SpriteBatch.DrawString(font, text, position, color);
		}

		/// <summary>
		/// A simple helper to draw shadowed text.
		/// </summary>
		void DrawString(SpriteFont font, string text, Vector2 position, Color color, float fontScale)
		{
			ScreenManager.SpriteBatch.DrawString(font, text, new Vector2(position.X + 1,
				position.Y + 1), Color.Black, 0, new Vector2(0, font.LineSpacing / 2),
				fontScale, SpriteEffects.None, 0);
			ScreenManager.SpriteBatch.DrawString(font, text, position, color, 0,
				new Vector2(0, font.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
		}

		bool ExitButtonPressed(InputState input)
		{
			if (input.MouseGesture.HasFlag(MouseGestureType.LeftClick) &&
				exitButton.Contains(new Point((int)input.CurrentMousePosition.X, (int)input.CurrentMousePosition.Y)))
				return true;

			foreach (GestureSample gestureSample in input.Gestures)
			{
				if (gestureSample.GestureType == GestureType.Tap &&
					exitButton.Contains(new Point((int)gestureSample.Position.X, (int)gestureSample.Position.Y)))
					return true;
			}
			return false;
		}

		void DrawExitButton()
		{
			if (buttonPixel == null || gameOver)
				return;

			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			spriteBatch.Draw(buttonPixel, exitButton, Color.Black * 0.55f);

			// Outline
			Color edge = Color.White * 0.8f;
			spriteBatch.Draw(buttonPixel, new Rectangle(exitButton.X, exitButton.Y, exitButton.Width, 2), edge);
			spriteBatch.Draw(buttonPixel, new Rectangle(exitButton.X, exitButton.Bottom - 2, exitButton.Width, 2), edge);
			spriteBatch.Draw(buttonPixel, new Rectangle(exitButton.X, exitButton.Y, 2, exitButton.Height), edge);
			spriteBatch.Draw(buttonPixel, new Rectangle(exitButton.Right - 2, exitButton.Y, 2, exitButton.Height), edge);

			Vector2 size = hudFont.MeasureString("X");
			spriteBatch.DrawString(hudFont, "X", new Vector2(
				exitButton.Center.X - size.X / 2f, exitButton.Center.Y - size.Y / 2f), Color.White);
		}

		/// <summary>
		/// Finish the current game
		/// </summary>
		private void FinishCurrentGame()
		{
			// A LAN game was started through the loading screen, which cleared the menus
			// away, so there is nothing underneath to return to: end the session and
			// rebuild the main menu instead.
			if (NetworkSession != null && NetworkSessionComponent.EndSession(ScreenManager))
				return;

			ExitScreen();
		}

		/// <summary>
		/// Pause the current game
		/// </summary>
		private void PauseCurrentGame()
		{
			var pauseMenuBackground = new BackgroundScreen();

			if (isDragging)
			{
				isDragging = false;
				playerOne.Catapult.CurrentState = CatapultState.Idle;
			}

			ScreenManager.AddScreen(pauseMenuBackground, null);
			ScreenManager.AddScreen(new PauseScreen(pauseMenuBackground, playerOne, playerTwo), null);
		}

		/// <summary>
		/// Starts a new game session, setting all game states to initial values.
		/// </summary>
		void Start()
		{
			// Set initial wind direction
			wind = Vector2.Zero;
			isFirstPlayerTurn = false;
			changeTurn = true;
			playerTwo.Catapult.CurrentState = CatapultState.Reset;
		}
	}
}