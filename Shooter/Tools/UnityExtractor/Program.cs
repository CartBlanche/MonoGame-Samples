using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnityExtractor.Unity;

namespace UnityExtractor;

public static class Program
{
    private static readonly string[] DefaultScenes = { "IntroMenu", "MainScene", "WinScene", "LoseScene" };

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private sealed class Options
    {
        public string ProjectRoot = "";
        public string OutputDir = "";
        public List<string> Scenes = new List<string>();
        public bool Flatten;
        public bool PrefabsAll;
        public List<string> Extra = new List<string>();
    }

    public static int Main(string[] args)
    {
        Options? options = ParseArgs(args);
        if (options == null)
        {
            Console.Error.WriteLine("Usage: UnityExtractor <unityProjectRoot> <outputDir> [--scenes IntroMenu,MainScene,WinScene,LoseScene] [--flatten] [--prefabs-all] [--extra Assets/...prefab,...]");
            return 1;
        }

        try
        {
            return Run(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("error: " + ex);
            return 2;
        }
    }

    private static Options? ParseArgs(string[] args)
    {
        Options o = new Options();
        List<string> positional = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--flatten")
            {
                o.Flatten = true;
            }
            else if (a == "--prefabs-all")
            {
                o.PrefabsAll = true;
            }
            else if (a == "--extra")
            {
                if (i + 1 >= args.Length)
                {
                    return null;
                }

                o.Extra.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else if (a == "--scenes")
            {
                if (i + 1 >= args.Length)
                {
                    return null;
                }

                o.Scenes.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else if (a.StartsWith("--scenes=", StringComparison.Ordinal))
            {
                o.Scenes.AddRange(a.Substring("--scenes=".Length).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else if (a.StartsWith("--", StringComparison.Ordinal))
            {
                return null;
            }
            else
            {
                positional.Add(a);
            }
        }

        if (positional.Count != 2)
        {
            return null;
        }

        o.ProjectRoot = System.IO.Path.GetFullPath(positional[0]);
        o.OutputDir = System.IO.Path.GetFullPath(positional[1]);
        if (o.Scenes.Count == 0)
        {
            o.Scenes.AddRange(DefaultScenes);
        }

        return o;
    }

    private static int Run(Options options)
    {
        Console.WriteLine("Indexing assets in " + options.ProjectRoot + " ...");
        AssetIndex index = new AssetIndex(options.ProjectRoot);
        Console.WriteLine("  " + index.Count + " assets indexed");

        List<AssetEntry> sceneEntries = new List<AssetEntry>();
        foreach (string name in options.Scenes)
        {
            AssetEntry? e = ResolveScene(index, name);
            if (e == null)
            {
                Console.Error.WriteLine("error: scene '" + name + "' not found under Assets/");
                return 1;
            }

            sceneEntries.Add(e);
        }

        Directory.CreateDirectory(options.OutputDir);
        string scenesDir = System.IO.Path.Combine(options.OutputDir, "Scenes");
        string prefabsDir = System.IO.Path.Combine(options.OutputDir, "Prefabs");
        Directory.CreateDirectory(scenesDir);
        Directory.CreateDirectory(prefabsDir);

        Console.WriteLine("Collecting transitive asset references ...");
        AssetCollector collector = new AssetCollector(index);
        List<AssetEntry> extraEntries = new List<AssetEntry>();
        foreach (string path in options.Extra)
        {
            AssetEntry? e = index.FindByPath(path.Replace('\\', '/'));
            if (e == null)
            {
                Console.Error.WriteLine("error: extra asset '" + path + "' not found");
                return 1;
            }

            extraEntries.Add(e);
        }

        collector.Collect(sceneEntries.Concat(extraEntries).ToList());
        Console.WriteLine("  " + collector.Assets.Count + " assets reachable (including the scenes)");

        PrefabExpander expander = new PrefabExpander(index);
        SceneExporter exporter = new SceneExporter(index, expander);
        InventoryBuilder inventory = new InventoryBuilder();
        int totalWarnings = 0;

        foreach (AssetEntry e in sceneEntries)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(e.RelPath);
            UnityFile file = expander.LoadFile(e);
            ExportResult result = options.Flatten ? exporter.ExportFlattened(file, "scene") : exporter.ExportRaw(file, "scene");
            WriteJson(System.IO.Path.Combine(scenesDir, name + ".json"), result.Json);

            // The inventory always counts the fully resolved scene, regardless of the output mode.
            IEnumerable<Obj> live = options.Flatten ? result.LiveObjects : exporter.ExportFlattened(file, "scene").LiveObjects;
            inventory.AddScene(name, live);

            int warnCount = result.Json["warnings"] is JsonArray w ? w.Count : 0;
            totalWarnings += warnCount;
            Console.WriteLine("  scene " + name + ": " + live.Count(o => o.ClassId == UnityClassIds.GameObject) + " GameObjects, " + warnCount + " warnings");
        }

        // Prefabs: every prefab reachable from the scenes (via instances or serialized references).
        List<AssetEntry> prefabEntries = collector.Assets.Values
            .Where(a => a.Type == "prefab")
            .Select(a => a.Entry)
            .ToList();
        if (options.PrefabsAll)
        {
            HashSet<string> have = new HashSet<string>(prefabEntries.Select(p => p.Guid), StringComparer.OrdinalIgnoreCase);
            foreach (AssetEntry e in index.All)
            {
                if (e.Extension == ".prefab" && !e.IsDirectory && e.RelPath.StartsWith("Assets/", StringComparison.Ordinal) && have.Add(e.Guid))
                {
                    prefabEntries.Add(e);
                }
            }
        }

        prefabEntries = prefabEntries.OrderBy(p => p.RelPath, StringComparer.Ordinal).ToList();
        Dictionary<string, string> prefabFiles = UniqueNames(prefabEntries);
        JsonObject prefabIndex = new JsonObject();
        foreach (AssetEntry e in prefabEntries)
        {
            UnityFile file = expander.LoadFile(e);
            ExportResult result = options.Flatten ? exporter.ExportFlattened(file, "prefab") : exporter.ExportRaw(file, "prefab");
            WriteJson(System.IO.Path.Combine(prefabsDir, prefabFiles[e.RelPath]), result.Json);
            prefabIndex[e.RelPath] = "Prefabs/" + prefabFiles[e.RelPath];

            IEnumerable<Obj> live = options.Flatten ? result.LiveObjects : exporter.ExportFlattened(file, "prefab").LiveObjects;
            inventory.AddPrefab(e.RelPath, live);
            totalWarnings += result.Json["warnings"] is JsonArray w ? w.Count : 0;
        }

        WriteJson(System.IO.Path.Combine(prefabsDir, "index.json"), prefabIndex);
        Console.WriteLine("  " + prefabEntries.Count + " prefabs exported");

        WriteJson(System.IO.Path.Combine(options.OutputDir, "asset-manifest.json"),
            collector.ToJson(sceneEntries.Select(s => s.RelPath)));
        WriteJson(System.IO.Path.Combine(options.OutputDir, "component-inventory.json"), inventory.ToJson());

        MaterialExporter materials = new MaterialExporter(index, expander);
        WriteJson(System.IO.Path.Combine(options.OutputDir, "materials.json"), materials.Export(collector));

        Console.WriteLine("Done. " + totalWarnings + " warnings in total. Output: " + options.OutputDir);
        foreach (string w in expander.Warnings.Distinct().Take(15))
        {
            Console.WriteLine("  warning: " + w);
        }

        return 0;
    }

    private static AssetEntry? ResolveScene(AssetIndex index, string name)
    {
        string file = name.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ? name : name + ".unity";
        AssetEntry? direct = index.FindByPath(file.Replace('\\', '/'));
        if (direct != null)
        {
            return direct;
        }

        List<AssetEntry> matches = index.All
            .Where(a => !a.IsDirectory
                && a.RelPath.StartsWith("Assets/", StringComparison.Ordinal)
                && System.IO.Path.GetFileName(a.RelPath).Equals(file, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.RelPath.Contains("ModAssets", StringComparison.Ordinal) ? 1 : 0)
            .ThenBy(a => a.RelPath, StringComparer.Ordinal)
            .ToList();
        return matches.FirstOrDefault();
    }

    // Output file names for prefabs: the plain name, prefixed by parent folders only if it would collide.
    private static Dictionary<string, string> UniqueNames(List<AssetEntry> entries)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AssetEntry e in entries)
        {
            string[] parts = e.RelPath.Split('/');
            string stem = System.IO.Path.GetFileNameWithoutExtension(e.RelPath);
            string candidate = stem + ".json";
            int up = parts.Length - 2;
            while (!used.Add(candidate) && up >= 0)
            {
                stem = parts[up] + "__" + stem;
                candidate = stem + ".json";
                up--;
            }

            result[e.RelPath] = candidate;
        }

        return result;
    }

    private static void WriteJson(string path, JsonNode node)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, node.ToJsonString(JsonOptions));
    }
}
