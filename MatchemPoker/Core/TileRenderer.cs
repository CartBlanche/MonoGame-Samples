
namespace MatchemPoker
{
    /// <summary>
    /// Enumeration for different effects used in TileGames
    /// </summary>
    public enum TileGameEffect
    {
        eCLICK = 1,
        eCHANGING,
        eCHANGE_COMPLETED,
        eEFFECT_LEVELCOMPLETED,
        eEFFECT_NEWLEVEL,
        eEFFECT_GAMEOVER,
        eILLEGAL_MOVE,
        eEFFECT_DESTROYING,
        eEFFECT_DESTROYING_BONUS,
        eEFFECT_MENU,
        eEFFECT_BUYMORE,
        eEFFECT_EXIT,
        eEFFECT_BLOCK_BEGIN_FINISHED,
        eEFFECT_BLOCK_VANISH_STARTED,
        eEFFECT_XBONUS,
        eEFFECT_SCORECHANGED,

        // number of items
        eTILEGAME_EFFECT_MAX
    }


    /// <summary>
    /// Interface for an engine running a ITilegame.
    /// Platform dependent implementation should be derived from here. 
    /// The methods are explained in the TileGameRenderer which is the XNA implementation of this interface.
    /// </summary>
    public class ITileRenderer
    {
        protected ITileGame m_game;

        public ITileRenderer() { }

        public void setGame(ITileGame game)
        {
            m_game = game;
        }
        
        public virtual void RenderTile(float x, float y, float width, float height, float angle,
                                      int mode,
                                      uint tileIndex, int arg) { }

        public virtual void RenderBackground(int index) { }
        public virtual void RenderForeground(int index) { }

        public virtual void EffectNotify(TileGameEffect effect, int arg1, int arg2) { }
        public virtual int Run(int fixedFrameTime16Bit) { return 0; }

        public static uint BuildTileIndex(TextureID textureID, uint texturePart, uint fadeFactor)
        {
            return ((texturePart & 0xFFFF) + ((((uint)textureID) & 0xFF) << 16) + ((fadeFactor & 0xFF) << 24));
        }
    }

}