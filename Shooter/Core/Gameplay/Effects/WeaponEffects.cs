using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Audio;
using Shooter.Gameplay.Weapons;
using Shooter.Rendering.Particles;
using Shooter.Scene;

namespace Shooter.Gameplay.Effects
{
    /// <summary>
    /// Presentation of the weapon: muzzle flash, impact VFX and sounds, driven by the same events Unity's scripts use.
    ///   WeaponController.HandleShoot: muzzle flash prefab instantiated at the muzzle, parented to it (kept 2 s), then ShootSfx one-shot.
    ///   WeaponController.ShowWeapon: ChangeWeaponSfx one-shot when the weapon is shown.
    ///   ProjectileStandard.OnHit: ImpactVfx at point + normal * ImpactVfxSpawnOffset, rotation LookRotation(normal), destroyed after ImpactVfxLifetime.
    ///   OverheatBehavior.Update: the cooling sound plays while the ammo refills (volume from AmmoToVolumeRatioCurve of 1 - ammo ratio).
    /// Not here: the overheat gem colour, steam and fuel cells (the rest of OverheatBehavior / WeaponFuelCellHandler).
    /// </summary>
    public sealed class WeaponEffects : IDisposable
    {
        private const float MuzzleFlashLifetime = 2f;   // Destroy(muzzleFlashInstance, 2f)

        private readonly PlayerController _player;
        private readonly PlayerWeaponsManager _weapons;
        private readonly ProjectileManager _projectiles;
        private readonly VfxLibrary _vfx;
        private readonly SfxPlayer _sfx;
        private readonly ParticleManager _particles;
        private readonly List<VfxLibrary.Instance> _live = new List<VfxLibrary.Instance>();
        private readonly List<VfxLibrary.Instance> _followMuzzle = new List<VfxLibrary.Instance>();

        private WeaponController _weapon;
        private readonly float _shootVolume;
        private readonly string _coolingSound;
        private readonly AnimCurve _coolingVolume;
        private SfxPlayer.SfxInstance _cooling;

        public int MuzzleFlashesSpawned { get; private set; }
        public int ImpactEffectsSpawned { get; private set; }
        public int LiveEffects { get { return _live.Count; } }
        public SfxPlayer.SfxInstance Cooling { get { return _cooling; } }

        public WeaponEffects(SceneFile scene, PlayerController player, PlayerWeaponsManager weapons, ProjectileManager projectiles, VfxLibrary vfx, ParticleManager particles, SfxPlayer sfx)
        {
            _player = player;
            _weapons = weapons;
            _projectiles = projectiles;
            _vfx = vfx;
            _particles = particles;
            _sfx = sfx;

            // AudioSource on the weapon: volume of every PlayOneShot (shot, weapon change)
            _shootVolume = 1f;
            if (scene.Weapon.Components != null && scene.Weapon.Components.TryGetValue("AudioSource", out JsonElement audio))
            {
                _shootVolume = JsonFields.Float(audio, "m_Volume", 1f);
            }
            _coolingVolume = AnimCurve.Constant(1f);
            if (scene.Weapon.Components != null && scene.Weapon.Components.TryGetValue("OverheatBehavior", out JsonElement oh))
            {
                _coolingSound = JsonFields.AssetName(oh, "CoolingCellsSound");
                if (oh.ValueKind == JsonValueKind.Object && oh.TryGetProperty("AmmoToVolumeRatioCurve", out JsonElement curve))
                {
                    _coolingVolume = AnimCurve.FromJson(curve);
                }
            }
            if (_coolingSound != null)
            {
                _cooling = _sfx.Create(_coolingSound);
            }

            _weapons.ShotFired += OnShot;
            _weapons.SwitchedToWeapon += OnWeaponChanged;
            _projectiles.Impact += OnImpact;
            Hook(_weapons.ActiveWeapon);
            // the manager's constructor already raised the first ShowWeapon(true) (Start -> SwitchWeapon), before anyone could listen
            if (_weapon != null && _weapon.IsWeaponActive) OnWeaponShown(true);
        }

        public void Dispose()
        {
            _weapons.ShotFired -= OnShot;
            _weapons.SwitchedToWeapon -= OnWeaponChanged;
            _projectiles.Impact -= OnImpact;
            Hook(null);
            if (_cooling != null) _cooling.Stop();
            foreach (VfxLibrary.Instance i in _live) _vfx.Destroy(i);
            _live.Clear();
            _followMuzzle.Clear();
        }

        // ChangeWeaponSfx is played from ShowWeapon(true) on the weapon that is being shown
        private void Hook(WeaponController w)
        {
            if (_weapon != null) _weapon.WeaponShown -= OnWeaponShown;
            _weapon = w;
            if (_weapon != null) _weapon.WeaponShown += OnWeaponShown;
        }

        private void OnWeaponChanged(WeaponController w) { Hook(w); }

        private void OnWeaponShown(bool show)
        {
            if (show && _weapon != null && _weapon.Settings.ChangeWeaponSfx != null)
            {
                _sfx.PlayOneShot(_weapon.Settings.ChangeWeaponSfx, _shootVolume);
            }
        }

