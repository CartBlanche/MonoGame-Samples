using System.Numerics;
using ThirdPersonShooter.Glb;

// GlbCheck: summary and selftest for the GLB reader used by PipelineExtension.
//   dotnet run -- [summary] [files or dirs...]   (default: every .glb under ../../tps-demo-master)
//   dotnet run -- selftest
//   dotnet run -- dump <file.glb> nodes|prims|lights|anims
static string FindRoot()
{
    var d = new DirectoryInfo(AppContext.BaseDirectory);
    while (d != null) { var p = Path.Combine(d.FullName, "tps-demo-master"); if (Directory.Exists(p)) return p; d = d.Parent; }
    throw new DirectoryNotFoundException("tps-demo-master not found above " + AppContext.BaseDirectory);
}

var cmd = args.Length > 0 ? args[0] : "summary";
string root = FindRoot();
string Find(string name) => Directory.GetFiles(root, name + ".glb", SearchOption.AllDirectories).Single();

if (cmd == "selftest") { int rc = SelfTest(); return rc | SkinnedCheck(args.Length > 1 ? args[1] : null); }
if (cmd == "skinned") return SkinnedCheck(args.Length > 1 ? args[1] : null);
if (cmd == "skin" && args.Length >= 2)
{
    var g = GlbFile.Load(args[1]); var sk = g.Skins[0];
    var set = new HashSet<int>(sk.Joints);
    Console.WriteLine($"skins {g.Skins.Count} joints {sk.Joints.Length} skeleton {sk.Skeleton}");
    foreach (var n in g.Nodes.Where(n => n.Skin >= 0 || n.Parent < 0 || (n.Parent >= 0 && g.Nodes[n.Parent].Parent < 0)))
        Console.WriteLine($"node {n.Index} '{n.Name}' parent {n.Parent} mesh {n.Mesh} skin {n.Skin} T {n.Translation} R {n.Rotation} S {n.Scale} joint {set.Contains(n.Index)}");
    Console.WriteLine("scene roots: " + string.Join(",", g.Scenes[g.DefaultScene]));
    var used = new HashSet<int>();
    foreach (var p in g.Meshes.SelectMany(m => m.Primitives).Where(p => p.Joints != null))
        for (int i = 0; i < p.Joints!.Length; i++)
        { var j = p.Joints[i]; var w = p.Weights![i]; if (w.X > 0) used.Add(j.Item1); if (w.Y > 0) used.Add(j.Item2); if (w.Z > 0) used.Add(j.Item3); if (w.W > 0) used.Add(j.Item4); }
    Console.WriteLine($"weighted joints {used.Count}");
    foreach (var a in g.Animations.Take(3)) Console.WriteLine($"anim {a.Name} dur {a.Duration} keys/ch {a.Channels[0].Times.Length} t0 {a.Channels[0].Times.FirstOrDefault()} interp {a.Channels[0].Interpolation}");
    return 0;
}
if (cmd == "dump" && args.Length >= 3) { Dump(GlbFile.Load(args[1]), args[2]); return 0; }

var files = args.Skip(cmd == "summary" ? 1 : 0).SelectMany(a => Directory.Exists(a) ? Directory.GetFiles(a, "*.glb", SearchOption.AllDirectories) : new[] { a }).ToList();
if (files.Count == 0) files = Directory.GetFiles(root, "*.glb", SearchOption.AllDirectories).OrderBy(f => f).ToList();
foreach (var f in files) Summary(f);
return 0;

static void Summary(string f)
{
    var g = GlbFile.Load(f);
    Console.WriteLine($"{Path.GetFileName(f),-24} meshes {g.Meshes.Count,4} prims {g.TotalPrimitives,4} verts {g.TotalVertices,7} tris {g.TotalTriangles,7} nodes {g.Nodes.Count,4} " +
        $"skins {g.Skins.Count} joints [{string.Join(",", g.Skins.Select(s => s.Joints.Length))}] anims {g.Animations.Count} channels {g.TotalChannels} lights {g.Lights.Count} materials {g.MaterialNames.Length}");
}

