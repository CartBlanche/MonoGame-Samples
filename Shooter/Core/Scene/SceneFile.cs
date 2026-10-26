using System.Collections.Generic;
using System.Text.Json;

namespace Shooter.Scene
{
    // Plain data model of Content/Scenes/MainScene.scene.json (written by Tools/SceneBake, read by SceneLoader).
    // This file is linked into the baker, so it must not depend on MonoGame. All spatial data is already in the
    // MonoGame right-handed convention (see UnityConvert), except entries explicitly documented as "raw Unity".
    // Matrices are 16 floats in XNA row-major order (M11, M12, ... M44), row-vector convention.

    public class SceneFile
    {
        public int Version = 1;
        public string Source;
        public List<MeshRefDto> MeshRefs = new List<MeshRefDto>();
        public List<MaterialDto> Materials = new List<MaterialDto>();
        public List<InstanceDto> Instances = new List<InstanceDto>();
        public List<ModelInstanceDto> ModelInstances = new List<ModelInstanceDto>();
        public LightingDto Lighting = new LightingDto();
        public CameraDto Camera = new CameraDto();
        public CameraDto WeaponCamera = new CameraDto();
        public PlayerDto Player = new PlayerDto();
        public WeaponDto Weapon;
        public WeaponDto TurretProjectile;  // Projectile_Turret prefab
        public WeaponDto EnemyProjectile;   // Projectile_Hoverbot prefab (same layout as Projectile)
        public WeaponDto Projectile;   // Projectile_Blaster prefab: Pieces relative to the projectile root, TipInRoot, Components (ProjectileStandard fields)
        public List<EnemyDto> Enemies = new List<EnemyDto>();
        public List<PickupDto> Pickups = new List<PickupDto>();
        public List<PatrolPathDto> PatrolPaths = new List<PatrolPathDto>();
        public List<ObjectiveDto> Objectives = new List<ObjectiveDto>();
        public List<ColliderDto> Colliders = new List<ColliderDto>();
        public List<EmitterDto> Emitters = new List<EmitterDto>();
        public List<ProjectorDto> Projectors = new List<ProjectorDto>();
    }

    /// <summary>A mesh asset: ContentManager name of the Model + the ModelMesh name inside it (null = all meshes).</summary>
    public class MeshRefDto
    {
        public string Model;   // e.g. "Models/Level/OBJWall_4m"
        public string Mesh;    // e.g. "Wall_4m" (Unity MeshFilter.m_Mesh.name == ModelMesh.Name)
    }

    public class MaterialDto
    {
        public string Name;
        public string Kind;        // MaterialKind enum name
        public string Shader;      // Unity shader name
        public string Surface;     // Opaque | Transparent
        public string Blend;       // Alpha | Premultiply | Additive | Multiply
        public string Cull;        // Back | Front | None
        public bool ZWrite = true;
        public bool AlphaClip;
        public float Cutoff = 0.5f;
        public int RenderQueue = -1;
        public bool Emission;                  // _EMISSION keyword
        public bool SpecularHighlights = true; // false when _SPECULARHIGHLIGHTS_OFF
        public bool EnvironmentReflections = true; // false when _ENVIRONMENTREFLECTIONS_OFF
        public bool ReceiveShadows = true;
        public float[] BaseColor;              // rgba as serialised by Unity
        public float[] EmissionColor;          // rgb HDR (already zeroed if !Emission)
        public float Metallic;
        public float Smoothness = 0.5f;
        public float BumpScale = 1f;
        public float OcclusionStrength = 1f;
        public float[] TilingOffset;           // x,y = tiling, z,w = offset (of _BaseMap / _MainTex)
        public string BaseMap;                 // ContentManager texture names (null = none)
        public string NormalMap;
        public string MetallicGlossMap;
        public string OcclusionMap;
        public string EmissionMap;
        public Dictionary<string, string> Textures;   // every texture property -> content name (incl. the five above, Skybox _MainTex, Projector tex)
        public Dictionary<string, float> Floats;      // raw Unity floats
        public Dictionary<string, float[]> Vectors;   // raw Unity colors / vectors
        public List<string> Keywords;
    }

    public static class InstanceFlags
    {
        public const int CastShadows = 1;
        public const int ReceiveShadows = 2;
        public const int Static = 4;
    }

    /// <summary>A MeshFilter+MeshRenderer instance in the level.</summary>
    public class InstanceDto
    {
        public string Name;     // GameObject name
        public int Mesh;        // index into MeshRefs
        public int[] Mats;      // indices into Materials, in Unity m_Materials order (null entry = -1)
        public float[] M;       // world matrix, MonoGame space
        public int Flags;       // InstanceFlags
        public int Layer;
    }

    /// <summary>A model asset placed as a prefab instance (pickup mesh, sun). Every ModelMesh of the model is drawn.</summary>
    public class ModelInstanceDto
    {
        public string Name;
        public string Kind;     // "pickup" | "sun"
        public string Model;    // content name
        public int[] Mats;      // material override(s) by submesh/part index, indices into Materials
        public float[] M;
        public int Flags;
        public bool Render = true;
    }

    public class LightingDto
    {
        public float[] SunDirection;     // direction the light travels, MonoGame space
        public float[] SunColor;         // rgb, as serialised
        public float SunIntensity = 1f;
        public bool SunShadows = true;
        public float ShadowStrength = 1f;
        public float ShadowBias = 1f;         // URP asset (pipeline) values
        public float ShadowNormalBias = 1f;
        public float ShadowDistance = 50f;
        public float[] AmbientColor;     // flat ambient (AmbientMode 3: Color)
        public float AmbientIntensity = 1f;
        public int AmbientMode;
        public string SkyboxMaterial;
        public string SkyboxTexture;
        public float SkyExposure = 1f;
        public float[] SkyTint;
        public float SkyRotation;
        public bool Fog;
        public float[] FogColor;
        public float FogDensity;
        public int FogMode;
    }

