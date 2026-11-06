//-----------------------------------------------------------------------------
// LoadingScreen.cs
//
// The title card shown while the content loads in the background: the RacingGame
// logo, an orange progress bar, what is being loaded, a tip and the version.
// The splash screen fades the same card out, so the two screens cross-fade.
//-----------------------------------------------------------------------------
using Microsoft.Xna.Framework;
using System;
using System.Reflection;
using System.Threading.Tasks;
using RacingGame.Graphics;
using RacingGame.Helpers;

namespace RacingGame.GameScreens
{
	/// <summary>
	/// Loading screen
	/// </summary>
	class LoadingScreen : IGameScreen
	{
		/// <summary>Colour of the card behind the logo</summary>
		static readonly Color CardColor = new Color(18, 18, 20);
		static readonly Color Orange = new Color(255, 156, 0);

		/// <summary>Fade in from black when the game starts, in seconds</summary>
		const float FadeInTime = 0.35f;
		/// <summary>The bar fills at least this fast once the content is loaded (per second)</summary>
		const float FinishSpeed = 3.0f;
		const float SecondsPerTip = 4.0f;
		/// <summary>
		/// The card stays at least this long (even if the content is loaded sooner) so the logo
		/// and the first tip can be seen; the bar fills over this time.
		/// </summary>
		const float MinimumShowTime = 2.5f;

		static readonly string[] Tips =
		{
			"Brake before the corner, not in it",
			"Build your own tracks with Tarmac Toolbox",
			"Race your friends over your local network: Start Race, Multiplayer",
			"Custom tracks can be shared in a LAN race",
			"Press Esc during a race for the menu",
			"Turn off Post Screen Effects in Options if the game runs slowly",
		};

		/// <summary>
		/// How far the bar should be for each status the loading thread reports
		/// </summary>
		static float TargetForStatus(string status)
		{
			switch (status)
			{
				case "Models...": return 0.10f;
				case "Landscape...": return 0.35f;
				case "Textures...": return 0.75f;
				default: return 1.0f;
			}
		}

		private string loadingStatus = "Starting...";
		private volatile float targetProgress = 0.05f;
		private float shownProgress = 0;
		private float startTime = -1;
		private float lastTime = -1;
		private readonly int tipOffset = RandomHelper.GetRandomInt(Tips.Length);

		public LoadingScreen()
		{
			//Setup the handler before we start the thread
			RacingGameManager.LoadEvent += new EventHandler<EventArgs>(LoadEvent);
		}
		/// <summary>
		/// Gather input on the loading screen and update it if progress has
		/// changed in loading the game.
		/// </summary>
		public void Update(GameTime gameTime)
		{
			if (RacingGameManager.LoadingTask == null)
			{
				RacingGameManager.LoadingTask = Task.Run(() =>
					RacingGameManager.LoadResources());
			}

			if (RacingGameManager.LoadingTask.IsCompleted)
			{
				if (RacingGameManager.LoadingTask.IsFaulted)
				{
					Log.Write("Content loading failed: " +
						RacingGameManager.LoadingTask.Exception);
				}
				// Proceed to next screen (see Render), once the bar is full
			}
		}

		public void LoadEvent(object sender, EventArgs e)
		{
			loadingStatus = (string)sender;
			targetProgress = TargetForStatus(loadingStatus);
		}

		/// <summary>
		/// The game version shown in the corner (from the assembly, set with
		/// Version in the project file)
		/// </summary>
		public static string VersionText
		{
			get
			{
				try
				{
					string v = Assembly.GetEntryAssembly()?
						.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
					if (string.IsNullOrEmpty(v))
						return "";
					int plus = v.IndexOf('+');
					return "v" + (plus >= 0 ? v.Substring(0, plus) : v);
				}
				catch (Exception)
				{
					return "";
				}
			}
		}

		/// <summary>
		/// Draw the title card: dark background, logo, progress bar, texts. alpha fades
		/// everything (the splash screen uses it to fade the card away); status and tip
		/// are skipped when null.
		/// </summary>
		public static void RenderCard(float alpha, float progress, string status, string tip,
			float tipAlpha)
		{
			int w = BaseGame.Width, h = BaseGame.Height;
			BaseGame.UI.FillRect(new Rectangle(0, 0, w, h), CardColor * alpha);

			// Logo, centred in the upper half (1024 x 374 art)
			int logoW = (int)(Math.Min(w * 0.55f, h * 0.55f * 1024 / 374f));
			int logoH = logoW * 374 / 1024;
			BaseGame.UI.RenderLogo(new Rectangle((w - logoW) / 2, (int)(h * 0.40f) - logoH / 2,
				logoW, logoH), Color.White * alpha);

			// Progress bar
			int barW = (int)(w * 0.36f), barH = Math.Max(6, (int)(h * 0.014f));
			Rectangle bar = new Rectangle((w - barW) / 2, (int)(h * 0.66f), barW, barH);
			BaseGame.UI.FillRect(bar, new Color(50, 50, 54) * alpha);
			int fill = (int)Math.Round(barW * MathHelper.Clamp(progress, 0, 1));
			BaseGame.UI.FillRect(new Rectangle(bar.X, bar.Y, fill, barH), Orange * alpha);
			BaseGame.UI.FillRect(new Rectangle(bar.X, bar.Y, fill, Math.Max(1, barH / 3)),
				new Color(255, 205, 120) * alpha);

			if (status != null)
				TextureFont.WriteTextCentered(w / 2, bar.Bottom + (int)(h * 0.035f), status,
					new Color(210, 210, 210) * alpha, 0.8f);
			if (tip != null)
				TextureFont.WriteTextCentered(w / 2, (int)(h * 0.86f), tip,
					new Color(150, 150, 150) * (alpha * tipAlpha), 0.7f);

			string version = VersionText;
			if (version.Length > 0)
				TextureFont.WriteText(w - TextureFont.GetTextWidth(version) - BaseGame.XToRes(14),
					h - TextureFont.Height - BaseGame.YToRes(10), version,
					new Color(110, 110, 110) * alpha);
		}

		/// <summary>
		/// Render loading screen
		/// </summary>
		public bool Render()
		{
			float now = BaseGame.TotalTime;
			if (startTime < 0)
				startTime = lastTime = now;
			float dt = Math.Max(0, now - lastTime);
			lastTime = now;

			// Ease towards what the loading thread reported; once everything is loaded,
			// finish the bar quickly (no fixed wait, only as long as the bar needs)
			float elapsed = now - startTime;
			bool loaded = RacingGameManager.ContentLoaded;
			// (the bar is paced over the minimum show time, so it never races ahead of it)
			float paced = Math.Min(1, elapsed / MinimumShowTime);
			float target = Math.Min(loaded ? 1.0f : Math.Min(targetProgress, 0.95f), paced);
			shownProgress += (target - shownProgress) * Math.Min(1, dt * 6);
			if (loaded && paced >= 1)
				shownProgress = Math.Min(1, shownProgress + FinishSpeed * dt);

			float tipTime = elapsed % SecondsPerTip;
			float tipAlpha = Math.Min(1, Math.Min(tipTime * 2, (SecondsPerTip - tipTime) * 2));
			string tip = Tips[(tipOffset + (int)(elapsed / SecondsPerTip)) % Tips.Length];

			// Fade in from black at startup (texts fade with the alpha)
			float fade = Math.Min(1, elapsed / FadeInTime);
			RenderCard(fade, shownProgress, loadingStatus, tip, tipAlpha);

			return loaded && elapsed >= MinimumShowTime && shownProgress >= 0.999f;
		}
	}
}
