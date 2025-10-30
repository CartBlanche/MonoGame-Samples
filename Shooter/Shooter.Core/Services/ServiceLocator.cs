using System.Numerics;
using Shooter.Core.Plugins.Physics;
using Shooter.Core.Plugins.Graphics;
using Shooter.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Shooter.Core.Services;

/// <summary>
/// Service Locator pattern for accessing core game services.
/// 
/// DESIGN PATTERN: Service Locator
/// Provides global access to services without tight coupling.
/// 
/// UNITY COMPARISON:
/// Unity provides services through static classes (Physics, Input, etc.)
/// We use this pattern to:
/// 1. Make services swappable (especially physics and graphics providers)
/// 2. Support dependency injection
/// 3. Make testing easier
/// 
/// USAGE:
/// var physics = ServiceLocator.Get<IPhysicsProvider>();
/// physics.Raycast(origin, direction, maxDistance, out hit);
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    private static bool _isInitialized = false;

    /// <summary>
    /// Initialize the service locator.
    /// Must be called before using any services.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("ServiceLocator is already initialized");
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Register a service implementation.
    /// </summary>
    public static void Register<TService>(TService implementation) where TService : class
    {
        var serviceType = typeof(TService);

        if (_services.ContainsKey(serviceType))
        {
            throw new InvalidOperationException(
                $"Service of type {serviceType.Name} is already registered");
        }

        _services[serviceType] = implementation;
    }

    /// <summary>
    /// Register a service with an interface type.
    /// Useful when you want to register implementations of interfaces.
    /// </summary>
    public static void Register<TInterface, TImplementation>(TImplementation implementation)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var serviceType = typeof(TInterface);

        if (_services.ContainsKey(serviceType))
        {
            throw new InvalidOperationException(
                $"Service of type {serviceType.Name} is already registered");
        }

        _services[serviceType] = implementation;
    }

    /// <summary>
    /// Get a registered service.
    /// Throws if the service is not found.
    /// </summary>
    public static TService Get<TService>() where TService : class
    {
        var serviceType = typeof(TService);

        if (_services.TryGetValue(serviceType, out var service))
        {
            return (TService)service;
        }

        throw new InvalidOperationException(
            $"Service of type {serviceType.Name} is not registered. " +
            $"Make sure to register it during initialization.");
    }

    /// <summary>
    /// Try to get a registered service.
    /// Returns null if not found.
    /// </summary>
    public static TService? TryGet<TService>() where TService : class
    {
        var serviceType = typeof(TService);
        return _services.TryGetValue(serviceType, out var service) ? (TService)service : null;
    }

    /// <summary>
    /// Check if a service is registered.
    /// </summary>
    public static bool IsRegistered<TService>() where TService : class
    {
        return _services.ContainsKey(typeof(TService));
    }

    /// <summary>
    /// Unregister a service.
    /// </summary>
    public static void Unregister<TService>() where TService : class
    {
        _services.Remove(typeof(TService));
    }

    /// <summary>
    /// Clear all services.
    /// Useful when shutting down or restarting the game.
    /// </summary>
    public static void Clear()
    {
        _services.Clear();
        _isInitialized = false;
    }
}

/// <summary>
/// Input service for handling keyboard, mouse, and gamepad input.
/// Implemented in InputService.cs - see that file for full API documentation.
/// </summary>
public interface IInputService
{
    void Initialize(Point screenCenter);
    void Update();
    
    // Keyboard
    bool IsKeyDown(Keys key);
    bool IsKeyPressed(Keys key);
    bool IsKeyReleased(Keys key);
    
    // Mouse
    System.Numerics.Vector2 MousePosition { get; }
    System.Numerics.Vector2 MouseDelta { get; }
    float MouseScrollDelta { get; }
    bool IsMouseLocked { get; set; }
    bool IsMouseButtonDown(MouseButton button);
    bool IsMouseButtonPressed(MouseButton button);
    bool IsMouseButtonReleased(MouseButton button);
    
    // GamePad
    bool IsButtonDown(Buttons button);
    bool IsButtonPressed(Buttons button);
    System.Numerics.Vector2 GetLeftThumbstick();
    System.Numerics.Vector2 GetRightThumbstick();
    float GetLeftTrigger();
    float GetRightTrigger();
    bool IsGamePadConnected();
    
    // Helper methods
    System.Numerics.Vector2 GetMovementInput();
    bool IsJumpPressed();
    bool IsFireDown();
    bool IsAimDown();
}

/// <summary>
/// Audio service for playing sounds and music.
/// 
/// UNITY COMPARISON:
/// Similar to Unity's AudioSource.PlayOneShot() and AudioSource.Play()
/// MonoGame uses SoundEffect and SoundEffectInstance.
/// We wrap them here for easier management.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Play a one-shot sound effect at a position.
    /// Similar to AudioSource.PlayClipAtPoint() in Unity.
    /// </summary>
    void PlaySound(string soundName, System.Numerics.Vector3? position = null, float volume = 1.0f);

    /// <summary>
    /// Play background music.
    /// </summary>
    void PlayMusic(string musicName, bool loop = true);

    /// <summary>
    /// Stop all sounds.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Set master volume (0.0 to 1.0).
    /// </summary>
    float MasterVolume { get; set; }
}

/// <summary>
/// Time service for game timing and delta time.
/// 
/// UNITY COMPARISON:
/// Similar to Unity's Time class.
/// Provides delta time, total time, time scale, etc.
/// </summary>
public interface ITimeService
{
    /// <summary>
    /// Time in seconds since the start of the game.
    /// Similar to Time.time in Unity.
    /// </summary>
    float TotalTime { get; }

    /// <summary>
    /// Time in seconds since the last frame.
    /// Similar to Time.deltaTime in Unity.
    /// </summary>
    float DeltaTime { get; }

    /// <summary>
    /// Fixed timestep for physics updates.
    /// Similar to Time.fixedDeltaTime in Unity.
    /// </summary>
    float FixedDeltaTime { get; }

    /// <summary>
    /// Time scale multiplier (1.0 = normal, 0.5 = half speed, 2.0 = double speed).
    /// Similar to Time.timeScale in Unity.
    /// </summary>
    float TimeScale { get; set; }

    /// <summary>
    /// Unscaled delta time (ignores TimeScale).
    /// Similar to Time.unscaledDeltaTime in Unity.
    /// </summary>
    float UnscaledDeltaTime { get; }

    /// <summary>
    /// Update the time service. Called by the game loop.
    /// </summary>
    void Update(Microsoft.Xna.Framework.GameTime gameTime);
}