    public class CameraDto
    {
        public string Name;
        public float[] World;        // camera-to-world matrix, MonoGame space (camera looks down its local -Z)
        public float[] Position;     // MonoGame space
        public float[] UnityEuler;   // Unity world eulerAngles (degrees), for reference
        public float Fov = 60f;      // vertical, degrees
        public float Near = 0.1f;
        public float Far = 300f;
        public float[] BackgroundColor;
        public int CullingMask;
    }

    public class PlayerDto
    {
        public string Name;
        public float[] M;                 // player root world matrix
        public float[] Position;          // MonoGame space (root, i.e. capsule base minus center)
        public float YawDegrees;          // Unity y euler of the root
        public float CameraHeight;        // local y of the Main Camera under the player root
        public float CharacterHeight, CharacterRadius, SlopeLimit, StepOffset, SkinWidth;
        public float[] CharacterCenter;   // MonoGame space local (z negated)
        public float[] StartPositionUnity; // raw Unity world position of the player root
        public float[] AimPointInRoot;    // Actor.AimPoint relative to the root (enemies aim and cast line of sight at it)
        public Dictionary<string, JsonElement> Components;   // MonoBehaviour fields keyed by script name (raw Unity values)
    }

    public class WeaponPieceDto
    {
        public string Name;
        public int Mesh;           // index into MeshRefs
        public int[] Mats;
        public float[] M;          // matrix relative to the weapon root (MonoGame space), root = WeaponParentSocket
    }

    public class WeaponDto
    {
        public string Name;
        public List<WeaponPieceDto> Pieces = new List<WeaponPieceDto>();
        // Weapon root (DefaultWeaponPosition) relative to the main camera, MonoGame camera space (camera looks down -Z).
        public float[] RootInCamera;
        public float[] MuzzleInRoot;     // GunMuzzle local position, MonoGame space
        // PlayerWeaponsManager sockets relative to the main camera (same convention as RootInCamera): the weapon socket
        // moves between Default, Aiming and Down while playing.
        public float[] AimingInCamera;
        public float[] DownInCamera;
        public List<string> FuelCellPieces = new List<string>();   // WeaponFuelCellHandler.FuelCells resolved to piece names, in handler order
        public float[] TipInRoot;        // projectile prefab only: Tip transform position relative to the root, MonoGame space
        public Dictionary<string, JsonElement> Components;
    }

    public class EnemyDto
    {
        public string Name;
        public string Kind;           // "HoverBot" | "Turret"
        public string Prefab;
        public float[] M;
        public float[] Position;
        public float MaxHealth;
        public int PatrolPath = -1;   // index into PatrolPaths, -1 = none
        public Dictionary<string, JsonElement> Parts;   // NavMeshAgent values and scripts on child GameObjects (DetectionModule, WeaponController): raw Unity fields
        public Dictionary<string, NodeDto> Hierarchy;   // child GameObject name -> parent name (null = the enemy root) and local matrix
        public Dictionary<string, float[]> Nodes;       // child GameObject name -> position relative to the enemy root, MonoGame space
        public Dictionary<string, JsonElement> Components;   // raw MonoBehaviour fields (Unity values)
    }

    public class NodeDto
    {
        public string Parent;      // parent GameObject name, null = the enemy root
        public float[] Local;      // local matrix, MonoGame space (row-vector convention, translation in 12..14)
        public bool ModelNode;     // a node of the FBX (bone or mesh node): its real pose comes from the model's skeleton, Local is a placeholder
    }

    public class PickupDto
    {
        public string Name;
        public string Kind = "Health";
        public float[] M;
        public float[] Position;
        public float HealAmount;
        public float[] TriggerCenter;   // MonoGame space local
        public float[] TriggerSize;
        public Dictionary<string, JsonElement> Components;
    }

    public class PatrolPathDto
    {
        public string Name;
        public List<float[]> Points = new List<float[]>();   // world, MonoGame space
        public List<string> PointNames = new List<string>();
        public List<string> Enemies = new List<string>();    // names of the enemies assigned (resolved when possible)
    }

    public class ObjectiveDto
    {
        public string Name;
        public string Kind;       // script name
        public float[] Position;
        public Dictionary<string, JsonElement> Fields;
    }

    public class ColliderDto
    {
        public string Type;        // box | sphere | capsule | mesh | character
        public string Owner;       // GameObject name
        public int Layer;
        public bool Trigger;
        public bool Convex;
        public int Instance = -1;  // MeshCollider on a GameObject that also has a MeshRenderer: index into Instances (M omitted)
        public int Mesh = -1;      // MeshCollider: index into MeshRefs
        public float[] M;          // owner world matrix (incl. scale), MonoGame space
        public float[] Center;     // local, MonoGame space (z negated)
        public float[] Size;       // box
        public float Radius;       // sphere/capsule (local, before M scale)
        public float Height;       // capsule
        public int Direction = 1;  // capsule axis: 0 X, 1 Y, 2 Z (unchanged by the Z mirror)
    }

    public class EmitterDto
    {
        public string Name;
        public string Owner;       // top-level object it belongs to
        public float[] M;
        public bool Active;
        public string Material;    // renderer material name
    }

    public class ProjectorDto
    {
        public string Name;
        public string Owner;
        public float[] M;
        public bool Orthographic;
        public float OrthoSize;
        public float Near, Far, Fov;
        public string Material;
    }
}
