using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Shooter.Scene;

namespace SceneBake
{
    /// <summary>
    /// Converts Tools/UnityExtractor/out (flattened MainScene.json + materials.json) into the compact runtime scene
    /// Core/Content/Scenes/MainScene.scene.json. Usage (from anywhere inside the Shooter folder):
    ///   dotnet run -c Release --project Tools/SceneBake [-- --out path]
    /// All Unity -> MonoGame conversion goes through Shooter.Scene.UnityConvert (see ASSET-CONVENTIONS.md section 1).
    /// </summary>
    internal static class Program
    {
        private static string _root;
        private static JsonElement _assetMap;
        private static readonly SceneFile Scene = new SceneFile();
        private static readonly Dictionary<string, int> MaterialByGuid = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> MaterialByPath = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> MeshRefIndex = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> EnemyByControllerId = new Dictionary<string, int>();
        private static readonly List<string> Warnings = new List<string>();
        private static readonly Dictionary<string, int> Stats = new Dictionary<string, int>();
        private static float _maxWorldMismatch;
        /// <summary>World placement applied to a prefab that is walked on its own (the extractor's world transforms of such a prefab are relative to its own origin).</summary>
        private static Matrix _placement = Matrix.Identity;

        /// <summary>
        /// Prefabs the Unity scene does not place but this port does (see PORTING-PLAN, Jetpack): prefab json, Unity world position of the root.
        /// MainScene has no Pickup_Jetpack (only SecondaryScene refers to Loot_Jetpack): one is added in the second room, its root on the floor like the health pickups (floor 0.25 there).
        /// </summary>
        private static readonly (string prefab, float[] unityPosition)[] ExtraPlacements =
        {
            ("Pickup_Jetpack", new[] { -5f, 0.25f, -3f })
        };
        private static int _worldFallbacks;

        private static int Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            _root = FindRoot();
            string outFile = Path.Combine(_root, "Core", "Content", "Scenes", "MainScene.scene.json");
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--out")
                {
                    outFile = Path.GetFullPath(args[i + 1]);
                }
            }

