using System;
using System.Collections.Generic;

#if ANDROID
using Android.App;
#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;

namespace MonoGame.Samples.VideoPlayer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont font;

        Video video;
        Microsoft.Xna.Framework.Media.VideoPlayer videoPlayer;

        public Game1 ()
        {
            graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            graphics.PreferMultiSampling = true;
            graphics.IsFullScreen = true;

            graphics.SupportedOrientations =
                DisplayOrientation.LandscapeLeft |
                DisplayOrientation.LandscapeRight;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize ()
        {
            // TODO: Add your initialization logic here

            base.Initialize ();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent ()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("spriteFont1");

            video = Content.Load<Video>("sintel_trailer");
            videoPlayer = new Microsoft.Xna.Framework.Media.VideoPlayer();
            videoPlayer.IsLooped = true;

            _slices.Add("start", new Slice(
                TimeSpan.Zero, TimeSpan.Zero,
                gt => videoPlayer.Play(video),
                null));

            _slices.Add("loop", new Slice(
                TimeSpan.Zero, TimeSpan.FromSeconds(video.Duration.TotalSeconds * 1.5),
                gt => videoPlayer.IsLooped = true,
                gt => videoPlayer.IsLooped = false));

            _slices.Add("pause", new Slice(
                TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(2),
                gt => videoPlayer.Pause(),
                gt => videoPlayer.Resume()));

            _slices.Add("mute", new Slice(
                TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(3),
                gt => videoPlayer.IsMuted = true,
                gt => videoPlayer.IsMuted = false));

            _slices.Add("force_drop", new Slice(
                TimeSpan.FromSeconds(22), TimeSpan.Zero,
                gt => Console.WriteLine("Forcing dropped frames..."),
                gt => System.Threading.Thread.Sleep(2000)));
        }

        private Dictionary<string, Slice> _slices = new Dictionary<string, Slice>();

        protected override void Update (GameTime gameTime)
        {
            base.Update(gameTime);

            foreach (var entry in _slices)
                entry.Value.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            Vector2 maxSize;
            if (Window.CurrentOrientation == DisplayOrientation.Portrait)
            {
                maxSize = new Vector2(
                    graphics.GraphicsDevice.Viewport.Width,
                    graphics.GraphicsDevice.Viewport.Height);
            }
            else
            {
                maxSize = new Vector2(
                    graphics.GraphicsDevice.Viewport.Height,
                    graphics.GraphicsDevice.Viewport.Width);
            }

            spriteBatch.Begin();

            string stateString = videoPlayer.State.ToString();
            spriteBatch.DrawString(
                font, stateString,
                new Vector2(maxSize.X - font.MeasureString(stateString).X, 0), Color.White);

            if (videoPlayer.State != MediaState.Stopped)
            {
                float xScale = maxSize.X / video.Width;
                float yScale = maxSize.Y / video.Height;

                float scale = Math.Min(xScale, yScale);

                var rect = new Rectangle(0, 0, (int)(scale * video.Width), (int)(scale * video.Height));

                rect.X = (int)((maxSize.X - rect.Width) / 2);
                rect.Y = (int)((maxSize.Y - rect.Height) / 2);

                spriteBatch.Draw(videoPlayer.GetTexture(), rect, Color.White);
                spriteBatch.DrawString(font, videoPlayer.PlayPosition.ToString(), Vector2.Zero, Color.White);

                if (_slices["pause"].IsActive)
                {
                    var timeLeft = _slices["pause"].EndTime - gameTime.TotalGameTime;
                    spriteBatch.DrawString(
                        font, "Pausing for: " + timeLeft.ToString(),
                        new Vector2(0, maxSize.Y - font.LineSpacing), Color.White);
                }

                if (_slices["mute"].IsActive)
                {
                    var timeLeft = _slices["mute"].EndTime - gameTime.TotalGameTime;
                    spriteBatch.DrawString(
                        font, "Muting for: " + timeLeft.ToString(),
                        new Vector2(0, maxSize.Y - font.LineSpacing), Color.White);
                }
            }
            else
            {
                string message = "Video has ended, let the Game BEGIN!!";
                var messageSize = font.MeasureString(message);
                var position = new Vector2(
                    (maxSize.X - messageSize.X) / 2,
                    (maxSize.Y - messageSize.Y) / 2);
                spriteBatch.DrawString(font, message, position, Color.White);
            }

            if (_slices["force_drop"].IsActive)
            {
                string message = "FORCING DROPPED FRAMES";
                var messageSize = font.MeasureString(message);
                var position = new Vector2(
                    (maxSize.X - messageSize.X) / 2,
                    (maxSize.Y - messageSize.Y) / 2);
                spriteBatch.DrawString(font, message, position, Color.Red);
            }
            spriteBatch.End();
        }

        private class Slice
        {
            private enum SliceState
            {
                NotStarted,
                Started,
                Ended
            }

            private SliceState _state;
            private Action<GameTime> _startAction;
            private Action<GameTime> _endAction;

            public Slice(
                TimeSpan startTime, TimeSpan length,
                Action<GameTime> startAction, Action<GameTime> endAction)
            {
                if (startTime < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("startTime", "startTime must be non-negative");
                if (length < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException("length", "length must be non-negative");

                _startTime = startTime;
                _endTime = startTime + length;

                _startAction = startAction;
                _endAction = endAction;
            }

            public bool IsActive
            {
                get { return _state == SliceState.Started; }
            }

            private TimeSpan _startTime;
            public TimeSpan StartTime
            {
                get { return _startTime; }
            }

            private TimeSpan _endTime;
            public TimeSpan EndTime
            {
                get { return _endTime; }
            }

            public void Update(GameTime gameTime)
            {
                switch (_state)
                {
                case SliceState.NotStarted:
                    if (gameTime.TotalGameTime >= _startTime)
                    {
                        _state = SliceState.Started;
                        SafeInvoke(_startAction, gameTime);
                    }
                    break;
                case SliceState.Started:
                    if (gameTime.TotalGameTime >= _endTime)
                    {
                        _state = SliceState.Ended;
                        SafeInvoke(_endAction, gameTime);
                    }
                    break;
                case SliceState.Ended:
                    // Nothing to do.
                    break;
                }
            }

            private void SafeInvoke(Action<GameTime> action, GameTime gameTime)
            {
                if (action == null)
                    return;
                action(gameTime);
            }
        }
    }
}
