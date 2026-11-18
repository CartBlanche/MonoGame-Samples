using System;
using System.Collections.Generic;

namespace ThirdPersonShooter
{
    public enum AudioEvent { PlayerStep, PlayerJump, PlayerLand, PlayerShoot, BulletExplode, RobotCharge, RobotShoot, RobotHit, RobotExplosion, RobotWalk }
    public enum MusicTrack { None, Menu, Level }

    /// <summary>One sound: content asset, 2D/3D, node volume_db, unit_size (3D), pitch scale, exclusive channel (same Godot AudioStreamPlayer node: a new play replaces the old) and the Godot source it was read from.</summary>
    public sealed class SoundDef
    {
        public AudioEvent Event; public string Asset; public bool Is3D; public float VolumeDb, UnitSize, PitchScale = 1f; public string Channel; public string Source;
        public string ContentPath => "Audio/Sfx/" + Asset;
        /// <summary>3D sounds are copied raw into the content (Audio/Sfx/NAME.wav) and downmixed to MONO at load (OpenAL pans only mono buffers); 2D sounds stay SoundEffect xnb (stereo as authored).</summary>
        public string FilePath => Is3D ? ContentPath + ".wav" : ContentPath + ".xnb";
    }

    /// <summary>Event -> sound mapping (data only; shared by the runtime and `--test-audio`).</summary>
    public static class AudioTable
    {
        public const string MenuMusicAsset = "Audio/Music/menu_music", LevelMusicAsset = "Audio/Music/level_music";
        public const float MusicFadeSeconds = 0.5f;   // plan 05 crossfade 0.5 s (Godot swaps the scene abruptly)

        public static readonly SoundDef[] All =
        {
            // 2D AudioStreamPlayer nodes of player.tscn (Master bus, volume_db 0)
            new SoundDef { Event = AudioEvent.PlayerStep,   Asset = "player_step",   Channel = "p.step",  Source = "player.tscn:666 Step = step_random_pitch.tres (randomizer defaults: no pitch/volume variation); NEVER played in the 4.7 demo (no script, no method track): port addition" },
            new SoundDef { Event = AudioEvent.PlayerJump,   Asset = "player_jump",   Channel = "p.jump",  Source = "player.tscn:669, player.gd:182 jump()" },
            new SoundDef { Event = AudioEvent.PlayerLand,   Asset = "player_land",   Channel = "p.land",  Source = "player.tscn:672, player.gd:188 land()" },
            new SoundDef { Event = AudioEvent.PlayerShoot,  Asset = "player_shoot",  Channel = "p.shoot", Source = "player.tscn:675 (AudioStreamRandomizer, 1 stream), player.gd:200 shoot()" },
            // 3D: bullet.tscn ExplosionAudio, played by the 'explode' animation value track at 0.01 s
            new SoundDef { Event = AudioEvent.BulletExplode, Asset = "bullet_explode", Is3D = true, UnitSize = 20f, Source = "bullet.tscn:512-514 unit_size 20, track 4 'ExplosionAudio:playing' at 0.01 s" },
            // 3D: red_robot.tscn SoundEffects (Node3D under the robot root)
            new SoundDef { Event = AudioEvent.RobotCharge,    Asset = "robot_charge",    Is3D = true, UnitSize = 15f, Channel = "r.cannon", Source = "red_robot.tscn:11029 Cannon unit_size 15; shoot anim audio track 2 key 0.0 s (charge.wav)" },
            new SoundDef { Event = AudioEvent.RobotShoot,     Asset = "robot_shoot",     Is3D = true, UnitSize = 15f, Channel = "r.cannon", Source = "red_robot.tscn:11029; shoot anim audio track 2 key 2.05 s (shoot.wav)" },
            new SoundDef { Event = AudioEvent.RobotExplosion, Asset = "robot_explosion", Is3D = true, UnitSize = 15f, VolumeDb = 12f, Channel = "r.explosion", Source = "red_robot.tscn:11032 volume_db 12 unit_size 15; red_robot.gd:100 explosion_sound.play()" },
            new SoundDef { Event = AudioEvent.RobotHit,       Asset = "robot_hit",       Is3D = true, UnitSize = 15f, VolumeDb = 3f, Channel = "r.hit", Source = "red_robot.tscn:11037 volume_db 3 unit_size 15 (AudioStreamRandomizer, 1 stream); red_robot.gd:84 hit_sound.play()" },
            new SoundDef { Event = AudioEvent.RobotWalk,      Asset = "robot_walk",      Is3D = true, UnitSize = 15f, VolumeDb = 3f, Channel = "r.walk", Source = "red_robot.tscn:11042 volume_db 3 unit_size 15; Walk-cycle audio track 67 keys 0.8 s and 2.0333 s (use_blend: volume x clip weight)" },
        };

        /// <summary>default_bus_layout.tres: Outside = AudioEffectReverb id 1 (room_size 0.98, hipass 0.42, wet 0.29), Reactor = id 2 (room_size 0.74, hipass 0.14, wet 0.56); other fields are the Godot defaults (damping 0.5, dry 1, predelay 150 ms, feedback 0.4). Master = no effect.</summary>
        public static ReverbSettings ReverbFor(AudioBus b) => b switch
        {
            AudioBus.Outside => new ReverbSettings { RoomSize = 0.98f, Hipass = 0.42f, Wet = 0.29f },
            AudioBus.Reactor => new ReverbSettings { RoomSize = 0.74f, Hipass = 0.14f, Wet = 0.56f },
            _ => null,
        };

        static Dictionary<AudioEvent, SoundDef> _byEvent;
        public static SoundDef Get(AudioEvent e)
        {
            if (_byEvent == null) { var d = new Dictionary<AudioEvent, SoundDef>(); foreach (var s in All) d[s.Event] = s; _byEvent = d; }
            return _byEvent[e];
        }

        /// <summary>Walk-cycle audio keys of the robot (red_robot.tscn tracks/67/keys times) and the clip's loop length.</summary>
        public static readonly float[] RobotWalkKeys = { 0.8f, 2.0333333f };
        public const float RobotWalkCycle = 2.5f;       // Walk-cycle length
        public const float RobotCannonChargeKey = 0f, RobotCannonShootKey = 2.05f;
    }
}
