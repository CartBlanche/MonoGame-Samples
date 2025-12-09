using System.Numerics;

namespace Shooter.Gameplay.AI;

/// <summary>
/// Steering behaviors for AI movement.
/// Adapted from the ChaseAndEvade sample and extended for 3D.
///
/// PATTERN: Steering Behaviors (Boids, Reynolds)
/// Common AI movement patterns like seek, flee, wander, pursuit, evade.
///
/// UNITY COMPARISON:
/// Unity: Often uses NavMeshAgent for automatic steering
/// MonoGame: Manual implementation of steering behaviors gives more control
/// </summary>
public static class SteeringBehaviors
{
    /// <summary>
    /// Smoothly turn to face a target position.
    /// Returns the new orientation (yaw angle) after turning.
    ///
    /// Adapted from ChaseAndEvade sample.
    /// </summary>
    /// <param name="currentPosition">Current entity position</param>
    /// <param name="targetPosition">Position to face toward</param>
    /// <param name="currentYaw">Current yaw angle in radians</param>
    /// <param name="turnSpeed">Maximum turn rate in radians per second</param>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    /// <returns>New yaw angle in radians</returns>
    public static float TurnToFace(Vector3 currentPosition, Vector3 targetPosition,
        float currentYaw, float turnSpeed, float deltaTime)
    {
        // Calculate direction vector (flatten to horizontal plane)
        Vector3 direction = targetPosition - currentPosition;
        direction.Y = 0;

        // Handle zero-length vector
        if (direction.LengthSquared() < 0.001f)
            return currentYaw;

        direction = Vector3.Normalize(direction);

        // Calculate desired yaw angle
        float desiredYaw = MathF.Atan2(direction.X, direction.Z);

        // Calculate shortest turn angle
        float angleDifference = WrapAngle(desiredYaw - currentYaw);

        // Clamp turn amount by turn speed
        float maxTurn = turnSpeed * deltaTime;
        float turnAmount = Math.Clamp(angleDifference, -maxTurn, maxTurn);

        // Return new orientation
        return WrapAngle(currentYaw + turnAmount);
    }

    /// <summary>
    /// Generate a wander direction for idle movement.
    /// Returns a direction vector for wandering behavior.
    ///
    /// Adapted from ChaseAndEvade sample for 3D.
    /// </summary>
    /// <param name="currentPosition">Current entity position</param>
    /// <param name="currentDirection">Current heading direction (normalized)</param>
    /// <param name="centerPosition">Center point to orbit around</param>
    /// <param name="wanderStrength">How much random variation (0-1)</param>
    /// <param name="deltaTime">Time since last frame</param>
    /// <returns>New wander direction (normalized)</returns>
    public static Vector3 Wander(Vector3 currentPosition, Vector3 currentDirection,
        Vector3 centerPosition, float wanderStrength, float deltaTime)
    {
        // Add random variation to current direction
        Random random = new Random();
        float randomAngle = ((float)random.NextDouble() - 0.5f) * wanderStrength;

        // Rotate direction by random angle (around Y axis)
        float cos = MathF.Cos(randomAngle);
        float sin = MathF.Sin(randomAngle);
        Vector3 newDirection = new Vector3(
            currentDirection.X * cos - currentDirection.Z * sin,
            0,
            currentDirection.X * sin + currentDirection.Z * cos
        );

        // Calculate vector toward center
        Vector3 toCenter = centerPosition - currentPosition;
        toCenter.Y = 0;
        float distanceFromCenter = toCenter.Length();

        // Add center attraction (stronger as we get farther from center)
        if (distanceFromCenter > 0.1f)
        {
            Vector3 centerDirection = Vector3.Normalize(toCenter);
            float centerWeight = Math.Min(distanceFromCenter * 0.1f, 1.0f);
            newDirection = Vector3.Lerp(newDirection, centerDirection, centerWeight);
        }

        return Vector3.Normalize(newDirection);
    }

    /// <summary>
    /// Calculate evade direction (flee from a target).
    /// Returns a position to move toward that's away from the threat.
    ///
    /// Adapted from ChaseAndEvade sample's mouse evade behavior.
    /// </summary>
    /// <param name="currentPosition">Current entity position</param>
    /// <param name="threatPosition">Position to flee from</param>
    /// <returns>Target position to move toward (away from threat)</returns>
    public static Vector3 Evade(Vector3 currentPosition, Vector3 threatPosition)
    {
        // Calculate escape vector (opposite side from threat)
        // Formula: seekPosition = 2 * currentPosition - threatPosition
        // This creates a point directly opposite the threat
        return 2 * currentPosition - threatPosition;
    }

    /// <summary>
    /// Wrap an angle to the range [-PI, PI].
    /// Prevents angle accumulation issues.
    ///
    /// From ChaseAndEvade sample.
    /// </summary>
    public static float WrapAngle(float angle)
    {
        while (angle > MathF.PI)
            angle -= MathF.PI * 2;
        while (angle < -MathF.PI)
            angle += MathF.PI * 2;
        return angle;
    }

    /// <summary>
    /// Calculate arrival behavior - slow down as approaching target.
    /// Returns desired speed based on distance to target.
    /// </summary>
    /// <param name="distanceToTarget">Distance from target</param>
    /// <param name="slowingRadius">Distance at which to start slowing</param>
    /// <param name="maxSpeed">Maximum speed</param>
    /// <returns>Desired speed (0 to maxSpeed)</returns>
    public static float Arrival(float distanceToTarget, float slowingRadius, float maxSpeed)
    {
        if (distanceToTarget < slowingRadius)
        {
            // Slow down proportionally as we approach target
            return maxSpeed * (distanceToTarget / slowingRadius);
        }
        return maxSpeed;
    }

    /// <summary>
    /// Simple obstacle avoidance using a forward raycast.
    /// Returns a steering force to avoid obstacles.
    /// </summary>
    /// <param name="currentDirection">Current heading direction</param>
    /// <param name="hasObstacle">Whether an obstacle was detected ahead</param>
    /// <param name="avoidanceForce">Strength of avoidance</param>
    /// <returns>Avoidance steering vector</returns>
    public static Vector3 AvoidObstacle(Vector3 currentDirection, bool hasObstacle, float avoidanceForce)
    {
        if (!hasObstacle)
            return Vector3.Zero;

        // Turn perpendicular to current direction to avoid obstacle
        // Rotate 90 degrees to the right (around Y axis)
        return new Vector3(currentDirection.Z, 0, -currentDirection.X) * avoidanceForce;
    }
}
