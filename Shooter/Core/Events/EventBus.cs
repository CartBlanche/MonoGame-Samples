using System.Numerics;
using Shooter.Core.Entities;

namespace Shooter.Core.Events;

/// <summary>
/// Event bus for decoupled communication between game systems.
/// 
/// UNITY COMPARISON:
/// The Unity FPS sample uses a custom EventManager for game events.
/// This is the MonoGame equivalent with similar functionality.
/// 
/// USAGE EXAMPLE:
/// // Subscribe to an event
/// EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
/// 
/// // Publish an event
/// EventBus.Publish(new PlayerDiedEvent { Position = player.Position });
/// 
/// // Unsubscribe (important to prevent memory leaks!)
/// EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
/// 
/// BEST PRACTICE:
/// Always unsubscribe in OnDestroy() or when the listener is destroyed.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _eventHandlers = new();

    /// <summary>
    /// Subscribe to an event type.
    /// The handler will be called whenever the event is published.
    /// </summary>
    public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
    {
        var eventType = typeof(TEvent);

        if (!_eventHandlers.ContainsKey(eventType))
        {
            _eventHandlers[eventType] = new List<Delegate>();
        }

        _eventHandlers[eventType].Add(handler);
    }

    /// <summary>
    /// Unsubscribe from an event type.
    /// Always call this when the listener is destroyed to prevent memory leaks.
    /// </summary>
    public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
    {
        var eventType = typeof(TEvent);

        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);

            // Clean up empty handler lists
            if (handlers.Count == 0)
            {
                _eventHandlers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// Publish an event to all subscribers.
    /// Similar to Unity's EventManager.Broadcast()
    /// </summary>
    public static void Publish<TEvent>(TEvent eventData) where TEvent : IGameEvent
    {
        var eventType = typeof(TEvent);

        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            // Create a copy to avoid issues if handlers modify the list
            var handlersCopy = handlers.ToList();

            foreach (var handler in handlersCopy)
            {
                try
                {
                    (handler as Action<TEvent>)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other handlers
                    Console.WriteLine($"Error in event handler for {eventType.Name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Clear all event subscriptions.
    /// Useful when changing scenes or restarting the game.
    /// </summary>
    public static void Clear()
    {
        _eventHandlers.Clear();
    }

    /// <summary>
    /// Get the number of subscribers for a specific event type.
    /// Useful for debugging.
    /// </summary>
    public static int GetSubscriberCount<TEvent>() where TEvent : IGameEvent
    {
        var eventType = typeof(TEvent);
        return _eventHandlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }
}

/// <summary>
/// Base interface for all game events.
/// Marker interface for type safety.
/// </summary>
public interface IGameEvent
{
}

// ===== GAME EVENTS (ported from Unity FPS sample) =====

/// <summary>
/// Published when the player dies.
/// Similar to PlayerDeathEvent in Unity sample.
/// </summary>
public struct PlayerDiedEvent : IGameEvent
{
    public Vector3 Position;
}

/// <summary>
/// Published when an enemy is killed.
/// Similar to EnemyKillEvent in Unity sample.
/// </summary>
public struct EnemyKilledEvent : IGameEvent
{
    public Entity Enemy;
    public Entity? Killer; // null if environmental death
    public Vector3 Position;
}

/// <summary>
/// Published when damage is dealt.
/// Similar to DamageEvent in Unity sample.
/// </summary>
public struct DamageDealtEvent : IGameEvent
{
    public Entity Target;
    public Entity? Source; // null if environmental damage
    public float Damage;
    public Vector3 HitPosition;
    public Vector3 HitNormal;
}

/// <summary>
/// Published when a weapon is fired.
/// </summary>
public struct WeaponFiredEvent : IGameEvent
{
    public Entity Weapon;
    public Entity Owner;
    public Vector3 MuzzlePosition;
    public Vector3 Direction;
}

/// <summary>
/// Published when a pickup item is collected.
/// Similar to PickupEvent in Unity sample.
/// </summary>
public struct ItemPickedUpEvent : IGameEvent
{
    public Entity Item;
    public Entity Collector;
    public string ItemType; // "Health", "Ammo", "Weapon", etc.
}

/// <summary>
/// Published when an objective is updated.
/// Similar to ObjectiveUpdateEvent in Unity sample.
/// </summary>
public struct ObjectiveUpdatedEvent : IGameEvent
{
    public string ObjectiveId;
    public float Progress; // 0.0 to 1.0
    public bool IsCompleted;
    public string Description;
}

/// <summary>
/// Published when all objectives are completed.
/// Similar to AllObjectivesCompletedEvent in Unity sample.
/// </summary>
public struct AllObjectivesCompletedEvent : IGameEvent
{
    public float CompletionTime;
}

/// <summary>
/// Published for UI messages/notifications.
/// Similar to DisplayMessageEvent in Unity sample.
/// </summary>
public struct DisplayMessageEvent : IGameEvent
{
    public string Message;
    public float DisplayDuration;
    public MessageType Type;
}

public enum MessageType
{
    Info,
    Warning,
    Success,
    Objective
}

/// <summary>
/// Published when the game state changes.
/// </summary>
public struct GameStateChangedEvent : IGameEvent
{
    public GameState OldState;
    public GameState NewState;
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Victory
}
