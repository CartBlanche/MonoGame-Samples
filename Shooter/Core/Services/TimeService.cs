using Shooter.Core.Components;

namespace Shooter.Core.Services;

/// <summary>
/// Time service for game timing, delta time, and time scaling.
/// 
/// UNITY COMPARISON:
/// This replaces Unity's Time class which is a static singleton.
/// MonoGame provides GameTime in Update()/Draw() methods, but we wrap it
/// in a service for:
/// 1. Global access without passing GameTime everywhere
/// 2. Time scaling support (slow motion, pause)
/// 3. Fixed timestep tracking for physics
/// 
/// EDUCATIONAL NOTE ON TIMING:
/// 
/// Game loops typically use two types of time steps:
/// 
/// 1. VARIABLE TIME STEP (what Update() uses):
///    - Each frame can take different amounts of time
///    - Good for rendering, animation, input
///    - Use DeltaTime to make movement frame-rate independent
///    - Example: position += velocity * DeltaTime
/// 
/// 2. FIXED TIME STEP (what physics should use):
///    - Physics runs at constant intervals (e.g., 60 FPS = 0.0166s)
///    - Ensures deterministic physics simulation
///    - Unity uses FixedUpdate() for this
///    - We'll implement this in Phase 2 when we add the game loop manager
/// 
/// TIME SCALING:
/// Unity's Time.timeScale allows slow motion effects, pausing, etc.
/// When TimeScale = 0.5, everything runs at half speed.
/// This is useful for:
/// - Slow motion effects (bullet time)
/// - Game pause (set to 0)
/// - Fast forward (set > 1)
/// </summary>
public class TimeService : ITimeService
{
    private float _totalTime;
    private float _deltaTime;
    private float _unscaledDeltaTime;
    private float _timeScale = 1.0f;
    private float _fixedDeltaTime = 1.0f / 60.0f; // 60 FPS physics
    private int _frameCount;
    
    /// <summary>
    /// Total elapsed time since game start (affected by TimeScale).
    /// Similar to Time.time in Unity.
    /// </summary>
    public float TotalTime => _totalTime;
    
    /// <summary>
    /// Time elapsed since last frame (affected by TimeScale).
    /// Similar to Time.deltaTime in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// This is THE most important value for frame-rate independent movement.
    /// 
    /// BAD (frame-rate dependent):
    ///   position.X += 5; // Moves 5 units per frame (fast on 144Hz, slow on 30Hz)
    /// 
    /// GOOD (frame-rate independent):
    ///   position.X += 5 * DeltaTime; // Moves 5 units per second regardless of FPS
    /// 
    /// If running at 60 FPS, DeltaTime ≈ 0.0166 seconds
    /// If running at 30 FPS, DeltaTime ≈ 0.0333 seconds
    /// The movement speed remains constant!
    /// </summary>
    public float DeltaTime => _deltaTime;
    
    /// <summary>
    /// Time elapsed since last frame (NOT affected by TimeScale).
    /// Similar to Time.unscaledDeltaTime in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// Use this for things that should continue even when game is paused:
    /// - UI animations
    /// - Pause menu timers
    /// - Debug overlays
    /// </summary>
    public float UnscaledDeltaTime => _unscaledDeltaTime;
    
    /// <summary>
    /// Scale at which time passes (default = 1.0).
    /// Similar to Time.timeScale in Unity.
    /// 
    /// Examples:
    /// - 0.0 = Paused
    /// - 0.5 = Half speed (slow motion)
    /// - 1.0 = Normal speed
    /// - 2.0 = Double speed
    /// 
    /// EDUCATIONAL NOTE:
    /// This affects DeltaTime but NOT UnscaledDeltaTime.
    /// Physics simulation should respect this.
    /// </summary>
    public float TimeScale
    {
        get => _timeScale;
        set => _timeScale = Math.Max(0, value); // Prevent negative time
    }
    
    /// <summary>
    /// Fixed time step for physics simulation (default = 1/60 second).
    /// Similar to Time.fixedDeltaTime in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// Physics engines work best with constant time steps.
    /// Variable time steps can cause:
    /// - Jittery movement
    /// - Objects phasing through walls
    /// - Non-deterministic behavior
    /// 
    /// Unity calls FixedUpdate() at this interval.
    /// We'll implement accumulator-based fixed timestep in Game.cs
    /// </summary>
    public float FixedDeltaTime
    {
        get => _fixedDeltaTime;
        set => _fixedDeltaTime = Math.Max(0.001f, value); // Minimum 0.001s (1000 FPS)
    }
    
    /// <summary>
    /// Number of frames rendered since game start.
    /// Similar to Time.frameCount in Unity.
    /// 
    /// Useful for:
    /// - Debug logging
    /// - Every-N-frames logic
    /// - Performance profiling
    /// </summary>
    public int FrameCount => _frameCount;
    
    /// <summary>
    /// Approximate frames per second.
    /// Calculated as 1 / UnscaledDeltaTime.
    /// </summary>
    public float FPS => _unscaledDeltaTime > 0 ? 1.0f / _unscaledDeltaTime : 0;
    
    /// <summary>
    /// Update the time service with the latest frame timing.
    /// Call this once per frame from Game.Update().
    /// </summary>
    /// <param name="gameTime">GameTime from MonoGame's Update method</param>
    public void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        // Get the raw (unscaled) time delta
        _unscaledDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Apply time scale to get scaled delta
        _deltaTime = _unscaledDeltaTime * _timeScale;
        
        // Accumulate total time (only scaled time)
        _totalTime += _deltaTime;
        
        // Increment frame counter
        _frameCount++;
    }
    
    /// <summary>
    /// Pause the game (sets TimeScale to 0).
    /// </summary>
    public void Pause()
    {
        _timeScale = 0;
    }
    
    /// <summary>
    /// Resume the game (sets TimeScale to 1).
    /// </summary>
    public void Resume()
    {
        _timeScale = 1.0f;
    }
    
    /// <summary>
    /// Check if game is paused.
    /// </summary>
    public bool IsPaused => _timeScale == 0;
    
    /// <summary>
    /// Set slow motion effect.
    /// </summary>
    /// <param name="scale">Speed multiplier (0.5 = half speed, 0.25 = quarter speed)</param>
    public void SetSlowMotion(float scale)
    {
        _timeScale = Math.Clamp(scale, 0, 1.0f);
    }
    
    /// <summary>
    /// Reset time tracking (useful for scene transitions).
    /// </summary>
    public void Reset()
    {
        _totalTime = 0;
        _deltaTime = 0;
        _unscaledDeltaTime = 0;
        _frameCount = 0;
        _timeScale = 1.0f;
    }
}