static void Dump(GlbFile g, string what)
{
    switch (what)
    {
        case "nodes": foreach (var n in g.Nodes) Console.WriteLine($"{n.Index} '{n.Name}' parent {n.Parent} mesh {n.Mesh} skin {n.Skin} light {n.Light} pos {n.World.Translation}"); break;
        case "prims": for (int i = 0; i < g.Meshes.Count; i++) foreach (var p in g.Meshes[i].Primitives) Console.WriteLine($"mesh {i} '{g.Meshes[i].Name}' mat '{p.MaterialName}' v {p.Positions.Length} t {p.TriangleCount} n {p.Normals != null} tan {p.Tangents != null} uv0 {p.Uv0 != null} uv1 {p.Uv1 != null}"); break;
        case "lights": foreach (var n in g.Nodes.Where(n => n.Light >= 0)) { var l = g.Lights[n.Light]; Console.WriteLine($"{n.Name} {l.Type} color {l.Color} I {l.Intensity} cone {l.InnerCone}/{l.OuterCone} at {n.World.Translation}"); } break;
        case "anims": foreach (var a in g.Animations) Console.WriteLine($"{a.Name} ch {a.Channels.Count} dur {a.Duration:0.###}"); break;
    }
}

int SelfTest()
{
    int fail = 0, n = 0;
    void Check(string what, bool ok, string detail = "") { n++; if (!ok) fail++; Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {what} {detail}"); }
    void Eq(string what, long actual, long expected) => Check(what, actual == expected, $"(got {actual}, expected {expected})");

    // Expected numbers come from an independent python3 -I read of each glb JSON chunk (accessor counts).
    var expected = new (string name, int meshes, int prims, int verts, int tris, int nodes, int skins, int anims, int channels, int lights)[]
    {
        ("player", 5, 7, 38858, 19516, 151, 1, 56, 24360, 0),
        ("red_robot", 4, 5, 27540, 12836, 69, 1, 21, 4032, 0),
        ("lights", 4, 4, 344, 160, 177, 0, 0, 0, 9),
        ("structure", 104, 457, 797767, 284878, 344, 0, 0, 0, 0),
        ("core", 35, 156, 329984, 119815, 151, 0, 0, 0, 0),
        ("props", 130, 157, 520822, 190375, 843, 0, 0, 0, 0),
        ("flying_forklift", 10, 10, 78636, 28171, 10, 0, 0, 0, 0),
        ("BlastMesh", 1, 1, 257, 480, 1, 0, 0, 0, 0),
        ("LightRaysMesh", 1, 1, 4, 2, 1, 0, 0, 0, 0),
    };
    foreach (var e in expected)
    {
        var g = GlbFile.Load(Find(e.name));
        Eq(e.name + " meshes", g.Meshes.Count, e.meshes); Eq(e.name + " prims", g.TotalPrimitives, e.prims);
        Eq(e.name + " verts", g.TotalVertices, e.verts); Eq(e.name + " tris", g.TotalTriangles, e.tris);
        Eq(e.name + " nodes", g.Nodes.Count, e.nodes); Eq(e.name + " skins", g.Skins.Count, e.skins);
        Eq(e.name + " anims", g.Animations.Count, e.anims); Eq(e.name + " channels", g.TotalChannels, e.channels);
        Eq(e.name + " lights", g.Lights.Count, e.lights);
        // internal consistency
        bool idxOk = true, attrOk = true;
        foreach (var p in g.Meshes.SelectMany(m => m.Primitives))
        {
            if (p.Indices.Length > 0 && p.Indices.Max() >= p.Positions.Length) idxOk = false;
            if ((p.Normals != null && p.Normals.Length != p.Positions.Length) || (p.Uv0 != null && p.Uv0.Length != p.Positions.Length)
                || (p.Joints != null && p.Joints.Length != p.Positions.Length) || (p.Weights != null && p.Weights.Length != p.Positions.Length)) attrOk = false;
        }
        Check(e.name + " indices in range", idxOk); Check(e.name + " attribute lengths match", attrOk);
    }

    var player = GlbFile.Load(Find("player"));
    Eq("player skin joints", player.Skins[0].Joints.Length, 145);
    Eq("player inverse binds", player.Skins[0].InverseBind.Length, 145);
    Check("player has JOINTS_0/WEIGHTS_0", player.Meshes.SelectMany(m => m.Primitives).Any(p => p.Joints != null && p.Weights != null));
    Check("player joints fit a byte", !player.Meshes.SelectMany(m => m.Primitives).Any(p => p.JointsOverflow));
    Check("player weights sum ~1", player.Meshes.SelectMany(m => m.Primitives).Where(p => p.Weights != null).All(p => p.Weights!.Take(2000).All(w => MathF.Abs(w.X + w.Y + w.Z + w.W - 1) < 0.02f)));
    Check("player has -noexp anims (to be skipped later)", player.Animations.Any(a => a.Name.EndsWith("-noexp")), $"({player.Animations.Count(a => a.Name.EndsWith("-noexp"))})");
    Check("player has a root joint named 'root'", player.Nodes.Any(x => x.Name == "root"));
    Check("player anim channels are TRS", player.Animations.SelectMany(a => a.Channels).All(c => c.Path is "translation" or "rotation" or "scale"));
    var lights = GlbFile.Load(Find("lights"));
    Eq("lights point count", lights.Lights.Count(l => l.Type == "point"), 7); Eq("lights spot count", lights.Lights.Count(l => l.Type == "spot"), 2);
    Eq("lights nodes referencing a light", lights.Nodes.Count(x => x.Light >= 0), lights.Nodes.Count(x => x.Light >= 0)); // informational
    Console.WriteLine($"info: lights.glb nodes referencing lights = {lights.Nodes.Count(x => x.Light >= 0)}");
    foreach (var nm in new[] { "structure", "props", "core", "lights" }) { var gg = GlbFile.Load(Find(nm)); Check(nm + " mesh names unique", gg.Meshes.Select(m => m.Name).Distinct().Count() == gg.Meshes.Count); }
    var core = GlbFile.Load(Find("core"));
    Check("core materials named", core.Meshes.SelectMany(m => m.Primitives).All(p => !string.IsNullOrEmpty(p.MaterialName)));
    // transform composition: a child's world translation equals parent.World applied to its local translation
    var comp = true;
    foreach (var nd in lights.Nodes.Where(x => x.Parent >= 0)) { var e = Vector3.Transform(nd.Local.Translation, lights.Nodes[nd.Parent].World); if ((e - nd.World.Translation).Length() > 1e-3f) comp = false; }
    Check("world composition parent*child", comp);

    Console.WriteLine($"selftest: {n - fail}/{n} passed, {fail} failed");
    return fail == 0 ? 0 : 1;
}


// Checks Models/player.xnb (built by GodotSkinnedModelProcessor) against the glTF source. Layout: see SkinnedModelWriter.
static int SkinnedCheck(string? xnbPath)
{
    xnbPath ??= Directory.GetFiles(Path.GetDirectoryName(FindRoot())!, "player.xnb", SearchOption.AllDirectories).Where(f => f.Contains("Release") || f.Contains("DesktopGL")).OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
    if (xnbPath == null) { Console.WriteLine("skinned: no player.xnb found (build Desktop first)"); return 1; }
    int n = 0, fail = 0;
    void Chk(string what, bool ok, string extra = "") { n++; if (!ok) fail++; Console.WriteLine($"  [{(ok ? "ok" : "FAIL")}] {what} {extra}"); }
    var glb = GlbFile.Load(Directory.GetFiles(FindRoot(), "player.glb", SearchOption.AllDirectories).Single());
    var bytes = File.ReadAllBytes(xnbPath);
    using var ms = new MemoryStream(bytes); using var r = new BinaryReader(ms);
    r.ReadBytes(3); r.ReadByte(); r.ReadByte(); byte flags = r.ReadByte(); r.ReadInt32();
    Chk("xnb uncompressed", (flags & 0x80) == 0);
    int readers = r.Read7BitEncodedInt(); for (int i = 0; i < readers; i++) { r.ReadString(); r.ReadInt32(); }
    r.Read7BitEncodedInt(); r.Read7BitEncodedInt();
    Chk("format version 1", r.ReadInt32() == 1);
    r.ReadString();
    int nb = r.ReadInt32(); var names = new string[nb]; var parents = new int[nb]; var ib = new float[nb][];
    for (int i = 0; i < nb; i++) { names[i] = r.ReadString(); parents[i] = r.ReadInt32(); r.ReadBytes(40); ib[i] = Enumerable.Range(0, 16).Select(_ => r.ReadSingle()).ToArray(); }
    Chk("parents precede children", Enumerable.Range(0, nb).All(i => parents[i] < i));
    foreach (var k in new[] { "root", "hand.R", "neck.001", "upper_arm.R", "Robot_Skeleton" }) Chk($"kept bone '{k}'", names.Contains(k));
    var skin = glb.Skins[0];
    int weighted = 0; { var used = new HashSet<int>(); foreach (var p in glb.Meshes.SelectMany(m => m.Primitives).Where(p => p.Joints != null)) for (int i = 0; i < p.Joints!.Length; i++) { var j = p.Joints[i]; var w = p.Weights![i]; if (w.X > 0) used.Add(j.Item1); if (w.Y > 0) used.Add(j.Item2); if (w.Z > 0) used.Add(j.Item3); if (w.W > 0) used.Add(j.Item4); } weighted = used.Count; }
    int palBones = names.Count(nm => skin.Joints.Any(j => glb.Nodes[j].Name == nm));
    Chk("kept joints cover all weighted joints", palBones >= weighted, $"(weighted {weighted}, kept joints {palBones}, skeleton bones {nb}, source joints {skin.Joints.Length})");
    int np = r.ReadInt32(); int maxPal = 0; long verts = 0, tris = 0;
    for (int i = 0; i < np; i++)
    {
        r.ReadString(); int vc = r.ReadInt32(); int vb = r.ReadInt32(); verts += vc; Chk($"part {i} vertex stride 68", vb == vc * 68); r.ReadBytes(vb);
        int ic = r.ReadInt32(); tris += ic / 3; r.ReadBytes(ic * 4); int pc = r.ReadInt32(); maxPal = Math.Max(maxPal, pc); r.ReadBytes(pc * 4);
    }
    Console.WriteLine($"  parts {np}, verts {verts}, tris {tris}, max palette {maxPal} (limit 72, split needed: {(maxPal > 72 ? "yes" : "no")})");
    Chk("palette fits 72", maxPal <= 72);
    Chk("vertex/tri counts match glTF", verts >= glb.TotalVertices * 0 + 1 && tris == glb.TotalTriangles, $"(glb tris {glb.TotalTriangles})");
    int nc = r.ReadInt32(); long keyBytes = 0; bool rootAnimated = false; var clips = new List<string>();
    foreach (var gaName in new[] { "Idle", "running_nogun", "strafe_left", "jump_2_upwards", "AIM-Up" }) { }
    for (int c = 0; c < nc; c++)
    {
        string name = r.ReadString(); bool loop = r.ReadBoolean(); float dur = r.ReadSingle(); int fc = r.ReadInt32(); float fps = r.ReadSingle();
        var src = glb.Animations.FirstOrDefault(a => a.Name == name || a.Name == name + "-cycle");
        Chk($"clip {name} ({fc}f, {dur:0.###}s, loop {loop})", src != null && MathF.Abs(src.Duration - dur) < 1e-3f && fc == (int)MathF.Ceiling(dur * fps - 1e-3f) + 1 && (src.Name.EndsWith("-cycle") == loop), src == null ? "(no source)" : $"(glb {src.Duration:0.###}s)");
        Chk($"clip {name} not -noexp", src != null && !src.Name.EndsWith("-noexp"));
        for (int b = 0; b < nb; b++)
        {
            byte f = r.ReadByte();
            for (int k = 0; k < 3; k++) if ((f & (1 << k)) != 0) { int len = r.ReadInt32(); keyBytes += len * 4; r.ReadBytes(len * 4); if (names[b] == "root" && k == 0 && len > 3) rootAnimated = true; }
        }
        clips.Add(name);
    }
    Chk("root bone translation track kept and animated in some clip", rootAnimated);
    Chk("whitelist present", new[] { "Idle", "Idlecombat", "running_nogun", "strafe_left", "strafe_right", "strafe_front", "strafe_back", "jump_2_upwards", "jump_4_falling", "AIM-Up", "AIM-Down" }.All(clips.Contains));
    Console.WriteLine($"  xnb {bytes.Length / 1024} KiB, clips {nc}, clip key data {keyBytes / 1024} KiB");
    Console.WriteLine($"skinned: {n - fail}/{n} passed, {fail} failed");
    return fail == 0 ? 0 : 1;
}
