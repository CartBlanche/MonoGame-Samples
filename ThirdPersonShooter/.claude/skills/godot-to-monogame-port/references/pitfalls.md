# Pitfalls: symptom -> cause -> fix
Rows marked **[hit]** happened on the TPS port; the rest are predicted (planned, unverified).

| Symptom | Likely cause | Fix |
|---|---|---|
| **[hit]** Game starts facing a door / the opposite way to the reference | Assumed the camera looks along the spawn's -Z and added a 180 deg turn; the SpringArm3D basis (x=-1, z=-0.94) makes the camera look along +Z and the glb faces +Z | Multiply out the rig's basis chain; yaw from the spawn's Backward; no 180 at model placement; compare the first frame at the spawn |
| **[hit]** Player/robot idles in place, no locomotion or speed wrong | Root track pruned, or root delta not taken through the parent-chain scale | Keep root track; delta to model space through Robot_Skeleton scale; speed = root motion |
| **[hit]** Clip plays as bind pose / missing animation | Clip not in `ClipWhitelist` (e.g. `Idlecombatrest`, `eyes` used by the menu tree) | Add every clip any AnimationTree or code uses |
| **[hit]** Clip names do not match | glb names carry a `-cycle` suffix | Strip it in the processor; `-cycle` = loop; drop `-noexp` |
| **[hit]** Bone-driven aim/cannon does nothing | Extra bones (MASTER, cannon) pruned | `ForceKeepBones` |
| **[hit]** Emissive too dim in the menu but fine in game | Emission tuned for the game pass exposure (3.4); menu pass uses 1.3 | Rescale emission by `3.4/exposure` per pass |
| **[hit]** Stats match the reference but the image looks wrong | Mean/std/sat/hue only | Spatial grid metric on the same frame + view both PNGs |
| **[hit]** Floors too bright / flat, no contrast | Key light pointing straight down; ambient raised | Slant the fake key toward player-facing surfaces; distance fade on key and ambient |
| **[hit]** Every run/shot hangs on macOS | Display asleep, SwapWindow blocks | `caffeinate -u -t 2` first |
| **[hit]** Debug build can't find PipelineExtension | `.mgcb` references the Release DLL | Build Release |
| **[hit]** structure.xnb 147 MB | Mesh emitted per instancing node | Emit each mesh once |
| **[hit]** Blob shadow invisible | Custom shader never drew it; ground height under the player wrong | `BasicEffect` + radial alpha texture; snap to feet via ground probe |
| **[hit]** 3D sounds are not panned | OpenAL does not pan stereo buffers | Mono downmix processor (not done) |
| **[hit]** Footsteps missing | Godot never plays the player step sound | Derive phases from foot-bone heights (port addition) |
| **[hit]** Menu sounds expected but absent | Godot has no UI focus/confirm sound | Audit the scene before porting audio |
| Node rotated/mirrored wrongly | Basis rows read as columns | Transpose; unit-test one rotated node |
| Level materials look like grey placeholders | Used glb materials | Resolve via `.glb.import` `use_external` + `.tres` |
| Effect appears despite `*_enabled=false` | Stored params of disabled effect used | Honour the flag; absent = default |
| Black/unlit meshes | Effect params not set (GL defaults 0) | Set all each frame |
| Palette overflow / garbled skin | >72 bones in one draw | Prune, split parts by bone subset |
| Gameplay constant differs from script | Value overridden in `.tscn` | Grep tscn, sub-resources, timers, method tracks |
| Enemy shoots at wrong time | `shoot_check` is an AnimationPlayer method track | Port as clip event |
| Vector math inverted | GDScript `vec * transform` is inverse | Copy formula verbatim, test |
| Camera jitter | `physics_interpolation` on | Fixed step + interpolated render (unverified) |
| Triggers/bullets hit the wrong bodies | Layer/mask bitfields ignored | Decode to flags enum; copy Godot even if odd (bullets ignore layer 4) |
| Stick Y inverted on gamepad | XNA sign differs | Negate |
| Sound loud/clipped | dB sums over 0 dB | Record per-source and master dB (-10 dB bus) |
| Huge repo / slow rerun | Source PNGs committed, no cache | Texture cap, DXT, deterministic generator |
| Pale/grey layers z-fight over real props, blotches shimmer on camera move | glTF nodes named `*-colonly` (material-less primitives) are collision proxies; Godot imports them collision-only, we drew a grey fallback **[hit]** | Skip material-less primitives by default (env flag to draw them); do NOT assume depth precision first |
| "Z-fighting" fix by raising the near plane only hides it | Near-plane clipping removed the proxy layer, not the cause **[hit]** | Bisect by skipping one material/node at a time and diffing frames before touching depth settings |
| Half-grey / half-coloured prop | Two neighbouring props interpenetrate in the source data (bottles 0.4 m apart) **[hit]** | Check node transforms and skip-diff before "fixing" the renderer; compare with the Godot frame |
| Sparks/particles do not fly like the original | Godot default initial velocity is 0 and the invented motion was wrong **[hit]** | Read the particle sub-resources in the `.tscn`; port only what is set |
| White "slats" under a grate | Untextured `tile_steel` (white albedo, metallic 1) turned diffuse by a metal-as-diffuse hack **[hit]** | Find the layer with skip-material diffs; tint that material, not the global metal rule |
| Shadow light barely visible / metre-long shadows | Fake key light is near-horizontal (11 deg) **[hit]** | Use a separate, higher elevation for the shadow light and darken ambient on lit-facing surfaces only |
| Shadow map shimmers when the player moves | View translation not on texel grid | Snap the light-view translation to whole texels; test it headlessly |
| Shadow depth cannot be sampled | DesktopGL depth renderbuffers are not sampleable | Write depth to an R32F (`SurfaceFormat.Single`) colour target (works on macOS GL) |
| Build of an old commit in a worktree fails (missing JSON) | Gitignored generated files (extractor `out/`) are absent | Copy the generated folder into the worktree first |
| `timeout`/`sed -i` fail on macOS | No `timeout`; BSD sed needs `-i ''` **[hit]** | Avoid `timeout`; use `sed -i ''` or python |
| Tracked `__pycache__` shows modified | Stats script run created bytecode | `git checkout -- Tools/ImageStats/__pycache__` (or untrack it) |
| Whole frame one flat colour (or black) after adding a shader using a `float4x4 X[4]` uniform array (MonoGame DesktopGL, macOS) | `mul(v, X[s])` on a matrix-array uniform produced a wrong GL program; with the use compiled out all was fine (the optimiser removed the uniform) **[hit]** | Upload the matrices as `float4 R[16]` rows (`p.x*R[4s]+p.y*R[4s+1]+p.z*R[4s+2]+R[4s+3]`), set with `Vector4[]`; bisect by returning a constant at the top, then adding one use at a time |
| Build warnings CS0618 for `LogMessage`/`LogImportantMessage`/`LogWarning`, and `ContentImporter(string)` (MonoGame 3.8.6 pipeline) **[hit]** | The logger methods and the single-string importer constructor are deprecated | `context.Logger.Log(string.Format(...))`; `[ContentImporter(new[] { ".glb" }, ...)]`; keep the build at 0 warnings so new ones stand out |
| A live `--sim` crashes or hangs while every still renders **[hit]** | `DrawUserPrimitives` with an empty/<3-vertex array (a particle with alpha < 1 produced no quad) | Guard every user-primitive draw; always finish a step with a live `--sim` without `--shot` |
| Agent "milestone" shots end up in `_tuning/` and the devlog loses them **[hit]** | Brief did not name the destination | Briefs must say: milestone -> `Documentation/reference/` next free `mono-NN`; tuning -> `_tuning/`; the lead promotes strays |
| A shell tool call hangs on `cat >> file` with no heredoc **[hit]** | stdin waits | Use heredocs or the Write/Edit tools |
| `find ~/.nuget` takes minutes **[hit]** | Whole package cache scanned | Read the compiler's deprecation message instead of searching packages |
