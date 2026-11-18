using System.Text.Json;
using System.Text.Json.Nodes;

namespace GodotExtract;

public static class Program
{
    static string FindRoot(string? arg)
    {
        if (arg != null) return arg;
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
            {
                var c = Path.Combine(d.FullName, "tps-demo-master");
                if (Directory.Exists(c)) return c;
            }
        throw new DirectoryNotFoundException("tps-demo-master not found; pass --root <dir>");
    }
    static string FindOut(string? arg)
    {
        if (arg != null) return arg;
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "GodotExtract.csproj"))) return Path.Combine(d.FullName, "out");
        return Path.Combine(Directory.GetCurrentDirectory(), "out");
    }
    static string? Opt(string[] a, string name) { int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    static void WriteJson(string dir, string file, JsonNode n)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, file), n.ToJsonString(Extract.Pretty));
        Console.WriteLine($"wrote {Path.Combine(dir, file)}");
    }

    public static int Main(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine("usage: GodotExtract dump <file> | materials | assetmap | bakelevel | selftest  [--root tps-demo-master] [--out dir]"); return 1; }
        try
        {
            var root = FindRoot(Opt(args, "--root")); var out_ = FindOut(Opt(args, "--out")); var p = new Project(root);
            switch (args[0])
            {
                case "dump":
                    {
                        string file = args[1]; if (!File.Exists(file)) file = p.ToDisk(file);
                        Console.WriteLine(GodotFile.ParseFile(file).ToJson().ToJsonString(Extract.Pretty)); return 0;
                    }
                case "materials":
                    {
                        var m = Extract.Materials(p); WriteJson(out_, "Materials.json", m);
                        Console.WriteLine($"materials: {((JsonObject)m["materials"]!).Count} (shader: {((JsonArray)m["shaderMaterials"]!).Count}), glb material maps: {((JsonObject)m["glbMaterialMap"]!).Count}"); return 0;
                    }
                case "assetmap":
                    {
                        var m = Extract.AssetMap(p); WriteJson(out_, "AssetMap.json", m);
                        var a = ((JsonObject)m["assets"]!).Select(x => (JsonObject)x.Value!).ToList();
                        foreach (var g in a.GroupBy(x => x["type"]!.GetValue<string>())) Console.WriteLine($"  {g.Key}: {g.Count()}");
                        Console.WriteLine($"  capped(>{Extract.TextureCap}): {a.Count(x => x["capped"]?.GetValue<bool>() == true)}, name collisions: {((JsonArray)m["nameCollisions"]!).Count}"); return 0;
                    }
                case "bakelevel":
                    {
                        var l = Bake.BakeLevel(p, out var counts); WriteJson(out_, "level.scene.json", l);
                        Console.WriteLine("counts: " + counts.ToJsonString()); return 0;
                    }
                case "selftest": return SelfTest.Run(p);
                default: Console.Error.WriteLine("unknown command " + args[0]); return 1;
            }
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 2; }
    }
}

