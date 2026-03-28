using System.IO;
using Microsoft.Xna.Framework;

namespace MatchemPoker
{
    /// <summary>
    /// Overall mainstate of the level
    /// </summary>
    public enum LevelState
    {
        LevelStateNormal,
        LevelStateBeginnig,
        LevelStateGameOver,
        LevelStateLevelCompleted,
        LevelStateIdle
    }

    public enum PointerState
    {
        PointerDown = 0,
        PointerDrag,
        PointerUp
    }

    /// <summary>
    /// A game level which contains of width * height grid items and everything needed to manipulate them.
    /// </summary>
    public class NpcLevel
    {
        // Extra card - indices
        private const int EMPTY_CARD = 100;
        private const int JOKER_CARD = 101;

        // Position of a deck when handing the cards at the beginning
        private /*const*/ Vector2 DECK_POSITION = new Vector2(52000.0f / 65536.0f, 14000.0f / 65536.0f);

        // Make the tiles little larger 
        private const float TILE_SIZE_ADD = 3000.0f / 65536.0f;

        protected int m_destroyingRound;
        protected NpcGridItem[] m_changing = new NpcGridItem[2];
        protected int m_dragBegan;
        protected float m_changingCounter;
        protected int m_illegalMoveCounter;

        protected NpcGridItem m_hint1, m_hint2;

        protected float m_startupCounter;

        protected Point m_gridSize;

        protected Vector2 m_itemSize;

        protected NpcGridItem[] m_grid;

        protected int m_levelScore;
        protected int m_levelProgressChange;

        protected float m_floatingAngle;
        protected int m_currentLevel;

        protected float m_deckVisibility;

        protected float m_areaX, m_areaY;
        protected float m_areaWidth, m_areaHeight;

        ITileRenderer m_renderer;
        ParticleEngine m_pengine;

        System.Random rand = new System.Random();

        protected LevelState m_state;

        public LevelState State
        {
            get { return m_state; }
            set
            {
                m_state = value;
                switch (m_state)
                {
                    case LevelState.LevelStateBeginnig:
                        ZeroMask(GridFlags.GRIDFLAG_CALCULATION_TEMP);
                        m_renderer.EffectNotify(TileGameEffect.eEFFECT_NEWLEVEL, 0, 0);
                        m_startupCounter = 5.0f;
                        break;

                    case LevelState.LevelStateGameOver:
                        m_startupCounter = (m_gridSize.X * m_gridSize.Y);
                        break;

                    case LevelState.LevelStateLevelCompleted:
                        ZeroMask(GridFlags.GRIDFLAG_CALCULATION_TEMP);
                        m_startupCounter = 5.0f;
                        break;
                };
            }
        }

        public int Difficulty { get; set; }

        // Does level has moves left (0 - no moves)
        public int HasMoves { get; private set; }

        protected float[] m_cosineTable = new float[4096];
        public float[] CosineTable
        {
            get { return m_cosineTable; }
            private set { m_cosineTable = value; }
        }

        // Particlespray structures for different kind of particles. 
        public ParticleSprayType ScoreSpray { get; private set; }
        public ParticleSprayType SmokeSpray { get; private set; }
        public ParticleSprayType SparkleSpray { get; private set; }
        public ParticleSprayType FruitSpray { get; private set; }
        public ParticleSprayType MorphSpray { get; private set; }
        
        /// <summary>
        /// Is something happening in a level,.. some pieces dropping, destroying, etc. 
        /// </summary>
        public bool DoingNothing { get; private set; }
        
        /// <summary>
        /// Constructs a level and initializes it.
        /// </summary>
        /// <param name="renderer">Reference to a ITileRenderer (in XNA port the TileRendererXNA).</param>
        /// <param name="pengine">Reference to a prepeared instance of ParticleEngine.</param>
        /// <param name="width">Grid width for this level</param>
        /// <param name="height">Grid height for this level</param>
        public NpcLevel(ITileRenderer renderer, ParticleEngine pengine, int width, int height)
        {
            m_renderer = renderer;
            m_pengine = pengine;
            m_deckVisibility = 0;
            m_destroyingRound = 0;
            m_dragBegan = 0;
            m_hint1 = null;
            m_hint2 = null;
            m_levelScore = 0;
            m_levelProgressChange = 0;
            m_grid = null;
            Difficulty = 1;
            m_gridSize = Point.Zero;
            m_floatingAngle = 0;
            m_state = LevelState.LevelStateIdle;

            m_levelScore = 0;
            m_levelProgressChange = 0;

            m_currentLevel = 0;
            HasMoves = 0;

            m_changing[0] = null;
            m_changing[1] = null;
            SetGameArea(0, 0, 1.0f, 1.0f);
            m_changingCounter = 0;
            m_illegalMoveCounter = 0;

            // Initialize cosine table
            for (int f = 0; f < 4096; f++)
            {
                m_cosineTable[f] = (float)System.Math.Cos((double)f / 4096.0f * System.Math.PI * 2.0);
            }

            // Initialize the grid. 
            ResetGrid(width, height);


            // Create particlesprays     --- XNA NOTE .. sprays are not correct. Check from the Qt-version.
            ScoreSpray = new ParticleSprayType();
            SmokeSpray = new ParticleSprayType();
            SparkleSpray = new ParticleSprayType();
            FruitSpray = new ParticleSprayType();
            MorphSpray = new ParticleSprayType();


            pengine.CreateSprayType(ScoreSpray, (((int)TextureID.TexFont) << 16) + 10, 0, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f / 8.0f, 0.0f, 0.0f, 0.0f, 0.0f, 10.0f, 0.0f, 0.0f, 0);                 // different rendering function than in the others. 
            ScoreSpray.setLevel(this);
            pengine.CreateSprayType(SmokeSpray, (((int)TextureID.TexParticle) << 16), 0, 0, 0, 1.0f / 4.0f, 1.0f / 4, 1.0f / 16, 1.0f / 16, 1.0f / 4.0f, 1.0f / 8.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0);
            pengine.CreateSprayType(SparkleSpray, (((int)TextureID.TexParticle) << 16) + 2, 0, 80000.0f / 65536.0f, 3000.0f / 65536.0f, 0.5f, 80536.0f / 65536.0f, 1.0f / 20.0f, 1.0f / 16.0f, -9000.0f / 65536.0f, 6000 / 65536.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1);
            pengine.CreateSprayType(FruitSpray, (((int)TextureID.TexPieces) << 16), 8, 164000 / 65536.0f, 4000 / 65536.0f, 1.0f, 1.0f, 1.0f / 10, 1.0f / 10, -5500.0f / 65536.0f, 4000 / 65536.0f, 0.0f, 6.0f, -12.0f, 24.0f, 0);
            pengine.CreateSprayType(MorphSpray, (((int)TextureID.TexParticle) << 16) + 3, 0, 0, 2000.0f / 65536.0f, 1.0f / 3.0f, 1.0f / 8.0f, 1.0f / 2.0f, 0, -32000.0f / 65536.0f, 0, 0, 1.0f, 0, 16000.0f / 65536.0f, 1);
        }

