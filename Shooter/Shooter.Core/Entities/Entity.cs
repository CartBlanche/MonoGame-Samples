using Shooter.Core.Components;

namespace Shooter.Core.Entities;

/// <summary>
/// Entity class that represents a game object in the world.
/// 
/// UNITY COMPARISON:
/// This is our equivalent to Unity's GameObject. Key differences:
/// 
/// Unity GameObject:
/// - Has built-in Transform component (always present)
/// - Components added via AddComponent<T>()
/// - Managed by Unity's scene system
/// - Serialized to scene files automatically
/// 
/// MonoGame Entity:
/// - Transform is just another component (add it yourself)
/// - Manual component management
/// - You control creation/destruction
/// - Serialized via JSON (see SceneLoader)
/// 
/// USAGE EXAMPLE:
/// var player = new Entity("Player");
/// player.AddComponent<Transform3D>();
/// player.AddComponent<PlayerController>();
/// player.Initialize();
/// </summary>
public class Entity
{
    private readonly Dictionary<Type, EntityComponent> _components = new();
    private readonly List<EntityComponent> _componentList = new();
    private bool _isInitialized;

    /// <summary>
    /// Unique identifier for this entity.
    /// Similar to Unity's instanceID, but we manage it ourselves.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Name of the entity for debugging and identification.
    /// Similar to Unity's GameObject.name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Whether this entity is active in the scene.
    /// Similar to Unity's GameObject.activeSelf
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Tag for categorizing entities (e.g., "Player", "Enemy", "Projectile").
    /// Similar to Unity's GameObject.tag
    /// </summary>
    public string Tag { get; set; } = "Untagged";

    /// <summary>
    /// Layer for collision filtering and rendering.
    /// Similar to Unity's GameObject.layer
    /// </summary>
    public int Layer { get; set; } = 0;

    /// <summary>
    /// Quick access to the Transform component if one exists.
    /// In Unity, transform is always available. Here it's optional but recommended.
    /// </summary>
    public Transform3D? Transform => GetComponent<Transform3D>();

    public Entity(string name = "Entity")
    {
        Name = name;
    }

    #region Component Management

    /// <summary>
    /// Adds a component of type T to this entity.
    /// Similar to Unity's AddComponent<T>()
    /// 
    /// NOTE: Unlike Unity, this creates the component immediately.
    /// You may need to call Initialize() on the entity after adding components.
    /// </summary>
    public T AddComponent<T>() where T : EntityComponent, new()
    {
        var type = typeof(T);
        
        if (_components.ContainsKey(type))
        {
            throw new InvalidOperationException(
                $"Entity '{Name}' already has a component of type {type.Name}");
        }

        var component = new T { Owner = this };
        _components[type] = component;
        _componentList.Add(component);

        // If entity is already initialized, initialize the new component immediately
        if (_isInitialized)
        {
            component.Initialize();
        }

        return component;
    }

    /// <summary>
    /// Gets the component of type T attached to this entity.
    /// Similar to Unity's GetComponent<T>()
    /// </summary>
    public T? GetComponent<T>() where T : EntityComponent
    {
        var type = typeof(T);
        return _components.TryGetValue(type, out var component) ? component as T : null;
    }

    /// <summary>
    /// Gets all components of type T (including derived types).
    /// Similar to Unity's GetComponents<T>()
    /// </summary>
    public IEnumerable<T> GetComponents<T>() where T : EntityComponent
    {
        return _componentList.OfType<T>();
    }

    /// <summary>
    /// Checks if this entity has a component of type T.
    /// Similar to Unity's TryGetComponent<T>()
    /// </summary>
    public bool HasComponent<T>() where T : EntityComponent
    {
        return _components.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Removes a component from this entity.
    /// Similar to Unity's Destroy(component)
    /// </summary>
    public bool RemoveComponent<T>() where T : EntityComponent
    {
        var type = typeof(T);
        if (_components.TryGetValue(type, out var component))
        {
            component.OnDestroy();
            _components.Remove(type);
            _componentList.Remove(component);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all components attached to this entity.
    /// Useful for iteration during Update/Draw calls.
    /// </summary>
    public IReadOnlyList<EntityComponent> GetAllComponents()
    {
        return _componentList.AsReadOnly();
    }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Initializes this entity and all its components.
    /// Call this after adding all components.
    /// 
    /// In Unity, this happens automatically when a GameObject is created in a scene.
    /// In MonoGame, you must call this explicitly.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        foreach (var component in _componentList)
        {
            component.Initialize();
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Updates all enabled components on this entity.
    /// 
    /// NOTE: In Unity, Update() is called automatically by the engine.
    /// In MonoGame, you must call this from your game loop or scene manager.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (!Active) return;

        foreach (var component in _componentList)
        {
            if (component.Enabled)
            {
                component.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Draws all enabled components on this entity.
    /// 
    /// NOTE: MonoGame separates Update (logic) from Draw (rendering).
    /// Call this during your game's Draw phase.
    /// </summary>
    public void Draw(GameTime gameTime)
    {
        if (!Active) return;

        foreach (var component in _componentList)
        {
            if (component.Enabled)
            {
                component.Draw(gameTime);
            }
        }
    }

    /// <summary>
    /// Destroys this entity and all its components.
    /// Similar to Unity's Destroy(gameObject)
    /// </summary>
    public void Destroy()
    {
        foreach (var component in _componentList)
        {
            component.OnDestroy();
        }

        _components.Clear();
        _componentList.Clear();
        Active = false;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Creates a child entity with this entity as parent.
    /// Sets up the Transform hierarchy automatically if both have Transform3D components.
    /// </summary>
    public Entity CreateChild(string name)
    {
        var child = new Entity(name);
        
        // If both have transforms, set up parent-child relationship
        var parentTransform = Transform;
        if (parentTransform != null)
        {
            var childTransform = child.AddComponent<Transform3D>();
            childTransform.Parent = parentTransform;
        }

        return child;
    }

    public override string ToString()
    {
        return $"Entity: {Name} (ID: {Id}, Components: {_componentList.Count})";
    }

    #endregion
}
