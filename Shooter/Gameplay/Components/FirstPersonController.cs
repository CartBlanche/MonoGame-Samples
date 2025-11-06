using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shooter.Core.Components;
using Shooter.Core.Services;
using Shooter.Core.Plugins.Physics;
using System.Numerics;

namespace Shooter.Gameplay.Components;

/// <summary>
/// First-person shooter controller for player movement and camera control.
/// 
/// UNITY COMPARISON - CHARACTER CONTROLLER:
/// Unity has a built-in CharacterController component that handles:
/// - WASD movement with collision detection
/// - Gravity and ground detection
/// - Step offset for climbing stairs
/// 
/// In MonoGame, we build this ourselves using:
/// - InputService for keyboard/mouse input
/// - Camera component for rotation (looking around)
/// - Transform3D for position (movement)
/// - Rigidbody for physics-based movement (optional)
/// 
/// EDUCATIONAL NOTE - FPS CONTROLS:
/// 
/// Classic FPS controls use:
/// - WASD for movement (relative to camera direction)
/// - Mouse for looking (yaw = horizontal, pitch = vertical)
/// - Space for jump
/// - Shift for sprint
/// - Mouse locked to center (captured)
/// 
/// The key trick is moving RELATIVE to where you're looking:
/// - W moves forward in the direction you're facing
/// - A/D strafe left/right perpendicular to facing
/// - Mouse X rotates your view horizontally (yaw)
/// - Mouse Y tilts your view up/down (pitch)
/// 
/// We clamp pitch to prevent looking more than 90° up/down
/// (prevents camera from flipping upside-down).
/// </summary>
public class FirstPersonController : Core.Components.EntityComponent
{
    // Stance system
    public enum PlayerStance { Standing, Crouching }
    private PlayerStance _stance = PlayerStance.Standing;
    public PlayerStance Stance => _stance;

    // Camera bobbing
    private float _bobTimer = 0f;
    private float _bobFrequency = 7.5f; // Bobbing speed
    private float _bobAmplitude = 0.05f; // Bobbing height
    private float _bobSmoothing = 8.0f; // Smoothing for transition

    // Death zone settings
    private float _killHeight = -50.0f; // Y position below which player dies
    // Fall damage settings
    private float _fallDamageThreshold = -12.0f; // Minimum Y velocity to trigger damage
    private float _fallDamageMultiplier = 5.0f;   // Damage per unit of velocity over threshold
    private float _lastVerticalVelocity = 0f;
    private bool _wasGroundedLastFrame = true;
    // Movement settings
    private float _moveSpeed = 5.0f;
    private float _sprintMultiplier = 1.8f;
    private float _jumpForce = 8.0f;
    private float _crouchSpeedMultiplier = 0.5f; // Crouch movement speed
    private float _crouchHeight = 0.9f; // Camera height when crouched
    private float _standHeight = 1.6f; // Camera height when standing
    private float _crouchTransitionSpeed = 8.0f; // How fast camera/capsule transitions

    // Look settings
    private float _mouseSensitivity = 0.15f;
    private float _maxPitchAngle = 89f; // Prevent looking straight up/down (gimbal lock)
    private float _cameraHeightOffset = 1.6f; // Camera height above player position (head height)
    private bool _invertMouseY = false; // Invert Y-axis for mouse look

    // State
    private float _currentYaw = 0f;   // Horizontal rotation (left/right)
    private float _currentPitch = 0f; // Vertical rotation (up/down)
    private bool _isGrounded = false;
    private bool _mouseCaptured = false;
    private bool _firstUpdate = true; // Skip input checks on first frame
    private bool _isCrouching = false;
    private float _targetCameraHeight = 1.6f;
    private float _currentCameraHeight = 1.6f;

    // Ground detection
    private float _groundCheckDistance = 0.1f; // Distance to check below player for ground
    private float _groundCheckRadius = 0.3f;   // Radius of sphere for ground check

    // Cached components
    private Camera? _camera;
    private Transform3D? _transform;
    private Core.Components.Rigidbody? _rigidbody;
    private IPhysicsProvider? _physicsProvider;

    // Services
    private IInputService? _inputService;
    private IAudioService? _audioService;

    // Footstep audio
    private float _footstepTimer = 0f;
    private float _footstepInterval = 0.45f; // Time between footsteps (seconds)
    private bool _wasMovingLastFrame = false;

