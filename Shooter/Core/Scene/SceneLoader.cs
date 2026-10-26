using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Animation;
using Shooter.Physics;
using Shooter.Rendering;
using RMaterial = Shooter.Rendering.Material;

namespace Shooter.Scene
{
    public class SceneLoadOptions
    {
        /// <summary>
        /// Material matching for model parts. Default (false): by NAME - ModelMeshPart.Tag (the OBJ/FBX material name, e.g.
        /// "Floor_Grey") is looked up among the Unity renderer's m_Materials by material file name; parts whose tag is not found
        /// fall back to the part's ordinal (= Unity submesh index). True: ordinal only (Unity submesh order == m_Materials order).
        /// This only matters for OBJBasic_Floor, whose first-use order (Floor_Grey, Floor_DarkGrey) differs from the prefab list
        /// ([Floor_DarkGrey, Floor_Grey]); see ASSET-CONVENTIONS.md "OPEN QUESTION".
        /// </summary>
        public bool MatchMaterialsByIndex;

        /// <summary>Draw each HoverBot at its scene transform in the bind pose (visual comparison only).</summary>
        public bool DrawStaticEnemies = true;
        public string ScenePath = "Content/Scenes/MainScene.scene.json";

        /// <summary>Build the player collision world from the baked colliders (reads vertex/index buffers back from the GPU).</summary>
        public bool BuildCollision = true;
    }

    /// <summary>Everything the debug/game layer needs from MainScene, in renderer-ready form.</summary>
    public class HitboxInfo
    {
        public int Tag;
        public int EnemyIndex;
        public string Name;
        public float DamageMultiplier = 1f;
        public float SensibilityToSelfdamage = 0.5f;
    }

    /// <summary>A render item that moves at run time (pickups): drawn when <see cref="Active"/>, culled with its current <see cref="Bounds"/>.</summary>
    public sealed class DynamicItem
    {
        public RenderItem Item;
        public BoundingBox Bounds;
        public bool Active = true;

        public void SetWorld(Matrix world)
        {
            Item.World = world;
            Bounds = SceneLoader.TransformBounds(Item.Mesh.Bounds, world);
        }
    }

    public class LoadedScene
    {
        public SceneFile Data;
        public SceneLighting Lighting;
        public Camera Camera;
        public Camera WeaponCamera;
        public PostProcessSettings Post = new PostProcessSettings();

        /// <summary>All static level items (+ pickup meshes). Parallel to WorldBounds.</summary>
        public List<RenderItem> Items = new List<RenderItem>();
        public List<BoundingBox> WorldBounds = new List<BoundingBox>();

        /// <summary>Pickup meshes (they bob and spin, so they are not in <see cref="Items"/>); <see cref="PickupItemIndex"/> maps each PickupDto to its entry.</summary>
        public List<DynamicItem> DynamicItems = new List<DynamicItem>();
        public List<int> PickupItemIndex = new List<int>();
        /// <summary>Pose of each pickup's mesh relative to its root (Pickup_Health has scale 2, Pickup_Jetpack scale 1), parallel to <see cref="PickupItemIndex"/>.</summary>
        public List<Matrix> PickupMeshRelatives = new List<Matrix>();
        /// <summary>DynamicItems made at load; everything after it is loot spawned during play.</summary>
        public int BakedDynamicCount;
        /// <summary>The health pickup mesh item (world = the baked placement of the first pickup) and its pose relative to the pickup root (scale 2 included), for loot spawned later.</summary>
        public RenderItem? PickupTemplate;
        public Matrix PickupMeshRelative = Matrix.Identity;

        /// <summary>Weapon viewmodel pieces: World is relative to the WEAPON ROOT; see BuildWeaponItems.</summary>
        public List<RenderItem> WeaponPieces = new List<RenderItem>();
        public List<string> WeaponPieceNames = new List<string>();   // parallel to WeaponPieces
        /// <summary>Per-piece translation in weapon-root space added when the viewmodel is built (fuel cells sliding); null = none. Parallel to WeaponPieces.</summary>
        public Vector3[] WeaponPieceOffsets;
        public Matrix WeaponRootInCamera = Matrix.Identity;
        /// <summary>Projectile mesh pieces relative to the projectile root (Projectile_Blaster prefab).</summary>
        public List<RenderItem> ProjectilePieces = new List<RenderItem>();

        public int MaterialsInstantiated;
        public int TexturesLoaded;
        public int RenderMeshes;
        public int DistinctVertexBuffers;
        public int NegativeDeterminantItems;
        public long LoadMilliseconds;
        public long ParseMilliseconds;

        public Matrix StartCameraWorld;

        /// <summary>Static collision geometry for the player controller (null when SceneLoadOptions.BuildCollision is false).</summary>
        public CollisionWorld Collision;
        public string CollisionSummary;
        /// <summary>Enemy hitboxes found among the colliders: the collision tag, the enemy (index into Data.Enemies) and the Damageable multiplier.</summary>
        public List<HitboxInfo> Hitboxes = new List<HitboxInfo>();
        /// <summary>The static render item drawn for each enemy (parallel to Data.Enemies); removed from Items when the enemy dies.</summary>
        public RenderItem?[] EnemyItems;
        /// <summary>The animated view of each enemy that has one (parallel to Data.Enemies; null = drawn statically).</summary>
        public EnemyRig[] EnemyViews;
        /// <summary>Projectile_Hoverbot pieces relative to the projectile root.</summary>
        public List<RenderItem> EnemyProjectilePieces = new List<RenderItem>();

