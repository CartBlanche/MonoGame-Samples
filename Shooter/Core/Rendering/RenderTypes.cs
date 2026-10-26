using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shooter.Rendering
{
    /// <summary>
    /// Shared contract between the scene loader (Core/Scene) and the renderer (Core/Rendering).
    /// All world-space data is in the project's right-handed MonoGame convention (see Core/Content/ASSET-CONVENTIONS.md).
    /// Either side may ADD members, but must not rename or remove existing ones without updating the other.
    /// </summary>
    public enum MaterialKind
    {
        Lit,              // Universal Render Pipeline/Lit
        ParticlesUnlit,   // Universal Render Pipeline/Particles/Unlit
        ParticlesLit,     // Universal Render Pipeline/Particles/Lit
        ProjectorMultiply,// FPS/Art/Shaders/ProjectorMultiply
        SpriteUnlit,      // Sprites/Default and Sprite-Lit-Default
        Skybox            // Skybox/Panoramic
    }

    public enum SurfaceType { Opaque, Transparent }

    public enum BlendKind { Alpha, Premultiply, Additive, Multiply }

    public enum CullKind { Back, Front, None }

    /// <summary>Unity material properties, already resolved to runtime textures.</summary>
    public class Material
    {
        public string Name;
        public MaterialKind Kind = MaterialKind.Lit;
        public SurfaceType Surface = SurfaceType.Opaque;
        public BlendKind Blend = BlendKind.Alpha;
        public CullKind Cull = CullKind.Back;
        public bool ZWrite = true;
        public bool AlphaClip;
        public float Cutoff = 0.5f;
        public int RenderQueue = -1;

        public Texture2D BaseMap;
        public Texture2D NormalMap;
        public Texture2D MetallicGlossMap;   // R = metallic, A = smoothness (Unity packing)
        public Texture2D OcclusionMap;
        public Texture2D EmissionMap;

        public Vector4 BaseColor = Vector4.One;     // linear-space values as serialised by Unity
        public Vector3 EmissionColor = Vector3.Zero; // HDR
        public float Metallic;
        public float Smoothness = 0.5f;
        public float BumpScale = 1f;
        public float OcclusionStrength = 1f;
        public Vector4 TilingOffset = new Vector4(1, 1, 0, 0); // xy = tiling, zw = offset

        public bool SpecularHighlights = true;
        public bool EnvironmentReflections = true;
        public bool ReceiveShadows = true;

        // ---- renderer additions (Core/Rendering owns these; the scene loader may leave the defaults) ----

        /// <summary>
        /// True (default): <see cref="BaseColor"/> holds the colour exactly as Unity serialises it (gamma / "sRGB" authored values)
        /// and the renderer converts it to linear like Unity does for Color properties in a linear project.
        /// <see cref="EmissionColor"/> is an HDR colour and is always used as-is (verified against the baseline: Sign_Green emission
        /// 0.126 is meant linear). Set to false if the loader already linearised BaseColor.
        /// </summary>
        public bool ColorsAreGamma = true;

        /// <summary>Unity's authored sRGB flag per texture (AssetMap "srgb"). Textures flagged true are decoded to linear in the shader.</summary>
        public bool BaseMapIsSrgb = true;
        public bool EmissionMapIsSrgb = true;
        public bool OcclusionMapIsSrgb = true;
        public bool MetallicGlossMapIsSrgb = true;

        /// <summary>Particle colour mode (URP _ColorMode: 0 Multiply, 1 Additive, 2 Subtractive, 3 Overlay, 4 Color, 5 Difference). Falls back to Floats["_ColorMode"].</summary>
        public int ParticleColorMode = -1;

        /// <summary>Projector/Multiply inputs: cookie (_ShadowTex), falloff (_FalloffTex) and the unity_Projector / unity_ProjectorClip matrices.</summary>
        public Texture2D CookieMap;
        public Texture2D FalloffMap;
        public Matrix ProjectorMatrix = Matrix.Identity;
        public Matrix FalloffMatrix = Matrix.Identity;

        /// <summary>Wrap mode of the textures (Unity wrapU/wrapV: repeat unless clamped). Applies to all maps of this material.</summary>
        public bool ClampTextures;

        /// <summary>Anything not modelled above (raw Unity floats/colors by property name).</summary>
        public Dictionary<string, float> Floats = new Dictionary<string, float>();
        public Dictionary<string, Vector4> Vectors = new Dictionary<string, Vector4>();
    }

    public class MeshPart
    {
        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;
        public int BaseVertex;
        public int StartIndex;
        public int PrimitiveCount;
        public Material Material;
    }

    public class RenderMesh
    {
        public string Name;
        public List<MeshPart> Parts = new List<MeshPart>();
        public BoundingBox Bounds; // object space
    }

    public enum RenderLayer { Default, Weapon }

    public struct RenderItem
    {
        public RenderMesh Mesh;
        public Matrix World;
        public Matrix[] BoneMatrices; // null for static meshes; skinning palette otherwise (SkinnedModel convention)
        public bool CastShadows;
        public bool ReceiveShadows;
        public RenderLayer Layer;
    }

    public class SceneLighting
    {
        public Vector3 SunDirection = new Vector3(0.778f, -0.529f, -0.338f); // direction light TRAVELS, MonoGame space
        public Vector3 SunColor = new Vector3(1f, 0.373f, 0f);              // linear
        public float SunIntensity = 0.55f;
        public bool SunShadows = true;
        public Vector3 AmbientColor = new Vector3(0.455f, 0.686f, 0.745f); // flat ambient
        public float AmbientIntensity = 1f;
        public float ShadowDistance = 50f;
        public float ShadowDepthBias = 1f;
        public float ShadowNormalBias = 1f;
        public Texture2D SkyboxTexture;        // lat-long panoramic
        public float SkyExposure = 0.6f;
        public Vector4 SkyTint = new Vector4(1, 1, 1, 0.5f);
        public float SkyRotationDegrees;

        // ---- renderer additions ----

        /// <summary>
        /// True (default): SunColor / AmbientColor / SkyTint hold the gamma values exactly as serialised by Unity (lighting.json) and the
        /// renderer converts them to linear (Unity light.finalColor / RenderSettings.ambientLight in a linear project).
        /// Set to false when the loader supplies already-linear values.
        /// </summary>
        public bool ColorsAreGamma = true;

        /// <summary>Skybox panorama is an sRGB texture (Unity skybox albedo): decode it in the shader.</summary>
        public bool SkyboxIsSrgb = true;
    }

    public class Camera
    {
        public Matrix View = Matrix.Identity;
        public Matrix Projection = Matrix.Identity;
        public Vector3 Position;
        public float NearPlane = 0.1f;
        public float FarPlane = 300f;
        public float FieldOfViewY = MathHelper.ToRadians(60f);
        public float AspectRatio = 16f / 9f;
    }

    public class PostProcessSettings
    {
        public bool Enabled = true;
        public float BloomThreshold = 0.9f, BloomIntensity = 0.1f, BloomScatter = 0.7f;
        public Vector3 VignetteColor = new Vector3(0.183f, 0.047f, 0.321f);
        public float VignetteIntensity = 0.084f, VignetteSmoothness = 0.2f;
        public bool Tonemapping = true; // ACES
        public float MotionBlurIntensity = 0.35f;
        public float FilmGrainIntensity = 0.3f, FilmGrainResponse = 0.8f;
        public bool Ssao = true;
        public bool Fxaa = true;
    }

    public interface ISceneRenderer
    {
        /// <summary>UI layer (premultiplied, linear light) the final pass blends over the frame; null = none. Renderers without a final pass ignore it.</summary>
        Texture2D Overlay { get; set; }

        /// <summary>Draws the scene to the current back buffer (including post-processing and the weapon layer).</summary>
        void Render(GameTime gameTime, IReadOnlyList<RenderItem> items, SceneLighting lighting, Camera camera, Camera weaponCamera, PostProcessSettings post);
    }
}
