using System.Text.Json.Nodes;

namespace UnityExtractor.Unity;

/// <summary>
/// Rewrites Unity object references ({fileID, guid, type} maps) inside a property tree.
/// External refs (with a GUID) become {guid, path, fileID, name?}; local refs are delegated to a resolver.
/// </summary>
public sealed class RefRewriter
{
    private readonly AssetIndex _index;
    private readonly Func<long, JsonNode?> _local;

    public int ExternalRefsMissing;

    public RefRewriter(AssetIndex index, Func<long, JsonNode?> localResolver)
    {
        _index = index;
        _local = localResolver;
    }

    public static bool IsRefMap(JsonObject o)
    {
        if (!o.ContainsKey("fileID"))
        {
            return false;
        }

        foreach (KeyValuePair<string, JsonNode?> kv in o)
        {
            if (kv.Key != "fileID" && kv.Key != "guid" && kv.Key != "type")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Rewrites the node, returning the replacement (which may be the same instance).</summary>
    public JsonNode? Rewrite(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (IsRefMap(obj))
            {
                return MakeRef(obj);
            }

            foreach (string key in obj.Select(kv => kv.Key).ToList())
            {
                JsonNode? before = obj[key];
                JsonNode? after = Rewrite(before);
                if (!ReferenceEquals(before, after))
                {
                    obj[key] = after;
                }
            }

            return obj;
        }

        if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                JsonNode? before = arr[i];
                JsonNode? after = Rewrite(before);
                if (!ReferenceEquals(before, after))
                {
                    arr[i] = after;
                }
            }

            return arr;
        }

        return node;
    }

    public JsonNode? MakeRef(JsonObject r)
    {
        long fileId = ToLong(r["fileID"]);
        string guid = r["guid"]?.ToString() ?? "";
        if (guid.Length == 0)
        {
            if (fileId == 0)
            {
                return null;
            }

            return _local(fileId);
        }

        if (fileId == 0 && AssetIndex.IsBuiltinGuid(guid))
        {
            return null;
        }

        return MakeExternal(fileId, guid);
    }

    public JsonNode? MakeExternal(long fileId, string guid)
    {
        if (fileId == 0 && string.IsNullOrEmpty(guid))
        {
            return null;
        }

        JsonObject result = new JsonObject();
        if (AssetIndex.IsBuiltinGuid(guid))
        {
            result["guid"] = guid;
            result["path"] = AssetIndex.BuiltinName(guid);
            result["fileID"] = fileId;
            result["builtin"] = true;
            return result;
        }

        AssetEntry? entry = _index.FindByGuid(guid);
        result["guid"] = guid;
        if (entry == null)
        {
            result["path"] = null;
            result["fileID"] = fileId;
            result["missing"] = true;
            ExternalRefsMissing++;
            return result;
        }

        result["path"] = entry.RelPath;
        result["fileID"] = fileId;
        if (_index.SubAssetNames(entry).TryGetValue(fileId, out string? name))
        {
            result["name"] = name;
        }

        return result;
    }

    public static long ToLong(JsonNode? n)
    {
        if (n is JsonValue v)
        {
            if (v.TryGetValue(out long l))
            {
                return l;
            }

            if (v.TryGetValue(out string? s) && long.TryParse(s, out long parsed))
            {
                return parsed;
            }

            if (v.TryGetValue(out double d))
            {
                return (long)d;
            }
        }

        return 0;
    }

    /// <summary>The identity a local reference points at: "ref" (flatten mode) or "fileID" (raw mode), or null.</summary>
    public static string? RefId(JsonNode? n)
    {
        if (n is JsonObject o && !o.ContainsKey("guid"))
        {
            if (o["ref"] != null)
            {
                return o["ref"]!.ToString();
            }

            if (o["fileID"] != null)
            {
                return o["fileID"]!.ToString();
            }
        }

        return null;
    }
}