        /// <summary>
        /// Builds the weapon items for the given main-camera world matrix: world = piece * rootInCamera * cameraWorld
        /// (the viewmodel is rigidly attached to the camera, rendered by the weapon camera with layer Weapon).
        /// </summary>
        public void BuildWeaponItems(Matrix cameraWorld, List<RenderItem> output)
        {
            Matrix root = WeaponRootInCamera * cameraWorld;
            AddWeaponPieces(root, output);
        }

        /// <summary>The viewmodel with the PlayerWeaponsManager socket (bob / recoil / aim / switch) as a camera-space offset instead of the baked Default position.</summary>
        public void BuildWeaponItems(Matrix cameraWorld, Vector3 socketLocal, List<RenderItem> output)
        {
            Matrix root = Matrix.CreateTranslation(socketLocal) * cameraWorld;
            AddWeaponPieces(root, output);
        }

        private void AddWeaponPieces(Matrix root, List<RenderItem> output)
        {
            for (int i = 0; i < WeaponPieces.Count; i++)
            {
                RenderItem item = WeaponPieces[i];
                Matrix local = item.World;
                if (WeaponPieceOffsets != null && WeaponPieceOffsets[i] != Vector3.Zero)
                {
                    local *= Matrix.CreateTranslation(WeaponPieceOffsets[i]);
                }
                item.World = local * root;
                output.Add(item);
            }
        }

        /// <summary>One item per projectile piece, placed at <paramref name="position"/> facing <paramref name="forward"/>.</summary>
        public void BuildProjectileItems(Vector3 position, Vector3 forward, List<RenderItem> output)
        {
            Matrix root = Matrix.CreateWorld(position, forward, Vector3.Up);
            foreach (RenderItem piece in ProjectilePieces)
            {
                RenderItem item = piece;
                item.World = piece.World * root;
                output.Add(item);
            }
        }

        /// <summary>Moves an enemy's render item (the enemy walked or turned).</summary>
        public void SetEnemyWorld(int enemyIndex, Matrix world)
        {
            if (EnemyItems == null || EnemyItems[enemyIndex] == null)
            {
                return;
            }
            RenderItem old = EnemyItems[enemyIndex].Value;
            RenderItem item = old;
            item.World = world;
            for (int i = 0; i < Items.Count; i++)
            {
                if (ReferenceEquals(Items[i].Mesh, old.Mesh) && Items[i].World == old.World)
                {
                    Items[i] = item;
                    WorldBounds[i] = BoundsOf(item.Mesh.Bounds, world);
                    EnemyItems[enemyIndex] = item;
                    return;
                }
            }
            // hidden (destroyed) or not found: remember the pose for RestoreEnemyItem
            EnemyItems[enemyIndex] = item;
            if (_hiddenEnemyBounds.ContainsKey(enemyIndex))
            {
                _hiddenEnemyBounds[enemyIndex] = BoundsOf(item.Mesh.Bounds, world);
            }
        }

        private static BoundingBox BoundsOf(BoundingBox b, Matrix m)
        {
            Vector3[] corners = b.GetCorners();
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector3.Transform(corners[i], m);
            }
            return BoundingBox.CreateFromPoints(corners);
        }

        /// <summary>One item per piece of Projectile_Hoverbot, placed at <paramref name="position"/> facing <paramref name="forward"/>.</summary>
        public void BuildEnemyProjectileItems(Vector3 position, Vector3 forward, List<RenderItem> output)
        {
            Matrix root = Matrix.CreateWorld(position, forward, Vector3.Up);
            foreach (RenderItem piece in EnemyProjectilePieces)
            {
                RenderItem item = piece;
                item.World = piece.World * root;
                output.Add(item);
            }
        }

        private readonly Dictionary<int, BoundingBox> _hiddenEnemyBounds = new Dictionary<int, BoundingBox>();

        /// <summary>Takes an enemy's render item out of the scene (it was destroyed); <see cref="RestoreEnemyItem"/> brings it back.</summary>
        public void RemoveEnemyItem(int enemyIndex)
        {
            if (EnemyItems == null || EnemyItems[enemyIndex] == null)
            {
                return;
            }
            RenderItem item = EnemyItems[enemyIndex].Value;
            for (int i = 0; i < Items.Count; i++)
            {
                if (ReferenceEquals(Items[i].Mesh, item.Mesh) && Items[i].World == item.World)
                {
                    _hiddenEnemyBounds[enemyIndex] = WorldBounds[i];
                    Items.RemoveAt(i);
                    WorldBounds.RemoveAt(i);
                    return;
                }
            }
        }

        public void RestoreEnemyItem(int enemyIndex)
        {
            if (EnemyItems != null && EnemyItems[enemyIndex] != null && _hiddenEnemyBounds.TryGetValue(enemyIndex, out BoundingBox b))
            {
                _hiddenEnemyBounds.Remove(enemyIndex);
                Items.Add(EnemyItems[enemyIndex].Value);
                WorldBounds.Add(b);
            }
        }

        /// <summary>
        /// Items that can influence the picture: visible in the camera frustum, or close enough to cast shadows into it
        /// (within <paramref name="shadowRange"/> of the camera, the shadow distance of the sun cascade).
        /// </summary>
        public void Cull(BoundingFrustum frustum, Vector3 cameraPosition, float shadowRange, List<RenderItem> output)
        {
            var sphere = new BoundingSphere(cameraPosition, shadowRange);
            for (int i = 0; i < Items.Count; i++)
            {
                BoundingBox b = WorldBounds[i];
                if (frustum.Intersects(b) || (Items[i].CastShadows && sphere.Intersects(b)))
                {
                    output.Add(Items[i]);
                }
            }
            AddDynamic(output, frustum, sphere);
        }

