# Shared planning memory (all planning agents)

Purpose: avoid clashes when agents run at the same time. Read this at the start and before you finish.

## Rules
1. **Write only your own plan file** (`0N-*.md`) and **your own section below**. Never edit another agent's plan file or section.
2. To change the shared decisions, add a line to "Proposed changes" and the lead (main session) will resolve it.
3. Append-only: add to your section with Edit; do not rewrite the file with Write.
4. Don't create other files in `Documentation/planning/` without telling the lead in your final reply.
5. No code, no builds, no changes outside `Documentation/planning/`.

## File ownership
| File | Owner |
|---|---|
| 01-scene-and-asset-extraction.md | Agent 1 (assets) |
| 02-rendering-materials-lighting.md | Agent 2 (rendering) |
| 03-player-camera-animation.md | Agent 3 |
| 04-enemies-ai-gameplay.md | Agent 4 |
| 05-audio-ui-packaging.md | Agent 5 |

## Shared decisions (set by the lead; user-approved)
- Desktop (DesktopGL) only. No Android/iOS/Windows projects.
- Layout mirrors `RacingGame`: `Core/`, `Desktop/`, `Assets/`, `Documentation/`, `PipelineExtension/`, `scripts/`, `.sln`.
- Like-for-like look and feel. Vertical slice first: player movement, 3rd-person camera, shooting, one enemy type, the level.
- Content through the MonoGame content pipeline (.mgcb), with a skinned-animation pipeline extension.
- Source demo is MIT: keep `LICENSE.md` and credit it in the README.
- A `godot-to-monogame-port` skill will be written at `ThirdPersonShooter/.claude/skills/godot-to-monogame-port/`; each agent ends its plan with "lessons for the porting skill".
- Namespace/project name: `ThirdPersonShooter` (proposed; see changes below).

## Interfaces between domains (append what you PRODUCE or EXPECT)
Format: `[agentN] produces|expects <thing> — <format/units/convention>`

[agent1] produces `Materials.json` — res:// material name -> {albedo/orm/normal/emission texture content names, albedoColor, metallic, roughness, emission energy, uv1 scale/offset, cull, transparency, ORM channel selectors (AO=R, rough=G, metal=B), shader ref for ShaderMaterials (listed, not translated)}.
[agent1] produces `AssetMap.json` — res:// path -> content name, type, `srgb` flag per texture, loop flag/3D info for audio.
[agent1] produces static `Model` XNB via GlbImporter + GodotStaticModelProcessor (tangents generated, no UV flip, no winding flip, node names kept) and `SkinnedModelContent` XNB (joint-pruned bone palette, `Byte4` joints + `Vector4` weights, clips resampled at 30 fps from a whitelist, `-noexp` clips dropped).
[agent1] produces `level.scene` (JSON dev, binary later): instances with composed world matrices, colliders (box/sphere/capsule/hull), KHR point/spot lights, reflection + light probes, spawn Marker3D transforms, audio volumes, environment settings, prefab sections (player, red_robot, bullet, door).
[agent1] expects from agent 2 — decision on sRGB handling (ColorSRgb vs shader decode), lightmap/EXR reuse yes/no, and a vertex layout for static + skinned meshes (I will emit Position, Normal, Tangent, UV0, optional UV1; skinned adds Byte4 joints, Vector4 weights).
[agent2] produces `PbrLit.fx` (+ PostTonemap, PostBloom, particle/beam fx), HDR RT pipeline, `RenderTargets` helper, blob-shadow quads, CPU particle system (plan file 02).
[agent2] expects from agent1 — static + skinned vertex layout as agent1 proposed (Position, Normal, Tangent, UV0, Byte4 joints, Vector4 weights) accepted; UV1 not needed; per-mesh-part material id from `Materials.json`; level lights (KHR point/spot) with colour/energy/range; emissive material list so agent2's tool can build proxy lights; particle `.tres` curves/gradients as data if feasible (low priority).
[agent2] expects from agents 3/4 — skinned bone matrices (`inverseBind * pose`) as `Matrix[]` up to 72 per draw; animation/camera supply camera position for the player dither fade; physics ground ray-cast callback for blob shadow Y/normal.
[agent2] answers agent1 — (a) NO UV2/lightmaps and NO level.exr conversion: the level has no LightmapGI; baked GI is VoxelGI (`giprobe_data.res`, binary, unusable). (b) sRGB: keep `Color` + `srgb` flag, decode albedo/emission in the shader (`pow 2.2`). (c) 145 bones: prune to weighted joints as you planned; shader palette limit is ~72 `float4x3` per draw on SM3/GL, so if the pruned palette is >72, split mesh parts by bone subset in the processor (or tell me the pruned count).
[agent1] expects from agents 3/4 — whitelist of animation clip names used by the player/red_robot AnimationTrees.
[agent1] expects from agent 5 — which audio buses/reverb are feasible in MonoGame, music as Song vs SoundEffect.

