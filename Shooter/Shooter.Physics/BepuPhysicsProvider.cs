using Shooter.Core.Plugins.Physics;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using System.Runtime.CompilerServices;

namespace Shooter.Physics;

/// <summary>
/// BepuPhysics v2 implementation of the physics provider.
/// 
/// UNITY COMPARISON - PHYSICS ENGINES:
/// Unity uses PhysX (by NVIDIA) internally.
/// We use BepuPhysics v2 because:
/// 1. Pure C# (easier to understand and modify)
/// 2. Excellent performance (often faster than PhysX)
/// 3. Open source (educational value)
/// 4. Modern design (DOD - Data Oriented Design)
/// 
/// EDUCATIONAL NOTE - BEPU ARCHITECTURE:
/// 
/// BepuPhysics uses several core components:
/// 
/// 1. BufferPool: Memory allocator for physics data
///    - Reduces garbage collection pressure
///    - Recycles memory buffers for performance
///    
/// 2. Simulation: The main physics world
///    - Contains all bodies, shapes, constraints
///    - Steps the simulation forward in time
///    
/// 3. ThreadDispatcher: Manages multi-threading
///    - Spreads physics work across CPU cores
///    - Improves performance on multi-core systems
///    
/// 4. Bodies vs Statics:
///    - Bodies: Dynamic objects that can move (like Rigidbody in Unity)
///    - Statics: Immovable objects (like Collider without Rigidbody in Unity)
/// 
/// COORDINATE SYSTEM:
/// BepuPhysics uses the same coordinate system as MonoGame:
/// - X: Right
/// - Y: Up
/// - Z: Backward (toward camera in right-handed system)
/// Unity uses: X: Right, Y: Up, Z: Forward (left-handed)
/// </summary>
public class BepuPhysicsProvider : IPhysicsProvider
{
    // Core Bepu components
    private BufferPool? _bufferPool;
    private Simulation? _simulation;
    private SimpleThreadDispatcher? _threadDispatcher;
    
    // Tracking
    private Dictionary<BodyHandle, BepuPhysicsBody> _bodies = new();
    private Dictionary<StaticHandle, BepuPhysicsBody> _statics = new();
    
    /// <summary>
    /// Gravity vector for the simulation.
    /// Default is -9.81 m/s² on Y axis (Earth gravity).
    /// Similar to Physics.gravity in Unity.
    /// </summary>
    public Vector3 Gravity { get; set; } = new Vector3(0, -9.81f, 0);
    
    /// <summary>
    /// Initialize the BepuPhysics simulation.
    /// 
    /// EDUCATIONAL NOTE - INITIALIZATION ORDER:
    /// 1. Create BufferPool (memory management)
    /// 2. Create ThreadDispatcher (parallelism)
    /// 3. Create Simulation with gravity and collision settings
    /// 4. Configure collision callbacks if needed
    /// </summary>
    public void Initialize()
    {
        // Step 1: Create buffer pool for memory management
        // Bepu's DOD design requires explicit memory management
        _bufferPool = new BufferPool();
        
        // Step 2: Create thread dispatcher
        // Use all available CPU cores for better performance
        // TEMPORARY: Disable multi-threading to debug null reference issue
        var targetThreadCount = 0; // Math.Max(1, Environment.ProcessorCount - 1);
        if (targetThreadCount > 0)
        {
            _threadDispatcher = new SimpleThreadDispatcher(targetThreadCount);
        }
        
        // Step 3: Create simulation
        // SolveDescription configures the constraint solver
        // - VelocityIterationCount: Higher = more accurate but slower
        // - SubstepCount: Number of physics substeps per Step()
        _simulation = Simulation.Create(
            _bufferPool,
            new NarrowPhaseCallbacks(),  // Collision callbacks
            new PoseIntegratorCallbacks(Gravity), // Gravity integration
            new SolveDescription(8, 1) // 8 velocity iterations, 1 substep
        );
        
        // Note: BepuPhysics v2.4+ handles threading through Simulation.Timestep() parameters
        // We pass the thread dispatcher there
        
        Console.WriteLine($"[BepuPhysics] Initialized with {targetThreadCount} threads (single-threaded for now)");
    }
    
