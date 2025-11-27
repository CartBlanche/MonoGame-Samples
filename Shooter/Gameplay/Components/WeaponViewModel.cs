using Shooter.Core.Components;
using Shooter.Core.Entities;
using System.Numerics;

namespace Shooter.Gameplay.Components;

/// <summary>
/// Attaches a weapon model to the camera for first-person view.
///
/// UNITY COMPARISON - FPS WEAPON VIEW MODELS:
///
/// Unity Approach:
/// 1. Weapon is a child GameObject of the Camera
/// 2. Uses Transform hierarchy: Camera → WeaponHolder → Weapon
/// 3. Position is relative to camera (localPosition)
/// 4. Automatically follows camera because it's a child
///
/// MonoGame Approach (This Component):
/// 1. Weapon is a separate entity (no built-in parenting)
/// 2. Must manually update weapon position every frame
/// 3. Calculate world position from camera position + offset
/// 4. Apply camera rotation to weapon
///
/// EDUCATIONAL NOTE - WHY NO PARENTING?
///
/// Unity's Transform hierarchy is convenient but has overhead:
/// - Automatic matrix recalculation on every transform change
/// - Traversing hierarchy to build world matrices
/// - Memory overhead for parent/child relationships
///
/// MonoGame's explicit approach:
/// - Only update when needed (performance control)
/// - Explicit about what's happening (learning)
/// - Can optimize for specific use cases (FPS viewmodel vs world object)
///
/// For FPS games, viewmodels have special requirements:
/// - Different FOV than world (to prevent distortion)
/// - Rendered on top of everything (depth buffer tricks)
/// - Smooth independent movement (weapon sway, recoil)
///
/// This component handles the basic case: stick weapon to camera.
/// Phase 3 will add: weapon sway, bobbing, recoil animations.
/// </summary>
public class WeaponViewModel : EntityComponent
{
    private Entity? _cameraEntity;
    private Camera? _camera;
    private Transform3D? _weaponTransform;

    /// <summary>
    /// Offset from camera position (right, down, forward in camera space).
    /// Similar to Unity's localPosition when weapon is child of camera.
    /// </summary>
    public Vector3 ViewmodelOffset { get; set; } = new Vector3(0.5f, -0.5f, -1.5f);

    /// <summary>
    /// The camera entity to follow. Must be set before Initialize() is called.
    /// </summary>
    public Entity? CameraEntity { get; set; }

    public override void Initialize()
    {
        base.Initialize();

        _weaponTransform = Owner?.GetComponent<Transform3D>();
        _cameraEntity = CameraEntity;

        if (_cameraEntity != null)
        {
            _camera = _cameraEntity.GetComponent<Camera>();
        }

        if (_camera == null)
        {
            Console.WriteLine("[WeaponViewModel] WARNING: No camera found! Weapon will not follow camera.");
        }
    }

    public override void Update(Core.Components.GameTime gameTime)
    {
        base.Update(gameTime);

        if (_camera == null || _weaponTransform == null || _cameraEntity?.Transform == null)
            return;

        // Get camera's position and direction
        var cameraPos = _camera.Position;
        var forward = Vector3.Normalize(_camera.Target - _camera.Position);

        // Calculate right and up vectors for camera space
        var worldUp = new Vector3(0, 1, 0);
        var right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
        var up = Vector3.Normalize(Vector3.Cross(forward, right));

        // Transform viewmodel offset from camera space to world space
        // ViewmodelOffset is in camera space: (right, up, forward)
        // Note: Negative Z in camera space means "in front of camera"
        var worldOffset =
            right * ViewmodelOffset.X +      // Right component
            up * ViewmodelOffset.Y +          // Up component
            forward * -ViewmodelOffset.Z;     // Forward component (negated!)

        // Position weapon relative to camera
        _weaponTransform.Position = cameraPos + worldOffset;

        // Apply camera rotation to weapon
        var cameraRot = QuaternionFromForward(forward);
        _weaponTransform.Rotation = cameraRot;

        // EDUCATIONAL NOTE - WHY THIS WORKS:
        //
        // Camera space offset (0.5, -0.5, -1.5) means:
        // - 0.5 units to the RIGHT of camera
        // - 0.5 units DOWN from camera
        // - 1.5 units IN FRONT of camera (negative Z in camera forward direction)
        //
        // Vector3.Transform(offset, rotation) converts camera-relative offset
        // to world-relative offset, accounting for where camera is looking.
        //
        // Example:
        // - Camera looking NORTH (rotation = 0°):
        //   Offset (0, 0, -1.5) → World offset (0, 0, -1.5) NORTH
        //
        // - Camera looking EAST (rotation = 90°):
        //   Offset (0, 0, -1.5) → World offset (1.5, 0, 0) EAST
        //
        // This is equivalent to Unity's transform.TransformPoint(localPosition)!
    }

    /// <summary>
    /// Create a quaternion from a forward direction vector.
    /// This assumes the up vector is (0, 1, 0).
    /// </summary>
    private Quaternion QuaternionFromForward(Vector3 forward)
    {
        // Ensure forward is normalized
        forward = Vector3.Normalize(forward);

        // Calculate right vector (cross product of world up and forward)
        var worldUp = new Vector3(0, 1, 0);
        var right = Vector3.Normalize(Vector3.Cross(worldUp, forward));

        // Recalculate up vector (cross product of forward and right)
        var up = Vector3.Cross(forward, right);

        // Build rotation matrix from basis vectors
        var rotationMatrix = new System.Numerics.Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1
        );

        // Convert matrix to quaternion
        return Quaternion.CreateFromRotationMatrix(rotationMatrix);
    }
}