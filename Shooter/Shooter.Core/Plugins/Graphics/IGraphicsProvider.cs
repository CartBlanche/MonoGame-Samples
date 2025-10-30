using System.Numerics;

namespace Shooter.Core.Plugins.Graphics;

/// <summary>
/// Plugin interface for graphics rendering implementations.
/// This allows swapping between different rendering approaches.
/// 
/// DESIGN PATTERN: Provider/Strategy Pattern
/// 
/// UNITY COMPARISON:
/// Unity abstracts rendering completely - you don't directly control it.
/// MonoGame gives you full control, so we create this provider to:
/// 1. Make rendering more modular
/// 2. Allow different rendering techniques (forward, deferred, etc.)
/// 3. Support simple colored shapes initially, complex models later
/// </summary>
public interface IGraphicsProvider
{
    /// <summary>
    /// Initialize the graphics system.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Begin rendering a frame.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// End rendering a frame.
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Render a scene with a specific camera.
    /// </summary>
    void RenderScene(ICamera camera, IEnumerable<IRenderable> renderables);

    /// <summary>
    /// Draw a debug primitive (for visualization during development).
    /// </summary>
    void DrawDebugPrimitive(DebugPrimitive primitive);

    /// <summary>
    /// Set the lighting configuration.
    /// </summary>
    void SetLighting(LightingConfiguration lighting);

    /// <summary>
    /// Clean up resources.
    /// </summary>
    void Shutdown();
}

/// <summary>
/// Camera interface for rendering.
/// Provides view and projection matrices.
/// 
/// UNITY COMPARISON:
/// Similar to Unity's Camera component, but more minimal.
/// Unity handles a lot automatically; here we're explicit.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Position of the camera in world space.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Direction the camera is looking.
    /// </summary>
    Vector3 Forward { get; }

    /// <summary>
    /// Up vector of the camera.
    /// </summary>
    Vector3 Up { get; }

    /// <summary>
    /// View matrix (transforms world space to view space).
    /// In Unity, this is Camera.worldToCameraMatrix
    /// </summary>
    Matrix4x4 ViewMatrix { get; }

    /// <summary>
    /// Projection matrix (transforms view space to clip space).
    /// In Unity, this is Camera.projectionMatrix
    /// </summary>
    Matrix4x4 ProjectionMatrix { get; }

    /// <summary>
    /// Field of view in degrees (for perspective cameras).
    /// Similar to Camera.fieldOfView in Unity.
    /// </summary>
    float FieldOfView { get; set; }

    /// <summary>
    /// Near clipping plane.
    /// Similar to Camera.nearClipPlane in Unity.
    /// </summary>
    float NearPlane { get; }

    /// <summary>
    /// Far clipping plane.
    /// Similar to Camera.farClipPlane in Unity.
    /// </summary>
    float FarPlane { get; }
}

/// <summary>
/// Interface for objects that can be rendered.
/// Any entity that wants to be drawn should implement this or have a component that does.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// World transformation matrix for this renderable.
    /// </summary>
    Matrix4x4 WorldMatrix { get; }

    /// <summary>
    /// The material/appearance of this renderable.
    /// </summary>
    Material Material { get; }

    /// <summary>
    /// The geometry to render.
    /// </summary>
    Mesh Mesh { get; }

    /// <summary>
    /// Whether this renderable is currently visible.
    /// </summary>
    bool Visible { get; }

    /// <summary>
    /// Render layer for sorting and filtering.
    /// </summary>
    int Layer { get; }
}

/// <summary>
/// Material definition for rendering.
/// Contains colors, textures, and shader parameters.
/// 
/// UNITY COMPARISON:
/// Similar to Unity's Material class, but simplified.
/// For this educational project, we start with basic properties.
/// </summary>
public class Material
{
    /// <summary>
    /// Base color/albedo.
    /// Similar to Material._Color in Unity.
    /// </summary>
    public Vector4 Color { get; set; } = Vector4.One;

    /// <summary>
    /// Emissive color (self-illumination).
    /// Similar to Material._EmissionColor in Unity.
    /// </summary>
    public Vector4 EmissiveColor { get; set; } = Vector4.Zero;

    /// <summary>
    /// Specular intensity (shininess).
    /// </summary>
    public float Specular { get; set; } = 0.5f;

    /// <summary>
    /// Texture (optional, null for solid colors).
    /// </summary>
    public object? Texture { get; set; }

    /// <summary>
    /// Whether this material is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Create a simple colored material (no texture).
    /// Useful for initial prototyping with colored boxes.
    /// </summary>
    public static Material CreateSolid(Vector4 color)
    {
        return new Material { Color = color };
    }

