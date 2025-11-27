using Microsoft.Xna.Framework.Graphics;
using System.Numerics;
using Shooter.Core.Plugins.Graphics;

namespace Shooter.Core.Components;

/// <summary>
/// Component for rendering MonoGame Model files (FBX imported through Content Pipeline).
///
/// UNITY COMPARISON:
/// Similar to Unity's combination of:
/// - MeshFilter (holds mesh data)
/// - MeshRenderer (renders the mesh)
/// - Model Importer (FBX import pipeline)
///
/// Unity automatically handles FBX imports and creates GameObjects with MeshRenderer.
/// In MonoGame, we:
/// 1. Import FBX through Content.mgcb
/// 2. Load Model with Content.Load&lt;Model&gt;()
/// 3. Manually render each mesh in the model
///
/// EDUCATIONAL NOTE - WHY SEPARATE FROM MeshRenderer?
///
/// MeshRenderer uses our custom Mesh class (for procedural geometry like cubes/spheres).
/// ModelMeshRenderer uses MonoGame's Model class (for imported FBX files).
///
/// These are different systems:
/// - Custom Mesh: We manually create vertices and indices
/// - Model: MonoGame's Content Pipeline processes FBX and creates optimized data
///
/// Unity hides this distinction. MonoGame exposes it for more control.
/// </summary>
public class ModelMeshRenderer : EntityComponent
{
    private Model? _model;
    private Vector4 _tintColor = Vector4.One; // White = no tint
    private bool _visible = true;
    private float _scale = 1.0f;

    /// <summary>
    /// The MonoGame Model to render.
    /// Load this from the Content Pipeline, e.g.:
    /// renderer.Model = Content.Load&lt;Model&gt;("Models/Mesh_Weapon_Primary");
    /// </summary>
    public Model? Model
    {
        get => _model;
        set => _model = value;
    }

    /// <summary>
    /// Tint color applied to all meshes in the model.
    /// Use Vector4(1,1,1,1) for white (no tint).
    ///
    /// EDUCATIONAL NOTE - TINTING:
    /// Tinting multiplies the model's texture colors by this color.
    /// - Vector4(1, 0, 0, 1) = red tint
    /// - Vector4(0.5, 0.5, 0.5, 1) = darken 50%
    /// - Vector4(2, 2, 2, 1) = brighten 2x (emissive effect)
    ///
    /// Unity has similar functionality in the Material color property.
    /// </summary>
    public Vector4 TintColor
    {
        get => _tintColor;
        set => _tintColor = value;
    }

    /// <summary>
    /// Whether this model is currently visible.
    /// Similar to MeshRenderer.enabled in Unity.
    /// </summary>
    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    /// <summary>
    /// Uniform scale applied to the model.
    /// For non-uniform scaling, use the Transform3D component's scale property.
    ///
    /// EDUCATIONAL NOTE:
    /// Sometimes imported models are too large or small.
    /// Use this for quick adjustments without re-exporting from Unity.
    ///
    /// Example:
    /// - Unity model is 10 units tall
    /// - MonoGame needs 2 units
    /// - Set Scale = 0.2f
    /// </summary>
    public float Scale
    {
        get => _scale;
        set => _scale = value;
    }

    public override void Initialize()
    {
        base.Initialize();
        // Model is set by code or loaded from Content Manager
    }

