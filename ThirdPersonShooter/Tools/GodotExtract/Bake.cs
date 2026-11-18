using System.Text.Json.Nodes;

namespace GodotExtract;

public static class Bake
{
    static JsonArray Mat(FlatScene fs, FlatNode n) => new JsonArray(fs.World(n).ToFloats().Select(f => (JsonNode)JsonValue.Create(f)!).ToArray());
    static double D(JsonNode? n, double def) => n is JsonValue v && v.TryGetValue<double>(out var d) ? d : def;
    static JsonNode? Vals(JsonNode? n) => n is JsonObject o ? o["values"]?.DeepClone() : n?.DeepClone();
    static double[] Vec(JsonNode? n, params double[] def) => n is JsonObject o && o["values"] is JsonArray a ? a.Select(x => D(x, 0)).ToArray() : def;

    /// <summary>Shape descriptor from a CollisionShape3D's resolved "shape" sub-resource, or null if unsupported.</summary>
    static JsonObject? Shape(FlatNode n)
    {
        if (n.Props["shape"] is not JsonObject sh) return null;
        string st = sh["subType"]?.GetValue<string>() ?? "";
        var pr = sh["props"] as JsonObject ?? new JsonObject();
        var o = new JsonObject();
        switch (st)
        {
            case "BoxShape3D": o["type"] = "box"; o["size"] = new JsonArray(Vec(pr["size"], 1, 1, 1).Select(x => (JsonNode)JsonValue.Create(x)!).ToArray()); break;
            case "SphereShape3D": o["type"] = "sphere"; o["radius"] = D(pr["radius"], 0.5); break;
            case "CapsuleShape3D": o["type"] = "capsule"; o["radius"] = D(pr["radius"], 0.5); o["height"] = D(pr["height"], 2); break;
            case "CylinderShape3D": o["type"] = "cylinder"; o["radius"] = D(pr["radius"], 0.5); o["height"] = D(pr["height"], 2); break;
            case "ConvexPolygonShape3D": o["type"] = "convexHull"; o["points"] = Vals(pr["points"]); break;
            case "ConcavePolygonShape3D": o["type"] = "trimesh"; o["faces"] = Vals(pr["data"]); break;
            case "SeparationRayShape3D": o["type"] = "separationRay"; o["length"] = D(pr["length"], 1); break;
            default: o["type"] = st; o["unsupported"] = true; break;
        }
        return o;
    }

    static bool IsBody(string? t) => t is "StaticBody3D" or "RigidBody3D" or "CharacterBody3D" or "AnimatableBody3D";

    public static (JsonArray colliders, JsonArray triggers) Colliders(FlatScene fs)
    {
        var colliders = new JsonArray(); var triggers = new JsonArray();
        foreach (var n in fs.Order)
        {
            if (n.Type != "CollisionShape3D" && n.Type != "CollisionPolygon3D") continue;
            if (n.Props["disabled"] is JsonValue dv && dv.TryGetValue<bool>(out var dis) && dis) continue;
            var par = fs.Parent(n); if (par == null) continue;
            JsonObject? shape;
            if (n.Type == "CollisionPolygon3D")
                shape = new JsonObject { ["type"] = "polygonPrism", ["polygon"] = Vals(n.Props["polygon"]), ["depth"] = D(n.Props["depth"], 1) };
            else shape = Shape(n);
            if (shape == null) continue;
            shape["node"] = n.Path; shape["world"] = Mat(fs, n);
            shape["layer"] = (int)D(par.Props["collision_layer"], 1); shape["mask"] = (int)D(par.Props["collision_mask"], 1);
            shape["body"] = par.Path; shape["bodyType"] = par.Type;
            if (IsBody(par.Type)) colliders.Add(shape);
            else if (par.Type == "Area3D") triggers.Add(shape);
        }
        return (colliders, triggers);
    }

    static JsonArray Animations(GodotFile f)
    {
        var arr = new JsonArray();
        foreach (var s in f.Of("sub_resource").Where(s => s.Attr("type") == "Animation"))
        {
            var o = new JsonObject
            {
                ["id"] = s.Attr("id"), ["name"] = s.Prop("resource_name") is VStr n ? n.S : null,
                ["length"] = s.Prop("length") is VNum l ? l.D : 1.0, ["loopMode"] = s.Prop("loop_mode") is VNum lm ? (int)lm.D : 0,
            };
            int tracks = 0; var hooks = new JsonArray();
            foreach (var (k, v) in s.Props)
            {
                if (!k.StartsWith("tracks/") || !k.EndsWith("/type")) continue;
                tracks++;
                string idx = k.Split('/')[1], type = Section.Str(v) ?? "";
                if (type is "audio" or "method")
                    hooks.Add(new JsonObject { ["track"] = int.Parse(idx), ["type"] = type, ["path"] = s.Prop($"tracks/{idx}/path")?.ToString(), ["keys"] = Resolve.Json(s.Prop($"tracks/{idx}/keys")!, f) });
            }
            o["trackCount"] = tracks; o["audioMethodTracks"] = hooks;
            var tr = new JsonArray();   // value tracks in full (path, interp, loop_wrap, keys{times,transitions,update,values}) so the runtime can evaluate e.g. mawaru
            foreach (var (k, v) in s.Props)
            {
                if (!k.StartsWith("tracks/") || !k.EndsWith("/type") || Section.Str(v) != "value") continue;
                string idx = k.Split('/')[1];
                tr.Add(new JsonObject { ["track"] = int.Parse(idx), ["path"] = s.Prop($"tracks/{idx}/path")?.ToString(), ["interp"] = s.Prop($"tracks/{idx}/interp") is VNum ip ? (int)ip.D : 1, ["loopWrap"] = s.Prop($"tracks/{idx}/loop_wrap") is VBool lw ? lw.B : true, ["keys"] = Resolve.Json(s.Prop($"tracks/{idx}/keys")!, f) });
            }
            o["valueTracks"] = tr; arr.Add(o);
        }
        return arr;
    }