        /// <summary>
        /// XNA specific workaround over function-pointers. Originally each particlespray could define it's own rendering function. XNA-version always uses the same OR this method
        /// if specified. This rendering method is used for rendering all of the "particletype" texts in the game.
        /// </summary>
        /// <param name="p">Particle to be rendered</param>
        public void ScoreParticleRenderFunction(Particle p)
        {
            float x = p.X;
            float y = p.Y;
            string testr;

            float size = 2.0f + 1.0f - p.LifeTime / 2;
            size /= 14.0f;
            y -= size / 2;

            // Type 0 indicates a match text.
            if ((p.UserData >> 16) == 0)
            {
                size /= 2;
                int val = (p.UserData & 255);
                int type = ((p.UserData >> 8) & 255);
                switch (type)
                {
                    case 0:
                    default:
                        testr = val + " of a kind";
                        break;

                    case 1:
                        testr = "flush of " + val;
                        break;

                    case 2:
                        testr = "straight of " + val;
                        break;
                }
            }
            else
            {
                // Extra preset texts.
                int j = ((p.UserData >> 16) & 255);

                if (j == 50)
                {
                    size /= 2;
                    testr = "solve bonus";
                }
                else
                {
                    if (j == 51)
                    {
                        size /= 2;
                        testr = "taptap";
                    }
                    else
                        testr = "x" + j;
                }
            }

            x -= testr.Length * size * 3 / 5 / 2;

            // End fade
            int fade = (int)(255 - (p.LifeTime * 256.0f));

            if (fade < 0)
            {
                fade = 0;
            }

            // Start fade
            if (fade == 0)
            {
                fade = (int)(255 - p.LifeTime * 4.0f);
                if (fade < 0) fade = 0;
            }

            // Draw the tests into the screen with rendertileootas
            int length = testr.Length;
            char[] str = testr.ToCharArray();

            for (int i = 0; i < length; i++)
            {
                uint ch = (uint)str[i] - 32;

                uint tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexFont, ch, (uint)fade);

                m_renderer.RenderTile(x, y, size, size, 0, 0, tileIndex, 0);
                x += size * 3 / 5;
            }
        }

        /// <summary>
        /// Reset the NPCGridItem table accorging the new width and height
        /// </summary>
        /// <param name="width">Width of the grid</param>
        /// <param name="height">Height of the grid</param>
        protected void ResetGrid(int width, int height)
        {
            m_grid = null;  // Get rid of the current grid if exists.
            m_hint1 = null;
            m_hint2 = null;
            m_gridSize = new Point(width, height);
            m_grid = new NpcGridItem[m_gridSize.X * m_gridSize.Y];
            m_itemSize = new Vector2(1.0f / (float)m_gridSize.X, 1.0f / (float)m_gridSize.Y);

            float x, y;

            // (re)calculate coordinates
            for (int g = 0; g < m_gridSize.X; g++)
            {
                x = g / (float)m_gridSize.X + 0.5f / m_gridSize.X;
                float yoff = 0.5f / m_gridSize.Y;

                for (int f = 0; f < m_gridSize.Y; f++)
                {
                    m_grid[g + f * m_gridSize.X] = new NpcGridItem();
                    NpcGridItem i = m_grid[g + f * m_gridSize.X];
                    i.GridPos = new Point(g, f);
                    y = ((float)f) / m_gridSize.Y + yoff;
                    i.Flags = 0;
                    i.LPos = new Vector2(x - m_itemSize.X / 2.0f, y - m_itemSize.Y / 2.0f);
                    i.GenCount = 0;
                    i.Destroying = -1;
                    i.Dropping = -1;
                    i.AnimationCount = 0;
                    i.Wobble = 0;
                    i.Wobbleinc = 0;
                }
            }
        }

        /// <summary>
        /// Create a new level and prepeare level for playing it.
        /// </summary>
        /// <param name="levelIndex">Unused, but indicates the current level index.</param>
        public void CreateLevel(int levelIndex)
        {
            m_currentLevel = levelIndex;
            m_hint1 = null;
            m_hint2 = null;
            int tecount = 0;

            // Completely random the level. Then make sure it has moves left and no automatic destroys. 
            // Loop until ok.
            do
            {
                // "plant the seeds"
                for (int f = 0; f < m_gridSize.X * m_gridSize.Y; f++)
                {
                    m_grid[f].Index = RandomBlock();
                    m_grid[f].Flags = 0;
                    m_grid[f].AnimationCount = (float)rand.NextDouble() * 8.0f;
                    m_grid[f].Destroying = -1;
                    m_grid[f].Dropping = -1;
                    m_grid[f].Wobble = 0;
                    m_grid[f].Wobbleinc = 0;
                }

                tecount++;
            } while (CheckDestroyWholeLevel(0)  || !HasMovesLeft());

            HasMoves = 1;
            ZeroMask(GridFlags.GRIDFLAG_ALL);

            m_changing[0] = null;
            m_changing[1] = null;
            m_levelScore = 0;
            m_levelProgressChange = 0;
            CancelSelection(0);
            CancelSelection(1);
            m_destroyingRound = 0;

            HasMovesLeft();         // update hint
            State = LevelState.LevelStateBeginnig;
        }

