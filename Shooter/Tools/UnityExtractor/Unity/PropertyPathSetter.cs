using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UnityExtractor.Yaml;

namespace UnityExtractor.Unity;

/// <summary>Applies a Unity PrefabInstance modification (propertyPath/value/objectReference) to a property tree.</summary>
public static class PropertyPathSetter
{
    private static readonly Regex DataRegex = new Regex(@"^data\[(\d+)\]$", RegexOptions.Compiled);

    /// <summary>
    /// Returns false when the path could not be followed.
    /// <paramref name="objRef"/> is the already-rewritten reference (null for none), <paramref name="hasObjRef"/>
    /// says whether the modification carries a non-null object reference.
    /// </summary>
    public static bool Apply(JsonObject root, string path, string value, JsonNode? objRef, bool hasObjRef)
    {
        string[] tokens = path.Split('.');
        JsonNode? cur = root;
        JsonObject? parentObj = null;
        string? key = null;
        JsonArray? parentArr = null;
        int idx = -1;
        bool afterArray = false;

        for (int i = 0; i < tokens.Length; i++)
        {
            string t = tokens[i];
            bool last = i == tokens.Length - 1;

            if (t == "Array" && cur is JsonArray)
            {
                afterArray = true;
                continue;
            }

            if (afterArray && t == "size" && cur is JsonArray sizeArr)
            {
                int size = int.TryParse(value, out int sv) ? sv : sizeArr.Count;
                while (sizeArr.Count > size)
                {
                    sizeArr.RemoveAt(sizeArr.Count - 1);
                }

                while (sizeArr.Count < size)
                {
                    sizeArr.Add(sizeArr.Count > 0 ? sizeArr[sizeArr.Count - 1]?.DeepClone() : null);
                }

                return true;
            }

            afterArray = false;
            Match dm = DataRegex.Match(t);
            if (dm.Success && cur is JsonArray arr)
            {
                int n = int.Parse(dm.Groups[1].Value);
                while (arr.Count <= n)
                {
                    arr.Add(arr.Count > 0 ? arr[arr.Count - 1]?.DeepClone() : null);
                }

                parentArr = arr;
                parentObj = null;
                idx = n;
                cur = arr[n];
                if (cur == null && !last)
                {
                    cur = new JsonObject();
                    arr[n] = cur;
                }
            }
            else if (cur is JsonObject o)
            {
                parentObj = o;
                parentArr = null;
                key = t;
                cur = o[t];
                if (cur == null && !last)
                {
                    cur = new JsonObject();
                    o[t] = cur;
                }
            }
            else
            {
                return false;
            }
        }

        JsonNode? newVal = Coerce(cur, value, objRef, hasObjRef);
        if (parentObj != null && key != null)
        {
            parentObj[key] = newVal;
            return true;
        }

        if (parentArr != null)
        {
            parentArr[idx] = newVal;
            return true;
        }

        return false;
    }

    private static JsonNode? Coerce(JsonNode? existing, string value, JsonNode? objRef, bool hasObjRef)
    {
        if (hasObjRef)
        {
            return objRef?.DeepClone();
        }

        if (existing is JsonObject eo && (eo.ContainsKey("guid") || eo.ContainsKey("ref")) && value.Length == 0)
        {
            return null;
        }

        if (existing is JsonValue ev)
        {
            if (ev.TryGetValue(out string? _))
            {
                return JsonValue.Create(value);
            }

            return YamlNode.Scalar(value, false).ToJson();
        }

        if (existing == null && value.Length == 0)
        {
            return null;
        }

        return YamlNode.Scalar(value, false).ToJson();
    }
}
