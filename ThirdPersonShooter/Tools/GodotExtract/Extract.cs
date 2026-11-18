using System.Text.Json;
using System.Text.Json.Nodes;

namespace GodotExtract;

public static class Extract
{
    public static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    public const int TextureCap = 2048;

    static JsonArray? Floats(Val? v) => v == null ? null : new JsonArray(v.Numbers().Select(d => (JsonNode)JsonValue.Create(d)!).ToArray());
    static string? TexName(GodotFile f, Val? v)
    {
        if (v is VCall c && c.Name == "ExtResource" && f.Ext.TryGetValue(c.FirstString ?? "", out var e) && e.Attr("path") is string p)
            return Project.ContentName(p);
        return null;
    }
    static double Num(Section r, string k, double def) => r.Prop(k) is VNum n ? n.D : def;
    static int Int(Section r, string k, int def) => r.Prop(k) is VNum n ? (int)n.D : def;

    // textures used as sRGB colour (albedo/emission); everything else linear. Filled by Materials().
    public static readonly HashSet<string> SrgbTextures = new(), LinearTextures = new();

    public static JsonObject Materials(Project p)
    {
        var mats = new JsonObject();
        var shaderMats = new JsonArray();
        foreach (var disk in p.FindFiles(".tres"))
        {
            var f = GodotFile.ParseFile(disk);
            string type = f.Header?.Attr("type") ?? "";
            if (type is not ("StandardMaterial3D" or "ORMMaterial3D" or "ShaderMaterial")) continue;
            string res = Project.ToRes(p.Root, disk);
            var r = f.Resource; if (r == null) continue;
            var m = new JsonObject { ["path"] = res, ["type"] = type, ["name"] = r.Prop("resource_name") is VStr rn ? rn.S : Path.GetFileNameWithoutExtension(disk) };
            if (type == "ShaderMaterial")
            {
                var sh = r.Prop("shader");
                m["shader"] = sh != null ? Resolve.Json(sh, f) : null;
                var pr = new JsonObject();
                foreach (var (k, v) in r.Props.Where(kv => kv.Key.StartsWith("shader_parameter/"))) pr[k["shader_parameter/".Length..]] = Resolve.Json(v, f);
                m["shaderParams"] = pr; m["translated"] = false;
                mats[res] = m; shaderMats.Add(res); continue;
            }
            string? albedo = TexName(f, r.Prop("albedo_texture")), emis = TexName(f, r.Prop("emission_texture"));
            string? metTex = TexName(f, r.Prop("metallic_texture")), rouTex = TexName(f, r.Prop("roughness_texture")), aoTex = TexName(f, r.Prop("ao_texture"));
            string? normal = TexName(f, r.Prop("normal_texture"));
            m["albedo"] = albedo; m["albedoColor"] = Floats(r.Prop("albedo_color")) ?? new JsonArray(1, 1, 1, 1);
            m["normal"] = normal; m["normalScale"] = Num(r, "normal_scale", 1);
            m["orm"] = rouTex ?? metTex ?? aoTex; // Godot ORM material: one packed texture sampled through channel selectors
            m["roughnessTexture"] = rouTex; m["metallicTexture"] = metTex; m["aoTexture"] = aoTex;
            m["texChannels"] = new JsonObject { ["ao"] = Int(r, "ao_texture_channel", 0), ["roughness"] = Int(r, "roughness_texture_channel", 0), ["metallic"] = Int(r, "metallic_texture_channel", 0) }; // 0=R 1=G 2=B 3=A
            m["metallic"] = Num(r, "metallic", 0); m["roughness"] = Num(r, "roughness", 1); m["specular"] = Num(r, "metallic_specular", 0.5);
            m["emissionEnabled"] = r.Prop("emission_enabled") is VBool eb && eb.B;
            m["emission"] = emis; m["emissionColor"] = Floats(r.Prop("emission")); m["emissionEnergy"] = Num(r, "emission_energy_multiplier", 1);
            m["aoEnabled"] = r.Prop("ao_enabled") is VBool ab && ab.B; m["normalEnabled"] = r.Prop("normal_enabled") is VBool nb && nb.B;
            m["uv1Scale"] = Floats(r.Prop("uv1_scale")) ?? new JsonArray(1, 1, 1);
            m["uv1Offset"] = Floats(r.Prop("uv1_offset")) ?? new JsonArray(0, 0, 0);
            m["uv1Triplanar"] = r.Prop("uv1_triplanar") is VBool tb && tb.B;
            m["cull"] = Int(r, "cull_mode", 0);            // 0 back, 1 front, 2 disabled
            m["transparency"] = Int(r, "transparency", 0); // 0 off, 1 alpha, 2 scissor, 3 hash, 4 depth pre-pass
            m["blendMode"] = Int(r, "blend_mode", 0);
            m["alphaScissor"] = Num(r, "alpha_scissor_threshold", 0.5);
            m["shadingMode"] = Int(r, "shading_mode", 1);
            m["textureFilter"] = Int(r, "texture_filter", 3);
            m["unshaded"] = Int(r, "shading_mode", 1) == 0;
            m["vertexColorAsAlbedo"] = r.Prop("vertex_color_use_as_albedo") is VBool vb && vb.B;
            mats[res] = m;
            foreach (var t in new[] { albedo, emis }) if (t != null) SrgbTextures.Add(t);
            foreach (var t in new[] { normal, rouTex, metTex, aoTex }) if (t != null) LinearTextures.Add(t);
        }
        // glb material-name -> .tres mapping from .glb.import (use_external)
        var glbMap = new JsonObject();
        foreach (var disk in p.FindFiles(".import").Where(d => d.EndsWith(".glb.import")))
        {
            var f = GodotFile.ParseFile(disk);
            var par = f.Of("params").FirstOrDefault();
            if (par?.Prop("_subresources") is not VDict d) continue;
            var o = new JsonObject();
            foreach (var (k, v) in d.Items)
                if (k is VStr ks && ks.S == "materials" && v is VDict md)
                    foreach (var (mk, mv) in md.Items)
                        if (mv is VDict inner)
                            foreach (var (ik, iv) in inner.Items)
                                if (ik is VStr iks && iks.S == "use_external/fallback_path" && iv is VStr path && mk is VStr mks) o[mks.S] = path.S;
            if (o.Count > 0) glbMap[Path.GetFileName(disk)[..^".glb.import".Length]] = o;
        }
        return new JsonObject { ["materials"] = mats, ["shaderMaterials"] = shaderMats, ["glbMaterialMap"] = glbMap };
    }

