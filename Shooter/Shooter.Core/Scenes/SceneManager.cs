using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Core.Components;
using Shooter.Core.Entities;

namespace Shooter.Core.Scenes;

/// <summary>
/// Manages scene loading, unloading, and entity lifecycle.
/// 
/// UNITY COMPARISON:
/// Unity has a built-in SceneManager that loads .unity scene files.
/// This is our equivalent that loads scenes from JSON files.
/// 
/// USAGE:
/// var sceneManager = new SceneManager();
/// await sceneManager.LoadSceneAsync("Levels/MainScene.json");
/// sceneManager.Update(gameTime);
/// sceneManager.Draw(gameTime);
/// </summary>
public class SceneManager
{
    private Scene? _activeScene;
    private readonly Dictionary<string, Func<Entity, EntityComponent>> _componentFactory = new();

    public Scene? ActiveScene => _activeScene;

    public SceneManager()
    {
        RegisterDefaultComponentTypes();
    }

    /// <summary>
    /// Register a component type so it can be created from JSON.
    /// 
    /// EXAMPLE:
    /// sceneManager.RegisterComponentType<PlayerController>("PlayerController");
    /// </summary>
    public void RegisterComponentType<T>(string typeName) where T : EntityComponent, new()
    {
        _componentFactory[typeName] = (entity) => entity.AddComponent<T>();
    }

    /// <summary>
    /// Load a scene from a JSON file.
    /// </summary>
    public async Task<Scene> LoadSceneAsync(string jsonPath)
    {
        // Unload current scene if any
        UnloadScene();

        // Read and parse JSON
        string json = await File.ReadAllTextAsync(jsonPath);
        var sceneData = JsonConvert.DeserializeObject<JObject>(json);

        if (sceneData == null)
        {
            throw new InvalidOperationException($"Failed to parse scene file: {jsonPath}");
        }

        // Create new scene
        _activeScene = new Scene(sceneData["name"]?.ToString() ?? "Untitled");

        // Load entities
        var entitiesArray = sceneData["entities"] as JArray;
        if (entitiesArray != null)
        {
            foreach (var entityData in entitiesArray)
            {
                LoadEntity(entityData as JObject);
            }
        }

        // Initialize all entities in the scene
        _activeScene.Initialize();

        return _activeScene;
    }

    /// <summary>
    /// Unload the current scene and clean up all entities.
    /// Similar to Unity's SceneManager.LoadScene() (which unloads the previous scene).
    /// </summary>
    public void UnloadScene()
    {
        if (_activeScene != null)
        {
            _activeScene.Destroy();
            _activeScene = null;
        }
    }

    /// <summary>
    /// Update the active scene.
    /// Call this from your game's Update method.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _activeScene?.Update(gameTime);
    }

    /// <summary>
    /// Draw the active scene.
    /// Call this from your game's Draw method.
    /// </summary>
    public void Draw(GameTime gameTime)
    {
        _activeScene?.Draw(gameTime);
    }

    private void LoadEntity(JObject? entityData)
    {
        if (entityData == null) return;

        string name = entityData["name"]?.ToString() ?? "Entity";
        var entity = new Entity(name);

        // Set properties
        if (entityData["tag"] != null)
            entity.Tag = entityData["tag"]!.ToString();

        if (entityData["layer"] != null)
            entity.Layer = entityData["layer"]!.Value<int>();

        // Load components
        var componentsArray = entityData["components"] as JArray;
        if (componentsArray != null)
        {
            foreach (var componentData in componentsArray)
            {
                LoadComponent(entity, componentData as JObject);
            }
        }

        _activeScene?.AddEntity(entity);
    }

    private void LoadComponent(Entity entity, JObject? componentData)
    {
        if (componentData == null) return;

        string? typeName = componentData["type"]?.ToString();
        if (string.IsNullOrEmpty(typeName)) return;

        // Create component using factory
        if (_componentFactory.TryGetValue(typeName, out var factory))
        {
            var component = factory(entity);

            // Special handling for Transform3D
            if (component is Transform3D transform)
            {
                if (componentData["position"] is JArray posArray && posArray.Count == 3)
                {
                    transform.LocalPosition = new Vector3(
                        posArray[0]!.Value<float>(),
                        posArray[1]!.Value<float>(),
                        posArray[2]!.Value<float>()
                    );
                }

                if (componentData["rotation"] is JArray rotArray && rotArray.Count == 4)
                {
                    transform.LocalRotation = new Quaternion(
                        rotArray[0]!.Value<float>(),
                        rotArray[1]!.Value<float>(),
                        rotArray[2]!.Value<float>(),
                        rotArray[3]!.Value<float>()
                    );
                }

                if (componentData["scale"] is JArray scaleArray && scaleArray.Count == 3)
                {
                    transform.LocalScale = new Vector3(
                        scaleArray[0]!.Value<float>(),
                        scaleArray[1]!.Value<float>(),
                        scaleArray[2]!.Value<float>()
                    );
                }
            }

            // TODO: Add serialization support for custom component properties
            // For now, components can override a Deserialize method if needed
        }
        else
        {
            Console.WriteLine($"Warning: Unknown component type '{typeName}'");
        }
    }

    private void RegisterDefaultComponentTypes()
    {
        // Register core components
        RegisterComponentType<Transform3D>("Transform3D");

        // Gameplay components will be registered when those assemblies are loaded
    }
}