        /// <summary>
        /// Run everything required in a level
        /// </summary>
        /// <param name="frameTime">How many seconds have passed since last frame</param>
        public void Run(float frameTime)
        {
            // Make the deck of cards appear if levelstate is beginning, and disappear if not.
            if (m_state == LevelState.LevelStateBeginnig)
            {
                m_deckVisibility += (1.0f - m_deckVisibility) * frameTime * 4.0f;
            }
            else
            {
                m_deckVisibility -= (m_deckVisibility) * frameTime * 4.0f;
            }

            if (m_state == LevelState.LevelStateIdle)
            {
                return;
            }

            // Limit the frameTime just in case. If the frametime is too high, level will not function correctly.
            if (frameTime > 0.15f)
            {
                frameTime = 0.15f;
            }

            m_floatingAngle += frameTime;
            float width = 1.0f / m_gridSize.X;
            float height = 1.0f / m_gridSize.Y;

            // Reset some flags to know later what have happened in the level
            DoingNothing = true;
            int something_destroyed = 0;
            int something_dropped = 0;
            int something_destroying = 0;
            int something_dropping = 0;
            int something_created = 0;

            int ind = 0;

            float f, g;

            while (ind < m_gridSize.X * m_gridSize.Y)
            {
                NpcGridItem i = m_grid[ind];

                // Fill the top line of the grid with cards if there are empty space.
                if (ind < m_gridSize.X && i.Index == -1)
                {
                    int safeCount = 0;

                    do
                    {            // try a block as long as required
                        i.Index = RandomBlock();
                        safeCount++;
                    } 
                    while (safeCount < 1000 && CheckDestroyAt(ind, 0, 0));

                    something_created = 1;
                    DoingNothing = false;
                }

                // If this item (i) is destroying, process it.
                if (i.Destroying >= 0)
                {
                    something_destroying = 1;
                    f = i.Destroying;
                    i.Destroying += (frameTime * 4.0f); // destroying speed: 4

                    if (f <= (1.5f) && i.Destroying > (1.5f))
                    {
                        // If about to actually be destroyed (the destroy animationtime have passed), emit some particles.
                        m_pengine.Spray(10, XToGameArea(i.LPos.X + width / 2), YToGameArea(i.LPos.Y + height / 2), 8000.0f / 65536.0f,
                                         0, -10000 / 65536.0f, 0.25f, 0, SparkleSpray);

                        m_pengine.Spray(6 + rand.Next(4), XToGameArea(i.LPos.X + width / 2), YToGameArea(i.LPos.Y + height / 2), 6000.0f / 65536.0f,
                                         0, -40000.0f / 65536.0f, 50000.0f / 65536.0f, 0, FruitSpray);
                    }

                    if (i.Destroying >= 2.0f)
                    {
                        // Actually destroy, finally.
                        if (i == m_changing[0] || i == m_changing[1])
                        {
                            CancelSelection(0);
                            CancelSelection(1);
                            m_changingCounter = 0;

                        }

                        i.Index = -1;
                        i.Destroying = -1;
                        i.Dropping = -1;
                        i.PosOffSetX = 0;
                        i.PosOffSetY = 0;
                        i.Flags = 0;
                        i.AnimationCount = 0;
                        something_destroyed = 1;
                    }
                }

                i.AnimationCount += frameTime;

                // Make animation count circle from 0 to 16
                i.AnimationCount -= (float)(((int)i.AnimationCount) / 16 * 16);

                // Spring-effect for "wobble" attirbute
                g = i.Wobble * frameTime * 16.0f;
                i.Wobbleinc -= g;
                g = i.Wobbleinc * frameTime * 8.0f;
                i.Wobbleinc -= g;
                g = i.Wobbleinc * frameTime * 8.0f;
                i.Wobble += g;

                if (State != LevelState.LevelStateGameOver)
                {
                    if ((i.Flags & GridFlags.GRIDFLAG_SELECTED) != 0)
                    {
                        i.GenCount += (8000 / 65536.0f - i.GenCount) * frameTime * 4.0f;
                    }
                    else
                    {
                        g = m_cosineTable[((int)(m_floatingAngle * 65536.0f / 16.0f) + (ind << 10)) & 4095] / 64.0f;
                        //int g = (m_cosineTable[ ((m_floatingAngle>>4) + (ind<<10))&4095 ]>>6);
                        i.GenCount += (g - i.GenCount) * frameTime * 8.0f;

                        // gameovereffect
                        i.GenCount += (-i.GenCount) * frameTime * 4.0f;
                    }
                }

                ind++;
            }

            if (something_destroyed == 1)
            {
                m_renderer.EffectNotify(TileGameEffect.eEFFECT_DESTROYING, m_destroyingRound, 0);
                m_destroyingRound++;
            }

            int count = 0;

            // Run dropping for each grid item
            while (count < m_gridSize.X * (m_gridSize.Y - 1))
            {
                NpcGridItem i = m_grid[count];

                if (i.Dropping < 0.0f && i.Index != -1)
                {
                    // destroying pieces will drop as well
                    if ((m_grid[count + m_gridSize.X].Index == -1 || m_grid[count + m_gridSize.X].Dropping >= 0.0f))
                    {
                        i.Dropping = 0.001f; // enable dropping.
                        something_dropping = 1;
                        i.PosOffSetY = 0.0f;
                    }
                }
                else
                {
                    something_dropping = 1;
                    i.Dropping += frameTime * 4.0f;
                    i.PosOffSetY += i.Dropping * frameTime;

                    if (i.PosOffSetY >= height)
                    {
                        if (m_grid[count + m_gridSize.X].Index == -1)
                        {
                            i.PosOffSetY -= height;
                            m_grid[count + m_gridSize.X].AnimationCount = i.AnimationCount;
                            m_grid[count + m_gridSize.X].Destroying = i.Destroying;
                            m_grid[count + m_gridSize.X].Flags = i.Flags;
                            m_grid[count + m_gridSize.X].Index = i.Index;
                            m_grid[count + m_gridSize.X].PosOffSetX = i.PosOffSetX;
                            m_grid[count + m_gridSize.X].PosOffSetY = i.PosOffSetY;
                            m_grid[count + m_gridSize.X].Dropping = i.Dropping;
                            m_grid[count + m_gridSize.X].Wobble = i.Wobble;
                            m_grid[count + m_gridSize.X].Wobbleinc = i.Wobbleinc;
                            i.Index = -1;
                            i.Dropping = -1;
                            i.Wobble = 0;
                            i.Wobbleinc = 0;
                        }
                    }

                    if (m_grid[count + m_gridSize.X].Index != -1 && m_grid[count + m_gridSize.X].Dropping < 0.0f)
                    {
                        // Stop dropping...
                        f = count / m_gridSize.X;
                        CheckDestroyAt(count - (((int)f) * m_gridSize.X), (int)f, 1);
                        something_dropped = 1;
                        i.Dropping = -1;
                        i.Wobbleinc = 4000.0f / 65536.0f;
                        i.PosOffSetY = 0;
                    }

                }

                count++;
            }

            // Loop one more time only the last row. Disable dropping for those.
            for (int loop = m_gridSize.X * (m_gridSize.Y - 1); loop < m_gridSize.X * m_gridSize.Y; loop++)
            {
                NpcGridItem i = m_grid[loop];

                if (i.Dropping >= 0.0f)
                {
                    i.Dropping = -1.0f;
                    i.PosOffSetY = 0;
                }
            }

            // Raise flag if something have happened. 
            if (something_destroyed != 0 || something_dropped != 0 ||
                something_destroying != 0 || something_dropping != 0)
            {
                DoingNothing = false;
            }

            if (DoingNothing == true && (m_hint1 == null || m_hint2 == null))
            {
                HasMovesLeft();
            }

            switch (m_state)
            {
                // Run the "beginning" animation. Make the cards arrive to the table.
                case LevelState.LevelStateBeginnig:
                    {
                        m_startupCounter -= frameTime;

                        if (m_startupCounter <= 0)
                        {
                            m_startupCounter = 0;
                            State = LevelState.LevelStateNormal;
                        }

                        int amount = m_gridSize.X * m_gridSize.Y;

                        for (int loop = 0; loop < m_gridSize.X * m_gridSize.Y; loop++)
                        {
                            NpcGridItem i = m_grid[loop];
                            g = m_startupCounter - (loop * 4.0f / amount);

                            if (g < 0)
                            {
                                g = 0;

                                if ((i.Flags & GridFlags.GRIDFLAG_CALCULATION_TEMP) == 0)
                                {
                                    i.Flags |= GridFlags.GRIDFLAG_CALCULATION_TEMP;
                                    // Do something when block is in place
                                    m_renderer.EffectNotify(TileGameEffect.eEFFECT_BLOCK_BEGIN_FINISHED, 0, 0);
                                }
                            }
                            else
                                i.Wobble = -1000 / 65536.0f;

                            g *= 4.0f;
                            if (g > 1.0f) g = 1.0f;

                            i.PosOffSetX = DECK_POSITION.X - i.LPos.X - m_itemSize.X / 2;
                            i.PosOffSetY = DECK_POSITION.Y - i.LPos.Y - m_itemSize.Y;
                            i.PosOffSetX = (i.PosOffSetX * g);
                            i.PosOffSetY = (i.PosOffSetY * g);
                            i.GenCount = g * 1.25f;
                        }
                    }
                    break;

                // Remove cards from the table
                case LevelState.LevelStateLevelCompleted:
                    {
                        m_startupCounter -= frameTime;

                        if (m_startupCounter <= 0)
                        {
                            m_startupCounter = 0;
                            State = LevelState.LevelStateIdle;
                        }

                        int amount = m_gridSize.X * m_gridSize.Y;

                        for (int loop = 0; loop < m_gridSize.X * m_gridSize.Y; loop++)
                        {
                            NpcGridItem i = m_grid[loop];
                            g = (loop * 4.0f / amount) - m_startupCounter;
                            g *= 4.0f;

                            if (g > 1.0f)
                                g = 1.0f;

                            if (g <= 0)
                                g = 0;
                            else
                            {
                                if ((i.Flags & GridFlags.GRIDFLAG_CALCULATION_TEMP) == 0)
                                {
                                    i.Flags |= GridFlags.GRIDFLAG_CALCULATION_TEMP;
                                    // Do something when block is taken from its place
                                    m_renderer.EffectNotify(TileGameEffect.eEFFECT_BLOCK_VANISH_STARTED, 0, 0);
                                }
                            }

                            i.PosOffSetX = (1.0f - i.LPos.X + m_itemSize.X / 2) * g;
                            i.PosOffSetY = (1.0f - i.LPos.Y + m_itemSize.Y) * g;
                            i.GenCount = g * 1.25f;
                        }
                        break;
                    }

                // Remove cards from the desk
                case LevelState.LevelStateGameOver:
                    {
                        float jj = (m_gridSize.X * m_gridSize.Y) - m_startupCounter / 2;
                        if (jj < 1.0f) jj = 1.0f;
                        jj *= frameTime;
                        m_startupCounter -= jj;

                        if (m_startupCounter <= -1.0f * 32)
                        {
                            m_startupCounter = -1.0f * 32;
                            State = LevelState.LevelStateIdle;
                            // Wait and do nothing... until the game changes levelstate to something else.
                        }

                        for (int loop = 0; loop < m_gridSize.X * m_gridSize.Y; loop++)
                        {
                            NpcGridItem i = m_grid[loop];
                            g = loop - m_startupCounter;
                            if (g < 0) g = 0;
                            if (g > 1.0f * 28) g = 1.0f * 28;
                            i.GenCount -= g / 32.0f;
                        }

                        break;
                    }


                case LevelState.LevelStateNormal:
                    {
                        if (something_created != 0)
                            CheckDestroyWholeLevel(1);

                        // Check if the level have moves left and update the hint
                        if (something_destroyed != 0 || something_dropped != 0 || something_created != 0)
                        {
                            m_hint1 = null;
                            m_hint2 = null;

                            if (something_destroying == 0 && something_dropping == 0)
                            {
                                if (HasMovesLeft())
                                {            // update hint

                                }
                                else
                                {            // NO MOVES LEFT!!
                                    // Give scores for player for getting the game into a situation like this since we cannot make sure it doesn't happend.
                                    m_pengine.Spray(1, XToGameArea(0.5f), YToGameArea(0.25f),
                                                     0, 0, 0.122f, 0, 50, ScoreSpray);

                                    m_levelScore += 800;
                                    m_levelProgressChange += 40;

                                    // destroy some pieces randomly
                                    int dcount = m_gridSize.Y;
                                    int safeCount = 0;

                                    while (dcount > 0 && safeCount < 2000)
                                    {
                                        NpcGridItem i = GetGridItemAt(rand.Next(m_gridSize.X), rand.Next(m_gridSize.Y));

                                        if (i.Index != -1 && ((i.Flags & GridFlags.GRIDFLAG_MARKED) == 0) && i != m_changing[0] && i != m_changing[1])
                                        {
                                            i.Flags |= GridFlags.GRIDFLAG_MARKED;
                                            dcount--;
                                        }
                                        safeCount++;
                                    }

                                    ApplyDestroy((uint)GridFlags.GRIDFLAG_MARKED);
                                }
                            }
                        }

                        // Combo: Several hands have been destroyed continuously without user's manipulation.
                        if (m_destroyingRound > 3 && DoingNothing)
                        {
                            m_pengine.Spray(1, XToGameArea(0.5f), YToGameArea(0.5f),
                                             0, 0, 8000 / 65536.0f, 0, ((m_destroyingRound - 1) << 16), ScoreSpray);
                            m_renderer.EffectNotify(TileGameEffect.eEFFECT_XBONUS, 0, 0);
                            m_levelProgressChange += m_destroyingRound * 3;
                            m_levelScore += (m_destroyingRound * 30);
                            m_destroyingRound = 0;
                        }

                        // Two cards (grid items) are changing their places, make it happend.
                        if (m_changingCounter > 0)
                        {
                            SwapCards(frameTime);
                        }
                        break;		// dont continue.
                    }
            }
        }

