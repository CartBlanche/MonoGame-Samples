using System.Text;
using System.Text.Json;

// ContentGen: reads Tools/GodotExtract/out/{AssetMap,Materials}.json and rewrites the generated texture section of
// Core/Content/ThirdPersonShooter.mgcb (between the BEGIN/END GENERATED TEXTURES markers). Deterministic and re-runnable.
// usage: dotnet run --project Tools/ContentGen -- [--all] [--format Color|DxtCompressed] [--only name1,name2]
const string Begin = "#--- BEGIN GENERATED TEXTURES (Tools/ContentGen) ---";
const string End = "#--- END GENERATED TEXTURES ---";
string root = AppContext.BaseDirectory;
while (root != null && !File.Exists(Path.Combine(root, "ThirdPersonShooter.sln"))) root = Path.GetDirectoryName(root)!;
if (root == null) { Console.Error.WriteLine("cannot find repo root"); return 1; }
bool all = args.Contains("--all");
string format = "Color";
HashSet<string>? only = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--format") format = args[i + 1];
    if (args[i] == "--only") only = args[i + 1].Split(',').ToHashSet();
}
var amap = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Tools/GodotExtract/out/AssetMap.json"))).RootElement;
var mats = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Tools/GodotExtract/out/Materials.json"))).RootElement;
int cap = amap.GetProperty("textureCap").GetInt32();

// name -> (path, srgb, normal)
var textures = new SortedDictionary<string, (string path, bool srgb, bool normal)>(StringComparer.Ordinal);
foreach (var a in amap.GetProperty("assets").EnumerateObject())
{
    if (a.Value.GetProperty("type").GetString() != "texture") continue;
    string p = a.Value.GetProperty("path").GetString()!;
    if (!p.EndsWith(".png")) continue; // .hdr/.exr unsupported here
    textures[a.Value.GetProperty("name").GetString()!] = (p, a.Value.GetProperty("srgb").GetBoolean(), a.Value.GetProperty("normalMap").GetBoolean());
}

// closure: textures of materials used by the level models
var used = new SortedSet<string>(StringComparer.Ordinal);
if (all) foreach (var k in textures.Keys) used.Add(k);
else
{
    string[] levelModels = { "structure", "props", "core", "lights", "flying_forklift" };
    string[] slots = { "albedo", "normal", "orm", "roughnessTexture", "metallicTexture", "aoTexture", "emission" };
    var map = mats.GetProperty("glbMaterialMap");
    foreach (var m in levelModels)
    {
        if (!map.TryGetProperty(m, out var mm)) continue;
        foreach (var e in mm.EnumerateObject())
        {
            if (!mats.GetProperty("materials").TryGetProperty(e.Value.GetString()!, out var mat)) continue;
            foreach (var s in slots)
                if (mat.TryGetProperty(s, out var v) && v.ValueKind == JsonValueKind.String && textures.ContainsKey(v.GetString()!)) used.Add(v.GetString()!);
        }
    }
}
if (only != null) { used.IntersectWith(only); }

var sb = new StringBuilder();
sb.Append(Begin).Append('\n');
foreach (var n in used)
{
    var t = textures[n];
    string rel = "../../tps-demo-master/" + t.path.Substring("res://".Length);
    sb.Append($"#begin Textures/{n}.xnb\n/importer:TextureImporter\n/processor:GodotTextureProcessor\n");
    sb.Append($"/processorParam:MaxSize={cap}\n/processorParam:GenerateMipmaps=True\n/processorParam:TextureFormat={format}\n");
    sb.Append($"/build:{rel};Textures/{n}\n\n");
}
sb.Append(End).Append('\n');

string mgcb = Path.Combine(root, "Core/Content/ThirdPersonShooter.mgcb");
string text = File.ReadAllText(mgcb);
int b = text.IndexOf(Begin), e2 = text.IndexOf(End);
if (b >= 0 && e2 > b) text = text.Substring(0, b) + sb.ToString() + text.Substring(e2 + End.Length + 1);
else text = text.TrimEnd() + "\n\n" + sb;
File.WriteAllText(mgcb, text);
Console.WriteLine($"wrote {used.Count} texture entries (format {format}, cap {cap}) of {textures.Count} png textures");
// Manifest for the runtime: which content textures exist
File.WriteAllLines(Path.Combine(root, "Tools/ContentGen/last-textures.txt"), used);
return 0;
