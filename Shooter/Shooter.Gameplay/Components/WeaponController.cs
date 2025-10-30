using System;
using System.Numerics;
using Shooter.Core;
using Shooter.Core.Components;
using Shooter.Core.Services;
using Shooter.Gameplay.Weapons;
using Shooter.Gameplay.Systems;

namespace Shooter.Gameplay.Components
{
    /// <summary>
    /// Component that manages weapon equipping, switching, and firing for an entity.
    /// Typically attached to the player entity.
    /// </summary>
    public class WeaponController : Core.Components.EntityComponent
    {
        // Dependencies
        private IInputService? _inputService;
        private Camera? _camera;
        private ProjectileSystem? _projectileSystem;

        // Weapon management
        private Weapon[] _weapons;
        private int _currentWeaponIndex = -1;
        public Weapon CurrentWeapon => _currentWeaponIndex >= 0 && _currentWeaponIndex < _weapons.Length 
            ? _weapons[_currentWeaponIndex] 
            : null;

        // Input tracking
        private bool _wasFireButtonDown = false;

        // Properties
        public int WeaponCount => _weapons?.Length ?? 0;
        public int CurrentWeaponIndex => _currentWeaponIndex;

        public WeaponController() : this(3)
        {
        }

        public WeaponController(int maxWeapons)
        {
            _weapons = new Weapon[maxWeapons];
        }

        public override void Initialize()
        {
            base.Initialize();

            // Get the InputService from ServiceLocator
            _inputService = ServiceLocator.Get<IInputService>();
            if (_inputService == null)
            {
                throw new InvalidOperationException("WeaponController requires InputService to be available.");
            }

            // Get the ProjectileSystem from ServiceLocator
            _projectileSystem = ServiceLocator.Get<ProjectileSystem>();
            if (_projectileSystem == null)
            {
                throw new InvalidOperationException("WeaponController requires ProjectileSystem to be available.");
            }

            // Get the camera component from the same entity
            _camera = Owner?.GetComponent<Camera>();
            if (_camera == null)
            {
                throw new InvalidOperationException("WeaponController requires a Camera component on the same entity.");
            }

            // Hook up weapon firing events
            foreach (var weapon in _weapons)
            {
                if (weapon is Gun gun)
                {
                    gun.OnHitscanFired += HandleHitscanFired;
                }
            }
        }

        public override void Update(Core.Components.GameTime gameTime)
        {
            base.Update(gameTime);

            // Update the current weapon
            CurrentWeapon?.Update(gameTime);

            // Handle weapon switching
            HandleWeaponSwitching();

            // Handle firing
            HandleFiring();

            // Handle reloading
            HandleReloading();
        }

        /// <summary>
        /// Handle weapon switching input (number keys 1-9)
        /// </summary>
        private void HandleWeaponSwitching()
        {
            for (int i = 0; i < Math.Min(_weapons.Length, 9); i++)
            {
                if (_inputService.IsKeyPressed((Microsoft.Xna.Framework.Input.Keys)(Microsoft.Xna.Framework.Input.Keys.D1 + i)))
                {
                    if (_weapons[i] != null && _currentWeaponIndex != i)
                    {
                        SwitchToWeapon(i);
                    }
                }
            }
        }

        /// <summary>
        /// Handle firing input
        /// </summary>
        private void HandleFiring()
        {
            if (CurrentWeapon == null)
                return;

            bool isFireButtonDown = _inputService.IsMouseButtonDown(MouseButton.Left);

            // Determine if we should fire based on firing mode
            bool shouldFire = false;

            if (CurrentWeapon is Gun gun)
            {
                if (gun.FiringMode == FiringMode.SemiAutomatic)
                {
                    // Semi-auto: only fire on button press (not held)
                    shouldFire = isFireButtonDown && !_wasFireButtonDown;
                }
                else if (gun.FiringMode == FiringMode.FullyAutomatic)
                {
                    // Full-auto: fire while button is held
                    shouldFire = isFireButtonDown;
                }
            }
            else
            {
                // Default behavior for other weapon types
                shouldFire = isFireButtonDown && !_wasFireButtonDown;
            }

            // Attempt to fire
            if (shouldFire)
            {
                // Get firing origin and direction from camera (already System.Numerics.Vector3)
                Vector3 origin = _camera.Position;
                Vector3 direction = _camera.Forward;

                CurrentWeapon.TryFire(origin, direction);
            }

            _wasFireButtonDown = isFireButtonDown;
        }