        /// <summary>
        /// Swap cards.
        /// </summary>
        private void SwapCards(float frameTime)
        {
            m_changingCounter -= frameTime * 6.0f;			// 6 = changing speed. 1/6 sec's for now.

            // Do the change with m_changing[0] and [1]
            if (m_changingCounter <= 0)
            {
                m_hint1 = null;
                m_hint2 = null;
                int temp = m_changing[0].Index;
                m_changing[0].Index = m_changing[1].Index;
                m_changing[1].Index = temp;

                // Reset the destroying rounds.
                m_destroyingRound = 0;

                // Check if the move is allowed.
                int allow_move = 0;

                // check destroy.
                if (CheckDestroyAt(m_changing[0].GridPos.X, m_changing[0].GridPos.Y, 1) ||
                    CheckDestroyAt(m_changing[1].GridPos.X, m_changing[1].GridPos.Y, 1))
                {
                    allow_move = 1;
                }

                // Move wasn't allowed (didn't cause any hands). MatchemPoker allows the move anyway, but subtracts some time.
                if (allow_move == 0)
                {
                    allow_move = 1;         // subtract some scores
                    m_levelProgressChange -= 15;
                }

                if (allow_move != 0)
                {
                    m_renderer.EffectNotify(TileGameEffect.eCHANGE_COMPLETED, 0, 0);
                    m_changingCounter = 0;
                }
                else
                {
                    // ILLEGAL MOVE... Cancel it
                    if (m_illegalMoveCounter == 0)
                    {
                        m_changingCounter = 1.0f;
                        m_illegalMoveCounter++;
                        m_renderer.EffectNotify(TileGameEffect.eILLEGAL_MOVE, 0, 0);
                    }
                    else
                    {
                        // this was a "backwards" move.
                        m_changingCounter = 0;			// stay still.
                    }
                }

                if (m_changingCounter == 0)
                {
                    m_changing[0].Flags &= GridFlags.GRIDFLAG_ALL_BUT_SELECTED;
                    m_changing[1].Flags &= GridFlags.GRIDFLAG_ALL_BUT_SELECTED;
                    m_changing[0].PosOffSetX = 0;
                    m_changing[0].PosOffSetY = 0;
                    m_changing[1].PosOffSetX = 0;
                    m_changing[1].PosOffSetY = 0;
                    m_changing[0] = null;
                    m_changing[1] = null;
                }
            }
            else
            {
                float dx, dy;

                if (m_changing[0] != null && m_changing[1] != null)
                {

                    float i = 1.0f - m_changingCounter;
                    dx = (m_changing[1].LPos.X - m_changing[0].LPos.X) * i;
                    dy = (m_changing[1].LPos.Y - m_changing[0].LPos.Y) * i;

                    float ndx = dy * m_cosineTable[((int)(i * 1024.0f)) & 4095];
                    float ndy = -dx * m_cosineTable[((int)(i * 1024.0f)) & 4095];

                    m_changing[0].PosOffSetX = dx + ndx * 3 / 2;
                    m_changing[0].PosOffSetY = dy + ndy;
                    m_changing[1].PosOffSetX = -dx - ndx * 3 / 2;
                    m_changing[1].PosOffSetY = -dy - ndy;
                }
                else
                {
                    CancelSelection(0);
                    CancelSelection(1);
                    m_changingCounter = 0;
                }
            }
        }

