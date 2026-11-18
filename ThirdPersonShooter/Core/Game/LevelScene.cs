using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>One drawable mesh (all parts) with its flattened world matrix.</summary>
    public sealed class DrawItem
    {
        public ModelMesh Mesh;
        public Matrix World;
        public PbrMaterial[] PartMaterials;
        public string Model;
        public Matrix Local;       // Dynamic items: world before the level animation
        public bool Dynamic;       // FlyingForklifts/*: World = Local * LevelAnimation.ParentTransform(t)
    }

    /// <summary>Loads level.scene.json + models, resolves @marker bones into flat draw items.</summary>
    public sealed class LevelScene
    {
        public readonly List<DrawItem> Items = new();
        public Matrix PlayerSpawn = Matrix.Identity;
        public LevelAnimation Anim = new LevelAnimation();
        public readonly List<int> ForkliftVariants = new();
        /// <summary>World matrix of each FlyingForklift node (before the mawaru parent rotation), in scene order.</summary>
        public readonly List<Matrix> ForkliftRoots = new();
        /// <summary>FlyingForklift i world matrix at level time t (root * mawaru parent transform).</summary>
        public Matrix ForkliftWorld(int i, float t) => ForkliftRoots[i] * Anim.ParentTransform(t);

        /// <summary>Evaluate the level animation (mawaru) at sim time t and move the dynamic draw items.</summary>
        public void UpdateDynamics(float t)
        {
            var parent = Anim.ParentTransform(t);
            foreach (var it in Items) if (it.Dynamic) it.World = it.Local * parent;
        }

        public static LevelScene Load(ContentManager content, string contentRoot)
        {
            var s = new LevelScene();
            foreach (var _ in s.LoadSteps(content, contentRoot)) { }
            return s;
        }

        /// <summary>
        /// Incremental load for the loading screen: fills <see cref="Items"/>, yielding the fraction done (0..1) after each distinct model (with its textures) is loaded,
        /// so the caller can spread the work over frames. All GL work happens on the calling thread inside MoveNext.
        /// </summary>
        public IEnumerable<float> LoadSteps(ContentManager content, string contentRoot)
        {
            var s = this;
            using var sceneDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", "level.scene.json")));
            using var matDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", "Materials.json")));
            var mats = matDoc.RootElement.GetProperty("materials");
            var map = matDoc.RootElement.GetProperty("glbMaterialMap");

            s.Anim = LevelAnimation.Load(contentRoot);
            s.PlayerSpawn = ReadMatrix(sceneDoc.RootElement.GetProperty("spawns").GetProperty("playerSpawns")[0].GetProperty("world"));

            var distinct = new HashSet<string>();
            foreach (var inst in sceneDoc.RootElement.GetProperty("instances").EnumerateArray()) if (inst.GetProperty("model").ValueKind == JsonValueKind.String) distinct.Add(inst.GetProperty("model").GetString());
            int loadedModels = 0;
            var models = new Dictionary<string, Model>();
            var matCache = new Dictionary<string, PbrMaterial>();
            var texCache = new Dictionary<string, Texture2D>();
            foreach (var inst in sceneDoc.RootElement.GetProperty("instances").EnumerateArray())
            {
                if (inst.GetProperty("model").ValueKind != JsonValueKind.String) continue;
                string name = inst.GetProperty("model").GetString();
                Matrix instWorld = ReadMatrix(inst.GetProperty("world")); bool fresh = false;
                bool dyn = inst.TryGetProperty("dynamic", out var dynProp) && dynProp.GetBoolean();
                if (!models.TryGetValue(name, out var model))
                {
                    try { model = content.Load<Model>("Models/" + name); } catch (Exception e) { Console.WriteLine($"[scene] cannot load Models/{name}: {e.Message}"); continue; }
                    models[name] = model; fresh = true;
                }
                var abs = new Matrix[model.Bones.Count];
                model.CopyAbsoluteBoneTransformsTo(abs);
                var meshByBone = new Dictionary<string, ModelMesh>();
                foreach (var m in model.Meshes) meshByBone[m.ParentBone.Name] = m;
                var colorCache = new Dictionary<ModelMesh, PbrMaterial[]>();
                int markers = 0;
                ModelBone variant = null;   // dynamic forklift: only one of the glb root's children (Red/Yellow/Blue) is visible, flying_forklift.gd _ready
                if (dyn)
                {
                    var vs = new List<ModelBone>(); foreach (var b in model.Root.Children) if (b.Name == null || !b.Name.StartsWith("mesh/")) vs.Add(b);
                    int k = LevelAnimation.PickVariant(s.ForkliftVariants.Count, vs.Count); s.ForkliftVariants.Add(k); s.ForkliftRoots.Add(instWorld); variant = vs[k];
                    Console.WriteLine($"[scene] forklift {s.ForkliftVariants.Count - 1}: variant {k} = {variant.Name}");
                }
                foreach (var bone in model.Bones)
                {
                    if (bone.Name == null || !bone.Name.StartsWith("@")) continue;
                    if (variant != null) { var top = bone; while (top.Parent != null && top.Parent != model.Root) top = top.Parent; if (top != variant) continue; }
                    if (!meshByBone.TryGetValue("mesh/" + bone.Name.Substring(1), out var mesh)) { Console.WriteLine($"[scene] marker {bone.Name} has no mesh"); continue; }
                    if (!colorCache.TryGetValue(mesh, out var cols))
                    {
                        cols = new PbrMaterial[mesh.MeshParts.Count];
                        for (int p = 0; p < cols.Length; p++)
                            cols[p] = ResolveMaterial(map, mats, name, mesh.MeshParts[p].Tag as string, matCache, texCache, content);
                        colorCache[mesh] = cols;
                    }
                    s.Items.Add(new DrawItem { Mesh = mesh, World = abs[bone.Index] * instWorld, Local = abs[bone.Index] * instWorld, Dynamic = dyn, PartMaterials = cols, Model = name });
                    markers++;
                }
                Console.WriteLine($"[scene] {name}: {markers} instances");
                if (fresh) { loadedModels++; yield return loadedModels / (float)Math.Max(1, distinct.Count); }
            }
            yield return 1f;
        }

        public static PbrMaterial ResolveMaterial(JsonElement map, JsonElement mats, string model, string tag,
            Dictionary<string, PbrMaterial> matCache, Dictionary<string, Texture2D> texCache, ContentManager content)
        {
            if (string.IsNullOrEmpty(tag)) return null; // glb primitives without a material (hidden helper geometry in Godot): not drawn
            if (!map.TryGetProperty(model, out var mm) || !mm.TryGetProperty(tag, out var path)) { Console.WriteLine($"[mat] no mapping for {model}/{tag}"); return PbrMaterial.Fallback; }
            string key = path.GetString();
            if (matCache.TryGetValue(key, out var cached)) return cached;
            PbrMaterial r = PbrMaterial.Fallback;
            if (mats.TryGetProperty(key, out var mat))
                r = PbrMaterial.FromJson(mat, tn =>
                {
                    if (texCache.TryGetValue(tn, out var t)) return t;
                    try { t = content.Load<Texture2D>("Textures/" + tn); }
                    catch (Exception e) { Console.WriteLine($"[mat] texture {tn} missing: {e.Message}"); t = null; }
                    texCache[tn] = t; return t;
                });
            matCache[key] = r;
            return r;
        }

        static Matrix ReadMatrix(JsonElement a)
        {
            var f = new float[16];
            for (int i = 0; i < 16; i++) f[i] = a[i].GetSingle();
            return new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
        }
    }
}
