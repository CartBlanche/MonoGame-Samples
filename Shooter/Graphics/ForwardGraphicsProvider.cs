using Microsoft.Xna.Framework.Graphics;
using Shooter.Core.Plugins.Graphics;
using Microsoft.Xna.Framework;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;
using Matrix = System.Numerics.Matrix4x4;
using Shooter.Gameplay.Components;

namespace Shooter.Graphics;

/// <summary>
/// Simple forward rendering provider for MonoGame.
/// 
/// UNITY COMPARISON - RENDERING PIPELINE:
/// 
/// Unity uses one of several rendering pipelines:
/// - Built-in Render Pipeline (older, forward or deferred)
/// - Universal Render Pipeline (URP) - optimized, forward renderer
/// - High Definition Render Pipeline (HDRP) - AAA quality, deferred
/// 
/// We're implementing a FORWARD RENDERER similar to Unity's built-in or URP:
/// 1. For each object:
///    a. Set material properties (color, texture)
///    b. Set transformation matrices (world, view, projection)
///    c. Draw the mesh
/// 2. Lighting is calculated in the pixel shader for each pixel
/// 
/// FORWARD vs DEFERRED RENDERING:
/// 
/// Forward (what we're doing):
/// - Simple, easy to understand
/// - Good for few lights (1-4)
/// - Each object drawn once
/// - Lighting calculated during draw
/// 
/// Deferred (more advanced):
/// - Better for many lights (10+)
/// - Draws objects to multiple render targets first (G-buffer)
/// - Then applies lighting in screen space
/// - More complex but more efficient for lots of lights
/// 
/// For an educational FPS, forward rendering is perfect!
/// <summary>
public class ForwardGraphicsProvider : IGraphicsProvider
{
    // Temporary font for HUD
    private SpriteFont? _hudFont;

    // Reference to player weapon controller for HUD
    private Shooter.Gameplay.Components.WeaponController? _playerWeaponController;

    /// <summary>
    /// Set the font to use for HUD overlay.
    /// </summary>
    public void SetHUDFont(SpriteFont font)
    {
        _hudFont = font;
    }

    /// <summary>
    /// Set the player weapon controller for HUD overlay.
    /// </summary>
    public void SetPlayerWeaponController(Shooter.Gameplay.Components.WeaponController controller)
    {
        _playerWeaponController = controller;
    }
    private GraphicsDevice? _graphicsDevice;
    private BasicEffect? _basicEffect;
    private RasterizerState? _rasterizerState;
    private DepthStencilState? _depthStencilState;
    
    // Primitive meshes (cached for performance)
    private PrimitiveMesh? _cubeMesh;
    private PrimitiveMesh? _sphereMesh = null;
    private PrimitiveMesh? _capsuleMesh = null;
    
    // Current rendering state
    private ICamera? _currentCamera;
    private LightingConfiguration _lighting = new();
    
