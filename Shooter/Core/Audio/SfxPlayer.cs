using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Shooter.Audio
{
    /// <summary>
    /// Minimal 2D sound effect player over MonoGame's SoundEffect (the weapon's AudioSource is 2D: spatial blend 0). Works without an
    /// audio device (the first failure disables sound and everything still counts calls, which the self-tests read).
    /// Mixer groups are not modelled: MainAudioMixer's WeaponShoot / WeaponOverheat / Master volumes are all 0 dB in the snapshot.
    /// </summary>
    public sealed class SfxPlayer
    {
        private readonly ContentManager _content;
        private readonly Dictionary<string, SoundEffect> _clips = new Dictionary<string, SoundEffect>();
        private bool _disabled;

        public SfxPlayer(ContentManager content) { _content = content; }

        public bool Enabled { get { return !_disabled; } }
        /// <summary>One-shots requested per clip name (counted even when no audio device exists).</summary>
        public Dictionary<string, int> PlayCounts { get; } = new Dictionary<string, int>();

        private SoundEffect Clip(string name)
        {
            if (_disabled || string.IsNullOrEmpty(name)) return null;
            if (_content == null) { _disabled = true; return null; }   // headless checks: count the calls only
            if (!_clips.TryGetValue(name, out SoundEffect clip))
            {
                try
                {
                    clip = _content.Load<SoundEffect>(name);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Audio disabled (" + e.GetType().Name + ": " + e.Message + ")");
                    _disabled = true;
                    return null;
                }
                _clips[name] = clip;
            }
            return clip;
        }

        /// <summary>AudioSource.PlayOneShot(clip) at the given volume (0..1).</summary>
        public void PlayOneShot(string name, float volume)
        {
            PlayOneShot(name, volume, 0f);
        }

        private void PlayOneShot(string name, float volume, float pan)
        {
            if (string.IsNullOrEmpty(name)) return;
            PlayCounts[name] = (PlayCounts.TryGetValue(name, out int n) ? n : 0) + 1;
            LastVolume = Math.Max(0f, Math.Min(1f, volume));
            LastPan = Math.Max(-1f, Math.Min(1f, pan));
            SoundEffect clip = Clip(name);
            if (clip != null)
            {
                try { clip.Play(LastVolume, 0f, LastPan); } catch (Exception) { _disabled = true; }
            }
        }

        /// <summary>Volume and pan of the last one-shot as requested (before the device), for the self-tests.</summary>
        public float LastVolume { get; private set; }
        public float LastPan { get; private set; }

        /// <summary>
        /// Unity's logarithmic rolloff (the AudioSource default): full volume inside minDistance, minDistance / distance outside it, and no further
        /// attenuation beyond maxDistance.
        /// </summary>
        public static float RolloffGain(float distance, float minDistance, float maxDistance)
        {
            float d = Math.Min(Math.Max(distance, minDistance), maxDistance);
            return minDistance / d;
        }

        /// <summary>
        /// AudioSource at <paramref name="position"/> with a spatial blend: the volume blends from 2D (no attenuation) to the rolloff gain and the
        /// pan from centre to the source's side of the listener (camera). <paramref name="mixerGain"/> is the linear gain of the AudioMixer group
        /// (a value above 1 cannot be played: the one-shot volume is capped at 1).
        /// </summary>
        public void PlayOneShot3D(string name, float volume, float spatialBlend, Vector3 position, Matrix listenerWorld, float minDistance, float maxDistance, float mixerGain = 1f)
        {
            Vector3 toSource = position - listenerWorld.Translation;
            float distance = toSource.Length();
            float gain = MathHelper.Lerp(1f, RolloffGain(distance, minDistance, maxDistance), spatialBlend);
            float pan = 0f;
            if (distance > 1e-4f)
            {
                pan = spatialBlend * Vector3.Dot(toSource / distance, Vector3.Normalize(listenerWorld.Right));
            }
            PlayOneShot(name, volume * gain * mixerGain, pan);
        }

        /// <summary>A clip with its own play state (AudioSource.Play / Stop / isPlaying / volume).</summary>
        public SfxInstance Create(string name) { return new SfxInstance(this, name); }

        public sealed class SfxInstance
        {
            private readonly SfxPlayer _owner;
            private readonly string _name;
            private SoundEffectInstance _inst;
            private bool _playing;

            internal SfxInstance(SfxPlayer owner, string name) { _owner = owner; _name = name; }

            public int PlayCalls { get; private set; }
            public float Volume { get; set; }
            /// <summary>AudioSource.loop: set before the first <see cref="Play"/>.</summary>
            public bool Loop { get; set; }

            public bool IsPlaying
            {
                get
                {
                    if (_inst != null) return _inst.State == SoundState.Playing;
                    return _playing;
                }
            }

            public void Play()
            {
                PlayCalls++;
                _playing = true;
                SoundEffect clip = _owner.Clip(_name);
                if (clip == null) return;
                try
                {
                    if (_inst == null) { _inst = clip.CreateInstance(); _inst.IsLooped = Loop; }
                    _inst.Volume = Math.Max(0f, Math.Min(1f, Volume));
                    _inst.Play();
                }
                catch (Exception) { _owner._disabled = true; _inst = null; }
            }

            public void Stop()
            {
                _playing = false;
                if (_inst != null) _inst.Stop();
            }

            public void ApplyVolume()
            {
                if (_inst != null) _inst.Volume = Math.Max(0f, Math.Min(1f, Volume));
            }
        }
    }
}
