using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Shooter.Core.Entities;
using Shooter.Core.Services;
using Shooter.Gameplay.Components;

namespace Shooter.Gameplay.Systems;

/// <summary>
/// System for managing pickups and automatic collection.
/// Checks proximity between player and pickups each frame.
/// </summary>
public class PickupSystem
{
    private Entity? _player;

    public PickupSystem(Entity? player)
    {
        _player = player;
    }

    /// <summary>
    /// Update the pickup system - check for auto-collection
    /// </summary>
    public void Update(Core.Components.GameTime gameTime, IEnumerable<Pickup> pickups)
    {
        if (_player == null)
            return;

        var playerTransform = _player.GetComponent<Core.Components.Transform3D>();
        if (playerTransform == null)
            return;

        Vector3 playerPosition = playerTransform.Position;

        // Check each pickup for proximity
        foreach (var pickup in pickups)
        {
            if (pickup.IsCollected)
                continue;

            if (pickup.IsInRange(playerPosition))
            {
                CollectPickup(pickup, _player);
            }
        }
    }

    /// <summary>
    /// Collect a pickup and apply its effects to the player
    /// </summary>
    private void CollectPickup(Pickup pickup, Entity player)
    {
        switch (pickup.Type)
        {
            case PickupType.Health:
                ApplyHealthPickup(pickup, player);
                break;

            case PickupType.Ammo:
                ApplyAmmoPickup(pickup, player);
                break;

            case PickupType.Armor:
                // TODO: Implement armor system
                Console.WriteLine($"[PickupSystem] Armor pickup not yet implemented");
                break;

            case PickupType.PowerUp:
                // TODO: Implement power-up system
                Console.WriteLine($"[PickupSystem] Power-up pickup not yet implemented");
                break;
        }

        // Trigger the pickup's collection
        pickup.TryCollect(player);
    }

    /// <summary>
    /// Apply health pickup to player
    /// </summary>
    private void ApplyHealthPickup(Pickup pickup, Entity player)
    {
        var health = player.GetComponent<Health>();
        if (health != null && health.CurrentHealth < health.MaxHealth)
        {
            health.Heal(pickup.Value);
            Console.WriteLine($"[PickupSystem] Player healed for {pickup.Value} HP");

            // Play pickup sound
            var audioService = ServiceLocator.Get<IAudioService>();
            audioService?.PlaySound("Audio/SFX/Pickups/Pickup_Health", null, 0.6f);
        }
    }

    /// <summary>
    /// Apply ammo pickup to player
    /// </summary>
    private void ApplyAmmoPickup(Pickup pickup, Entity player)
    {
        var weaponController = player.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            var currentWeapon = weaponController.CurrentWeapon;
            if (currentWeapon != null)
            {
                int ammoToAdd = (int)pickup.Value;
                currentWeapon.AddAmmo(ammoToAdd);
                Console.WriteLine($"[PickupSystem] Player gained {ammoToAdd} reserve ammo");

                // Play pickup sound
                var audioService = ServiceLocator.Get<IAudioService>();
                audioService?.PlaySound("Audio/SFX/Pickups/Pickup_Weapon_Small", null, 0.6f);
            }
        }
    }
}