    /// <summary>
    /// Step the physics simulation forward by a fixed time step.
    /// 
    /// EDUCATIONAL NOTE - FIXED TIMESTEP:
    /// Physics simulations MUST use fixed time steps for:
    /// 1. Determinism (same inputs = same outputs)
    /// 2. Stability (variable dt causes jitter/explosions)
    /// 3. Numerical accuracy (integration errors accumulate)
    /// 
    /// Unity calls FixedUpdate() at Time.fixedDeltaTime intervals (default 0.02s = 50 FPS).
    /// We'll call this from a fixed timestep accumulator in Game.cs.
    /// 
    /// Typical values:
    /// - 60 FPS: 1/60 = 0.0166s
    /// - 50 FPS: 1/50 = 0.02s (Unity default)
    /// - 30 FPS: 1/30 = 0.0333s (minimum for stability)
    /// </summary>
    /// <param name="deltaTime">Time step in seconds (usually 1/60)</param>
    public void Step(float deltaTime)
    {
        if (_simulation == null || _threadDispatcher == null)
            return;
            
        // Step the simulation forward
        // This updates all body positions, velocities, and resolves collisions
        _simulation.Timestep(deltaTime, _threadDispatcher);
    }
    
    /// <summary>
    /// Create a physics body (rigid body or static collider).
    /// 
    /// UNITY COMPARISON:
    /// This is like adding a Rigidbody + Collider component in Unity.
    /// - BodyType.Dynamic = Rigidbody with default settings
    /// - BodyType.Kinematic = Rigidbody with isKinematic = true
    /// - BodyType.Static = Just a Collider, no Rigidbody
    /// </summary>
    public IPhysicsBody CreateBody(Core.Plugins.Physics.BodyDescription description)
    {
        if (_simulation == null)
            throw new InvalidOperationException("Physics not initialized");
            
        // Convert our ColliderShape to Bepu's TypedIndex
        var shapeIndex = CreateShape(description.Shape);
        
        var body = new BepuPhysicsBody
        {
            BodyType = description.BodyType,
            UserData = description.UserData,
            IsTrigger = description.IsTrigger,
            Layer = description.Layer,
            HalfExtents = description.Shape is BoxShape box ? box.HalfExtents : Vector3.One // Store half extents for raycasting
        };
        
        if (description.BodyType == BodyType.Static)
        {
            // Static bodies don't move (terrain, walls, etc.)
            var staticDescription = new StaticDescription(
                new RigidPose(description.Position, description.Rotation),
                shapeIndex
            );
            
            var handle = _simulation.Statics.Add(staticDescription);
            body.StaticHandle = handle;
            _statics[handle] = body;
        }
        else
        {
            // Dynamic/Kinematic bodies can move
            // Calculate inertia based on shape and mass
            var inertia = description.BodyType == BodyType.Dynamic 
                ? CalculateInertia(shapeIndex, description.Mass)
                : new BodyInertia(); // Zero inertia for kinematic
                
            var bodyDescription = BepuPhysics.BodyDescription.CreateDynamic(
                new RigidPose(description.Position, description.Rotation),
                inertia,
                shapeIndex, // Just the shape index
                0.01f // Activity threshold
            );
            
            var handle = _simulation.Bodies.Add(bodyDescription);
            body.BodyHandle = handle;
            body.Simulation = _simulation;
            _bodies[handle] = body;
        }
        
        return body;
    }
    
    /// <summary>
    /// Remove a body from the simulation.
    /// Similar to Destroy(gameObject.GetComponent&lt;Rigidbody&gt;()) in Unity.
    /// </summary>
    public void RemoveBody(IPhysicsBody body)
    {
        if (_simulation == null || body is not BepuPhysicsBody bepuBody)
            return;
            
        if (bepuBody.IsStatic)
        {
            _simulation.Statics.Remove(bepuBody.StaticHandle);
            _statics.Remove(bepuBody.StaticHandle);
        }
        else
        {
            _simulation.Bodies.Remove(bepuBody.BodyHandle);
            _bodies.Remove(bepuBody.BodyHandle);
        }
    }
    
