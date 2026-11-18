# Shared with the Unity skill vs Godot-specific
Use to decide whether to host both skills in a shared repo (a common `port-core` skill plus two engine skills). Assessment written from the planning docs, checked against the finished TPS slice (v0.2): the split held.

| Area | Same as Unity skill | Godot-specific |
|---|---|---|
| Goal, scope-first, plan file | yes | |
| Working rules (regenerate, caps, never commit, verified vs assumed) | yes | add: never modify source, audit before assuming, always-server |
| Pipeline shape (extract -> ContentGen -> bake -> runtime) | yes | extractor parses tscn/tres/import, not YAML/GUID |
| Phases, per-mechanic loop, plan-entry template | yes | |
| Commit-group convention | yes | |
| Verification ranking, shot harness, self-test style | yes | reference = Godot capture; no Play-mode GUID poses; `--sim` scripting and ImageStats grid metric were added here and are reusable in Unity ports |
| Packaging, icon trick | yes | |
| Coordinates | | no mirror (Unity needs a Z flip); but check the camera-rig basis chain (spring arm turned 180 deg) |
| Transform decoding | | Basis rows in tscn |
| Model import | FBX/Assimp-ish | custom GLB importer, ORM from `.tres` |
| Animation | clip resample, root bone, generic AnimationPlayer with root motion | AnimationTree graph, root_motion_track, `-cycle` clip suffixes, ClipWhitelist |
| Materials/lighting | PBR basics | StandardMaterial3D, AgX, VoxelGI, .gdshader |
| Scripts | C# MonoBehaviour | GDScript semantics (`vec * transform`, signals, groups) |
| Physics | capsule motor reuse | `move_and_slide` defaults, layer/mask bitfields |
| Audio/UI | recipes; UI kit and message box pattern; OpenAL no stereo pan | bus graph (-10 dB), `AudioStreamPlayer3D`, `Control` focus, StyleBox constants |
| Pitfall table | rendering/particle items partly | most items engine-specific |

Rough split: ~50% (process, verification, packaging) factors into a shared core skill; mapping, conventions, pitfalls stay per engine.
