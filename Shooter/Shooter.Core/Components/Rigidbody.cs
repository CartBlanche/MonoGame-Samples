using System.Numerics;
using Shooter.Core.Plugins.Physics;
using Shooter.Core.Services;

namespace Shooter.Core.Components;

/// <summary>
/// Rigidbody component for physics simulation.
/// 
/// UNITY COMPARISON:
/// Very similar to Unity's Rigidbody component.
/// Main differences:
/// - Unity uses PhysX, we use BepuPhysics
/// - We need to manually sync with Transform3D
/// - Unity handles this automatically
/// 
/// EDUCATIONAL NOTE - RIGIDBODY PHYSICS:
/// 
/// A Rigidbody makes an object participate in physics simulation:
/// - Gravity affects it
/// - Collisions are detected and resolved
/// - Forces and impulses can be applied
/// - Velocity and angular velocity are tracked
/// 
/// Without a Rigidbody, objects are just visual - they don't interact physically.
/// 
/// Types of Rigidbodies:
/// 1. Dynamic: Full physics simulation (affected by forces, gravity, collisions)
/// 2. Kinematic: Moved by code, affects other bodies, not affected by physics
/// 3. Static: Never moves, used for walls/floors/obstacles
/// </summary>
public class Rigidbody : EntityComponent
{
    private IPhysicsBody? _physicsBody;
    private BodyType _bodyType = BodyType.Dynamic;
    private float _mass = 1.0f;
    private ColliderShape _shape = new BoxShape(1.0f, 1.0f, 1.0f);
    private bool _isTrigger = false;
    private int _layer = 0;
    
    // Physics state
    private Vector3 _velocity;
    private Vector3 _angularVelocity;
    
    /// <summary>
    /// Type of physics body.
    /// Similar to Rigidbody.isKinematic in Unity, but more explicit.
    /// </summary>
    public BodyType BodyType
    {
        get => _bodyType;
        set
        {
            if (_bodyType != value)
            {
                _bodyType = value;
                RecreatePhysicsBody();
            }
        }
    }
    
    /// <summary>
    /// Mass of the object in kilograms.
    /// Similar to Rigidbody.mass in Unity.
    /// 
    /// EDUCATIONAL NOTE - MASS:
    /// Mass affects:
    /// - How much force is needed to move it (F = ma)
    /// - How much it pushes other objects
    /// - How much gravity affects it
    /// 
    /// Typical masses:
    /// - Player character: 70-80 kg
    /// - Crate: 20-50 kg
    /// - Barrel: 100-200 kg (if full)
    /// - Vehicle: 1000+ kg
    /// 
    /// Use realistic masses for best results.
    /// </summary>
    public float Mass
    {
        get => _mass;
        set
        {
            if (_mass != value && value > 0)
            {
                _mass = value;
                RecreatePhysicsBody();
            }
        }
    }
    
    /// <summary>
    /// Collision shape for this body.
    /// Similar to adding a BoxCollider/SphereCollider in Unity.
    /// 
    /// EDUCATIONAL NOTE - COLLISION SHAPES:
    /// 
    /// Primitive shapes (fast, simple):
    /// - Box: Buildings, crates, walls
    /// - Sphere: Balls, explosions, simple characters
    /// - Capsule: Characters (good for preventing getting stuck)
    /// 
    /// Complex shapes (slower, more accurate):
    /// - Mesh: Terrain, complex static geometry
    /// - Compound: Multiple shapes combined
    /// 
    /// Use the simplest shape that works for your needs!
    /// </summary>
    public ColliderShape Shape
    {
        get => _shape;
        set
        {
            if (_shape != value)
            {
                _shape = value;
                RecreatePhysicsBody();
            }
        }
    }
    
    /// <summary>
    /// Is this a trigger collider?
    /// Triggers detect collisions but don't physically block objects.
    /// Similar to Collider.isTrigger in Unity.
    /// 
    /// EDUCATIONAL NOTE - TRIGGERS:
    /// Use triggers for:
    /// - Pickups (walk through to collect)
    /// - Teleport zones
    /// - Damage areas
    /// - Proximity detection (enemy awareness)
    /// 
    /// Triggers generate events but don't apply forces.
    /// </summary>
    public bool IsTrigger
    {
        get => _isTrigger;
        set
        {
            _isTrigger = value;
            if (_physicsBody != null)
            {
                _physicsBody.IsTrigger = value;
            }
        }
    }
    
    /// <summary>
    /// Physics layer for collision filtering.
    /// Similar to GameObject.layer in Unity.
    /// </summary>
    public int Layer
    {
        get => _layer;
        set
        {
            _layer = value;
            if (_physicsBody != null)
            {
                _physicsBody.Layer = value;
            }
        }
    }
    
    /// <summary>
    /// Linear velocity of the rigidbody.
    /// Similar to Rigidbody.velocity in Unity.
    /// 
    /// EDUCATIONAL NOTE - VELOCITY:
    /// Velocity is the rate of change of position (meters/second).
    /// 
    /// Setting velocity directly is useful for:
    /// - Jump (add upward velocity)
    /// - Dash (instant speed boost)
    /// - Launch (projectiles)
    /// 
    /// For continuous movement, use AddForce() instead - it's more realistic.
    /// </summary>
    public Vector3 Velocity
    {
        get
        {
            if (_physicsBody != null)
                return _physicsBody.Velocity;
            return _velocity;
        }
        set
        {
            _velocity = value;
            if (_physicsBody != null)
                _physicsBody.Velocity = value;
        }
    }
    