[agent3] produces `IPlayerView` (pos, ModelWorld, Matrix[] skin palette, camera pos/View/Projection fov 75 near 0.05 far 300, CrosshairAlpha, FadeAlpha, MuzzleWorld), events OnShoot/OnJump/OnLand/OnFootstep, `GroundProbe` for blob shadows, `ProjectileManager` (20 m/s, 5 s, r 0.17), shared `CollisionWorld` (reuse Shooter/Core/Physics, custom capsule motor, no Bepu).
[agent3] answers agent1 — animation clip whitelist (player.glb): Idle, Idlecombat, running_nogun, strafe_left, strafe_right, strafe_front, strafe_back, jump_2_upwards, jump_4_falling, AIM-Up, AIM-Down; optional: running_gun, jump_5_hardlanding, eyes. KEEP bones root (root motion!), hand.R, neck.001, upper_arm.R even if unweighted; keep root track; expose Robot_Skeleton scale 0.803991 and loop flags.
[agent3] answers agent2 — scripts never change Environment; only a black full-screen fade when player y < -17 (alpha=(−17−y)/15). Camera FOV 75 fixed (no aim zoom), far 300.
[agent3] expects from agent4 — IHittable.Hit(point) for bullets; enemy collision shape/layer for projectile sweeps; player has no health (Player.Hit() only adds camera trauma 0.75).
[agent3] expects from agent5 — crosshair drawing (uses CrosshairAlpha), InputCapture.Set(bool) for pause, sound hooks to events.

## Conventions claimed (coordinate, units, naming). First writer wins; conflicts go to "Proposed changes".

[agent1] Coordinates: Godot and MonoGame are both right-handed, Y-up, camera looks -Z, 1 unit = 1 m. **No axis mirror, no scale bake, no 180 deg turn** on import. Quaternions copied (x,y,z,w). glTF/Godot "model front is +Z" only matters for gameplay facing code.
[agent1] .tscn `Transform3D` holds the Basis as three rows then origin; converted to XNA row-vector `Matrix` by transposing (unit-tested with forklift 2). World matrices are composed in the baker, `child * parent`.
[agent1] Winding CCW (MonoGame default CullCounterClockwise), UV origin top-left with NO FlipUVs, normal maps are OpenGL-style (+Y up), tangent W = handedness sign.
[agent1] Textures keep Godot ORM packing (R=AO, G=roughness, B=metallic); colours stored as plain `Color`, `srgb` flag in AssetMap.
[agent1] Naming: content names = path under `res://` without extension, `/` replaced by `_` (lowercase snake, e.g. `level_textures_structure_column_albedo`); models `structure`, `props`, `core`, `lights`, `player`, `red_robot`; scene `level`. Root namespace `ThirdPersonShooter`; content tools in `Tools/`, extension in `PipelineExtension/`.

[agent2] Rendering conventions: linear HDR scene-referred, tonemap (AgX, exposure 4.5) at the end; shader parameter names = Godot names (`AlbedoColor`, `Metallic`, `EmissionEnergy`, ...); `Matrix` row-vector; all effect params set from C# every frame (GL init = 0); camera vertical FOV 75, near 0.05, far 300 (assumed). Agrees with agent1's coordinate, winding, UV and normal-map conventions (no conflicts).

## Open questions for the lead / other agents
Format: `[agentN → agentM|lead] question`

[agent2 → lead] Can the user supply Godot reference screenshots (menu, level, laser hit)? Needed to calibrate AgX/ambient since baked VoxelGI is approximated.
[agent2 → agent1] Does `lights.glb` contain KHR_lights_punctual nodes (count/colour/energy)? Level has no Light3D nodes in .tscn.
[agent2 → agent3/4] Do scripts change the Environment at runtime (glow, damage flash)? Level env has glow/SSAO/SDFGI disabled.

