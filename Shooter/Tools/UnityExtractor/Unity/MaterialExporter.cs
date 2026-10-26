using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace UnityExtractor.Unity;

/// <summary>Produces materials.json: shader, textures (tiling/offset), colors, floats and derived render state.</summary>
public sealed class MaterialExporter
{
    private readonly AssetIndex _index;
    private readonly PrefabExpander _expander;
    private readonly RefRewriter _rewriter;

    public MaterialExporter(AssetIndex index, PrefabExpander expander)
    {
        _index = index;
        _expander = expander;
        _rewriter = new RefRewriter(index, id => null);
    }

    public JsonObject Export(AssetCollector collector)
    {
        JsonArray materials = new JsonArray();
        JsonArray embedded = new JsonArray();
        JsonObject shaders = new JsonObject();

        foreach (ManifestAsset a in collector.Assets.Values.OrderBy(x => x.Entry.RelPath, StringComparer.Ordinal))
        {
            if (a.Type == "material")
            {
                JsonObject? m = ExportMaterial(a.Entry);
                if (m != null)
                {
                    materials.Add(m);
                    string shaderName = m["shader"]?["name"]?.ToString() ?? "?";
                    shaders[shaderName] = (RefRewriter.ToLong(shaders[shaderName]) + 1);
                }
            }
            else if (a.Type == "model")
            {
                foreach (KeyValuePair<long, string> kv in a.SubAssetsUsed)
                {
                    if (AssetCollector.SubAssetClass(kv.Key) == "Material")
                    {
                        JsonObject e = new JsonObject();
                        e["model"] = a.Entry.RelPath;
                        e["fileID"] = kv.Key;
                        e["name"] = kv.Value;
                        e["note"] = "Material embedded in model file; no .mat asset, uses model importer defaults/remap.";
                        embedded.Add(e);
                    }
                }
            }
        }

        JsonObject root = new JsonObject();
        root["materialCount"] = materials.Count;
        root["shaderUsage"] = shaders;
        root["materials"] = materials;
        root["embeddedModelMaterials"] = embedded;
        return root;
    }

    private JsonObject? ExportMaterial(AssetEntry entry)
    {
        UnityFile file = _expander.LoadFile(entry);
        UnityDoc? doc = file.Docs.FirstOrDefault(d => d.ClassId == UnityClassIds.Material);
        if (doc == null)
        {
            return null;
        }

        JsonObject p = doc.Props;
        JsonObject m = new JsonObject();
        m["path"] = entry.RelPath;
        m["guid"] = entry.Guid;
        m["name"] = p["m_Name"]?.DeepClone();

        JsonObject shader = new JsonObject();
        JsonObject? shaderRef = p["m_Shader"] as JsonObject;
        string shaderGuid = shaderRef?["guid"]?.ToString() ?? "";
        long shaderFileId = RefRewriter.ToLong(shaderRef?["fileID"]);
        AssetEntry? shaderEntry = _index.FindByGuid(shaderGuid);
        shader["name"] = ShaderName(shaderEntry, shaderGuid, shaderFileId);
        shader["path"] = shaderEntry?.RelPath;
        shader["guid"] = shaderGuid;
        m["shader"] = shader;

        JsonObject? parent = p["m_Parent"] as JsonObject;
        if (parent != null && RefRewriter.ToLong(parent["fileID"]) != 0)
        {
            m["parent"] = _rewriter.MakeRef((JsonObject)parent.DeepClone());
        }

        JsonArray keywords = new JsonArray();
        if (p["m_ValidKeywords"] is JsonArray vk)
        {
            foreach (JsonNode? k in vk)
            {
                keywords.Add(k?.ToString());
            }
        }

        string? legacyKeywords = p["m_ShaderKeywords"]?.ToString();
        if (!string.IsNullOrEmpty(legacyKeywords))
        {
            foreach (string k in legacyKeywords.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                keywords.Add(k);
            }
        }

        m["keywords"] = keywords;
        m["renderQueue"] = p["m_CustomRenderQueue"]?.DeepClone();
        m["lightmapFlags"] = p["m_LightmapFlags"]?.DeepClone();
        m["doubleSidedGI"] = p["m_DoubleSidedGI"]?.DeepClone();
        if (p["stringTagMap"] is JsonObject tags && tags.Count > 0)
        {
            m["tags"] = tags.DeepClone();
        }

        JsonObject? saved = p["m_SavedProperties"] as JsonObject;
        JsonObject textures = new JsonObject();
        JsonObject colors = new JsonObject();
        JsonObject floats = new JsonObject();
        JsonObject ints = new JsonObject();

        foreach ((string name, JsonNode? value) in Entries(saved?["m_TexEnvs"]))
        {
            JsonObject? tex = value as JsonObject;
            JsonObject? texRef = tex?["m_Texture"] as JsonObject;
            if (texRef == null || RefRewriter.ToLong(texRef["fileID"]) == 0)
            {
                continue;
            }

            JsonObject t = new JsonObject();
            t["texture"] = _rewriter.MakeRef((JsonObject)texRef.DeepClone());
            t["tiling"] = new JsonArray(Num(tex?["m_Scale"]?["x"], 1), Num(tex?["m_Scale"]?["y"], 1));
            t["offset"] = new JsonArray(Num(tex?["m_Offset"]?["x"], 0), Num(tex?["m_Offset"]?["y"], 0));
            textures[name] = t;
        }

        foreach ((string name, JsonNode? value) in Entries(saved?["m_Colors"]))
        {
            if (value is JsonObject c)
            {
                colors[name] = new JsonArray(Num(c["r"], 0), Num(c["g"], 0), Num(c["b"], 0), Num(c["a"], 1));
            }
        }

        foreach ((string name, JsonNode? value) in Entries(saved?["m_Floats"]))
        {
            floats[name] = value?.DeepClone();
        }

        foreach ((string name, JsonNode? value) in Entries(saved?["m_Ints"]))
        {
            ints[name] = value?.DeepClone();
        }

        m["textures"] = textures;
        m["colors"] = colors;
        m["floats"] = floats;
        if (ints.Count > 0)
        {
            m["ints"] = ints;
        }

        m["derived"] = Derive(keywords, textures, colors, floats, shader["name"]?.ToString());
        return m;
    }

