using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework.Content;


namespace MatchemPoker
{
    /// <summary>
    /// The central class of the game.
    /// </summary>
    public class TileNpc : ITileGame
    {
        // The size of the grid used in the game
        private const int TILE_GRIDWIDTH = 6;
        private const int TILE_GRIDHEIGHT = 8;

        // How many lines info-screen has
        private const int INFOSCREEN_LINECOUNT = 18;

        // Level completed string drawing position
        private const float levelCompletedStringBeginY = 0.5798f;
        private const float levelCompletedStringCharSize = 0.0915f;

        /// <summary>
        /// Stringtable containing strings to be displayed when level's are finished. Content is loaded from levelcompleted.xml with XNA's content pipeline
        /// </summary>
        private string[] levelCompletedStrings;

        /// <summary>
        /// Stringtable containing info-screens lines loaded from infotextx.xml
        /// </summary>
        private string[] infoScreenLines;

        // score roll
        private const float rollWidth = 0.1525878f;
        private const float rollSpacing = 0.099f;
        private const float scoreRollStart = 0.503f;
        private static readonly float[] scoreRollXPos = new float[] { 0.099f, 0.099f + rollSpacing, scoreRollStart, scoreRollStart + rollSpacing, scoreRollStart + rollSpacing * 2, scoreRollStart + rollSpacing * 3, scoreRollStart + rollSpacing * 4 };


        // Info screen
        private const float infoScreenBeginY = 0.045776367f;
        private const float infoScreenYSpace = 0.083923339f;
        private const float infoScreenCharSize = 0.066293945f;

        protected NpcLevel m_level;


        // These values will be loaded and saved
        protected int m_difficulty;
        protected float m_blockTimerEffect;				// How much a destroying of a hand will affect to the level timer
        protected float m_timeTimerEffect;				// How much passed time is affecting to the level timer.
        protected int m_currentLevel;
        protected float m_targetTimer;
        protected float m_timer;
        protected int m_score;
        protected int m_displayScore;
        protected int m_highScore;
        protected int m_gameIsOn;
        protected int m_waitBeforeTimerStartsTime;

        protected float[] m_scoreRollPos = new float[7];
        protected float[] m_scoreRollTargetPos = new float[7];
        protected float[] m_scoreRollExTargetPos = new float[7];
        protected float[] m_scoreRollInc = new float[7];

                // Game specific drawing
        protected float m_infoScreenDisplayCounter;
        protected float m_timeSinceLastScore;
        protected float m_logoWobble;
        protected float m_logoWobbleInc;
        protected float m_bgAngle;
        protected float m_completedTextCounter;
        protected float m_completedTextAngle;
        protected string m_levelCompletedPoem;
        protected float m_levelCompletedCounter;
        protected float m_gameOverCounter;
        protected float m_pauseCounter;
        protected float m_eventCounter;
        protected float m_menuCounter;
        protected float m_menuModeCounter;
        protected float m_effectAngle;
        protected bool m_hasSaveGame;

        protected int m_bgIndex1, m_bgIndex2;
        protected float m_fadingBgCounter;

        System.Random rand = new System.Random();


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rend">ITileRenderer which is used for rendering and as an engine.</param>
        /// <param name="x">Game area X begin</param>
        /// <param name="y">Game area Y begin</param>
        /// <param name="width">Game area width</param>
        /// <param name="height">Game area height</param>
        public TileNpc(ITileRenderer renderer, float x, float y, float width, float height)
            : base(renderer, x, y, width, height)
        {
            m_level = new NpcLevel(renderer, m_pengine, TILE_GRIDWIDTH, TILE_GRIDHEIGHT);
            m_infoScreenDisplayCounter = -1;
            m_effectAngle = 0;
            m_bgIndex1 = 1;
            m_bgIndex2 = 0;
            m_fadingBgCounter = -1;
            m_menuCounter = -1;
            m_menuModeCounter = -1;

            m_timeSinceLastScore = 0;
            m_gameIsOn = 0;
            m_timer = 0;
            m_gameOverCounter = -1;
            m_pauseCounter = -1;
            m_levelCompletedPoem = null;
            m_timeTimerEffect = 65535;
            m_completedTextAngle = 0;
            m_completedTextCounter = -21;

            m_eventCounter = 0;
            m_bgAngle = 0;
            m_logoWobble = 0;
            m_logoWobbleInc = 0;

            m_currentLevel = 0;
            m_blockTimerEffect = 0;
            m_targetTimer = 0;

            m_levelCompletedCounter = 0;
            m_highScore = 0;
            m_timer = 0;
            m_score = 0;
            m_displayScore = 0;
            m_difficulty = 1;			// adults, 0 for kids ?
            m_hasSaveGame = false;
            m_level.SetGameArea(x, y - 0.12f, width, height + 0.1f);

            for (int f = 0; f < 7; f++)
            {
                m_scoreRollPos[f] = 0.0f;
                m_scoreRollTargetPos[f] = 0.0f;
                m_scoreRollExTargetPos[f] = 0.0f;
                m_scoreRollInc[f] = 0.0f;
            }
        }

        public override void Init(ContentManager Content)
        {
            // Load the levelCompletedStrings from the XML file.
            levelCompletedStrings = Content.Load<string[]>(@"levelcompleted");
            infoScreenLines = Content.Load<string[]>(@"infotexts");
            
            // Check if a save game exists
            m_hasSaveGame = CheckForSaveFile();
        }

