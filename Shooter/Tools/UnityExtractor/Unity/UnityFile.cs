using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UnityExtractor.Yaml;

namespace UnityExtractor.Unity;

public sealed class UnityDoc
{
    public int ClassId;
    public long FileId;
    public bool Stripped;
    public string TypeName = "";
    public JsonObject Props = new JsonObject();

    public string Type
    {
        get { return UnityClassIds.NameOf(ClassId, TypeName); }
    }
}

/// <summary>A Unity YAML asset split into its `--- !u!id &amp;fileID` documents.</summary>
public sealed class UnityFile
{
    private static readonly Regex HeaderRegex =
        new Regex(@"^--- !u!(\d+) &(-?\d+)(\s+stripped)?\s*$", RegexOptions.Compiled);

    public string Path = "";
    public List<UnityDoc> Docs = new List<UnityDoc>();
    public Dictionary<long, UnityDoc> ById = new Dictionary<long, UnityDoc>();

    public static UnityFile Load(string relPath, string absPath)
    {
        UnityFile file = new UnityFile { Path = relPath };
        string? header = null;
        List<string> body = new List<string>();

        void Flush()
        {
            if (header == null)
            {
                return;
            }

            Match m = HeaderRegex.Match(header);
            if (!m.Success)
            {
                return;
            }

            YamlNode root = UnityYamlParser.Parse(body);
            UnityDoc doc = new UnityDoc
            {
                ClassId = int.Parse(m.Groups[1].Value),
                FileId = long.Parse(m.Groups[2].Value),
                Stripped = m.Groups[3].Success
            };

            if (root.Kind == YamlKind.Map && root.Map.Count > 0)
            {
                doc.TypeName = root.Map[0].Key;
                JsonNode? json = root.Map[0].Value.ToJson();
                if (json is JsonObject jo)
                {
                    doc.Props = jo;
                }
            }

            file.Docs.Add(doc);
            file.ById[doc.FileId] = doc;
        }

        foreach (string line in File.ReadLines(absPath))
        {
            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                Flush();
                header = line;
                body = new List<string>();
            }
            else if (header != null)
            {
                body.Add(line);
            }
        }

        Flush();
        return file;
    }
}
