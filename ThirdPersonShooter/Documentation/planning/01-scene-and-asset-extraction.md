# 01 - Scene and asset extraction (Godot 4.7 TPS demo -> MonoGame DesktopGL)

Scope: turn `.tscn/.tres/.glb/.import/project.godot/default_bus_layout.tres` into MonoGame content-pipeline inputs plus a compact baked scene. Rendering, player, enemies and audio/UI detail live in plans 02-05; this file claims the data formats they consume. Facts below were measured from `tps-demo-master/` (glb JSON headers, grep of .tscn node types); items marked (unverified) were not checked.

## 1. Asset inventory (measured)

| Kind | Count | On-disk size | Notes |
|---|---|---|---|
| `.glb` | 15 | 96 MB | Textures are NOT embedded (0 images in every glb header). Materials in glb are placeholders replaced by `.tres` via `.glb.import` `use_external` |
| `.png` | 137 | 211 MB | 63 level/structure, 27 level/props, 4 car, 5 core, 8 red_robot, 4 player, 4 door, 5 forklift, rest VFX/UI |
| `.exr` | 2 | 65 MB | `level/level.exr` 56 MB (lightmap atlas, unverified), `level.GPUParticlesCollisionSDF3D_data.exr` 9 MB (SDF particle collision, drop) |
| `.hdr` | 1 | 0.4 MB | `menu/experiment.hdr` (menu sky/experiment) |
| `.res` / `.lmbake` | 1 / 1 | 40 MB / 2.3 MB | `giprobe_data.res` = baked VoxelGI, `level.lmbake` = Godot lightmap bake. Godot-internal binary, **not portable**; drop and rebuild lighting (plan 02) |
| `.tscn` | 17 | 2.9 MB | 2.7 MB of that is `red_robot.tscn` (10 019 `tracks/` lines = baked animation tracks) |
| `.tres` | 73 | 0.3 MB | StandardMaterial3D (~45), ShaderMaterial, Curve/Gradient, AudioBusLayout, Environment, blob_shadow texture |
| `.gdshader` | 7 | 24 KB | laser, ray, smoke, sparks, embers (VFX; plan 02) |
| `.gd` | 15 | 104 KB | gameplay logic (plans 03/04) |
| `.wav` | 11 | 2.8 MB | player 4 (step, jump, land, shoot), bullet_explode, red_robot 5 (charge, shoot, explosion, walk, hit), door open_close |
| `.ogg` | 2 | 2.5 MB | menu_intro_music 1.4 MB, level_music 1.2 MB |
| `.ttf` | 1 | 0.46 MB | PT Sans Bold (menu). Check OFL licence, copy text |
| `.mesh` / `.occ` / `.material` | few | small | raw Godot ArrayMesh (laser circle, ray, smoke plane, spark sphere, muzzle cone), occluder, 2 materials. Godot binary/text; unverified readability, see Q6 |
| `.import` | 182 | 0.7 MB | per-asset import settings (see section 4) |

### GLB headers (JSON chunk)

| File | MB | meshes | nodes | skins (joints) | anims | mats | Role |
|---|---|---|---|---|---|---|---|
| `player/model/player.glb` | 23.3 | 5 | 151 | 1 (145) | 56 | 2 | Player robot. 24 360 channels, LINEAR TRS only. Anim names ending `-noexp` are Godot "do not export" helpers: skip them |
| `enemies/red_robot/model/red_robot.glb` | 5.7 | 4 | 69 | 1 | 21 | 2 | Enemy |
| `level/geometry/models/structure.glb` | 28.4 | 104 | 344 | 0 | 0 | 32 | Level shell |
| `level/geometry/models/props.glb` | 21.7 | 130 | 843 | 0 | 0 | 21 | Props (843 nodes: mostly instanced mesh nodes) |
| `level/geometry/models/core.glb` | 11.8 | 35 | 151 | 0 | 0 | 30 | Reactor core |
| `level/geometry/models/lights.glb` | 0.06 | 4 | 177 | 0 | 0 | 1 | `KHR_lights_punctual`: 177 nodes, point/spot lights baked into node tree |
| `level/forklift/flying_forklift.glb` | 4.5 | 10 | 10 | 0 | 0 | 2 | Ambient prop, animated in level.tscn |
| `enemies/red_robot/parts/part_head,part_shield,ray.glb` | 0.3 | 1 each | 1 | 0 | 0 | 1 | Death debris |
| tiny VFX glbs (BlastMesh, BarrelSmoke, LightRaysMesh, SparkParticle, CoreOutLight) | <0.06 | 1-4 | | | | | VFX meshes |