    /// <summary>
    /// Perform a raycast in the physics world.
    /// Similar to Physics.Raycast() in Unity.
    /// 
    /// EDUCATIONAL NOTE - RAYCASTING:
    /// Raycasts are essential for:
    /// - Hit detection (shooting)
    /// - Line of sight checks (AI)
    /// - Ground detection (is player on ground?)
    /// - Interaction (what am I looking at?)
    /// 
    /// Unity: Physics.Raycast(origin, direction, out hit, maxDistance, layerMask)
    /// Bepu: Requires creating a RayHitHandler and calling Simulation.RayCast()
    /// </summary>
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit, int layerMask = -1)
    {
        hit = default;
        
        if (_simulation == null)
            return false;
        
        // Normalize direction
        direction = Vector3.Normalize(direction);
        
        // Use Bepu's optimized raycasting with hit handler
        var hitHandler = new ClosestHitHandler(this);
        _simulation.RayCast(origin, direction, maxDistance, ref hitHandler);
        
        if (hitHandler.Hit)
        {
            hit = new RaycastHit
            {
                Point = origin + direction * hitHandler.T,
                Normal = hitHandler.Normal,
                Distance = hitHandler.T,
                Body = hitHandler.HitBody
                // UserData is automatically provided by Body.UserData property
            };
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Helper struct for Bepu raycast hit detection.
    /// Implements IRayHitHandler to find the closest hit.
    /// </summary>
    private struct ClosestHitHandler : IRayHitHandler
    {
        public bool Hit;
        public float T;
        public Vector3 Normal;
        public IPhysicsBody? HitBody;
        
        private readonly BepuPhysicsProvider _provider;
        
        public ClosestHitHandler(BepuPhysicsProvider provider)
        {
            _provider = provider;
            Hit = false;
            T = 0;
            Normal = Vector3.Zero;
            HitBody = null;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            // Allow testing against all collidables
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
        {
            // Only record if this is closer than previous hits
            if (!Hit || t < T)
            {
                Hit = true;
                T = t;
                Normal = normal;
                
                // Try to get the body
                if (collidable.Mobility == CollidableMobility.Static)
                {
                    // It's a static body
                    if (_provider._statics.TryGetValue(collidable.StaticHandle, out var staticBody))
                    {
                        HitBody = staticBody;
                    }
                }
                else
                {
                    // It's a dynamic/kinematic body
                    if (_provider._bodies.TryGetValue(collidable.BodyHandle, out var dynamicBody))
                    {
                        HitBody = dynamicBody;
                    }
                }
                
                // Update maximum T to find closer hits
                maximumT = t;
            }
        }
    }
    
    public bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, out RaycastHit hit, int layerMask = -1)
    {
        // TODO Phase 2: Implement sphere casting
        hit = default;
        return false;
    }
    
    public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, out RaycastHit hit, int layerMask = -1)
    {
        // TODO Phase 2: Implement box casting
        hit = default;
        return false;
    }
    
    public IEnumerable<IPhysicsBody> OverlapSphere(Vector3 center, float radius, int layerMask = -1)
    {
        // TODO Phase 2: Implement overlap queries
        return Enumerable.Empty<IPhysicsBody>();
    }
    
    public void Shutdown()
    {
        _simulation?.Dispose();
        _threadDispatcher?.Dispose();
        _bufferPool?.Clear();
        
        _bodies.Clear();
        _statics.Clear();
    }
    
    #region Helper Methods
    
    private TypedIndex CreateShape(ColliderShape shape)
    {
        if (_bufferPool == null || _simulation == null)
            throw new InvalidOperationException("Physics not initialized");
            
        return shape switch
        {
            BoxShape box => _simulation.Shapes.Add(new Box(
                box.HalfExtents.X * 2,
                box.HalfExtents.Y * 2,
                box.HalfExtents.Z * 2
            )),
            
            SphereShape sphere => _simulation.Shapes.Add(new Sphere(sphere.Radius)),
            
            CapsuleShape capsule => _simulation.Shapes.Add(new Capsule(
                capsule.Radius,
                capsule.Height
            )),
            
            _ => throw new NotSupportedException($"Shape type {shape.Type} not yet supported")
        };
    }
    
    private BodyInertia CalculateInertia(TypedIndex shapeIndex, float mass)
    {
        if (_simulation == null)
            throw new InvalidOperationException("Physics not initialized");
        
        // For Phase 1, use a simple box inertia
        // TODO Phase 2: Properly calculate based on shape type
        return new BodyInertia { InverseMass = 1.0f / mass };
    }
    
    #endregion
}

/// <summary>
/// Wrapper for Bepu's body/static handles to implement IPhysicsBody.
/// </summary>
internal class BepuPhysicsBody : IPhysicsBody
{
    public BodyHandle BodyHandle { get; set; }
    public StaticHandle StaticHandle { get; set; }
    public Simulation? Simulation { get; set; }
    public BodyType BodyType { get; set; }
    public bool IsStatic => BodyType == BodyType.Static;
    
    public object? UserData { get; set; }
    public bool IsTrigger { get; set; }
    public int Layer { get; set; }
    