    static (int w, int h)? PngSize(string path)
    {
        try { using var fs = File.OpenRead(path); var b = new byte[24]; if (fs.Read(b, 0, 24) < 24 || b[1] != 'P') return null;
              return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]); }
        catch { return null; }
    }

    public static JsonObject AssetMap(Project p)
    {
        if (SrgbTextures.Count == 0 && LinearTextures.Count == 0) Materials(p);
        var assets = new JsonObject(); var names = new Dictionary<string, string>(); var collisions = new JsonArray();
        string[] tex = { ".png", ".jpg", ".jpeg", ".webp", ".hdr", ".exr" };
        string[] all = tex.Concat(new[] { ".wav", ".ogg", ".glb", ".ttf", ".mesh" }).ToArray();
        foreach (var disk in all.SelectMany(e => p.FindFiles(e)).OrderBy(x => x, StringComparer.Ordinal))
        {
            string res = Project.ToRes(p.Root, disk);
            if (res.StartsWith("res://screenshots/")) continue;
            string ext = Path.GetExtension(disk).ToLowerInvariant();
            string pathName = Project.ContentName(res);
            string name = ext == ".glb" ? Path.GetFileNameWithoutExtension(disk).ToLowerInvariant() : pathName;
            var o = new JsonObject { ["name"] = name, ["pathName"] = pathName };
            GodotFile? imp = File.Exists(disk + ".import") ? GodotFile.ParseFile(disk + ".import") : null;
            var par = imp?.Of("params").FirstOrDefault();
            if (tex.Contains(ext))
            {
                o["type"] = ext == ".exr" ? "exr-unsupported" : "texture";
                bool normalMap = par?.Prop("compress/normal_map") is VNum nm && nm.D == 1;
                bool srgb = !(LinearTextures.Contains(pathName) && !SrgbTextures.Contains(pathName)) && !normalMap && ext != ".exr";
                o["srgb"] = srgb;
                if (SrgbTextures.Contains(pathName) && LinearTextures.Contains(pathName)) o["srgbConflict"] = true;
                o["normalMap"] = normalMap || (LinearTextures.Contains(pathName) && name.Contains("normal"));
                o["mipmaps"] = par?.Prop("mipmaps/generate") is VBool mb ? mb.B : true;
                var sz = ext == ".png" ? PngSize(disk) : null;
                if (sz != null) { o["width"] = sz.Value.w; o["height"] = sz.Value.h; o["capped"] = Math.Max(sz.Value.w, sz.Value.h) > TextureCap; } // metadata only; resize is done by the content generator
                o["maxSize"] = TextureCap;
            }
            else if (ext is ".wav" or ".ogg")
            {
                o["type"] = ext == ".ogg" ? "song" : "sound";
                bool loop = false; int loopMode = 0;
                if (ext == ".wav") { loopMode = par?.Prop("edit/loop_mode") is VNum lm ? (int)lm.D : 0; loop = loopMode != 0; }
                else loop = par?.Prop("loop") is VBool lb && lb.B;
                o["loop"] = loop; if (ext == ".wav") o["loopMode"] = loopMode;
                if (ext == ".ogg") o["loopOffset"] = par?.Prop("loop_offset") is VNum lo ? lo.D : 0;
                o["spatial"] = false; // 3D/spatial info lives on the AudioStreamPlayer3D nodes (see prefab sections of level.scene)
            }
            else o["type"] = ext == ".glb" ? "model" : ext == ".ttf" ? "font" : "mesh";
            o["path"] = res;
            if (names.TryGetValue(name, out var other)) { collisions.Add($"{name}: {other} vs {res}"); name = pathName; o["name"] = name; }
            names[name] = res;
            assets[res] = o;
        }
        return new JsonObject { ["textureCap"] = TextureCap, ["assets"] = assets, ["nameCollisions"] = collisions };
    }
}