Skin vertex data: `JOINTS_0` componentType 5121 (unsigned byte), `WEIGHTS_0`, no TANGENT attribute (Godot generates, `meshes/ensure_tangents=true`), no extensions except KHR_lights_punctual on lights.glb. Level meshes: `meshes/light_baking=2` (static lightmaps; whether UV2 `TEXCOORD_1` is present is unverified, Q3).

### Scene (.tscn) content that matters

- `level.tscn` (68 nodes): Core/Structure/Props/Lights instances (packed scenes wrapping the glb + colliders), 8 spawn `Marker3D` (4 robot, 4 player), 3 `ReflectionProbe`, 32 `LightmapProbe`, `VoxelGI` (size 228x98x234), `WorldEnvironment` -> `geometry/environment.tres`, 2 sound `Area3D` (Outside polygon, ReactorRoom box -> audio bus switch), 2 flying forklifts (animated by an `AnimationPlayer`), music player, `MultiplayerSpawner` (ignore).
- `structure.tscn`: 447 `CollisionShape3D` (31 `BoxShape3D` sub-resources reused, so mostly box shapes with per-node transforms) under 6 `StaticBody3D`, plus an `OccluderInstance3D`. Other collision scenes: `radial_colliders`, `radial_player_only`, `stair_pillar`. Physics layers: 1 Level, 2 Player, 3 Player Only.
- `player.tscn`: `CapsuleShape3D`, `SeparationRayShape3D`, `AnimationTree` (2 BlendSpace2D, OneShot, TimeScale, Transition, 15 animation nodes), 2 CPUParticles3D, 4 AudioStreamPlayers, camera shake script. State machine itself belongs to plan 03.
- `red_robot.tscn`: `AnimationTree` (3 OneShot, 12 animation nodes), 8 collision shapes (5 box, 3 sphere), 3 `RigidBody3D` (debris), 9 CPUParticles3D, 4 AudioStreamPlayer3D, `AnimationLibrary`x3 with 23 `Animation` sub-resources (the 10k track lines: property/method tracks driving sound, particles, visibility; NOT skeletal data, that is in the glb).
- Environment (`environment.tres`): background colour mode 1, ambient colour (0.26,0.20,0.15), tonemap_mode 4 (AgX) exposure 4.5 white 6.0, SSAO, SDFGI, glow, fog + volumetric fog density 0.002. Handed to plan 02.
- `default_bus_layout.tres`: Master -10 dB; bus `Outside` with Reverb (room 0.98, wet 0.29); bus `Reactor` with Reverb (room 0.74, wet 0.56). Handed to plan 05.
- `project.godot`: 1920x1080, input map (move_*, jump, aim, shoot, view_* , quit, toggle_fullscreen, toggle_debug, ui_*), autoload `Settings`, physics Bullet, 3 named layers. Keycodes are Godot `Key` enums (4194305 = Escape ...), convert via a table.

## 2. Coordinate and convention table (verified from Godot/glTF specs; runtime proof pending, see tasks 12/21)