    /// <summary>
    /// Create an emissive (glowing) material.
    /// Useful for muzzle flashes, enemy eyes, etc.
    /// </summary>
    public static Material CreateEmissive(Vector4 color, float intensity = 1.0f)
    {
        return new Material
        {
            Color = color,
            EmissiveColor = color * intensity
        };
    }
}

/// <summary>
/// Mesh geometry data.
/// 
/// NOTE: For the initial implementation, we'll use primitive shapes.
/// Later, this can be extended to load .fbx models.
/// </summary>
public class Mesh
{
    public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();
    public int[] Indices { get; set; } = Array.Empty<int>();
    public Vector3[] Normals { get; set; } = Array.Empty<Vector3>();
    public Vector2[] TexCoords { get; set; } = Array.Empty<Vector2>();

    /// <summary>
    /// Create a cube mesh.
    /// Useful for placeholder objects.
    /// </summary>
    public static Mesh CreateCube(float size = 1.0f)
    {
        float half = size / 2f;
        return new Mesh
        {
            Vertices = new[]
            {
                // Front face
                new Vector3(-half, -half, half), new Vector3(half, -half, half),
                new Vector3(half, half, half), new Vector3(-half, half, half),
                // Back face
                new Vector3(-half, -half, -half), new Vector3(-half, half, -half),
                new Vector3(half, half, -half), new Vector3(half, -half, -half),
                // Top face
                new Vector3(-half, half, -half), new Vector3(-half, half, half),
                new Vector3(half, half, half), new Vector3(half, half, -half),
                // Bottom face
                new Vector3(-half, -half, -half), new Vector3(half, -half, -half),
                new Vector3(half, -half, half), new Vector3(-half, -half, half),
                // Right face
                new Vector3(half, -half, -half), new Vector3(half, half, -half),
                new Vector3(half, half, half), new Vector3(half, -half, half),
                // Left face
                new Vector3(-half, -half, -half), new Vector3(-half, -half, half),
                new Vector3(-half, half, half), new Vector3(-half, half, -half)
            },
            Indices = new[]
            {
                0, 1, 2, 0, 2, 3,    // Front
                4, 5, 6, 4, 6, 7,    // Back
                8, 9, 10, 8, 10, 11, // Top
                12, 13, 14, 12, 14, 15, // Bottom
                16, 17, 18, 16, 18, 19, // Right
                20, 21, 22, 20, 22, 23  // Left
            }
        };
    }

    /// <summary>
    /// Create a sphere mesh (icosphere approximation).
    /// </summary>
    public static Mesh CreateSphere(float radius = 0.5f, int subdivisions = 2)
    {
        // TODO: Implement icosphere generation
        // For now, return a simple octahedron
        return new Mesh();
    }

    /// <summary>
    /// Create a capsule mesh.
    /// Useful for character representations.
    /// </summary>
    public static Mesh CreateCapsule(float radius = 0.5f, float height = 2.0f)
    {
        // TODO: Implement capsule mesh
        return new Mesh();
    }
}

/// <summary>
/// Debug primitive for visualization.
/// Useful for debugging physics, AI paths, etc.
/// </summary>
public struct DebugPrimitive
{
    public DebugPrimitiveType Type;
    public Vector3 Position;
    public Vector3 End; // For lines
    public float Radius; // For spheres
    public Vector4 Color;
    public float Duration; // How long to display (0 = one frame)
}

public enum DebugPrimitiveType
{
    Line,
    Sphere,
    Box,
    Capsule
}

/// <summary>
/// Lighting configuration for the scene.
/// </summary>
public class LightingConfiguration
{
    /// <summary>
    /// Ambient light color (base illumination).
    /// Similar to RenderSettings.ambientLight in Unity.
    /// </summary>
    public Vector3 AmbientColor { get; set; } = new Vector3(0.3f, 0.3f, 0.3f);

    /// <summary>
    /// Directional lights (like sun).
    /// Similar to Directional Light in Unity.
    /// </summary>
    public List<DirectionalLight> DirectionalLights { get; set; } = new();

    /// <summary>
    /// Point lights.
    /// Similar to Point Light in Unity.
    /// </summary>
    public List<PointLight> PointLights { get; set; } = new();
}

public struct DirectionalLight
{
    public Vector3 Direction;
    public Vector3 Color;
    public float Intensity;
}

public struct PointLight
{
    public Vector3 Position;
    public Vector3 Color;
    public float Intensity;
    public float Range;
}
