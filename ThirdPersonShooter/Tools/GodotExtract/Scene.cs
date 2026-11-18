using System.Text.Json.Nodes;

namespace GodotExtract;

/// <summary>A tps-demo-master checkout: res:// path resolution + parse cache.</summary>
public sealed class Project
{
    public string Root;
    readonly Dictionary<string, GodotFile> cache = new();
    public Project(string root) { Root = Path.GetFullPath(root); }
    public string ToDisk(string res) => Path.Combine(Root, res.StartsWith("res://") ? res[6..] : res);
    public GodotFile Load(string res)
    {
        if (!cache.TryGetValue(res, out var f)) cache[res] = f = GodotFile.ParseFile(ToDisk(res));
        return f;
    }
    public static string ToRes(string root, string disk) => "res://" + Path.GetRelativePath(root, disk).Replace('\\', '/');

    /// <summary>Content naming rule: res:// path without extension, '/' (and any non-alnum) -> '_', lowercase.</summary>
    public static string ContentName(string res)
    {
        string p = res.StartsWith("res://") ? res[6..] : res;
        int dot = p.LastIndexOf('.'); if (dot > p.LastIndexOf('/')) p = p[..dot];
        return new string(p.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray());
    }

    public IEnumerable<string> FindFiles(string ext) =>
        Directory.EnumerateFiles(Root, "*" + ext, SearchOption.AllDirectories)
            .Where(f => !f.Contains("/.godot/") && !f.Contains("/.git/")).OrderBy(f => f, StringComparer.Ordinal);
}

/// <summary>Resolves ExtResource/SubResource references of one file into plain JSON.</summary>
public static class Resolve
{
    public static JsonNode? Json(Val v, GodotFile f, int depth = 0)
    {
        switch (v)
        {
            case VCall c when c.Name == "ExtResource":
                {
                    var e = f.Ext.TryGetValue(c.FirstString ?? "", out var s) ? s : null;
                    return new JsonObject { ["path"] = e?.Attr("path"), ["resType"] = e?.Attr("type") };
                }
            case VCall c when c.Name == "SubResource":
                {
                    var s = f.Sub.TryGetValue(c.FirstString ?? "", out var x) ? x : null;
                    var o = new JsonObject { ["subType"] = s?.Attr("type"), ["id"] = c.FirstString };
                    if (s == null || depth > 6 || s.Attr("type") == "Animation") return o;
                    var p = new JsonObject();
                    foreach (var (k, pv) in s.Props) p[k] = Json(pv, f, depth + 1);
                    o["props"] = p; return o;
                }
            case VArr a: { var r = new JsonArray(); foreach (var i in a.Items) r.Add(Json(i, f, depth)); return r; }
            case VDict d:
                {
                    var o = new JsonObject();
                    foreach (var (k, x) in d.Items) o[k is VStr ks ? ks.S : k.ToString()] = Json(x, f, depth);
                    return o;
                }
            default: return v.ToJson();
        }
    }
    public static JsonObject Props(Dictionary<string, Val> props, GodotFile f)
    {
        var o = new JsonObject(); foreach (var (k, v) in props) o[k] = Json(v, f); return o;
    }
}

public sealed class FlatNode
{
    public string Path = ""; public string? ParentPath; public string Name = ""; public string? Type;
    public Mat4 Local = Mat4.Identity();
    public string? Glb;          // res:// path when the node instances a glb
    public string? InstanceOf;   // res:// path of the instanced .tscn / .glb
    public string SourceScene = "";
    public JsonObject Props = new();
    public FlatNode Clone() { var n = (FlatNode)MemberwiseClone(); n.Props = (JsonObject)JsonNode.Parse(Props.ToJsonString())!; n.Local = Local.Clone(); return n; }
}