        /// <summary>Active moving items (pickups) in view; <paramref name="frustum"/> null adds all of them.</summary>
        public void AddDynamic(List<RenderItem> output, BoundingFrustum frustum, BoundingSphere sphere)
        {
            for (int i = 0; i < DynamicItems.Count; i++)
            {
                DynamicItem d = DynamicItems[i];
                if (d.Active && (frustum == null || frustum.Intersects(d.Bounds) || (d.Item.CastShadows && sphere.Intersects(d.Bounds))))
                {
                    output.Add(d.Item);
                }
            }
        }
    }

    /// <summary>
    /// Loads Content/Scenes/MainScene.scene.json (baked by Tools/SceneBake) plus the .xnb models and textures through
    /// the ContentManager and produces the Shooter.Rendering contract objects.
    /// </summary>
    public class SceneLoader
    {
        private readonly GraphicsDevice _device;
        private readonly ContentManager _content;
        private readonly SceneLoadOptions _options;
        private SceneFile _file;
        private LoadedScene _scene;

        private readonly Dictionary<string, Model> _models = new Dictionary<string, Model>();
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private RMaterial[] _materials;
        private readonly Dictionary<string, RenderMesh> _renderMeshes = new Dictionary<string, RenderMesh>();
        private readonly Dictionary<string, BoundingBox> _meshBounds = new Dictionary<string, BoundingBox>();
        private readonly HashSet<VertexBuffer> _vertexBuffers = new HashSet<VertexBuffer>();
        private readonly Dictionary<string, int> _missingTags = new Dictionary<string, int>();
        private RMaterial _fallback;

        public SceneLoader(GraphicsDevice device, ContentManager content, SceneLoadOptions options = null)
        {
            _device = device;
            _content = content;
            _options = options ?? new SceneLoadOptions();
        }

        public static SceneFile ReadSceneFile(string path)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            using (Stream s = TitleContainer.OpenStream(path))
            {
                return JsonSerializer.Deserialize<SceneFile>(s, options);
            }
        }

        public LoadedScene Load()
        {
            var sw = Stopwatch.StartNew();
            _file = ReadSceneFile(_options.ScenePath);
            _scene = new LoadedScene { Data = _file, ParseMilliseconds = sw.ElapsedMilliseconds };
            _materials = new RMaterial[_file.Materials.Count];
            _fallback = new RMaterial { Name = "<missing>", BaseColor = new Vector4(1f, 0f, 1f, 1f) };

            // Level meshes.
            foreach (InstanceDto inst in _file.Instances)
            {
                RenderMesh mesh = GetRenderMesh(inst.Mesh, inst.Mats);
                if (mesh == null)
                {
                    continue;
                }
                AddItem(mesh, ToMatrix(inst.M), inst.Flags, RenderLayer.Default, null);
            }

            // Prefab-placed model assets (pickups). The sun disk is skipped (Render == false).
            foreach (ModelInstanceDto mi in _file.ModelInstances)
            {
                if (!mi.Render)
                {
                    continue;
                }
                RenderMesh mesh = GetRenderMesh(mi.Model, null, mi.Mats);
                if (mesh != null)
                {
                    if (mi.Kind == "pickup")
                    {
                        AddPickupItem(mesh, mi);
                    }
                    else
                    {
                        AddItem(mesh, ToMatrix(mi.M), mi.Flags, RenderLayer.Default, null);
                    }
                }
            }
            MatchPickups();
            _scene.BakedDynamicCount = _scene.DynamicItems.Count;

            // Enemies: static first-frame-of-Idle preview only (no AI, animation or NavMesh yet).
            if (_options.DrawStaticEnemies)
            {
                AddStaticEnemies();
            }

            // Weapon viewmodel.
            if (_file.Weapon != null)
            {
                foreach (WeaponPieceDto piece in _file.Weapon.Pieces)
                {
                    RenderMesh mesh = GetRenderMesh(piece.Mesh, piece.Mats);
                    if (mesh != null)
                    {
                        _scene.WeaponPieceNames.Add(piece.Name);
                        _scene.WeaponPieces.Add(new RenderItem
                        {
                            Mesh = mesh,
                            World = ToMatrix(piece.M),
                            CastShadows = false,
                            ReceiveShadows = false,
                            Layer = RenderLayer.Weapon
                        });
                    }
                }
                if (_file.Weapon.RootInCamera != null)
                {
                    _scene.WeaponRootInCamera = ToMatrix(_file.Weapon.RootInCamera);
                }
            }

            if (_options.BuildCollision)
            {
                _scene.Collision = BuildCollision();
            }

            if (_file.Projectile != null)
            {
                foreach (WeaponPieceDto piece in _file.Projectile.Pieces)
                {
                    RenderMesh mesh = GetRenderMesh(piece.Mesh, piece.Mats);
                    if (mesh != null)
                    {
                        _scene.ProjectilePieces.Add(new RenderItem { Mesh = mesh, World = ToMatrix(piece.M), CastShadows = false, ReceiveShadows = false, Layer = RenderLayer.Default });
                    }
                }
            }

            if (_file.EnemyProjectile != null)
            {
                foreach (WeaponPieceDto piece in _file.EnemyProjectile.Pieces)
                {
                    RenderMesh mesh = GetRenderMesh(piece.Mesh, piece.Mats);
                    if (mesh != null)
                    {
                        _scene.EnemyProjectilePieces.Add(new RenderItem { Mesh = mesh, World = ToMatrix(piece.M), CastShadows = false, ReceiveShadows = false, Layer = RenderLayer.Default });
                    }
                }
            }

            _scene.Lighting = BuildLighting();
            _scene.Camera = BuildCamera(_file.Camera, 16f / 9f);
            _scene.WeaponCamera = BuildCamera(_file.WeaponCamera, 16f / 9f);
            _scene.StartCameraWorld = ToMatrix(_file.Camera.World);
            _scene.RenderMeshes = _renderMeshes.Count;
            _scene.DistinctVertexBuffers = _vertexBuffers.Count;
            _scene.TexturesLoaded = _textures.Count;
            foreach (RMaterial m in _materials)
            {
                if (m != null)
                {
                    _scene.MaterialsInstantiated++;
                }
            }
            sw.Stop();
            _scene.LoadMilliseconds = sw.ElapsedMilliseconds;
            return _scene;
        }

        public IReadOnlyDictionary<string, int> MissingMaterialTags
        {
            get { return _missingTags; }
        }

        // ------------------------------------------------------------------ items

        // Seconds into the looping IdleClosed clip: the front panel is open and the eye visible (matches the Unity front capture).
        private const float IdlePreviewTime = 0.42f;

        /// <summary>One static-preview rig: skinned mesh with materials, and a skin palette from a single animation frame.</summary>
        private sealed class StaticRig
        {
            public RenderMesh Mesh;
            public Matrix[] Palette;
            public SkinnedModel Skinned;
            public AnimationPlayer Player;
            public RMaterial Eye, Body;
        }

        private StaticRig BuildStaticRig(EnemyDto e, string model, string clip, float clipTime, Func<string, string> unityMaterialOfPart, string eyeMaterial)
        {
            SkinnedModel skinned = _content.Load<SkinnedModel>(model);
            var rig = new StaticRig { Mesh = new RenderMesh { Name = e.Kind, Bounds = skinned.Bounds } };
            foreach (SkinnedMeshPart part in skinned.Parts)
            {
                int idx = _file.Materials.FindIndex(m => m.Name == unityMaterialOfPart(part.Name));
                if (idx >= 0 && _file.Materials[idx].Name == eyeMaterial.Replace("_Eye", "_Body") && rig.Body == null)
                {
                    rig.Body = GetMaterial(idx);   // BodyMaterial: the on-hit flash writes its _EmissionColor
                }
                if (idx >= 0 && _file.Materials[idx].Name == eyeMaterial && e.Components != null &&
                    e.Components.TryGetValue("EnemyController", out JsonElement ec) && ec.TryGetProperty("DefaultEyeColor", out JsonElement c))
                {
                    // EnemyController writes DefaultEyeColor into _EmissionColor through a MaterialPropertyBlock (overrides the material asset);
                    // Unity treats it as gamma -> linear.
                    var gamma = new Vector3((float)c.GetProperty("r").GetDouble(), (float)c.GetProperty("g").GetDouble(), (float)c.GetProperty("b").GetDouble());
                    GetMaterial(idx).EmissionColor = SceneRenderer.GammaToLinear(gamma);
                    rig.Eye = GetMaterial(idx);
                }
                rig.Mesh.Parts.Add(new MeshPart
                {
                    VertexBuffer = part.VertexBuffer,
                    IndexBuffer = part.IndexBuffer,
                    BaseVertex = 0,
                    StartIndex = 0,
                    PrimitiveCount = part.PrimitiveCount,
                    Material = idx >= 0 ? GetMaterial(idx) : _fallback
                });
                _vertexBuffers.Add(part.VertexBuffer);
            }
            rig.Palette = new Matrix[SkinnedModel.MaxBones];
            var player = new AnimationPlayer(skinned);
            player.Play(_content.Load<AnimationClip>(clip), 0f);
            player.Update(clipTime);
            player.Evaluate();
            player.CopySkinMatricesTo(rig.Palette);
            rig.Skinned = skinned;
            rig.Player = player;
            return rig;
        }

        /// <summary>An enemy's Animator (its controller exported to *.animator.json) over its skinned model; the default state plays from time 0.</summary>
        private EnemyRig BuildEnemyView(EnemyDto e, int enemyIndex, StaticRig rig, string animatorFile, Matrix initialModelWorld)
        {
            AnimatorControllerData data;
            try
            {
                using (Stream s = TitleContainer.OpenStream(animatorFile))
                using (var reader = new StreamReader(s))
                {
                    data = AnimatorControllerData.FromJson(reader.ReadToEnd());
                }
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Console.WriteLine("  note: " + animatorFile + " not found: the " + e.Kind + " is not animated");
                return null;
            }
            // the controller starts in its default state (Idle = IdleClosed) from time 0 on a fresh player over the same skeleton
            var player = new AnimationPlayer(rig.Skinned);
            var controller = new AnimatorController(data, player, name => _content.Load<AnimationClip>(name));
            controller.Update(0f);
            player.Evaluate();
            player.CopySkinMatricesTo(rig.Palette);
            var ec = e.Components["EnemyController"];
            Vector3 Eye(string n) { JsonElement c = ec.GetProperty(n); return new Vector3((float)c.GetProperty("r").GetDouble(), (float)c.GetProperty("g").GetDouble(), (float)c.GetProperty("b").GetDouble()); }
            var view = new EnemyRig(_scene, enemyIndex, controller, player, rig.Palette, rig.Eye, Eye("DefaultEyeColor"), Eye("AttackEyeColor"), rig.Body);
            view.SetInitialModelWorld(initialModelWorld);
            return view;
        }

        private void AddStaticEnemies()
        {
            StaticRig hoverBot = null, turret = null;
            const int flags = InstanceFlags.CastShadows | InstanceFlags.ReceiveShadows;
            _scene.EnemyItems = new RenderItem?[_file.Enemies.Count];
            _scene.EnemyViews = new EnemyRig[_file.Enemies.Count];
            for (int enemyIndex = 0; enemyIndex < _file.Enemies.Count; enemyIndex++)
            {
                EnemyDto e = _file.Enemies[enemyIndex];
                if (e.Kind == "HoverBot")
                {
                    // Parts (SKINNING-NOTES.md): Top, EyeBall_0 (Body), EyeBall_1 (Glass/lens), EyeBall_2 (Eye), Bottom (Body).
                    // Default animator state is Idle = IdleClosed; its looping frame is the static pose.
                    hoverBot = BuildStaticRig(e, "Characters/HoverBot", "Characters/HoverBot@IdleClosed", IdlePreviewTime,
                        p => p == "EyeBall_1" ? "HoverBot_Glass" : p == "EyeBall_2" ? "HoverBot_Eye" : "HoverBot_Body", "HoverBot_Eye");
                    // The baked transform is the editor pose (identity). In Play mode the bot sits at rotation Y -89.28 (Unity), seen in the
                    // Inspector captures; Unity -> MonoGame flips the Y rotation sign.
                    // The model child (BasicRobot) sits 0.768 above the root in Enemy_HoverBot.prefab; the hitbox (1.359), weapon root (1.347) and health bar pivot (2.101) are authored relative to it.
                    const float HoverBotModelOffsetY = 0.768f;
                    Matrix world = Matrix.CreateRotationY(MathHelper.ToRadians(89.28f)) * Matrix.CreateTranslation(ToMatrix(e.M).Translation + new Vector3(0f, HoverBotModelOffsetY, 0f));
                    _scene.EnemyItems[enemyIndex] = AddItem(hoverBot.Mesh, world, flags, RenderLayer.Default, hoverBot.Palette);
                    _scene.EnemyViews[enemyIndex] = BuildEnemyView(e, enemyIndex, hoverBot, "Content/Characters/HoverBot.animator.json", world);
                }
                else if (e.Kind == "Turret")
                {
                    // Parts: TurretBottom, TurretCenter (Body), TurretTop_0 (Body), TurretTop_1 (Glass), TurretTop_2 (Light = eye).
                    // The baked transform already carries the scene scale (4) and orientation.
                    turret = BuildStaticRig(e, "Characters/Turret", "Characters/Turret@Idle", 0f,
                        p => p == "TurretTop_1" ? "Turret_Glass" : p == "TurretTop_2" ? "Turret_Eye" : "Turret_Body", "Turret_Eye");
                    _scene.EnemyItems[enemyIndex] = AddItem(turret.Mesh, ToMatrix(e.M), flags, RenderLayer.Default, turret.Palette);
                    _scene.EnemyViews[enemyIndex] = BuildEnemyView(e, enemyIndex, turret, "Content/Characters/Turret.animator.json", ToMatrix(e.M));
                }
            }
        }

        private readonly List<ModelInstanceDto> _pickupModels = new List<ModelInstanceDto>();

        /// <summary>Pickup meshes move (bob, spin), so they live in <see cref="LoadedScene.DynamicItems"/> at their baked pose until the game animates them.</summary>
        private void AddPickupItem(RenderMesh mesh, ModelInstanceDto mi)
        {
            Matrix world = ToMatrix(mi.M);
            if (world.Determinant() < 0f)
            {
                _scene.NegativeDeterminantItems++;
            }
            var item = new RenderItem
            {
                Mesh = mesh,
                World = world,
                CastShadows = (mi.Flags & InstanceFlags.CastShadows) != 0,
                ReceiveShadows = (mi.Flags & InstanceFlags.ReceiveShadows) != 0,
                Layer = RenderLayer.Default
            };
            _scene.DynamicItems.Add(new DynamicItem { Item = item, Bounds = TransformBounds(mesh.Bounds, world) });
            _pickupModels.Add(mi);
        }

        /// <summary>PickupDto i -> its mesh (same x / z as the pickup root); the first one is kept as the template for loot.</summary>
        private void MatchPickups()
        {
            foreach (PickupDto pd in _file.Pickups)
            {
                int found = -1;
                for (int i = 0; i < _pickupModels.Count && found < 0; i++)
                {
                    Matrix m = ToMatrix(_pickupModels[i].M);
                    if (Math.Abs(m.Translation.X - pd.Position[0]) < 0.05f && Math.Abs(m.Translation.Z - pd.Position[2]) < 0.05f)
                    {
                        found = i;
                    }
                }
                _scene.PickupItemIndex.Add(found);
                Matrix relative = found >= 0 ? _scene.DynamicItems[found].Item.World * Matrix.CreateTranslation(-pd.Position[0], -pd.Position[1], -pd.Position[2]) : Matrix.Identity;
                _scene.PickupMeshRelatives.Add(relative);
                if (found >= 0 && _scene.PickupTemplate == null && pd.Kind == "Health")
                {
                    _scene.PickupTemplate = _scene.DynamicItems[found].Item;
                    _scene.PickupMeshRelative = relative;
                }
            }
        }

        private RenderItem AddItem(RenderMesh mesh, Matrix world, int flags, RenderLayer layer, Matrix[] bones)
        {
            if (world.Determinant() < 0f)
            {
                _scene.NegativeDeterminantItems++;
            }
            var item = new RenderItem
            {
                Mesh = mesh,
                World = world,
                BoneMatrices = bones,
                CastShadows = (flags & InstanceFlags.CastShadows) != 0,
                ReceiveShadows = (flags & InstanceFlags.ReceiveShadows) != 0,
                Layer = layer
            };
            _scene.Items.Add(item);
            _scene.WorldBounds.Add(TransformBounds(mesh.Bounds, world));
            return item;
        }

        internal static BoundingBox TransformBounds(BoundingBox b, Matrix m)
        {
            Vector3[] corners = b.GetCorners();
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector3.Transform(corners[i], m);
            }
            return BoundingBox.CreateFromPoints(corners);
        }

        public static Matrix ToMatrix(float[] f)
        {
            return new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
        }


        // ------------------------------------------------------------------ collision

        private readonly Dictionary<int, Vector3[]> _colliderTriangles = new Dictionary<int, Vector3[]>();

        /// <summary>
        /// Player collision world from the baked colliders. MeshColliders use the very mesh the renderer draws (same
        /// Model, same instance matrix), so what you see is what you hit. Triggers (pickup volumes) and the player's own
        /// CharacterController are skipped; enemy hitboxes (box/sphere) are included as static blockers like in Unity.
        /// </summary>
        private CollisionWorld BuildCollision()
        {
            var sw = Stopwatch.StartNew();
            var world = new CollisionWorld();
            int meshColliders = 0, boxes = 0, spheres = 0, skipped = 0;
            var hitboxTags = new Dictionary<string, int>();
            foreach (ColliderDto c in _file.Colliders)
            {
                if (c.Trigger || c.Type == "character")
                {
                    skipped++;
                    continue;
                }
                world.CurrentOwner = HitboxTag(c, hitboxTags);
                switch (c.Type)
                {
                    case "mesh":
                    {
                        float[] m = c.Instance >= 0 ? _file.Instances[c.Instance].M : c.M;
                        if (m == null || c.Mesh < 0)
                        {
                            skipped++;
                            break;
                        }
                        Matrix mat = ToMatrix(m);
                        Vector3[] tris = GetColliderTriangles(c.Mesh);
                        for (int i = 0; i + 2 < tris.Length; i += 3)
                        {
                            world.AddTriangle(Vector3.Transform(tris[i], mat), Vector3.Transform(tris[i + 1], mat), Vector3.Transform(tris[i + 2], mat));
                        }
                        meshColliders++;
                        break;
                    }
                    case "box":
                        if (world.CurrentOwner >= 0)
                        {
                            // an enemy hitbox may hang off an animated bone (the Turret's head): kept as a movable box
                            world.AddDynamicBox(world.CurrentOwner, ToMatrix(c.M), new Vector3(c.Center[0], c.Center[1], c.Center[2]), new Vector3(c.Size[0], c.Size[1], c.Size[2]));
                        }
                        else
                        {
                            world.AddBox(ToMatrix(c.M), new Vector3(c.Center[0], c.Center[1], c.Center[2]), new Vector3(c.Size[0], c.Size[1], c.Size[2]));
                        }
                        boxes++;
                        break;
                    case "sphere":
                        if (world.CurrentOwner >= 0)
                        {
                            // an enemy hitbox moves with its enemy: exact sphere (radius scaled by the owner matrix), not a triangle shell
                            Matrix sm = ToMatrix(c.M);
                            world.AddDynamicSphere(world.CurrentOwner, Vector3.Transform(new Vector3(c.Center[0], c.Center[1], c.Center[2]), sm), c.Radius * sm.Right.Length());
                        }
                        else
                        {
                            world.AddSphere(ToMatrix(c.M), new Vector3(c.Center[0], c.Center[1], c.Center[2]), c.Radius);
                        }
                        spheres++;
                        break;
                    default:
                        skipped++;   // capsule: none in MainScene
                        break;
                }
            }
            world.CurrentOwner = -1;
            world.Build(4f);
            _scene.CollisionSummary = meshColliders + " mesh + " + boxes + " box + " + spheres + " sphere colliders -> " + world.TriangleCount + " triangles, " + skipped + " skipped, " + sw.ElapsedMilliseconds + " ms";
            return world;
        }

        /// <summary>
        /// Enemy hitboxes ("Hitbox Top", "Hitbox Base", "HitBox": box / sphere colliders with a Damageable) get a collision tag per
        /// (enemy, GameObject name) so rays can tell which enemy they hit; the enemy is the nearest one to the collider's owner.
        /// Every Damageable in the prefabs and the scene has DamageMultiplier 1 (checked in MainScene.json and the prefab dumps).
        /// </summary>
        private int HitboxTag(ColliderDto c, Dictionary<string, int> tags)
        {
            if (c.Owner == null || !c.Owner.StartsWith("hitbox", StringComparison.OrdinalIgnoreCase) || c.M == null)
            {
                return -1;
            }
            Vector3 at = ToMatrix(c.M).Translation;
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _file.Enemies.Count; i++)
            {
                float d = Vector3.DistanceSquared(at, ToMatrix(_file.Enemies[i].M).Translation);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            if (best < 0)
            {
                return -1;
            }
            string key = best + "/" + c.Owner;
            if (!tags.TryGetValue(key, out int tag))
            {
                tag = tags.Count;
                tags[key] = tag;
                _scene.Hitboxes.Add(new HitboxInfo { Tag = tag, EnemyIndex = best, Name = c.Owner });
            }
            return tag;
        }

        private Vector3[] GetColliderTriangles(int meshRef)
        {
            if (_colliderTriangles.TryGetValue(meshRef, out Vector3[] cached))
            {
                return cached;
            }
            RenderMesh mesh = GetRenderMesh(meshRef, null);
            var tris = new List<Vector3>();
            foreach (MeshPart p in mesh.Parts)
            {
                int stride = p.VertexBuffer.VertexDeclaration.VertexStride;
                var verts = new Vector3[p.VertexBuffer.VertexCount - p.BaseVertex];
                p.VertexBuffer.GetData(p.BaseVertex * stride, verts, 0, verts.Length, stride);
                int count = p.PrimitiveCount * 3;
                var idx = new int[count];
                if (p.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
                {
                    var tmp = new ushort[count];
                    p.IndexBuffer.GetData(p.StartIndex * 2, tmp, 0, count);
                    for (int i = 0; i < count; i++) idx[i] = tmp[i];
                }
                else
                {
                    p.IndexBuffer.GetData(p.StartIndex * 4, idx, 0, count);
                }
                for (int i = 0; i < count; i++)
                {
                    tris.Add(verts[idx[i]]);
                }
            }
            Vector3[] result = tris.ToArray();
            _colliderTriangles[meshRef] = result;
            return result;
        }

        // ------------------------------------------------------------------ meshes

        private RenderMesh GetRenderMesh(int meshRef, int[] mats)
        {
            MeshRefDto r = _file.MeshRefs[meshRef];
            return GetRenderMesh(r.Model, r.Mesh, mats);
        }

        private Model GetModel(string name)
        {
            if (!_models.TryGetValue(name, out Model model))
            {
                model = _content.Load<Model>(name);
                _models[name] = model;
            }
            return model;
        }

        /// <summary>
        /// One RenderMesh per (model, mesh name, material set); vertex/index buffers are shared with the Model, so the
        /// ~1,990 level instances reference only a handful of GPU buffers.
        /// </summary>
        private RenderMesh GetRenderMesh(string modelName, string meshName, int[] mats)
        {
            string key = modelName + "|" + meshName + "|" + (mats == null ? "" : string.Join(",", mats));
            if (_renderMeshes.TryGetValue(key, out RenderMesh cached))
            {
                return cached;
            }

            Model model = GetModel(modelName);
            var mesh = new RenderMesh { Name = meshName ?? modelName };
            int ordinal = 0;
            bool any = false;
            for (int pass = 0; pass < 2 && !any; pass++)
            {
                // pass 0: ModelMeshes named exactly like Unity's mesh; pass 1 (fallback): the whole model.
                foreach (ModelMesh mm in model.Meshes)
                {
                    if (pass == 0 && meshName != null && mm.Name != meshName)
                    {
                        continue;
                    }
                    foreach (ModelMeshPart part in mm.MeshParts)
                    {
                        if (part.PrimitiveCount == 0)
                        {
                            continue;
                        }
                        string tag = part.Tag as string;
                        mesh.Parts.Add(new MeshPart
                        {
                            VertexBuffer = part.VertexBuffer,
                            IndexBuffer = part.IndexBuffer,
                            BaseVertex = part.VertexOffset,
                            StartIndex = part.StartIndex,
                            PrimitiveCount = part.PrimitiveCount,
                            Material = ResolveMaterial(tag, ordinal, mats, modelName)
                        });
                        _vertexBuffers.Add(part.VertexBuffer);
                        ordinal++;
                        any = true;
                    }
                }
                if (pass == 0 && !any && meshName != null)
                {
                    Console.WriteLine("SceneLoader: no ModelMesh named '" + meshName + "' in " + modelName + "; using the whole model");
                }
            }

            mesh.Bounds = ComputeBounds(modelName, meshName, mesh);
            _renderMeshes[key] = mesh;
            return mesh;
        }

        private RMaterial ResolveMaterial(string tag, int ordinal, int[] mats, string modelName)
        {
            if (mats == null || mats.Length == 0)
            {
                return _fallback;
            }
            if (!_options.MatchMaterialsByIndex && !string.IsNullOrEmpty(tag))
            {
                foreach (int idx in mats)
                {
                    if (idx >= 0 && _file.Materials[idx].Name == tag)
                    {
                        return GetMaterial(idx);
                    }
                }
                string k = modelName + ":" + tag;
                _missingTags[k] = (_missingTags.TryGetValue(k, out int n) ? n : 0) + 1;
            }
            int slot = ordinal < mats.Length ? ordinal : mats.Length - 1;
            return mats[slot] >= 0 ? GetMaterial(mats[slot]) : _fallback;
        }

        private BoundingBox ComputeBounds(string modelName, string meshName, RenderMesh mesh)
        {
            string key = modelName + "|" + meshName;
            if (_meshBounds.TryGetValue(key, out BoundingBox bb))
            {
                return bb;
            }
            var points = new List<Vector3>();
            foreach (MeshPart p in mesh.Parts)
            {
                int stride = p.VertexBuffer.VertexDeclaration.VertexStride;
                var verts = new Vector3[p.VertexBuffer.VertexCount - p.BaseVertex];
                p.VertexBuffer.GetData(p.BaseVertex * stride, verts, 0, verts.Length, stride);
                points.AddRange(verts);
            }
            bb = points.Count > 0 ? BoundingBox.CreateFromPoints(points) : new BoundingBox(Vector3.Zero, Vector3.Zero);
            _meshBounds[key] = bb;
            return bb;
        }

        // ------------------------------------------------------------------ materials and textures

        /// <summary>Material of the baked table by its Unity name (particle renderers refer to materials by name); the missing-material fallback when unknown.</summary>
        public RMaterial GetMaterialByName(string name)
        {
            for (int i = 0; i < _file.Materials.Count; i++)
            {
                if (_file.Materials[i].Name == name)
                {
                    return GetMaterial(i);
                }
            }
            return _fallback;
        }

        public RMaterial GetMaterial(int index)
        {
            RMaterial m = _materials[index];
            if (m == null)
            {
                m = BuildMaterial(_file.Materials[index]);
                _materials[index] = m;
            }
            return m;
        }

        private Texture2D Tex(string name)
        {
            if (name == null)
            {
                return null;
            }
            if (!_textures.TryGetValue(name, out Texture2D t))
            {
                t = _content.Load<Texture2D>(name);
                _textures[name] = t;
            }
            return t;
        }

        private RMaterial BuildMaterial(MaterialDto d)
        {
            var m = new RMaterial
            {
                Name = d.Name,
                Kind = (MaterialKind)Enum.Parse(typeof(MaterialKind), d.Kind),
                Surface = (SurfaceType)Enum.Parse(typeof(SurfaceType), d.Surface),
                Blend = (BlendKind)Enum.Parse(typeof(BlendKind), d.Blend),
                Cull = (CullKind)Enum.Parse(typeof(CullKind), d.Cull),
                ZWrite = d.ZWrite,
                AlphaClip = d.AlphaClip,
                Cutoff = d.Cutoff,
                RenderQueue = d.RenderQueue,
                BaseColor = new Vector4(d.BaseColor[0], d.BaseColor[1], d.BaseColor[2], d.BaseColor[3]),
                EmissionColor = new Vector3(d.EmissionColor[0], d.EmissionColor[1], d.EmissionColor[2]),
                Metallic = d.Metallic,
                Smoothness = d.Smoothness,
                BumpScale = d.BumpScale,
                OcclusionStrength = d.OcclusionStrength,
                TilingOffset = new Vector4(d.TilingOffset[0], d.TilingOffset[1], d.TilingOffset[2], d.TilingOffset[3]),
                SpecularHighlights = d.SpecularHighlights,
                EnvironmentReflections = d.EnvironmentReflections,
                ReceiveShadows = d.ReceiveShadows,
                BaseMap = Tex(d.BaseMap),
                NormalMap = Tex(d.NormalMap),
                MetallicGlossMap = Tex(d.MetallicGlossMap),
                OcclusionMap = Tex(d.OcclusionMap),
                EmissionMap = Tex(d.EmissionMap),
                // Measured against the Unity Play-mode shots: the blaster gem only reaches Unity's pale green when the
                // faint grey emission texels are used undecoded (sRGB decoding leaves it grey-white).
                EmissionMapIsSrgb = d.EmissionMap != "Textures/Weapons/Blaster_Emissive"
            };
            if (d.Floats != null)
            {
                foreach (KeyValuePair<string, float> kv in d.Floats)
                {
                    m.Floats[kv.Key] = kv.Value;
                }
            }
            if (d.Vectors != null)
            {
                foreach (KeyValuePair<string, float[]> kv in d.Vectors)
                {
                    float[] v = kv.Value;
                    m.Vectors[kv.Key] = new Vector4(v.Length > 0 ? v[0] : 0, v.Length > 1 ? v[1] : 0, v.Length > 2 ? v[2] : 0, v.Length > 3 ? v[3] : 1);
                }
            }
            return m;
        }

        // ------------------------------------------------------------------ lighting and cameras

        private SceneLighting BuildLighting()
        {
            LightingDto l = _file.Lighting;
            var s = new SceneLighting();
            if (l.SunDirection != null)
            {
                s.SunDirection = new Vector3(l.SunDirection[0], l.SunDirection[1], l.SunDirection[2]);
                s.SunColor = new Vector3(l.SunColor[0], l.SunColor[1], l.SunColor[2]);
            }
            s.SunIntensity = l.SunIntensity;
            s.SunShadows = l.SunShadows;
            s.AmbientColor = new Vector3(l.AmbientColor[0], l.AmbientColor[1], l.AmbientColor[2]);
            s.AmbientIntensity = l.AmbientIntensity;
            s.ShadowDistance = l.ShadowDistance;
            s.ShadowDepthBias = l.ShadowBias;
            s.ShadowNormalBias = l.ShadowNormalBias;
            s.SkyExposure = l.SkyExposure;
            s.SkyRotationDegrees = l.SkyRotation;
            if (l.SkyTint != null)
            {
                s.SkyTint = new Vector4(l.SkyTint[0], l.SkyTint[1], l.SkyTint[2], l.SkyTint[3]);
            }
            s.SkyboxTexture = Tex(l.SkyboxTexture);
            return s;
        }

        private static Camera BuildCamera(CameraDto d, float aspect)
        {
            Matrix world = ToMatrix(d.World);
            var c = new Camera
            {
                Position = world.Translation,
                NearPlane = d.Near,
                FarPlane = d.Far,
                FieldOfViewY = MathHelper.ToRadians(d.Fov),
                AspectRatio = aspect
            };
            c.View = Matrix.Invert(world);
            c.Projection = Matrix.CreatePerspectiveFieldOfView(c.FieldOfViewY, aspect, c.NearPlane, c.FarPlane);
            return c;
        }

        /// <summary>Updates a camera from a camera-to-world matrix.</summary>
        public static void SetCamera(Camera c, Matrix world, float aspect)
        {
            c.Position = world.Translation;
            c.AspectRatio = aspect;
            c.View = Matrix.Invert(world);
            c.Projection = Matrix.CreatePerspectiveFieldOfView(c.FieldOfViewY, aspect, c.NearPlane, c.FarPlane);
        }
    }
}