    /// <summary>
    /// Render the model using MonoGame's BasicEffect.
    /// Called by the ForwardGraphicsProvider during the render pass.
    ///
    /// EDUCATIONAL NOTE - MODEL RENDERING LOOP:
    ///
    /// A Model contains multiple ModelMeshes (for complex objects).
    /// Each ModelMesh contains multiple ModelMeshParts (for different materials).
    ///
    /// Example FBX structure:
    /// Model "HoverBot"
    ///   ├── ModelMesh "Body"
    ///   │     ├── ModelMeshPart (Metal material)
    ///   │     └── ModelMeshPart (Glass material)
    ///   └── ModelMesh "Eyes"
    ///         └── ModelMeshPart (Emissive material)
    ///
    /// We iterate through all parts and draw them with the correct transforms.
    ///
    /// COMMON UNITY-TO-MONOGAME PORTING ISSUE - TRANSFORM MATRICES:
    ///
    /// Unity automatically applies GameObject.transform to all renderers.
    /// In MonoGame, YOU must build the world matrix from your entity's transform.
    ///
    /// If models render at world origin (0,0,0) instead of their entity positions:
    /// → Check that you're building the SRT matrix (Scale-Rotation-Translation)
    /// → Matrix multiplication order matters: Scale * Rotation * Translation
    /// → Remember to actually USE the entity's Position and Rotation!
    ///
    /// This is one of the most common issues when porting from Unity.
    /// Unity hides this complexity; MonoGame exposes it for performance and control.
    /// </summary>
    public void Draw(GraphicsDevice graphicsDevice, Matrix4x4 world, Matrix4x4 view, Matrix4x4 projection)
    {
        if (!_visible || _model == null)
            return;

        // Get transform from owner entity
        var transform = Owner?.GetComponent<Transform3D>();
        if (transform == null)
            return;

        // Build world matrix: Scale -> Rotation -> Translation (SRT order)
        var scaleMatrix = Matrix4x4.CreateScale(_scale);
        var rotationMatrix = Matrix4x4.CreateFromQuaternion(transform.Rotation);
        var translationMatrix = Matrix4x4.CreateTranslation(transform.Position);

        // Combine: First scale, then rotate, then translate (right-to-left multiplication)
        var entityWorld = scaleMatrix * rotationMatrix * translationMatrix;
        var finalWorld = entityWorld * world;

        // Convert System.Numerics matrices to MonoGame matrices for BasicEffect
        var mgWorld = ToXnaMatrix(finalWorld);
        var mgView = ToXnaMatrix(view);
        var mgProjection = ToXnaMatrix(projection);

        // Draw each mesh in the model
        foreach (var mesh in _model.Meshes)
        {
            // Each mesh has its own transformation within the model
            var meshWorld = mesh.ParentBone.Transform * mgWorld;

            foreach (var meshPart in mesh.MeshParts)
            {
                var effect = meshPart.Effect as BasicEffect;
                if (effect != null)
                {
                    // Set transformation matrices
                    effect.World = meshWorld;
                    effect.View = mgView;
                    effect.Projection = mgProjection;

                    // Apply tint color to diffuse
                    effect.DiffuseColor = new Microsoft.Xna.Framework.Vector3(
                        _tintColor.X,
                        _tintColor.Y,
                        _tintColor.Z
                    );
                    effect.Alpha = _tintColor.W;

                    // Enable default lighting (MonoGame's BasicEffect has simple lighting)
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;

                    // Apply the effect and draw
                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.SetVertexBuffer(meshPart.VertexBuffer, meshPart.VertexOffset);
                        graphicsDevice.Indices = meshPart.IndexBuffer;
                        graphicsDevice.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0,
                            meshPart.StartIndex,
                            meshPart.PrimitiveCount
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Convert System.Numerics.Matrix4x4 to Microsoft.Xna.Framework.Matrix.
    ///
    /// EDUCATIONAL NOTE - WHY TWO MATRIX TYPES?
    ///
    /// As explained in UnityToMonoGame.md, we use System.Numerics throughout our codebase
    /// for SIMD performance and BepuPhysics compatibility.
    ///
    /// But MonoGame's BasicEffect requires Microsoft.Xna.Framework.Matrix.
    /// This conversion only happens at the rendering boundary.
    ///
    /// The overhead is minimal compared to the benefits of SIMD math everywhere else.
    /// </summary>
    private Microsoft.Xna.Framework.Matrix ToXnaMatrix(Matrix4x4 matrix)
    {
        return new Microsoft.Xna.Framework.Matrix(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        );
    }

    /// <summary>
    /// Convenience method to load a model from the Content Manager.
    ///
    /// UNITY COMPARISON:
    /// In Unity, you drag-drop an FBX into a prefab and it's ready.
    /// In MonoGame, you:
    /// 1. Add FBX to Content.mgcb
    /// 2. Build content
    /// 3. Call this method to load at runtime
    ///
    /// Example usage:
    /// var renderer = entity.AddComponent&lt;ModelMeshRenderer&gt;();
    /// renderer.LoadModel(contentManager, "Models/Mesh_Weapon_Primary");
    /// </summary>
    public void LoadModel(Microsoft.Xna.Framework.Content.ContentManager content, string assetName)
    {
        try
        {
            _model = content.Load<Model>(assetName);
            Console.WriteLine($"[ModelMeshRenderer] Loaded model: {assetName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ModelMeshRenderer] Failed to load model '{assetName}': {ex.Message}");
            _model = null;
        }
    }

    /// <summary>
    /// Get the bounding sphere for the entire model.
    /// Useful for culling and distance calculations.
    ///
    /// UNITY COMPARISON:
    /// Similar to MeshRenderer.bounds in Unity.
    /// Unity calculates bounds automatically.
    /// MonoGame stores it in the model data.
    /// </summary>
    public Microsoft.Xna.Framework.BoundingSphere? GetBoundingSphere()
    {
        if (_model == null || _model.Meshes.Count == 0)
            return null;

        // Get the first mesh's bounding sphere as approximation
        return _model.Meshes[0].BoundingSphere;
    }
}