            string exOut = Path.Combine(_root, "Tools", "UnityExtractor", "out");
            using JsonDocument assetMapDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(_root, "Core", "Content", "AssetMap.json")));
            _assetMap = assetMapDoc.RootElement.GetProperty("assets");
            using JsonDocument matDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(exOut, "materials.json")));
            using JsonDocument sceneDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(exOut, "Scenes", "MainScene.json")));
            using JsonDocument weaponDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(exOut, "Prefabs", "Weapon_Blaster.json")));
            using JsonDocument projectileDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(exOut, "Prefabs", "Projectile_Blaster.json")));
            using JsonDocument turretProjectileDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(exOut, "Prefabs", "Projectile_Turret.json")));
            using JsonDocument enemyProjectileDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(exOut, "Prefabs", "Projectile_Hoverbot.json")));
            JsonElement scene = sceneDoc.RootElement;
            Scene.Source = scene.GetProperty("source").GetString();

            BakeMaterials(matDoc.RootElement.GetProperty("materials"));

            var roots = new List<JsonElement>();
            foreach (JsonElement r in scene.GetProperty("roots").EnumerateArray())
            {
                roots.Add(r);
            }

            var ctx = new WalkContext();
            foreach (JsonElement r in roots)
            {
                Walk(r, Matrix.Identity, true, r.GetProperty("name").GetString(), false, ctx);
            }
            foreach ((string prefab, float[] unityPosition) in ExtraPlacements)
            {
                using JsonDocument prefabDoc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(exOut, "Prefabs", prefab + ".json")));
                _placement = Matrix.CreateTranslation(UnityConvert.Position(new Vector3(unityPosition[0], unityPosition[1], unityPosition[2])));
                foreach (JsonElement r in prefabDoc.RootElement.GetProperty("roots").EnumerateArray())
                {
                    Walk(r, _placement, true, r.GetProperty("name").GetString(), false, ctx);
                }
                _placement = Matrix.Identity;
            }
            ResolvePatrolPaths(ctx);

            BakeLighting(scene.GetProperty("settings").GetProperty("RenderSettings"), ctx);
            BakeWeapon(weaponDoc.RootElement, ctx);
            Scene.Projectile = BakeProjectile(projectileDoc.RootElement, "Projectile_Blaster");
            Scene.EnemyProjectile = BakeProjectile(enemyProjectileDoc.RootElement, "Projectile_Hoverbot");
            Scene.TurretProjectile = BakeProjectile(turretProjectileDoc.RootElement, "Projectile_Turret");
            Scene.Player.StartPositionUnity = ctx.PlayerUnityPos;
            // Actor.AimPoint of the Player is a Transform reference the extractor cannot resolve; read by hand from Player.prefab (Transform 3625601170703586302 = child "AimPoint", local position (0, 1.09, 0) under the root).
            Scene.Player.AimPointInRoot = new[] { 0f, 1.09f, 0f };

            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
            Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(Scene, options);
            File.WriteAllBytes(outFile, bytes);

            Console.WriteLine("Wrote " + outFile + " (" + (bytes.Length / 1024) + " KB) in " + sw.ElapsedMilliseconds + " ms");
            Console.WriteLine("  mesh refs " + Scene.MeshRefs.Count + ", materials " + Scene.Materials.Count + ", instances " + Scene.Instances.Count +
                              ", model instances " + Scene.ModelInstances.Count + ", colliders " + Scene.Colliders.Count);
            Console.WriteLine("  enemies " + Scene.Enemies.Count + ", pickups " + Scene.Pickups.Count + ", patrol paths " + Scene.PatrolPaths.Count +
                              ", objectives " + Scene.Objectives.Count + ", emitters " + Scene.Emitters.Count + ", projectors " + Scene.Projectors.Count +
                              ", weapon pieces " + (Scene.Weapon != null ? Scene.Weapon.Pieces.Count : 0));
            Console.WriteLine("  world matrix check: local-composed vs extractor world translation, max mismatch " + _maxWorldMismatch.ToString("0.#####") +
                              " (" + _worldFallbacks + " nodes fell back to the extractor's world TRS)");
            foreach (KeyValuePair<string, int> kv in Stats)
            {
                Console.WriteLine("  " + kv.Key + ": " + kv.Value);
            }
            foreach (string w in Warnings)
            {
                Console.WriteLine("  WARN " + w);
            }
            return 0;
        }

        private static string FindRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "Core", "Content", "AssetMap.json")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "Core", "Content", "AssetMap.json")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new InvalidOperationException("Run from inside the Shooter folder.");
        }

        private static void Bump(string key)
        {
            Stats[key] = (Stats.TryGetValue(key, out int v) ? v : 0) + 1;
        }

        // ------------------------------------------------------------------ JSON helpers

        private static bool Has(JsonElement e, string name)
        {
            return e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind != JsonValueKind.Null;
        }

        private static float F(JsonElement e, string name, float def = 0f)
        {
            if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number)
            {
                return (float)v.GetDouble();
            }
            return def;
        }

        private static float[] Arr(JsonElement a)
        {
            var r = new float[a.GetArrayLength()];
            int i = 0;
            foreach (JsonElement x in a.EnumerateArray())
            {
                r[i++] = (float)x.GetDouble();
            }
            return r;
        }

        private static Vector3 V3(JsonElement obj)   // {x,y,z}
        {
            return new Vector3(F(obj, "x"), F(obj, "y"), F(obj, "z"));
        }

        private static Vector3 V3a(JsonElement a)    // [x,y,z]
        {
            float[] f = Arr(a);
            return new Vector3(f[0], f[1], f[2]);
        }

        private static float[] Rgba(JsonElement c)   // {r,g,b,a}
        {
            return new[] { R(F(c, "r")), R(F(c, "g")), R(F(c, "b")), R(F(c, "a", 1f)) };
        }

        private static float R(float v)
        {
            return (float)Math.Round(v, 6);
        }

        private static float[] Mat(Matrix m)
        {
            return new[]
            {
                R(m.M11), R(m.M12), R(m.M13), R(m.M14), R(m.M21), R(m.M22), R(m.M23), R(m.M24),
                R(m.M31), R(m.M32), R(m.M33), R(m.M34), R(m.M41), R(m.M42), R(m.M43), R(m.M44)
            };
        }

        private static float[] Vec(Vector3 v)
        {
            return new[] { R(v.X), R(v.Y), R(v.Z) };
        }

        private static JsonElement Prop(JsonElement c)
        {
            if (c.TryGetProperty("props", out JsonElement p))
            {
                return p;
            }
            return c.GetProperty("fields");
        }

        // ------------------------------------------------------------------ asset names

        private static string AssetName(string unityPath)
        {
            if (unityPath != null && _assetMap.TryGetProperty(unityPath, out JsonElement a) && a.TryGetProperty("name", out JsonElement n))
            {
                return n.GetString();
            }
            return null;
        }

        private static string MaterialNameFromPath(string path)
        {
            return Path.GetFileNameWithoutExtension(path ?? string.Empty);
        }

        private static int MaterialIndex(JsonElement matRef)
        {
            if (matRef.ValueKind != JsonValueKind.Object)
            {
                return -1;
            }
            if (matRef.TryGetProperty("guid", out JsonElement g) && g.GetString() != null && MaterialByGuid.TryGetValue(g.GetString(), out int i))
            {
                return i;
            }
            if (matRef.TryGetProperty("path", out JsonElement p) && p.ValueKind == JsonValueKind.String && MaterialByPath.TryGetValue(p.GetString(), out i))
            {
                return i;
            }
            Warnings.Add("material not in materials.json: " + matRef.ToString());
            return -1;
        }

        private static int[] MaterialIndices(JsonElement array)
        {
            var list = new List<int>();
            if (array.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement m in array.EnumerateArray())
                {
                    list.Add(MaterialIndex(m));
                }
            }
            return list.ToArray();
        }

        private static int MeshRef(string modelPath, string meshName)
        {
            string model = AssetName(modelPath);
            if (model == null)
            {
                Warnings.Add("mesh asset not in AssetMap: " + modelPath);
                return -1;
            }
            string key = model + "|" + meshName;
            if (!MeshRefIndex.TryGetValue(key, out int idx))
            {
                idx = Scene.MeshRefs.Count;
                Scene.MeshRefs.Add(new MeshRefDto { Model = model, Mesh = meshName });
                MeshRefIndex[key] = idx;
            }
            return idx;
        }

        // ------------------------------------------------------------------ materials

        private static void BakeMaterials(JsonElement materials)
        {
            foreach (JsonElement m in materials.EnumerateArray())
            {
                MaterialDto dto = BakeMaterial(m);
                int idx = Scene.Materials.Count;
                Scene.Materials.Add(dto);
                MaterialByGuid[m.GetProperty("guid").GetString()] = idx;
                MaterialByPath[m.GetProperty("path").GetString()] = idx;
            }
        }

        private static MaterialDto BakeMaterial(JsonElement m)
        {
            string name = m.GetProperty("name").GetString();
            string shader = m.GetProperty("shader").GetProperty("name").GetString();
            var floats = new Dictionary<string, float>();
            if (m.TryGetProperty("floats", out JsonElement fl))
            {
                foreach (JsonProperty p in fl.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.Number)
                    {
                        floats[p.Name] = (float)p.Value.GetDouble();
                    }
                }
            }
            var colors = new Dictionary<string, float[]>();
            if (m.TryGetProperty("colors", out JsonElement co))
            {
                foreach (JsonProperty p in co.EnumerateObject())
                {
                    colors[p.Name] = Arr(p.Value);
                }
            }
            var keywords = new List<string>();
            if (m.TryGetProperty("keywords", out JsonElement kw))
            {
                foreach (JsonElement k in kw.EnumerateArray())
                {
                    keywords.Add(k.GetString());
                }
            }
            var textures = new Dictionary<string, string>();
            var tiling = new Dictionary<string, float[]>();
            if (m.TryGetProperty("textures", out JsonElement te))
            {
                foreach (JsonProperty p in te.EnumerateObject())
                {
                    JsonElement tex = p.Value.GetProperty("texture");
                    string path = tex.ValueKind == JsonValueKind.Object && tex.TryGetProperty("path", out JsonElement pp) && pp.ValueKind == JsonValueKind.String ? pp.GetString() : null;
                    if (path == null)
                    {
                        continue;
                    }
                    string cn = AssetName(path);
                    if (cn == null)
                    {
                        Warnings.Add("texture not in AssetMap: " + path + " (material " + name + ")");
                        continue;
                    }
                    textures[p.Name] = cn;
                    float[] t = Arr(p.Value.GetProperty("tiling"));
                    float[] o = Arr(p.Value.GetProperty("offset"));
                    tiling[p.Name] = new[] { t[0], t[1], o[0], o[1] };
                }
            }

            var d = new MaterialDto { Name = name, Shader = shader, Keywords = keywords, Textures = textures, Floats = floats, Vectors = colors };
            d.Kind = shader.StartsWith("Universal Render Pipeline/Lit", StringComparison.Ordinal) ? "Lit"
                   : shader == "Universal Render Pipeline/Particles/Unlit" ? "ParticlesUnlit"
                   : shader == "Universal Render Pipeline/Particles/Lit" ? "ParticlesLit"
                   : shader == "Projector/Multiply" ? "ProjectorMultiply"
                   : shader.Contains("Sprite") ? "SpriteUnlit"
                   : name == "Skybox" ? "Skybox" : "Lit";

            bool lit = d.Kind == "Lit";
            bool transparent = floats.TryGetValue("_Surface", out float sf) && sf >= 1f || keywords.Contains("_SURFACE_TYPE_TRANSPARENT");
            d.Surface = transparent ? "Transparent" : "Opaque";

            floats.TryGetValue("_SrcBlend", out float src);
            floats.TryGetValue("_DstBlend", out float dst);
            // Unity BlendMode enum: Zero 0, One 1, DstColor 2, SrcAlpha 5, OneMinusSrcAlpha 10.
            d.Blend = !transparent ? "Alpha"
                    : (int)src == 2 ? "Multiply"
                    : (int)dst == 1 ? "Additive"
                    : ((int)dst == 10 && (int)src == 1) ? "Premultiply" : "Alpha";

            float cull = floats.TryGetValue("_Cull", out float cf) ? cf : 2f;
            d.Cull = (int)cull == 0 ? "None" : (int)cull == 1 ? "Front" : "Back";
            d.ZWrite = !floats.TryGetValue("_ZWrite", out float zw) || zw != 0f;
            d.AlphaClip = (floats.TryGetValue("_AlphaClip", out float ac) && ac >= 1f) || keywords.Contains("_ALPHATEST_ON");
            d.Cutoff = floats.TryGetValue("_Cutoff", out float cut) ? cut : 0.5f;
            d.RenderQueue = m.TryGetProperty("renderQueue", out JsonElement rq) ? (int)rq.GetDouble() : -1;
            d.Emission = keywords.Contains("_EMISSION");
            d.SpecularHighlights = !keywords.Contains("_SPECULARHIGHLIGHTS_OFF");
            d.EnvironmentReflections = !keywords.Contains("_ENVIRONMENTREFLECTIONS_OFF");
            d.ReceiveShadows = !floats.TryGetValue("_ReceiveShadows", out float rs) || rs != 0f;

            float[] bc;
            if (!colors.TryGetValue("_BaseColor", out bc) && !colors.TryGetValue("_Color", out bc))
            {
                bc = new[] { 1f, 1f, 1f, 1f };
            }
            d.BaseColor = bc;
            float[] ec = colors.TryGetValue("_EmissionColor", out float[] e0) ? e0 : new float[] { 0, 0, 0, 1 };
            d.EmissionColor = d.Emission ? new[] { ec[0], ec[1], ec[2] } : new float[] { 0, 0, 0 };
            d.Metallic = floats.TryGetValue("_Metallic", out float met) ? met : 0f;
            d.Smoothness = floats.TryGetValue("_Smoothness", out float sm) ? sm : floats.TryGetValue("_Glossiness", out float gl) ? gl : 0.5f;
            d.BumpScale = floats.TryGetValue("_BumpScale", out float bs) ? bs : 1f;
            d.OcclusionStrength = floats.TryGetValue("_OcclusionStrength", out float os) ? os : 1f;

            string T(string a, string b = null)
            {
                if (textures.TryGetValue(a, out string v))
                {
                    return v;
                }
                return b != null && textures.TryGetValue(b, out v) ? v : null;
            }
            d.BaseMap = T("_BaseMap", "_MainTex");
            // NORMALMAP/OCCLUSIONMAP/METALLICSPECGLOSSMAP keywords: textures are only active when the keyword is set
            d.NormalMap = keywords.Contains("_NORMALMAP") ? T("_BumpMap") : null;
            d.OcclusionMap = keywords.Contains("_OCCLUSIONMAP") ? T("_OcclusionMap") : null;
            d.MetallicGlossMap = keywords.Contains("_METALLICSPECGLOSSMAP") ? T("_MetallicGlossMap") : null;
            d.EmissionMap = d.Emission ? T("_EmissionMap") : null;
            if (!lit && d.Kind != "ParticlesLit")
            {
                d.NormalMap = null;
                d.OcclusionMap = null;
                d.MetallicGlossMap = null;
            }
            string tk = textures.ContainsKey("_BaseMap") ? "_BaseMap" : "_MainTex";
            d.TilingOffset = tiling.TryGetValue(tk, out float[] to) ? to : new float[] { 1, 1, 0, 0 };
            return d;
        }

        // ------------------------------------------------------------------ hierarchy walk

        private class WalkContext
        {
            public readonly List<(string compId, JsonElement fields)> PatrolPathComponents = new List<(string, JsonElement)>();
            public readonly List<(int path, JsonElement fields)> PatrolFields = new List<(int, JsonElement)>();
            public Matrix MainCameraWorld = Matrix.Identity;
            public bool HaveMainCamera;
            public JsonElement Light;
            public Matrix LightWorld;
            public bool HaveLight;
            public float[] PlayerUnityPos;
            public Matrix DefaultWeaponWorld;
            public bool HaveDefaultWeapon;
            public Matrix AimingWeaponWorld, DownWeaponWorld;
            public bool HaveAimingWeapon, HaveDownWeapon;
        }

        private static Matrix NodeWorld(JsonElement go, Matrix parentWorld)
        {
            JsonElement t = go.GetProperty("transform");
            Matrix local = UnityConvert.TrsToMatrix(
                V3a(t.GetProperty("localPosition")),
                new Quaternion(Arr(t.GetProperty("localRotation"))[0], Arr(t.GetProperty("localRotation"))[1], Arr(t.GetProperty("localRotation"))[2], Arr(t.GetProperty("localRotation"))[3]),
                V3a(t.GetProperty("localScale")));
            Matrix world = local * parentWorld;

            // Sanity check against the extractor's own world transform; model placeholder nodes (bones/nodes) have
            // identity locals in the extractor, so there we trust the extractor's world TRS instead.
            Vector3 wp = Vector3.Transform(UnityConvert.Position(V3a(t.GetProperty("worldPosition"))), _placement);
            float mismatch = Vector3.Distance(world.Translation, wp);
            if (mismatch > 0.002f)
            {
                float[] q = Arr(t.GetProperty("worldRotation"));
                world = UnityConvert.TrsToMatrix(V3a(t.GetProperty("worldPosition")), new Quaternion(q[0], q[1], q[2], q[3]), V3a(t.GetProperty("worldScale"))) * _placement;
                _worldFallbacks++;
            }
            else
            {
                _maxWorldMismatch = Math.Max(_maxWorldMismatch, mismatch);
            }
            return world;
        }

        private static string ScriptOf(JsonElement c)
        {
            return c.GetProperty("type").GetString() == "MonoBehaviour" ? c.GetProperty("script").GetString() : c.GetProperty("type").GetString();
        }

        private static void Walk(JsonElement go, Matrix parentWorld, bool parentActive, string top, bool inModel, WalkContext ctx)
        {
            string name = go.GetProperty("name").GetString();
            Matrix world = NodeWorld(go, parentWorld);

            var comps = new List<JsonElement>();
            bool hasModelInstance = false, hasModelNode = false;
            foreach (JsonElement c in go.GetProperty("components").EnumerateArray())
            {
                comps.Add(c);
                string t = c.GetProperty("type").GetString();
                hasModelInstance |= t == "ModelInstance";
                hasModelNode |= t == "ModelNode";
            }

            // The extractor marks FBX/OBJ model roots and their placeholder nodes as inactive (no YAML m_IsActive);
            // Unity shows them, so they inherit the parent's state.
            bool selfActive = go.GetProperty("active").GetBoolean() || hasModelInstance || hasModelNode;
            bool active = parentActive && selfActive;
            bool nowInModel = inModel || hasModelInstance;

            int layer = (int)F(go, "layer");
            int staticFlags = go.TryGetProperty("staticFlags", out JsonElement sfe) && sfe.ValueKind == JsonValueKind.Number ? (int)sfe.GetDouble() : 0;

            int rendererInstance = -1;
            JsonElement meshFilter = default, meshRenderer = default;
            bool haveMf = false, haveMr = false;
            foreach (JsonElement c in comps)
            {
                string t = c.GetProperty("type").GetString();
                if (t == "MeshFilter") { meshFilter = Prop(c); haveMf = true; }
                else if (t == "MeshRenderer") { meshRenderer = Prop(c); haveMr = true; }
            }

            if (active && haveMf && haveMr && F(meshRenderer, "m_Enabled", 1f) != 0f && Has(meshFilter, "m_Mesh"))
            {
                JsonElement mesh = meshFilter.GetProperty("m_Mesh");
                bool builtin = mesh.TryGetProperty("builtin", out JsonElement b) && b.GetBoolean();
                if (!builtin && mesh.TryGetProperty("path", out JsonElement mp))
                {
                    int meshRef = MeshRef(mp.GetString(), mesh.TryGetProperty("name", out JsonElement mn) ? mn.GetString() : null);
                    if (meshRef >= 0)
                    {
                        int cast = (int)F(meshRenderer, "m_CastShadows", 1f);
                        int flags = (cast != 0 ? InstanceFlags.CastShadows : 0)
                                  | (F(meshRenderer, "m_ReceiveShadows", 1f) != 0f ? InstanceFlags.ReceiveShadows : 0)
                                  | (staticFlags != 0 ? InstanceFlags.Static : 0);
                        rendererInstance = Scene.Instances.Count;
                        Scene.Instances.Add(new InstanceDto
                        {
                            Name = name,
                            Mesh = meshRef,
                            Mats = MaterialIndices(meshRenderer.GetProperty("m_Materials")),
                            M = Mat(world),
                            Flags = flags,
                            Layer = layer
                        });
                        Bump("mesh instance: " + Scene.MeshRefs[meshRef].Model);
                    }
                }
            }

            foreach (JsonElement c in comps)
            {
                string type = c.GetProperty("type").GetString();
                switch (type)
                {
                    case "MeshCollider":
                        if (active)
                        {
                            AddMeshCollider(go, c, world, layer, rendererInstance);
                        }
                        break;
                    case "BoxCollider":
                    case "SphereCollider":
                    case "CapsuleCollider":
                    case "CharacterController":
                        if (active || nowInModel)
                        {
                            AddPrimitiveCollider(name, type, Prop(c), world, layer);
                        }
                        break;
                    case "Light":
                        if (active && F(Prop(c), "m_Type") == 1f && !ctx.HaveLight)
                        {
                            ctx.Light = Prop(c);
                            ctx.LightWorld = world;
                            ctx.HaveLight = true;
                        }
                        break;
                    case "Camera":
                        AddCamera(name, Prop(c), world, ctx);
                        break;
                    case "ParticleSystemRenderer":
                        {
                            JsonElement p = Prop(c);
                            string mat = null;
                            if (p.TryGetProperty("m_Materials", out JsonElement ma) && ma.ValueKind == JsonValueKind.Array && ma.GetArrayLength() > 0 && ma[0].ValueKind == JsonValueKind.Object)
                            {
                                mat = MaterialNameFromPath(ma[0].TryGetProperty("path", out JsonElement mpth) ? mpth.GetString() : null);
                            }
                            Scene.Emitters.Add(new EmitterDto { Name = name, Owner = top, M = Mat(world), Active = active, Material = mat });
                        }
                        break;
                    case "Projector":
                        {
                            JsonElement p = Prop(c);
                            string mat = null;
                            if (Has(p, "m_Material") && p.GetProperty("m_Material").TryGetProperty("path", out JsonElement mpth))
                            {
                                mat = MaterialNameFromPath(mpth.GetString());
                            }
                            Scene.Projectors.Add(new ProjectorDto
                            {
                                Name = name,
                                Owner = top,
                                M = Mat(world),
                                Orthographic = F(p, "m_Orthographic") != 0f,
                                OrthoSize = F(p, "m_OrthographicSize"),
                                Near = F(p, "m_NearClipPlane"),
                                Far = F(p, "m_FarClipPlane"),
                                Fov = F(p, "m_FieldOfView"),
                                Material = mat
                            });
                        }
                        break;
                    case "ModelInstance":
                        AddModelInstance(name, Prop(c), world, active);
                        break;
                    case "MonoBehaviour":
                        HandleScript(go, name, c, comps, world, active, ctx);
                        break;
                }
            }

            foreach (JsonElement child in go.GetProperty("children").EnumerateArray())
            {
                Walk(child, world, active, top, nowInModel, ctx);
            }
        }

        private static void AddMeshCollider(JsonElement go, JsonElement c, Matrix world, int layer, int rendererInstance)
        {
            JsonElement p = Prop(c);
            if (F(p, "m_Enabled", 1f) == 0f || !Has(p, "m_Mesh"))
            {
                return;
            }
            JsonElement mesh = p.GetProperty("m_Mesh");
            if (mesh.TryGetProperty("builtin", out JsonElement b) && b.GetBoolean())
            {
                return;
            }
            int meshRef = MeshRef(mesh.GetProperty("path").GetString(), mesh.TryGetProperty("name", out JsonElement mn) ? mn.GetString() : null);
            if (meshRef < 0)
            {
                return;
            }
            var dto = new ColliderDto
            {
                Type = "mesh",
                Owner = go.GetProperty("name").GetString(),
                Layer = layer,
                Trigger = F(p, "m_IsTrigger") != 0f,
                Convex = F(p, "m_Convex") != 0f,
                Mesh = meshRef
            };
            if (rendererInstance >= 0 && Scene.Instances[rendererInstance].Mesh == meshRef)
            {
                dto.Instance = rendererInstance;   // same GameObject, same world matrix
            }
            else
            {
                dto.M = Mat(world);
            }
            Scene.Colliders.Add(dto);
        }

        private static void AddPrimitiveCollider(string owner, string type, JsonElement p, Matrix world, int layer)
        {
            if (F(p, "m_Enabled", 1f) == 0f)
            {
                return;
            }
            Vector3 center = Has(p, "m_Center") ? UnityConvert.Position(V3(p.GetProperty("m_Center"))) : Vector3.Zero;
            var dto = new ColliderDto { Owner = owner, Layer = layer, Trigger = F(p, "m_IsTrigger") != 0f, M = Mat(world), Center = Vec(center) };
            switch (type)
            {
                case "BoxCollider":
                    dto.Type = "box";
                    dto.Size = Vec(V3(p.GetProperty("m_Size")));
                    break;
                case "SphereCollider":
                    dto.Type = "sphere";
                    dto.Radius = F(p, "m_Radius");
                    break;
                case "CapsuleCollider":
                    dto.Type = "capsule";
                    dto.Radius = F(p, "m_Radius");
                    dto.Height = F(p, "m_Height");
                    dto.Direction = (int)F(p, "m_Direction", 1f);
                    break;
                default:
                    dto.Type = "character";
                    dto.Radius = F(p, "m_Radius");
                    dto.Height = F(p, "m_Height");
                    break;
            }
            Scene.Colliders.Add(dto);
        }

        private static void AddCamera(string name, JsonElement p, Matrix world, WalkContext ctx)
        {
            // Strip scale; the matrix is camera-to-world in MonoGame space (camera looks down local -Z).
            Matrix w = Matrix.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(world)) * Matrix.CreateTranslation(world.Translation);
            var dto = new CameraDto
            {
                Name = name,
                World = Mat(w),
                Position = Vec(world.Translation),
                Fov = F(p, "field of view", 60f),
                Near = F(p, "near clip plane", 0.1f),
                Far = F(p, "far clip plane", 300f),
                BackgroundColor = Has(p, "m_BackGroundColor") ? Rgba(p.GetProperty("m_BackGroundColor")) : null,
                CullingMask = Has(p, "m_CullingMask") ? (int)F(p.GetProperty("m_CullingMask"), "m_Bits") : -1
            };
            if (name == "Main Camera")
            {
                Scene.Camera = dto;
                ctx.MainCameraWorld = w;
                ctx.HaveMainCamera = true;
            }
            else if (name == "WeaponCamera")
            {
                Scene.WeaponCamera = dto;
            }
        }

        private static void AddModelInstance(string name, JsonElement p, Matrix world, bool active)
        {
            string path = p.GetProperty("model").GetProperty("path").GetString();
            string model = AssetName(path);
            string file = Path.GetFileNameWithoutExtension(path);
            string kind = file == "Mesh_Health_Pickup" || file == "Mesh_Jetpack" ? "pickup" : file == "Mesh_Sun" ? "sun" : null;
            if (kind == null || model == null)
            {
                return;   // skinned characters (HoverBot, Turret) are handled as enemies, later phases
            }
            int[] mats = Array.Empty<int>();
            int flags = InstanceFlags.CastShadows | InstanceFlags.ReceiveShadows;
            if (p.TryGetProperty("nodeOverrides", out JsonElement ov))
            {
                foreach (JsonElement o in ov.EnumerateArray())
                {
                    if (o.GetProperty("class").GetString() != "MeshRenderer" || !o.TryGetProperty("overrides", out JsonElement ovr))
                    {
                        continue;
                    }
                    if (ovr.TryGetProperty("m_Materials", out JsonElement mm) && mm.TryGetProperty("Array", out JsonElement arr))
                    {
                        var list = new List<int>();
                        foreach (JsonProperty dp in arr.EnumerateObject())
                        {
                            list.Add(MaterialIndex(dp.Value));
                        }
                        mats = list.ToArray();
                    }
                    if (ovr.TryGetProperty("m_CastShadows", out JsonElement cs) && cs.GetDouble() == 0)
                    {
                        flags &= ~InstanceFlags.CastShadows;
                    }
                    if (ovr.TryGetProperty("m_ReceiveShadows", out JsonElement rs) && rs.GetDouble() == 0)
                    {
                        flags &= ~InstanceFlags.ReceiveShadows;
                    }
                }
            }
            Scene.ModelInstances.Add(new ModelInstanceDto
            {
                Name = name,
                Kind = kind,
                Model = model,
                Mats = mats,
                M = Mat(world),
                Flags = flags,
                Render = kind != "sun" && active   // Sun: a 100 m disk that follows the player, handled by the renderer/skybox phase
            });
        }

        private static readonly HashSet<string> EnemyPartScripts = new HashSet<string> { "DetectionModule", "WeaponController", "NavigationModule", "ProjectileStandard" };

        /// <summary>
        /// What the AI needs from the enemy's children and native components: the NavMeshAgent values, the scripts that live on child
        /// GameObjects (DetectionModule, the weapon) and the root-space position of every child GameObject by name (MonoGame space).
        /// The Transform references in the scripts were resolved from the raw prefab YAML: AimPoint / DeathVfxSpawnPoint = HitBox,
        /// DetectionSourcePoint = WeaponRoot, WeaponMuzzle = GunMuzzle, HealthBarPivot = HealthBarPivot.
        /// </summary>
        private static void BakeEnemyParts(EnemyDto dto, JsonElement go, List<JsonElement> comps)
        {
            dto.Parts = new Dictionary<string, JsonElement>();
            dto.Nodes = new Dictionary<string, float[]>();
            dto.Hierarchy = new Dictionary<string, NodeDto>();
            foreach (JsonElement c in comps)
            {
                if (c.GetProperty("type").GetString() == "NavMeshAgent")
                {
                    dto.Parts["NavMeshAgent"] = Prop(c).Clone();
                }
            }
            WalkEnemyChildren(go, Matrix.Identity, dto, null);
        }

        private static void WalkEnemyChildren(JsonElement go, Matrix parentRel, EnemyDto dto, string parentName)
        {
            if (!go.TryGetProperty("children", out JsonElement kids))
            {
                return;
            }
            foreach (JsonElement child in kids.EnumerateArray())
            {
                JsonElement t = child.GetProperty("transform");
                float[] q = Arr(t.GetProperty("localRotation"));
                Matrix rel = UnityConvert.TrsToMatrix(V3a(t.GetProperty("localPosition")), new Quaternion(q[0], q[1], q[2], q[3]), V3a(t.GetProperty("localScale"))) * parentRel;
                string name = child.GetProperty("name").GetString();
                if (!dto.Nodes.ContainsKey(name))
                {
                    dto.Nodes[name] = Vec(rel.Translation);
                    // parent link and the LOCAL matrix (MonoGame space): needed where a node hangs off an animated bone (the Turret's weapon under IrisBlade_C_Jnt).
                    // Model placeholder nodes (bones / mesh nodes) have identity locals in the extractor: their entries are only names, the skeleton gives their pose.
                    bool modelNode = false;
                    foreach (JsonElement mc in ComponentList(child))
                    {
                        string mt = mc.GetProperty("type").GetString();
                        modelNode |= mt == "ModelNode" || mt == "ModelInstance";
                    }
                    dto.Hierarchy[name] = new NodeDto
                    {
                        Parent = parentName,
                        Local = Mat(UnityConvert.TrsToMatrix(V3a(t.GetProperty("localPosition")), new Quaternion(q[0], q[1], q[2], q[3]), V3a(t.GetProperty("localScale")))),
                        ModelNode = modelNode
                    };
                }
                foreach (KeyValuePair<string, JsonElement> kv in ScriptFields(ComponentList(child)))
                {
                    if (EnemyPartScripts.Contains(kv.Key) && !dto.Parts.ContainsKey(kv.Key))
                    {
                        dto.Parts[kv.Key] = kv.Value;
                    }
                }
                WalkEnemyChildren(child, rel, dto, name);
            }
        }

        private static Dictionary<string, JsonElement> ScriptFields(List<JsonElement> comps)
        {
            var d = new Dictionary<string, JsonElement>();
            foreach (JsonElement c in comps)
            {
                if (c.GetProperty("type").GetString() == "MonoBehaviour" && c.TryGetProperty("fields", out JsonElement f))
                {
                    string s = c.GetProperty("script").GetString();
                    if (!d.ContainsKey(s))
                    {
                        d[s] = f.Clone();
                    }
                }
            }
            return d;
        }

        private static void HandleScript(JsonElement go, string name, JsonElement c, List<JsonElement> comps, Matrix world, bool active, WalkContext ctx)
        {
            string script = c.GetProperty("script").GetString();
            JsonElement fields = c.GetProperty("fields");
            switch (script)
            {
                case "EnemyController":
                    {
                        Dictionary<string, JsonElement> all = ScriptFields(comps);
                        string kind = all.ContainsKey("EnemyTurret") ? "Turret" : "HoverBot";
                        float maxHealth = all.TryGetValue("Health", out JsonElement h) ? F(h, "MaxHealth", 100f) : 100f;
                        EnemyByControllerId[c.GetProperty("id").GetString()] = Scene.Enemies.Count;
                        Scene.Enemies.Add(new EnemyDto
                        {
                            Name = name,
                            Kind = kind,
                            Prefab = go.TryGetProperty("prefab", out JsonElement pf) && pf.ValueKind == JsonValueKind.String ? pf.GetString() : null,
                            M = Mat(world),
                            Position = Vec(world.Translation),
                            MaxHealth = maxHealth,
                            Components = all
                        });
                        BakeEnemyParts(Scene.Enemies[Scene.Enemies.Count - 1], go, comps);
                    }
                    break;
                case "HealthPickup":
                case "JetpackPickup":
                    {
                        Vector3 tc = Vector3.Zero, ts = Vector3.One;
                        foreach (JsonElement cc in comps)
                        {
                            if (cc.GetProperty("type").GetString() == "BoxCollider")
                            {
                                JsonElement bp = Prop(cc);
                                tc = UnityConvert.Position(V3(bp.GetProperty("m_Center")));
                                ts = V3(bp.GetProperty("m_Size"));
                            }
                        }
                        Scene.Pickups.Add(new PickupDto
                        {
                            Name = name,
                            Kind = script == "JetpackPickup" ? "Jetpack" : "Health",
                            M = Mat(world),
                            Position = Vec(world.Translation),
                            HealAmount = script == "JetpackPickup" ? 0f : F(fields, "HealAmount", 40f),
                            TriggerCenter = Vec(tc),
                            TriggerSize = Vec(ts),
                            Components = ScriptFields(comps)
                        });
                    }
                    break;
                case "PatrolPath":
                    {
                        var path = new PatrolPathDto { Name = name };
                        int index = Scene.PatrolPaths.Count;
                        foreach (JsonElement child in go.GetProperty("children").EnumerateArray())
                        {
                            // children of the PatrolPath object are its nodes, in PathNodes order
                            JsonElement t = child.GetProperty("transform");
                            Matrix cw = NodeWorld(child, world);
                            path.Points.Add(Vec(cw.Translation));
                            path.PointNames.Add(child.GetProperty("name").GetString());
                        }
                        Scene.PatrolPaths.Add(path);
                        ctx.PatrolFields.Add((index, fields.Clone()));
                    }
                    break;
                case "ObjectiveKillEnemies":
                    Scene.Objectives.Add(new ObjectiveDto { Name = name, Kind = script, Position = Vec(world.Translation), Fields = ToDict(fields) });
                    break;
                case "PlayerCharacterController":
                    {
                        PlayerDto p = Scene.Player;
                        p.Name = name;
                        p.M = Mat(world);
                        p.Position = Vec(world.Translation);
                        JsonElement t = go.GetProperty("transform");
                        float[] q = Arr(t.GetProperty("worldRotation"));
                        Vector3 fwd = Vector3.Transform(Vector3.UnitZ, new Quaternion(q[0], q[1], q[2], q[3]));   // Unity +Z in Unity space (math is handedness-agnostic)
                        p.YawDegrees = MathHelper.ToDegrees((float)Math.Atan2(fwd.X, fwd.Z));
                        ctx.PlayerUnityPos = Arr(t.GetProperty("worldPosition"));
                        p.Components = ScriptFields(comps);
                        foreach (JsonElement cc in comps)
                        {
                            if (cc.GetProperty("type").GetString() == "CharacterController")
                            {
                                JsonElement cp = Prop(cc);
                                p.CharacterHeight = F(cp, "m_Height");
                                p.CharacterRadius = F(cp, "m_Radius");
                                p.SlopeLimit = F(cp, "m_SlopeLimit");
                                p.StepOffset = F(cp, "m_StepOffset");
                                p.SkinWidth = F(cp, "m_SkinWidth");
                                p.CharacterCenter = Vec(UnityConvert.Position(V3(cp.GetProperty("m_Center"))));
                            }
                        }
                        foreach (JsonElement child in go.GetProperty("children").EnumerateArray())
                        {
                            if (child.GetProperty("name").GetString() == "Main Camera")
                            {
                                p.CameraHeight = Arr(child.GetProperty("transform").GetProperty("localPosition"))[1];
                                // DefaultWeaponPosition lives under Main Camera/FirstPersonSocket
                                FindDefaultWeapon(child, NodeWorld(child, world), ctx);
                            }
                        }
                    }
                    break;
            }
        }

        private static void FindDefaultWeapon(JsonElement go, Matrix world, WalkContext ctx)
        {
            foreach (JsonElement child in go.GetProperty("children").EnumerateArray())
            {
                Matrix cw = NodeWorld(child, world);
                if (child.GetProperty("name").GetString() == "DefaultWeaponPosition")
                {
                    ctx.DefaultWeaponWorld = cw;
                    ctx.HaveDefaultWeapon = true;
                }
                else if (child.GetProperty("name").GetString() == "AimingWeaponPosition")
                {
                    ctx.AimingWeaponWorld = cw;
                    ctx.HaveAimingWeapon = true;
                }
                else if (child.GetProperty("name").GetString() == "DownWeaponPosition")
                {
                    ctx.DownWeaponWorld = cw;
                    ctx.HaveDownWeapon = true;
                }
                FindDefaultWeapon(child, cw, ctx);
            }
        }

        private static Dictionary<string, JsonElement> ToDict(JsonElement obj)
        {
            var d = new Dictionary<string, JsonElement>();
            foreach (JsonProperty p in obj.EnumerateObject())
            {
                d[p.Name] = p.Value.Clone();
            }
            return d;
        }

        private static void ResolvePatrolPaths(WalkContext ctx)
        {
            foreach ((int pathIndex, JsonElement fields) in ctx.PatrolFields)
            {
                if (!fields.TryGetProperty("EnemiesToAssign", out JsonElement list))
                {
                    continue;
                }
                foreach (JsonElement e in list.EnumerateArray())
                {
                    if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty("ref", out JsonElement r) && EnemyByControllerId.TryGetValue(r.GetString(), out int enemy))
                    {
                        Scene.Enemies[enemy].PatrolPath = pathIndex;
                        Scene.PatrolPaths[pathIndex].Enemies.Add(Scene.Enemies[enemy].Name);
                    }
                    else
                    {
                        Warnings.Add("patrol path enemy ref unresolved: " + e);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ lighting

        private static void BakeLighting(JsonElement rs, WalkContext ctx)
        {
            LightingDto l = Scene.Lighting;
            if (ctx.HaveLight)
            {
                Vector3 dir = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0, 0, -1), ctx.LightWorld));   // Unity forward (+Z) -> MonoGame -Z of the converted frame
                l.SunDirection = Vec(dir);
                l.SunColor = Rgba(ctx.Light.GetProperty("m_Color"))[0..3];
                l.SunIntensity = F(ctx.Light, "m_Intensity", 1f);
                JsonElement sh = ctx.Light.GetProperty("m_Shadows");
                l.SunShadows = F(sh, "m_Type") != 0f;
                l.ShadowStrength = F(sh, "m_Strength", 1f);
            }
            l.AmbientMode = (int)F(rs, "m_AmbientMode");
            l.AmbientColor = Rgba(rs.GetProperty("m_AmbientSkyColor"))[0..3];
            l.AmbientIntensity = F(rs, "m_AmbientIntensity", 1f);
            l.Fog = F(rs, "m_Fog") != 0f;
            l.FogColor = Rgba(rs.GetProperty("m_FogColor"));
            l.FogDensity = F(rs, "m_FogDensity");
            l.FogMode = (int)F(rs, "m_FogMode");
            if (Has(rs, "m_SkyboxMaterial"))
            {
                int sky = MaterialIndex(rs.GetProperty("m_SkyboxMaterial"));
                if (sky >= 0)
                {
                    MaterialDto sm = Scene.Materials[sky];
                    l.SkyboxMaterial = sm.Name;
                    l.SkyboxTexture = sm.BaseMap;
                    l.SkyExposure = sm.Floats.TryGetValue("_Exposure", out float ex) ? ex : 1f;
                    l.SkyRotation = sm.Floats.TryGetValue("_Rotation", out float ro) ? ro : 0f;
                    l.SkyTint = sm.Vectors.TryGetValue("_Tint", out float[] tint) ? tint : new float[] { 1, 1, 1, 0.5f };
                }
            }
            string lightingJson = Path.Combine(_root, "Tools", "UnityReference", "Baseline", "lighting.json");
            if (File.Exists(lightingJson))
            {
                using JsonDocument ld = JsonDocument.Parse(File.ReadAllBytes(lightingJson));
                if (ld.RootElement.TryGetProperty("urpAsset", out JsonElement urp))
                {
                    l.ShadowDistance = F(urp, "shadowDistance", 50f);
                    l.ShadowBias = F(urp, "shadowDepthBias", 1f);
                    l.ShadowNormalBias = F(urp, "shadowNormalBias", 1f);
                }
            }
        }

        // ------------------------------------------------------------------ weapon viewmodel

        private static void BakeWeapon(JsonElement prefab, WalkContext ctx)
        {
            var weapon = new WeaponDto { Name = "Weapon_Blaster" };
            JsonElement root = default;
            foreach (JsonElement r in prefab.GetProperty("roots").EnumerateArray())
            {
                root = r;
                break;
            }
            weapon.Components = ScriptFields(ComponentList(root));
            // WeaponFuelCellHandler.FuelCells are GameObject references: resolve them to the piece (GameObject) names
            if (weapon.Components.TryGetValue("WeaponFuelCellHandler", out JsonElement fuel) && fuel.TryGetProperty("FuelCells", out JsonElement cells) && cells.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement cell in cells.EnumerateArray())
                {
                    string id = cell.TryGetProperty("ref", out JsonElement r) ? r.GetString() : null;
                    string found = id == null ? null : FindNameById(root, id);
                    if (found == null) Warnings.Add("fuel cell reference " + id + " not found in the weapon prefab");
                    else weapon.FuelCellPieces.Add(found);
                }
            }
            // the weapon's AudioSource (volume of the shot / weapon-change one-shots) is stored under its own name
            foreach (JsonElement c in ComponentList(root))
            {
                if (c.GetProperty("type").GetString() == "AudioSource")
                {
                    weapon.Components["AudioSource"] = Prop(c).Clone();
                    break;
                }
            }
            WalkWeapon(root, Matrix.Identity, weapon);

            if (ctx.HaveDefaultWeapon && ctx.HaveMainCamera)
            {
                // Weapon root (prefab origin) sits at the DefaultWeaponPosition socket, which has the camera's rotation.
                weapon.RootInCamera = Mat(ctx.DefaultWeaponWorld * Matrix.Invert(ctx.MainCameraWorld));
                if (ctx.HaveAimingWeapon) weapon.AimingInCamera = Mat(ctx.AimingWeaponWorld * Matrix.Invert(ctx.MainCameraWorld));
                if (ctx.HaveDownWeapon) weapon.DownInCamera = Mat(ctx.DownWeaponWorld * Matrix.Invert(ctx.MainCameraWorld));
            }
            else
            {
                Warnings.Add("DefaultWeaponPosition / Main Camera not found; weapon pose missing");
            }
            Scene.Weapon = weapon;
        }

        private static WeaponDto BakeProjectile(JsonElement prefab, string name)
        {
            var projectile = new WeaponDto { Name = name };
            foreach (JsonElement r in prefab.GetProperty("roots").EnumerateArray())
            {
                projectile.Components = ScriptFields(ComponentList(r));
                WalkWeapon(r, Matrix.Identity, projectile);
                break;
            }
            return projectile;
        }

        private static string FindNameById(JsonElement go, string id)
        {
            if (go.GetProperty("id").GetString().EndsWith(id)) return go.GetProperty("name").GetString();
            if (go.TryGetProperty("children", out JsonElement kids))
            {
                foreach (JsonElement k in kids.EnumerateArray())
                {
                    string n = FindNameById(k, id);
                    if (n != null) return n;
                }
            }
            return null;
        }

        private static List<JsonElement> ComponentList(JsonElement go)
        {
            var l = new List<JsonElement>();
            foreach (JsonElement c in go.GetProperty("components").EnumerateArray())
            {
                l.Add(c);
            }
            return l;
        }

        private static void WalkWeapon(JsonElement go, Matrix parentWorld, WeaponDto weapon)
        {
            Matrix world = NodeWorld(go, parentWorld);   // prefab space; extractor worlds agree with composed locals
            string name = go.GetProperty("name").GetString();
            if (name == "GunMuzzle")
            {
                weapon.MuzzleInRoot = Vec(world.Translation);
            }
            if (name == "Tip")
            {
                weapon.TipInRoot = Vec(world.Translation);
            }
            bool haveMf = false, haveMr = false;
            JsonElement mf = default, mr = default;
            foreach (JsonElement c in go.GetProperty("components").EnumerateArray())
            {
                string t = c.GetProperty("type").GetString();
                if (t == "MeshFilter") { mf = Prop(c); haveMf = true; }
                else if (t == "MeshRenderer") { mr = Prop(c); haveMr = true; }
            }
            if (go.GetProperty("active").GetBoolean() && haveMf && haveMr && Has(mf, "m_Mesh") && F(mr, "m_Enabled", 1f) != 0f)
            {
                JsonElement mesh = mf.GetProperty("m_Mesh");
                int meshRef = MeshRef(mesh.GetProperty("path").GetString(), mesh.TryGetProperty("name", out JsonElement mn) ? mn.GetString() : null);
                if (meshRef >= 0)
                {
                    weapon.Pieces.Add(new WeaponPieceDto { Name = name, Mesh = meshRef, Mats = MaterialIndices(mr.GetProperty("m_Materials")), M = Mat(world) });
                }
            }
            foreach (JsonElement child in go.GetProperty("children").EnumerateArray())
            {
                WalkWeapon(child, world, weapon);
            }
        }
    }
}
