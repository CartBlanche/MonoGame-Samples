using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace UnityExtractor.Unity;

public sealed class ManifestAsset
{
    public AssetEntry Entry = null!;
    public string Type = "other";
    public SortedSet<string> ReferencedBy = new SortedSet<string>(StringComparer.Ordinal);
    public SortedDictionary<long, string> SubAssetsUsed = new SortedDictionary<long, string>();
    public string? Detail;
}

/// <summary>Transitively collects every asset reachable from a set of root scenes by scanning GUID references.</summary>
public sealed class AssetCollector
{
    private static readonly Regex RefRegex =
        new Regex(@"(?:fileID: (-?\d+), )?guid: ([0-9a-f]{32})", RegexOptions.Compiled);

    private static readonly HashSet<string> YamlExtensions = new HashSet<string>(StringComparer.Ordinal)
    {
        ".unity", ".prefab", ".mat", ".controller", ".anim", ".asset", ".mask", ".mixer", ".lighting",
        ".overridecontroller", ".playable", ".physicmaterial", ".flare", ".rendertexture", ".cubemap",
        ".terrainlayer", ".shadervariants", ".spriteatlas"
    };

    private static readonly HashSet<string> ModelExtensions = new HashSet<string>(StringComparer.Ordinal)
    {
        ".fbx", ".obj", ".dae", ".blend", ".3ds", ".max", ".ma", ".mb"
    };

    private readonly AssetIndex _index;

    public Dictionary<string, ManifestAsset> Assets { get; } = new Dictionary<string, ManifestAsset>(StringComparer.OrdinalIgnoreCase);

    public SortedDictionary<string, SortedSet<string>> UnresolvedGuids { get; } = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

    public AssetCollector(AssetIndex index)
    {
        _index = index;
    }

    public void Collect(IEnumerable<AssetEntry> roots)
    {
        Queue<AssetEntry> queue = new Queue<AssetEntry>();
        HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<AssetEntry> rootList = roots.ToList();
        foreach (AssetEntry r in rootList)
        {
            visited.Add(r.Guid);
            queue.Enqueue(r);
            Assets[r.Guid] = new ManifestAsset { Entry = r, Type = Classify(r) };
        }

        while (queue.Count > 0)
        {
            AssetEntry cur = queue.Dequeue();
            string? text = ReadScannableText(cur);
            if (text == null)
            {
                continue;
            }

            foreach (Match m in RefRegex.Matches(text))
            {
                string guid = m.Groups[2].Value;
                if (AssetIndex.IsBuiltinGuid(guid) || guid == cur.Guid)
                {
                    continue;
                }

                AssetEntry? target = _index.FindByGuid(guid);
                if (target == null)
                {
                    if (!UnresolvedGuids.TryGetValue(guid, out SortedSet<string>? by))
                    {
                        by = new SortedSet<string>(StringComparer.Ordinal);
                        UnresolvedGuids[guid] = by;
                    }

                    by.Add(cur.RelPath);
                    continue;
                }

                if (!Assets.TryGetValue(target.Guid, out ManifestAsset? asset))
                {
                    asset = new ManifestAsset { Entry = target, Type = Classify(target) };
                    Assets[target.Guid] = asset;
                }

                asset.ReferencedBy.Add(cur.RelPath);
                if (m.Groups[1].Success && long.TryParse(m.Groups[1].Value, out long fileId))
                {
                    Dictionary<long, string> names = _index.SubAssetNames(target);
                    if (names.TryGetValue(fileId, out string? name))
                    {
                        asset.SubAssetsUsed[fileId] = name;
                    }
                }

                if (visited.Add(target.Guid))
                {
                    queue.Enqueue(target);
                }
            }
        }
    }

    private string? ReadScannableText(AssetEntry entry)
    {
        if (entry.IsDirectory)
        {
            return null;
        }

        string ext = entry.Extension;
        try
        {
            if (YamlExtensions.Contains(ext))
            {
                return File.ReadAllText(entry.AbsPath);
            }

            if (ModelExtensions.Contains(ext) && File.Exists(entry.AbsPath + ".meta"))
            {
                return File.ReadAllText(entry.AbsPath + ".meta");
            }
        }
        catch (IOException)
        {
        }

        return null;
    }