        /// <summary>The muzzle as a transform: the camera's orientation (the weapon has none of its own) at the muzzle position.</summary>
        /// <summary>Gem colour, steam and fuel cells of the weapon (driven from <see cref="Update"/>); null = none.</summary>
        public OverheatVisuals Overheat;
        private float? _ratioOverride;     // --shot previews pin the ammo ratio
        private Matrix? _cameraOverride;   // --shot previews place the muzzle from a pose camera instead of the player

        private Matrix MuzzleMatrix()
        {
            Matrix cam = _cameraOverride ?? _player.CameraWorld;
            cam.Translation = _weapons.MuzzleWorld(cam);
            return cam;
        }

        private void OnShot(Vector3 muzzle, Vector3 direction)
        {
            WeaponController w = _weapons.ActiveWeapon;
            if (w == null) return;
            WeaponSettings s = w.Settings;
            if (s.MuzzleFlashPrefab != null)
            {
                VfxLibrary.Instance flash = _vfx.Spawn(s.MuzzleFlashPrefab, MuzzleMatrix(), MuzzleFlashLifetime);
                flash.SetWeaponLayer(true, true);   // seen in front of the viewmodel: the weapon camera, no depth test against the viewmodel (the Unity capture shows the rays over the barrel)
                _live.Add(flash);
                if (!s.UnparentMuzzleFlash) _followMuzzle.Add(flash);   // parented to WeaponMuzzle
                MuzzleFlashesSpawned++;
            }
            if (s.ShootSfx != null && s.ShootType != WeaponShootType.Charge)
            {
                _sfx.PlayOneShot(s.ShootSfx, _shootVolume);
            }
        }

        private void OnImpact(ProjectileImpact hit)
        {
            ProjectileSettings p = hit.Settings;
            if (p == null) return;
            if (p.ImpactVfx != null)
            {
                Vector3 normal = hit.Normal;
                Vector3 up = Math.Abs(Vector3.Dot(normal, Vector3.Up)) > 0.999f ? Vector3.Forward : Vector3.Up;
                Matrix root = Matrix.CreateWorld(hit.Point + normal * p.ImpactVfxSpawnOffset, normal, up);   // LookRotation(normal)
                _live.Add(_vfx.Spawn(p.ImpactVfx, root, p.ImpactVfxLifetime));
                ImpactEffectsSpawned++;
            }
            if (p.ImpactSfx != null)
            {
                _sfx.PlayOneShot(p.ImpactSfx, 1f);   // AudioUtility.CreateSFX at the hit point; none for the Blaster projectile
            }
        }

        /// <summary>Shot harness: a muzzle flash and a Blaster impact as if fired from <paramref name="cameraWorld"/>, aged by <paramref name="seconds"/>.</summary>
        public void Preview(Matrix cameraWorld, Vector3 impactPoint, Vector3 impactNormal, float seconds, float? ammoRatio = null)
        {
            _cameraOverride = cameraWorld;
            _ratioOverride = ammoRatio;
            foreach (VfxLibrary.Instance i in _live) _vfx.Destroy(i);
            _live.Clear();
            _followMuzzle.Clear();
            OnShot(Vector3.Zero, Vector3.Zero);
            OnImpact(new ProjectileImpact { Point = impactPoint, Normal = impactNormal, Settings = _weapons.ProjectileSettings });
            for (float t = 0f; t < seconds; t += 0.01f)
            {
                Update(0.01f);
            }
            _cameraOverride = null;
            _ratioOverride = null;
        }

        public void Update(float dt)
        {
            if (_followMuzzle.Count > 0)
            {
                Matrix muzzle = MuzzleMatrix();
                foreach (VfxLibrary.Instance i in _followMuzzle) i.SetRoot(muzzle);
            }
            for (int k = _live.Count - 1; k >= 0; k--)
            {
                VfxLibrary.Instance i = _live[k];
                i.Age += dt;
                if (i.Age >= i.Lifetime)
                {
                    _vfx.Destroy(i);
                    _followMuzzle.Remove(i);
                    _live.RemoveAt(k);
                }
            }
            if (Overheat != null && _weapons.ActiveWeapon != null)
            {
                Matrix cam = _cameraOverride ?? _player.CameraWorld;
                Overheat.Update(_ratioOverride ?? _weapons.ActiveWeapon.CurrentAmmoRatio, Matrix.CreateTranslation(_weapons.SocketLocal) * cam);
            }
            _particles.Update(dt);

            // OverheatBehavior.Update (sound part)
            WeaponController w = _weapons.ActiveWeapon;
            if (_cooling != null && w != null)
            {
                float ratio = w.CurrentAmmoRatio;
                if (!_cooling.IsPlaying && ratio != 1f && w.IsWeaponActive && w.IsCooling)
                {
                    _cooling.Volume = _coolingVolume.Evaluate(1f - ratio);
                    _cooling.Play();
                }
                else if (_cooling.IsPlaying && (ratio == 1f || !w.IsWeaponActive || !w.IsCooling))
                {
                    _cooling.Stop();
                    return;
                }
                _cooling.Volume = _coolingVolume.Evaluate(1f - ratio);
                _cooling.ApplyVolume();
            }
        }
    }
}
