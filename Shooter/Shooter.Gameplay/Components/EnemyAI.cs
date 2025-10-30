using Shooter.Core.Components;
using Shooter.Core.Entities;
using Shooter.Core.Services;
using Shooter.Gameplay.Systems;
using System.Numerics;
using System.Linq;

namespace Shooter.Gameplay.Components;

/// <summary>
/// AI state for enemy behavior.
/// Uses a simple finite state machine pattern.
/// 
/// UNITY COMPARISON:
/// Similar to Unity's NavMeshAgent + custom AI scripts.
/// Unity: NavMeshAgent handles pathfinding automatically
/// MonoGame: We implement simple direct-pursuit AI manually
/// </summary>
public enum AIState
{
    /// <summary>
    /// Enemy is stationary, looking for targets
    /// </summary>
    Idle,
    
    /// <summary>
    /// Enemy is moving toward the player
    /// </summary>
    Chase,
    
    /// <summary>
    /// Enemy is in range and attacking the player
    /// </summary>
    Attack,
    
    /// <summary>
    /// Enemy is dead (could play death animation, etc)
    /// </summary>
    Dead
}

/// <summary>
/// Basic enemy AI component with chase and attack behaviors.
/// Implements a simple state machine for enemy behavior.
/// 
/// EDUCATIONAL NOTE - AI State Machines:
/// State machines are a fundamental AI pattern:
/// - Define clear states (Idle, Chase, Attack, Dead)
/// - Define transitions between states (based on distance, health, etc)
/// - Each state has its own update logic
/// 
/// Unity uses similar patterns with Animator State Machines and custom scripts.
/// This is a pure code-based state machine for simplicity.
/// </summary>
public class EnemyAI : EntityComponent
{
    // State machine
    private AIState _currentState = AIState.Idle;
    private float _stateTimer = 0f;
    
    // Target tracking
    private Entity? _target;
    private Transform3D? _targetTransform;
    private Transform3D? _transform;
    private Health? _health;
    
    // AI parameters
    private float _detectionRange = 20f;
    private float _attackRange = 2f;
    private float _moveSpeed = 3f;
    private float _turnSpeed = 5f;
    private float _attackDamage = 10f;
    private float _attackCooldown = 1.5f;
    private float _lastAttackTime = 0f;
    
    // Line of sight
    private float _losCheckInterval = 0.5f;
    private float _losCheckTimer = 0f;
    private bool _hasLineOfSight = false;