        /// <summary>
        /// Handle reload input
        /// </summary>
        private void HandleReloading()
        {
            if (CurrentWeapon == null)
                return;

            if (_inputService.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R))
            {
                CurrentWeapon.StartReload();
                if (CurrentWeapon.IsReloading)
                {
                    Console.WriteLine($"Reloading {CurrentWeapon.Name}...");
                }
            }
        }

        /// <summary>
        /// Equip a weapon in a specific slot
        /// </summary>
        public void EquipWeapon(Weapon weapon, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _weapons.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            _weapons[slotIndex] = weapon;

            // Hook up weapon events
            if (weapon is Gun gun && _projectileSystem != null)
            {
                gun.OnHitscanFired += HandleHitscanFired;
            }

            // If no weapon is currently selected, select this one
            if (_currentWeaponIndex < 0)
            {
                SwitchToWeapon(slotIndex);
            }

            Console.WriteLine($"Equipped {weapon.Name} in slot {slotIndex + 1}");
        }

        /// <summary>
        /// Switch to a specific weapon slot
        /// </summary>
        public void SwitchToWeapon(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _weapons.Length)
                return;

            if (_weapons[slotIndex] == null)
                return;

            // Cancel reload on current weapon if switching
            CurrentWeapon?.CancelReload();

            _currentWeaponIndex = slotIndex;
            Console.WriteLine($"Switched to {CurrentWeapon.Name}");
        }

        /// <summary>
        /// Get weapon in a specific slot
        /// </summary>
        public Weapon GetWeapon(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _weapons.Length)
                return null;

            return _weapons[slotIndex];
        }

        /// <summary>
        /// Remove weapon from a specific slot
        /// </summary>
        public void RemoveWeapon(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _weapons.Length)
                return;

            _weapons[slotIndex] = null;

            // If we removed the current weapon, try to switch to another
            if (_currentWeaponIndex == slotIndex)
            {
                _currentWeaponIndex = -1;
                
                // Try to find another weapon to switch to
                for (int i = 0; i < _weapons.Length; i++)
                {
                    if (_weapons[i] != null)
                    {
                        SwitchToWeapon(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Check if entity has a specific weapon
        /// </summary>
        public bool HasWeapon(Weapon weapon)
        {
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] == weapon)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Handle hitscan firing from guns
        /// </summary>
        private void HandleHitscanFired(Vector3 origin, Vector3 direction, float damage)
        {
            if (_projectileSystem == null)
                return;

            // Debug camera state when firing
            if (_camera != null)
            {
                Console.WriteLine($"[WeaponController] Camera - Pos:{_camera.Position}, Target:{_camera.Target}, Forward:{_camera.Forward}");
            }
            Console.WriteLine($"[WeaponController] Firing from {origin} in direction {direction}");

            // Fire the hitscan raycast through the projectile system
            // Pass Owner as the attacker for damage attribution
            var hit = _projectileSystem.FireHitscan(origin, direction, damage, CurrentWeapon?.Range ?? 1000f, Owner);
        }

        /// <summary>
        /// Cleanup when component is destroyed
        /// </summary>
        public override void OnDestroy()
        {
            // Unhook weapon events
            foreach (var weapon in _weapons)
            {
                if (weapon is Gun gun)
                {
                    gun.OnHitscanFired -= HandleHitscanFired;
                }
            }

            base.OnDestroy();
        }
    }
}
