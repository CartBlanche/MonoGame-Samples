using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Net;

namespace Gorillas
{
	public class HeadsUpTrajectory
	{
		GraphicsDevice graphics;
		SpriteBatch spriteBatch;
		
		protected SpriteFont gameFont;
		
		string trajectory = string.Empty;
		Vector2 position = Vector2.Zero;
		
		string _name = string.Empty;
		Gorilla gorilla;

		// guide
		Texture2D guide;
		Vector2 minSize = Vector2.Zero;
		
		KeyboardState lastKeyboardState = new KeyboardState ();
		KeyboardState currentKeyboardState = new KeyboardState ();
		
		MouseState lastMouseState = new MouseState ();
		MouseState currentMouseState = new MouseState ();
		
		bool classic = false;
		
		GameplayScreen game;
		NetworkSession networkSession;
		
		public HeadsUpTrajectory (GameplayScreen game, NetworkSession networkSession)
			: this(game, networkSession, false)
		{}
		
		public HeadsUpTrajectory (GameplayScreen game, NetworkSession networkSession, bool classic)
		{
			graphics = game.ScreenManager.Game.GraphicsDevice;
			spriteBatch = game.ScreenManager.SpriteBatch;
			this.game = game;
			this.networkSession = networkSession;
			
			fields.Add (string.Empty);
			fields.Add (string.Empty);
			fieldSelected.Add (true);
			fieldSelected.Add (true);
			this.classic = classic;
			
			LoadContent ();
		}

		ContentManager content = null;
		
		private ContentManager Content 
		{
			get {
				if (content == null)
					content = new ContentManager (game.ScreenManager.Game.Services, "Content");				
				return content;
			}
		}
		
		private GraphicsDevice GraphicsDevice
		{
			get {
				return game.ScreenManager.GraphicsDevice;
			}
		}
		
		Color guideColor = Color.Yellow;
		
		protected void LoadContent ()
		{
			gameFont = Content.Load<SpriteFont> ("Fonts/Pericles14Bold");
			guide = new Texture2D (GraphicsDevice,1,1);
			guide.SetData<Color> (new Color[] {Color.White});
			
			guideColor.A = 127;  // set the alpha for the guid color

			minSize = gameFont.MeasureString("Velocity: 999  ");
			//base.LoadContent ();
		}
		
		bool _visible = false;
		public bool Visible 
		{
			get { return _visible; }
			set { _visible = value; }
		}
		
		public void Update (GameTime gameTime)
		{
			if (!Visible)
				return;
			if (!Visible && !classic && !gorilla.IsAI)
				return;
			//HandleInput ();
			if (classic || gorilla.IsAI) {
				//HandleInput ();

				if (fieldIndex == 0)
					trajectory = string.Format ("Name: {0}\nAngle: {1:F0}", _name, Angle);
				else
					trajectory = string.Format ("Name: {0}\nAngle: {1:F0}\nVelocity: {2:F0}", _name, Angle, Velocity);
			} else {
				trajectory = string.Format ("Name: {0}\nAngle: {1:F0}\nVelocity: {2:F0}", _name, Angle, Velocity);			
			}
		}

		public void Draw (GameTime gameTime)
		{
			if (!Visible)
				return;
			
			if (!Visible && !classic && !gorilla.IsAI)
				return;

			spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);
			spriteBatch.DrawString (gameFont, trajectory, position, Color.Chartreuse, 0, 
				Vector2.Zero, 0.75f, SpriteEffects.None, 0);
			spriteBatch.End ();