public static class SelfTest
{
    static int fails, checks;
    static void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  [" + detail + "]" : "")}"); }
    static bool Near(double a, double b, double eps = 1e-5) => Math.Abs(a - b) < eps;
    static int CountLines(string disk, string prefix) => File.ReadLines(disk).Count(l => l.StartsWith(prefix));

    public static int Run(Project p)
    {
        // 1. Transform3D conversion
        var fork = Mat4.FromTransform3D(new double[] { -1, 0, -1.5e-7, 0, 1, 0, 1.5e-7, 0, -1, 1, 2, 3 });
        Check("forklift2 180deg Y turn: M11=-1,M22=1,M33=-1", Near(fork.M[0], -1) && Near(fork.M[5], 1) && Near(fork.M[10], -1));
        var q = fork.TransformPoint(0, 0, 1);
        Check("forklift2 local +Z maps to -Z + origin", Near(q[0], 1) && Near(q[1], 2) && Near(q[2], 2, 1e-4));
        // level.tscn Marker3D (rows: r0=(0.843905,0,-0.536493) r1=(0,1,0) r2=(0.536493,0,0.843905)) basis is Y rotation: Godot B*(0,0,1) = column 2 = (r0z,r1z,r2z) = (-0.536493,0,0.843905)
        var mk = Mat4.FromTransform3D(new double[] { 0.843905, 0, -0.536493, 0, 1, 0, 0.536493, 0, 0.843905, 71.5907, -6.05686, 46.2736 });
        var z = mk.TransformPoint(0, 0, 1);
        Check("spawn marker +Z -> basis column 2 + origin", Near(z[0], 71.5907 - 0.536493) && Near(z[1], -6.05686) && Near(z[2], 46.2736 + 0.843905));
        Check("rows->XNA transpose: M13=r2x, M31=r0z", Near(mk.M[2], 0.536493) && Near(mk.M[8], -0.536493) && Near(mk.M[12], 71.5907) && Near(mk.M[15], 1));
        var gen = Mat4.FromTransform3D(new double[] { 0.088873334, 0.36089236, -0.93208843, 0.4157426, 0.8376457, 0.3639617, 0.90908164, -0.41835597, -0.075325064, -0.39473683, 1.0766551, -0.09200796 });
        var gx = gen.TransformPoint(1, 0, 0);
        Check("general basis: X axis = column 0 (r0x,r1x,r2x)+o", Near(gx[0], 0.088873334 - 0.39473683) && Near(gx[1], 0.4157426 + 1.0766551) && Near(gx[2], 0.90908164 - 0.09200796));
        var comp = Mat4.Mul(Mat4.Translation(1, 0, 0), Mat4.FromTransform3D(new double[] { 0, 0, 1, 0, 1, 0, -1, 0, 0, 10, 0, 0 }));
        // child at +1 x inside parent: parent basis rows (0,0,1),(0,1,0),(-1,0,0) -> B*(1,0,0)=column0=(0,0,-1)
        var cp = comp.TransformPoint(0, 0, 0);
        Check("child*parent composition", Near(cp[0], 10) && Near(cp[2], -1), $"{cp[0]},{cp[2]}");

        // 2. value parser
        var v = ValParser.Parse("{\n\"a\": [1, -2.5e-3, inf],\n\"b\": Vector3(1,2,3), \"c\": \"x\\\"y\", \"d\": ExtResource(\"3\")\n}");
        var vj = v.ToJson()!;
        Check("value parser: nested dict/array/call/escape", vj["a"]![1]!.GetValue<double>() == -2.5e-3 && vj["b"]!["values"]![2]!.GetValue<double>() == 3 && vj["c"]!.GetValue<string>() == "x\"y" && vj["d"]!["ext"]!.GetValue<string>() == "3");

        // 3. round trip
        string small = "[gd_scene load_steps=3 format=3 uid=\"uid://abc\"]\n\n[ext_resource type=\"PackedScene\" uid=\"uid://x\" path=\"res://a/b.tscn\" id=\"1_a\"]\n\n[sub_resource type=\"BoxShape3D\" id=\"1\"]\nsize = Vector3(1, 2, 3)\n\n[node name=\"Root\" type=\"Node3D\" unique_id=5]\ntransform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 4, 5, 6)\ngroups = [\"a\", \"b\"]\nkeys = {\n\"times\": PackedFloat32Array(0, 1),\n\"update\": 0\n}\n\n[node name=\"Child\" parent=\".\" instance=ExtResource(\"1_a\")]\nnode_paths = NodePath(\"../Root\")\n\n[connection signal=\"x\" from=\".\" to=\"Child\" method=\"m\"]\n";
        var a1 = GodotFile.Parse(small); string t1 = a1.ToText(); var a2 = GodotFile.Parse(t1);
        Check("round trip: text stable + json equal", t1 == a2.ToText() && a1.ToJson().ToJsonString() == a2.ToJson().ToJsonString());
        Check("small tscn counts: 2 nodes, 1 ext, 1 sub, 1 connection", a1.Nodes.Count == 2 && a1.Ext.Count == 1 && a1.Sub.Count == 1 && a1.Of("connection").Count() == 1);
        Check("small tscn multi-line dict parsed", a1.Nodes[0].Props["keys"] is VDict);
        string door = p.ToDisk("res://door/door.tscn"); var df = GodotFile.Parse(File.ReadAllText(door));
        Check("round trip door.tscn", df.ToText() == GodotFile.Parse(df.ToText()).ToText() && df.ToJson().ToJsonString() == GodotFile.Parse(df.ToText()).ToJson().ToJsonString());

        // 4. counts vs independent line counts + plan numbers
        foreach (var res in new[] { "res://level/level.tscn", "res://player/player.tscn", "res://level/geometry/scenes/structure.tscn" })
        {
            var f = p.Load(res); string d = p.ToDisk(res);
            Check($"{res}: node/ext/sub counts match line counts", f.Nodes.Count == CountLines(d, "[node ") && f.Ext.Count == CountLines(d, "[ext_resource") && f.Of("sub_resource").Count() == CountLines(d, "[sub_resource"),
                $"nodes={f.Nodes.Count} ext={f.Ext.Count} sub={f.Of("sub_resource").Count()}");
        }
        Check("level.tscn has 68 nodes (plan 01)", p.Load("res://level/level.tscn").Nodes.Count == 68);
        int structShapes = p.Load("res://level/geometry/scenes/structure.tscn").Nodes.Count(n => n.Attr("type") == "CollisionShape3D");
        Console.WriteLine($"info: structure.tscn direct CollisionShape3D = {structShapes}; player.tscn nodes = {p.Load("res://player/player.tscn").Nodes.Count}");
        Check("player.tscn has CapsuleShape3D + AnimationTree", p.Load("res://player/player.tscn").Of("sub_resource").Any(s => s.Attr("type") == "CapsuleShape3D") && p.Load("res://player/player.tscn").Nodes.Any(n => n.Attr("type") == "AnimationTree"));
        // all text files parse
        int ok = 0, bad = 0; var badList = new List<string>();
        foreach (var e in new[] { ".tscn", ".tres" }) foreach (var disk in p.FindFiles(e)) { try { GodotFile.ParseFile(disk); ok++; } catch (Exception ex) { bad++; badList.Add(Path.GetFileName(disk) + ": " + ex.Message); } }
        Check("all .tscn/.tres parse (17 tscn + 73 tres)", bad == 0 && ok == 90, $"ok={ok} bad={bad} {string.Join("; ", badList.Take(3))}");
        var rr = p.Load("res://enemies/red_robot/red_robot.tscn");
        int tracks = rr.Of("sub_resource").Where(s => s.Attr("type") == "Animation").Sum(s => s.Props.Count(k => k.Key.StartsWith("tracks/") && k.Key.EndsWith("/type")));
        Check("red_robot.tscn parses ~10k track props", tracks > 1000, $"tracks={tracks}");

        // 5. flatten + bake counts
        var lvl = Bake.BakeLevel(p, out var c);
        Check("bake: 8 spawns (4+4)", c["playerSpawns"]!.GetValue<int>() == 4 && c["robotSpawns"]!.GetValue<int>() == 4);
        Check("bake: 32 lightmap probes, 3 reflection probes", c["lightProbes"]!.GetValue<int>() == 32 && c["reflectionProbes"]!.GetValue<int>() == 3);
        Check("bake: >=447 colliders", c["colliders"]!.GetValue<int>() >= 447, c.ToJsonString());
        Check("bake: 2 audio volumes, 2 dynamics", c["volumes"]!.GetValue<int>() == 2 && c["dynamics"]!.GetValue<int>() == 2);
        Check("bake: instances reference structure/props/core/lights", new[] { "structure", "props", "core", "lights" }.All(m => ((JsonArray)lvl["instances"]!).Any(i => i!["model"]!.GetValue<string>() == m)));
        var fk = ((JsonArray)lvl["dynamics"]!)[1]!["world"]!.AsArray();
        Console.WriteLine($"{(fails == 0 ? "ALL PASS" : "FAILURES")}: {checks - fails}/{checks} checks passed, {fails} failing");
        return fails == 0 ? 0 : 1;
    }
}
