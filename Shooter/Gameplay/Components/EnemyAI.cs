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
    private EnemyController? _enemyController;

    // AI parameters
    private float _detectionRange = 20f;
    private float _attackRange = 10f; // Ranged attack range (Unity default)
    private float _attackStopDistanceRatio = 0.5f; // Stop at 50% of attack range (Unity default)
    private float _moveSpeed = 3f;
    private float _turnSpeed = 5f;
    
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
    /// Stop distance ratio (0.0-1.0) relative to attack range.
    /// Unity default: 0.5 (stop at 50% of attack range)
    /// </summary>
    public float AttackStopDistanceRatio
    {
        get => _attackStopDistanceRatio;
        set => _attackStopDistanceRatio = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Initialize component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _transform = Owner?.GetComponent<Transform3D>();
        _health = Owner?.GetComponent<Health>();
        _enemyController = Owner?.GetComponent<EnemyController>();

        // Subscribe to death event
        if (_health != null)
        {
            _health.OnDeath += OnDeath;
        }

        // Reduced logging - only show critical warnings
        if (_enemyController == null)
        {
            Console.WriteLine($"[EnemyAI] WARNING: {Owner?.Name} has no EnemyController");
        }
    }

    /// <summary>
    /// Update AI behavior based on current state
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Lazy-load target transform if we have a target but no transform cached
        if (_target != null && _targetTransform == null)
        {
            _targetTransform = _target.GetComponent<Transform3D>();
            if (_targetTransform != null)
            {
                Console.WriteLine($"[EnemyAI] {Owner?.Name} successfully loaded target transform");
            }
        }

        if (_transform == null || _target == null || _targetTransform == null)
        {
            // DEBUG: Log why update is skipped (only once per second to avoid spam)
            if (_stateTimer == 0f || _stateTimer > 1f)
            {
                Console.WriteLine($"[EnemyAI] {Owner?.Name} Update skipped - Transform:{_transform != null}, Target:{_target != null}, TargetTransform:{_targetTransform != null}");
                _stateTimer = 0.01f; // Reset to avoid immediate re-log
            }
            return;
        }

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

        // DEBUG: Log detection checks every 2 seconds
        if (_stateTimer > 2f)
        {
            Console.WriteLine($"[EnemyAI] {Owner?.Name} Idle - Distance:{distanceToTarget:F1}, LOS:{_hasLineOfSight}, Range:{_detectionRange}");
            _stateTimer = 0f;
        }

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
    /// Update attack state - shoot at player
    /// </summary>
    private void UpdateAttack(float deltaTime)
    {
        if (_targetTransform == null || _transform == null || _target == null)
            return;

        float distanceToTarget = Vector3.Distance(_transform.Position, _targetTransform.Position);

        // If player moved out of range or lost line of sight, chase again
        if (distanceToTarget > _attackRange * 1.2f || !_hasLineOfSight) // Small buffer to prevent state flickering
        {
            CurrentState = AIState.Chase;
            return;
        }

        // Calculate stop distance based on attack range ratio
        float stopDistance = _attackRange * _attackStopDistanceRatio;

        // Move toward or away to maintain optimal attack distance
        if (distanceToTarget > stopDistance)
        {
            // Move closer
            Vector3 direction = Vector3.Normalize(_targetTransform.Position - _transform.Position);
            _transform.Position += direction * _moveSpeed * deltaTime;
        }
        else if (distanceToTarget < stopDistance * 0.8f)
        {
            // Back away slightly (too close)
            Vector3 direction = Vector3.Normalize(_transform.Position - _targetTransform.Position);
            _transform.Position += direction * _moveSpeed * 0.5f * deltaTime;
        }

        // Aim and fire at target
        if (_enemyController != null)
        {
            // Calculate aim point (target's center mass)
            Vector3 aimPoint = _targetTransform.Position + new Vector3(0, 0.5f, 0);

            // Orient toward target
            _enemyController.OrientTowards(aimPoint);
            _enemyController.OrientWeaponsTowards(aimPoint);

            // Try to shoot (weapon handles its own fire rate)
            _enemyController.TryAttack(aimPoint);
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

        // Calculate direction to target
        Vector3 targetPos = _targetTransform.Position + new Vector3(0, 0.5f, 0);
        Vector3 toTarget = targetPos - _transform.Position;
        Vector3 direction = Vector3.Normalize(toTarget);
        float distance = toTarget.Length();

        // Start raycast from slightly in front of enemy to avoid hitting self
        // Offset by 1.5 units forward to clear the enemy's collider
        Vector3 origin = _transform.Position + new Vector3(0, 0.5f, 0) + (direction * 1.5f);

        // Reduce distance by the offset amount
        float adjustedDistance = distance - 1.5f;
        if (adjustedDistance <= 0f)
        {
            // Target is too close to raycast offset, assume we can see them
            return true;
        }

        // Raycast toward target
        if (physicsProvider.Raycast(origin, direction, adjustedDistance, out var hit))
        {
            // We hit something - check if it's the target or an obstacle
            if (hit.Body?.UserData is Entity hitEntity)
            {
                return hitEntity == _target;
            }
            // Hit something that's not an entity (terrain, etc)
            return false;
        }

        // No obstacle in the way - clear line of sight
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
        // DEBUG: Track AI state changes
        Console.WriteLine($"[EnemyAI] {Owner?.Name} -> {state}");
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
