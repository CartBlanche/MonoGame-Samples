//-----------------------------------------------------------------------------
// AudioManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;
using System.Linq;

namespace CatapultGame
{
    /// <summary>
    /// Component that manages audio playback for all sounds.
    /// </summary>
    public class AudioManager : GameComponent
    {
        /// <summary>
        /// The singleton for this type.
        /// </summary>
        private static AudioManager audioManager = null;

        private SoundEffectInstance musicSound;
        private Dictionary<string, SoundEffectInstance> soundBank;
        private string[,] soundNames;


        private AudioManager(Game game)
            : base(game) { }

        /// <summary>
        /// Initialize the static AudioManager functionality.
        /// </summary>
        /// <param name="game">The game that this component will be attached to.</param>
        public static void Initialize(Game game)
        {
            audioManager = new AudioManager(game);

            if (game != null)
            {
                game.Components.Add(audioManager);
            }
        }


        /// <summary>
        /// Loads a sounds and organizes them for future usage
        /// </summary>
        public static void LoadSounds()
        {
            string soundLocation = "Sounds/";
            audioManager.soundNames = new string[,] { 
                            {"CatapultExplosion", "catapultExplosion"}, 
                            {"Lose", "gameOver_Lose"},
                            {"Win", "gameOver_Win"},
                            {"BoulderHit", "boulderHit"},
                            {"CatapultFire", "catapultFire"},
                            {"RopeStretch", "ropeStretch"}};

            audioManager.soundBank = new Dictionary<string, SoundEffectInstance>();

            for (int i = 0; i < audioManager.soundNames.GetLength(0); i++)
            {
                SoundEffect se = audioManager.Game.Content.Load<SoundEffect>(
                    soundLocation + audioManager.soundNames[i, 0]);
                audioManager.soundBank.Add(
                    audioManager.soundNames[i, 1], se.CreateInstance());
            }
        }

        /// <summary>
        /// Plays a sound by name.
        /// </summary>
        /// <param name="soundName">The name of the sound to play</param>
        public static void PlaySound(string soundName)
        {
            // If the sound exists, start it
            if (audioManager.soundBank.ContainsKey(soundName))
                audioManager.soundBank[soundName].Play();
        }

        /// <summary>
        /// Plays a sound by name.
        /// </summary>
        /// <param name="soundName">The name of the sound to play</param>
        /// <param name="isLooped">Indicates if the sound should loop</param>
        public static void PlaySound(string soundName, bool isLooped)
        {
            // If the sound exists, start it
            if (audioManager.soundBank.ContainsKey(soundName))
            {
                if (audioManager.soundBank[soundName].IsLooped != isLooped)
                    audioManager.soundBank[soundName].IsLooped = isLooped;

                audioManager.soundBank[soundName].Play();
            }
        }


        /// <summary>
        /// Stops a sound mid-play. If the sound is not playing, this
        /// method does nothing.
        /// </summary>
        /// <param name="soundName">The name of the sound to stop</param>
        public static void StopSound(string soundName)
        {
            // If the sound exists, stop it
            if (audioManager.soundBank.ContainsKey(soundName))
                audioManager.soundBank[soundName].Stop();
        }

        /// <summary>
        /// Stops a sound mid-play. If the sound is not playing, this
        /// method does nothing.
        /// </summary>
        /// <param name="soundName">The name of the sound to stop</param>
        public static void StopSounds()
        {
            var soundEffectInstances = from sound in audioManager.soundBank.Values
                                       where sound.State != SoundState.Stopped
                                       select sound;

            foreach (var soundeffectInstance in soundEffectInstances)
                soundeffectInstance.Stop();
        }

        /// <summary>
        /// Pause or Resume all sounds to support pause screen
        /// </summary>
        /// <param name="isPause">Should pause or resume?</param>
        public static void PauseResumeSounds(bool isPause)
        {
            SoundState state = isPause ? SoundState.Paused : SoundState.Playing;

            var soundEffectInstances = from sound in audioManager.soundBank.Values
                                where sound.State == state
                                select sound;

            foreach (var soundeffectInstance in soundEffectInstances)
            {
                if (isPause)
                    soundeffectInstance.Play();
                else
                    soundeffectInstance.Pause();
            }
        }
        /// <summary>
        /// Play music by sound name.
        /// </summary>
        /// <param name="musicSoundName">The name of the music sound</param>
        public static void PlayMusic(string musicSoundName)
        {
            // Stop the old music sound
            if (audioManager.musicSound != null)
                audioManager.musicSound.Stop(true);

            // If the music sound exists
            if (audioManager.soundBank.ContainsKey(musicSoundName))
            {
                // Get the instance and start it
                audioManager.musicSound = audioManager.soundBank[musicSoundName];
                if (!audioManager.musicSound.IsLooped)
                    audioManager.musicSound.IsLooped = true;
                audioManager.musicSound.Play();
            }
        }

        /// <summary>
        /// Clean up the component when it is disposing.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    foreach (var item in soundBank)
                    {
                        item.Value.Dispose();
                    }
                    soundBank.Clear();
                    soundBank = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
