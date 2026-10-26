using System;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace Shooter.Gameplay.Weapons
{
    public enum WeaponShootType { Manual, Automatic, Charge }

    /// <summary>WeaponController.CrosshairData: sprite (content name, no extension), size in canvas pixels, tint.</summary>
    public struct CrosshairData
    {
        public string Sprite;
        public int Size;
        public Vector4 Color;   // r, g, b, a (0..1, raw Unity values)

        public static CrosshairData From(JsonElement c, string field)
        {
            var d = new CrosshairData { Color = Vector4.One };
            if (c.ValueKind != JsonValueKind.Object || !c.TryGetProperty(field, out JsonElement e) || e.ValueKind != JsonValueKind.Object)
            {
                return d;
            }
            d.Sprite = JsonFields.AssetName(e, "CrosshairSprite");
            d.Size = JsonFields.Int(e, "CrosshairSize", 0);
            if (e.TryGetProperty("CrosshairColor", out JsonElement col) && col.ValueKind == JsonValueKind.Object)
            {
                d.Color = new Vector4(JsonFields.Float(col, "r", 1f), JsonFields.Float(col, "g", 1f), JsonFields.Float(col, "b", 1f), JsonFields.Float(col, "a", 1f));
            }
            return d;
        }
    }

    /// <summary>WeaponController Inspector values (Unity space for AimOffset; everything else unitless/seconds).</summary>
    public class WeaponSettings
    {
        public string WeaponName = "";
        public WeaponShootType ShootType;
        public float DelayBetweenShots = 0.5f;
        public float BulletSpreadAngle;
        public int BulletsPerShot = 1;
        public float RecoilForce = 1f;
        public float AimZoomRatio = 1f;
        public Vector3 AimOffsetUnity;
        public bool AutomaticReload = true;
        public float AmmoReloadRate = 1f;
        public float AmmoReloadDelay = 2f;
        public int MaxAmmo = 8;
        public bool AutomaticReleaseOnCharged;
        public float MaxChargeDuration = 2f;
        public float AmmoUsedOnStartCharge = 1f;
        public float AmmoUsageRateWhileCharging = 1f;
        public string WeaponIcon;   // content name of the HUD icon
        public string ShootSfx, ChangeWeaponSfx;   // content names
        public string MuzzleFlashPrefab;           // VFX prefab name (Tools/ParticleBake file)
        public bool UnparentMuzzleFlash;
        public CrosshairData CrosshairDefault, CrosshairTargetInSight;

        public static WeaponSettings From(JsonElement c)
        {
            var s = new WeaponSettings();
            if (c.ValueKind == JsonValueKind.Object && c.TryGetProperty("WeaponName", out JsonElement n) && n.ValueKind == JsonValueKind.String)
            {
                s.WeaponName = n.GetString();
            }
            s.ShootType = (WeaponShootType)JsonFields.Int(c, "ShootType", 0);
            s.DelayBetweenShots = JsonFields.Float(c, "DelayBetweenShots", s.DelayBetweenShots);
            s.BulletSpreadAngle = JsonFields.Float(c, "BulletSpreadAngle", s.BulletSpreadAngle);
            s.BulletsPerShot = JsonFields.Int(c, "BulletsPerShot", s.BulletsPerShot);
            s.RecoilForce = JsonFields.Float(c, "RecoilForce", s.RecoilForce);
            s.AimZoomRatio = JsonFields.Float(c, "AimZoomRatio", s.AimZoomRatio);
            s.AimOffsetUnity = JsonFields.Vector3(c, "AimOffset", Vector3.Zero);
            s.AutomaticReload = JsonFields.Bool(c, "AutomaticReload", s.AutomaticReload);
            s.AmmoReloadRate = JsonFields.Float(c, "AmmoReloadRate", s.AmmoReloadRate);
            s.AmmoReloadDelay = JsonFields.Float(c, "AmmoReloadDelay", s.AmmoReloadDelay);
            s.MaxAmmo = JsonFields.Int(c, "MaxAmmo", s.MaxAmmo);
            s.AutomaticReleaseOnCharged = JsonFields.Bool(c, "AutomaticReleaseOnCharged", false);
            s.MaxChargeDuration = JsonFields.Float(c, "MaxChargeDuration", s.MaxChargeDuration);
            s.AmmoUsedOnStartCharge = JsonFields.Float(c, "AmmoUsedOnStartCharge", s.AmmoUsedOnStartCharge);
            s.AmmoUsageRateWhileCharging = JsonFields.Float(c, "AmmoUsageRateWhileCharging", s.AmmoUsageRateWhileCharging);
            s.WeaponIcon = JsonFields.AssetName(c, "WeaponIcon");
            s.ShootSfx = JsonFields.AssetName(c, "ShootSfx");
            s.ChangeWeaponSfx = JsonFields.AssetName(c, "ChangeWeaponSfx");
            s.MuzzleFlashPrefab = JsonFields.AssetFileName(c, "MuzzleFlashPrefab");
            s.UnparentMuzzleFlash = JsonFields.Bool(c, "UnparentMuzzleFlash", false);
            s.CrosshairDefault = CrosshairData.From(c, "CrosshairDataDefault");
            s.CrosshairTargetInSight = CrosshairData.From(c, "CrosshairDataTargetInSight");
            return s;
        }
    }

    /// <summary>
    /// Port of Unity's WeaponController (Assets/FPS/Scripts/Game/Shared): ammo as a heat-like resource that refills over
    /// time, fire-rate gate, manual/automatic/charge shooting. Not ported: physical bullets and shells (shotgun only,
    /// SecondaryScene), manual reload animation, SFX, muzzle flash (the manager raises <see cref="Shot"/>).
    /// Time is passed in (seconds since start), like Unity's Time.time.
    /// </summary>
    public class WeaponController
    {
        private readonly WeaponSettings _s;
        private float _currentAmmo;
        private float _lastTimeShot = float.NegativeInfinity;
        private float _time;

        public WeaponController(WeaponSettings settings)
        {
            _s = settings;
            _currentAmmo = settings.MaxAmmo;
            CurrentAmmoRatio = 1f;
        }

        public WeaponSettings Settings { get { return _s; } }
        public bool IsCharging { get; private set; }
        public float CurrentAmmoRatio { get; private set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsCooling { get; private set; }
        public float CurrentCharge { get; private set; }
        public bool IsReloading { get { return false; } }   // manual reload animation not ported
        public float CurrentAmmo { get { return _currentAmmo; } }

        /// <summary>WeaponController.GetAmmoNeededToShoot(): ammo ratio one shot (or a charge start) costs.</summary>
        public float AmmoNeededToShoot
        {
            get { return (_s.ShootType != WeaponShootType.Charge ? 1f : Math.Min(1f, _s.AmmoUsedOnStartCharge)) / _s.MaxAmmo; }
        }

        /// <summary>Raised once per shot, after the bullets were requested; the manager spawns muzzle flash / audio from it.</summary>
        public event Action Shot;
        /// <summary>Asked once per bullet with the bullet index; the owner spawns the projectile. Argument: spread direction factory.</summary>
        public Action<int> SpawnBullet;

        /// <summary>Raised by <see cref="ShowWeapon"/> with the new state; the owner plays ChangeWeaponSfx when it is true.</summary>
        public event Action<bool> WeaponShown;

        public void ShowWeapon(bool show)
        {
            IsWeaponActive = show;
            if (WeaponShown != null) WeaponShown(show);
        }

        public void Update(float time, float dt)
        {
            _time = time;
            UpdateAmmo(dt);
            UpdateCharge(dt);
        }

        private void UpdateAmmo(float dt)
        {
            if (_s.AutomaticReload && _lastTimeShot + _s.AmmoReloadDelay < _time && _currentAmmo < _s.MaxAmmo && !IsCharging)
            {
                _currentAmmo += _s.AmmoReloadRate * dt;
                _currentAmmo = MathHelper.Clamp(_currentAmmo, 0f, _s.MaxAmmo);
                IsCooling = true;
            }
            else
            {
                IsCooling = false;
            }
            CurrentAmmoRatio = _currentAmmo / _s.MaxAmmo;
        }

        private void UpdateCharge(float dt)
        {
            if (!IsCharging || CurrentCharge >= 1f)
            {
                return;
            }
            float chargeLeft = 1f - CurrentCharge;
            float chargeAdded = _s.MaxChargeDuration <= 0f ? chargeLeft : (1f / _s.MaxChargeDuration) * dt;
            chargeAdded = MathHelper.Clamp(chargeAdded, 0f, chargeLeft);
            float ammoNeeded = chargeAdded * _s.AmmoUsageRateWhileCharging;
            if (ammoNeeded <= _currentAmmo)
            {
                UseAmmo(ammoNeeded);
                CurrentCharge = MathHelper.Clamp(CurrentCharge + chargeAdded, 0f, 1f);
            }
        }

        public void UseAmmo(float amount)
        {
            _currentAmmo = MathHelper.Clamp(_currentAmmo - amount, 0f, _s.MaxAmmo);
            _lastTimeShot = _time;
        }

        /// <summary>Returns true when a shot was fired this frame.</summary>
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            switch (_s.ShootType)
            {
                case WeaponShootType.Manual:
                    return inputDown && TryShoot();
                case WeaponShootType.Automatic:
                    return inputHeld && TryShoot();
                case WeaponShootType.Charge:
                    if (inputHeld)
                    {
                        TryBeginCharge();
                    }
                    if (inputUp || (_s.AutomaticReleaseOnCharged && CurrentCharge >= 1f))
                    {
                        return TryReleaseCharge();
                    }
                    return false;
                default:
                    return false;
            }
        }

        private bool TryShoot()
        {
            if (_currentAmmo >= 1f && _lastTimeShot + _s.DelayBetweenShots < _time)
            {
                HandleShoot();
                _currentAmmo -= 1f;
                return true;
            }
            return false;
        }

        private bool TryBeginCharge()
        {
            if (!IsCharging && _currentAmmo >= _s.AmmoUsedOnStartCharge &&
                (int)Math.Floor((_currentAmmo - _s.AmmoUsedOnStartCharge) * _s.BulletsPerShot) > 0 &&
                _lastTimeShot + _s.DelayBetweenShots < _time)
            {
                UseAmmo(_s.AmmoUsedOnStartCharge);
                IsCharging = true;
                return true;
            }
            return false;
        }

        private bool TryReleaseCharge()
        {
            if (!IsCharging)
            {
                return false;
            }
            HandleShoot();
            CurrentCharge = 0f;
            IsCharging = false;
            return true;
        }

        private void HandleShoot()
        {
            int bullets = _s.ShootType == WeaponShootType.Charge ? (int)Math.Ceiling(CurrentCharge * _s.BulletsPerShot) : _s.BulletsPerShot;
            for (int i = 0; i < bullets; i++)
            {
                if (SpawnBullet != null) SpawnBullet(i);
            }
            _lastTimeShot = _time;
            if (Shot != null) Shot();
        }

        /// <summary>Vector3.Slerp(forward, random point in the unit sphere, spread / 180) like Unity.</summary>
        public Vector3 GetShotDirectionWithinSpread(Vector3 forward, Random rng)
        {
            float ratio = _s.BulletSpreadAngle / 180f;
            if (ratio <= 0f)
            {
                return forward;
            }
            Vector3 r;
            do
            {
                r = new Vector3((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1));
            } while (r.LengthSquared() > 1f);
            return Slerp(forward, r, ratio);
        }

        // Unity Vector3.Slerp semantics: interpolates direction and magnitude
        private static Vector3 Slerp(Vector3 a, Vector3 b, float t)
        {
            float la = a.Length(), lb = b.Length();
            if (la < 1e-6f || lb < 1e-6f)
            {
                return Vector3.Lerp(a, b, t);
            }
            Vector3 na = a / la, nb = b / lb;
            float dot = MathHelper.Clamp(Vector3.Dot(na, nb), -1f, 1f);
            float theta = (float)Math.Acos(dot);
            float mag = MathHelper.Lerp(la, lb, t);
            if (theta < 1e-4f)
            {
                return Vector3.Normalize(Vector3.Lerp(na, nb, t)) * mag;
            }
            float sin = (float)Math.Sin(theta);
            Vector3 d = na * ((float)Math.Sin((1f - t) * theta) / sin) + nb * ((float)Math.Sin(t * theta) / sin);
            return d * mag;
        }
    }
}
