using System;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.SamplesFramework
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class FarseerPhysicsGame : Game
    {
        private GraphicsDeviceManager _graphics;

        public FarseerPhysicsGame()
        {
            Window.Title = "Farseer Samples Framework";
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferMultiSampling = true;
#if DESKTOP || XBOX
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            ConvertUnits.SetDisplayUnitToSimUnitRatio(24f);
            IsFixedTimeStep = true;
#elif WINDOWS_PHONE
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 480;
            ConvertUnits.SetDisplayUnitToSimUnitRatio(16f);
            IsFixedTimeStep = false;
#endif
#if DESKTOP
            _graphics.IsFullScreen = false;
#elif XBOX || WINDOWS_PHONE
            _graphics.IsFullScreen = true;
#endif

            Content.RootDirectory = "Content";

            //new-up components and add to Game.Components
            ScreenManager = new ScreenManager(this);
            Components.Add(ScreenManager);

            FrameRateCounter frameRateCounter = new FrameRateCounter(ScreenManager);
            frameRateCounter.DrawOrder = 101;
            Components.Add(frameRateCounter);
        }

        public ScreenManager ScreenManager { get; set; }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            SimpleDemo1 simple1 = new SimpleDemo1();
            SimpleDemo2 simple2 = new SimpleDemo2();
            SimpleDemo3 simple3 = new SimpleDemo3();
            SimpleDemo4 simple4 = new SimpleDemo4();
            SimpleDemo5 simple5 = new SimpleDemo5();
            SimpleDemo6 simple6 = new SimpleDemo6();
            SimpleDemo7 simple7 = new SimpleDemo7();
            SimpleDemo8 simple8 = new SimpleDemo8();
            SimpleDemo9 simple9 = new SimpleDemo9();

            AdvancedDemo1 advanced1 = new AdvancedDemo1();
            AdvancedDemo2 advanced2 = new AdvancedDemo2();
            AdvancedDemo3 advanced3 = new AdvancedDemo3();
            AdvancedDemo4 advanced4 = new AdvancedDemo4();
            AdvancedDemo5 advanced5 = new AdvancedDemo5();

            GameDemo1 game1 = new GameDemo1();

            MenuScreen menuScreen = new MenuScreen("Farseer Samples");

            menuScreen.AddMenuItem("Simple Samples", EntryType.Separator, null);
            menuScreen.AddMenuItem(simple1.GetTitle(), EntryType.Screen, simple1);
            menuScreen.AddMenuItem(simple2.GetTitle(), EntryType.Screen, simple2);
            menuScreen.AddMenuItem(simple3.GetTitle(), EntryType.Screen, simple3);
            menuScreen.AddMenuItem(simple4.GetTitle(), EntryType.Screen, simple4);
            menuScreen.AddMenuItem(simple5.GetTitle(), EntryType.Screen, simple5);
            menuScreen.AddMenuItem(simple6.GetTitle(), EntryType.Screen, simple6);
            menuScreen.AddMenuItem(simple7.GetTitle(), EntryType.Screen, simple7);
            menuScreen.AddMenuItem(simple8.GetTitle(), EntryType.Screen, simple8);
            menuScreen.AddMenuItem(simple9.GetTitle(), EntryType.Screen, simple9);

            menuScreen.AddMenuItem("Advanced Samples", EntryType.Separator, null);
            menuScreen.AddMenuItem(advanced1.GetTitle(), EntryType.Screen, advanced1);
            menuScreen.AddMenuItem(advanced2.GetTitle(), EntryType.Screen, advanced2);
            menuScreen.AddMenuItem(advanced3.GetTitle(), EntryType.Screen, advanced3);
            menuScreen.AddMenuItem(advanced4.GetTitle(), EntryType.Screen, advanced4);
            menuScreen.AddMenuItem(advanced5.GetTitle(), EntryType.Screen, advanced5);

            menuScreen.AddMenuItem("Game Samples", EntryType.Separator, null);
            menuScreen.AddMenuItem(game1.GetTitle(), EntryType.Screen, game1);

            menuScreen.AddMenuItem("", EntryType.Separator, null);
            menuScreen.AddMenuItem("Exit", EntryType.ExitItem, null);

            ScreenManager.AddScreen(new BackgroundScreen());
            ScreenManager.AddScreen(menuScreen);
            ScreenManager.AddScreen(new LogoScreen(TimeSpan.FromSeconds(3.0)));
        }
    }
}