    /// <summary>
    /// Angular velocity (rotation speed).
    /// Similar to Rigidbody.angularVelocity in Unity.
    /// </summary>
    public Vector3 AngularVelocity
    {
        get
        {
            if (_physicsBody != null)
                return _physicsBody.AngularVelocity;
            return _angularVelocity;
        }
        set
        {
            _angularVelocity = value;
            if (_physicsBody != null)
                _physicsBody.AngularVelocity = value;
        }
    }
    
    /// <summary>
    /// Initialize the rigidbody and create physics body.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // Get physics provider from service locator
        var physics = ServiceLocator.Get<IPhysicsProvider>();
        if (physics == null)
        {
            Console.WriteLine("[Rigidbody] Warning: No physics provider registered");
            return;
        }
        
        // Create the physics body
        CreatePhysicsBody(physics);
    }
    
    /// <summary>
    /// Sync transform with physics body each frame.
    /// 
    /// EDUCATIONAL NOTE - PHYSICS SYNC:
    /// 
    /// Physics simulation runs independently of rendering.
    /// We need to sync the visual transform with the physics position.
    /// 
    /// Unity does this automatically for you.
    /// In MonoGame, we do it manually in Update().
    /// 
    /// Direction: Physics → Transform
    /// The physics simulation is authoritative for dynamic bodies.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        if (_physicsBody == null || Owner == null)
            return;
            
        // Get the transform component
        var transform = Owner.GetComponent<Transform3D>();
        if (transform == null)
            return;
        
        // Sync physics body position to transform
        // For dynamic bodies, physics is authoritative
        if (_bodyType == BodyType.Dynamic)
        {
            transform.Position = _physicsBody.Position;
            transform.Rotation = _physicsBody.Rotation;
        }
        // For kinematic bodies, transform is authoritative
        else if (_bodyType == BodyType.Kinematic)
        {
            _physicsBody.Position = transform.Position;
            _physicsBody.Rotation = transform.Rotation;
        }
    }
    
    /// <summary>
    /// Apply a force to the rigidbody.
    /// Similar to Rigidbody.AddForce() in Unity.
    /// 
    /// EDUCATIONAL NOTE - FORCES:
    /// 
    /// Force is mass × acceleration (F = ma).
    /// Forces are applied over time (continuous).
    /// 
    /// Use forces for:
    /// - Character movement (walking/running)
    /// - Wind effects
    /// - Explosions (applied over multiple frames)
    /// - Vehicle engines
    /// 
    /// The heavier the object, the more force needed to move it.
    /// Force(100) on 1kg object = acceleration of 100 m/s²
    /// Force(100) on 10kg object = acceleration of 10 m/s²
    /// </summary>
    public void AddForce(Vector3 force)
    {
        _physicsBody?.ApplyForce(force);
    }
    
    /// <summary>
    /// Apply an impulse (instant force) to the rigidbody.
    /// Similar to Rigidbody.AddForce(force, ForceMode.Impulse) in Unity.
    /// 
    /// EDUCATIONAL NOTE - IMPULSE VS FORCE:
    /// 
    /// Impulse is an instant change in velocity.
    /// Impulse = Force × Time (but applied instantly).
    /// 
    /// Use impulses for:
    /// - Jumps (instant upward velocity)
    /// - Explosions (instant push)
    /// - Collisions (instant bounce)
    /// - Launch pads
    /// 
    /// Impulse affects velocity directly, independent of mass.
    /// </summary>
    public void AddImpulse(Vector3 impulse)
    {
        _physicsBody?.ApplyImpulse(impulse);
    }
    
    /// <summary>
    /// Apply torque (rotational force).
    /// Similar to Rigidbody.AddTorque() in Unity.
    /// </summary>
    public void AddTorque(Vector3 torque)
    {
        _physicsBody?.ApplyTorque(torque);
    }
    
    /// <summary>
    /// Recreate the physics body (when properties change).
    /// </summary>
    private void RecreatePhysicsBody()
    {
        var physics = ServiceLocator.Get<IPhysicsProvider>();
        if (physics == null)
            return;
            
        // Remove old body
        if (_physicsBody != null)
        {
            physics.RemoveBody(_physicsBody);
            _physicsBody = null;
        }
        
        // Create new body
        CreatePhysicsBody(physics);
    }
    
    /// <summary>
    /// Create the physics body in the simulation.
    /// </summary>
    private void CreatePhysicsBody(IPhysicsProvider physics)
    {
        if (Owner == null)
            return;
            
        var transform = Owner.GetComponent<Transform3D>();
        if (transform == null)
            return;
        
        // Create body description
        var description = new BodyDescription
        {
            Position = ToNumerics(transform.Position),
            Rotation = transform.Rotation,
            BodyType = _bodyType,
            Shape = _shape,
            Mass = _mass,
            Friction = 0.5f,
            Restitution = 0.0f, // Bounciness (0 = no bounce, 1 = perfect bounce)
            IsTrigger = _isTrigger,
            Layer = _layer,
            UserData = Owner // Store reference to entity
        };
        
        // Create the body
        _physicsBody = physics.CreateBody(description);
        
        Console.WriteLine($"[Rigidbody] Created {_bodyType} body for entity {Owner.Name}");
    }
    
    /// <summary>
    /// Clean up physics body when destroyed.
    /// </summary>
    public override void OnDestroy()
    {
        if (_physicsBody != null)
        {
            var physics = ServiceLocator.Get<IPhysicsProvider>();
            physics?.RemoveBody(_physicsBody);
            _physicsBody = null;
        }
        
        base.OnDestroy();
    }
    
    // Helper methods for coordinate conversion
    private Vector3 ToNumerics(Microsoft.Xna.Framework.Vector3 v) => 
        new Vector3(v.X, v.Y, v.Z);
    
    private Microsoft.Xna.Framework.Vector3 FromNumerics(Vector3 v) => 
        new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
}