    static JsonObject Prefab(Project p, string res)
    {
        var f = p.Load(res); var fs = Flattener.Flatten(p, res);
        var nodes = new JsonArray(); var audio = new JsonArray(); var particles = new JsonArray(); var lights = new JsonArray();
        foreach (var n in fs.Order)
        {
            if (n.Type == null) continue;
            var o = new JsonObject { ["path"] = n.Path, ["type"] = n.Type, ["world"] = Mat(fs, n), ["glb"] = n.Glb, ["props"] = n.Props.DeepClone() };
            nodes.Add(o);
            if (n.Type is "AudioStreamPlayer" or "AudioStreamPlayer2D" or "AudioStreamPlayer3D")
                audio.Add(new JsonObject { ["node"] = n.Path, ["type"] = n.Type, ["props"] = n.Props.DeepClone() });
            else if (n.Type is "CPUParticles3D" or "GPUParticles3D") particles.Add(new JsonObject { ["node"] = n.Path, ["type"] = n.Type, ["world"] = Mat(fs, n), ["props"] = n.Props.DeepClone() });
            else if (n.Type is "OmniLight3D" or "SpotLight3D" or "DirectionalLight3D") lights.Add(new JsonObject { ["node"] = n.Path, ["type"] = n.Type, ["world"] = Mat(fs, n), ["props"] = n.Props.DeepClone() });
        }
        var (colliders, triggers) = Colliders(fs);
        var conns = new JsonArray();
        foreach (var c in f.Of("connection")) conns.Add($"{c.Attr("from")}.{c.Attr("signal")} -> {c.Attr("to")}.{c.Attr("method")}");
        return new JsonObject { ["scene"] = res, ["nodes"] = nodes, ["colliders"] = colliders, ["triggers"] = triggers, ["audio"] = audio, ["particles"] = particles, ["lights"] = lights, ["animations"] = Animations(f), ["connections"] = conns };
    }

    /// <summary>Each dynamics entry (a FlyingForklifts/* scene instance) gets the glb + model name of its nested glb instance (backwards compatible: previously null).</summary>
    static JsonArray FillDynamicModels(JsonArray dynamics, JsonArray instances)
    {
        foreach (var d in dynamics)
            foreach (var i in instances)
                if (i!["node"]!.GetValue<string>().StartsWith(d!["node"]!.GetValue<string>() + "/") && i["glb"] != null) { d["glb"] = i["glb"]!.DeepClone(); d["model"] = i["model"]!.DeepClone(); break; }
        return dynamics;
    }

