
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MatchemPoker
{
    
    /// <summary>
    /// Enumeration of attributes used in Tilegames. Currently not in use.
    /// </summary>
    public enum TileGameAttribute
    {
        eSCORE = 1,
        eATTRIBUTE_HIGHSCORE,
        eTIMELIMIT_CURRENT,
        eTIMELIMIT_MAX,
        eATTRIBUTE_CURRENTLEVEL,
        eATTRIBUTE_DIFFICULTY
    };


    /// <summary>
    /// Enumeration for general state of a TileGame. 
    /// </summary>
    public enum TileGameState
    {
        eTILEGAMESTATE_NOTSET = 1,
        eTILEGAMESTATE_MENU,
        eTILEGAMESTATE_RUNGAME,
        eTILEGAMESTATE_GAMEOVER,
        eTILEGAMESTATE_PAUSED,
        eTILEGAMESTATE_SHOWINFOSCREEN,
        //etc..
    };

    /// <summary>
    /// Enumeration for mouse event type
    /// </summary>
    public enum MouseEventType
    {
        eMOUSEEVENT_BUTTONUP = 1,
        eMOUSEEVENT_BUTTONDOWN,
        eMOUSEEVENT_MOUSEDRAG
    };



    /// <summary>
    /// Interface for a TileGame. The npclevel.cs implement's this on MatchEmPoker's XNA version.
    /// </summary>
    public abstract class ITileGame
    {
        protected ITileRenderer m_renderer;
        protected ParticleEngine m_pengine;

        protected float m_areaX, m_areaY, m_areaWidth, m_areaHeight;
        protected TileGameState m_state;
        protected float m_logoState;			// states: 0 - invisible, 65536 - completely visible
        protected float m_hudState;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rend">ITileRenderer which is used for rendering and as an engine.</param>
        /// <param name="x">Game area X begin</param>
        /// <param name="y">Game area Y begin</param>
        /// <param name="width">Game area width</param>
        /// <param name="height">Game area height</param>
        public ITileGame(ITileRenderer rend, float x, float y, float width, float height)
        {
            m_state = TileGameState.eTILEGAMESTATE_NOTSET;
            m_renderer = rend;
            m_logoState = 0.0f;
            m_hudState = 0.0f;
            m_pengine = new ParticleEngine(rend);
            SetGameArea(x, y, width, height);
        }

        /// <summary>
        /// Call this to end the game, if ongoing.
        /// </summary>
        public virtual void EndGame() { }

        /// <summary>
        /// ITileGame have received a mouse event
        /// </summary>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="etype">type of the event (up, down, move)</param>
        public void Click(float x, float y, MouseEventType etype)
        {
            GameClick(x,y, etype);
        }

        /// <summary>
        /// Run the tilegame framework
        /// </summary>
        /// <param name="frameTime">Seconds after the previous frame</param>
        /// <returns>Returns always 1</returns>
        public void Run(float frameTime)
        {
            float hudStateTarget = 0.0f;
            float logoStateTarget = 0.0f;

            switch (m_state)
            {
                case TileGameState.eTILEGAMESTATE_NOTSET:
                    {
                        // Always start at menu, but check if save game exists
                        SetGameState(TileGameState.eTILEGAMESTATE_MENU);
                    }
                    break;

                case TileGameState.eTILEGAMESTATE_MENU:
                    logoStateTarget = 1.0f;
                    break;

                case TileGameState.eTILEGAMESTATE_RUNGAME:
                    {
                        hudStateTarget = 1.0f;
                    }
                    break;
            }

            m_logoState += (logoStateTarget - m_logoState) * frameTime * 8.0f;
            m_hudState += (hudStateTarget - m_hudState) * frameTime * 4.0f;

            GameRun(frameTime);
            m_pengine.Run(frameTime);
        }

        /// <summary>
        /// Draw (render) the TileGame famework using ITileRenderer given at construction phase.
        /// </summary>
        /// <returns></returns>
        public void Draw()
        {
            m_renderer.RenderBackground(0);
            GameDraw();
            m_renderer.RenderForeground(0);
        }

        /// <summary>
        /// Set the overall state of the game
        /// </summary>
        /// <param name="newstate">State to be set</param>
        public void SetGameState(TileGameState newstate)
        {
            switch (newstate)
            {
                case TileGameState.eTILEGAMESTATE_MENU:
                    m_renderer.EffectNotify(TileGameEffect.eEFFECT_MENU, 0, 0);
                    break;
                case TileGameState.eTILEGAMESTATE_GAMEOVER:
                    m_renderer.EffectNotify(TileGameEffect.eEFFECT_GAMEOVER, 0, 0);
                    break;
            }

            m_state = newstate;
            GameStateChanged();	// notify
        }

        /// <summary>
        /// XNA specific initialization for content
        /// </summary>
        /// <param name="Content">Initialized XNA ContentManager object</param>
        public abstract void Init(ContentManager Content);

        public abstract void GameClick(float x, float y, MouseEventType etype);
        public abstract void GameRun(float frameTime);
        public abstract void GameDraw();
        public abstract void GameStateChanged();

        public abstract int GetIntAttribute(TileGameAttribute att, int arg);
        public abstract void SetIntAttribute(TileGameAttribute att, int set);

        /// <summary>
        /// Completely save the state of a tilegame
        /// </summary>
        /// <returns>true if succeeded</returns>
        public abstract bool Load();

        /// <summary>
        /// Completely load the state of a tilegame
        /// </summary>
        /// <returns>true if succeeded</returns>
        public abstract bool Save();

        /// <summary>
        /// Check if a save game file exists
        /// </summary>
        /// <returns>true if save game exists</returns>
        public abstract bool HasSaveGame();

        /// <summary>
        /// Handle ESC/Back button press
        /// </summary>
        public abstract void HandleEscapeKey();

        /// <summary>
        /// Get the current game state
        /// </summary>
        /// <returns>The current TileGameState</returns>
        public TileGameState GetCurrentGameState()
        {
            return m_state;
        }

        /// <summary>
        /// Pause the TileGame
        /// </summary>
        public virtual void Pause()
        {
        }

        /// <summary>
        /// Resume from pause
        /// </summary>
        public void Resume()
        {
        }
        
        /// <summary>
        /// Reset the game area
        /// </summary>
        /// <param name="x">Game area X begin</param>
        /// <param name="y">Game area Y begin</param>
        /// <param name="width">Game area width</param>
        /// <param name="height">Game area height</param>
        public void SetGameArea(float x, float y, float width, float height)
        {
            m_areaX = x;
            m_areaY = y;
            m_areaWidth = width;
            m_areaHeight = height;
        }
    }
}