[agent1 → agent2] Do level meshes need UV2/lightmaps (level.exr 56 MB), or do we drop baked GI and rebuild lighting? Decides EXR conversion work.
[agent1 → agent2] 145-bone player palette: can your skinned shader take 145 x 3 vec4 (or do I prune to weighted joints)?
[agent1 → lead] Texture size cap (proposed 2048, flag to disable) and whether source PNGs/glbs (~300 MB) stay in repo or fetched by script.
[agent1 → agent5] Are .ogg Songs and OpenAL reverb buses feasible? Else I convert ogg to wav/mp3.

## Proposed changes to shared decisions

(none yet)

## Token ledger (lead maintains)
| Agent | Model | Status | Tokens |
|---|---|---|---|
| 1 assets | sonnet | done | 82,422 |
| 2 rendering | sonnet | done | 86,265 |
| 3 player/camera/anim | sonnet | done | 96,226 |
| 4 enemies/AI/gameplay | sonnet | done | 84,947 |
| 5 audio/UI/packaging | sonnet | done | 87,314 |
| skill writer | sonnet | done | 60,507 |
| extractor | sonnet | done | 109,867 |
| glb importer | sonnet | done | 99,344 |
| first frame | sonnet | done | 72,016 |
| textures+PBR | sonnet | done | 100,935 |
| skinned player | sonnet | done | 158,782 |
| lighting pass 1 | sonnet | done | 129,574 |
| lighting pass 2 | sonnet | done | 137,149 |
| player controller A | sonnet | done | 146,461 |
| player controller B1 (anim + root motion) | sonnet | done | 123,739 |
| player controller B2 (aim + shooting) | sonnet | done | 165,619 |
| robot C1 (red robot) | sonnet | done | 216,412 |
| menu D1 (menu + settings + UI kit) | sonnet | done | 200,720 |
| pause D2 (message box, pause, quit) | sonnet | done | 90,572 |
| audio E1 (music, SFX, 3D sound) | sonnet | done | 189,972 |
| loading F1 (loading screen, lazy boot) | sonnet | done | 116,667 |
| lighting pass 3 (contrast) | sonnet | done | 101,613 |
| lighting pass 4 (image shape) | sonnet | partial | 115,108 |
| docs G1 (README + skill write-up) | sonnet | done | 115,511 |
| parity H1 (crosshair, fall-fade, 4 robots) | sonnet | done | 122,809 |
| parity I1 (forklifts; doors do not exist) | sonnet | done | 135,399 |
| parity J1 (explosion debris) | sonnet | done | 176,408 |
| audio E2 (reverb zones + mono 3D panning) | sonnet | done | 116,613 |
| effects polish (lighting block 1) | sonnet | done | not reported by harness; ~63 tool calls of 80, 7 renders of 8 (6 shots + menu) |

## Agent 4 entries (append-only)
[agent4] FINDING: the demo has NO navmesh/NavigationAgent3D; robots steer straight to the player via animation root motion. The player has no health, no win/lose, no pickups. Multiplayer code is dropped (always server).
[agent4] produces `Physics` module (Raycast, SweepSphere, MoveAndSlide, layer flags Level=1/Player=2/PlayerOnly=4), `World` fixed 60 Hz tick + TimerQueue, `IDamageable`, `ProjectileSystem.Spawn`, `TriggerVolume`, `Services` locator.
[agent4] expects from agent1 — red_robot clip list with root-bone track kept (idle, turn_left, turn_right, walk, 5 aim poses, hit1-3), door clip doorsimple_opening; level.scene colliders with layer bits, 4+4 spawn markers, door box trigger 8.85x6.21x11.07.
[agent4] expects from agent3 — AnimationController with crossfade transition, bone-mask layer, 2D blend space, one-shot, root-motion deltas; Camera.AddTrauma(float); Player capsule/ShootOrigin/AimTarget.
[agent4] camera trauma values: shoot 0.35, hit 0.75, robot laser 13.0 (0.1 s delay). Bullet speed 20 m/s, life 5 s, fire cooldown 0.4 s.
[agent4 → lead] Q1: shared custom collision module (my proposal) vs BepuPhysics for agent3's player capsule too?
[agent4 → agent2] Answer: scripts never touch the Environment at runtime; only per-material shader params (laser clip, debris emission_cutout) and light envelopes (bullet explosion, ShootLight).

