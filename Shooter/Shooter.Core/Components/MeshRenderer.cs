using System.Numerics;
using Shooter.Core.Plugins.Graphics;

namespace Shooter.Core.Components;

/// <summary>
/// MeshRenderer component for rendering 3D meshes.
/// 
/// UNITY COMPARISON:
/// Similar to Unity's MeshRenderer + MeshFilter components.
/// Unity separates these:
/// - MeshFilter holds the mesh data
/// - MeshRenderer does the actual rendering
/// 
/// We combine them for simplicity in this educational project.
/// 
/// EDUCATIONAL NOTE - RENDERING PIPELINE:
/// 
/// To render an object, you need:
/// 1. Geometry (Mesh): What shape is it?
/// 2. Material: What does it look like (color, texture, shininess)?
/// 3. Transform (WorldMatrix): Where is it in the world?
/// 
/// The graphics provider takes all IRenderable objects and draws them.
/// This component implements IRenderable to participate in rendering.
/// </summary>
public class MeshRenderer : EntityComponent, IRenderable
{
    private Mesh _mesh = Mesh.CreateCube(1.0f);
    private Material _material = Material.CreateSolid(new Vector4(1, 1, 1, 1)); // White
    private bool _visible = true;
    private int _layer = 0;
    
    /// <summary>
    /// The mesh to render.
    /// Default is a 1x1x1 cube.
    /// 
    /// UNITY COMPARISON:
    /// In Unity, you'd do:
    /// GetComponent&lt;MeshFilter&gt;().mesh = myMesh;
    /// 
    /// Here we just set it directly:
    /// GetComponent&lt;MeshRenderer&gt;().Mesh = myMesh;
    /// </summary>
    public Mesh Mesh
    {
        get => _mesh;
        set => _mesh = value ?? Mesh.CreateCube(1.0f);
    }
    
    /// <summary>
    /// The material (appearance) for this mesh.
    /// 
    /// UNITY COMPARISON:
    /// In Unity: GetComponent&lt;MeshRenderer&gt;().material = myMaterial;
    /// Materials in Unity are more complex (shaders, textures, properties).
    /// We keep it simple for Phase 1: just color and basic properties.
    /// </summary>
    public Material Material
    {
        get => _material;
        set => _material = value ?? Material.CreateSolid(Vector4.One);
    }
    
    /// <summary>
    /// Whether this renderer is currently visible.
    /// Similar to MeshRenderer.enabled in Unity.
    /// 
    /// EDUCATIONAL NOTE:
    /// Setting visible = false prevents rendering but doesn't
    /// disable physics or other components.
    /// It's just a rendering optimization.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }
    
    /// <summary>
    /// Rendering layer for sorting and filtering.
    /// Similar to GameObject.layer in Unity.
    /// 
    /// EDUCATIONAL NOTE - LAYERS:
    /// Layers are used for:
    /// - Selective rendering (cameras can ignore certain layers)
    /// - Collision filtering (physics layers)
    /// - Raycast filtering (ignore certain layers)
    /// 
    /// Common layers in FPS:
    /// 0 = Default
    /// 1 = Player
    /// 2 = Enemies
    /// 3 = Weapons
    /// 4 = Environment
    /// 5 = Effects
    /// </summary>
    public int Layer
    {
        get => _layer;
        set => _layer = value;
    }
    
    /// <summary>
    /// World transformation matrix for this object.
    /// Built from the Entity's Transform3D component.
    /// 
    /// EDUCATIONAL NOTE - WORLD MATRIX:
    /// The world matrix transforms vertices from local space to world space.
    /// It's a combination of:
    /// - Scale: How big is the object?
    /// - Rotation: Which way is it facing?
    /// - Translation: Where is it positioned?
    /// 
    /// Matrix order matters! Scale → Rotate → Translate (SRT)
    /// 
    /// In Unity, this is calculated from transform.localToWorldMatrix.
    /// </summary>
    public Matrix4x4 WorldMatrix
    {
        get
        {
            // Get the transform from the entity
            var transform = Owner?.GetComponent<Transform3D>();
            if (transform == null)
                return Matrix4x4.Identity;
                
            return transform.WorldMatrix;
        }
    }
    
    /// <summary>
    /// Initialize the mesh renderer.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // Default to a white cube
        _mesh = Mesh.CreateCube(1.0f);
        _material = Material.CreateSolid(new Vector4(1, 1, 1, 1));
    }
    
    /// <summary>
    /// Update is not needed for basic rendering.
    /// But could be used for:
    /// - Animated materials
    /// - Billboard rotation (always face camera)
    /// - LOD (level of detail) switching
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        // No per-frame logic needed for basic rendering
        // Advanced features can be added here later
    }
    
    /// <summary>
    /// Drawing happens in the graphics provider's RenderScene().
    /// We don't draw here - this component just provides data.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        
        // The actual drawing is done by the ForwardGraphicsProvider
        // when it iterates over all IRenderable objects
    }
    
    /// <summary>
    /// Convenience method to set mesh to a cube.
    /// </summary>
    public void SetCube(float size, Vector4 color)
    {
        Mesh = Mesh.CreateCube(size);
        Material = Material.CreateSolid(color);
    }
    
    /// <summary>
    /// Convenience method to set mesh to a sphere.
    /// </summary>
    public void SetSphere(float radius, Vector4 color)
    {
        Mesh = Mesh.CreateSphere(radius);
        Material = Material.CreateSolid(color);
    }
    
    /// <summary>
    /// Convenience method to create an emissive (glowing) object.
    /// Useful for muzzle flashes, enemy eyes, power-ups, etc.
    /// </summary>
    public void SetEmissive(Vector4 color, float intensity = 1.0f)
    {
        Material = Material.CreateEmissive(color, intensity);
    }
    
    /// <summary>
    /// Set a custom color while keeping the current mesh.
    /// </summary>
    public void SetColor(Vector4 color)
    {
        Material.Color = color;
    }
    
    /// <summary>
    /// Set a custom color using RGB values (0-255).
    /// More intuitive for beginners than 0-1 range.
    /// </summary>
    public void SetColorRGB(int r, int g, int b)
    {
        Material.Color = new Vector4(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
    }
}