    private string Classify(AssetEntry e)
    {
        string ext = e.Extension;
        switch (ext)
        {
            case ".fbx": case ".obj": case ".dae": case ".blend": case ".3ds": case ".max": case ".ma": case ".mb":
                return "model";
            case ".mat": return "material";
            case ".png": case ".jpg": case ".jpeg": case ".tga": case ".psd": case ".bmp": case ".gif":
            case ".tif": case ".tiff": case ".exr": case ".hdr":
                return IsSpriteTexture(e) ? "sprite" : "texture";
            case ".wav": case ".mp3": case ".ogg": case ".aiff": case ".aif": case ".flac": return "audio";
            case ".anim": return "animationClip";
            case ".controller": return "animatorController";
            case ".overridecontroller": return "animatorOverrideController";
            case ".mask": return "avatarMask";
            case ".mixer": return "audioMixer";
            case ".ttf": case ".otf": case ".fontsettings": return "font";
            case ".shader": case ".shadergraph": case ".cginc": case ".hlsl": case ".compute": return "shader";
            case ".cs": return "script";
            case ".prefab": return "prefab";
            case ".unity": return "scene";
            case ".mov": case ".mp4": case ".webm": return "video";
            case ".lighting": return "lightingSettings";
            case ".inputactions": return "inputActions";
            case ".flare": return "flare";
            case ".cubemap": return "cubemap";
            case ".rendertexture": return "renderTexture";
            case ".asset": return ClassifyAsset(e);
            default: return "other";
        }
    }

    private bool IsSpriteTexture(AssetEntry e)
    {
        JsonObject? meta = _index.ReadMeta(e);
        if (meta?["TextureImporter"] is JsonObject ti)
        {
            long type = RefRewriter.ToLong(ti["textureType"]);
            return type == 8;
        }

        return false;
    }

    private string ClassifyAsset(AssetEntry e)
    {
        try
        {
            using StreamReader reader = new StreamReader(e.AbsPath);
            char[] buf = new char[16384];
            int n = reader.Read(buf, 0, buf.Length);
            string head = new string(buf, 0, n);
            if (head.Contains("m_FaceInfo") || head.Contains("m_AtlasTextures"))
            {
                return "fontAsset";
            }

            Match m = Regex.Match(head, @"^--- !u!(\d+) ", RegexOptions.Multiline);
            if (m.Success)
            {
                switch (int.Parse(m.Groups[1].Value))
                {
                    case 43: return "meshAsset";
                    case 238: return "navMeshData";
                    case 1120: return "lightingData";
                    case 114: return "scriptableObject";
                    case 128: return "font";
                    case 21: return "material";
                    case 1: return "asset";
                }
            }
        }
        catch (IOException)
        {
        }

        return "asset";
    }

    public static string SubAssetClassOr(long fileId, int classId)
    {
        string name = SubAssetClass(fileId);
        if (name != "Object")
        {
            return name;
        }

        return classId > 0 ? UnityClassIds.NameOf(classId) : "Object";
    }

    public static string SubAssetClass(long fileId)
    {
        if (fileId <= 0 || fileId >= 100000000)
        {
            return "Object";
        }

        return (fileId / 100000) switch
        {
            1 => "GameObject",
            4 => "Transform",
            21 => "Material",
            28 => "Texture2D",
            43 => "Mesh",
            74 => "AnimationClip",
            83 => "AudioClip",
            91 => "AnimatorController",
            137 => "SkinnedMeshRenderer",
            213 => "Sprite",
            var id => UnityClassIds.NameOf((int)id) is string n && !n.StartsWith("Class", StringComparison.Ordinal) ? n : "Object"
        };
    }

