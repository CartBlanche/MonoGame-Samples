using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;


namespace MatchemPoker
{
    /// <summary>
    /// Implementation of a ITileRenderer class which will function as renderer and event receiver for any tilegame.
    /// </summary>
    public class TileGameRenderer : ITileRenderer
    {

        // Sound effect constants
        const int SOUND_EFFECT_COUNT = 10;
        const int AUDIO_SAMPLE_CLICK = 0;
        const int AUDIO_SAMPLE_CHANGE = 1;
        const int AUDIO_SAMPLE_CHANGE_COMPLETED = 2;
        const int AUDIO_SAMPLE_DESTROY = 3;
        const int AUDIO_LEVEL_COMPLETED = 4;
        const int AUDIO_GAME_OVER = 5;
        const int AUDIO_ILLEGAL_MOVE = 6;
        const int AUDIO_NEXT_LEVEL = 7;
        const int AUDIO_SAMPLE_EXP_BONUS = 8;
        const int AUDIO_SAMPLE_X_BONUS = 9;


        // Names of the soundeffects loaded from the content. The actual files are .WAV's
        static readonly string[] sound_effect_names = {
                                                        "click",
                                                        "change",                   // milton
                                                        "change_completed",         // milton
                                                        "destroy",                  // milton
                                                        "fanfare",                  // by milton
                                                        "game_over",                // by milton
                                                        "illegal_move",             // milton
                                                        "next_level",               // by milton @ freesound
                                                        "exp_bonus",                // by mattwasser
                                                        "xbonus"                    // by Christianjinnyzoe
                                                      };

        // Name of the textures loaded from the content
        static readonly string[] texture_names = { "logo", 
                                                   "playing_cards", 
                                                   "playing_cards_selected", 
                                                   /*"scorefont"*/"logo", 
                                                   /*"gradient"*/"logo", 
                                                   "timer_numbers", 
                                                   "timer_frames", 
                                                   "particles", 
                                                   "font", 
                                                   "menu_bg", 
                                                   "dice_pause_play" };

        // In how many "blocks" a corresponding texture is divided. 4,5 would mean texture is divided in 4 parts horizontally and in 5 parts vertically.
        static readonly int[] texture_blocks = {1,1,
                                                4,5,
                                                4,5,
                                                10,1,
                                                1,1,
                                                4,5,
                                                1,2,
                                                2,2,
                                                16,6,
                                                2,2,
                                                2,1 };

        // Sound effects        
        SoundEffect[] soundEffects;

        // Viewport size
        protected float vpWidth;
        protected float vpHeight;

        protected SpriteBatch spriteBatch;
        protected GameTexture[] textures;

        /// <summary>
        /// Initialize XNA specific parts, load textures and sound.
        /// </summary>
        /// <param name="Content">Contant manager required to load textures and sounds.</param>
        /// <param name="gd">GraphicsDevice for initializing the viewportsize correctly.</param>
        /// <param name="gameWidth">Canonical game width in pixels. If 0 the live viewport width is used.</param>
        /// <param name="gameHeight">Canonical game height in pixels. If 0 the live viewport height is used.</param>
        public void Init(ContentManager Content, GraphicsDevice gd, int gameWidth = 0, int gameHeight = 0)
        {
            spriteBatch = new SpriteBatch(gd);
            textures = new GameTexture[(int)TextureID.TexEndOfList];
            vpWidth  = (gameWidth  > 0 ? (float)gameWidth  : (float)gd.Viewport.Width)  * 1024.0f;
            vpHeight = (gameHeight > 0 ? (float)gameHeight : (float)gd.Viewport.Height) * 1024.0f;
            
            // load textures
            for (int i = 0; i < (int)TextureID.TexEndOfList - 1; i++)
            {
                Texture2D texture = Content.Load<Texture2D>(Path.Combine("images", texture_names[i]));
                textures[i] = new GameTexture((TextureID)i, texture, "Texture", texture_blocks[i * 2], texture_blocks[i * 2 + 1]);
            }

            // Load the sounds
            soundEffects = new SoundEffect[SOUND_EFFECT_COUNT];

            for (int i = 0; i < SOUND_EFFECT_COUNT; i++)
            {
                soundEffects[i] = Content.Load<SoundEffect>(Path.Combine("sounds", sound_effect_names[i]));
            }
        }

        /// <summary>
        /// Called before the start of each frame.
        /// </summary>
        public void StartFrame()
        {
            Matrix tmatrix = Matrix.CreateScale(1 / 1024.0f);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, tmatrix);
        }

        /// <summary>
        /// Called after everything is rendered.
        /// </summary>
        public void EndFrame()
        {
            spriteBatch.End();
        }