        /// <summary>
        /// Draw the level.
        /// </summary>
        /// <param name="renderer">Referfence to ITileRenderer which is used for all of the rendering.</param>
        public void Draw(ITileRenderer renderer)
        {
            if (m_state == LevelState.LevelStateIdle) return;

            // Loop though all of the items in a grid and call rendering for them (1st time)
            for (int loop = 0; loop < m_gridSize.X * m_gridSize.Y; loop++)
            {
                NpcGridItem i = m_grid[loop];

                if (i.Index != -1 && //i->index!=8 &&
                    ((i.Flags & GridFlags.GRIDFLAG_HIDDEN) == 0) &&
                    ((i.Flags & GridFlags.GRIDFLAG_SELECTED) == 0) &&
                    ((i.Flags & GridFlags.GRIDFLAG_MARKED) == 0) &&
                    (i.GenCount < 1000))
                {
                    RenderTileCaller(i, renderer);
                }
            }

            // Loop though all of the items in a grid and call rendering for them (2nd time): This is done only for selected cards (which are topmost).
            for (int loop = 0; loop < m_gridSize.X * m_gridSize.Y; loop++)
            {
                NpcGridItem i = m_grid[loop];

                if (i.Index != -1 &&
                  ((i.Flags & GridFlags.GRIDFLAG_HIDDEN) == 0))
                {
                    GridFlags te = GridFlags.GRIDFLAG_SELECTED | GridFlags.GRIDFLAG_MARKED;
                    if (((i.Flags & te) != 0) || i.GenCount >= 1000)
                    {
                        RenderTileCaller(i, renderer);
                    }
                }
            }

            // Draw the deck of cards if it should be visible.
            if (m_deckVisibility > 0.01f)
            {
                // deck draw
                for (int f = 0; f < 3; f++)
                {
                    float ysize = m_itemSize.Y * 3;
                    float xsize = m_itemSize.X * 3;
                    float amount = 0.02f;
                    int a = (int)((m_startupCounter * 4024.0f) + (f * 650));

                    uint tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexPieces, 0, ((uint)(((1.0f - (m_deckVisibility)) * 255.0f))));

                    renderer.RenderTile(DECK_POSITION.X - xsize / 2 + (m_cosineTable[a & 4095]) * (m_cosineTable[(a * 2 + 1500) & 4095]) * amount,
                                        DECK_POSITION.Y - ysize / 2 + (m_cosineTable[(a + 1200) & 4095]) * amount,
                                        xsize, ysize, 0, 0, tileIndex, 0);
                }
            }
        }

        /// <summary>
        /// Pointer (or mouse) event arrives to level
        /// </summary>
        /// <param name="x">X Position of a pointer</param>
        /// <param name="y">Y Position of a pointer</param>
        /// <param name="type">State of a pointer</param>
        public void Click(float x, float y, PointerState type)
        {
            if (m_state == LevelState.LevelStateBeginnig ||
                m_state == LevelState.LevelStateLevelCompleted ||
                m_state == LevelState.LevelStateGameOver)
            {
                return;
            }

            // We are currently changing two cards: ignore the event for now.
            if (m_changingCounter > 0)
            {
                return;
            }

            // Scale coordinate into the gamearea.
            x = (x - m_areaX) / (float)m_areaWidth;
            y = (y - m_areaY) / (float)m_areaHeight;

            if (x < 0 || y < 0 || x > 1 || y > 1)
            {
                return;	// not inside gamelevel
            }

            float gx = (int)(x * (float)m_gridSize.X);
            float gy = (int)(y * (float)m_gridSize.Y);

            if (gy < 0)
            {
                return;
            }

            // Get a grid item under the pointer's position.
            NpcGridItem i = GetGridItemAt((int)gx, (int)gy);

            switch (type)
            {
                // Pointer move
                case PointerState.PointerDrag:
                    {
                        // No move events until something is selected.
                        if (m_changing[0] != null)
                        {
                            // Difficulty always is over zero in MatchEmPoker
                            if (Difficulty > 0)
                            {
                                float fx = (x - m_changing[0].LPos.X) - (1.0f / (float)m_gridSize.X / 2);
                                float fy = (y - m_changing[0].LPos.Y) - (1.0f / (float)m_gridSize.Y / 2);

                                // limit the position offset to 6000/65536
                                float le = (float)System.Math.Sqrt(fx * fx + fy * fy);

                                if (le > 6000.0f / 65536.0f)
                                {
                                    fx = fx / le * 6000.0f / 65536.0f;
                                    fy = fy / le * 6000.0f / 65536.0f;
                                }

                                m_changing[0].PosOffSetX = fx;
                                m_changing[0].PosOffSetY = fy;

                                gx = (m_changing[0].LPos.X + m_changing[0].PosOffSetX + (1.0f / (float)m_gridSize.X / 2));
                                gy = (m_changing[0].LPos.Y + m_changing[0].PosOffSetY + (1.0f / (float)m_gridSize.Y / 2));

                                i = GetGridItemAt((int)(gx * m_gridSize.X), (int)(gy * m_gridSize.Y));
                            }
                            else
                            {
                                m_changing[0].PosOffSetX = (x - m_changing[0].LPos.X) - (1.0f / (float)m_gridSize.X / 2);
                                m_changing[0].PosOffSetY = (y - m_changing[0].LPos.Y) - (1.0f / (float)m_gridSize.Y / 2);
                            }

                            if (i != m_changing[0])
                            {
                                if (m_changing[1] != i)
                                {
                                    m_changing[1] = i;
                                }
                                m_dragBegan = 1;

                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    break;

                // Button up
                case PointerState.PointerUp:
                    {
                        if (Difficulty > 0 && m_dragBegan != 0 && m_changing[1] != null)
                            i = m_changing[1];

                        if (m_changing[0] != null && m_changing[0] != i && i.Destroying == -1 && i.Dropping == -1)
                        {
                            TryApplyChange(i);
                        }
                        else
                        {
                            if (m_dragBegan != 0)
                            {
                                CancelSelection(0);
                                CancelSelection(1);
                            }
                        }

                        m_dragBegan = 0;
                    }

                    break;

                // Button down
                case PointerState.PointerDown:
                    {
                        m_dragBegan = 0;
                        if (i == null)
                            return;

                        if (i == m_changing[0])
                        {			// was already selected and now we are cancelling it.
                            m_renderer.EffectNotify(TileGameEffect.eCLICK, 0, 0);
                            CancelSelection(0);
                        }
                        else
                        {
                            if (i.Destroying == -1 && i.Dropping == -1)
                            {
                                if (m_changing[0] == null)
                                {
                                    // click empty card..
                                    if (i.Index == EMPTY_CARD)
                                    {
                                        m_renderer.EffectNotify(TileGameEffect.eCHANGE_COMPLETED, 0, 0);
                                        while (i.Index == EMPTY_CARD) i.Index = RandomBlock();
                                        m_levelProgressChange -= 20;

                                        m_pengine.Spray(1,
                                                         XToGameArea(i.LPos.X + m_itemSize.X / 2),
                                                         YToGameArea(i.LPos.Y + m_itemSize.Y / 2),
                                                         0.001f, 0, 0, 0, 0, MorphSpray);

                                        m_pengine.Spray(20,
                                                         XToGameArea(i.LPos.X + m_itemSize.X / 2),
                                                         YToGameArea(i.LPos.Y + m_itemSize.Y / 2),
                                                         8000 / 65536.0f, 0, -0.25f, 1.0f, 0, SparkleSpray);

                                        CheckDestroyAt(i.GridPos.X, i.GridPos.Y, 1);
                                    }
                                    else
                                    {
                                        m_renderer.EffectNotify(TileGameEffect.eCLICK, 0, 0);
                                        m_changing[0] = i;
                                        i.Flags ^= GridFlags.GRIDFLAG_SELECTED;			// flip the first bit
                                    }
                                }
                                else
                                {
                                    TryApplyChange(i);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Set the area for the level
        /// </summary>
        /// <param name="x">x begin for the area</param>
        /// <param name="y">y begin for the area</param>
        /// <param name="width">width of the area</param>
        /// <param name="height">height of the area</param>
        public void SetGameArea(float x, float y, float width, float height)
        {
            m_areaX = x;
            m_areaY = y;
            m_areaWidth = width;
            m_areaHeight = height;
        }

        /// <summary>
        /// Returns the current "levelScore" currently collected and sets the member to zero.
        /// </summary>
        /// <returns>Token level score</returns>
        public int TakeLevelScore()
        {
            int rval = m_levelScore;
            m_levelScore = 0;
            return rval;
        }

        /// <summary>
        /// Returns the current level progress and sets the member to zero.
        /// </summary>
        /// <returns>Current level progress</returns>
        public int TakeLevelProgressChange()
        {
            int rval = m_levelProgressChange;
            m_levelProgressChange = 0;
            return rval;
        }

        /// <summary>
        /// Converts X coordinate into the level's base
        /// </summary>
        /// <param name="sourcex">Source X</param>
        /// <returns>Level X</returns>
        private float XToGameArea(float sourcex)
        {
            return m_areaX + sourcex * m_areaWidth;
        }

        /// <summary>
        /// Converts Y coordinate into the level's base
        /// </summary>
        /// <param name="sourcey">Source Y</param>
        /// <returns>Level Y</returns>
        private float YToGameArea(float sourcey)
        {
            return m_areaY + sourcey * m_areaHeight;
        }

        /// <summary>
        /// Serialize the level data into a file
        /// </summary>
        /// <param name="bw">Reference to a binarywriter opened for the targetfile</param>
        public void SaveToFile(BinaryWriter bw)
        {
            for (int i = 0; i < m_gridSize.X * m_gridSize.Y; i++)
            {
                bw.Write(m_grid[i].Index);
            }
        }

        /// <summary>
        /// Load the leveldata from a file.
        /// </summary>
        /// <param name="br">Reference to a binaryreader opened for the source</param>
        public void LoadFromFile(BinaryReader br)
        {
            // just create something that the flags etc. are ok
            CreateLevel(0);

            for (int i = 0; i < m_gridSize.X * m_gridSize.Y; i++)
            {
                m_grid[i].Index = br.ReadInt32();
                m_grid[i].PosOffSetX = 0;
                m_grid[i].PosOffSetY = 0;
            }

            CheckDestroyWholeLevel(1);

            // Update hint
            HasMovesLeft();
            m_destroyingRound = 0;
        }

        /// <summary>
        /// Indicate that level should display a hint for next move
        /// </summary>
        public void WobbleHint()
        {
            if (m_hint1 == null || m_hint2 == null) return;		// error
            m_hint1.Wobbleinc = -1.0f / 16;
            m_hint2.Wobbleinc = -1.0f / 15;

            if (m_currentLevel == 0)
            {			// switch us
                float x = XToGameArea((1.0f / (float)m_gridSize.X + m_hint1.LPos.X + m_hint2.LPos.X) / 2);
                float y = YToGameArea((1.0f / (float)m_gridSize.Y + m_hint1.LPos.Y + m_hint2.LPos.Y) / 2);
                float dx = XToGameArea(0.5f) - x;
                float dy = YToGameArea(0.5f) - y;
                m_pengine.Spray(1, x, y, 0, dx / 4, dy / 4, 0, 51 << 16, ScoreSpray);
            }
        }

        /// <summary>
        /// Reset a selection of specified index
        /// </summary>
        /// <param name="index">0 or 1: Which selection should be cancelled.</param>
        protected void CancelSelection(int index)
        {
            if (m_changing[index] != null)
            {
                m_changing[index].PosOffSetX = 0;
                m_changing[index].PosOffSetY = 0;
                m_changing[index].Flags &= GridFlags.GRIDFLAG_ALL_BUT_SELECTED;
                m_changing[index] = null;
            }
        }

        /// <summary>
        /// Get a random block index of the game space (New random card)
        /// </summary>
        /// <returns>The index of new card.</returns>
        protected int RandomBlock()
        {
            int amount_of_numbers = 7;
            int rval = rand.Next(4) * 13 + (13 - amount_of_numbers) + rand.Next(amount_of_numbers);

            int empty_prob = -2 + m_currentLevel * 3;

            if (empty_prob > 38)
                empty_prob = 48;

            if ((rand.Next(256)) < empty_prob)
                rval = EMPTY_CARD;

            return rval;
        }


        /// <summary>
        /// Try changing m_changing[0] with i
        /// </summary>
        /// <param name="i">The second grid item to be changed</param>
        /// <returns>true if succeeded, false if not.</returns>
        protected bool TryApplyChange(NpcGridItem i)
        {
            // cannot select another peace when other than in normal mode
            if (m_state != LevelState.LevelStateNormal)
            {
                m_renderer.EffectNotify(TileGameEffect.eILLEGAL_MOVE, 0, 0);
                CancelSelection(0);
                CancelSelection(1);
                return false;						// cannot continue, level is processing itself.
            }

            bool allowChange = false;

            if (Difficulty == 0)
            {
                allowChange = true;			// for kids.
            }

            if (allowChange == false)
            {
                // must be next  to changing[0] to be allowed
                if ((System.Math.Abs(m_changing[0].GridPos.X - i.GridPos.X) +
                     System.Math.Abs(m_changing[0].GridPos.Y - i.GridPos.Y)) <= 1)
                {
                    allowChange = true;
                }
            }

            if (allowChange == true)
            {
                m_renderer.EffectNotify(TileGameEffect.eCHANGING, 0, 0);
                m_changing[1] = i;
                m_changingCounter = 1.0f;
                m_illegalMoveCounter = 0;
                // Changing is now enabled
                i.Flags ^= GridFlags.GRIDFLAG_SELECTED;			// flip the selected bit
            }
            else
            {
                m_renderer.EffectNotify(TileGameEffect.eILLEGAL_MOVE, 0, 0);
                CancelSelection(0);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Does the level has any moves achievable with a single move.
        /// </summary>
        /// <returns>nonzero if there are moves, zero if not</returns>
        protected bool HasMovesLeft()
        {
            m_hint1 = null;
            m_hint2 = null;

            for (int y = m_gridSize.Y - 1; y > 1; y--)
            {
                for (int x = 0; x < m_gridSize.X; x++)
                {
                    NpcGridItem i1 = m_grid[m_gridSize.X * y + x];
                    // simulate the moves..
                    if (TryMoveWith(i1, GetGridItemAt(x, y + 1)))
                    {
                        m_hint1 = i1;
                        m_hint2 = GetGridItemAt(x, y + 1);
                        return true;
                    }

                    if (TryMoveWith(i1, GetGridItemAt(x + 1, y)))
                    {
                        m_hint1 = i1;
                        m_hint2 = GetGridItemAt(x + 1, y);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Try to change two items with eachother and see if that would cause a hand.
        /// </summary>
        /// <param name="i1">First item to try to</param>
        /// <param name="i2">Second item to try to</param>
        /// <returns>true if move would cause a hand, false if it wouldn't</returns>
        protected bool TryMoveWith(NpcGridItem i1, NpcGridItem i2)
        {
            if (i1 == null || i2 == null)
            {
                return false;
            }

            // flip indexes to see if the move would cause destroying.
            int ctemp = i1.Index;
            i1.Index = i2.Index;
            i2.Index = ctemp;

            bool rval = false;

            if (CheckDestroyAt(i1.GridPos.X, i1.GridPos.Y, 0) ||
                CheckDestroyAt(i2.GridPos.X, i2.GridPos.Y, 0))
            {
                rval = true;
            }

            // flip indexes back.
            ctemp = i1.Index;
            i1.Index = i2.Index;
            i2.Index = ctemp;
            return rval;
        }


        /// <summary>
        /// Actually render a single NPCGridItem with ITileRenderer's renderTile-method
        /// </summary>
        /// <param name="i">Item to be rendered</param>
        /// <param name="renderer">Renderer to use</param>
        protected void RenderTileCaller(NpcGridItem i, ITileRenderer renderer)
        {
            // change size from i's gencount
            float sizeAdd = TILE_SIZE_ADD + (i.GenCount / 4.0f);
            int fade = 0;

            float width = m_itemSize.X;
            float height = m_itemSize.Y;

            TextureID tex = TextureID.TexPieces;
            int earth_index = i.Index / 13;
            int num_index = i.Index - earth_index * 13;
            num_index++;

            if (num_index > 12)
            {
                num_index = 0;
            }

            // Joker card is disabled for now.
            if (i.Index == JOKER_CARD)
            {
                earth_index = rand.Next(4);
                num_index = 6 + rand.Next(8);
            }

            if (i.Destroying != -1)
            {
                tex = TextureID.TexPiecesSelected;
            }

            if ((i.Flags & GridFlags.GRIDFLAG_MARKED) != 0)
            {
                tex = TextureID.TexPiecesSelected;
            }

            if ((i.Flags & GridFlags.GRIDFLAG_SELECTED) != 0)
            {
                tex = TextureID.TexPiecesSelected;
            }

            float wadd = (sizeAdd * 5) / 8.0f + m_itemSize.X / 2;
            float hadd = (sizeAdd / 4.0f);
            float rx = i.LPos.X + i.PosOffSetX - (wadd / 2.0f);
            float ry = i.LPos.Y + i.PosOffSetY - (hadd / 2.0f);

            ry += i.Wobble;
            width += wadd;
            height += hadd;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (m_state == LevelState.LevelStateGameOver)
            {
                fade = -(int)(i.GenCount / 200.0f);
            }

            if (fade < 0)
            {
                fade = 0;
            }
            else if (fade > 255)
            {
                fade = 255;
            }

            float rx2 = XToGameArea(rx + width);
            float ry2 = YToGameArea(ry + height);
            rx = XToGameArea(rx);
            ry = YToGameArea(ry);

            width = rx2 - rx;
            height = ry2 - ry;

            if (i.Destroying >= 0.0f)
            {
                fade = (int)((i.Destroying / 2.0f) - 255.0f * 4 / 5);
                if (fade < 0) fade = 0; else fade *= 10;
            }

            if (fade < 255)
            {
                uint tileIndex;

                if (i.Index != EMPTY_CARD)
                {             
                    tileIndex = ITileRenderer.BuildTileIndex(tex, 1, (uint)fade);
                    // Render a background for the card.
                    renderer.RenderTile(rx, ry, width, height, 0, 0, tileIndex, 0);

                    tileIndex = ITileRenderer.BuildTileIndex(tex, (uint)(earth_index + 2), (uint)fade);
                    // Render color (or earth)
                    renderer.RenderTile(
                                rx + (width / 4.0f),
                                ry + (height / 16.0f),
                                (width * 4) / 8.0f,
                                height / 2.0f,
                                0, 0,
                                tileIndex, 0);

                    if (num_index == 13)
                        num_index = 0;

                    tileIndex = ITileRenderer.BuildTileIndex(tex, ((uint)num_index + 6), (uint)fade);

                    // Render Number
                    renderer.RenderTile(
                                rx + (width / 4.0f),
                                ry + (height / 2.0f),
                                (width * 4) / 8.0f,
                                (height / 2.0f),
                                0, 0,
                                tileIndex, 0);
                }
                else
                {
                    tileIndex = ITileRenderer.BuildTileIndex(tex, 0, (uint)fade);
                    // Render as empty card
                    renderer.RenderTile(rx, ry, width, height, 0, 0, tileIndex, 0);
                }
            }

            // draw flare on top of the card when it is destroying.
            if (i.Destroying >= 0.0f)
            {
                fade = 255 - (int)(i.Destroying * 128.0f);

                if (fade < 0)
                    fade = 0;

                float size = 0.001f + ((i.Destroying) * (i.Destroying) * 0.15f);

                if (fade < 255)
                {
                    uint tileIndex = ITileRenderer.BuildTileIndex(TextureID.TexParticle, 2, (uint)fade);
                    renderer.RenderTile(
                                rx - size,
                                ry - size,
                                width + size * 2,
                                height + size * 2,
                                i.AnimationCount * 2.0f, 1,
                                tileIndex, 0);
                }
            }
        }

        /// <summary>
        /// Start destroying for items which are flagged with a selected flag
        /// </summary>
        /// <param name="flag">a Flag which should be destroyed</param>
        protected void ApplyDestroy(uint flag)
        {
            bool wasFirst = false;
            float width = m_itemSize.X;
            float height = m_itemSize.Y;

            DoingNothing = false;

            int blocks = 0;
            float mx = 0;
            float my = 0;

            for (int loop = 0; loop < m_gridSize.X * m_gridSize.Y; loop++)
            {
                NpcGridItem i = m_grid[loop];

                // Just destoy the ones that are not already destroying
                if (i.Index != -1 && (((uint)i.Flags & flag) != 0) && (i.Destroying < 0.0f))
                {
                    // At least one of the line wasn't marked before, .. this was first found. mark for scores
                    if ((i.Flags & GridFlags.GRIDFLAG_MARKED) == 0)
                        wasFirst = true;

                    mx += i.LPos.X;
                    my += i.LPos.Y;
                    blocks++;
                    i.Flags |= GridFlags.GRIDFLAG_MARKED;
                    i.Destroying = 0.0001f;				// start destroying.
                }
            }

            // Calculate and gives scores for this destroy
            if (blocks < 1)
            {
                return;
            }

            if (wasFirst)
            {
                int scoreAdd = (blocks * blocks * 2) / (m_destroyingRound + 1);

                int sind = 0;
                int scoreMul = 1;

                switch (flag)
                {
                    case (int)GridFlags.GRIDFLAG_MINUS_STRAIGHT:
                    case (int)GridFlags.GRIDFLAG_PLUS_STRAIGHT:
                        {

                            sind = 2;
                            scoreMul = 11;
                        }
                        break;

                    case (int)GridFlags.GRIDFLAG_SAME_EARTH:
                        {
                            sind = 1;
                            scoreMul = 3;
                        }
                        break;

                    case (int)GridFlags.GRIDFLAG_SAME_NUMBER:
                        {
                            sind = 0;
                            scoreMul = 7;
                        }
                        break;
                }

                if (scoreAdd < 1)
                {
                    scoreAdd = 1;
                }

                scoreAdd = (scoreAdd * scoreMul) / 3;           // most scores from straights, then from earth, then from number

                m_levelProgressChange += (scoreAdd);//  / (m_destroyingRound+1));
                float x = XToGameArea(mx / blocks + width / 2);
                float y = YToGameArea(my / blocks + height / 2);
                float dx = XToGameArea(0.5f) - x;
                float dy = YToGameArea(0.5f) - y;
                m_pengine.Spray(1, x, y, 0, dx, dy, 0, sind * 256 + blocks, ScoreSpray);

                m_levelScore += scoreAdd;
            }
        }


        /// <summary>
        /// Recursive function for calculating hands
        /// </summary>
        /// <param name="index">Index of our current card to be scanned</param>
        /// <param name="x">Grid X Position we are processing</param>
        /// <param name="y">Grid Y Position we are processing</param>
        /// <param name="checkmode">Which hand type we are checking, same number, -earth or straights.</param>
        /// <param name="dir">Direction of this check: Left, down, right, up.</param>
        /// <returns>How many similar was found according these settings.</returns>
        protected int CalculateSimilar(int index, int x, int y, int checkmode, int dir)
        {
            switch (dir)
            {
                case 0: 
                    y--; 
                    break;
                case 1:
                    y++; 
                    break;
                case 2: 
                    x--; 
                    break;
                case 3: 
                    x++; 
                    break;
            };

            NpcGridItem i = GetGridItemAt(x, y);

            if (i == null) 
                return 0;
            if (i.Index == EMPTY_CARD) 
                return 0;
            if (i.Index == -1) 
                return 0;
            if (i.Dropping != -1) 
                return 0;
            if (i.Destroying > 0) 
                return 0;		// 0 is correct,... NOT -1

            int earth_index = i.Index / 13;
            int number_index = i.Index - earth_index * 13;

            switch (checkmode)
            {
                default:
                case 0:					// similar earth
                    {
                        if ((i.Flags & GridFlags.GRIDFLAG_SAME_EARTH) != 0) 
                            return 0;

                        if ((earth_index != index / 13)) 
                            return 0;

                        i.Flags |= GridFlags.GRIDFLAG_SAME_EARTH;
                    }
                    break;

                case 1:					// similar number
                    {
                        if ((i.Flags & GridFlags.GRIDFLAG_SAME_NUMBER) != 0) 
                            return 0;
                        if (number_index != (index - index / 13 * 13)) 
                            return 0;

                        i.Flags |= GridFlags.GRIDFLAG_SAME_NUMBER;
                    }
                    break;

                case 2:					// "normal" minus straight
                    {
                        if ((i.Flags & GridFlags.GRIDFLAG_MINUS_STRAIGHT) != 0) 
                            return 0;

                        if ((dir & 1) == 0)
                        {		// backwards
                            if (number_index != (index - index / 13 * 13) - 1) 
                                return 0;
                        }
                        else
                        {
                            if (number_index != (index - index / 13 * 13) + 1) 
                                return 0;
                        }

                        i.Flags |= GridFlags.GRIDFLAG_MINUS_STRAIGHT;
                    }
                    break;

                case 3:					// "reverse" plus straight
                    {
                        if ((i.Flags & GridFlags.GRIDFLAG_PLUS_STRAIGHT) != 0) 
                            return 0;

                        if ((dir & 1) == 0)
                        {		// backwards
                            if (number_index != (index - index / 13 * 13) + 1) 
                                return 0;
                        }
                        else
                        {
                            if (number_index != (index - index / 13 * 13) - 1) 
                                return 0;
                        }

                        i.Flags |= GridFlags.GRIDFLAG_PLUS_STRAIGHT;
                    }
                    break;
            };

            return 1 + CalculateSimilar(i.Index, x, y, checkmode, dir);
        }

        /// <summary>
        /// Check if something should be destroyed at specified location
        /// </summary>
        /// <param name="blockx">Grid X position for check</param>
        /// <param name="blocky">Grid Y position for check</param>
        /// <param name="apply">Should the destroy be applied or only checked (0=do not apply).</param>
        /// <returns>True if something wouldbe / is destroyed, false otherwise.</returns>
        public bool CheckDestroyAt(int blockx, int blocky, int apply)
        {
            NpcGridItem i = GetGridItemAt(blockx, blocky);
            bool rval = false;
            int sim_earth;
            int sim_number;
            int minus_straight;
            int plus_straight;

            if (i.Index != -1 && i.Index != EMPTY_CARD)
            {
                for (int dir = 0; dir < 2; dir++)
                {
                    ZeroMask(GridFlags.GRIDFLAG_CHECK_DESTROY_FLAGS);

                    // Negative straight
                    minus_straight = CalculateSimilar(i.Index, blockx, blocky, 2, dir * 2) + CalculateSimilar(i.Index, blockx, blocky, 2, dir * 2 + 1) + 1;
                    
                    if (minus_straight > 3)
                    {
                        i.Flags |= GridFlags.GRIDFLAG_MINUS_STRAIGHT;
                        if (apply != 0) ApplyDestroy((uint)GridFlags.GRIDFLAG_MINUS_STRAIGHT);
                        rval = true;
                    }

                    // Positive straight
                    plus_straight = CalculateSimilar(i.Index, blockx, blocky, 3, dir * 2) + CalculateSimilar(i.Index, blockx, blocky, 3, dir * 2 + 1) + 1;
                  
                    if (plus_straight > 3)
                    {
                        i.Flags |= GridFlags.GRIDFLAG_PLUS_STRAIGHT;
                        if (apply != 0) ApplyDestroy((uint)GridFlags.GRIDFLAG_PLUS_STRAIGHT);
                        rval = true;
                    }
                    
                    // Similar earth
                    sim_earth = CalculateSimilar(i.Index, blockx, blocky, 0, dir * 2) + CalculateSimilar(i.Index, blockx, blocky, 0, dir * 2 + 1) + 1;

                    if (sim_earth >= 4)
                    {
                        i.Flags |= GridFlags.GRIDFLAG_SAME_EARTH;
                        if (apply != 0) ApplyDestroy((uint)GridFlags.GRIDFLAG_SAME_EARTH);
                        rval = true;
                    }

                    // Similar number
                    sim_number = CalculateSimilar(i.Index, blockx, blocky, 1, dir * 2) + CalculateSimilar(i.Index, blockx, blocky, 1, dir * 2 + 1) + 1;
                   
                    if (sim_number > 2)
                    {
                        i.Flags |= GridFlags.GRIDFLAG_SAME_NUMBER;
                        if (apply != 0) ApplyDestroy((uint)GridFlags.GRIDFLAG_SAME_NUMBER);
                        rval = true;
                    }
                }
            }

            return rval;
        }

        /// <summary>
        /// Check if something should be destroyed for the whole level.
        /// </summary>
        /// <param name="apply">Nonzero if destroys should apply</param>
        /// <returns>True if something is / should be destroyed, false otherwise</returns>
        bool CheckDestroyWholeLevel(int apply)
        {
            bool rval = false;

            for (int f = 0; f < m_gridSize.Y; f++)
            {
                for (int g = 0; g < m_gridSize.X; g++)
                {
                    if (CheckDestroyAt(g, f, apply))
                        rval = true;
                }
            }

            return rval;
        }


        /// <summary>
        /// Zero a specified mask for all of the items
        /// </summary>
        /// <param name="maskToZero">Mask to be zeroed.</param>
        protected void ZeroMask(GridFlags maskToZero)
        {
            for (int f = 0; f < m_gridSize.X * m_gridSize.Y; f++)
            {
                m_grid[f].Flags &= (GridFlags.GRIDFLAG_ALL ^ maskToZero);
            }
        }

        /// <summary>
        /// Get grid item at requested grid position
        /// </summary>
        /// <param name="blockx">X position</param>
        /// <param name="blocky">Y position</param>
        /// <returns>Item of this place or null if out of bounds.</returns>
        protected NpcGridItem GetGridItemAt(int blockx, int blocky)
        {
            if (blockx < 0 || blocky < 0 || blockx >= m_gridSize.X || blocky >= m_gridSize.Y)
            {
                return null;
            }

            return m_grid[blocky * m_gridSize.X + blockx];
        }
    };
}

