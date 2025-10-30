using System.Numerics;
using Shooter.Core.Entities;

namespace Shooter.Core.Components;

/// <summary>
/// Base class for all components in the Entity-Component System.
/// 
/// UNITY COMPARISON:
/// In Unity, you inherit from MonoBehaviour and attach to GameObjects.
/// In MonoGame, we use this EntityComponent base class attached to Entities.
/// 
/// Key differences:
/// - No automatic lifecycle (Awake/Start/Update) - you must call these explicitly
/// - No automatic scene serialization - we use JSON
/// - More explicit control over execution order
/// 
/// NOTE: Renamed from GameComponent to EntityComponent to avoid namespace collision
/// with Microsoft.Xna.Framework.GameComponent.
/// </summary>
public abstract class EntityComponent
{
    /// <summary>
    /// The entity that owns this component.
    /// Similar to Unity's gameObject reference.
    /// </summary>
    public Entity? Owner { get; internal set; }

    /// <summary>
    /// Whether this component is active and should be updated.
    /// Similar to Unity's enabled property.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Called once when the component is first added to an entity.
    /// Similar to Unity's Awake() method.
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// Called every frame for game logic updates.
    /// Similar to Unity's Update() method.
    /// 
    /// NOTE: In MonoGame, you receive GameTime which contains:
    /// - TotalGameTime: Total elapsed time since game start
    /// - ElapsedGameTime: Time since last Update call
    /// Use ElapsedGameTime.TotalSeconds for deltaTime equivalent
    /// </summary>
    /// <param name="gameTime">Timing information (similar to Time.deltaTime in Unity)</param>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Called for rendering and visual updates.
    /// Separate from Update to support fixed timestep physics.
    /// 
    /// NOTE: MonoGame separates Update (logic) from Draw (rendering).
    /// Unity combines these in Update/LateUpdate.
    /// </summary>
    /// <param name="gameTime">Timing information for interpolation</param>
    public virtual void Draw(GameTime gameTime) { }

    /// <summary>
    /// Called when the component is being destroyed.
    /// Similar to Unity's OnDestroy() method.
    /// Use this to clean up resources, unsubscribe from events, etc.
    /// </summary>
    public virtual void OnDestroy() { }

    /// <summary>
    /// Helper method to get another component on the same entity.
    /// Similar to Unity's GetComponent<T>() method.
    /// </summary>
    protected T? GetComponent<T>() where T : EntityComponent
    {
        return Owner?.GetComponent<T>();
    }

    /// <summary>
    /// Helper to get all components of a type on this entity.
    /// Similar to Unity's GetComponents<T>() method.
    /// </summary>
    protected IEnumerable<T> GetComponents<T>() where T : EntityComponent
    {
        return Owner?.GetComponents<T>() ?? Enumerable.Empty<T>();
    }
}

/// <summary>
/// Timing information for update and draw calls.
/// This is MonoGame's equivalent to Unity's Time class.
/// 
/// USAGE:
/// - gameTime.ElapsedGameTime.TotalSeconds = Time.deltaTime in Unity
/// - gameTime.TotalGameTime.TotalSeconds = Time.time in Unity
/// </summary>
public class GameTime
{
    public TimeSpan TotalGameTime { get; set; }
    public TimeSpan ElapsedGameTime { get; set; }
    public bool IsRunningSlowly { get; set; }

    public GameTime()
    {
        TotalGameTime = TimeSpan.Zero;
        ElapsedGameTime = TimeSpan.Zero;
    }

    public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
    {
        TotalGameTime = totalGameTime;
        ElapsedGameTime = elapsedGameTime;
    }
}