        /// <summary>
        /// Run the game
        /// </summary>
        /// <param name="frameTime">How many seconds have passed since previous frame</param>
        /// <returns></returns>
        public override void GameRun(float frameTime)
        {
            // Limit the frametime a little bit, just in case
            if (frameTime > 0.1f) 
                frameTime = 0.1f;

            // Display the infoscreen if requested
            if (m_infoScreenDisplayCounter >= 0)
            {
                m_infoScreenDisplayCounter += frameTime;
            }

            m_effectAngle += frameTime;

            // Fade different backgrounds together.
            if (m_fadingBgCounter >= 0)
            {
                m_fadingBgCounter += frameTime;

                if (m_fadingBgCounter >= 1.0f)
                {
                    m_bgIndex1 = m_bgIndex2;
                    m_bgIndex2 = 0;
                    m_fadingBgCounter = -1;
                }
            }

            if (m_state == TileGameState.eTILEGAMESTATE_MENU)
            {
                m_menuCounter += frameTime;
                m_menuModeCounter += frameTime;
            }
            else
            {
                if (m_menuCounter >= 0)
                {
                    if (m_menuCounter > 3.0f) m_menuCounter = 3.0f;
                    m_menuCounter -= frameTime;
                }

                if (m_menuModeCounter >= 0)
                {
                    if (m_menuModeCounter > 2.0f) m_menuModeCounter = 2.0f;
                    m_menuModeCounter -= frameTime * 2;
                }
            }
            
            if ((m_gameIsOn != 0) && (m_level.State == LevelState.LevelStateIdle
                || m_level.State == LevelState.LevelStateLevelCompleted))
            {
                m_levelCompletedCounter += frameTime;
            }
            else
            {
                if (m_levelCompletedCounter > 3.0f) 
                    m_levelCompletedCounter = 3.0f;

                if (m_levelCompletedCounter > -1) 
                    m_levelCompletedCounter -= frameTime * 2;
            }

            m_eventCounter += frameTime * 4;

            while (m_eventCounter > 1.0f)
            {
                if (m_state == TileGameState.eTILEGAMESTATE_MENU)
                {
                    // Random spray particles in the menu-mode.
                    if (rand.Next(256) < 128)
                    {
                        float dx = (float)rand.NextDouble() - 0.5f;
                        float dy = (float)rand.NextDouble() - 1.0f;

                        m_pengine.Spray(
                                    (7 + rand.Next(5)) * 5,
                                    0.5f + dx, 0.16f, (float)rand.NextDouble() * 0.1f,
                                    dx, dy, (float)rand.NextDouble(),
                                    0, m_level.FruitSpray);


                    }

                    m_eventCounter = 0;
                }

                if (m_state == TileGameState.eTILEGAMESTATE_RUNGAME)
                {
                    // Fasten up the game.
                    m_timeTimerEffect -= 20.0f;

                    if (m_level.State == LevelState.LevelStateIdle)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (rand.Next(256) < 100)
                            {
                                m_pengine.Spray(10, (float)rand.NextDouble(), 0.33f + (float)rand.NextDouble() * 0.3f, 0.3f, 0, 0, 1.5f, 0, m_level.SparkleSpray);
                            }
                        }
                    }

                    int timeLimit = 4 + m_currentLevel * 4;

                    if (timeLimit > 20)
                    {
                        timeLimit = 20;
                    }

                    // Display hint
                    if (m_level.State == LevelState.LevelStateNormal && m_timeSinceLastScore > (float)timeLimit)
                    {
                        if (rand.Next(256) < 32)
                        {
                            m_level.WobbleHint();
                        }
                    }
                }

                m_eventCounter -= 1.0f;

                // Change the score to be displayed towards actual score
                int d = (m_score - m_displayScore);
                d /= 2;

                if (System.Math.Abs(d) < 1)
                {
                    if (m_displayScore > m_score)
                    {
                        d = -1;
                    }
                    else if (m_displayScore < m_score)
                    {
                        d = 1;
                    }
                }

                m_displayScore += d;
            }

            float i = m_timeTimerEffect;
            m_bgAngle += frameTime;

            // Wobble the logo
            float g = m_logoWobble * frameTime * 8.0f;
            m_logoWobbleInc -= g;
            g = m_logoWobbleInc * frameTime * 4.0f;
            m_logoWobbleInc -= g;
            g = m_logoWobbleInc * frameTime * 8.0f;
            m_logoWobble += g;

            if (m_gameOverCounter >= 0)
            {
                m_gameOverCounter += frameTime;
            }

            if (m_pauseCounter >= 0)
            {
                m_pauseCounter += frameTime;
            }

            m_completedTextAngle += frameTime;

            if (m_state == TileGameState.eTILEGAMESTATE_GAMEOVER
                || m_state == TileGameState.eTILEGAMESTATE_RUNGAME)
            {
                m_level.Run(frameTime);
            }

            if (m_state == TileGameState.eTILEGAMESTATE_RUNGAME)
            {
                int scoreAdd = m_level.TakeLevelScore();

                if (scoreAdd > 0)
                {
                    m_timeSinceLastScore = 0;
                }

                m_score += scoreAdd;

                if (m_score > m_highScore)
                {
                    m_highScore = m_score;
                }

                m_targetTimer += m_level.TakeLevelProgressChange() * m_blockTimerEffect;
                m_timer = m_targetTimer;

                if (m_completedTextCounter >= -20.0f)
                {
                    if (m_level.State != LevelState.LevelStateLevelCompleted)
                    {
                        // WAIT FOR level to finish completing the animation
                        int tlen = m_levelCompletedPoem.Length;
                        m_completedTextCounter += frameTime * 28.0f;

                        if (m_completedTextCounter > 800.0f)
                        {
                            m_completedTextCounter = -21.0f; // end and stop
                        }
                    }
                }
                
                switch (m_level.State)
                {
                    case LevelState.LevelStateNormal:
                        {
                            m_timeSinceLastScore += frameTime;

                            if (m_timeSinceLastScore > m_waitBeforeTimerStartsTime)
                            {
                                m_targetTimer += frameTime * m_timeTimerEffect;
                            }

                            if (m_level.DoingNothing == true)
                            {
                                if (m_targetTimer >= 256.0f * 65536.0f)
                                {
                                    LevelCompleted();
                                }

                                if (m_targetTimer <= 0)
                                {
                                    GameOver();
                                }
                            }
                        }
                        break;

                    case LevelState.LevelStateIdle:
                    default:
                        break;
                }
            }


