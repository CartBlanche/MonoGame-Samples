using Shooter.Core.Components;
using Shooter.Core.Entities;

namespace Shooter.Gameplay.Components;

/// <summary>
/// Navigation module that stores movement parameters for AI-controlled entities.
/// Based on Unity's NavigationModule pattern from the FPS sample.
///
/// UNITY COMPARISON:
/// Unity: NavigationModule as configuration component for NavMeshAgent
/// MonoGame: NavigationModule as configuration for manual movement system
/// </summary>
public class NavigationModule : EntityComponent
{
    /// <summary>
    /// Maximum movement speed in units per second.
    /// Unity default: varies by enemy type (HoverBot: 5.0)
    /// </summary>
    public float MoveSpeed { get; set; } = 5.0f;

    /// <summary>
    /// Maximum rotation speed in degrees per second.
    /// Unity default: 120 degrees/sec
    /// </summary>
    public float AngularSpeed { get; set; } = 120f;

    /// <summary>
    /// How quickly the entity accelerates to max speed (units per second squared).
    /// Unity default: 50.0
    /// </summary>
    public float Acceleration { get; set; } = 50.0f;

    /// <summary>
    /// Current velocity (managed by movement system).
    /// Unity equivalent: NavMeshAgent.velocity
    /// </summary>
    public float CurrentSpeed { get; set; } = 0f;

    /// <summary>
    /// Distance at which entity considers it has reached its destination.
    /// Unity default: 2.0 units
    /// </summary>
    public float PathReachingRadius { get; set; } = 2.0f;

    /// <summary>
    /// Apply acceleration to current speed, clamped to MoveSpeed.
    /// </summary>
    public void UpdateSpeed(float deltaTime, bool isMoving)
    {
        if (isMoving)
        {
            // Accelerate toward max speed
            CurrentSpeed += Acceleration * deltaTime;
            CurrentSpeed = Math.Min(CurrentSpeed, MoveSpeed);
        }
        else
        {
            // Decelerate to stop
            CurrentSpeed -= Acceleration * deltaTime * 2f; // Decelerate faster than accelerate
            CurrentSpeed = Math.Max(CurrentSpeed, 0f);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        CurrentSpeed = 0f;
    }
}
