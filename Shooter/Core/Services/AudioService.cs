using System.Numerics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Shooter.Core.Services;

/// <summary>
/// Audio service for playing sounds and music.
/// 
/// UNITY COMPARISON:
/// Unity has several audio systems:
/// - AudioSource component on GameObjects
/// - AudioSource.PlayClipAtPoint() for one-shots
/// - AudioListener component on camera
/// - AudioMixer for routing/effects
/// 
/// MonoGame provides:
/// - SoundEffect (loaded .wav/.ogg files)
/// - SoundEffectInstance (for looping/pitch control)
/// - Song/MediaPlayer (for music streaming)
/// 
/// EDUCATIONAL NOTE ON 3D AUDIO:
/// 
/// Unity automatically handles 3D audio positioning based on:
/// - Distance between AudioSource and AudioListener
/// - Rolloff curves (how volume decreases with distance)
/// - Spatial blend (2D vs 3D)
/// 
/// In MonoGame, you need to manually calculate:
/// 1. Distance attenuation: volume = 1.0 / (1.0 + distance * rolloff)
/// 2. Left/right panning based on listener orientation
/// 3. Doppler effect if needed
/// 
/// This implementation is a STUB for Phase 1.
/// Full 3D audio will be implemented in Phase 4.
/// </summary>
public class AudioService : IAudioService
{
    private float _masterVolume = 1.0f;
    private Dictionary<string, SoundEffect> _loadedSounds = new();
    private ContentManager? _contentManager;
    
    /// <summary>
    /// Initialize the audio service with a ContentManager
    /// </summary>
    public void Initialize(ContentManager contentManager)
    {
        _contentManager = contentManager;
        Console.WriteLine("[AudioService] Initialized");
    }

    /// <summary>
    /// Master volume control (0.0 to 1.0).
    /// Similar to AudioListener.volume in Unity.
    /// </summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = Math.Clamp(value, 0, 1.0f);
    }
    
    /// <summary>
    /// Play a sound effect (one-shot, non-looping).
    /// 
    /// UNITY COMPARISON:
    /// Similar to AudioSource.PlayClipAtPoint() in Unity.
    /// 
    /// </summary>
    /// <param name="soundName">Name of the sound file (without extension)</param>
    /// <param name="position">3D position for spatial audio (null for 2D)</param>
    /// <param name="volume">Volume multiplier (0.0 to 1.0)</param>
    public void PlaySound(string soundName, Vector3? position = null, float volume = 1.0f)
    {
        try
        {
            // Try to get already loaded sound
            if (!_loadedSounds.TryGetValue(soundName, out var soundEffect))
            {
                // Try to load it on-demand
                if (_contentManager != null)
                {
                    soundEffect = _contentManager.Load<SoundEffect>(soundName);
                    _loadedSounds[soundName] = soundEffect;
                }
                else
                {
                    Console.WriteLine($"[Audio] WARNING: Cannot load sound '{soundName}' - ContentManager not initialized");
                    return;
                }
            }

            // Play the sound (simple 2D playback for now, 3D audio in Phase 4)
            float finalVolume = volume * MasterVolume;
            soundEffect.Play(finalVolume, 0f, 0f); // volume, pitch, pan
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audio] ERROR playing sound '{soundName}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Play background music (looping).
    /// 
    /// UNITY COMPARISON:
    /// Similar to creating an AudioSource with:
    /// - audioSource.clip = musicClip
    /// - audioSource.loop = true
    /// - audioSource.Play()
    /// </summary>
    /// <param name="musicName">Name of the music file</param>
    /// <param name="loop">Whether to loop the music</param>
    public void PlayMusic(string musicName, bool loop = true)
    {
        // TODO Phase 4: Implement with MonoGame's MediaPlayer
        Console.WriteLine($"[Audio] PlayMusic: {musicName} (loop: {loop})");
        
        // Future implementation:
        // if (_loadedMusic.TryGetValue(musicName, out var song))
        // {
        //     MediaPlayer.IsRepeating = loop;
        //     MediaPlayer.Volume = MasterVolume;
        //     MediaPlayer.Play(song);
        // }
    }
    
    /// <summary>
    /// Stop all currently playing sounds and music.
    /// Similar to AudioSource.Stop() on all sources.
    /// </summary>
    public void StopAll()
    {
        Console.WriteLine("[Audio] StopAll");
        
        // TODO Phase 4:
        // MediaPlayer.Stop();
        // foreach (var instance in _activeInstances)
        // {
        //     instance.Stop();
        // }
    }
    
    /// <summary>
    /// Pre-load a sound effect from the content pipeline.
    /// Optional - sounds are loaded on-demand if not pre-loaded.
    /// </summary>
    /// <param name="soundName">Sound name (path in Content folder without extension)</param>
    public void LoadSound(string soundName)
    {
        if (_contentManager == null)
        {
            Console.WriteLine($"[Audio] WARNING: Cannot load sound '{soundName}' - ContentManager not initialized");
            return;
        }

        try
        {
            var soundEffect = _contentManager.Load<SoundEffect>(soundName);
            _loadedSounds[soundName] = soundEffect;
            Console.WriteLine($"[Audio] Loaded sound: {soundName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audio] ERROR loading sound '{soundName}': {ex.Message}");
        }
    }
}