public sealed class FlatScene
{
    public List<FlatNode> Order = new();
    public Dictionary<string, FlatNode> Map = new();
    readonly Dictionary<string, Mat4> worldCache = new();
    /// <summary>World matrix = child * parent (XNA row-vector order). The scene root's own transform is ignored (identity).</summary>
    public Mat4 World(FlatNode n)
    {
        if (worldCache.TryGetValue(n.Path, out var w)) return w;
        if (n.ParentPath == null) w = Mat4.Identity();
        else if (Map.TryGetValue(n.ParentPath, out var p)) w = Mat4.Mul(n.Local, World(p));
        else w = n.Local.Clone();
        return worldCache[n.Path] = w;
    }
    public FlatNode? Parent(FlatNode n) => n.ParentPath != null && Map.TryGetValue(n.ParentPath, out var p) ? p : null;
    public IEnumerable<FlatNode> ChildrenOf(string path) => Order.Where(n => n.ParentPath == path);
}

public static class Flattener
{
    public static FlatScene Flatten(Project p, string resPath, int depth = 0)
    {
        var f = p.Load(resPath);
        var fs = new FlatScene();
        foreach (var n in f.Nodes)
        {
            string name = n.Attr("name") ?? "";
            string? parent = n.Attr("parent");
            string path = parent == null ? "" : parent == "." ? name : parent + "/" + name;
            string? parentPath = parent == null ? null : parent == "." ? "" : parent;
            string? type = n.Attr("type");
            string? instId = n.RefId("instance");
            string? instPath = instId != null && f.Ext.TryGetValue(instId, out var ie) ? ie.Attr("path") : null;

            if (instPath != null && instPath.EndsWith(".glb"))
            {
                var node = new FlatNode { Path = path, ParentPath = parentPath, Name = name, Type = "glb", Glb = instPath, InstanceOf = instPath, SourceScene = resPath };
                ApplyProps(node, n, f); Add(fs, node);
            }
            else if (instPath != null && instPath.EndsWith(".tscn") && depth < 8)
            {
                var inner = Flatten(p, instPath, depth + 1);
                foreach (var inn in inner.Order)
                {
                    var c = inn.Clone();
                    c.Path = inn.Path == "" ? path : path + "/" + inn.Path;
                    c.ParentPath = inn.ParentPath == null ? parentPath : inn.ParentPath == "" ? path : path + "/" + inn.ParentPath;
                    if (inn.ParentPath == null) { c.Name = name; c.InstanceOf = instPath; }
                    Add(fs, c);
                }
                // outer overrides on the instance root (incl. transform)
                if (fs.Map.TryGetValue(path, out var root)) ApplyProps(root, n, f);
            }
            else if (type == null)
            {
                // override of an inherited node
                if (fs.Map.TryGetValue(path, out var ex)) ApplyProps(ex, n, f);
                else { var node = new FlatNode { Path = path, ParentPath = parentPath, Name = name, SourceScene = resPath }; ApplyProps(node, n, f); Add(fs, node); }
            }
            else
            {
                var node = new FlatNode { Path = path, ParentPath = parentPath, Name = name, Type = type, SourceScene = resPath };
                ApplyProps(node, n, f); Add(fs, node);
            }
        }
        return fs;
    }

    static void Add(FlatScene fs, FlatNode n)
    {
        if (fs.Map.ContainsKey(n.Path)) { fs.Order[fs.Order.FindIndex(x => x.Path == n.Path)] = n; }
        else fs.Order.Add(n);
        fs.Map[n.Path] = n;
    }

    static void ApplyProps(FlatNode node, Section n, GodotFile f)
    {
        foreach (var (k, v) in n.Props)
        {
            node.Props[k] = Resolve.Json(v, f);
            if (k == "transform" && v is VCall c && c.Name == "Transform3D") node.Local = Mat4.FromTransform3D(v.Numbers().ToList());
            else if (k == "position" && v is VCall pc && pc.Name == "Vector3") { var x = v.Numbers().ToList(); node.Local = Mat4.Translation(x[0], x[1], x[2]); }
        }
    }
}
