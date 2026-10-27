# Pipeline stages

Shooter's implementations live under `Tools/` and are project-specific starting points; copy and adapt rather than rewrite.

## 1. UnityExtractor (.NET, no NuGet)
`dotnet run -c Release -- <unityProjectRoot> <outputDir> [--scenes A,B] [--flatten] [--prefabs-all] [--extra path1,path2]`
- Never modifies the Unity project. Reads YAML scenes/prefabs with a hand-rolled parser (multi-document `--- !u!<classId> &<fileID>`, exact 64-bit fileIDs).
- GUID index from every `.meta` under `Assets/`, `Packages/`, `Library/PackageCache/`.
- `--flatten` resolves nested prefab instances: overrides (`propertyPath`, `Array.size`, `Array.data[N]`), removed components/objects, added objects; hierarchy rebuilt from `Transform.m_Father`; nested-in-nested keys use `innerFileID ^ nestedInstanceFileID`.
- `--extra` adds root assets (and everything they reference) without pulling in a whole other scene. Use it for pickups/prefabs the scenes never place.
- Output: `Scenes/*.json`, `Prefabs/*.json` + `index.json`, `asset-manifest.json` (transitive closure by GUID, `unresolvedGuids`), `component-inventory.json`, `materials.json`.
- GameObject JSON: `id, name, active, transform{local*, world*}, rectTransform?, components[], children[]`; MonoBehaviour components carry `script, scriptPath, fields`.

Limits to remember:
- FBX/OBJ internal node hierarchy and transforms are **not** read (model instances become `ModelInstance` + placeholder `ModelNode` objects with identity transforms). Skeletons come from the character export.
- Script fields referencing Transforms are not resolvable; read raw YAML.
- Prefab world transforms are relative to the prefab's own origin; apply the placement when you bake an extra prefab.
- ParticleSystem is raw props only; a separate ParticleBake interprets it.
- Material variants (`m_Parent`) are not merged.
- Stale overrides on missing targets are skipped and listed in `warnings` (Unity ignores them too).

## 2. ContentGen (python, ImageMagick)
Reads `asset-manifest.json` + `.meta`, writes `Textures/ Audio/ Fonts/ Models/`, `*.mgcb`, `AssetMap.json`. See `assets.md`.
**Gotcha:** a rerun can rewrite bytes of unrelated textures (a different ImageMagick build re-downscaling). After a rerun, diff the textures and `git checkout` the ones you did not mean to change. 29 files were rewritten in one rerun.

## 3. Content pipeline extensions
Static-model processor (bakes RotationY 180 + scale, tangents) and a skinned-character pipeline. Target the .NET that the `dotnet-mgcb` host runs on (net8.0 for 3.8.5) even if the game is newer; the game csproj builds the extension before mgcb runs.

## 4. SceneBake
Turns extractor JSON into a compact runtime scene: instances with converted matrices, materials, colliders (boxes/spheres/mesh), lights, settings, prefab-derived sections (weapon, projectile, enemies with parts/nodes/hierarchy, pickups, player values). Rules:
- Emit the **scene's** values, not script defaults.
- Emit what the runtime needs as data (names of sounds/prefabs, numbers) so nothing is hard-coded. Constants that could not be baked (mixer dB, prefab AudioSource values) go in code with the source named in a comment.
- Enemy child nodes: bake a `hierarchy` (parent + local matrix + model-node flag); the flat model-part locals are untrustworthy for skeleton-attached children.
- JSON may carry `"Infinity"` as a string; read floats tolerantly.
- Extra prefabs: list placements explicitly (`ExtraPlacements`), rebake, check indices that shift.

## 5. ParticleBake
Interprets ParticleSystem props into per-prefab JSON: shape, emission, curves, gradients, renderer (mode, pivot, max size), trails, `scalingMode` and `localScale`, material names, placement (with active flag). See `pitfalls.md` for the renderer quirks.

## 6. Character export
Skeleton + animator JSON (controller states, parameters, clips, avatar masks) plus skinned model/animation processing. Additive layers use the bind pose as reference (*approximation*).

## 7. Runtime
Loader builds renderable items, collision soup, dynamic items; `Core/` is shared, `Platforms/*` are thin launchers. CLI flags for headless work (`--test-physics`, `--shot`, `--poses`) are part of the pipeline, see `verification.md`.