## Lead decisions (user-approved)
- Texture size cap: 2048 (keep a flag to disable). Source PNGs and glbs stay in the repo (no fetch script).
- Physics: custom capsule motor + CollisionWorld (no BepuPhysics), reusing the Shooter port's C#.

## Agent 5 entries (append-only)
[agent5] answers agent1 — music = Song (ogg, standard importer), wavs = SoundEffect, no conversion; real reverb buses NOT feasible on DesktopGL (dry audio + zone stub, software reverb post-slice).
[agent5] answers agent4 — robot walk and cannon sounds are AudioStreamPlayer3D playback tracks inside the walk/shoot animations (red_robot.tscn track 67 / track 2); agent1 should dump audio tracks (clip + key times), agent4 raises `OnAudioKey(name)`.
[agent5] produces `Audio` (Play2D/Play3D/PlayMusic/UpdateListener/SetZone), UI module (SpriteBatch + FontStashSharp, virtual 1920x1080), `InputCapture.Set(bool)`, `GameSettings`, `CommandLine` flags; expects `IPlayerView.CrosshairAlpha/FadeAlpha`, animation audio keys, audio zone volumes in level.scene.
[agent5] proposes: tps-demo-master moves to gitignored `Assets/Source/` fetched by `scripts/fetch-demo.sh` (pinned SHA); Tools/ and PipelineExtension/ at repo root.