			if (aimVisible && gorilla != null && !gorilla.IsAI) {
				spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, ScreenManager.GlobalTransformation);
				DrawAimArrow (Gorilla.EyeSight, GuideAngle, Velocity);
				spriteBatch.End ();
			}
		}

		// The aim arrow starts at the gorilla, points along the throw angle and its length is the throw
		// velocity. The colour runs from yellow (soft) to red (full power).
		const float arrowMaxLength = 100f;

		void DrawAimArrow (Vector2 origin, float angle, float length)
		{
			Color fill = Color.Lerp (Color.Yellow, Color.Red, MathHelper.Clamp (length / arrowMaxLength, 0, 1));
			RetroArrow.Draw (spriteBatch, guide, origin, angle, length, fill);
		}

		public Gorilla Gorilla { 
			get { return gorilla; } 
			set {
				gorilla = value;
				_name = gorilla.Name;
				if (gorilla.PlayerClass == Gorilla.GorillaPlayerClass.Player1)
					position = new Vector2 (2,2);
				if (gorilla.PlayerClass == Gorilla.GorillaPlayerClass.Player2) {
					Vector2 ss = gameFont.MeasureString ("Name: " + _name + "  ");
					if (ss.X < minSize.X)
						ss.X = minSize.X;
					
					position.X = ScreenManager.BaseWidth - (ss.X * 0.75f);
				}

				for (int x =0; x < fields.Count; x++) {
					fields [x] = string.Empty;
					fieldSelected [x] = true;
				}

				fieldIndex = 0;
				thrown = false;
				aimVisible = false;
				dragging = false;
				pointerHeld = false;
				padAiming = false;
				Angle = 0;
				Velocity = 0;

				if (OptionsMenuScreen.currentSkillLevel < SkillLevel.Advanced) {
					Angle = gorilla.Angle;
					if (gorilla.PlayerClass == Gorilla.GorillaPlayerClass.Player2
						&& Angle > 0)
						Angle = 180 - Angle;
				
					fields[0] = ((int)Angle).ToString();
					keyPresses.Clear ();
					keyPresses.Append(fields[0]);
					Velocity = gorilla.Velocity;
					fields[1] = ((int)gorilla.Velocity).ToString();
				}
				
				if (gorilla.IsAI) {
					
					Vector2 traj = gorilla.AITrajectory;

					// if the AI trajectory is zero then we need to calculate the trajectory
					if (gorilla.AITrajectory == Vector2.Zero) {
						Gorilla target = game.players.GorillaNext(gorilla);					
						TrajectoryAI.Tracker(game, gorilla, target, ref traj );
						gorilla.AITrajectory = traj;
					}					

					// We need to calculate an accuracy
					TrajectoryAI.CalculateAccuracy(ref traj);
					Console.WriteLine("Traj AI: " + traj);					
					
					SetFields(traj.X, traj.Y);
				}	
			}
		}
		
		internal void SetFields(float angle, float velocity) 
		{
			for (int x =0; x < fields.Count; x++)
				fields [x] = string.Empty;
			fieldIndex = 0;
			thrown = false;
			Angle = 0;
			Velocity = 0;
			
			fields [0] = angle.ToString();
			Angle = angle;
			fields [1] = velocity.ToString();
			Velocity = velocity;
			fieldIndex = 2;
			
		}
		public float Angle { get; set; }

		public float Velocity { get; set; }

		public float GuideAngle { get; set; }

		public bool Classic {
			get { return classic; }

			set { classic = value; }
		}

		/// <summary>
		/// Handles input for the specified player. In local game modes, this is called
		/// just once for the controlling player. In network modes, it can be called
		/// more than once if there are multiple profiles playing on the local machine.
		/// Returns true if we should continue to handle input for subsequent players,
		/// or false if this player has paused the game.
		/// </summary>
		internal bool HandlePlayerInput (InputState input, PlayerIndex? controllingPlayer, PlayerIndex playerIndex)
		{
			
			if (thrown || !game.IsActive)
				return true;
			
			// Look up inputs for the specified player profile.
			currentKeyboardState = input.CurrentKeyboardStates [(int)playerIndex];
			lastKeyboardState = input.LastKeyboardStates[(int)playerIndex];
			
			GamePadState gamePadState = input.CurrentGamePadStates [(int)playerIndex];

			// The game pauses either if the user presses the pause button, or if
			// they unplug the active gamepad. This requires us to keep track of
			// whether a gamepad was ever plugged in, because we don't want to pause
			// on PC if they are playing with a keyboard and have no gamepad at all!
			bool gamePadDisconnected = !gamePadState.IsConnected && 
						input.GamePadWasConnected [(int)playerIndex];
			try {
				if (!gorilla.IsRemote && !gorilla.IsAI) {
					// Primary input: touch / mouse drag and the gamepad stick.
					HandlePointer (input);
					HandleGamePad (input, playerIndex);

					// Legacy input: type the angle and velocity on a keyboard.
					HandleKeyboard (input, controllingPlayer);
				}
			}
			catch (Exception exc2) {
				Console.WriteLine("exception 2: " +exc2.Message);
			}

			if (!thrown && fieldIndex > fields.Count - 1) {
				gorilla.Angle = Angle;
				if ((int)gorilla.PlayerClass % 2 == 0)
					gorilla.Angle = 180 - gorilla.Angle;
				gorilla.Velocity = Velocity;
			
				game.SendBananaThrown(Angle, Velocity);						
				gorilla.ThrowBanana();
				thrown = true;
			}			
			
			return true;
		}
		
		List<String> fields = new List<String> ();
		List<bool> fieldSelected = new List<bool> ();
		int fieldIndex = 0;
		bool thrown = false;
		
		StringBuilder keyPresses = new StringBuilder ();

		void HandleKeyPress (Keys key)
		{
			int number = -1;
			
			try {
			if (keyPresses.Length >= 1 && (key == Keys.Delete || key == Keys.Back)) {
				keyPresses.Remove (keyPresses.Length - 1, 1);
				fieldSelected [fieldIndex] = false;
				
			}
														}
					catch (Exception exc5) {
						Console.WriteLine("exception 5: " +exc5.Message);						
					}
			try {
			if (fieldSelected [fieldIndex]) {
				fieldSelected [fieldIndex] = false;
				keyPresses.Clear();
			}
														}
					catch (Exception exc6) {
						Console.WriteLine("exception 6: " +exc6.Message);						
					}				
			if (key == Keys.Enter) {
				fieldIndex++;
				keyPresses.Clear ();
				if (fieldIndex < fields.Count)
					keyPresses.Append(fields[fieldIndex]);
			}

			switch (key) {
			case Keys.D0:
			case Keys.NumPad0:
				number = 0;
				break;
			case Keys.D1:
			case Keys.NumPad1:
				number = 1;
				break;
			case Keys.D2:
			case Keys.NumPad2:
				number = 2;
				break;
			case Keys.D3:
			case Keys.NumPad3:
				number = 3;
				break;
			case Keys.D4:
			case Keys.NumPad4:
				number = 4;
				break;
			case Keys.D5:
			case Keys.NumPad5:
				number = 5;
				break;
			case Keys.D6:
			case Keys.NumPad6:
				number = 6;
				break;
			case Keys.D7:
			case Keys.NumPad7:
				number = 7;
				break;
			case Keys.D8:
			case Keys.NumPad8:
				number = 8;
				break;
			case Keys.D9:
			case Keys.NumPad9:
				number = 9;
				break;
			}	

			if (number >= 0) {
				keyPresses.Append (number.ToString ());
			}

			if (fieldIndex < fields.Count) {
				fields [fieldIndex] = keyPresses.ToString ();	
			}				
			
			Angle = 0;
			Velocity = 0;
			float parsed = 0;
			if (float.TryParse (fields [0], out parsed))
				Angle = (int)parsed;
			if (float.TryParse (fields [1], out parsed))
				Velocity = (int)parsed;
			
			Angle = MathHelper.Clamp(Angle, 0, 360);
			Velocity = MathHelper.Clamp(Velocity, 0, 100);
			
			if (OptionsMenuScreen.currentSkillLevel < SkillLevel.Advanced) {
				if ((int)gorilla.PlayerClass % 2 != 0)
					game.banana.GuidePosition(360 - Angle, (Velocity == 0) ? 50 : Velocity, gorilla.EyeSight);
				else
					game.banana.GuidePosition(180 + Angle, (Velocity == 0) ? 50 : Velocity, gorilla.EyeSight);
			}
				
		}
		
		double bananaAngle;
		float bananaVelocity;

		// True while the arrow should be drawn (drag in progress, or the stick is aiming).
		bool aimVisible = false;
		bool dragging = false;
		bool pointerHeld = false;
		Vector2 pointerOrigin = Vector2.Zero;
		const float dragThreshold = 3f;

		// Releasing a drag with less velocity than this cancels the throw (drag back onto the gorilla).
		const float minThrowVelocity = 8f;

		/// <summary>
		/// Touch / mouse: press anywhere and drag. The arrow runs from the gorilla towards the pointer;
		/// releasing throws the banana. Dragging back onto the gorilla cancels.
		/// </summary>
		void HandlePointer (InputState input)
		{
			if (thrown)
				return;

			if (input.PointerPressed) {
				classic = false;   // pointer takes over from keyboard entry
				pointerHeld = true;
				dragging = false;
				pointerOrigin = input.PointerPosition;
			}

			if (pointerHeld && input.PointerDown) {
				if (!dragging && Vector2.Distance (pointerOrigin, input.PointerPosition) > dragThreshold)
					dragging = true;

				if (dragging)
					AimTowards (input.PointerPosition - gorilla.EyeSight);
			}

			if (pointerHeld && input.PointerReleased) {
				pointerHeld = false;
				bool wasDragging = dragging;
				dragging = false;
				aimVisible = false;

				if (wasDragging && bananaVelocity >= minThrowVelocity)
					Throw ();
			}
		}

		/// <summary>
		/// Gamepad: the left stick aims (direction = angle, how far it is pushed = velocity), A or the
		/// right trigger throws.
		/// </summary>
		void HandleGamePad (InputState input, PlayerIndex playerIndex)
		{
			if (thrown || pointerHeld)
				return;

			int i = (int)playerIndex;
			GamePadState pad = input.CurrentGamePadStates [i];
			if (!pad.IsConnected)
				return;

			Vector2 stick = pad.ThumbSticks.Left;
			if (stick.Length () > 0.25f) {
				classic = false;
				// Thumb sticks have Y pointing up, the screen has it pointing down.
				AimTowards (new Vector2 (stick.X, -stick.Y) * arrowMaxLength);
				padAiming = true;
			}

			bool throwPressed = input.IsNewButtonPress (Buttons.A, playerIndex, out PlayerIndex unused) ||
				input.IsNewButtonPress (Buttons.RightTrigger, playerIndex, out unused);

			if (padAiming && throwPressed && bananaVelocity >= minThrowVelocity) {
				padAiming = false;
				aimVisible = false;
				Throw ();
			}
		}

		bool padAiming = false;

		/// <summary>
		/// Legacy keyboard entry: type the angle, Enter, the velocity, Enter. Only reacts to the keys it
		/// needs, so it never fights with touch, mouse or gamepad.
		/// </summary>
		void HandleKeyboard (InputState input, PlayerIndex? controllingPlayer)
		{
			if (thrown)
				return;

			PlayerIndex unused;
			foreach (Keys key in input.CurrentKeyboardStates [(int)(controllingPlayer ?? PlayerIndex.One)].GetPressedKeys ()) {
				bool relevant = (key >= Keys.D0 && key <= Keys.D9) || (key >= Keys.NumPad0 && key <= Keys.NumPad9) ||
					key == Keys.Enter || key == Keys.Back || key == Keys.Delete;

				if (relevant && input.IsNewKeyPress (key, controllingPlayer, out unused)) {
					classic = true;
					aimVisible = false;
					try {
						HandleKeyPress (key);
					} catch (Exception exc4) {
						Console.WriteLine("exception 4: " +exc4.Message);
					}
				}
			}
		}

		/// <summary>
		/// Works out angle and velocity from a vector pointing away from the gorilla (screen space)
		/// and updates the arrow, the on-screen text and the banana guide.
		/// </summary>
		void AimTowards (Vector2 delta)
		{
			// The arrow simply points where the player is aiming (screen space, y down).
			GuideAngle = (float)Math.Atan2 (delta.Y, delta.X);

			// The angle shown to the player (and typed on the keyboard) is the elevation above the
			// horizontal, measured towards the opponent: Player 1 faces right, Player 2 faces left.
			bool facingLeft = (int)gorilla.PlayerClass % 2 == 0;
			float elevation = MathHelper.ToDegrees ((float)Math.Atan2 (-delta.Y, facingLeft ? -delta.X : delta.X));
			if (elevation < 0)
				elevation += 360;

			bananaVelocity = MathHelper.Clamp (delta.Length (), 0, arrowMaxLength);

			Angle = elevation;
			Velocity = bananaVelocity;

			Visible = true;
			aimVisible = true;

			if (OptionsMenuScreen.currentSkillLevel < SkillLevel.Advanced) {
				if ((int)gorilla.PlayerClass % 2 != 0)
					game.banana.GuidePosition (360 - Angle, (Velocity == 0) ? 50 : Velocity, gorilla.EyeSight);
				else
					game.banana.GuidePosition (180 + Angle, (Velocity == 0) ? 50 : Velocity, gorilla.EyeSight);
			}
		}

		void Throw ()
		{
			if (thrown)
				return;

			// Same convention as the keyboard path: Player 2's angle is mirrored for the physics.
			gorilla.Angle = Angle;
			if ((int)gorilla.PlayerClass % 2 == 0)
				gorilla.Angle = 180 - gorilla.Angle;
			gorilla.Velocity = Velocity;
			game.SendBananaThrown (Angle, Velocity);
			gorilla.ThrowBanana ();
			thrown = true;
		}
	}
}
