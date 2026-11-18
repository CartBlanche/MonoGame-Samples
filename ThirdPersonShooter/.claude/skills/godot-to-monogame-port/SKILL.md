---
name: godot-to-monogame-port
description: Port a Godot 4 project (.tscn/.tres/.gd/.glb, AnimationTree, Area3D, CharacterBody3D, WorldEnvironment) to MonoGame DesktopGL with a like-for-like look and feel. Use when asked to port, convert or recreate a Godot game, scene or mechanic in MonoGame, or to extract Godot scenes/resources/glTF for a MonoGame build. Captures the pipeline (extractor, content generator, scene baker, runtime), coordinate and asset conventions, per-mechanic loop, verification and pitfalls from the Godot TPS demo port (MonoGame-Samples/ThirdPersonShooter). STATUS - v0.4, verified end-to-end on a playable vertical slice (menu, loading, player, shooting, robots with contacts and debris, effects, audio, pause, spot shadows from the level's own SpotLight3Ds); hand-tested on mouse and gamepad by the user. Multiplayer/LAN, mobile and GI/lightmaps are unproven; lighting is a tuned approximation, not a match.
---

# Godot -> MonoGame port

Worked example: `MonoGame-Samples/ThirdPersonShooter` (Godot 4.7 TPS demo, MIT). Plans: `Documentation/planning/01..05`, `_SHARED.md`, `LIGHTING-NOTES.md`. Sibling skill: `unity-to-monogame-port` (Shooter); shared rules in `references/shared-with-unity.md`. Evidence per item: `STATUS.md`.

Goal is **like-for-like look and feel, not byte-identical**. Agree scope first (vertical slice: movement, camera, shooting, one enemy, the level, menu, audio) and write it at the top of a plan file.

## Working rules
- **Never modify the Godot source project.** Manifests come from the transitive closure of the main scene (`--extra` escape hatch).
- **Regenerate, don't hand-edit** generated content (`.mgcb` texture section, AssetMap, Materials.json, baked scene). Fix the generator, rerun.
- **Read the .gd AND the .tscn before porting.** Constants hide in `.tscn` overrides, sub-resources, timers, AnimationPlayer method tracks (`grep '"method":'`) and AnimationTree. Missing property = Godot default.
- **Collision-only glTF nodes (`*-colonly`) are not drawn in Godot:** skip material-less primitives; bisect artefacts (skip-material diffs) before changing depth settings.
- **Audit before assuming** an engine feature exists (`Navigation*`, groups, UI sounds, health, autoloads): grep first. Godot often does less than you expect (no UI click sound, step sound never played).
- **Always-server single player:** delete `@rpc`, `MultiplayerSynchronizer`, `is_server`; keep the server branch.
- **Check which Godot version produced each reference capture** (the YouTube frames were ~2x brighter than Godot 4.4.1 captures of the same view); write the version next to every reference and tune to one baseline.
- **Record the player position/yaw (and any random spawn) with every reference capture**: lights lists and guesses landed in the wrong rooms 4 times.
- **Caps:** max 3 attempts per approach; no metric improvement in 3 iterations -> stop and report. Max 2 agents at once.
- **Never stage or commit**; propose commit groups at phase end. Keep source assets out of git if the licence/size needs it.
- Label claims **verified** (vs Godot capture or headless check) vs **assumed** (read from script/tscn). Say "not heard" / "not played by hand" when true.
- Cite the Godot file:line next to every ported constant (`PlayerConstants.cs` style).

## Pipeline
```
Godot project --GodotExtract--> JSON (scenes, resources, import settings, asset manifest)
  --ContentGen--> Core/Content (textures DXT, .mgcb section, AssetMap.json, Materials.json)
  --PipelineExtension (GlbImporter + static/skinned processors)--> .xnb models + clips
  --SceneBake--> level.scene.json (world matrices, colliders, lights, probes, spawns)
  --runtime--> MonoGame game (Core + Desktop)
```
Details: `references/pipeline.md`. Build **Release** only (the .mgcb references the Release extension DLL): `references/packaging.md`.

## Phases
0. Inventory + Godot reference screenshots (and a `poses`/start-frame shot). 1. Scaffold + extractor. 2. Static models, textures, then skinned character + clips.
3. Rendering vs reference (`references/rendering.md`). 4. Gameplay one mechanic at a time (`physics.md`, `animation.md`).
5. Audio, UI, loading, packaging (`audio.md`, `ui.md`, `packaging.md`).

## Per-mechanic loop
1. Read script(s) + `.tscn` values + AnimationTree + defaults; record formulas verbatim (`vec * transform` is the inverse transform).
2. Extend the baker/extractor so the data is in the baked scene.
3. Port logic on a fixed 60 Hz step; convert units/handedness at the edges only.
4. Add headless checks to the self-test (exit code = failures) using real baked values.
5. Add/extend a `--sim` script and a `--shot`; compare with the Godot capture (`references/verification.md`).
6. Update the plan entry: Code, Data (source), Behaviour copied, Verified, Not verified, Not done.
7. Append a STATUS entry (Verified / Lesson / Open), stop, report, propose commit groups.

## Stop-and-report rules
Stop when: a cap is hit; a source file/asset is missing (note it, continue elsewhere); a Godot feature has no MonoGame equivalent (list the approximation); results differ from the reference after 3 attempts; a shared decision in `_SHARED.md` would need changing (propose, don't edit).

## What was verified on the TPS demo (v0.4)
Scaffold and Release-only rule; GLB reader (116/116 vs independent read); DXT textures on macOS arm64; skinned player + robot with clips and root motion; HDR target + tonemap + bloom; player controller, aim, shooting, one enemy AI; menu/settings/pause/quit UI kit; lazy boot + loading screen; Ogg/Wav audio; start-frame orientation vs Godot capture; self-tests 28 + 8 + 7; effects (laser beam shader, particles, bullet light); debris physics; reverb zones + mono 3D audio; shadows (limited); collision-proxy skip and skip-diff bisecting. Added in v0.4: spot shadows from the level's spot lights (3 nearest, atlas, PCF), robot-robot and debris contacts decoded from layers/masks, mouse + gamepad play confirmed by the user, lighting passes 5-6 re-baselined on Godot 4.4.1 captures, build at 0 warnings. Not proven: multiplayer/LAN, Android/iOS/Windows/Linux builds, GI/lightmaps (real shadows now verified in a limited form: one key-light ortho map + a menu spot map, see STATUS 2026-11-17 shadows), reverb, stereo panning, interactive mouse/gamepad play. Lighting reached a partial match only (`rendering.md`).

## Reference files
- `conventions.md` coordinates, winding, UV, Transform3D -> Matrix, camera-rig lesson
- `concept-mapping.md` Godot concept -> MonoGame
- `pipeline.md` extractor, ContentGen, GLB importer, skinned processor, clips
- `animation.md` AnimationPlayer, root motion, blend spaces, additive layers, AnimationTree mapping
- `physics.md` CollisionWorld/CharacterMotor reuse, layers/masks, bullets, enemy AI
- `rendering.md` materials, HDR, tonemap, lighting without GI, tuning method
- `audio.md` songs/effects, buses, 3D attenuation, footsteps
- `ui.md` UI kit, message box, loading screen, lazy boot, settings
- `verification.md` self-tests, `--sim`, `--shot`, ImageStats, caffeinate
- `agents.md` briefing, caps, verifying agent claims
- `pitfalls.md` symptom -> cause -> fix
- `packaging.md` csproj/.mgcb/Release rule/icon
- `shared-with-unity.md` shared vs Godot-specific
- `STATUS.md` changelog: what is verified
