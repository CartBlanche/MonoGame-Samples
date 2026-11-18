# Pipeline

Items are verified on the TPS demo unless marked (unverified).

## GodotExtract (Tools/GodotExtract; `selftest` = 23 checks)
- Parses `.tscn`, `.tres`, `.import`, `project.godot` (no NuGet deps); emits JSON (Materials, AssetMap, `level.scene.json`). Resolves `res://`, `ExtResource`, `SubResource`.
- Manifest = transitive closure from the main scene; `--extra` adds paths. Source project is read-only.
- Inventory first: node-type histogram per `.tscn`, file sizes by extension.
- Decode integer layer/mask bitfields, `texture_filter` enums, ORM channel selectors (`*_texture_channel` 0..4 = R,G,B,A,grey).
- Only non-default properties are written to `.tscn`/`.tres`: absent `glow_enabled` = off. Apply Godot defaults in the extractor.
- `.tscn` bloat is mostly baked animation tracks: dump `method`/audio tracks only.
- Non-portable binaries (VoxelGI `.res`, `.lmbake`, SDF `.exr`, occluders): drop or approximate. LightProbes/ReflectionProbes export **positions only** (colour is in lightmaps).

## GLB importer (PipelineExtension/Glb; GlbCheck selftest 116 checks vs an independent JSON read)
- Custom reader (no Assimp): meshes, nodes, skins, animations, KHR lights. No UV flip, no winding flip.
- `.glb` materials are placeholders; the real material is a `.tres` via `.glb.import` `use_external`. Always resolve through `.import`.
- Emit each glTF mesh **once**; instancing nodes reference it (per-node duplication: 147 MB vs 38 MB structure.xnb).
- Static processor: tangents if absent; vertex layout Position, Normal, Tangent, UV0.

## Skinned processor -> `SkinnedModelContent` (own writer + runtime reader)
- Player: 145 joints, 100 weighted, 107 skeleton bones kept, 7 parts, largest palette 65 (<72, no split needed). Check the palette limit (~72 `float4x3` on SM3/GL) and split parts by bone subset if exceeded.
- Keep weighted joints plus `ForceKeepBones` (non-weighted bones that code reads: hand, neck; robot: `MASTER` + the cannon bones).
- **ClipWhitelist** = every clip the AnimationTrees and the code use (menu tree included: `Idlecombatrest` + `eyes` were missing at first). A clip missing from the whitelist silently falls back to bind pose.
- Clips resampled at 30 fps; `-cycle` suffix in glb clip names = loop and is **stripped**; `-noexp` clips dropped; **root track kept** (do not prune it); node scale (Robot_Skeleton 0.803991) carried.
- Player clips: Idle, Idlecombat, Idlecombatrest, eyes, running_nogun, strafe_left/right/front/back, jump_2_upwards, jump_4_falling, AIM-Up/Down. Robot: Idle, TurnLeft/Right, Walk, Cannon-N/L/R/U/D, Damage_take1/2/heavy.

## ContentGen (Tools/ContentGen)
- Rewrites only the generated texture section of the `.mgcb` (BEGIN/END markers) from AssetMap + Materials; deterministic and re-runnable. Default = textures the level materials use (90 of 137 PNGs).
- **DxtCompressed** textures build and load on macOS arm64 (43.7 MB Color vs 5.5 MB DXT for two 2048 textures). Level: 90 textures = 149 MB of 232 MB content; clean Release build ~91 s, first-ever content build minutes.
- Audio: `OggImporter` + `SongProcessor` (music), `WavImporter` + `SoundEffectProcessor` (sfx) build on macOS arm64; source files referenced in place. Convert only if the format is unsupported (ffmpeg).
- Fonts: SpriteFont via the pipeline (PT Sans Bold 22/30/44 from the same TTF as Godot).

## SceneBake (`bakelevel`)
`level.scene.json`: instances with composed world matrices, colliders (box/sphere/capsule/hull), KHR lights, probe positions, spawn Marker3D transforms, environment. JSON is fine at demo scale.
