using System.Text.Json.Nodes;

namespace UnityExtractor.Unity;

/// <summary>Counts every component type and MonoBehaviour script used by the flattened scenes and prefabs.</summary>
public sealed class InventoryBuilder
{
    private sealed class Counter
    {
        public string? ScriptPath;
        public string? ScriptGuid;
        public int Total;
        public SortedDictionary<string, int> PerScene = new SortedDictionary<string, int>(StringComparer.Ordinal);
        public HashSet<string> Prefabs = new HashSet<string>(StringComparer.Ordinal);
    }

    private readonly Dictionary<string, Counter> _components = new Dictionary<string, Counter>(StringComparer.Ordinal);
    private readonly Dictionary<string, Counter> _scripts = new Dictionary<string, Counter>(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> _gameObjectsPerScene = new SortedDictionary<string, int>(StringComparer.Ordinal);

    public void AddScene(string sceneName, IEnumerable<Obj> live)
    {
        int gos = 0;
        foreach (Obj o in live)
        {
            if (o.ClassId == UnityClassIds.GameObject)
            {
                gos++;
                continue;
            }

            Counter c = CounterFor(o);
            c.Total++;
            c.PerScene[sceneName] = c.PerScene.GetValueOrDefault(sceneName) + 1;
        }

        _gameObjectsPerScene[sceneName] = gos;
    }

    public void AddPrefab(string prefabPath, IEnumerable<Obj> live)
    {
        foreach (Obj o in live)
        {
            if (o.ClassId != UnityClassIds.GameObject)
            {
                CounterFor(o).Prefabs.Add(prefabPath);
            }
        }
    }

    private Counter CounterFor(Obj o)
    {
        Dictionary<string, Counter> map = _components;
        string key = o.Type;
        if (o.ClassId == UnityClassIds.MonoBehaviour)
        {
            map = _scripts;
            key = SceneExporter.ScriptName(o);
        }

        if (!map.TryGetValue(key, out Counter? c))
        {
            c = new Counter();
            map[key] = c;
        }

        if (o.ClassId == UnityClassIds.MonoBehaviour && c.ScriptPath == null)
        {
            c.ScriptPath = o.Props["m_Script"]?["path"]?.ToString();
            c.ScriptGuid = o.Props["m_Script"]?["guid"]?.ToString();
        }

        return c;
    }

    public JsonObject ToJson()
    {
        JsonObject root = new JsonObject();
        JsonObject gos = new JsonObject();
        foreach (KeyValuePair<string, int> kv in _gameObjectsPerScene)
        {
            gos[kv.Key] = kv.Value;
        }

        root["gameObjectsPerScene"] = gos;
        root["components"] = Section(_components, false);
        root["monoBehaviourScripts"] = Section(_scripts, true);
        return root;
    }

    private static JsonArray Section(Dictionary<string, Counter> map, bool scripts)
    {
        JsonArray arr = new JsonArray();
        foreach (KeyValuePair<string, Counter> kv in map.OrderByDescending(x => x.Value.Total).ThenBy(x => x.Key, StringComparer.Ordinal))
        {
            JsonObject j = new JsonObject();
            j[scripts ? "script" : "type"] = kv.Key;
            if (scripts)
            {
                j["scriptPath"] = kv.Value.ScriptPath;
                j["scriptGuid"] = kv.Value.ScriptGuid;
                j["projectScript"] = kv.Value.ScriptPath != null && kv.Value.ScriptPath.StartsWith("Assets/", StringComparison.Ordinal);
            }

            j["totalInScenes"] = kv.Value.Total;
            JsonObject per = new JsonObject();
            foreach (KeyValuePair<string, int> s in kv.Value.PerScene)
            {
                per[s.Key] = s.Value;
            }

            j["perScene"] = per;
            j["prefabAssetsContaining"] = kv.Value.Prefabs.Count;
            arr.Add(j);
        }

        return arr;
    }
}
