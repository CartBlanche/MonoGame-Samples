using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Shooter.Audio;
using Shooter.Rendering.Particles;
using Shooter.Scene;

namespace Shooter.Gameplay.Effects
{
    /// <summary>
    /// What the Jetpack looks and sounds like: while it thrusts the two trail emitters on the player (VFX_JetpackTrail_left / _right under Player/Capsule,
    /// Jetpack.JetpackVfx) emit and the looping JetpackSfx (a 2D AudioSource on the player) plays; otherwise the emission is off (existing particles live on)
    /// and the sound stops. The emitters follow the player's position and yaw.
    /// </summary>
    public sealed class JetpackEffects : IDisposable
    {
        private readonly Jetpack _jetpack;
        private readonly PlayerController _player;
        private readonly ParticleManager _particles;
        private readonly List<ParticleSystemInstance> _trails = new List<ParticleSystemInstance>();
        private readonly List<Matrix> _trailLocal = new List<Matrix>();   // prefab space: the player root at the origin, not yawed
        private readonly SfxPlayer.SfxInstance _sound;

        public IReadOnlyList<ParticleSystemInstance> Trails { get { return _trails; } }
        public SfxPlayer.SfxInstance Sound { get { return _sound; } }

        public JetpackEffects(Jetpack jetpack, PlayerController player, VfxLibrary vfx, ParticleManager particles, SfxPlayer sfx)
        {
            _jetpack = jetpack;
            _player = player;
            _particles = particles;
            if (vfx != null)
            {
                foreach (ParticleSystemData.Placement p in vfx.Placements("Player"))
                {
                    if (!p.Active || !p.Data.Name.StartsWith("VFX_JetpackTrail", StringComparison.Ordinal)) continue;
                    ParticleSystemInstance ps = vfx.AddSystem(p, p.World * PlayerRoot());
                    ps.SetEmissionEnabled(false);   // Jetpack.Update turns the emission off whenever it is not in use
                    _trails.Add(ps);
                    _trailLocal.Add(p.World);
                }
            }
            if (sfx != null && !string.IsNullOrEmpty(jetpack.Settings.SfxName))
            {
                _sound = sfx.Create(jetpack.Settings.SfxName);
                _sound.Loop = true;
                _sound.Volume = 1f;
            }
        }

        public void Dispose()
        {
            foreach (ParticleSystemInstance ps in _trails) _particles.Remove(ps);
            _trails.Clear();
            _trailLocal.Clear();
            if (_sound != null) _sound.Stop();
        }

        /// <summary>The player root: translated to the feet position and turned with the yaw (clockwise from above in Unity, so negative in the mirrored world).</summary>
        private Matrix PlayerRoot()
        {
            return Matrix.CreateRotationY(-MathHelper.ToRadians(_player.YawDegrees)) * Matrix.CreateTranslation(_player.Position);
        }

        /// <summary>After <see cref="Jetpack.Update"/>.</summary>
        public void Update()
        {
            Matrix root = PlayerRoot();
            for (int i = 0; i < _trails.Count; i++)
            {
                _trails[i].SetWorld(_trailLocal[i] * root);
                _trails[i].SetEmissionEnabled(_jetpack.IsInUse);
            }
            if (_sound != null)
            {
                if (_jetpack.IsInUse && !_sound.IsPlaying) _sound.Play();
                else if (!_jetpack.IsInUse && _sound.IsPlaying) _sound.Stop();
            }
        }
    }
}
