# Pitfalls: symptom -> cause -> fix

All found in the Shooter port. **Fixed** = verified against a capture or a check; the rest are noted as such.

## Rendering / particles
| Symptom | Cause | Fix |
|---|---|---|
| Particle looks like an opaque square with black around it | Unity texture has `alphaUsage: 2` (alpha from gray scale); generator ignored it | Convert in ContentGen (alpha = luma) and record `alphaFromGrayScale`. Fixed. |
| Unlit white quad where Unity shows nothing (e.g. a glow) | The GameObject is **inactive** in the prefab; the runtime drew it | Honour the active flag (`Placement.Active`) when spawning. Fixed. |
| Opposite: effect shows in Unity though its GameObject is inactive (enemy "!", steam, sparks) | A script enables/plays them (`OnDetectVfx`, `RandomHitSparks`) | Ignore the active flag for systems the scripts reference. Fixed (from a capture). |
| Effect works in `--shot` but never in play | Non-looping, PlayOnAwake-off systems were dropped by `AutoRemoveFinished` before first use | `KeepWhenFinished` on systems an owner will `Play()` later; idle ~30 frames in shot mode like play does. Fixed. |
| Effect attached to a bone sits at the model origin | Baked `Placement.World` ignores bones (skeleton is inside the FBX) | Use the scene hierarchy's local pose under the bone (`NodeWorld(name)`); do not recompose from the bind pose (double counts). Fixed. |
| Particle tiny vs Unity (steam cloud 2.2x too small) | `scalingMode = Local`: only the system's own scale counts, parents ignored | Bake `localScale`; world matrix keeps rotation+translation, scale = LocalScale. Verified for the weapon steam; *assumed* for Turret/pickup/jetpack systems (eyeball them). |
| Mark floats too low | Renderer **Pivot** (`m_Pivot`) parsed but not applied | Shift quad by pivot x size along its own axes. Fixed. |
| Stretched billboards invisible | Negative velocity scale flips winding, back face culled | Flip the axis instead (`length < 0`: swap direction and side). Fixed. |
| Trails smear off a moving weapon | Ribbons stored in world space; module's `worldSpace` was off | Honour `worldSpace`: store local points and transform by the current matrix. Fixed. |
| Flash hidden behind viewmodel | Drawn by the main camera, Unity draws weapon-layer systems with the weapon camera | Weapon-layer pass with depth cleared; muzzle flash without depth test, steam/cylinders with. Verified. |
| Particle quad too large | Renderer max particle size (fraction of viewport) | Clamp like Unity (0.5 of half the viewport height). Verified. |
| HUD/pickup colours slightly off | UI blends in linear in Unity | See `conventions.md`. |

## Materials / emission
- Eye colour from a MaterialPropertyBlock is gamma -> linear in Unity; the material asset's value is not what is drawn. Verified.
- Overheat gradient as authored matched better than converting. Verified (within 16 levels).
- **Shared eye material** between two enemies of one type would share colour (only one exists; *known limitation*).
- One-time effects at first frame: Unity's `LastTimeDamaged` starts at 0 so bots flash for 0.2 s at scene start; ours starts at -inf (*deliberate deviation*).

## Content pipeline
- ContentGen rerun rewrites unrelated textures: diff and revert. See `pipeline.md`.
- Mixer values and prefab AudioSources are not in the baked data: constants in code with their source commented.
- `--extra` is needed for any prefab no scene references (pickups placed by script, loot prefabs).
- Floor/wall submesh order for OBJ can be ambiguous: name-match first, then confirm against a capture.

## Gameplay
- **Spawn inside geometry:** baked start (editor value before gravity) may be inside a slab; PhysX depenetration moved it +0.495 up and 0.101 along z. Reproduced by resolving the shallowest contact first at spawn only (calibrated on one observation, *assumed* elsewhere; normal movement recovers deepest-first).
- **GroundCheckDistance** in the scene (1) differs from the script default (0.05); always take scene values.
- CharacterController stand-in: sub-step moves (<= min(0.08 m, radius/4)) so zero-thickness walls cannot be skipped; step offset, slope limit and skin width reproduced from PhysX's documented rules, not compared (*assumed*).
- Prefab authored oddities must be reproduced: projectile tip 10 m ahead of its root (hits up to 10 m ahead), model offset 0.768 above the HoverBot root (hitbox, weapon root and health bar are relative to it). The crosshair going red a bot-height above the mesh was this. Add a test: hitbox centre lies inside the drawn mesh bounds.
- **Hitboxes** that move (hover sphere, turret head on a bone) must follow their node; static triangle boxes from the editor pose silently drift. A flat baked hierarchy put the turret head 1 m too low.
- NavMesh has no stand-in in MonoGame: a straight-line steering agent with wall sliding stalls behind obstacles (*known limitation*; fine on baked patrol paths).
- Trigger enter fires once per entry, not while standing in the volume; a spinning box means the player can re-enter.
- `Mathf.Lerp` clamps; ported ratios must be clamped to 1 (fall damage).
- `GameFlowManager.GameIsEnding` makes `TryAtack` return false before aiming; check such guards when enemies behave differently around death/win. Found from a capture.
- Script execution order that Unity leaves undefined (e.g. patrol path assignment vs `Start`) needs a stated assumption.

## UI
- CanvasScaler "scale with screen size, match width": canvas px = screen width / reference width. Resolve RectTransforms (anchors, anchoredPosition, sizeDelta, pivot) in Unity's y-up canvas space.
- `CrosshairImage` is the child image, not the parent; the weapon's crosshair data replaces the scene sprite.
- World-space canvases: order background/fill by render queue, not depth (1 mm apart sorts wrongly when the fill is off-centre). Fixed.
- Menu navigation: first arrow/Tab/Enter only selects the default button.
- Window resizing not handled beyond reallocating layers (*known*).
- OpenGL driver warning in first frames once an off-screen HUD layer is attached; skipping the layer for 30 frames hides it (*cause unknown*).