            // Run the scorerolls displaying current time and scores
            int v = (int)((m_timer / 256.0f / 65536.0f) * 100.0f);

            if (v > 99)
            {
                v = 99;
            }

            m_scoreRollTargetPos[1] = (v % 10);
            m_scoreRollTargetPos[0] = ((v / 10) % 10);
            m_scoreRollTargetPos[6] = (m_displayScore % 10);
            m_scoreRollTargetPos[5] = ((m_displayScore / 10) % 10);
            m_scoreRollTargetPos[4] = ((m_displayScore / 100) % 10);
            m_scoreRollTargetPos[3] = ((m_displayScore / 1000) % 10);
            m_scoreRollTargetPos[2] = ((m_displayScore / 10000) % 10);


            bool rollChanged = false;

            for (int sr = 0; sr < 7; sr++)
            {
                if (m_scoreRollTargetPos[sr] != m_scoreRollExTargetPos[sr])
                {
                    rollChanged = true;
                }

                m_scoreRollExTargetPos[sr] = m_scoreRollTargetPos[sr];

                float delta = m_scoreRollTargetPos[sr] - m_scoreRollPos[sr];
                float negDelta = (m_scoreRollTargetPos[sr] - 10) - m_scoreRollPos[sr];
                float plusDelta = (m_scoreRollTargetPos[sr] + 10) - m_scoreRollPos[sr];

                if (System.Math.Abs(delta) > System.Math.Abs(negDelta))
                {
                    delta = negDelta;
                }

                if (System.Math.Abs(delta) > System.Math.Abs(plusDelta))
                {
                    delta = plusDelta;
                }

                m_scoreRollInc[sr] += delta * frameTime * 8.0f;
                m_scoreRollInc[sr] -= m_scoreRollInc[sr] * frameTime * 8.0f;
                m_scoreRollPos[sr] += m_scoreRollInc[sr];

                if (m_scoreRollPos[sr] < 0)
                {
                    m_scoreRollPos[sr] += 10;
                }
                else if (m_scoreRollPos[sr] >= 10)
                {
                    m_scoreRollPos[sr] -= 10;
                }
            }

