using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace UnityExtractor.Yaml;

public enum YamlKind
{
    Scalar,
    Map,
    Seq
}

/// <summary>
/// Minimal YAML node tree. Only the subset of YAML that Unity serializes is supported.
/// </summary>
public sealed class YamlNode
{
    private static readonly Regex IntRegex = new Regex(@"^-?(0|[1-9][0-9]*)$", RegexOptions.Compiled);
    private static readonly Regex FloatRegex = new Regex(@"^-?([0-9]+\.?[0-9]*|\.[0-9]+)([eE][-+]?[0-9]+)?$", RegexOptions.Compiled);

    // Keys whose scalar value must always stay a string, even if it looks numeric.
    private static readonly HashSet<string> StringKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "guid", "m_Name", "m_TagString", "m_EditorClassIdentifier", "propertyPath", "value",
        "m_Text", "m_text", "text", "name", "second", "m_LocalIdentfierInFile", "m_Path", "path"
    };

    public YamlKind Kind;
    public string Text = "";
    public bool Quoted;
    public List<KeyValuePair<string, YamlNode>> Map = new List<KeyValuePair<string, YamlNode>>();
    public List<YamlNode> Seq = new List<YamlNode>();

    public static YamlNode Scalar(string text, bool quoted)
    {
        return new YamlNode { Kind = YamlKind.Scalar, Text = text, Quoted = quoted };
    }

    public YamlNode? Get(string key)
    {
        foreach (KeyValuePair<string, YamlNode> kv in Map)
        {
            if (kv.Key == key)
            {
                return kv.Value;
            }
        }

        return null;
    }

    public JsonNode? ToJson(string? keyHint = null)
    {
        switch (Kind)
        {
            case YamlKind.Map:
            {
                JsonObject obj = new JsonObject();
                foreach (KeyValuePair<string, YamlNode> kv in Map)
                {
                    obj[kv.Key] = kv.Value.ToJson(kv.Key);
                }

                return obj;
            }
            case YamlKind.Seq:
            {
                JsonArray arr = new JsonArray();
                foreach (YamlNode item in Seq)
                {
                    arr.Add(item.ToJson(null));
                }

                return arr;
            }
            default:
                return ScalarToJson(keyHint);
        }
    }

    private JsonNode? ScalarToJson(string? keyHint)
    {
        if (Quoted)
        {
            return JsonValue.Create(Text);
        }

        if (keyHint != null && StringKeys.Contains(keyHint))
        {
            return JsonValue.Create(Text);
        }

        if (Text.Length == 0)
        {
            return JsonValue.Create("");
        }

        if (IntRegex.IsMatch(Text))
        {
            if (long.TryParse(Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long l))
            {
                return JsonValue.Create(l);
            }

            // Too large for a signed 64-bit integer: keep exact text rather than losing precision as a double.
            return JsonValue.Create(Text);
        }

        if (FloatRegex.IsMatch(Text)
            && double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
            && !double.IsInfinity(d))
        {
            return JsonValue.Create(d);
        }

        return JsonValue.Create(Text);
    }
}