    public static JsonObject BakeLevel(Project p, out JsonObject counts)
    {
        const string lvl = "res://level/level.tscn";
        var fs = Flattener.Flatten(p, lvl); var f = p.Load(lvl);
        var instances = new JsonArray(); var dynamics = new JsonArray(); var probes = new JsonArray(); var lightProbes = new JsonArray();
        var spawns = new JsonObject { ["playerSpawns"] = new JsonArray(), ["robotSpawns"] = new JsonArray() };
        var volumes = new JsonArray(); JsonObject? env = null; JsonObject music = new();

        foreach (var n in fs.Order)
        {
            bool dyn = n.Path.StartsWith("FlyingForklifts/");
            if (n.Glb != null || (dyn && n.InstanceOf != null && n.ParentPath == "FlyingForklifts"))
            {
                var inst = new JsonObject { ["node"] = n.Path, ["glb"] = n.Glb, ["model"] = n.Glb != null ? Path.GetFileNameWithoutExtension(n.Glb).ToLowerInvariant() : null, ["scene"] = n.SourceScene, ["world"] = Mat(fs, n) };
                if (n.Glb != null) { if (dyn) inst["dynamic"] = true; instances.Add(inst); }   // "dynamic": the glb root of a FlyingForklifts/* instance (animated by dynamicAnimations; the runtime draws it itself)
                if (dyn && n.ParentPath == "FlyingForklifts") { inst["scene"] = n.InstanceOf; dynamics.Add(inst); }
            }
            switch (n.Type)
            {
                case "Marker3D" when n.ParentPath is "PlayerSpawnpoints" or "RobotSpawnpoints":
                    ((JsonArray)spawns[n.ParentPath == "PlayerSpawnpoints" ? "playerSpawns" : "robotSpawns"]!).Add(new JsonObject { ["node"] = n.Path, ["world"] = Mat(fs, n) });
                    break;
                case "ReflectionProbe":
                    probes.Add(new JsonObject { ["node"] = n.Path, ["world"] = Mat(fs, n), ["props"] = n.Props.DeepClone() }); break;
                case "LightmapProbe":
                    lightProbes.Add(new JsonObject { ["node"] = n.Path, ["position"] = new JsonArray(fs.World(n).M[12], fs.World(n).M[13], fs.World(n).M[14]) }); break;
                case "WorldEnvironment":
                    if (n.Props["environment"] is JsonObject eo && eo["path"] is JsonNode ep)
                    {
                        var r = p.Load(ep.GetValue<string>()).Resource!;
                        env = new JsonObject { ["path"] = ep.GetValue<string>(), ["props"] = Resolve.Props(r.Props, p.Load(ep.GetValue<string>())) };
                    }
                    break;
                case "AudioStreamPlayer":
                    music = new JsonObject { ["node"] = n.Path, ["props"] = n.Props.DeepClone() }; break;
            }
        }
        var (colliders, triggers) = Colliders(fs);
        // audio zones: Area3D nodes with an audio bus override; their shapes are the triggers whose body is that area
        foreach (var n in fs.Order.Where(x => x.Type == "Area3D" && x.Props["audio_bus_override"] != null))
        {
            var shapes = new JsonArray();
            foreach (var t in triggers.Cast<JsonObject>().Where(t => t["body"]!.GetValue<string>() == n.Path)) shapes.Add(t.DeepClone());
            volumes.Add(new JsonObject { ["node"] = n.Path, ["bus"] = n.Props["audio_bus_name"]?.DeepClone(), ["shapes"] = shapes });
        }
        var areaPaths = volumes.Cast<JsonObject>().Select(v => v["node"]!.GetValue<string>()).ToHashSet();
        var otherTriggers = new JsonArray(); foreach (var t in triggers.Cast<JsonObject>().Where(t => !areaPaths.Contains(t["body"]!.GetValue<string>()))) otherTriggers.Add(t.DeepClone());

        // bus layout + physics layer names
        JsonObject buses = new();
        var busFile = p.Load("res://default_bus_layout.tres");
        if (busFile.Resource != null)
        {
            buses["resource"] = Resolve.Props(busFile.Resource.Props, busFile);
            var effects = new JsonObject(); foreach (var s in busFile.Of("sub_resource")) effects[s.Attr("id")!] = new JsonObject { ["type"] = s.Attr("type"), ["props"] = Resolve.Props(s.Props, busFile) };
            buses["effects"] = effects;
        }
        var layers = new JsonObject();
        var pg = p.Load("res://project.godot").Of("layer_names").FirstOrDefault();
        if (pg != null) foreach (var (k, v) in pg.Props) layers[k] = v.ToJson();

        var prefabs = new JsonObject
        {
            ["player"] = Prefab(p, "res://player/player.tscn"),
            ["red_robot"] = Prefab(p, "res://enemies/red_robot/red_robot.tscn"),
            ["bullet"] = Prefab(p, "res://player/bullet/bullet.tscn"),
            ["door"] = Prefab(p, "res://door/door.tscn"),
        };
        var level = new JsonObject
        {
            ["version"] = 1, ["source"] = lvl,
            ["settings"] = new JsonObject { ["environment"] = env, ["busLayout"] = buses, ["physicsLayers"] = layers, ["music"] = music },
            ["instances"] = instances, ["colliders"] = colliders, ["spawns"] = spawns, ["volumes"] = volumes, ["triggers"] = otherTriggers,
            ["dynamics"] = FillDynamicModels(dynamics, instances), ["dynamicAnimations"] = Animations(f), ["reflectionProbes"] = probes, ["lightProbes"] = lightProbes,
            ["prefabs"] = prefabs,
        };
        counts = new JsonObject
        {
            ["instances"] = instances.Count, ["colliders"] = colliders.Count, ["playerSpawns"] = ((JsonArray)spawns["playerSpawns"]!).Count,
            ["robotSpawns"] = ((JsonArray)spawns["robotSpawns"]!).Count, ["volumes"] = volumes.Count, ["reflectionProbes"] = probes.Count,
            ["lightProbes"] = lightProbes.Count, ["dynamics"] = dynamics.Count, ["levelNodesFlattened"] = fs.Order.Count,
            ["boxColliders"] = colliders.Cast<JsonObject>().Count(c => c["type"]!.GetValue<string>() == "box"),
        };
        return level;
    }
}
