using System.Numerics;

namespace Shooter.Core.Plugins.Physics;

/// <summary>
/// Plugin interface for physics engine implementations.
/// This allows swapping between different physics engines (Bepu, Jitter, custom).
/// 
/// DESIGN PATTERN: Provider/Strategy Pattern
/// This interface defines the contract that any physics engine must fulfill.
/// The actual implementation is injected at runtime via ServiceLocator.
/// 
/// UNITY COMPARISON:
/// Unity uses PhysX internally and doesn't expose a provider interface.
/// We expose this to make the system more modular and educational.
/// </summary>
public interface IPhysicsProvider
{
    /// <summary>
    /// Initialize the physics engine.
    /// Called once during game startup.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Update physics simulation.
    /// Should be called on a fixed timestep for deterministic physics.
    /// 
    /// NOTE: Unity uses FixedUpdate() for physics.
    /// In MonoGame, you control the timing yourself.
    /// </summary>
    /// <param name="deltaTime">Time step in seconds (typically 1/60)</param>
    void Step(float deltaTime);

    /// <summary>
    /// Clean up physics resources.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Create a rigid body in the physics world.
    /// Similar to adding a Rigidbody component in Unity.
    /// </summary>
    IPhysicsBody CreateBody(BodyDescription description);

    /// <summary>
    /// Remove a body from the physics simulation.
    /// </summary>
    void RemoveBody(IPhysicsBody body);

    /// <summary>
    /// Perform a raycast in the physics world.
    /// Similar to Physics.Raycast() in Unity.
    /// </summary>
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit, int layerMask = -1);

    /// <summary>
    /// Perform a sphere cast (raycast with radius).
    /// Similar to Physics.SphereCast() in Unity.
    /// </summary>
    bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, out RaycastHit hit, int layerMask = -1);

    /// <summary>
    /// Perform a box cast.
    /// Similar to Physics.BoxCast() in Unity.
    /// </summary>
    bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, out RaycastHit hit, int layerMask = -1);

    /// <summary>
    /// Check if a sphere overlaps any colliders.
    /// Similar to Physics.OverlapSphere() in Unity.
    /// </summary>
    IEnumerable<IPhysicsBody> OverlapSphere(Vector3 center, float radius, int layerMask = -1);

    /// <summary>
    /// Set the gravity for the physics world.
    /// Similar to Physics.gravity in Unity.
    /// </summary>
    Vector3 Gravity { get; set; }
}

/// <summary>
/// Describes how to create a physics body.
/// Contains all necessary parameters for body creation.
/// </summary>
public struct BodyDescription
{
    public Vector3 Position;
    public Quaternion Rotation;
    public BodyType BodyType;
    public ColliderShape Shape;
    public float Mass;
    public float Friction;
    public float Restitution; // Bounciness
    public bool IsTrigger;
    public int Layer;
    public object? UserData; // Reference to Entity or component
}

/// <summary>
/// Type of physics body.
/// Similar to Unity's Rigidbody constraints.
/// </summary>
public enum BodyType
{
    /// <summary>
    /// Static body - doesn't move, infinite mass.
    /// Use for terrain, walls, static props.
    /// </summary>
    Static,

    /// <summary>
    /// Dynamic body - affected by forces and gravity.
    /// Use for physics objects, projectiles.
    /// </summary>
    Dynamic,

    /// <summary>
    /// Kinematic body - moved by code, not physics.
    /// Use for character controllers, moving platforms.
    /// </summary>
    Kinematic
}

/// <summary>
/// Shape of the collider.
/// Similar to Unity's Collider types (BoxCollider, SphereCollider, etc.)
/// </summary>
public abstract class ColliderShape
{
    public abstract ColliderType Type { get; }
}

public enum ColliderType
{
    Box,
    Sphere,
    Capsule,
    Mesh,
    Compound
}

public class BoxShape : ColliderShape
{
    public override ColliderType Type => ColliderType.Box;
    public Vector3 HalfExtents { get; set; }

    public BoxShape(Vector3 halfExtents)
    {
        HalfExtents = halfExtents;
    }

    public BoxShape(float width, float height, float depth)
    {
        HalfExtents = new Vector3(width / 2f, height / 2f, depth / 2f);
    }
}

public class SphereShape : ColliderShape
{
    public override ColliderType Type => ColliderType.Sphere;
    public float Radius { get; set; }

    public SphereShape(float radius)
    {
        Radius = radius;
    }
}

public class CapsuleShape : ColliderShape
{
    public override ColliderType Type => ColliderType.Capsule;
    public float Radius { get; set; }
    public float Height { get; set; }

    public CapsuleShape(float radius, float height)
    {
        Radius = radius;
        Height = height;
    }
}

/// <summary>
/// Interface for a physics body in the simulation.
/// Represents a single rigid body with collision.
/// Similar to Unity's Rigidbody component.
/// </summary>
public interface IPhysicsBody
{
    /// <summary>
    /// Position in world space.
    /// Similar to Rigidbody.position in Unity.
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Rotation in world space.
    /// Similar to Rigidbody.rotation in Unity.
    /// </summary>
    Quaternion Rotation { get; set; }

    /// <summary>
    /// Linear velocity.
    /// Similar to Rigidbody.velocity in Unity.
    /// </summary>
    Vector3 Velocity { get; set; }

    /// <summary>
    /// Angular velocity.
    /// Similar to Rigidbody.angularVelocity in Unity.
    /// </summary>
    Vector3 AngularVelocity { get; set; }

    /// <summary>
    /// Apply a force to the body.
    /// Similar to Rigidbody.AddForce() in Unity.
    /// </summary>
    void ApplyForce(Vector3 force);

    /// <summary>
    /// Apply an impulse (instant force).
    /// Similar to Rigidbody.AddForce(force, ForceMode.Impulse) in Unity.
    /// </summary>
    void ApplyImpulse(Vector3 impulse);

    /// <summary>
    /// Apply torque (rotational force).
    /// Similar to Rigidbody.AddTorque() in Unity.
    /// </summary>
    void ApplyTorque(Vector3 torque);

    /// <summary>
    /// User data associated with this body (typically the Entity).
    /// Similar to storing a reference in Unity.
    /// </summary>
    object? UserData { get; set; }

    /// <summary>
    /// Whether this body is a trigger (no physical collision, only events).
    /// Similar to Collider.isTrigger in Unity.
    /// </summary>
    bool IsTrigger { get; set; }

    /// <summary>
    /// Collision layer for filtering.
    /// Similar to GameObject.layer in Unity.
    /// </summary>
    int Layer { get; set; }
}

/// <summary>
/// Information about a raycast hit.
/// Similar to Unity's RaycastHit struct.
/// </summary>
public struct RaycastHit
{
    /// <summary>
    /// The body that was hit.
    /// </summary>
    public IPhysicsBody Body;

    /// <summary>
    /// World position of the hit point.
    /// Similar to RaycastHit.point in Unity.
    /// </summary>
    public Vector3 Point;

    /// <summary>
    /// Surface normal at the hit point.
    /// Similar to RaycastHit.normal in Unity.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// Distance from ray origin to hit point.
    /// Similar to RaycastHit.distance in Unity.
    /// </summary>
    public float Distance;

    /// <summary>
    /// User data from the hit body (typically an Entity).
    /// Similar to RaycastHit.collider.gameObject in Unity.
    /// </summary>
    public object? UserData => Body?.UserData;
}
