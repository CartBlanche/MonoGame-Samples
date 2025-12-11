using Shooter.Core.Components;
using Shooter.Core.Entities;
using Shooter.Gameplay.Weapons;
using System.Numerics;

namespace Shooter.Gameplay.Components;

/// <summary>
/// Enemy controller component that manages enemy weapons and combat.
/// Similar to Unity's EnemyController that handles weapon management.
///
/// UNITY COMPARISON:
/// - Unity: EnemyController manages weapons array, handles shooting/aiming
/// - MonoGame: Similar approach with weapon reference and combat methods
/// </summary>
public class EnemyController : EntityComponent
{
    private Weapon? _weapon;
    private Transform3D? _transform;

    // Orientation speed for aiming
    private float _orientationSpeed = 10f;

    /// <summary>
    /// Speed at which enemy orients toward targets (degrees per second)
    /// </summary>
    public float OrientationSpeed
    {
        get => _orientationSpeed;
        set => _orientationSpeed = value;
    }

    /// <summary>
    /// Event fired when enemy attacks
    /// </summary>
    public event Action? OnAttack;

    /// <summary>
    /// Set the weapon for this enemy
    /// </summary>
    public void SetWeapon(Weapon weapon)
    {
        _weapon = weapon;
    }

    /// <summary>
    /// Initialize component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        _transform = Owner?.GetComponent<Transform3D>();
    }

    /// <summary>
    /// Get the current weapon
    /// </summary>
    public Weapon? GetCurrentWeapon()
    {
        return _weapon;
    }

    /// <summary>
    /// Attempt to attack the target position.
    /// Orients weapon toward target and fires if possible.
    /// Returns true if weapon was fired.
    /// </summary>
    public bool TryAttack(Vector3 targetPosition)
    {
        if (_weapon == null || _transform == null)
            return false;

        // Orient toward target
        OrientTowards(targetPosition);

        // Calculate firing direction
        Vector3 direction = Vector3.Normalize(targetPosition - _transform.Position);

        // Offset origin up for weapon height and forward to avoid hitting self
        // Same offset as line-of-sight check (1.5 units forward)
        Vector3 origin = _transform.Position + new Vector3(0, 0.5f, 0) + (direction * 1.5f);

        if (_weapon.TryFire(origin, direction))
        {
            OnAttack?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Orient the enemy's body toward a target position.
    /// Uses smooth rotation based on OrientationSpeed.
    /// </summary>
    public void OrientTowards(Vector3 lookPosition)
    {
        if (_transform == null)
            return;

        // Calculate direction (horizontal plane only)
        Vector3 lookDirection = lookPosition - _transform.Position;
        lookDirection.Y = 0;

        if (lookDirection.LengthSquared() < 0.001f)
            return;

        lookDirection = Vector3.Normalize(lookDirection);

        // Calculate target rotation
        float targetYaw = MathF.Atan2(lookDirection.X, lookDirection.Z);
        Quaternion targetRotation = Quaternion.CreateFromYawPitchRoll(targetYaw, 0, 0);

        // Smooth rotation (using Update's delta time would be better, but for now instant)
        // TODO: Add deltaTime parameter for smooth interpolation
        _transform.Rotation = targetRotation;
    }

    /// <summary>
    /// Orient weapons toward a target position.
    /// In MonoGame, we don't have separate weapon transforms like Unity,
    /// so this currently just ensures proper aiming direction.
    /// </summary>
    public void OrientWeaponsTowards(Vector3 lookPosition)
    {
        // In Unity, this would rotate weapon transforms independently
        // For our simplified system, the weapon fires from enemy position
        // in the direction they're facing, so we just orient the body
        OrientTowards(lookPosition);
    }

    /// <summary>
    /// Update weapon state
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Update weapon timers
        _weapon?.Update(gameTime);
    }
}