    // Store shape extents for raycast testing
    public Vector3 HalfExtents { get; set; }
    
    public Vector3 Position
    {
        get
        {
            if (Simulation == null) return Vector3.Zero;
            
            if (IsStatic)
                return Simulation.Statics[StaticHandle].Pose.Position;
            else
                return Simulation.Bodies[BodyHandle].Pose.Position;
        }
        set
        {
            if (Simulation == null) return;
            
            if (IsStatic)
            {
                // Static bodies cannot be moved in Bepu - must remove and re-add
                // For Phase 1, we'll just skip this
                // TODO Phase 2: Implement if needed by removing/re-adding static
            }
            else
            {
                var bodyRef = Simulation.Bodies[BodyHandle];
                bodyRef.Pose.Position = value;
            }
        }
    }
    
    public Quaternion Rotation
    {
        get
        {
            if (Simulation == null) return Quaternion.Identity;
            
            if (IsStatic)
                return Simulation.Statics[StaticHandle].Pose.Orientation;
            else
                return Simulation.Bodies[BodyHandle].Pose.Orientation;
        }
        set
        {
            if (Simulation == null) return;
            
            if (IsStatic)
            {
                // Static bodies cannot be rotated - must remove and re-add
                // For Phase 1, we'll just skip this
            }
            else
            {
                var bodyRef = Simulation.Bodies[BodyHandle];
                bodyRef.Pose.Orientation = value;
            }
        }
    }
    
    public Vector3 Velocity
    {
        get
        {
            if (Simulation == null || IsStatic) return Vector3.Zero;
            return Simulation.Bodies[BodyHandle].Velocity.Linear;
        }
        set
        {
            if (Simulation == null || IsStatic) return;
            var bodyRef = Simulation.Bodies[BodyHandle];
            bodyRef.Velocity.Linear = value;
        }
    }
    
    public Vector3 AngularVelocity
    {
        get
        {
            if (Simulation == null || IsStatic) return Vector3.Zero;
            return Simulation.Bodies[BodyHandle].Velocity.Angular;
        }
        set
        {
            if (Simulation == null || IsStatic) return;
            var bodyRef = Simulation.Bodies[BodyHandle];
            bodyRef.Velocity.Angular = value;
        }
    }
    
    public void ApplyForce(Vector3 force)
    {
        if (Simulation == null || IsStatic || BodyType == BodyType.Kinematic) return;
        
        // F = ma, so a = F/m
        var bodyRef = Simulation.Bodies[BodyHandle];
        var acceleration = force * bodyRef.LocalInertia.InverseMass;
        bodyRef.Velocity.Linear += acceleration;
    }
    
    public void ApplyImpulse(Vector3 impulse)
    {
        if (Simulation == null || IsStatic || BodyType == BodyType.Kinematic) return;
        
        // Impulse directly changes velocity: Δv = impulse / mass
        var bodyRef = Simulation.Bodies[BodyHandle];
        bodyRef.Velocity.Linear += impulse * bodyRef.LocalInertia.InverseMass;
    }
    
    public void ApplyTorque(Vector3 torque)
    {
        if (Simulation == null || IsStatic || BodyType == BodyType.Kinematic) return;
        
        var bodyRef = Simulation.Bodies[BodyHandle];
        // TODO: Calculate angular acceleration from torque and inertia tensor
        // For now, apply directly (simplified)
        bodyRef.Velocity.Angular += torque * 0.01f;
    }
}

/// <summary>
/// Narrow phase callbacks for collision detection.
/// Required by Bepu but can be minimal for basic physics.
/// </summary>
unsafe struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation)
    {
    }
    
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        return true; // Allow all collisions
    }
    
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }
    
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // Basic material properties
        pairMaterial.FrictionCoefficient = 0.5f;
        pairMaterial.MaximumRecoveryVelocity = 2f;
        pairMaterial.SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1);
        return true;
    }
    
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }
    
    public void Dispose()
    {
    }
}

/// <summary>
/// Pose integrator callbacks - handles gravity and velocity integration.
/// </summary>
struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    private Vector3 _gravity;
    
    public PoseIntegratorCallbacks(Vector3 gravity)
    {
        _gravity = gravity;
    }
    
    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinematics => false;
    
    public void Initialize(Simulation simulation)
    {
    }
    
    public readonly void PrepareForIntegration(float dt)
    {
    }
    
    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        // Apply gravity to all Y components
        velocity.Linear.Y = velocity.Linear.Y + new Vector<float>(_gravity.Y) * dt;
    }
}