| Topic | Godot 4 | glTF 2.0 | MonoGame | Action |
|---|---|---|---|---|
| Handedness | right-handed | right-handed | right-handed | **No mirror** (unlike the Unity port's `diag(1,1,-1)`) |
| Up axis | +Y | +Y | +Y | none |
| Camera/forward | camera looks -Z; Godot docs: *model front is +Z* | asset front faces +Z | camera looks -Z | none for transforms. The player/enemy "faces +Z" convention matters only to gameplay (`look_at`/`Basis` forward). Do not bake any 180 deg turn; test with the asymmetric player model |
| Units | 1 unit = 1 m | 1 = 1 m | 1 = 1 | none (do not apply the Unity port's scale bake) |
| Quaternion | (x,y,z,w) | (x,y,z,w) | (X,Y,Z,W) | direct copy, normalise |
| Matrix | `.tscn` `Transform3D(r0x,r0y,r0z, r1x,r1y,r1z, r2x,r2y,r2z, ox,oy,oz)`: Basis as three **rows** (column vectors are x/y/z axes) | column-major 16 floats | row-vector `Matrix`, `M41..M43` = translation | Godot column vector `v' = B*v`; XNA row vector `v' = v*M`, so `M11..M13 = B.col0 = (r0x,r1x,r2x)` i.e. transpose the rows into `Matrix` rows. Verify with a one-node test (task 6): forklift 2 `Transform3D(-1,0,-1.5e-7, 0,1,0, 1.5e-7,0,-1, ...)` is a 180 deg Y turn |
| Triangle winding | CCW front | CCW front | `CullCounterClockwise` default | none; custom importer must not flip |
| UV origin | top-left, v down | top-left, v down | texture loaded top-left | **No `FlipUVs`**. A custom GLB importer needs no flip; Assimp-based importer flips by default, so avoid or compensate |
| Normal map | glTF/Godot: +Y up (OpenGL) | OpenGL | shader-defined | Treat as OpenGL (tangent W handedness from glTF/computed). `normal_map_invert_y=false` in .import |
| Tangents | generated at import | absent | content processor must generate (MikkTSpace-like) | processor computes, w = sign |
| Colour space | albedo sRGB, ORM/normal linear, rendered linear | same | `TextureProcessor` has no sRGB output | store as `Color`, decode sRGB albedo/emission in shader or use `SurfaceFormat.ColorSRgb` via custom processor (plan 02 owns the choice; see Q5) |
| ORM packing | Godot ORM: AO=R, Roughness=G, Metallic=B | glTF: occlusion R, rough G, metal B | | Same layout; confirmed via `column.tres`: ao_texture ch0 (default), roughness ch1, metallic ch2 |
| Skeleton | glTF joint nodes + inverseBindMatrices | | | Bake to bone palette; 145 joints (see Q2) |
| Scene root transforms | `.tscn` local TRS per node, parent-relative | | | compose `child * parent`; bake world matrices at export |

## 3. Importing glTF/GLB: options

| Option | Pros | Cons |
|---|---|---|
| A. MonoGame `OpenAssetImporter` (Assimp) on .glb | zero code | Assimp glTF2 support in MonoGame's bundled build is unverified (Q1); applies `FlipUVs`; skin of 145 bones, 56 animations x 24k channels is slow and loses names/`-noexp` filtering; no KHR lights; cannot read Godot `.tres` mapping |
| B. Convert glb -> FBX with Blender (headless) | ready path; `.blend` sources exist for player/red_robot/core/lights | Blender dependency; FBX axis/scale bake gotchas; each re-export drifts (the Unity port lesson: reruns rewrote bytes); `.blend` files not provided for every glb (structure/props?) (unverified) |
| **C. Custom GLB importer + processor (recommended)** | ~600-800 LOC, no native deps, full control: skip `-noexp` anims, resample to fixed rate, drop unused joints (145 -> weighted joints), keep node names/extras, read KHR lights, no UV flip, direct material-name join to `.tres` | must write it (but GLB = JSON + BIN, small spec) |

Recommended shape of C (in `PipelineExtension/`, net8.0 to match the `dotnet-mgcb` host, like RacingGame's `PipelineExtension.csproj`):
1. `GlbImporter : ContentImporter<NodeContent>` parse GLB (12-byte header, JSON chunk via `System.Text.Json`, BIN chunk); build `NodeContent` tree with `MeshContent` (positions, normals, uv0, uv1 if present, joints, weights) and `BoneContent` + `AnimationContent` for skinned files; material by name (`MaterialContent` with the glTF name only).
2. Static level geometry: `GodotStaticModelProcessor : ModelProcessor` (like RacingGame's `RacingGameModelProcessor`): generate tangents, merge to `ModelMesh` per glTF mesh, keep node names for `.scene` join.
3. Skinned: `GodotSkinnedModelProcessor` emits `SkinnedModelContent` (custom type + `ContentTypeWriter`/runtime reader): vertex stream (Position, Normal, Tangent, UV, `Byte4` joints, weights `Vector4`), bone hierarchy, inverse bind, clips resampled at 30 fps (`Vector3`/`Quaternion`/`Vector3` per bone per frame, keyed per clip). Size estimate: player 150 bones x 56 clips... restrict to the ~25 clips used by `player.tscn` AnimationTree (list in task 10) to avoid a ~10-50 MB content blob.
4. Materials never come from the glb: the **material resolver** (section 5, step 2) writes `Materials.json`, joined at bake time by glTF material name via `.glb.import` `_subresources/materials/<name>/use_external/fallback_path` (we saw `playerobot -> res://player/model/playerobot.tres`).

## 4. Textures, `.import` settings, audio

- Textures: read `compress/mode` (2 = VRAM compressed for these), `mipmaps/generate=true`, `normal_map` flag, `roughness/*`, `process/fix_alpha_border`. Godot `texture_filter = 5` = Linear mipmap anisotropic; project anisotropy 4x -> `SamplerState.Anisotropic` AF 4, wrap repeat.
- ContentGen converts nothing by hand: PNG goes straight to `TextureImporter` + `TextureProcessor` (`ColorKeyEnabled=False`, `PremultiplyAlpha=False`, `GenerateMipmaps=True`, `TextureFormat=DxtCompressed` for ORM/normal/albedo; alpha-bearing VFX keep `Color`). DXT on macOS arm64 mgcb needs verification (Q4). Source 211 MB of PNG implies many 2k/4k textures; add a `--max-size` cap (Unity port did 512; start at 2048, make it a script flag, `--full-res` to disable). Never reprocess unchanged files (mtime/hash cache) to avoid the Unity-port "rerun rewrote 29 textures" issue.
- Channel remap and packing: Godot materials sample three channels of one ORM texture; keep the ORM as is and let the shader pick channels (cheapest). Roughness/metallic factors come from `.tres`.
- EXR: no MonoGame importer. `level.exr` (probably the LightmapGI atlas) must be converted offline (ImageMagick/`OpenEXR` python -> RGBE `.hdr` or half-float RGBA16F raw, custom `Texture2D` of `SurfaceFormat.HalfVector4` through a tiny `ExrImporter`). If lightmaps are not ported (plan 02 decision), skip entirely and save 56 MB.
- Audio: `.wav` -> `SoundEffectProcessor` (Quality Best); `.ogg` music -> `SongImporter`/`SongProcessor` (`OggImporter` exists in the pipeline: unverified for 3.8.6, fall back to ffmpeg ogg->wav/mp3). Godot `loop_mode`/`loop_offset` come from each `.wav.import` (read `edit/loop_mode`). `AudioStreamRandomizer` in `player.tscn` -> list of variant names (pitch/volume ranges).
- Fonts: PT Sans Bold `.ttf` -> `.spritefont` per size used by `menu.tscn` Labels (read `font_size`/theme overrides).

## 5. Recommended pipeline (tools, formats)

All tools live in `ThirdPersonShooter/Tools/` (to be added next to the layout copied from RacingGame; `Documentation/` and `scripts/` exist already) and **never modify `tps-demo-master/`**.

| # | Stage | Tool | Input | Output / format |
|---|---|---|---|---|
| 1 | Godot text parser | `Tools/GodotExtractor` (.NET, no NuGet, like the Unity port's extractor) | `.tscn`, `.tres`, `project.godot`, `.import`, bus layout | `Extracted/scenes/*.json`, `resources.json`, `asset-manifest.json` (transitive closure by `res://` path and `uid://`, `unresolved` list) |
| 2 | Material resolver | part of extractor | `.tres` StandardMaterial3D + ShaderMaterial + `.glb.import` | `Materials.json`: `name -> {albedo, orm, normal, emission, albedoColor, metallic, roughness, emissionEnergy, uv1Scale/Offset, cull, transparency, blend, shader?, texChannels}` |
| 3 | Content generator | `scripts/gen-content.py` (+ ImageMagick only if needed) | manifest | `Core/Content/Content.mgcb` generated (do not hand edit), `AssetMap.json` (`res://path -> content name, type, srgb, loop, spatial info`) |
| 4 | Pipeline extension | `PipelineExtension/` (GlbImporter, static and skinned processors, optional ExrImporter) | `.glb` | `.xnb` of static `Model` and `SkinnedModelContent` |
| 5 | Scene baker | `Tools/SceneBake` | `Extracted/*.json` + `AssetMap.json` | `Content/Scenes/level.scene.json` (or compact binary `.scn`): see below |
| 6 | Runtime loader | `Core/Scene/SceneLoader` | `.scene` | instances, colliders, lights, spawns, volumes |

### Baked scene format (`level.scene`)
JSON at dev time (readable, diffable), `ContentTypeWriter` binary later. Sections:
- `settings`: environment (colours, fog, tonemap, ambient), bus layout values, physics layers.
- `instances[]`: `{model: "structure", node: "Pillar12" , mesh, material, world: float[16]}` world matrix already composed from the `.tscn` hierarchy and the glb node TRS (so the runtime never walks glb hierarchy for static level; `props.glb` 843 nodes flatten into ~130 mesh instances grouped for instancing).
- `colliders[]`: `{type: box|sphere|capsule|convexHull|trimesh|polygonPrism, world, size, layer, mask}`. Level is boxes (447 shapes, 31 reused box sizes); trimesh only if needed for stairs/ramps (check `stair_pillar`, `radial_*`).
- `lights[]`: from `lights.glb` KHR points/spots (plus energy/range/colour/shadow flag), world transform.
- `probes[]`: reflection probes (position, extents, intensity), light probes (32 `LightmapProbe` positions, optional).
- `spawns`: `playerSpawns[4]`, `robotSpawns[4]` `Marker3D` world transforms.
- `volumes[]`: audio Areas (`Outside` polygon prism, `ReactorRoom` box) -> bus name.
- `dynamics[]`: forklifts (model, transform, animation name and loop).
- `prefabs`: `player.json`, `red_robot.json`, `bullet.json`, `door.json` with component values (capsule size, speeds from exported node props, particle emitters from CPUParticles3D props, audio names + volumes + 3D unit sizes, animation tree description).

Rules carried over from the Unity port: emit scene values, not script defaults; bake what the runtime needs as data (sound names, numbers); constants that cannot be baked (bus dB, script constants such as `JUMP_SPEED=5`) go in code with a comment naming the `.gd` source; tolerate `inf`/`nan` strings in floats; flatten nested instanced scenes (`instance=ExtResource(...)`, `.tscn` overrides as property lines).

### Godot .tscn text format - parser notes
- Header `[gd_scene format=3 uid="..."]`, then `[ext_resource type path uid id]`, `[sub_resource type id]` + property lines, `[node name type parent instance index unique_id groups]` + props, `[connection ...]`.
- `parent="."` root children; `parent="A/B"` slash path; no `parent` = root; `instance=ExtResource("id")` = packed scene, property lines under it are **overrides** (including `transform`).
- Values: `Vector3(...)`, `Transform3D(12 floats)`, `Color(r,g,b,a)`, `ExtResource("id")`, `SubResource("id")`, `PackedFloat32Array(...)`, `NodePath`, `&"StringName"`, `Object(Type,...)` in project.godot input maps. Tokeniser must handle nested parentheses, quotes and multi-line values (animation `tracks/N/keys = { ... }`).
- Resource references use both `res://` path and `uid://`; resolve by path (uid index as fallback).
- Ignore: scripts (read property names only), `MultiplayerSynchronizer`/`Spawner`, `SceneReplicationConfig`, GPUParticlesCollisionSDF3D, VoxelGI, `Label` debug.

## 6. Vertical-slice asset subset

| Needed for slice | Source | Notes |
|---|---|---|
| Level shell+props+core+lights | `structure/props/core/lights.glb` (62 MB), `level.tscn`, 3 collision scenes, `environment.tres` | The level cannot be partial: it is one connected space. Defer reflection/lightmap data |
| Level textures | `level/textures/structure` (63), `props` (27), `Core` (5), `car` (4) | Take only those referenced by used `.tres` materials (manifest closure) |
| Player | `player.glb`, `player/textures` (4), `playerobot.tres`, `robotemitter.tres`, `crosshair.png`, `bullet/` + bullet effect pngs, 4 wav, `bullet_explode.wav`, `muzzle_flash_particle_mesh_Cone.mesh` | Clips: AIM-*, idle, walk/run, jump, shoot (restrict to AnimationTree list) |
| Enemy | `red_robot.glb`, `red_robot/textures` (8), 5 wav, laser/impact meshes+shaders (plan 02), parts glbs | Slice can skip debris parts/shield initially |
| UI minimal | `crosshair.png`, PT Sans font | Menu later |
| Audio | `level_music.ogg`, `open_close.wav` | Door and menu later |
| Deferred | `menu/*` (menu.tscn 27 KB, hdr, intro music), `door/`, forklifts, `*.exr`, `giprobe_data.res`, `level.lmbake`, particle SDF, `screenshots/` | |

Estimated content size after pipeline (guess, unverified): textures 60-120 MB DXT at 2k cap, models 40 MB, audio 5 MB.

## 7. Risks and open questions

Risks
1. **Lighting**: look is driven by SDFGI/VoxelGI/lightmaps/AgX tonemap/volumetric fog; extracted data is Godot-internal. Only direct light + probes + approximations are portable (plan 02).
2. **Skinned data volume and bone count**: 145-bone player, 24 360 channels. Need joint pruning, resampling and a clip whitelist, and a shader with a palette > 72 bones (`SkinnedEffect` limit 72).
3. **Custom importer scope creep**: GLB importer + skinned processor are the critical path for both player and enemy; schedule first, with a test against asymmetric player model.
4. **Texture weight**: 211 MB PNG + DXT-on-macOS tooling; build time and repo size (decide git LFS or keep sources out of the repo and use a fetch script from the upstream repo).
5. Transform matrix layout in `.tscn` is easy to get transposed; single-node unit test required.

Open questions
- Q1 Does MonoGame 3.8.6.1 OpenAssetImporter register `.glb/.gltf` (and does bundled Assimp read glTF2 with skins)? Only relevant if option A is considered; one 10-line test.
- Q2 Max uniform vec4 count on macOS GL 3.3-4.1 DesktopGL for a 145 x 3 row palette (435 vec4s; usually fine, but verify) vs pruning.
- Q3 Does `structure/props/core.glb` carry `TEXCOORD_1` (lightmap UV2)? Check with the glb header tool; decides whether `level.exr` lightmaps can be reused.
- Q4 DXT compression in `mgcb` on macOS arm64 (native lib availability); fallback to uncompressed `Color` with a size cap.
- Q5 sRGB handling (`ColorSRgb` texture vs shader decode): plan 02 decides; extractor records `srgb` per texture either way.
- Q6 Are the `.mesh`/`.occ`/`.material` files text or Godot binary? (`.mesh` used for VFX; if binary, re-author the 5 tiny meshes procedurally in code instead of parsing.)
- Q7 Is `level.exr` really the lightmap atlas (named `level.exr`, referenced by `level.lmbake`) and is it worth converting?
- Q8 Reverb buses on MonoGame/OpenAL: available via effects? (plan 05).
- Q9 How `-noexp` and `.001` suffixed clips map to AnimationTree nodes (plan 03 provides the whitelist).

## 8. Ordered task list (S <= 0.5 day, M ~ 1-2 days, L >= 3 days)

1. (S) Create `ThirdPersonShooter/` skeleton mirroring RacingGame: `Core/ Desktop/ Assets/ PipelineExtension/ scripts/ Tools/ .sln`; copy `LICENSE.md`; README credit.
2. (S) `scripts/` glb header dumper (python -I, JSON chunk only): outputs per-glb stats table + UV2/joint checks (answers Q3, Q2).
3. (M) `Tools/GodotExtractor`: tokenizer + `.tscn/.tres` parser (sections, values, nested instances, uid index) with tests on `main.tscn`, `door.tscn`.
4. (M) Flatten scene instances and overrides; emit `Extracted/*.json` + `asset-manifest.json` (closure from `main/main.tscn`; `--extra` list for unreferenced assets).
5. (S) `.import` reader (texture and audio keys, glb `use_external` material map); `project.godot` input map reader.
6. (S) Transform conversion + unit test against forklift 2 (180 deg about Y) and a spawn marker.
7. (M) Material resolver to `Materials.json` (StandardMaterial3D props, channel selectors, shader materials listed, not translated).
8. (L) `PipelineExtension`: `GlbImporter` (static meshes + node tree + KHR lights), tangent generation, `GodotStaticModelProcessor`. Verify on `lights.glb` then `core.glb`.
9. (L) Skinned path: skin/inverse-bind/joint pruning, animation resample, `SkinnedModelContent` writer/reader, clip whitelist. Verify with `player.glb` in a pose viewer.
10. (S) Obtain clip whitelist from plan 03/04 (`AnimationTree` node names) and apply in processor.
11. (M) `scripts/gen-content.py`: texture/audio/font list -> `Content.mgcb`, `AssetMap.json`, hash cache, `--max-size`, `--full-res`.
12. (M) `Tools/SceneBake`: instances (flattened world matrices), colliders, lights, probes, spawns, volumes -> `level.scene.json`; check counts against the .tscn grep numbers (447 colliders, 8 spawns, 32 probes, 3 reflection probes).
13. (M) Prefab bake (`player`, `red_robot`, `bullet`): colliders, emitters, audio refs, animation tree description for plans 03/04.
14. (S) Audio conversion check (wav loop flags, ogg path) and music `Song` entries.
15. (M) Runtime `SceneLoader` + collision soup build; screenshot test against `screenshots/screenshot.webp` (reference image).
16. (S) Optional EXR convert + loader if plan 02 wants lightmaps.
17. (S) Fonts to `.spritefont`; licence file.
18. (M) Document formats in `Documentation/` (`Scene-Format-Spec.md`), include extractor CLI usage.

## 9. Lessons for the porting skill (godot-to-monogame-port)

- Godot and MonoGame both are right-handed Y-up (-Z camera): **no axis mirror**, a big simplification relative to Unity. Still write a one-node transform unit test; the `.tscn` `Transform3D` stores basis rows, easy to transpose.
- glTF-native pipelines beat FBX conversion: write a small custom GLB importer instead of depending on Assimp; no UV flip, no winding flip, direct skin/anim data and node names.
- `.glb` materials are placeholders: the real material lives in `.tres` and is attached by `.glb.import` `use_external` mapping. Always resolve materials from `.import`, never from the glb.
- Godot-internal baked data (VoxelGI `.res`, `.lmbake`, SDF `.exr`, occluders) is not portable. Identify it up front by extension and decide: rebuild approximately or drop.
- Godot `.tscn` bloat: baked animation tracks (10k lines) are logic hooks (sound/particles), not skeletal data; skeletal clips are in the glb. Filter `-noexp` clips and whitelist from the AnimationTree.
- Plan texture budget early (211 MB of PNG); cap size via a script flag, cache by hash so reruns do not rewrite files.
- ORM packing (AO=R, Rough=G, Metal=B) is the same as glTF: keep textures packed and select channels in the shader; read the channel index from the `.tres`.
- Never modify the source project; manifests built by transitive closure from the main scene with an `--extra` escape hatch.
- Measure inventory with grep/JSON headers before planning: node-type histograms per .tscn showed collision and animation dominate size.