    /// <summary>
    /// Initialize the graphics provider with a GraphicsDevice.
    /// 
    /// EDUCATIONAL NOTE - GRAPHICS DEVICE:
    /// In Unity, you never directly access the graphics device.
    /// In MonoGame, GraphicsDevice is your gateway to the GPU:
    /// - Draw calls
    /// - Shader parameters
    /// - Render states
    /// - Textures and buffers
    /// 
    /// Think of it as the "connection to the graphics card".
    /// </summary>
    public void Initialize()
    {
        Console.WriteLine("[Graphics] Initializing Forward Renderer");
        
        // Set up default lighting (similar to Unity's default scene lighting)
        _lighting = new LightingConfiguration
        {
            AmbientColor = new Vector3(0.6f, 0.6f, 0.6f), // Higher ambient to show colors better
            DirectionalLights = new List<Core.Plugins.Graphics.DirectionalLight>
            {
                // Main directional light (like Unity's default sun)
                new Core.Plugins.Graphics.DirectionalLight
                {
                    Direction = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.5f)), // From top-left
                    Color = new Vector3(1f, 1f, 1f), // White light
                    Intensity = 0.5f // Reduced intensity to preserve colors
                },
                // Fill light from opposite side
                new Core.Plugins.Graphics.DirectionalLight
                {
                    Direction = Vector3.Normalize(new Vector3(0.5f, -0.5f, 0.5f)),
                    Color = new Vector3(0.8f, 0.8f, 1f), // Slightly blue
                    Intensity = 0.2f // Reduced fill light
                }
            }
        };
        
        Console.WriteLine($"[Graphics] Set up {_lighting.DirectionalLights.Count} directional lights");
        
        // We'll get the GraphicsDevice from SetGraphicsDevice() call
        // This is a lazy initialization pattern
    }
    
    /// <summary>
    /// Set the GraphicsDevice for rendering.
    /// Must be called before rendering can begin.
    /// </summary>
    public void SetGraphicsDevice(GraphicsDevice device)
    {
        if (_graphicsDevice == null)
        {
            InitializeDevice(device);
        }
    }
    
    /// <summary>
    /// Begin rendering a frame.
    /// Call this before drawing anything.
    /// 
    /// EDUCATIONAL NOTE - FRAME STRUCTURE:
    /// 
    /// Typical rendering frame in MonoGame:
    /// 1. BeginFrame() - clear buffers, set up render states
    /// 2. RenderScene() - draw all objects
    /// 3. DrawDebugPrimitives() - draw debug visualization
    /// 4. EndFrame() - present to screen
    /// 
    /// Unity does all of this automatically in Camera.Render().
    /// MonoGame gives you explicit control.
    /// </summary>
    public void BeginFrame()
    {
        // Graphics device will be set when RenderScene is first called
        // This is a lazy initialization pattern
    }
    
    /// <summary>
    /// Render all objects in the scene.
    ///
    /// EDUCATIONAL NOTE - RENDERING LOOP:
    ///
    /// For each object we want to draw:
    /// 1. Calculate world matrix (position/rotation/scale)
    /// 2. Set material properties (color, lighting)
    /// 3. Set transformation matrices on shader
    /// 4. Draw the mesh (send vertices to GPU)
    ///
    /// Unity hides this loop inside Camera.Render() and handles:
    /// - Frustum culling (only draw visible objects)
    /// - Sorting (transparent objects last)
    /// - Batching (combine similar objects)
    ///
    /// We're doing a simple version for education.
    /// Phase 2 will add culling and sorting.
    ///
    /// PHASE 2 UPDATE - MODEL RENDERING:
    /// Now supports both:
    /// - IRenderable (custom procedural meshes - cubes, spheres)
    /// - ModelMeshRenderer (FBX models from Content Pipeline)
    /// </summary>
    public void RenderScene(ICamera camera, IEnumerable<IRenderable> renderables)
    {
        if (_graphicsDevice == null || _basicEffect == null)
            return;

        _currentCamera = camera;

        // Set up camera matrices on the effect
        // These transform vertices from world space to screen space
        _basicEffect.View = ToXnaMatrix(camera.ViewMatrix);
        _basicEffect.Projection = ToXnaMatrix(camera.ProjectionMatrix);

        // Set up lighting
        ConfigureLighting(_basicEffect, _lighting);

        // Draw each renderable object (custom meshes)
        foreach (var renderable in renderables)
        {
            if (!renderable.Visible)
                continue;

            DrawRenderable(renderable);
        }
    }

    /// <summary>
    /// Render all ModelMeshRenderer components in the scene.
    /// This is called after RenderScene() to draw FBX models.
    ///
    /// EDUCATIONAL NOTE - WHY SEPARATE RENDERING?
    /// We separate procedural meshes (cubes/spheres) from imported models because:
    /// - Different data structures (Mesh vs Model)
    /// - Different effects (our BasicEffect vs Model's embedded effects)
    /// - Different optimization strategies
    ///
    /// Unity combines these automatically.
    /// MonoGame exposes the distinction for more control.
    /// </summary>
    public void RenderModels(ICamera camera, IEnumerable<Core.Components.ModelMeshRenderer> modelRenderers)
    {
        if (_graphicsDevice == null)
        {
            Console.WriteLine("[RenderModels] ERROR: GraphicsDevice is null!");
            return;
        }

        // Draw each model
        foreach (var modelRenderer in modelRenderers)
        {
            if (!modelRenderer.Visible || modelRenderer.Model == null)
                continue;

            // ModelMeshRenderer handles its own drawing (it has MonoGame's BasicEffect embedded)
            modelRenderer.Draw(_graphicsDevice, Matrix.Identity, camera.ViewMatrix, camera.ProjectionMatrix);
        }
    }

    
    /// <summary>
    /// Draw a single renderable object.
    /// This is the core of the rendering system.
    /// </summary>
    private void DrawRenderable(IRenderable renderable)
    {
        if (_basicEffect == null || _graphicsDevice == null)
            return;
            
        // Set the world matrix (object's position/rotation/scale)
        _basicEffect.World = ToXnaMatrix(renderable.WorldMatrix);
        
        // Set material properties
        var mat = renderable.Material;
        _basicEffect.DiffuseColor = ToXnaVector3(mat.Color);
        _basicEffect.EmissiveColor = ToXnaVector3(mat.EmissiveColor);
        _basicEffect.SpecularPower = mat.Specular;
        // For pure vertex colors, disable lighting (Primitives sample)
        _basicEffect.LightingEnabled = false;
        _basicEffect.VertexColorEnabled = true;

        // Enable/disable texturing
        _basicEffect.TextureEnabled = mat.Texture != null;
        if (mat.Texture is Texture2D texture)
        {
            _basicEffect.Texture = texture;
        }

        // Get color from material (assume RGBA in [0,1] range)
        var colorVec = mat.Color;
        Color solidColor = new Color(colorVec.X, colorVec.Y, colorVec.Z, colorVec.W);
        // Get or create the primitive mesh
        var mesh = GetOrCreateMesh(renderable.Mesh, solidColor);
        if (mesh == null)
            return;
        
        // Apply the effect and draw
        // This sends the vertices to the GPU
        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            
            _graphicsDevice.SetVertexBuffer(mesh.VertexBuffer);
            _graphicsDevice.Indices = mesh.IndexBuffer;
            
            _graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0, // base vertex
                0, // start index
                mesh.PrimitiveCount // primitive count (triangles)
            );
        }
    }
    
    /// <summary>
    /// Draw a debug primitive for visualization.
    /// Useful during development to see physics shapes, AI paths, etc.
    /// 
    /// UNITY COMPARISON:
    /// Similar to Unity's Debug.DrawLine(), Debug.DrawRay(), Gizmos.DrawSphere()
    /// Unity also has OnDrawGizmos() for editor visualization.
    /// </summary>
    public void DrawDebugPrimitive(DebugPrimitive primitive)
    {
        if (_basicEffect == null || _graphicsDevice == null)
            return;
            
        // Set up for debug drawing (no lighting, just solid colors)
        _basicEffect.LightingEnabled = false;
        _basicEffect.VertexColorEnabled = true;
        _basicEffect.DiffuseColor = ToXnaVector3(new Vector4(primitive.Color.X, primitive.Color.Y, primitive.Color.Z, 1));
        
        // TODO Phase 2: Implement debug primitive rendering
        // For now, just log
        Console.WriteLine($"[Graphics] Debug draw: {primitive.Type} at {primitive.Position}");
        
        // Re-enable lighting for regular rendering
        _basicEffect.LightingEnabled = true;
        _basicEffect.VertexColorEnabled = false;
    }
    
    /// <summary>
    /// Set lighting configuration.
    /// 
    /// EDUCATIONAL NOTE - LIGHTING IN GAMES:
    /// 
    /// Lighting makes 3D scenes look realistic. Without it, everything looks flat.
    /// 
    /// Types of lights:
    /// 1. Ambient - base light level (simulates light bouncing everywhere)
    /// 2. Directional - sun/moon (parallel rays, affects everything)
    /// 3. Point - light bulb (radiates in all directions)
    /// 4. Spot - flashlight (cone of light)
    /// 
    /// Unity uses physically-based lighting (PBR) with:
    /// - Real-world light units (lumens)
    /// - HDR (high dynamic range)
    /// - Global illumination (light bouncing)
    /// 
    /// MonoGame's BasicEffect uses simpler Phong lighting:
    /// - Ambient + Diffuse + Specular
    /// - Good enough for most games
    /// - Fast and easy to understand
    /// </summary>
    public void SetLighting(LightingConfiguration lighting)
    {
        _lighting = lighting;
    }
    
    /// <summary>
    /// Configure lighting on the BasicEffect.
    /// </summary>
    private void ConfigureLighting(BasicEffect effect, LightingConfiguration lighting)
    {
        effect.LightingEnabled = true;
        
        // Set ambient light (base illumination)
        effect.AmbientLightColor = ToXnaVector3(new Vector4(
            lighting.AmbientColor.X,
            lighting.AmbientColor.Y,
            lighting.AmbientColor.Z,
            1.0f
        ));
        
        // Disable all lights first
        effect.DirectionalLight0.Enabled = false;
        effect.DirectionalLight1.Enabled = false;
        effect.DirectionalLight2.Enabled = false;
        
        // Set up to 3 directional lights (BasicEffect limitation)
        if (lighting.DirectionalLights.Count > 0)
        {
            var light = lighting.DirectionalLights[0];
            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.Direction = ToXnaVector3(light.Direction);
            effect.DirectionalLight0.DiffuseColor = ToXnaVector3(light.Color) * light.Intensity;
            effect.DirectionalLight0.SpecularColor = ToXnaVector3(light.Color) * light.Intensity * 0.5f;
        }
        
        if (lighting.DirectionalLights.Count > 1)
        {
            var light = lighting.DirectionalLights[1];
            effect.DirectionalLight1.Enabled = true;
            effect.DirectionalLight1.Direction = ToXnaVector3(light.Direction);
            effect.DirectionalLight1.DiffuseColor = ToXnaVector3(light.Color) * light.Intensity;
        }
        
        if (lighting.DirectionalLights.Count > 2)
        {
            var light = lighting.DirectionalLights[2];
            effect.DirectionalLight2.Enabled = true;
            effect.DirectionalLight2.Direction = ToXnaVector3(light.Direction);
            effect.DirectionalLight2.DiffuseColor = ToXnaVector3(light.Color) * light.Intensity;
        }
        
        // Note: BasicEffect doesn't support point lights directly
        // Phase 2 could add custom shaders for point lights
    }
    
    /// <summary>
    /// End rendering a frame.
    /// This is called after all drawing is complete.
    /// </summary>
    public void EndFrame()
    {
        // In MonoGame, Present() is called by the Game class
        // We don't need to do anything here
    }
    
    /// <summary>
    /// Initialize graphics device and resources.
    /// Called when we first get access to GraphicsDevice.
    /// </summary>
    private void InitializeDevice(GraphicsDevice device)
    {
        _graphicsDevice = device;
        
        // Create BasicEffect for rendering
        // BasicEffect is MonoGame's simple shader for basic 3D rendering
        _basicEffect = new BasicEffect(device)
        {
            LightingEnabled = true,
            PreferPerPixelLighting = true, // Better quality lighting
            VertexColorEnabled = false
        };
        
        // Set up rasterizer state
        // Controls how polygons are drawn
        _rasterizerState = new RasterizerState
        {
            CullMode = CullMode.CullCounterClockwiseFace, // Don't draw back faces
            FillMode = FillMode.Solid // Draw filled polygons (not wireframe)
        };
        
        // Set up depth/stencil state
        // Controls depth testing (which objects are in front)
        _depthStencilState = new DepthStencilState
        {
            DepthBufferEnable = true, // Enable depth testing
            DepthBufferWriteEnable = true // Write to depth buffer
        };
        
    // Create primitive mesh with default color (white)
    _cubeMesh = CreateCubeMesh(device, Color.White);
        
        Console.WriteLine("[Graphics] Device initialized with BasicEffect renderer");
    }
    
    /// <summary>
    /// Get or create a mesh for rendering.
    /// Caches meshes to avoid recreating them every frame.
    /// </summary>
    private PrimitiveMesh? GetOrCreateMesh(Mesh mesh, Color solidColor)
    {
        // For Phase 1, we only support cube primitives
        // Check if it's a cube by vertex count
        if (mesh.Vertices.Length == 24) // Cube has 24 vertices (4 per face * 6 faces)
        {
            return CreateCubeMesh(_graphicsDevice!, solidColor);
        }
        
        // TODO Phase 2: Support sphere and capsule meshes
        // TODO Phase 3: Support loading custom meshes from files
        
        return _cubeMesh; // Default to cube for now
    }
    
    /// <summary>
    /// Create a cube mesh.
    /// 
    /// EDUCATIONAL NOTE - MESHES:
    /// 
    /// A mesh is geometry data for a 3D object:
    /// - Vertices: Points in 3D space (position, normal, UV)
    /// - Indices: Triangles (groups of 3 vertex indices)
    /// 
    /// A cube has:
    /// - 8 corner positions
    /// - But 24 vertices (because each corner needs different normals for each face)
    /// - 36 indices (12 triangles * 3 vertices each)
    /// 
    /// Why 24 vertices instead of 8?
    /// Each corner belongs to 3 faces, and each face needs a different normal vector
    /// for proper lighting. So we need separate vertices for each face.
    /// </summary>
    private PrimitiveMesh CreateCubeMesh(GraphicsDevice device, Color solidColor)
    {
        // Define cube vertices with positions and colors
        var vertices = new VertexPositionColor[24];
        var indices = new short[36];

        float size = 0.5f; // Half-size for a 1x1x1 cube

    // Use a single color for all vertices (solid color cube)

        // Front face (+Z)
        for (int i = 0; i < 4; i++)
            vertices[i] = new VertexPositionColor(
                new Vector3(i == 0 || i == 3 ? -size : size, i < 2 ? -size : size, size), solidColor);
        // Back face (-Z)
        for (int i = 0; i < 4; i++)
            vertices[4 + i] = new VertexPositionColor(
                new Vector3(i == 1 || i == 2 ? -size : size, i < 2 ? -size : size, -size), solidColor);
        // Top face (+Y)
        for (int i = 0; i < 4; i++)
            vertices[8 + i] = new VertexPositionColor(
                new Vector3(i == 0 || i == 3 ? -size : size, size, i < 2 ? size : -size), solidColor);
        // Bottom face (-Y)
        for (int i = 0; i < 4; i++)
            vertices[12 + i] = new VertexPositionColor(
                new Vector3(i == 1 || i == 2 ? -size : size, -size, i < 2 ? -size : size), solidColor);
        // Right face (+X)
        for (int i = 0; i < 4; i++)
            vertices[16 + i] = new VertexPositionColor(
                new Vector3(size, i < 2 ? -size : size, i == 0 || i == 3 ? size : -size), solidColor);
        // Left face (-X)
        for (int i = 0; i < 4; i++)
            vertices[20 + i] = new VertexPositionColor(
                new Vector3(-size, i < 2 ? -size : size, i == 1 || i == 2 ? size : -size), solidColor);

        // Define triangle indices (counter-clockwise winding)
        int idx = 0;
        for (int face = 0; face < 6; face++)
        {
            int baseVertex = face * 4;
            // First triangle
            indices[idx++] = (short)(baseVertex + 0);
            indices[idx++] = (short)(baseVertex + 1);
            indices[idx++] = (short)(baseVertex + 2);
            // Second triangle
            indices[idx++] = (short)(baseVertex + 0);
            indices[idx++] = (short)(baseVertex + 2);
            indices[idx++] = (short)(baseVertex + 3);
        }

        // Create vertex and index buffers on GPU
        var vertexBuffer = new VertexBuffer(device, VertexPositionColor.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(vertices);

        var indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);

        return new PrimitiveMesh
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            VertexCount = vertices.Length,
            PrimitiveCount = indices.Length / 3
        };
    }
    
    public void Shutdown()
    {
        _basicEffect?.Dispose();
        _rasterizerState?.Dispose();
        _depthStencilState?.Dispose();
        _cubeMesh?.Dispose();
        _sphereMesh?.Dispose();
        _capsuleMesh?.Dispose();
    }
    
    #region Helper Methods - Type Conversions
    
    /// <summary>
    /// Convert System.Numerics.Matrix4x4 to MonoGame's Matrix.
    /// We use System.Numerics for math because it's SIMD-optimized and cross-platform.
    /// But MonoGame's graphics APIs need MonoGame.Framework.Matrix.
    /// </summary>
    private Microsoft.Xna.Framework.Matrix ToXnaMatrix(Matrix matrix)
    {
        return new Microsoft.Xna.Framework.Matrix(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        );
    }
    
    private Microsoft.Xna.Framework.Vector3 ToXnaVector3(Vector4 v)
    {
        return new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
    }
    
    private Microsoft.Xna.Framework.Vector3 ToXnaVector3(Vector3 v)
    {
        return new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
    }
    
    #endregion
}

/// <summary>
/// Container for GPU mesh data.
/// </summary>
internal class PrimitiveMesh : IDisposable
{
    public VertexBuffer? VertexBuffer { get; set; }
    public IndexBuffer? IndexBuffer { get; set; }
    public int VertexCount { get; set; }
    public int PrimitiveCount { get; set; }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
    }
}