    private JsonObject Derive(JsonArray keywords, JsonObject textures, JsonObject colors, JsonObject floats, string? shaderName)
    {
        HashSet<string> kw = new HashSet<string>(keywords.Select(k => k?.ToString() ?? ""), StringComparer.Ordinal);
        JsonObject d = new JsonObject();

        JsonNode? Pick(JsonObject src, params string[] names)
        {
            foreach (string n in names)
            {
                if (src[n] != null)
                {
                    return src[n]!.DeepClone();
                }
            }

            return null;
        }

        d["baseColor"] = Pick(colors, "_BaseColor", "_Color");
        d["baseMap"] = Pick(textures, "_BaseMap", "_MainTex")?["texture"]?["path"]?.DeepClone();
        d["smoothness"] = Pick(floats, "_Smoothness", "_Glossiness");
        d["metallic"] = Pick(floats, "_Metallic");
        d["emissionColor"] = Pick(colors, "_EmissionColor");
        d["emissionMap"] = Pick(textures, "_EmissionMap")?["texture"]?["path"]?.DeepClone();
        d["emissionEnabled"] = kw.Contains("_EMISSION");
        d["normalMap"] = Pick(textures, "_BumpMap")?["texture"]?["path"]?.DeepClone();

        JsonNode? cull = Pick(floats, "_Cull");
        if (cull != null)
        {
            d["cullMode"] = RefRewriter.ToLong(cull) switch { 0 => "Off", 1 => "Front", _ => "Back" };
        }

        // The keyword is authoritative: some upgraded materials keep a stale _Surface value.
        JsonNode? surface = Pick(floats, "_Surface");
        if (kw.Contains("_SURFACE_TYPE_TRANSPARENT") || (surface != null && RefRewriter.ToLong(surface) == 1))
        {
            d["surfaceType"] = "Transparent";
        }
        else if (surface != null || shaderName != null)
        {
            d["surfaceType"] = "Opaque";
        }

        JsonNode? blend = Pick(floats, "_Blend");
        if (blend != null)
        {
            d["blendMode"] = RefRewriter.ToLong(blend) switch { 0 => "Alpha", 1 => "Premultiply", 2 => "Additive", 3 => "Multiply", _ => "Unknown" };
        }

        d["srcBlend"] = Pick(floats, "_SrcBlend");
        d["dstBlend"] = Pick(floats, "_DstBlend");
        d["zWrite"] = Pick(floats, "_ZWrite");
        d["alphaClip"] = Pick(floats, "_AlphaClip");
        d["alphaCutoff"] = Pick(floats, "_Cutoff");
        d["receiveShadows"] = Pick(floats, "_ReceiveShadows");
        d["workflow"] = Pick(floats, "_WorkflowMode");
        return d;
    }

    private static IEnumerable<(string, JsonNode?)> Entries(JsonNode? list)
    {
        if (list is JsonArray arr)
        {
            foreach (JsonNode? item in arr)
            {
                if (item is JsonObject o)
                {
                    foreach (KeyValuePair<string, JsonNode?> kv in o)
                    {
                        yield return (kv.Key, kv.Value);
                    }
                }
            }
        }
    }

    private static double Num(JsonNode? n, double fallback)
    {
        if (n is JsonValue v)
        {
            if (v.TryGetValue(out double d))
            {
                return Math.Round(d, 6);
            }

            if (v.TryGetValue(out long l))
            {
                return l;
            }
        }

        return fallback;
    }

    private string ShaderName(AssetEntry? entry, string guid, long fileId)
    {
        if (entry == null)
        {
            return AssetIndex.IsBuiltinGuid(guid) ? "builtin shader (fileID " + fileId + ")" : "<unknown shader " + guid + ">";
        }

        if (entry.Extension == ".shader")
        {
            try
            {
                using StreamReader r = new StreamReader(entry.AbsPath);
                char[] buf = new char[2048];
                int n = r.Read(buf, 0, buf.Length);
                Match m = Regex.Match(new string(buf, 0, n), "Shader\\s+\"([^\"]+)\"");
                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
            }
            catch (IOException)
            {
            }
        }
        else if (entry.Extension == ".shadergraph" || entry.Extension == ".shadersubgraph")
        {
            // Shader Graph assets are named "Shader Graphs/<file name>" when used by materials.
            return "Shader Graphs/" + System.IO.Path.GetFileNameWithoutExtension(entry.RelPath);
        }

        return entry.RelPath;
    }
}
