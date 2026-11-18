# GodotExtract

Reads the Godot 4.x text formats (`.tscn`, `.tres`, `.import`, `project.godot`) of `tps-demo-master/` and writes JSON for the MonoGame port (plan: `Documentation/planning/01-scene-and-asset-extraction.md`). Never modifies the source project. No NuGet dependencies.

## Run (from `Tools/GodotExtract/`)

    dotnet run -- selftest                 # 23 checks, exit code 1 on failure
    dotnet run -- dump res://door/door.tscn   # parsed JSON (a disk path also works)
    dotnet run -- materials                # out/Materials.json
    dotnet run -- assetmap                 # out/AssetMap.json
    dotnet run -- bakelevel                # out/level.scene.json
    # options: --root <tps-demo-master dir> (auto-detected upward), --out <dir> (default Tools/GodotExtract/out, gitignored)

## Conventions
- `Transform3D` rows -> XNA row-vector `Matrix` by transposing (`Mat4.FromTransform3D`); world = `child * parent`; matrices are emitted as 16 floats, row-major (M11..M44, translation at 12..14). The root node of each scene contributes identity.
- Content name = `res://` path without extension, non-alphanumerics -> `_`, lowercase. glb models use the short file name (`structure`, `props`, `core`, `lights`, `player`, `red_robot`); `pathName` holds the long form.
- Reference values: `{"ext": id}` / `{"sub": id}` in `dump`; resolved to `{"path","resType"}` / `{"subType","props"}` in the bake output.

## Output summary (last run)
- selftest: 23/23 pass (Transform3D transpose on forklift 2 / spawn marker / a general basis, value parser, round trip of a small tscn and door.tscn, node/ext/sub counts of level (68 nodes), player (31), structure, all 90 tscn/tres parse, bake counts).
- Materials.json: 61 materials (8 ShaderMaterial listed, not translated), 9 glb material maps (`.glb.import` `use_external`).
- AssetMap.json: 138 textures, 11 sounds, 2 songs, 15 models, 11 meshes, 1 font, 2 exr (flagged unsupported); 12 textures above the 2048 cap (metadata only, `capped`/`width`/`height`; PNG only). `srgb` = used as albedo/emission, or not referenced by a normal/ORM slot and not a normal map.
- level.scene.json: 6 glb instances (structure, props, core, lights, 2 forklifts), 1052 colliders (1040 box; layer/mask from the parent body), 4+4 spawns, 2 audio volumes (Outside polygon prism, ReactorRoom capsule), 3 reflection probes, 32 light probes, 2 dynamics + their Animation sub-resources, environment.tres props, bus layout, physics layer names, prefabs `player`, `red_robot`, `bullet`, `door` (nodes, colliders, triggers, audio, particles, lights, animation summaries incl. audio/method track keys, connections).

## Not done / open
- Flattening goes down to the glb instance root only; per-mesh glb node matrices (props.glb 843 nodes) and `KHR_lights_punctual` need a glb reader (PipelineExtension / later).
- Doors: the level has none. door.tscn is a prefab that no scene or script instances (checked all .tscn/.gd); door.gd's `$DoorModel` path does not even match the scene's `DoorModel2` node, so it is dead content in the demo.
- Dynamics: each `dynamics` entry now carries `glb`/`model` (flying_forklift), the matching glb root in `instances` is flagged `"dynamic": true`, and `dynamicAnimations[].valueTracks` holds the full value tracks (mawaru: `.:rotation_degrees`, 0 s (0,0,0) -> 30 s (0,-360,0)).
- Shader materials, `.mesh`/`.occ` binaries, EXR are not parsed.
- Animation sub-resources are summarised (tracks of type audio/method kept with keys); skeletal clips come from the glb.
