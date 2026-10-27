---
name: unity-to-monogame-port
description: Port a Unity project (scenes, prefabs, scripts, assets) to MonoGame with a like-for-like look and feel. Use when asked to port, convert or recreate a Unity game, scene, prefab or mechanic in MonoGame, or to extract Unity YAML/assets for a MonoGame build. Captures the pipeline (extractor, content generator, scene baker, runtime), coordinate and asset conventions, per-mechanic loop, verification method and known pitfalls learnt from the FPS Microgame port (MonoGame-Samples/Shooter).
---

# Unity -> MonoGame port

Worked example: `MonoGame-Samples/Shooter` (Unity FPS Microgame). Its `Tools/PORTING-PLAN.md` is the chronological log; this skill is the how-to distilled from it.

Goal is **like-for-like look and feel, not byte-identical**. Agree scope first (which scenes, what is out) and write it at the top of a `Tools/PORTING-PLAN.md`.

## Working rules
- **Regenerate, don't hand-edit** generated content (`.mgcb`, AssetMap, baked scene JSON, particle JSON). Fix the generator, rerun.
- **Read the Unity script before porting it.** Copy its formulas and use the *scene's* Inspector values, not the script defaults (they often differ: e.g. WeaponSwitchDelay 0.2 vs 1).
- **Transform references in script fields are not resolvable from the extractor dump.** Read them from the raw prefab YAML by hand.
- **Caps:** max 3 attempts per approach; if the metric has not improved in 3 iterations, stop and report. No more than 2 agents at once.
- **Never stage or commit.** At the end of a phase, propose commit groups (see `references/commit-groups.md`); the user stages.
- **Every phase ends with stop + report**, including what was *not* verified.
- Mark claims in docs as **verified** (compared against a Unity capture or a headless check) or **assumed** (taken from the script/prefab). Never blur them.

## Pipeline
```
Unity project --UnityExtractor--> JSON (scenes, prefabs, asset-manifest, materials)
  --ContentGen--> Core/Content (textures, audio, fonts, models, .mgcb, AssetMap.json)
  --SceneBake--> compact runtime scene JSON (+ ParticleBake, character export for skinned rigs)
  --loader/runtime--> MonoGame game (Core + Platforms/Desktop [+ Android])
```
Details, flags and schemas: `references/pipeline.md`.

## Phases
0. **Extraction + Unity baseline.** Run the extractor. Capture the reference from **Play mode** (Game view + a `poses.json` of camera poses), not edit-mode renders; they differ.
1. **Scaffold** (Core + Platforms/Desktop, layout like SolidOps).
2. **Asset pipeline**: static models/textures/audio/fonts, then skinned characters + animation.
3. **Rendering** verified against the captures (lighting, sky, materials, a static character).
4. **Gameplay**, one mechanic at a time (controller, weapons, HUD, enemies, pickups, VFX/SFX, game flow, menus).
5. **Packaging**: icon, desktop and Android (`references/packaging.md`).

## Per-mechanic loop
1. Read the Unity script(s) and the prefab/scene values for it.
2. Extend the baker so the data is in the baked scene (rerun, don't hand-edit).
3. Port the logic. Keep Unity coordinates and formulas internally (camera-local, z forward) and convert at the edges.
4. Add headless checks to a `--test-physics` style self-test (exit code = failures).
5. Add/extend a `--shot` flag and compare against the Play-mode capture (`references/verification.md`).
6. Update `PORTING-PLAN.md` using the entry template below.
7. Stop, report, propose commit groups.

## PORTING-PLAN entry template
`## Phase <id>: <name>` with bullets: **Code** (files), **Data** (scene/prefab values used, with source), **Behaviour copied from the scripts**, **Verified** (check counts, shots, what they were compared with), **Not verified**, **Not done (by design)**. Example: `examples/phase-entry.md`.

## Reference files (read when needed)
- `references/conventions.md`: coordinate flip, quaternions, mesh bake, UVs, tangents, winding, camera, colour space. Read before touching any transform, mesh or shader.
- `references/assets.md`: textures, audio, fonts, unresolved GUIDs. Read before running ContentGen or adding assets.
- `references/pipeline.md`: each stage's inputs/outputs/flags, schemas. Read when extending a tool.
- `references/systems.md`: recipes per system (controller, weapons, HUD, AI, particles, audio, pickups, flow). Read when starting a mechanic.
- `references/pitfalls.md`: symptom -> cause -> fix. Read when something looks wrong versus Unity.
- `references/verification.md`: capture method, pose reading, comparison and test style.
- `references/packaging.md`: Desktop/Android packaging and icon.
- `references/commit-groups.md`: commit-group convention.
