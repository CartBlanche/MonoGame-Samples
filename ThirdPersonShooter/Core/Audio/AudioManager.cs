using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Static audio service. Music = Song on MediaPlayer (looped, sequential 0.5 s fade out / fade in when the track changes; MediaPlayer has one song so no overlap);
    /// SFX = pooled SoundEffectInstance voices (max 32). 3D sounds use the manual Godot inverse-distance model of <see cref="AudioMath"/> (Volume + Pan set from the
    /// camera position/right vector, refreshed every frame), not OpenAL Apply3D. Pan only reaches the speakers for mono buffers (OpenAL ignores source position of stereo
    /// buffers), so the 3D sounds are loaded from raw wav and downmixed to mono. 3D sounds whose emitter lies in an audio zone play a reverb variant (<see cref="AudioZones"/>, <see cref="SfxPcm.Reverb"/>).
    /// Null-safe: without <see cref="Init"/> (or with no audio device, or in --sim/--shot) nothing is played; logging (`--audio-log` or env TPS_AUDIO_LOG) still prints every decision.
    /// </summary>
    public static class AudioManager
    {
        sealed class Voice { public SoundDef Def; public SoundEffectInstance Inst; public Vector3 Pos; public float Scale; public int Emitter; }

        public static bool Log;                      // `[audio]` console lines
        public static bool Playing;                  // real playback active (device present, not --sim/--shot)
        public static float LogTime;                 // sim clock for the log lines (set by Simulation)
        public static int PlayCount;                 // test aid: number of play requests
        public static string LastDevice = "";

        static float _master = 1f, _music = 1f, _sfx = 1f;
        static Vector3 _lPos, _lRight = Vector3.Right; static bool _hasListener;
        static readonly Dictionary<string, SoundEffect> Fx = new();
        static readonly Dictionary<string, Pcm> Mono3D = new();                       // dry mono PCM of the 3D sounds (source of the reverb renders)
        static readonly Dictionary<(string, AudioBus), SoundEffect> Wet = new();      // lazily rendered (sound, bus) reverb variants
        static string _root;
        static readonly List<Voice> Voices = new();
        const int MaxVoices = 32;
        static Song _menuSong, _levelSong;
        static MusicTrack _want = MusicTrack.None, _cur = MusicTrack.None; static float _fade; static float _lastMusicVol = -1f;

        /// <summary>Reads `--audio-log` / env TPS_AUDIO_LOG.</summary>
        public static void Configure(string[] args)
        {
            Log = Environment.GetEnvironmentVariable("TPS_AUDIO_LOG") is { Length: > 0 } || (args != null && Array.IndexOf(args, "--audio-log") >= 0);
        }

        /// <summary>Load the content. <paramref name="playback"/> false (--sim/--shot) keeps everything silent and device-free. Any failure (no OpenAL device, missing xnb) disables playback.</summary>
        public static void Init(ContentManager content, bool playback) { InitMenu(content, playback); InitLevel(content); }

        /// <summary>Boot half of <see cref="Init"/>: enables playback and loads only the menu song (the menu needs nothing else).</summary>
        public static void InitMenu(ContentManager content, bool playback)
        {
            try { _root = Path.Combine(AppContext.BaseDirectory, content.RootDirectory); AudioZones.Load(_root); if (Log) Console.WriteLine($"[audio] zones: {AudioZones.Count} volumes from level.scene.json"); }
            catch (Exception e) { Console.WriteLine($"[audio] zones not loaded (all sounds dry): {e.GetType().Name}: {e.Message}"); }
            if (!playback) { Playing = false; if (Log) Console.WriteLine("[audio] init: playback disabled (sim/shot run), logging only"); return; }
            try
            {
                _menuSong ??= content.Load<Song>(AudioTable.MenuMusicAsset);
                Playing = true; SoundEffect.MasterVolume = 1f;
                if (Log) Console.WriteLine("[audio] init: ok, menu song loaded");
            }
            catch (Exception e) { Playing = false; Console.WriteLine($"[audio] init failed, audio disabled: {e.GetType().Name}: {e.Message}"); }
        }

        /// <summary>Play half of <see cref="Init"/>: every sound effect plus the level song (a loading-screen step). No-op when playback is off.</summary>
        public static void InitLevel(ContentManager content)
        {
            if (!Playing) return;
            try
            {
                foreach (var d in AudioTable.All)
                {
                    if (Fx.ContainsKey(d.Asset)) continue;
                    if (!d.Is3D) { Fx[d.Asset] = content.Load<SoundEffect>(d.ContentPath); continue; }
                    var mono = SfxPcm.ToMono(SfxPcm.ReadWav(File.ReadAllBytes(Path.Combine(_root, d.FilePath))));   // mono: OpenAL pans/positions only mono buffers
                    Mono3D[d.Asset] = mono; Fx[d.Asset] = new SoundEffect(SfxPcm.ToBytes16(mono), mono.Rate, AudioChannels.Mono);
                }
                _levelSong ??= content.Load<Song>(AudioTable.LevelMusicAsset);
                if (Log) Console.WriteLine($"[audio] init: ok, {Fx.Count} sfx + 2 songs loaded");
            }
            catch (Exception e) { Playing = false; Console.WriteLine($"[audio] init failed, audio disabled: {e.GetType().Name}: {e.Message}"); }
        }

        // ------------------------------------------------------------------ volumes (live)
        public static void ApplySettings(GameSettings s) => SetVolumes(s.MasterVolume, s.MusicVolume, s.SfxVolume);
        public static void SetVolumes(float master, float music, float sfx)
        {
            _master = AudioMath.Clamp01(master); _music = AudioMath.Clamp01(music); _sfx = AudioMath.Clamp01(sfx);
            if (Log) Console.WriteLine($"[audio] volumes master={_master:F2} music={_music:F2} sfx={_sfx:F2} -> music gain {MusicGain():F3}, sfx bus {AudioMath.BusGain(_master, _sfx):F3}");
            Refresh();
        }
        static float MusicGain() => AudioMath.BusGain(_master, _music);

        public static void SetListener(Vector3 pos, Vector3 right) { _lPos = pos; _lRight = right; _hasListener = true; }

        // ------------------------------------------------------------------ sfx
        /// <summary>Play an event. 3D events need <paramref name="pos"/>; <paramref name="scale"/> multiplies the final linear gain (anim-track use_blend weight).</summary>
        /// <param name="emitter">Godot node identity (one per robot): a sound only replaces a still-playing stream of the SAME node (channel + emitter).</param>
        public static void Play(AudioEvent e, Vector3? pos = null, float scale = 1f, int emitter = 0)
        {
            var d = AudioTable.Get(e); PlayCount++;
            float dist = 0f, pan = 0f, db = d.VolumeDb; AudioBus bus = AudioBus.Master;
            if (d.Is3D)
            {
                Vector3 p = pos ?? _lPos; dist = _hasListener ? Vector3.Distance(p, _lPos) : 0f;
                db = AudioMath.Attenuation3DDb(dist, d.UnitSize, d.VolumeDb); pan = _hasListener ? AudioMath.Pan(p, _lPos, _lRight) : 0f;
                bus = AudioZones.BusAt(p);      // Godot: the emitter's position picks the Area3D audio_bus_override (2D sounds and music are dry)
            }
            float vol = Gain(db, scale);
            if (Log) Console.WriteLine($"[audio] t={LogTime:F2} play {d.Asset} {(d.Is3D ? "3D" : "2D")} vol={vol:F3} att={db:F1}dB dist={(d.Is3D ? dist.ToString("F1", CultureInfo.InvariantCulture) : "-")} pan={pan:F2} pitch={d.PitchScale:F2}" + (d.Is3D ? $" zone={bus}" : "") + (scale != 1f ? $" blend={scale:F2}" : ""));
            if (!Playing) return;
            try
            {
                if (d.Channel != null) for (int i = Voices.Count - 1; i >= 0; i--) if (Voices[i].Def.Channel == d.Channel && Voices[i].Emitter == emitter) { Kill(i); } // same Godot node: replaces the playing stream
                if (Voices.Count >= MaxVoices) Kill(0);
                var inst = (bus != AudioBus.Master ? WetFx(d, bus) : Fx[d.Asset]).CreateInstance();
                inst.Volume = vol; inst.Pan = pan; inst.Pitch = MathHelper.Clamp(MathF.Log2(d.PitchScale), -1f, 1f);
                inst.Play();
                Voices.Add(new Voice { Def = d, Inst = inst, Pos = pos ?? Vector3.Zero, Scale = scale, Emitter = emitter });
            }
            catch (Exception ex) { Console.WriteLine($"[audio] SoundEffect play failed ({d.Asset}): {ex.GetType().Name}: {ex.Message}"); }
        }

        /// <summary>Reverb variant of a 3D sound for a zone bus, rendered on first use (SfxPcm.Reverb) and cached.</summary>
        static SoundEffect WetFx(SoundDef d, AudioBus bus)
        {
            if (Wet.TryGetValue((d.Asset, bus), out var w)) return w;
            var sw = System.Diagnostics.Stopwatch.StartNew(); var dry = Mono3D[d.Asset]; var wp = SfxPcm.Reverb(dry, AudioTable.ReverbFor(bus));
            w = new SoundEffect(SfxPcm.ToBytes16(wp), wp.Rate, AudioChannels.Mono); Wet[(d.Asset, bus)] = w;
            if (Log) Console.WriteLine($"[audio] reverb render {d.Asset} @ {bus}: {dry.Seconds:F2}s -> {wp.Seconds:F2}s in {sw.ElapsedMilliseconds} ms");
            return w;
        }

        static float Gain(float db, float scale) => AudioMath.Clamp01(AudioMath.DbToLinear(db) * AudioMath.BusGain(_master, _sfx) * scale);

        static void Kill(int i) { var v = Voices[i]; try { v.Inst.Stop(); v.Inst.Dispose(); } catch { } Voices.RemoveAt(i); }

        /// <summary>Re-evaluate volume/pan of the playing voices (listener moved, sliders changed) and music volume.</summary>
        static void Refresh()
        {
            if (!Playing) return;
            try
            {
                for (int i = Voices.Count - 1; i >= 0; i--)
                {
                    var v = Voices[i];
                    if (v.Inst.State == SoundState.Stopped) { v.Inst.Dispose(); Voices.RemoveAt(i); continue; }
                    float db = v.Def.VolumeDb, pan = 0f;
                    if (v.Def.Is3D && _hasListener) { db = AudioMath.Attenuation3DDb(Vector3.Distance(v.Pos, _lPos), v.Def.UnitSize, v.Def.VolumeDb); pan = AudioMath.Pan(v.Pos, _lPos, _lRight); }
                    v.Inst.Volume = Gain(db, v.Scale); v.Inst.Pan = pan;
                }
                float mv = MusicGain() * AudioMath.Clamp01(_fade);
                if (Math.Abs(mv - _lastMusicVol) > 1e-4f) { MediaPlayer.Volume = mv; _lastMusicVol = mv; }
            }
            catch (Exception ex) { Console.WriteLine($"[audio] refresh failed: {ex.GetType().Name}: {ex.Message}"); Playing = false; }
        }

        // ------------------------------------------------------------------ music
        public static void PlayMusic(MusicTrack t)
        {
            if (t == _want) return;
            _want = t;
            if (Log) Console.WriteLine($"[audio] t={LogTime:F2} music request {t} (current {_cur})");
        }

        /// <summary>Per frame (also while paused or in menus): music fade state machine, voice cleanup, live volume refresh.</summary>
        public static void Update(float dt)
        {
            float step = dt / AudioTable.MusicFadeSeconds;
            if (_want != _cur)
            {
                if (_cur == MusicTrack.None) StartSong(_want);
                else
                {
                    _fade -= step;
                    if (_fade <= 0f) { _fade = 0f; StopSong(); }
                }
            }
            else if (_cur != MusicTrack.None && _fade < 1f)
            {
                _fade = Math.Min(1f, _fade + step); if (_fade >= 1f && Log) Console.WriteLine($"[audio] music {_cur} fade-in done");
            }
            Refresh();
        }

        static void StartSong(MusicTrack t)
        {
            if (t == MusicTrack.None) { _cur = MusicTrack.None; return; }
            _cur = t; _fade = 0f; _lastMusicVol = -1f;
            if (Log) Console.WriteLine($"[audio] t={LogTime:F2} music play {t} loop=true fade-in {AudioTable.MusicFadeSeconds:F1}s gain={MusicGain():F3}");
            if (!Playing) return;
            try { MediaPlayer.IsRepeating = true; MediaPlayer.Volume = 0f; MediaPlayer.Play(t == MusicTrack.Menu ? _menuSong : _levelSong); }
            catch (Exception ex) { Console.WriteLine($"[audio] Song play failed ({t}): {ex.GetType().Name}: {ex.Message}"); Playing = false; }
        }

        static void StopSong()
        {
            if (Log) Console.WriteLine($"[audio] t={LogTime:F2} music stop {_cur} (faded out)");
            _cur = MusicTrack.None; _lastMusicVol = -1f;
            if (!Playing) return;
            try { MediaPlayer.Stop(); } catch (Exception ex) { Console.WriteLine($"[audio] Song stop failed: {ex.GetType().Name}: {ex.Message}"); }
        }

        public static void Shutdown()
        {
            if (!Playing) return;
            try { MediaPlayer.Stop(); for (int i = Voices.Count - 1; i >= 0; i--) Kill(i); } catch { }
            Playing = false;
        }
    }
}