            if (rollChanged)
            {
                m_renderer.EffectNotify(TileGameEffect.eEFFECT_SCORECHANGED, 0, 0);
            }
        }

        /// <summary>
        /// Draw all of the game related components.
        /// </summary>
        public override void GameDraw()
        {
            RenderBackground();
            
            // Render the level
            if (m_state != TileGameState.eTILEGAMESTATE_MENU
                && m_state != TileGameState.eTILEGAMESTATE_PAUSED)
            {
                m_level.Draw(m_renderer);
            }

            // Render the infoscreen if active
            if (m_infoScreenDisplayCounter >= 0)
            {
                RenderInfoScreen();
            }

            // Render the menu selections if menu is active
            if (m_menuCounter >= 0)
            {
                RenderMenu();
            }

            // Render the "info" button if it should be visible.
            if (m_menuModeCounter > 0)
            {
                RenderInfoButton();
            }

            if (m_state != TileGameState.eTILEGAMESTATE_MENU)
            {
                // Render the level completed string
                if (m_levelCompletedCounter > 0)
                {
                    RenderLevelCompletedString();
                }

                // Render the pause menu and selections if visible.
                if (m_pauseCounter > 0)
                {
                    RenderPauseMenu();
                }

                // Render the gameover screen when visible.
                if (m_gameOverCounter > 0 && m_infoScreenDisplayCounter < 0)
                {
                    RenderGameOverScreen();
                }
            }

            // Write the level completed string. Function will not do anything if correct mode isn't set
            WriteLevelCompletedString();

            // Render the hud if it should be visible.
            if (m_hudState > 0)
            {
                RenderHud();
            }

            // Render the logo if it should be visible.
            if (m_logoState > 0)
            {
                RenderLogo();
            }

            // Render particles
            m_pengine.Draw();
        }

        /// <summary>
        /// Renders Info screen
        /// </summary>
        private void RenderBackground()
        {
            uint tileIndex;

            // Background.
            if (m_fadingBgCounter < 0.0f)
            {
                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexBackground, (uint)m_bgIndex1, 0);
                m_renderer.RenderTile(0, 0, m_areaWidth, m_areaHeight, 0, 0, tileIndex, 0);
            }
            else
            {
                uint a = (uint)(m_fadingBgCounter * 256.0f);

                if (a > 255)
                {
                    a = 255;
                }

                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexBackground, (uint)m_bgIndex1, a);

                m_renderer.RenderTile(0, 0, m_areaWidth, m_areaHeight, 0, 0, tileIndex, 0);

                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexBackground, (uint)m_bgIndex2, (255 - a));

                m_renderer.RenderTile(0, 0, m_areaWidth, m_areaHeight, 0, 0, tileIndex, 0);
            }

            tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexBackground, 2, 200);

            m_renderer.RenderTile(-m_areaWidth / 2.0f, -m_areaWidth / 2.0f, m_areaWidth * 2.0f, m_areaWidth * 2.0f,
                                  m_bgAngle / 4.0f, 1, tileIndex, 0);
        }

        /// <summary>
        /// Renders menu
        /// </summary>
        private void RenderMenu()
        {
            int fade = (int)((2.0f - m_menuCounter) * 256.0f);
            fade /= 2;

            if (fade < 0)
            {
                fade = 0;
            }

            WriteEffectText((65536 - 4500) / 65536.0f, "START", 14000 / 65536.0f, fade);

            string hiscore = "HI " + m_highScore;

            WriteText(0, 0, hiscore, fade >> 8, 0.076f, 0.053f);
        }

        /// <summary>
        /// Renders Info button
        /// </summary>
        private void RenderInfoButton()
        {
            int fade = (int)((1.0f - m_menuModeCounter) * 256.0f);

            if (fade < 0)
            {
                fade = 0;
            }

            WriteEffectText((65536 * 5 / 4 + 4000) / 65536.0f, "INFO", 8000 / 65536.0f, fade);
        }

        /// <summary>
        /// Renders pause menu
        /// </summary>
        private void RenderPauseMenu()
        {
            int fade = 255 - (int)(m_pauseCounter * 2 * 255.0f);

            if (fade < 0)
            {
                fade = 0;
            }

            WriteEffectText(0.3f - (float)fade / 300.0f, "GAME", 0.144f, fade);
            WriteEffectText(0.5f + (float)fade / 300.0f, "PAUSED", 0.144f, fade);

            WriteEffectText(1.4f - 0.061f - (float)fade / 256.0f, "RESUME", 0.083f, fade);
            WriteEffectText(1.4f + 0.061f + (float)fade / 256.0f, "END", 0.083f, fade);
        }

        /// <summary>
        /// Renders level completed string
        /// </summary>
        private void RenderLevelCompletedString()
        {
            int fade = 255 - (int)(m_levelCompletedCounter * 256.0f);

            if (fade < 0)
            {
                fade = 0;
            }

            WriteEffectText(19000 / 65536.0f + fade / 256.0f, "LEVEL", 7000 / 65536.0f, fade);
            WriteEffectText(24000 / 65536.0f + fade / 256.0f, "COMPLETED", 9500 / 65536.0f, fade);
        }

        /// <summary>
        /// Renders logo
        /// </summary>
        private void RenderLogo()
        {
            float mx = 0.5f;
            float my = (-1.0f + m_logoState * 2);

            float w = (1.0f - m_logoWobble * 2);
            float h = (1.0f + m_logoWobble * 2);
            int fade = 255 - (int)(m_logoState * 255.0f);

            if (fade < 0)
            {
                fade = 0;
            }

            uint tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexLogo, 0, (uint)fade);
            m_renderer.RenderTile(mx - w / 2, my - h, w, h, 0, 0, tileIndex, 0);
        }

        /// <summary>
        /// Renders Hud
        /// </summary>
        private void RenderHud()
        {
            // draw the meter, clocklike here
            float x = -3000 / 65536.0f;
            float w = (65536 + 7000) / 65536.0f; //65536/2;
            float h = 19000 / 65536.0f;

            float y = -(1.0f - m_hudState) / 3 - 0.05f;

            uint tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexMeterBase, 1, 0);
            m_renderer.RenderTile(2000 / 65536.0f, y, 15000 / 65536.0f, h, 0, 0, tileIndex, 0);
            m_renderer.RenderTile(29000 / 65536.0f, y, 35000 / 65536.0f, h, 0, 0, tileIndex, 0);


            for (int f = 0; f < 7; f++)
            {
                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexMeter, 0, 0);
                m_renderer.RenderTile(scoreRollXPos[f] - rollWidth / 2,
                                      y + h / 2 - (h * 7 / 32),
                                      rollWidth,
                                      h * 14 / 32,
                                      0, 0, tileIndex, 0);
            }

            for (int yroll = -2; yroll <= 1; yroll++)
            {
                for (int f = 0; f < 7; f++)
                {
                    int num = (((int)(m_scoreRollPos[f] * 65536.0f) >> 16) - 1 - yroll) % 10;

                    if (num < 0)
                    {
                        num = 10 + num;
                    }

                    float yofs = ((m_scoreRollPos[f] + 100.0f) - (float)System.Math.Floor(m_scoreRollPos[f] + 100.0f));

                    yofs += ((float)yroll);
                    yofs -= 100.0f;

                    float ypos = (float)System.Math.Sin((float)yofs * 3.14159f / 4.0f) * (float)h / 3.3f;

                    float numheight = h / 2 - System.Math.Abs(ypos) * 13 / 8;

                    tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexMeter, (uint)(1 + num), 0);

                    m_renderer.RenderTile(scoreRollXPos[f] - rollWidth / 2,
                                           y + h / 2 - (numheight / 2) + ypos,
                                           rollWidth,
                                           numheight,
                                           0, 0, tileIndex, 0);
                }
            }

            tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexMeterBase, 0, 0);
            m_renderer.RenderTile(x, y, w, h, 0, 0, tileIndex, 0);

            const float pauseButtonXPos = 0.302f;

            // render pausebutton
            if (m_state == TileGameState.eTILEGAMESTATE_PAUSED)
            {
                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexExtra1, 1, 0);
                m_renderer.RenderTile(x + pauseButtonXPos, y - 1.0f + m_hudState, h * 2 / 3,
                                      h * 2 / 3 * m_areaHeight / m_areaWidth, 0, 0, tileIndex, 0);
            }
            else
            {
                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexExtra1, 0, 0);
                m_renderer.RenderTile(x + pauseButtonXPos, y - 1.0f + m_hudState, h * 2 / 3, h * 2 / 3 * m_areaHeight / m_areaWidth, 0, 0, tileIndex, 0);
            }

            tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexParticle, 0, 0);
            m_renderer.RenderTile(14000 / 65536.0f, 6000 / 65536.0f - 1.0f + m_hudState, 14000 / 65536.0f, 14000 / 65536.0f * 1 / 1,
                                  0, 0, tileIndex, 0);

            int n = ((m_currentLevel + 1) / 10);

            if (n == 0)
            {
                n = 10;
            }

            tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexMeter, (uint)n, 0);
            m_renderer.RenderTile(14200 / 65536.0f, 6200 / 65536.0f - 1.0f + m_hudState, 13000 / 65536.0f, 13000 / 65536.0f * 1 / 1,
                                  0, 0, tileIndex, 0);

            tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexParticle, 0, 0);
            m_renderer.RenderTile(20000 / 65536.0f, 9000 / 65536.0f - 1.0f + m_hudState, 14000 / 65536.0f, 14000 / 65536.0f * 1 / 1,
                                  0, 0, tileIndex, 0);

            n = ((m_currentLevel + 1) % 10);

            if (n == 0)
            {
                n = 10;
            }

            tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexMeter, (uint)n, 0);
            m_renderer.RenderTile(20200 / 65536.0f, 9200 / 65536.0f - 1.0f + m_hudState, 13000 / 65536.0f, 13000 / 65536.0f * 1 / 1,
                                  0, 0, tileIndex, 0);
        }

        /// <summary>
        /// Renders Game Over screen
        /// </summary>
        private void RenderGameOverScreen()
        {
            int fade = (int)((m_gameOverCounter - 0.61f * 2) * 256.0f);

            if (fade > 255)
            {
                fade = 255;
            }

            if (fade > 0)
            {
                WriteEffectText(0.122f - (float)(255 - fade) / 256.0f, "TOO BAD", 0.0762f, 255 - fade);

                fade = (int)((m_gameOverCounter - 0.61f * 3) * 256.0f);

                if (fade > 0)
                {
                    if (fade > 255)
                    {
                        fade = 255;
                    }

                    WriteEffectText(0.244f + (float)(255 - fade) / 256.0f, "GAME OVER", 0.137f, 255 - fade);

                    fade = (int)((m_gameOverCounter - 0.61f * 4) * 256.0f);

                    if (fade > 0)
                    {
                        if (fade > 255)
                        {
                            fade = 255;
                        }

                        WriteEffectText(0.61f, "YOUR SCORE", 0.0762f, 255 - fade);

                        fade = (int)((m_gameOverCounter - 0.61f * 5) * 256.0f);

                        if (fade > 0)
                        {
                            if (fade > 255)
                            {
                                fade = 255;
                            }

                            string testr = m_score.ToString();
                            WriteEffectText(0.6866f, testr, 0.137f, 255 - fade);

                            fade = (int)((m_gameOverCounter - 0.61f * 6) * 256.0f);

                            if (fade > 0)
                            {
                                if (fade > 255)
                                {
                                    fade = 255;
                                }

                                WriteEffectText(0.95f, "AT LEVEL", 0.076f, 255 - fade);

                                fade = (int)((m_gameOverCounter - 0.61f * 7) * 256.0f);

                                if (fade > 0)
                                {
                                    if (fade > 255)
                                    {
                                        fade = 255;
                                    }

                                    int i = m_currentLevel + 1;
                                    testr = i.ToString();
                                    WriteEffectText(1.05f, testr, 0.137f, 255 - fade);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Renders Info screen
        /// </summary>
        private void RenderInfoScreen()
        {
            float f;

            for (int l = 0; l < INFOSCREEN_LINECOUNT; l++)
            {
                if (infoScreenLines[l].Length > 0)
                {
                    f = 0.5f + ((float)l * 0.5f) - m_infoScreenDisplayCounter * 2;

                    if (f < 0)
                    {
                        f = 0;
                    }
                    else if (f > 65535)
                    {
                        f = 65535;
                    }

                    WriteEffectText(infoScreenBeginY + l * infoScreenYSpace,
                                    infoScreenLines[l], infoScreenCharSize,
                                    (int)(f * 255.0f));
                }
            }
        }

        /// <summary>
        /// Game have received a mouse event
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="etype">Type of a event (up, down, move)</param>
        public override void GameClick(float x, float y, MouseEventType etype)
        {
            if (m_state == TileGameState.eTILEGAMESTATE_SHOWINFOSCREEN)
            {
                if (etype == MouseEventType.eMOUSEEVENT_BUTTONDOWN)
                {
                    SetGameState(TileGameState.eTILEGAMESTATE_MENU);
                }
                
                return;
            }
            if (etype == MouseEventType.eMOUSEEVENT_BUTTONDOWN)
            {
                switch (m_state)
                {
                    case TileGameState.eTILEGAMESTATE_GAMEOVER:
                        if (m_level.State == LevelState.LevelStateIdle)
                        {
                            if (m_gameOverCounter < 6.0f)
                                m_gameOverCounter = 6.0f;
                            else
                                SetGameState(TileGameState.eTILEGAMESTATE_MENU);
                        }
                        return;

                    case TileGameState.eTILEGAMESTATE_PAUSED:
                        if (y > 83000 / 65536.0f)
                        {
                            if (y > 90000 / 65536.0f) GameOver(); else SetGameState(TileGameState.eTILEGAMESTATE_RUNGAME);
                        }
                        return;

                    case TileGameState.eTILEGAMESTATE_MENU:
                        {
                            if (x > 42000 / 65536.0f && y < 10000 / 65536.0f)
                            {
                                // Qt version had exit here. Disable for XNA.
                                //m_renderer.effectNotify(eTILEGAME_EFFECT.eEFFECT_EXIT,0,0);
                            }
                            else
                                if (y > 84000 / 65536.0f)
                                {
                                    SetGameState(TileGameState.eTILEGAMESTATE_SHOWINFOSCREEN);
                                }
                                else
                                {
                                    if (y < 60000 / 65536.0f) 
                                        m_logoWobbleInc += 16000 / 65536.0f;
                                    else
                                    {
                                        // If there's a save game and user clicked in upper-middle area, load it
                                        if (m_hasSaveGame && y < 72500 / 65536.0f)
                                        {
                                            if (Load())
                                            {
                                                SetGameState(TileGameState.eTILEGAMESTATE_PAUSED);
                                            }
                                        }
                                        else
                                        {
                                            // Start new game
                                            SetGameState(TileGameState.eTILEGAMESTATE_RUNGAME);
                                            m_logoWobble = -20000 / 65536.0f;
                                        }
                                    }
                                }
                            return;
                        }

                    default:
                        {
                            if (m_level.State == LevelState.LevelStateIdle)
                            {
                                NextLevel(-1);
                            }
                            else
                            {
                                if (y < 12000 / 65536.0F)
                                {
                                    if (x > 21000 / 65536.0F && x < 28000 / 65536.0f)
                                        SetGameState(TileGameState.eTILEGAMESTATE_PAUSED);
                                    else 
                                        return;
                                }
                            }
                        }
                        break;
                }
            }

            // Send event to the level 
            if (m_gameIsOn != 0 && m_state == TileGameState.eTILEGAMESTATE_RUNGAME
                && m_level.State != LevelState.LevelStateIdle)
            {
                switch (etype)
                {
                    case MouseEventType.eMOUSEEVENT_BUTTONDOWN: 
                        m_level.Click(x, y, PointerState.PointerDown); 
                        break;

                    case MouseEventType.eMOUSEEVENT_MOUSEDRAG: 
                        m_level.Click(x, y, PointerState.PointerDrag); 
                        break;

                    case MouseEventType.eMOUSEEVENT_BUTTONUP: 
                        m_level.Click(x, y, PointerState.PointerUp); 
                        break;
                }
            }
        }

        /// <summary>
        /// Called by the framework when game state have been changed. 
        /// </summary>
        public override void GameStateChanged()
        {
            m_infoScreenDisplayCounter = -1;
            m_pauseCounter = -1;

            switch (m_state)
            {
                case TileGameState.eTILEGAMESTATE_PAUSED:
                    {
                        m_completedTextCounter = -21.0f;
                        m_pauseCounter = 0;
                    }
                    break;
                case TileGameState.eTILEGAMESTATE_GAMEOVER:
                    {
                        m_completedTextCounter = -21.0f;
                        m_level.State = LevelState.LevelStateGameOver;
                    }
                    break;
                case TileGameState.eTILEGAMESTATE_RUNGAME:
                    {
                        if (m_gameIsOn == 0)
                        {
                            NextLevel(0);
                        }
                    }
                    break;

                case TileGameState.eTILEGAMESTATE_MENU:
                    {
                        ChangeBg(0);
                        m_timeTimerEffect = 1.0f;
                        m_logoWobble = 1.0f;
                        m_menuCounter = 0;
                        m_menuModeCounter = 0;
                        m_completedTextCounter = -21.0f;
                    }
                    break;

                case TileGameState.eTILEGAMESTATE_SHOWINFOSCREEN:
                    m_infoScreenDisplayCounter = 0;
                    ChangeBg(0);  // use green background for info screen
                    break;
            }
        }

        public override int GetIntAttribute(TileGameAttribute att, int arg)
        {
            return 0;
        }

        public override void SetIntAttribute(TileGameAttribute att, int set)
        {

        }

        public override void EndGame()
        {
            GameOver();
        }

        /// <summary>
        /// Resets the game for playing a level.
        /// </summary>
        /// <param name="restartAt">Which level will be played. -1 indicates current level + 1</param>
        private void NextLevel(int restartAt)
        {
            if (restartAt >= 0)
            {
                m_currentLevel = restartAt;
                m_score = 0;
                m_displayScore = 0;
                m_gameIsOn = 1;				// mark game ongoing
                m_gameOverCounter = -1;
            }
            else
            {
                m_currentLevel++;
                m_score += m_currentLevel * m_currentLevel * 10;
            }

            m_timeSinceLastScore = 0;
            m_completedTextCounter = -21;
            m_targetTimer = 65536 * 100;

            float temp = 5.0f / ((float)m_currentLevel + 6.0f);
            m_timeTimerEffect = -(int)(240000.0f * (1.0f - temp));
            m_timeTimerEffect -= m_currentLevel * 13000;						// linear component
            m_blockTimerEffect = (65536 * 22) / (m_currentLevel + 60);			// 30

            m_waitBeforeTimerStartsTime = 65536 * 15 / (3 + m_currentLevel);

            if (m_waitBeforeTimerStartsTime < 0) 
                m_waitBeforeTimerStartsTime = 0;

            m_level.CreateLevel(m_currentLevel);

            ChangeBg(-1);
        }

        /// <summary>
        /// Save gamestate completely
        /// </summary>
        /// <returns>True if succeeded</returns>
        public override bool Save()
        {
            IsolatedStorageFile IS = IsolatedStorageFile.GetUserStoreForApplication();

            try
            {
                using (IsolatedStorageFileStream IS_FS = IS.CreateFile("mpok.dat"))
                {
                    using (BinaryWriter bw = new BinaryWriter(IS_FS))
                    {
                        bw.Write(m_difficulty);
                        bw.Write(m_blockTimerEffect);
                        bw.Write(m_timeTimerEffect);
                        bw.Write(m_currentLevel);
                        bw.Write(m_targetTimer);
                        bw.Write(m_timer);
                        bw.Write(m_score);
                        bw.Write(m_displayScore);
                        bw.Write(m_highScore);
                        bw.Write(m_gameIsOn);
                        bw.Write(m_waitBeforeTimerStartsTime);

                        if (m_gameIsOn == 1)
                        {
                            m_level.SaveToFile(bw);
                        }
                    }
                }
            }
            catch (IsolatedStorageException /*e*/)
            {
                return false;
            }     
            
            return true;
        }

        /// <summary>
        /// Load gamestate completely
        /// </summary>
        /// <returns>True if succeeded</returns>
        public override bool Load()
        {
            try
            {
                IsolatedStorageFile IS = IsolatedStorageFile.GetUserStoreForApplication();

                using (IsolatedStorageFileStream IS_FS = IS.OpenFile("mpok.dat", FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(IS_FS))
                    {

                        bool rval = false;
                        m_difficulty = br.ReadInt32();
                        m_blockTimerEffect = br.ReadSingle();
                        m_timeTimerEffect = br.ReadSingle();
                        m_currentLevel = br.ReadInt32();
                        m_targetTimer = br.ReadSingle();
                        m_timer = br.ReadSingle();
                        m_score = br.ReadInt32();
                        m_displayScore = br.ReadInt32();
                        m_highScore = br.ReadInt32();
                        m_gameIsOn = br.ReadInt32();
                        m_waitBeforeTimerStartsTime = br.ReadInt32();

                        if (m_gameIsOn == 1)
                        {
                            m_level.LoadFromFile(br);
                            m_level.Difficulty = m_difficulty;
                            ChangeBg(-1);
                            rval = true;
                        }
                        return rval;
                    }
                }
            }
            catch (IsolatedStorageException /*e*/)
            {
                return false; 
            }            
        }

        /// <summary>
        /// Check if a save file exists without loading it
        /// </summary>
        /// <returns>true if save file exists and contains a valid game</returns>
        private bool CheckForSaveFile()
        {
            try
            {
                IsolatedStorageFile IS = IsolatedStorageFile.GetUserStoreForApplication();
                if (!IS.FileExists("mpok.dat"))
                {
                    return false;
                }

                using (IsolatedStorageFileStream IS_FS = IS.OpenFile("mpok.dat", FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(IS_FS))
                    {
                        // Read header info to check if game was in progress
                        int difficulty = br.ReadInt32();
                        float blockTimerEffect = br.ReadSingle();
                        float timeTimerEffect = br.ReadSingle();
                        int currentLevel = br.ReadInt32();
                        float targetTimer = br.ReadSingle();
                        float timer = br.ReadSingle();
                        int score = br.ReadInt32();
                        int displayScore = br.ReadInt32();
                        int highScore = br.ReadInt32();
                        int gameIsOn = br.ReadInt32();

                        return gameIsOn == 1;
                    }
                }
            }
            catch (IsolatedStorageException)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a save game exists
        /// </summary>
        /// <returns>true if save game exists</returns>
        public override bool HasSaveGame()
        {
            return m_hasSaveGame;
        }

        /// <summary>
        /// Handle ESC/Back button press
        /// </summary>
        public override void HandleEscapeKey()
        {
            switch (m_state)
            {
                case TileGameState.eTILEGAMESTATE_RUNGAME:
                    // In-game: go to pause screen
                    SetGameState(TileGameState.eTILEGAMESTATE_PAUSED);
                    break;

                case TileGameState.eTILEGAMESTATE_PAUSED:
                    // In pause: go back to menu
                    SetGameState(TileGameState.eTILEGAMESTATE_MENU);
                    break;

                case TileGameState.eTILEGAMESTATE_SHOWINFOSCREEN:
                    // Info screen: go back to menu
                    SetGameState(TileGameState.eTILEGAMESTATE_MENU);
                    break;

                case TileGameState.eTILEGAMESTATE_GAMEOVER:
                    // Game over: go back to menu
                    SetGameState(TileGameState.eTILEGAMESTATE_MENU);
                    break;

                // Menu state: return true to allow exit (handled by caller)
                case TileGameState.eTILEGAMESTATE_MENU:
                case TileGameState.eTILEGAMESTATE_NOTSET:
                default:
                    break;
            }
        }


        /// <summary>
        /// Writes a string with an effect used in the game.
        /// </summary>
        /// <param name="y">Y position of the render</param>
        /// <param name="text">Text to be rendered</param>
        /// <param name="charSize">Size of a single character</param>
        /// <param name="fade">Fade (and effect strength) for the text.</param>
        protected void WriteEffectText(float y, string text, float charSize, int fade)
        {
            if (fade < 0)
            {
                return;
            }
            else if (fade > 255)
            {
                fade = 255;
            }

            int length = text.Length;
            int totcount = 0;
            float[] cosin = m_level.CosineTable;
            float yadd;
            float xadd;
            float charSpace = charSize * 5 / 8;
            float x = 0.5f - (float)length * charSpace / 2;
            char[] str = text.ToCharArray();

            uint tileIndex;
            
            for (int i = 0; i < length; i++)
            {
                uint ch = (uint)str[i];
                if (ch >= 'a' && ch <= 'z') 
                    ch = ch - 'a' + 'A';        // to high-case
                
                ch -= 32;

                yadd = (cosin[(totcount * 850 + (int)(m_effectAngle * 65536.0f / 70.0f)) & 4095] * (float)fade / 50.0f * charSize);
                yadd += cosin[(1000 + (int)(m_effectAngle * 65536 / 10.0f) + totcount * 600) & 4095] / 50.0f * charSize;
                xadd = cosin[((int)(m_effectAngle * 400.0f) + totcount * 800) & 4095] / 50.0f * charSize;

                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexFont, ch, (uint)fade);

                m_renderer.RenderTile(x + xadd, y + yadd, charSize, charSize, fade * 0.1f, 0, tileIndex, 0);

                totcount++;
                x += charSpace;
            }
        }


        /// <summary>
        /// Write a string.
        /// </summary>
        /// <param name="x">Target X position</param>
        /// <param name="y">Target Y position</param>
        /// <param name="text">Text to be rendered</param>
        /// <param name="fade">Fade for the render</param>
        /// <param name="charSize">Size of a single character</param>
        /// <param name="charSpace">Space between characters</param>
        protected void WriteText(float x, float y, string text, int fade, float charSize, float charSpace)
        {
            int length = text.Length;
            char[] str = text.ToCharArray();

            uint tileIndex;

            for (int i = 0; i < length; i++)
            {
                uint ch = (uint)str[i] - 32;
                tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexFont, ch, (uint)fade);
                m_renderer.RenderTile(x, y, charSize, charSize, 0, 0, tileIndex, 0);
                x += charSpace;
            }
        }

        /// <summary>
        /// Write the level completed string with an effect.
        /// </summary>
        protected void WriteLevelCompletedString()
        {
            if (m_levelCompletedPoem == null)
            {
                return;
            }

            float x = 0;
            float y = levelCompletedStringBeginY;
            string text = m_levelCompletedPoem;
            float charSize = levelCompletedStringCharSize;
            float charSpace = levelCompletedStringCharSize * 5 / 8;
            int fade = 0;

            float[] cosineTable = m_level.CosineTable;
            int totCount = 0;
            int f;
            float g;
            string testr = null;
            string cur_line = null;

            float sizeinc;

            char[] splitSeparators = new char[] { ' ', '\n' };
            string[] words = text.Split(splitSeparators);
            int wordCount = 0;

            while (true)            // Loop until all words are processed
            {
                if (wordCount < words.Length)
                {
                    testr = words[wordCount];
                    wordCount++;
                }
                else
                {
                    testr = null;
                }

                if (cur_line != null
                    && (testr == null || wordCount >= words.Length
                    || (cur_line.Length + testr.Length) * charSpace > 0.95f))
                {
                    // Curline must be drawn
                    f = 0;
                    int j = cur_line.Length;
                    x = 0.5f - j * charSpace / 2;
                    x -= charSpace / 4;
                    // Loop through each character

                    char[] str = cur_line.ToCharArray();

                    for (int i = 0; i < j; i++)
                    {
                        uint ch = (uint)str[i];
                        if (ch >= 'a' && ch <= 'z') ch = ch - 'a' + 'A';        // to high-case
                        ch -= 32;

                        g = totCount - m_completedTextCounter;

                        if (g < -128)
                        {
                            g = g + 128;
                        }
                        else if (g < 0)
                        {
                            g = 0;
                        }

                        sizeinc = -System.Math.Abs(g) / 256;
                        fade = (int)(g * 32.0f);

                        if (fade > 255)
                        {
                            return;
                        }

                        fade = (int)System.Math.Abs(fade);

                        if (ch != 255 && fade < 255)
                        {
                            uint tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexFont, ch, (uint)fade);

                            m_renderer.RenderTile(x - sizeinc / 2 + cosineTable[(1000 + (int)(m_completedTextAngle / 20) + f * 600) & 4095] / 64.0f,
                                                    g / 64 + y - sizeinc / 2 + cosineTable[((int)(m_completedTextAngle / 30) + f * 800) & 4095] / 128.0f,
                                                    charSize + sizeinc,
                                                    charSize + sizeinc,
                                                    -(sizeinc * 8),
                                                    0,
                                                    tileIndex,
                                                    0);
                        }

                        totCount++;
                        x += charSpace;
                    }

                    cur_line = null;
                    x = 0;
                    y += charSpace * 4 / 3;
                }

                if (testr != null)
                {
                    if (cur_line == null)
                    {
                        cur_line = testr;
                    }
                    else
                    {
                        cur_line += " " + testr;
                    }
                }

                if (cur_line == null && wordCount >= words.Length)
                {
                    break;
                }
            }
        }


        /// <summary>
        /// Called when level is completed and state changes are needed.
        /// </summary>
        protected void LevelCompleted()
        {
            m_levelCompletedCounter = 0;
            m_completedTextCounter = -20;
            m_levelCompletedPoem = levelCompletedStrings[m_currentLevel % 44];
            m_renderer.EffectNotify(TileGameEffect.eEFFECT_LEVELCOMPLETED, 0, 0);
            m_level.State = LevelState.LevelStateLevelCompleted;
            m_score += m_currentLevel * 100;
            m_pengine.Spray(
                        20,
                        0.5f, 20000.0f / 65536.0f, 1000 / 65536.0f,
                        0, -50000.0f / 65536.0f, 1.0f,
                        0, m_level.FruitSpray);
        }

        /// <summary>
        /// Called when game is over and state changes are needed.
        /// </summary>
        protected void GameOver()
        {
            m_gameOverCounter = 0;
            m_gameIsOn = 0;
            SetGameState(TileGameState.eTILEGAMESTATE_GAMEOVER);
        }


        /// <summary>
        /// Change the background image smoothly
        /// </summary>
        /// <param name="newBg">New background to be faded to. -1 indicates "next" background.</param>
        protected void ChangeBg(int newBg)         // default was -1
        {
            if (newBg == -1)
            {
                newBg = ((m_currentLevel + 1) & 1);
            }

            m_bgIndex2 = newBg;

            if (m_bgIndex1 == m_bgIndex2)
            {
                return;		// no need
            }

            m_fadingBgCounter = 0;		// START fading
        }
    }

}

