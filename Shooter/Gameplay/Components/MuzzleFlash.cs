using System;
using System.Numerics;
using Shooter.Core.Components;
using Shooter.Core.Entities;

namespace Shooter.Gameplay.Components;

/// <summary>
/// Muzzle flash effect component for weapons.
///
/// UNITY COMPARISON - PARTICLE SYSTEM FOR MUZZLE FLASH:
///
/// Unity Approach:
/// 1. Use ParticleSystem component with emission burst
/// 2. Attach to weapon's "MuzzlePoint" transform
/// 3. Play() on fire, automatic lifetime management
/// 4. Often includes light component for flash lighting
///
/// MonoGame Approach (This Component):
/// 1. Simple billboard sprite (quad facing camera)
/// 2. Manual flash timing and fade-out
/// 3. Position at weapon barrel offset
/// 4. Optional random rotation for variety
///
/// EDUCATIONAL NOTE - WHY SIMPLE BILLBOARD?
///
/// For an educational project, we start with the simplest approach:
/// - Billboard = 2D quad that always faces the camera
/// - Flash duration = 1-2 frames (50-100ms)
/// - Random rotation = visual variety without complexity
///
/// This is actually how many older FPS games did muzzle flashes!
/// Modern games use particle systems, but the principle is the same:
/// "Show bright flash briefly at weapon barrel when firing"
///
/// Phase 4 can upgrade this to a proper particle system if desired.
/// </summary>
public class MuzzleFlash : EntityComponent
{
    private float _flashTimer = 0f;
    private float _flashDuration = 0.05f; // 50ms flash (about 3 frames at 60fps)
    private bool _isFlashing = false;
    private float _randomRotation = 0f;

    /// <summary>
    /// Position offset from weapon origin to barrel (where flash appears).
    /// In camera space: (right, up, forward)
    /// </summary>
    public Vector3 BarrelOffset { get; set; } = new Vector3(0f, 0f, -0.8f);

    /// <summary>
    /// Scale of the flash sprite
    /// </summary>
    public float FlashScale { get; set; } = 0.3f;

    /// <summary>
    /// Whether the flash is currently visible
    /// </summary>
    public bool IsVisible => _isFlashing;

    /// <summary>
    /// Color/tint of the flash (typically yellow-orange)
    /// </summary>
    public Vector4 FlashColor { get; set; } = new Vector4(1f, 0.8f, 0.4f, 1f);

    public override void Initialize()
    {
        base.Initialize();
        _isFlashing = false;
    }

    public override void Update(Core.Components.GameTime gameTime)
    {
        base.Update(gameTime);

        // Update flash timer
        if (_isFlashing)
        {
            _flashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_flashTimer <= 0f)
            {
                _isFlashing = false;
            }
        }
    }

    /// <summary>
    /// Trigger the muzzle flash effect
    /// </summary>
    public void Flash()
    {
        _isFlashing = true;
        _flashTimer = _flashDuration;

        // Random rotation for visual variety (0-360 degrees)
        Random random = new Random();
        _randomRotation = (float)(random.NextDouble() * Math.PI * 2);

        // Debug output
        Console.WriteLine($"[MuzzleFlash] Flash triggered! Duration: {_flashDuration}s");

        // EDUCATIONAL NOTE:
        // In Unity, you'd call particleSystem.Play()
        // Here, we just set a flag and timer - the renderer will check IsVisible
    }

    /// <summary>
    /// Get the world position where the flash should be rendered
    /// </summary>
    public Vector3 GetWorldPosition(Vector3 cameraPosition, Vector3 cameraTarget)
    {
        // Get camera direction vectors
        var forward = Vector3.Normalize(cameraTarget - cameraPosition);
        var worldUp = new Vector3(0, 1, 0);
        var right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
        var up = Vector3.Normalize(Vector3.Cross(forward, right));

        // Transform barrel offset from camera space to world space
        var worldOffset =
            right * BarrelOffset.X +
            up * BarrelOffset.Y +
            forward * -BarrelOffset.Z;

        return cameraPosition + worldOffset;
    }

    /// <summary>
    /// Get the rotation to apply to the flash sprite
    /// </summary>
    public float GetRotation()
    {
        return _randomRotation;
    }

    // EDUCATIONAL NOTE - RENDERING APPROACH:
    //
    // For Phase 2, we'll use a simple approach:
    // 1. Check if MuzzleFlash.IsVisible
    // 2. If yes, render a camera-facing quad at GetWorldPosition()
    // 3. Apply FlashColor as tint
    // 4. Apply GetRotation() around the camera's forward axis
    //
    // In Phase 4, we can upgrade to:
    // - Additive blending (flash adds light to scene)
    // - Particle system with multiple flash sprites
    // - Point light that flashes with the sprite
    // - Smoke particles after flash
}