    public JsonObject ToJson(IEnumerable<string> scenes)
    {
        JsonObject root = new JsonObject();
        root["scenes"] = new JsonArray(scenes.Select(s => (JsonNode?)JsonValue.Create(s)).ToArray());

        List<ManifestAsset> ordered = Assets.Values
            .OrderBy(a => a.Type, StringComparer.Ordinal)
            .ThenBy(a => a.Entry.RelPath, StringComparer.Ordinal)
            .ToList();

        JsonObject summary = new JsonObject();
        foreach (IGrouping<string, ManifestAsset> g in ordered.GroupBy(a => a.Type).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            summary[g.Key] = g.Count();
        }

        root["summaryByType"] = summary;

        JsonArray assets = new JsonArray();
        foreach (ManifestAsset a in ordered)
        {
            JsonObject j = new JsonObject();
            j["path"] = a.Entry.RelPath;
            j["guid"] = a.Entry.Guid;
            j["type"] = a.Type;
            j["referencedBy"] = new JsonArray(a.ReferencedBy.Select(r => (JsonNode?)JsonValue.Create(r)).ToArray());

            if (a.SubAssetsUsed.Count > 0)
            {
                JsonArray subs = new JsonArray();
                foreach (KeyValuePair<long, string> kv in a.SubAssetsUsed)
                {
                    JsonObject s = new JsonObject();
                    s["fileID"] = kv.Key;
                    s["name"] = kv.Value;
                    s["class"] = SubAssetClass(kv.Key);
                    subs.Add(s);
                }

                j["subAssetsUsed"] = subs;
            }

            JsonObject? import = ImportInfo(a);
            if (import != null)
            {
                j["import"] = import;
            }

            assets.Add(j);
        }

        root["assets"] = assets;
        JsonArray unresolved = new JsonArray();
        foreach (KeyValuePair<string, SortedSet<string>> kv in UnresolvedGuids)
        {
            JsonObject u = new JsonObject();
            u["guid"] = kv.Key;
            u["referencedBy"] = new JsonArray(kv.Value.Select(r => (JsonNode?)JsonValue.Create(r)).ToArray());
            unresolved.Add(u);
        }

        root["unresolvedGuids"] = unresolved;
        return root;
    }

    private JsonObject? ImportInfo(ManifestAsset a)
    {
        JsonObject? import = null;
        JsonObject? meta = null;
        if (a.Type == "texture" || a.Type == "sprite" || a.Type == "model" || a.Type == "audio")
        {
            meta = _index.ReadMeta(a.Entry);
        }

        if (a.Type == "texture" || a.Type == "sprite")
        {
            import = new JsonObject();
            if (a.Entry.Extension == ".png")
            {
                ReadPngSize(a.Entry.AbsPath, import);
            }

            if (meta?["TextureImporter"] is JsonObject ti)
            {
                foreach (string key in new[] { "textureType", "sRGBTexture", "alphaIsTransparency", "spriteMode", "maxTextureSize", "enableMipMap", "convertToNormalMap", "wrapU", "wrapV", "filterMode" })
                {
                    if (ti[key] != null)
                    {
                        import[key] = ti[key]!.DeepClone();
                    }
                }
            }
        }
        else if (a.Type == "model" && meta?["ModelImporter"] is JsonObject mi)
        {
            import = new JsonObject();
            foreach (string key in new[] { "globalScale", "useFileScale", "animationType", "importAnimation", "materialImportMode" })
            {
                if (mi[key] != null)
                {
                    import[key] = mi[key]!.DeepClone();
                }
            }

            if (mi["meshes"] is JsonObject meshes)
            {
                foreach (string key in new[] { "generateSecondaryUV", "importBlendShapes" })
                {
                    if (meshes[key] != null)
                    {
                        import[key] = meshes[key]!.DeepClone();
                    }
                }
            }
        }
        else if (a.Type == "audio" && meta?["AudioImporter"] is JsonObject ai)
        {
            import = new JsonObject();
            foreach (string key in new[] { "forceToMono", "loadInBackground" })
            {
                if (ai[key] != null)
                {
                    import[key] = ai[key]!.DeepClone();
                }
            }

            if (ai["defaultSettings"] is JsonObject ds)
            {
                import["defaultSettings"] = ds.DeepClone();
            }
        }

        return import;
    }

    private static void ReadPngSize(string path, JsonObject import)
    {
        try
        {
            using FileStream fs = File.OpenRead(path);
            byte[] header = new byte[24];
            if (fs.Read(header, 0, 24) == 24 && header[1] == (byte)'P' && header[2] == (byte)'N' && header[3] == (byte)'G')
            {
                import["width"] = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                import["height"] = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            }
        }
        catch (IOException)
        {
        }
    }
}