        /// <summary>
        /// The rendering function used to draw everything in a game. Everything is mapped through this for portability. 
        /// </summary>
        /// <param name="x">Target X position of a tile to be rendered.</param>
        /// <param name="y">Target Y position of a tile to be rendered.</param>
        /// <param name="width">Target tile width</param>
        /// <param name="height">Target tile height</param>
        /// <param name="angle">Target Angle (in radians)</param>
        /// <param name="mode">Unused for XNA-port. Original implementation used alpha blending when mode was zero and additive blending when it wasn't</param>
        /// <param name="tileIndex">Consists of several parts: Least significant 8 bits indicates a PART (according textures division blocks) to be rendered. Second 8 bits indicates a texture to be used. Most significant 8 bits are negative alpha (fade).</param>
        /// <param name="arg">Unused.</param>
        public override void RenderTile(float x, float y, float width, float height, float angle,
                                        int mode,
                                        uint tileIndex, int arg)
        {
            uint texNum = ((tileIndex >> 16) & 255) - 1;
            uint fade = ((tileIndex) >> 24) & 255;

            float a = 1.0f - fade / 255.0f;

            if (a <= 0.0f)
            {
                // Not visible at all
                return;
            }

            Color c = new Color(1.0f * a, 1.0f * a, 1.0f * a, a);

            uint ind = (tileIndex & 65535);
            int yp = (int)(ind / textures[texNum].TilesX);
            int xp = (int)(ind - yp * textures[texNum].TilesX);

            int beginPosX = textures[texNum].XnaTexture.Width * xp / textures[texNum].TilesX;
            int beginPosY = textures[texNum].XnaTexture.Height * yp / textures[texNum].TilesY;

            Rectangle srect = new Rectangle(beginPosX, beginPosY,
                                            textures[texNum].XnaTexture.Width * (xp + 1) / textures[texNum].TilesX - beginPosX,
                                            textures[texNum].XnaTexture.Height * (yp + 1) / textures[texNum].TilesY - beginPosY);

            Rectangle trect = new Rectangle((int)(x * vpWidth), (int)(y * vpWidth), (int)(width * vpWidth), (int)(height * vpWidth));
            Vector2 orig = new Vector2(srect.Width / 2, srect.Height / 2);
            trect.X += (int)trect.Width / 2;
            trect.Y += (int)trect.Height / 2;
            spriteBatch.Draw(textures[texNum].XnaTexture, trect, srect, c, angle, orig, SpriteEffects.None, 0.0f);
        }


        /// <summary>
        /// Unused for XNA matchempoker 
        /// </summary>
        /// <param name="index"></param>
        public override void RenderBackground(int index) { }

        /// <summary>
        /// Unused for XNA matchempoker 
        /// </summary>
        /// <param name="index"></param>
        public override void RenderForeground(int index) { }

        /// <summary>
        /// The game calls this method when some effect (like a sound) is required. 
        /// </summary>
        /// <param name="effect">Effect to be played</param>
        /// <param name="arg1">Custom argument for the effect, meaning changes according to the effect.</param>
        /// <param name="arg2">Custom argument for the effect, meaning changes according to the effect.</param>
        public override void EffectNotify(TileGameEffect effect, int arg1, int arg2)
        {
            switch (effect)
            {
                case TileGameEffect.eEFFECT_EXIT:
                    break;

                case TileGameEffect.eEFFECT_DESTROYING:
                case TileGameEffect.eEFFECT_DESTROYING_BONUS:
                    {
                        soundEffects[AUDIO_SAMPLE_DESTROY].Play(1.0f, 0.0f, 0.0f);

                        if (arg1 > 0)
                        {
                            // Bonus sound as well
                            soundEffects[AUDIO_SAMPLE_EXP_BONUS].Play(1.0f, (float)arg1 / 12.0f, 0.0f);
                        }

                    }
                    break;

                case TileGameEffect.eEFFECT_GAMEOVER:
                    soundEffects[AUDIO_GAME_OVER].Play();
                    break;

                case TileGameEffect.eEFFECT_NEWLEVEL:
                    soundEffects[AUDIO_NEXT_LEVEL].Play();
                    break;

                case TileGameEffect.eEFFECT_LEVELCOMPLETED:
                    soundEffects[AUDIO_LEVEL_COMPLETED].Play();
                    break;

                case TileGameEffect.eEFFECT_XBONUS:
                    soundEffects[AUDIO_SAMPLE_X_BONUS].Play();
                    break;

                case TileGameEffect.eEFFECT_BLOCK_BEGIN_FINISHED:
                    soundEffects[AUDIO_SAMPLE_CHANGE].Play(0.4f, 0.0f, 0.0f);
                    break;

                case TileGameEffect.eEFFECT_BLOCK_VANISH_STARTED:
                    soundEffects[AUDIO_SAMPLE_CHANGE_COMPLETED].Play(0.4f, 0.0f, 0.0f);
                    break;

                case TileGameEffect.eCHANGING:
                    soundEffects[AUDIO_SAMPLE_CHANGE].Play(1.0f, 0.0f, 0.0f);
                    break;

                case TileGameEffect.eCHANGE_COMPLETED:
                    soundEffects[AUDIO_SAMPLE_CHANGE_COMPLETED].Play(1.0f, 0.0f, 0.0f);
                    break;

                case TileGameEffect.eILLEGAL_MOVE:
                    soundEffects[AUDIO_ILLEGAL_MOVE].Play();
                    break;

                case TileGameEffect.eCLICK:
                case TileGameEffect.eEFFECT_MENU:
                    soundEffects[AUDIO_SAMPLE_CLICK].Play();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Empty implementation
        /// </summary>
        /// <param name="fixedFrameTime16Bit"></param>
        /// <returns></returns>
        public override int Run(int fixedFrameTime16Bit)
        {
            return 0;
        }
    }
}