    /// <summary>
    /// Movement speed in units per second.
    /// Unity's CharacterController uses meters/second by default.
    /// </summary>
    public float MoveSpeed
    {
        get => _moveSpeed;
        set => _moveSpeed = value;
    }

    /// <summary>
    /// Sprint speed multiplier (applied when holding Shift).
    /// Default 1.8x means sprinting at 180% normal speed.
    /// </summary>
    public float SprintMultiplier
    {
        get => _sprintMultiplier;
        set => _sprintMultiplier = value;
    }

    /// <summary>
    /// Upward force applied when jumping.
    /// Higher = jump higher. Typical range: 5-12.
    /// </summary>
    public float JumpForce
    {
        get => _jumpForce;
        set => _jumpForce = value;
    }

    /// <summary>
    /// Mouse sensitivity for looking around.
    /// Lower = slower/smoother, Higher = faster/twitchy.
    /// Typical range: 0.05 - 0.5
    /// </summary>
    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set => _mouseSensitivity = value;
    }

    /// <summary>
    /// Camera height offset above player position (simulates head height).
    /// Typical value: 1.6 for human height.
    /// </summary>
    public float CameraHeightOffset
    {
        get => _cameraHeightOffset;
        set => _cameraHeightOffset = value;
    }

    /// <summary>
    /// Whether to invert the Y-axis for mouse look (up moves camera down, down moves camera up).
    /// Some players prefer inverted controls.
    /// </summary>
    public bool InvertMouseY
    {
        get => _invertMouseY;
        set => _invertMouseY = value;
    }

    /// <summary>
    /// Whether the mouse is currently captured (locked to center).
    /// </summary>
    public bool IsMouseCaptured => _mouseCaptured;

    /// <summary>
    /// Initialize the controller.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        // Cache components
        _camera = Owner?.GetComponent<Camera>();
        _transform = Owner?.GetComponent<Transform3D>();
        _rigidbody = Owner?.GetComponent<Core.Components.Rigidbody>();

        // Get services
        _inputService = ServiceLocator.Get<IInputService>();
        _physicsProvider = ServiceLocator.Get<IPhysicsProvider>();
        _audioService = ServiceLocator.Get<IAudioService>();

        // Initialize yaw/pitch to look forward along negative Z axis (MonoGame default)
        _currentYaw = 180f;  // 180° = looking down -Z axis
        _currentPitch = 0f;  // 0° = looking straight ahead (not up/down)

        // Capture mouse by default for FPS games
        CaptureMouse();

        if (_camera == null)
        {
            Console.WriteLine("[FirstPersonController] WARNING: No Camera component found on entity. Mouse look will not work.");
        }

        if (_transform == null)
        {
            Console.WriteLine("[FirstPersonController] ERROR: No Transform3D component found on entity. Movement will not work.");
        }
    }

    /// <summary>
    /// Update controller each frame.
    /// </summary>
    public override void Update(Core.Components.GameTime gameTime)
    {
        base.Update(gameTime);

        // Death zone check
        if (_transform != null && _transform.Position.Y < _killHeight)
        {
            KillPlayer();
        }

        if (_inputService == null || _transform == null)
        {
            Console.WriteLine("[FPS] ERROR: InputService or Transform is null!");
            return;
        }

        // Skip input processing on first frame (prevents false Escape detection)
        if (_firstUpdate)
        {
            _firstUpdate = false;
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        // ...existing code...
        // Handle mouse capture toggle (press Escape to release, click to recapture)
        HandleMouseCapture();
        // Only process input if mouse is captured (prevents moving when in menus)
        if (_mouseCaptured)
        {
            // Handle crouch/stand input and smooth transition
            HandleCrouch(deltaTime);
            // Sync camera position with transform FIRST (before rotation calculations)
            if (_camera != null)
            {
                // Smoothly interpolate camera height for crouch/stand
                _currentCameraHeight = MathHelper.Lerp(_currentCameraHeight, _targetCameraHeight, deltaTime * _crouchTransitionSpeed);
                float bobOffset = 0f;
                if (IsPlayerMoving() && _isGrounded)
                {
                    _bobTimer += deltaTime * _bobFrequency;
                    bobOffset = (float)Math.Sin(_bobTimer) * _bobAmplitude;
                }
                else
                {
                    // Smoothly reset bobbing when not moving
                    _bobTimer = MathHelper.Lerp(_bobTimer, 0f, deltaTime * _bobSmoothing);
                    bobOffset = 0f;
                }
                _camera.Position = _transform.Position + new System.Numerics.Vector3(0, _currentCameraHeight + bobOffset, 0);
            }
            // Handle looking (mouse movement)
            HandleMouseLook(deltaTime);
            // Handle movement (WASD)
            bool isMoving = IsPlayerMoving();
            HandleMovement(deltaTime);
            // Footstep audio logic
            if (isMoving && _isGrounded)
            {
                _footstepTimer += deltaTime;
                if (_footstepTimer >= _footstepInterval)
                {
                    PlayFootstepSound();
                    _footstepTimer = 0f;
                }
            }
            else
            {
                _footstepTimer = 0f;
            }
            _wasMovingLastFrame = isMoving;
            // Update ground detection
            UpdateGroundDetection();
            // Handle jumping (Space)
            HandleJump();
        }
        // Track vertical velocity for fall damage
        if (_rigidbody != null)
        {
            _lastVerticalVelocity = _rigidbody.Velocity.Y;
        }
        // Detect landing and apply fall damage
        if (!_wasGroundedLastFrame && _isGrounded)
        {
            if (_lastVerticalVelocity < _fallDamageThreshold)
            {
                float damage = MathF.Abs(_lastVerticalVelocity + _fallDamageThreshold) * _fallDamageMultiplier;
                ApplyFallDamage(damage);
            }
        }
        _wasGroundedLastFrame = _isGrounded;
    }

    /// <summary>
    /// Kill the player if they fall below kill height. Replace with respawn or game over logic.
    /// </summary>
    private void KillPlayer()
    {
        // TODO: Integrate with game state/respawn system. For now, print to console.
        Console.WriteLine($"[FPS] Player killed: fell below kill height {_killHeight}");
        // Example: Owner?.GetComponent<Health>()?.Kill();
    }

    /// <summary>
    /// Apply fall damage to the player. Replace with health system integration.
    /// </summary>
    private void ApplyFallDamage(float damage)
    {
        // TODO: Integrate with health system. For now, print to console.
        Console.WriteLine($"[FPS] Fall damage applied: {damage:0.0}");
        // Example: Owner?.GetComponent<Health>()?.TakeDamage(damage);
    }
    /// <summary>
    /// Handle crouch/stand toggle and smooth transition.
    /// Ctrl to crouch, release to stand. Smoothly transitions camera/capsule.
    /// </summary>
    private void HandleCrouch(float deltaTime)
    {
        if (_inputService == null)
            return;
        // Hold Ctrl to crouch, release to stand
        bool crouchKey = _inputService.IsKeyDown(Keys.LeftControl) || _inputService.IsKeyDown(Keys.RightControl);
        if (crouchKey && _stance != PlayerStance.Crouching)
        {
            _stance = PlayerStance.Crouching;
            _isCrouching = true;
            _targetCameraHeight = _crouchHeight;
            // TODO: Resize capsule/collider if needed
        }
        else if (!crouchKey && _stance != PlayerStance.Standing)
        {
            _stance = PlayerStance.Standing;
            _isCrouching = false;
            _targetCameraHeight = _standHeight;
            // TODO: Resize capsule/collider if needed
        }
    }

    /// <summary>
    /// Update ground detection using physics raycast.
    /// 
    /// EDUCATIONAL NOTE - GROUND DETECTION:
    /// Unity's CharacterController has built-in isGrounded property.
    /// In MonoGame, we implement it ourselves using raycasts:
    /// 
    /// 1. Cast a ray downward from player position
    /// 2. If it hits something within a small distance, we're grounded
    /// 3. This prevents jumping while in air (bunny-hopping)
    /// 
    /// We use a small sphere cast instead of a single ray for more reliable
    /// detection on uneven terrain.
    /// </summary>
    private void UpdateGroundDetection()
    {
        if (_physicsProvider == null || _transform == null)
        {
            _isGrounded = true; // Assume grounded if no physics
            return;
        }
        // Cast downward from player position
        System.Numerics.Vector3 origin = _transform.Position;
        System.Numerics.Vector3 direction = new System.Numerics.Vector3(0, -1, 0); // Downward
        float maxDistance = _groundCheckDistance;
        // Use sphere cast for more reliable ground detection
        // This catches edges and slopes better than a single ray
        if (_physicsProvider.SphereCast(origin, _groundCheckRadius, direction, maxDistance, out var hit))
        {
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }
    }

    /// <summary>
    /// Handle mouse capture/release.
    /// Press Escape to release mouse, click to recapture.
    /// </summary>
    private void HandleMouseCapture()
    {
        if (_inputService == null)
            return;

        // Release mouse on Escape key
        if (_inputService.IsKeyPressed(Keys.Escape))
        {
            ReleaseMouse();
        }

        // Recapture on left click (when not captured)
        if (!_mouseCaptured && _inputService.IsMouseButtonPressed(MouseButton.Left))
        {
            CaptureMouse();
        }
    }

    /// <summary>
    /// Handle mouse look (camera rotation).
    /// 
    /// EDUCATIONAL NOTE - MOUSE DELTA:
    /// We use the mouse movement delta (change since last frame)
    /// rather than absolute position. This gives smooth rotation.
    /// 
    /// We accumulate yaw (horizontal) and pitch (vertical) over time,
    /// then apply them to the camera rotation each frame.
    /// </summary>
    private void HandleMouseLook(float deltaTime)
    {
        if (_inputService == null || _camera == null)
            return;

        // Get mouse movement delta
        var mouseDelta = _inputService.MouseDelta;

        // Convert System.Numerics.Vector2 to XNA Vector2
        var mouseDeltaXna = new Microsoft.Xna.Framework.Vector2(mouseDelta.X, mouseDelta.Y);

        if (mouseDeltaXna == Microsoft.Xna.Framework.Vector2.Zero)
            return;

        // Apply sensitivity
        float yawDelta = -mouseDeltaXna.X * _mouseSensitivity; // Negative X for correct left/right
        float pitchDelta = mouseDeltaXna.Y * _mouseSensitivity; // Positive Y = look down (standard FPS)

        // Apply inversion if enabled
        if (_invertMouseY)
        {
            pitchDelta = -pitchDelta;
        }

        // Accumulate rotation
        _currentYaw += yawDelta;
        _currentPitch += pitchDelta;

        // Clamp pitch to prevent over-rotation (gimbal lock)
        // This keeps you from looking more than 89° up or down
        _currentPitch = Math.Clamp(_currentPitch, -_maxPitchAngle, _maxPitchAngle);

        // Apply rotation to camera
        // Yaw rotates around Y axis (left/right)
        // Pitch rotates around X axis (up/down)
        _camera.Rotate(_currentYaw, _currentPitch);
    }

    /// <summary>
    /// Handle WASD movement.
    /// 
    /// EDUCATIONAL NOTE - RELATIVE MOVEMENT:
    /// Movement is relative to where the camera is looking.
    /// 
    /// We calculate:
    /// - Forward direction (where camera is facing, ignoring vertical tilt)
    /// - Right direction (perpendicular to forward)
    /// 
    /// Then we combine WASD input into a movement vector:
    /// - W = move forward
    /// - S = move backward
    /// - A = move left (strafe)
    /// - D = move right (strafe)
    /// 
    /// We normalize the result so diagonal movement (W+A) isn't faster.
    /// </summary>
    private void HandleMovement(float deltaTime)
    {
        if (_inputService == null || _transform == null)
            return;

        // Calculate movement direction based on input
        System.Numerics.Vector3 inputDirection = System.Numerics.Vector3.Zero;

        if (_inputService.IsKeyDown(Keys.W))
            inputDirection.Z -= 1f; // Forward
        if (_inputService.IsKeyDown(Keys.S))
            inputDirection.Z += 1f; // Backward
        if (_inputService.IsKeyDown(Keys.A))
            inputDirection.X -= 1f; // Left
        if (_inputService.IsKeyDown(Keys.D))
            inputDirection.X += 1f; // Right

        // No movement input - early out
        if (inputDirection == System.Numerics.Vector3.Zero)
            return;

        // Normalize to prevent faster diagonal movement
        inputDirection = System.Numerics.Vector3.Normalize(inputDirection);

        // Calculate forward and right vectors based on camera yaw
        // We ignore pitch (vertical tilt) for ground-based movement
        float yawRadians = _currentYaw * (MathF.PI / 180f);

        System.Numerics.Vector3 forward = new System.Numerics.Vector3(
            MathF.Sin(yawRadians),
            0,
            MathF.Cos(yawRadians)
        );

        System.Numerics.Vector3 right = new System.Numerics.Vector3(
            MathF.Cos(yawRadians),
            0,
            -MathF.Sin(yawRadians)
        );

        // Combine input with camera-relative directions
        System.Numerics.Vector3 moveDirection =
            (forward * -inputDirection.Z) + // Forward/back (inverted Z)
            (right * inputDirection.X);      // Left/right

        // Apply speed
        float speed = _moveSpeed;
        // Stance modifier
        if (_stance == PlayerStance.Crouching)
        {
            speed *= _crouchSpeedMultiplier;
        }
        // Sprint modifier (hold Shift)
        if (_inputService.IsKeyDown(Keys.LeftShift) || _inputService.IsKeyDown(Keys.RightShift))
        {
            speed *= _sprintMultiplier;
        }
        // Air control: reduce movement speed if not grounded
        if (!_isGrounded)
        {
            speed *= 0.35f; // Air control multiplier (tweakable)
        }
        // Calculate velocity
        System.Numerics.Vector3 velocity = moveDirection * speed;

        // Apply movement
        if (_rigidbody != null)
        {
            // Physics-based movement: Set velocity on rigidbody
            // Preserve Y velocity (gravity/jumping)
            var currentVelocity = _rigidbody.Velocity;
            _rigidbody.Velocity = new System.Numerics.Vector3(
                velocity.X,
                currentVelocity.Y, // Keep vertical velocity
                velocity.Z
            );
        }
        else
        {
            // Direct movement: Update position directly
            _transform.Position += velocity * deltaTime;
        }
    }

    /// <summary>
    /// Handle jumping (Space bar).
    /// 
    /// EDUCATIONAL NOTE - JUMPING:
    /// We apply an upward impulse (instant velocity change).
    /// Gravity will naturally pull the player back down.
    /// 
    /// Ground detection prevents bunny-hopping (spamming jump in air).
    /// </summary>
    private void HandleJump()
    {
        if (_inputService == null)
            return;

        // Check for jump input
        if (!_inputService.IsKeyPressed(Keys.Space))
            return;

        // Check if grounded (now properly detected via raycast)
        if (!_isGrounded)
            return; // Can't jump in air

        // Apply jump force
        if (_rigidbody != null)
        {
            // Physics-based jump: Apply upward impulse
            _rigidbody.AddImpulse(new System.Numerics.Vector3(0, _jumpForce, 0));
        }
        else if (_transform != null)
        {
            // Direct jump: Instantly move up
            // (Not realistic, but works without physics)
            _transform.Position += new System.Numerics.Vector3(0, _jumpForce * 0.1f, 0);
        }
    }

    /// <summary>
    /// Capture the mouse (lock to center, hide cursor).
    /// Standard for FPS games.
    /// </summary>
    public void CaptureMouse()
    {
        _mouseCaptured = true;

        if (_inputService != null)
        {
            _inputService.IsMouseLocked = true;
        }

        Microsoft.Xna.Framework.Input.Mouse.SetPosition(
            Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width / 2,
            Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height / 2
        );
        // TODO: Hide cursor (MonoGame doesn't have built-in API, needs platform-specific code)
    }

    /// <summary>
    /// Release the mouse (unlock from center, show cursor).
    /// Used for menus and UI interaction.
    /// </summary>
    public void ReleaseMouse()
    {
        _mouseCaptured = false;

        if (_inputService != null)
        {
            _inputService.IsMouseLocked = false;
        }

        // TODO: Show cursor
    }

    /// <summary>
    /// Set the camera rotation directly.
    /// Useful for cutscenes or resetting view.
    /// </summary>
    public void SetRotation(float yaw, float pitch)
    {
        _currentYaw = yaw;
        _currentPitch = Math.Clamp(pitch, -_maxPitchAngle, _maxPitchAngle);

        if (_camera != null)
        {
            _camera.Rotate(_currentYaw, _currentPitch);
        }
    }

    /// <summary>
    /// Returns true if player is moving (WASD keys held)
    /// </summary>
    private bool IsPlayerMoving()
    {
        if (_inputService == null)
            return false;
        return _inputService.IsKeyDown(Keys.W) || _inputService.IsKeyDown(Keys.A) || _inputService.IsKeyDown(Keys.S) || _inputService.IsKeyDown(Keys.D);
    }
    /// <summary>
    /// Play footstep sound using audio service
    /// </summary>
    private void PlayFootstepSound()
    {
        if (_audioService != null && _transform != null)
        {
            // TODO: Use surface type for varied sounds
            _audioService.PlaySound("footstep", _transform.Position, 1.0f);
        }
    }
}