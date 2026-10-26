namespace UnityExtractor.Unity;

public static class UnityClassIds
{
    public const int GameObject = 1;
    public const int Transform = 4;
    public const int MonoBehaviour = 114;
    public const int RectTransform = 224;
    public const int PrefabInstance = 1001;
    public const int SceneRoots = 1660057539;
    public const int OcclusionCullingSettings = 29;
    public const int RenderSettings = 104;
    public const int LightmapSettings = 157;
    public const int NavMeshSettings = 196;
    public const int Material = 21;

    private static readonly Dictionary<int, string> Names = new Dictionary<int, string>
    {
        { 1, "GameObject" },
        { 4, "Transform" },
        { 20, "Camera" },
        { 21, "Material" },
        { 23, "MeshRenderer" },
        { 29, "OcclusionCullingSettings" },
        { 33, "MeshFilter" },
        { 54, "Rigidbody" },
        { 64, "MeshCollider" },
        { 65, "BoxCollider" },
        { 81, "AudioListener" },
        { 82, "AudioSource" },
        { 95, "Animator" },
        { 96, "TrailRenderer" },
        { 104, "RenderSettings" },
        { 108, "Light" },
        { 111, "Animation" },
        { 114, "MonoBehaviour" },
        { 119, "Projector" },
        { 120, "LineRenderer" },
        { 135, "SphereCollider" },
        { 136, "CapsuleCollider" },
        { 137, "SkinnedMeshRenderer" },
        { 143, "CharacterController" },
        { 157, "LightmapSettings" },
        { 195, "NavMeshAgent" },
        { 196, "NavMeshSettings" },
        { 198, "ParticleSystem" },
        { 199, "ParticleSystemRenderer" },
        { 205, "LODGroup" },
        { 212, "SpriteRenderer" },
        { 222, "CanvasRenderer" },
        { 223, "Canvas" },
        { 224, "RectTransform" },
        { 225, "CanvasGroup" },
        { 1001, "PrefabInstance" },
        { 1660057539, "SceneRoots" },
    };

    public static string NameOf(int classId, string? yamlTypeName = null)
    {
        if (Names.TryGetValue(classId, out string? name))
        {
            return name;
        }

        if (!string.IsNullOrEmpty(yamlTypeName))
        {
            return yamlTypeName;
        }

        return "Class" + classId;
    }

    public static bool IsSettings(int classId)
    {
        return classId == OcclusionCullingSettings
            || classId == RenderSettings
            || classId == LightmapSettings
            || classId == NavMeshSettings;
    }

    public static bool IsTransform(int classId)
    {
        return classId == Transform || classId == RectTransform;
    }
}