## Lead decisions, round 2 (user-approved 2026-11-17)
- Reference screenshots saved in `Documentation/reference/` (see README there). Agent 2's calibration open question is answered; frames are compressed video, so calibrate by eye.
- Add a pause overlay (the Godot demo has none).
- Main menu: REMOVE "Play Online". Entries: Play, Settings, Quit. After Play, the player chooses Single Player or Wi-Fi/LAN Multiplayer, like RacingGame. LAN multiplayer is NOT in the vertical slice; leave a note/hook only (the Godot demo's multiplayer code is dropped for now). Later: model it on RacingGame's LAN code (see memory project_racinggame_lan).
- Fonts: pipeline SpriteFonts for the slice (no FontStashSharp). Needs a slab/mono title font and a PT Sans-like UI font (check the Godot project's fonts/licences).
- Menu scene needs one real shadow-casting spot light on the robot (not blob shadows); agent 2's plan assumed blob shadows only for characters in the level.

## Lead notes, 2026-11-17
- LICENCE CORRECTION: the demo is NOT all MIT. Code is MIT; assets and music are CC-BY 3.0 (attribution + indicate changes); some materials from GameTextures.com. Root LICENSE.md is a copy, README credits all of it.
- Scaffold done and builds (Release): ThirdPersonShooter.sln, Core, Desktop, PipelineExtension, Tools/, scripts/. `tps-demo-master/` stays in place (agent 5's move to gitignored Assets/Source is rejected per user: PNGs stay in repo).
- User /usage after planning: session 15%, week 3%.

## Correction, 2026-11-17
- WINDING: agent 1/2's "CullCounterClockwise default" is wrong for this pipeline. glTF fronts are CCW, XNA fronts are CW, and the importer does not flip indices, so render glTF-derived meshes with `RasterizerState.CullClockwise` (verified in the first frame, Core/Rendering/LevelRenderer.cs). Any mesh we build ourselves must pick one winding and match.
- First frame (Documentation/reference/mono-01-level-start.png): 1484 instances, 3328 draw calls, 2.30M tris; geometry correct and upright; floors blown out (no tonemap/saturate), no textures yet, camera framing differs from the Godot reference, no frustum culling.

| 2026-11-17 | zfight-fix agent | LevelRenderer.cs (skip material-less "-colonly" parts, TPS_SKIPMAT/TPS_DUMPWALL/TPS_DRAWNULL), PlayerConstants.cs (TPS_NEAR/TPS_FAR) | root cause was coincident collision-proxy draws, not depth precision; crop MAE 38.7 -> 0.01; mono-36/37; no depth bias added |
| 2026-11-17 | lighting pass 5 agent | PbrCommon.fxh (AmbWarm, alpha scissor), PbrMaterial.cs (AlphaCut), LevelRenderer.cs (tile_steel tint, TPS_NOCUT), SceneLighting.cs (AmbWarm/SteelTint) | slats = tile_steel white metal; luma MAE 0.0421 -> 0.0444 (target 0.035 missed), sat 0.401 -> 0.505; mono-38; menu identical, tests green |
| 2026-11-17 | shadows agent | ShadowMap.cs, ShadowDepth.fx, MenuFloor.fx, PbrCommon.fxh (ShadowFactor), PbrLit*.fx (…Shadow techniques), LevelRenderer/PlayerRenderer/MenuBackdrop/SceneLighting, GameSettings (Shadows), SettingsScreen, game `--bench N`, PhysicsSelfTest (fit test) | one ortho key map (2048 R32F, 64 m, texel-snapped) + menu spot map; +4% frame cost; default shot luma MAE 0.0444 -> 0.0411; mono-39..42 |
| 2026-11-17 | spot-shadows agent | SpotShadows.cs (new), ShadowMap.cs (BeginTile/Frustum), PbrCommon.fxh (SpotShadow, Shade flags), PbrLit*.fx (…Spot techniques), SceneLighting/LevelRenderer/PlayerRenderer/PostProcess (viewport reset), game (TPS_SPOTSHADOWS=1 experimental), PhysicsSelfTest (spot fit/selection test) | per-lamp perspective shadow maps coded + headless-tested, but the spot fragment program renders flat garbage on macOS GL; stopped at the 10-build cap, default = key map unchanged (MAE 0.0411, menu = mono-39); next: bisect SpotShadow() (STATUS 2026-11-17 spot shadows) |
| 2026-11-18 | effects part 2 agent | CombatEffects.cs (Impact, buildup, ShootParticle, floor bounce, BeamGain 1.2), FxSpec.cs, RedRobot.cs (DebugForceShoot), Simulation.cs (`rshoot`, `robot:ahead,D,yaw`), game (RobotLaser -> impact, GroundY), PhysicsSelfTest (+2) | robot-fired beam seen; impact/buildup/shoot particles/bounce built; bench unchanged (22.19 -> 21.89 ms); 31 PASS, ui 8/8, audio 7/7, grid MAE 0.0411, menu = mono-20; forklift exhaust/spotlight NOT done; shots _tuning/e2-* |
| 2026-11-18 | effects part 3 agent | CombatEffects.cs (DrawForklifts, SetForkliftLights, LaserCenter), FxSpec.cs (forklift/LaserCenter data), LevelScene.cs (ForkliftRoots/ForkliftWorld), SceneLighting.cs (SetDynamicSpots, merged into nearest-8), game (level time local, forklift hooks outside the `!_fly` block), PhysicsSelfTest (+5) | exhaust + spot light + LaserCenter seen in shots, live sim 40 s clean, guards unchanged (MAE 0.0411, menu = mono-20) |
| 2026-11-18 | spot shadows bisect agent | PbrCommon.fxh (SpVP float4x4[4] -> SpR float4[16] rows, local `lit` renamed, SPOT_TAPS 4, radius 1.5), SpotShadows.cs (Tile 512, row upload), game (spot maps now DEFAULT; TPS_SPOTSHADOWS=0 = key map only) | root cause: float4x4[] uniform array in PbrCommon breaks the macOS GL program; spot shadows render; +13% cost; live run clean | switch pixel diff not measured |
| lighting pass 6 (partial) | sonnet | done, ~45 tool calls, 6 render iterations (~17 renders), tokens not measured | n/a |
| robot-robot + debris contacts (parity) | sonnet | done, ~38 tool calls, 5 build/test iterations, tokens not measured | n/a |
| 2026-11-18 | rumble agent | RumbleService.cs (new), CameraShake (TraumaAdded), GameSettings (Rumble), SettingsScreen (RUMBLE row), Simulation (Stepped), game (wiring, startup log, TPS_RUMBLETEST), UiSelfTest (+1) | ~30 tool calls, 3 build iterations, tokens not measured; ui 9/9, physics 40, audio 7/7, bench ~23.4 ms; mono-58; hardware feel unverified |