    /// <summary>
    /// Current AI state
    /// </summary>
    public AIState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                OnStateExit(_currentState);
                _currentState = value;
                _stateTimer = 0f;
                OnStateEnter(_currentState);
            }
        }
    }

    /// <summary>
    /// Target entity to chase/attack (usually the player)
    /// </summary>
    public Entity? Target
    {
        get => _target;
        set
        {
            _target = value;
            _targetTransform = _target?.GetComponent<Transform3D>();
        }
    }

    /// <summary>
    /// How far the enemy can detect the player
    /// </summary>
    public float DetectionRange
    {
        get => _detectionRange;
        set => _detectionRange = value;
    }

    /// <summary>
    /// How close the enemy must be to attack
    /// </summary>
    public float AttackRange
    {
        get => _attackRange;
        set => _attackRange = value;
    }

    /// <summary>
    /// Movement speed in meters/second
    /// </summary>
    public float MoveSpeed
    {
        get => _moveSpeed;
        set => _moveSpeed = value;
    }

    /// <summary>
    /// Damage dealt per attack
    /// </summary>
    public float AttackDamage
    {
        get => _attackDamage;
        set => _attackDamage = value;
    }

    /// <summary>
    /// Time between attacks in seconds
    /// </summary>
    public float AttackCooldown
    {
        get => _attackCooldown;
        set => _attackCooldown = value;
    }

    /// <summary>
    /// Initialize component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        _transform = Owner?.GetComponent<Transform3D>();
        _health = Owner?.GetComponent<Health>();
        
        // Subscribe to death event
        if (_health != null)
        {
            _health.OnDeath += OnDeath;
        }
        
        // Auto-find player as target if not set
        // For now, we'll need to manually set the target
        // TODO: Add scene reference to Entity class or use a global entity manager
        
        Console.WriteLine($"[EnemyAI] {Owner?.Name} initialized in {_currentState} state");
    }

    /// <summary>
    /// Update AI behavior based on current state
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        if (_transform == null || _target == null || _targetTransform == null)
            return;
        
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _stateTimer += deltaTime;
        
        // Check line of sight periodically
        _losCheckTimer += deltaTime;
        if (_losCheckTimer >= _losCheckInterval)
        {
            _losCheckTimer = 0f;
            _hasLineOfSight = CheckLineOfSight();
        }
        
        // Update state machine
        switch (_currentState)
        {
            case AIState.Idle:
                UpdateIdle(deltaTime);
                break;
            case AIState.Chase:
                UpdateChase(deltaTime);
                break;
            case AIState.Attack:
                UpdateAttack(deltaTime);
                break;
            case AIState.Dead:
                // Do nothing when dead
                break;
        }
    }

    /// <summary>
    /// Update idle state - look for player
    /// </summary>
    private void UpdateIdle(float deltaTime)
    {
        if (_targetTransform == null || _transform == null)
            return;
        
        float distanceToTarget = Vector3.Distance(_transform.Position, _targetTransform.Position);
        
        // If player is in range and visible, start chasing
        if (distanceToTarget <= _detectionRange && _hasLineOfSight)
        {
            CurrentState = AIState.Chase;
        }
    }

    /// <summary>
    /// Update chase state - move toward player
    /// </summary>
    private void UpdateChase(float deltaTime)
    {
        if (_targetTransform == null || _transform == null)
            return;
        
        float distanceToTarget = Vector3.Distance(_transform.Position, _targetTransform.Position);
        
        // If player is in attack range, switch to attack
        if (distanceToTarget <= _attackRange)
        {
            CurrentState = AIState.Attack;
            return;
        }
        
        // If player is too far or not visible, return to idle
        if (distanceToTarget > _detectionRange || !_hasLineOfSight)
        {
            CurrentState = AIState.Idle;
            return;
        }
        
        // Move toward target
        Vector3 direction = Vector3.Normalize(_targetTransform.Position - _transform.Position);
        _transform.Position += direction * _moveSpeed * deltaTime;
        
        // Rotate toward target
        LookAt(_targetTransform.Position, deltaTime);
    }

    /// <summary>
    /// Update attack state - deal damage to player
    /// </summary>
    private void UpdateAttack(float deltaTime)
    {
        if (_targetTransform == null || _transform == null || _target == null)
            return;
        
        float distanceToTarget = Vector3.Distance(_transform.Position, _targetTransform.Position);
        
        // If player moved out of range, chase again
        if (distanceToTarget > _attackRange * 1.2f) // Small buffer to prevent state flickering
        {
            CurrentState = AIState.Chase;
            return;
        }
        
        // Rotate toward target while attacking
        LookAt(_targetTransform.Position, deltaTime);
        
        // Attack on cooldown
        var timeService = ServiceLocator.Get<Core.Services.ITimeService>();
        float currentTime = timeService.TotalTime;
        if (currentTime - _lastAttackTime >= _attackCooldown)
        {
            _lastAttackTime = currentTime;
            PerformAttack();
        }
    }

    /// <summary>
    /// Perform an attack on the target
    /// </summary>
    private void PerformAttack()
    {
        if (_target == null || _transform == null)
            return;
        
        var targetHealth = _target.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsDead)
        {
            // Calculate hit direction and point
            var targetTransform = _target.GetComponent<Transform3D>();
            Vector3 hitDirection = Vector3.Normalize(targetTransform?.Position - _transform.Position ?? Vector3.UnitX);
            Vector3 hitPoint = targetTransform?.Position ?? Vector3.Zero;
            Vector3 hitNormal = -hitDirection; // Normal points back toward attacker
            
            // Create damage info
            var damageInfo = DamageInfo.CreateFromHit(
                _attackDamage, 
                DamageType.Melee, 
                Owner, 
                hitPoint,
                hitDirection,
                hitNormal);
            
            // Deal damage
            targetHealth.TakeDamage(damageInfo);
            
            Console.WriteLine($"[EnemyAI] {Owner?.Name} attacked {_target.Name} for {_attackDamage} damage!");
        }
    }

    /// <summary>
    /// Check if enemy has line of sight to target
    /// Uses raycasting to detect obstacles
    /// </summary>
    private bool CheckLineOfSight()
    {
        if (_transform == null || _targetTransform == null)
            return false;
        
        var physicsProvider = ServiceLocator.Get<Core.Plugins.Physics.IPhysicsProvider>();
        if (physicsProvider == null)
            return false;
        
        Vector3 origin = _transform.Position;
        Vector3 targetPos = _targetTransform.Position;
        Vector3 direction = Vector3.Normalize(targetPos - origin);
        float distance = Vector3.Distance(origin, targetPos);
        
        // Raycast toward target
        if (physicsProvider.Raycast(origin, direction, distance, out var hit))
        {
            // Check if we hit the target or something else
            if (hit.Body?.UserData is Entity hitEntity)
            {
                return hitEntity == _target;
            }
            return false;
        }
        
        // No obstacle in the way
        return true;
    }

    /// <summary>
    /// Smoothly rotate to look at a position
    /// </summary>
    private void LookAt(Vector3 targetPosition, float deltaTime)
    {
        if (_transform == null)
            return;
        
        // Calculate direction (ignoring Y for horizontal rotation)
        Vector3 direction = targetPosition - _transform.Position;
        direction.Y = 0; // Keep rotation on horizontal plane
        
        if (direction.LengthSquared() < 0.001f)
            return;
        
        direction = Vector3.Normalize(direction);
        
        // Calculate target rotation (yaw only for now)
        float targetYaw = MathF.Atan2(direction.X, direction.Z);
        
        // Convert current rotation to Euler angles
        // For simplicity, we'll just set the rotation directly
        // In a full game, you'd lerp/slerp for smooth rotation
        var euler = QuaternionToEuler(_transform.Rotation);
        euler.Y = targetYaw;
        _transform.Rotation = EulerToQuaternion(euler);
    }

    /// <summary>
    /// Convert quaternion to Euler angles
    /// </summary>
    private Vector3 QuaternionToEuler(Quaternion q)
    {
        Vector3 euler;
        
        // Pitch (X)
        float sinp = 2 * (q.W * q.X + q.Y * q.Z);
        float cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        euler.X = MathF.Atan2(sinp, cosp);
        
        // Yaw (Y)
        float siny = 2 * (q.W * q.Y - q.Z * q.X);
        float cosy = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        euler.Y = MathF.Atan2(siny, cosy);
        
        // Roll (Z)
        float sinr = 2 * (q.W * q.Z + q.X * q.Y);
        float cosr = 1 - 2 * (q.Z * q.Z + q.X * q.X);
        euler.Z = MathF.Atan2(sinr, cosr);
        
        return euler;
    }

    /// <summary>
    /// Convert Euler angles to quaternion
    /// </summary>
    private Quaternion EulerToQuaternion(Vector3 euler)
    {
        return Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
    }

    /// <summary>
    /// Called when entering a new state
    /// </summary>
    private void OnStateEnter(AIState state)
    {
        Console.WriteLine($"[EnemyAI] {Owner?.Name} entered {state} state");
        
        switch (state)
        {
            case AIState.Attack:
                // Reset attack timer when entering attack state
                var timeService = ServiceLocator.Get<Core.Services.ITimeService>();
                _lastAttackTime = timeService.TotalTime - _attackCooldown;
                break;
        }
    }

    /// <summary>
    /// Called when exiting a state
    /// </summary>
    private void OnStateExit(AIState state)
    {
        // Could add state exit logic here
    }

    /// <summary>
    /// Handle death event from Health component
    /// </summary>
    private void OnDeath(DamageInfo killingBlow)
    {
        CurrentState = AIState.Dead;
        Console.WriteLine($"[EnemyAI] {Owner?.Name} AI disabled (dead)");
    }

    /// <summary>
    /// Draw debug visualization (for editor/debug builds)
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        
        // TODO Phase 5: Draw debug visualization
        // - Detection range sphere
        // - Attack range sphere
        // - Line of sight ray
        // - Current state text
    }
}
