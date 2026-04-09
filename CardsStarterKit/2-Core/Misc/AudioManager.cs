//-----------------------------------------------------------------------------
// AudioManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace CardsFramework.Core
{
    /// <summary>
    /// Component that manages audio playback for all sounds.
    /// </summary>
    public class AudioManager : GameComponent
    {


        /// <summary>
        /// The singleton for this type.
        /// </summary>
        static AudioManager audioManager = null;
        public static AudioManager Instance
        {
            get
            {
                return audioManager;
            }
        }

        static readonly string soundAssetLocation = "Sounds";
        static readonly string musicAssetLocation = "Music";

        // Audio Data
        Dictionary<string, SoundEffectInstance> soundBank;
        Dictionary<string, Song> musicBank;

        // Playlist state
        List<string> playlist;
        bool isPlaylistActive;
        int currentTrackIndex;
        float currentVolumeMultiplier;

        // Volume providers — injected at Initialize() so AudioManager has no dependency on game-specific settings
        Func<float> soundVolumeProvider;
        Func<float> musicVolumeProvider;



        private AudioManager(Game game)
            : base(game) { }

        /// <summary>
        /// Initialize the static AudioManager functionality.
        /// </summary>
        /// <param name="game">The game that this component will be attached to.</param>
        /// <param name="getSoundVolume">Returns the current sound effect volume (0–1). Defaults to 1.0 if null.</param>
        /// <param name="getMusicVolume">Returns the current music volume (0–1). Defaults to 1.0 if null.</param>
        public static void Initialize(Game game, Func<float> getSoundVolume = null, Func<float> getMusicVolume = null)
        {
            audioManager = new AudioManager(game);
            audioManager.soundBank = new Dictionary<string, SoundEffectInstance>();
            audioManager.musicBank = new Dictionary<string, Song>();
            audioManager.soundVolumeProvider = getSoundVolume ?? (() => 1.0f);
            audioManager.musicVolumeProvider = getMusicVolume ?? (() => 1.0f);

            game.Components.Add(audioManager);
        }





        /// <summary>
        /// Loads a single sound into the sound manager, giving it a specified alias.
        /// </summary>
        /// <param name="contentName">The content name of the sound file. Assumes all sounds are located under
        /// the "Sounds" folder in the content project.</param>
        /// <param name="alias">Alias to give the sound. This will be used to identify the sound uniquely.</param>
        /// <param name="replaceExisting">If true, replaces and disposes an existing alias entry.</param>
        /// <remarks>Loading a sound with an alias that is already used will have no effect unless replaceExisting is true.</remarks>
        public static void LoadSound(string contentName, string alias, bool replaceExisting = false)
        {
            SoundEffect soundEffect = audioManager.Game.Content.Load<SoundEffect>(Path.Combine(soundAssetLocation, contentName));
            SoundEffectInstance soundEffectInstance = soundEffect.CreateInstance();

            if (audioManager.soundBank.ContainsKey(alias))
            {
                if (!replaceExisting)
                    return;

                audioManager.soundBank[alias].Dispose();
                audioManager.soundBank[alias] = soundEffectInstance;
                return;
            }

            audioManager.soundBank.Add(alias, soundEffectInstance);
        }

        /// <summary>
        /// Loads a single song into the sound manager, giving it a specified alias.
        /// </summary>
        /// <param name="contentName">The content name of the song file. Assumes all songs are located under
        /// the "Music" folder in the content project.</param>
        /// <param name="alias">Alias to give the song. This will be used to identify the song uniquely.</param>
        /// <param name="replaceExisting">If true, replaces an existing alias entry.</param>
        /// <remarks>Loading a song with an alias that is already used will have no effect unless replaceExisting is true.</remarks>
        public static void LoadSong(string contentName, string alias, bool replaceExisting = false)
        {
            Song song = audioManager.Game.Content.Load<Song>(Path.Combine(musicAssetLocation, contentName));

            if (audioManager.musicBank.ContainsKey(alias))
            {
                if (!replaceExisting)
                    return;

                audioManager.musicBank[alias] = song;
                return;
            }

            audioManager.musicBank.Add(alias, song);
        }

        /// <summary>
        /// Legacy no-op hook kept for compatibility.
        /// Games should explicitly call LoadSound(...) for their own audio assets.
        /// </summary>
        public static void LoadSounds()
        {
            // Intentionally empty.
        }

        /// <summary>
        /// Legacy no-op hook kept for compatibility.
        /// Games should explicitly call LoadSong(...) and SetPlaylist(...).
        /// </summary>
        public static void LoadMusic()
        {
            // Intentionally empty.
        }

        /// <summary>
        /// Defines the playlist order by alias for PlayPlaylist().
        /// Aliases not present in the music bank are ignored.
        /// </summary>
        public static void SetPlaylist(params string[] songAliases)
        {
            audioManager.playlist = new List<string>();

            if (songAliases == null)
                return;

            foreach (string alias in songAliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                    continue;

                if (audioManager.musicBank.ContainsKey(alias))
                    audioManager.playlist.Add(alias);
            }
        }




        /// <summary>
        /// Indexer. Return a sound instance by name.
        /// </summary>
        public SoundEffectInstance this[string soundName]
        {
            get
            {
                if (audioManager.soundBank.ContainsKey(soundName))
                {
                    return audioManager.soundBank[soundName];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Plays a sound by name with optional parameters for looping, volume, pitch, and stereo panning.
        /// </summary>
        /// <param name="soundName">The name of the sound to play.</param>
        /// <param name="isLooped">Indicates if the sound should loop. Default is false.</param>
        /// <param name="volume">Custom volume (0.0 to 1.0). If null, uses SoundVolume from settings.</param>
        /// <param name="pitch">Pitch adjustment (-1.0 to 1.0, where 0 is normal pitch). Default is 0.</param>
        /// <param name="pan">Stereo panning (-1.0 = left ear, 0.0 = center, 1.0 = right ear). Default is 0.</param>
        public static void PlaySound(string soundName, bool isLooped = false, float? volume = null, float pitch = 0f, float pan = 0f)
        {
            if (audioManager?.soundBank == null) return;

            // If the sound exists, start it
            if (audioManager.soundBank.ContainsKey(soundName))
            {
                var sound = audioManager.soundBank[soundName];

                // Set loop if different from current state
                if (sound.IsLooped != isLooped)
                {
                    sound.IsLooped = isLooped;
                }

                // Use custom volume or default to settings
                sound.Volume = volume ?? audioManager.soundVolumeProvider();

                // Set pitch (clamped to valid range)
                sound.Pitch = MathHelper.Clamp(pitch, -1.0f, 1.0f);

                // Set stereo panning (clamped to valid range)
                sound.Pan = MathHelper.Clamp(pan, -1.0f, 1.0f);

                sound.Play();
            }
        }

        /// <summary>
        /// Plays a sound by name with pitch adjustment.
        /// </summary>
        /// <param name="soundName">The name of the sound to play.</param>
        /// <param name="pitch">Pitch adjustment (-1.0 to 1.0, where 0 is normal pitch).
        /// Higher values (0.2-0.5) make the sound higher pitched, lower values (-0.3 to -0.5) make it lower pitched.</param>
        public static void PlaySoundWithPitch(string soundName, float pitch)
        {
            PlaySound(soundName, pitch: pitch);
        }

        /// <summary>
        /// Stops a sound mid-play. If the sound is not playing, this
        /// method does nothing.
        /// </summary>
        /// <param name="soundName">The name of the sound to stop.</param>
        public static void StopSound(string soundName)
        {
            // If the sound exists, stop it
            if (audioManager.soundBank.ContainsKey(soundName))
            {
                audioManager.soundBank[soundName].Stop();
            }
        }

        /// <summary>
        /// Stops all currently playing sounds.
        /// </summary>
        public static void StopSounds()
        {
            foreach (SoundEffectInstance sound in audioManager.soundBank.Values)
            {
                if (sound.State != SoundState.Stopped)
                {
                    sound.Stop();
                }
            }
        }

        /// <summary>
        /// Pause or resume all sounds.
        /// </summary>
        /// <param name="resumeSounds">True to resume all paused sounds or false
        /// to pause all playing sounds.</param>
        public static void PauseResumeSounds(bool resumeSounds)
        {
            SoundState state = resumeSounds ? SoundState.Paused : SoundState.Playing;

            foreach (SoundEffectInstance sound in audioManager.soundBank.Values)
            {
                if (sound.State == state)
                {
                    if (resumeSounds)
                    {
                        sound.Resume();
                    }
                    else
                    {
                        sound.Pause();
                    }
                }
            }
        }
        /// <summary>
        /// Play music by name. This stops the currently playing music first. Music will loop until stopped.
        /// </summary>
        /// <param name="musicSoundName">The name of the music sound.</param>
        /// <param name="volumeMultiplier">Optional volume multiplier applied to MusicVolume setting (0.0 to 1.0). Default is 1.0.</param>
        /// <remarks>If the desired music is not in the music bank, nothing will happen.</remarks>
        public static void PlayMusic(string musicSoundName, float volumeMultiplier = 1.0f)
        {
            // If the music sound exists
            if (audioManager.musicBank.ContainsKey(musicSoundName))
            {
                audioManager.isPlaylistActive = false; // Single track — no playlist cycling

                if (MediaPlayer.State != MediaState.Stopped)
                    MediaPlayer.Stop();

                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = audioManager.musicVolumeProvider() * MathHelper.Clamp(volumeMultiplier, 0f, 1f);

                MediaPlayer.Play(audioManager.musicBank[musicSoundName]);
            }
        }

        /// <summary>
        /// Starts cycling through the music playlist defined in LoadMusic().
        /// Each track plays to completion before the next one begins, looping indefinitely.
        /// </summary>
        /// <param name="volumeMultiplier">Optional volume multiplier applied to MusicVolume setting (0.0 to 1.0). Default is 1.0.</param>
        public static void PlayPlaylist(float volumeMultiplier = 1.0f)
        {
            if (audioManager.playlist == null || audioManager.playlist.Count == 0)
                return;

            audioManager.currentTrackIndex = 0;
            audioManager.currentVolumeMultiplier = MathHelper.Clamp(volumeMultiplier, 0f, 1f);
            audioManager.isPlaylistActive = true;

            audioManager.PlayCurrentTrack();
        }

        private void PlayCurrentTrack()
        {
            string alias = playlist[currentTrackIndex];
            if (!musicBank.TryGetValue(alias, out Song song))
                return;

            if (MediaPlayer.State != MediaState.Stopped)
                MediaPlayer.Stop();

            MediaPlayer.IsRepeating = false;
            MediaPlayer.Volume = musicVolumeProvider() * currentVolumeMultiplier;
            MediaPlayer.Play(song);
        }

        /// <summary>
        /// Advances to the next track when the current one finishes.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (isPlaylistActive && MediaPlayer.State == MediaState.Stopped)
            {
                currentTrackIndex = (currentTrackIndex + 1) % playlist.Count;
                PlayCurrentTrack();
            }
        }

        /// <summary>
        /// Stops the currently playing music.
        /// </summary>
        public static void StopMusic()
        {
            audioManager.isPlaylistActive = false; // Prevent Update() from restarting
            if (MediaPlayer.State != MediaState.Stopped)
            {
                MediaPlayer.Stop();
            }
        }

        /// <summary>
        /// Pauses the currently playing music.
        /// </summary>
        public static void PauseMusic()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
            }
        }

        /// <summary>
        /// Resumes the currently paused music.
        /// </summary>
        public static void ResumeMusic()
        {
            if (MediaPlayer.State == MediaState.Paused)
            {
                MediaPlayer.Resume();
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