/// <summary>
/// Represents a scene/level in the game.
/// Similar to Unity's Scene class.
/// </summary>
public class Scene
{
    private readonly List<Entity> _entities = new();
    private bool _isInitialized;

    public string Name { get; }
    public IReadOnlyList<Entity> Entities => _entities.AsReadOnly();

    public Scene(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Add an entity to this scene.
    /// Similar to instantiating a GameObject in Unity.
    /// </summary>
    public void AddEntity(Entity entity)
    {
        _entities.Add(entity);

        // If scene is already initialized, initialize the entity immediately
        if (_isInitialized)
        {
            entity.Initialize();
        }
    }

    /// <summary>
    /// Remove an entity from this scene.
    /// Similar to Destroy(gameObject) in Unity.
    /// </summary>
    public void RemoveEntity(Entity entity)
    {
        entity.Destroy();
        _entities.Remove(entity);
    }

    /// <summary>
    /// Find an entity by name.
    /// Similar to GameObject.Find() in Unity.
    /// </summary>
    public Entity? FindEntityByName(string name)
    {
        return _entities.FirstOrDefault(e => e.Name == name);
    }

    /// <summary>
    /// Find all entities with a specific tag.
    /// Similar to GameObject.FindGameObjectsWithTag() in Unity.
    /// </summary>
    public IEnumerable<Entity> FindEntitiesByTag(string tag)
    {
        return _entities.Where(e => e.Tag == tag);
    }

    /// <summary>
    /// Find first entity with a specific component type.
    /// Similar to FindObjectOfType<T>() in Unity.
    /// </summary>
    public Entity? FindEntityWithComponent<T>() where T : EntityComponent
    {
        return _entities.FirstOrDefault(e => e.HasComponent<T>());
    }

    /// <summary>
    /// Find all entities with a specific component type.
    /// Similar to FindObjectsOfType<T>() in Unity.
    /// </summary>
    public IEnumerable<Entity> FindEntitiesWithComponent<T>() where T : EntityComponent
    {
        return _entities.Where(e => e.HasComponent<T>());
    }

    /// <summary>
    /// Initialize all entities in the scene.
    /// </summary>
    public void Initialize()
    {
        foreach (var entity in _entities)
        {
            entity.Initialize();
        }
        _isInitialized = true;
    }

    /// <summary>
    /// Update all entities in the scene.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Update entities (some may be destroyed during update)
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            if (_entities[i].Active)
            {
                _entities[i].Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Draw all entities in the scene.
    /// </summary>
    public void Draw(GameTime gameTime)
    {
        foreach (var entity in _entities)
        {
            if (entity.Active)
            {
                entity.Draw(gameTime);
            }
        }
    }

    /// <summary>
    /// Destroy all entities and clean up the scene.
    /// </summary>
    public void Destroy()
    {
        foreach (var entity in _entities)
        {
            entity.Destroy();
        }
        _entities.Clear();
        _isInitialized = false;
    }
}
