using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UnityExtractor.Yaml;

namespace UnityExtractor.Unity;

public sealed class AssetEntry
{
    public string Guid = "";
    public string RelPath = "";
    public string AbsPath = "";
    public bool IsDirectory;

    public string Extension
    {
        get { return System.IO.Path.GetExtension(RelPath).ToLowerInvariant(); }
    }
}

/// <summary>GUID to asset path index built from *.meta files.</summary>
public sealed class AssetIndex
{
    private static readonly Regex GuidRegex = new Regex(@"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly Dictionary<string, AssetEntry> _byGuid = new Dictionary<string, AssetEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AssetEntry> _byPath = new Dictionary<string, AssetEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, JsonObject?> _metaCache = new Dictionary<string, JsonObject?>();
    private readonly Dictionary<string, Dictionary<long, string>> _subAssetCache = new Dictionary<string, Dictionary<long, string>>();

    public string ProjectRoot { get; }

    public IEnumerable<AssetEntry> All
    {
        get { return _byGuid.Values; }
    }

    public int Count
    {
        get { return _byGuid.Count; }
    }

    public AssetIndex(string projectRoot)
    {
        ProjectRoot = projectRoot;
        IndexTree(System.IO.Path.Combine(projectRoot, "Assets"), "Assets");
        IndexTree(System.IO.Path.Combine(projectRoot, "Packages"), "Packages");

        // Registry/built-in packages are resolved into Library/PackageCache (folder names are "name@hash").
        string cache = System.IO.Path.Combine(projectRoot, "Library", "PackageCache");
        if (Directory.Exists(cache))
        {
            foreach (string dir in Directory.EnumerateDirectories(cache))
            {
                string name = System.IO.Path.GetFileName(dir);
                int at = name.IndexOf('@');
                if (at > 0)
                {
                    name = name.Substring(0, at);
                }

                string virtualRoot = "Packages/" + name;
                if (!_byPath.Keys.Any(k => k.StartsWith(virtualRoot + "/", StringComparison.Ordinal)))
                {
                    IndexTree(dir, virtualRoot);
                }
            }
        }
    }

    private void IndexTree(string absDir, string virtualDir)
    {
        if (!Directory.Exists(absDir))
        {
            return;
        }

        foreach (string meta in Directory.EnumerateFiles(absDir, "*.meta", SearchOption.AllDirectories))
        {
            string abs = meta.Substring(0, meta.Length - ".meta".Length);
            bool isDir = Directory.Exists(abs);
            if (!isDir && !File.Exists(abs))
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(meta);
            }
            catch (IOException)
            {
                continue;
            }

            Match m = GuidRegex.Match(text);
            if (!m.Success)
            {
                continue;
            }

            string rel = virtualDir + "/" + System.IO.Path.GetRelativePath(absDir, abs).Replace('\\', '/');
            AssetEntry entry = new AssetEntry
            {
                Guid = m.Groups[1].Value.ToLowerInvariant(),
                RelPath = rel,
                AbsPath = abs,
                IsDirectory = isDir
            };
            _byGuid[entry.Guid] = entry;
            _byPath[rel] = entry;
        }
    }

    public AssetEntry? FindByGuid(string? guid)
    {
        if (string.IsNullOrEmpty(guid))
        {
            return null;
        }

        return _byGuid.TryGetValue(guid, out AssetEntry? e) ? e : null;
    }

    public AssetEntry? FindByPath(string relPath)
    {
        return _byPath.TryGetValue(relPath, out AssetEntry? e) ? e : null;
    }

    public static bool IsBuiltinGuid(string? guid)
    {
        if (string.IsNullOrEmpty(guid))
        {
            return false;
        }

        return guid.StartsWith("0000000000000000", StringComparison.Ordinal);
    }

    public static string BuiltinName(string guid)
    {
        if (guid.StartsWith("0000000000000000e", StringComparison.Ordinal))
        {
            return "builtin:extra-resources";
        }

        if (guid.StartsWith("0000000000000000f", StringComparison.Ordinal))
        {
            return "builtin:default-resources";
        }

        return "builtin:" + guid;
    }

    /// <summary>Parsed .meta of an asset (null when unavailable).</summary>
    public JsonObject? ReadMeta(AssetEntry entry)
    {
        if (_metaCache.TryGetValue(entry.Guid, out JsonObject? cached))
        {
            return cached;
        }

        JsonObject? result = null;
        string metaPath = entry.AbsPath + ".meta";
        if (File.Exists(metaPath))
        {
            YamlNode root = UnityYamlParser.Parse(File.ReadLines(metaPath));
            result = root.ToJson() as JsonObject;
        }

        _metaCache[entry.Guid] = result;
        return result;
    }

    /// <summary>Sub-asset names (e.g. meshes inside an FBX) keyed by local fileID, from the .meta name table and sprite sheet.</summary>
    public Dictionary<long, string> SubAssetNames(AssetEntry entry)
    {
        if (_subAssetCache.TryGetValue(entry.Guid, out Dictionary<long, string>? cached))
        {
            return cached;
        }

        Dictionary<long, string> result = new Dictionary<long, string>();
        string ext = entry.Extension;
        if (ext == ".fbx" || ext == ".obj" || ext == ".dae" || ext == ".png" || ext == ".psd" || ext == ".tga" || ext == ".jpg")
        {
            JsonObject? meta = ReadMeta(entry);
            if (meta != null)
            {
                CollectSubAssets(meta, result);
            }
        }

        _subAssetCache[entry.Guid] = result;
        return result;
    }

    private static void CollectSubAssets(JsonNode? node, Dictionary<long, string> result)
    {
        if (node is JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonNode?> kv in obj)
            {
                if (kv.Key == "internalIDToNameTable" && kv.Value is JsonArray table)
                {
                    foreach (JsonNode? item in table)
                    {
                        if (item is JsonObject io && io["first"] is JsonObject first && io["second"] != null)
                        {
                            foreach (KeyValuePair<string, JsonNode?> f in first)
                            {
                                if (f.Value is JsonValue v && v.TryGetValue(out long id))
                                {
                                    result[id] = io["second"]!.ToString();
                                }
                            }
                        }
                    }
                }
                else if (kv.Key == "sprites" && kv.Value is JsonArray sprites)
                {
                    foreach (JsonNode? sp in sprites)
                    {
                        if (sp is JsonObject so && so["internalID"] is JsonValue iv && iv.TryGetValue(out long sid) && so["name"] != null)
                        {
                            result[sid] = so["name"]!.ToString();
                        }
                    }
                }
                else
                {
                    CollectSubAssets(kv.Value, result);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (JsonNode? item in arr)
            {
                CollectSubAssets(item, result);
            }
        }
    }
}
