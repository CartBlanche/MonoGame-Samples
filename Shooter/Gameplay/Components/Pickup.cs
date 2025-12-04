using System;
using System.Numerics;
using Shooter.Core.Components;

namespace Shooter.Gameplay.Components;

/// <summary>
/// Pickup component for collectible items (health, ammo, power-ups).
/// Similar to pickup/collectible systems in Unity FPS games.
///
/// USAGE:
/// - Attach to entities that can be collected
/// - Configure PickupType and value
/// - Subscribe to OnPickedUp event for feedback (sound, particles)
/// - Physics trigger detection handled automatically
/// </summary>
public class Pickup : EntityComponent
{
    /// <summary>
    /// Type of pickup
    /// </summary>
    public PickupType Type { get; set; } = PickupType.Health;

    /// <summary>
    /// Value/amount of the pickup (health restored, ammo added, etc.)
    /// </summary>
    public float Value { get; set; } = 25f;

    /// <summary>
    /// Whether this pickup has been collected
    /// </summary>
    public bool IsCollected { get; private set; } = false;

    /// <summary>
    /// Should this pickup rotate for visual effect?
    /// </summary>
    public bool RotatePickup { get; set; } = true;

    /// <summary>
    /// Rotation speed in radians per second
    /// </summary>
    public float RotationSpeed { get; set; } = 2.0f;

    /// <summary>
    /// Should this pickup bob up and down?
    /// </summary>
    public bool BobPickup { get; set; } = true;

    /// <summary>
    /// Bob amplitude (how far up/down)
    /// </summary>
    public float BobAmplitude { get; set; } = 0.2f;

    /// <summary>
    /// Bob frequency (how fast)
    /// </summary>
    public float BobFrequency { get; set; } = 2.0f;

    /// <summary>
    /// Auto-pickup radius (distance at which player auto-collects)
    /// </summary>
    public float PickupRadius { get; set; } = 1.5f;

    // State
    private float _rotationAngle = 0f;
    private float _bobTimer = 0f;
    private Vector3 _originalPosition;

    /// <summary>
    /// Event fired when this pickup is collected. Parameters: (pickupType, value, collector)
    /// </summary>
    public event Action<PickupType, float, Core.Entities.Entity>? OnPickedUp;

    public override void Initialize()
    {
        base.Initialize();

        // Store original position for bobbing effect
        var transform = Owner?.GetComponent<Transform3D>();
        if (transform != null)
        {
            _originalPosition = transform.Position;
        }
    }

    public override void Update(Core.Components.GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsCollected)
            return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var transform = Owner?.GetComponent<Transform3D>();
        if (transform == null)
            return;

        // Rotate pickup
        if (RotatePickup)
        {
            _rotationAngle += RotationSpeed * deltaTime;
            transform.LocalRotation = Quaternion.CreateFromAxisAngle(
                Vector3.UnitY,
                _rotationAngle
            );
        }

        // Bob up and down
        if (BobPickup)
        {
            _bobTimer += deltaTime * BobFrequency;
            float bobOffset = MathF.Sin(_bobTimer) * BobAmplitude;
            transform.Position = new Vector3(
                _originalPosition.X,
                _originalPosition.Y + bobOffset,
                _originalPosition.Z
            );
        }
    }

    /// <summary>
    /// Attempt to collect this pickup
    /// </summary>
    /// <param name="collector">The entity collecting this pickup</param>
    /// <returns>True if successfully collected</returns>
    public bool TryCollect(Core.Entities.Entity collector)
    {
        if (IsCollected)
            return false;

        IsCollected = true;

        Console.WriteLine($"[Pickup] {collector.Name} collected {Type} (+{Value})");

        // Trigger event
        OnPickedUp?.Invoke(Type, Value, collector);

        // Hide the pickup
        if (Owner != null)
        {
            Owner.Active = false;
        }

        return true;
    }

    /// <summary>
    /// Check if an entity is close enough to auto-collect
    /// </summary>
    public bool IsInRange(Vector3 position)
    {
        var transform = Owner?.GetComponent<Transform3D>();
        if (transform == null)
            return false;

        float distance = Vector3.Distance(transform.Position, position);
        return distance <= PickupRadius;
    }
}

/// <summary>
/// Types of pickups available
/// </summary>
public enum PickupType
{
    Health,
    Ammo,
    Armor,
    PowerUp
}
