# Lighting notes (HDR + post + lights)

- HDR scene RT: HalfVector4 works on macOS GL (fallbacks Rgba1010102 -> Color with HdrScale in PostProcess.cs). Materials output linear HDR; PostTonemap.fx does exposure, AgX, saturation 1.4, dither; PostBloom.fx 3-level bloom (threshold 2.5 exposed, intensity 0.35).
- Env from environment.tres: ambient (0.258824,0.203922,0.152941), fog (0.4,0.25098,0.101961) density 0.002, exposure 4.5, saturation 1.4. Tuned (not real) values: ambient x0.2 and 25% saturation, fog density 0.0006, exposure 4.8, fake key light 0.3. Godot's values are for a scene that also has VoxelGI; raw values over-brighten and wash out.
- Lights: Tools/GodotExtract/export_lights.py -> out/lights.json (20 KHR lights, world space); 8 nearest to the camera/player per frame, inverse-square with smooth 40 m window, intensity = colour*100 (guess; Godot's cd conversion unverified).
- Tuning env vars: TPS_AMB, TPS_AMBSAT, TPS_KEY, TPS_LS, TPS_FOG, TPS_EXP, TPS_BT, TPS_BI. Debug: TPS_DBG, TPS_SKIPNULL, TPS_SKIPALPHA. Flag --noplayer for level-only shots (the mono-02 camera sits inside the player).
- OPEN: blob shadow (BlobShadow.cs) not visible in the player shot; spawn Y or floor height under the player is wrong, needs a ground ray cast. Result still sepia/monochrome orange, floors too bright, little contrast; needs per-area ambient (irradiance probes) rather than one constant. No MSAA on the HDR target. Alpha parts are drawn unsorted. Teal strips over-saturate.

## Pass 2 (hue/contrast, cheap occlusion, blob shadow)

Metric: `python3 -I Tools/ImageStats/imagestats.py --crop <png>` (mean RGB/luma, luma std, HSV saturation, avg hue, 4x4 luma grid); `Tools/ImageStats/run_shot.sh out.png [args]` runs one shot. Tuning shots are in Documentation/reference/_tuning/.

| frame | luma | luma std | sat | avg hue |
|---|---|---|---|---|
| godot-02 (target, level) | 0.207 | 0.153 | 0.587 | 33 |
| mono-05 (pass 1) | 0.446 | 0.115 | 0.421 | 36 |
| i1 (hemisphere amb, fog start, gamma 1.2) | 0.270 | 0.121 | 0.374 | 34 |
| i2b (+fog 0.3, sat 1.8) | 0.202 | 0.095 | 0.487 | 33 |
| i3a (LS 1.0, key 0.6) | 0.209 | 0.094 | 0.497 | 33 |
| i6a / mono-07 (S-curve 0.6, exp 3.4) | 0.219 | 0.113 | 0.536 | 32 |
| godot-03 (player frame, loose) | 0.243 | 0.170 | 0.474 | 32 |
| mono-06 (pass 1) | 0.411 | 0.097 | 0.444 | 34 |
| mono-08 (final) | 0.137 | 0.092 | 0.439 | 29 |

Level frame ends within target on luma (+6%), saturation (-9%), hue; luma std is 26% under (target 20%). The player frame is a different view of a darker room, so only loosely comparable (it is darker than godot-03 by design of the room, not tuned further).

Derived values (defaults in SceneLighting/PostProcess, all overridable by TPS_* env vars):
- Ambient is a hemisphere: sky (0.062,0.054,0.042) up, ground (0.022,0.018,0.014) down, lerp by (N.y*0.5+0.5)^2. Godot's 0.2588/0.2039/0.1529 comes from sky/probes we lack, so it was not used directly; ours is ~4x darker and nearly neutral.
- Warmth comes from lights (LightScale 1.0), fake key light (x0.6), fog and emissives. Reflection env is neutral grey (metals stay grey), only the horizon is warm.
- Fog: distance based, starts at 15 m (FogStart), density 0.015, colour = environment.tres fog x0.3; near surfaces untouched, far hangar goes amber.
- Emissive: x0.6, saturation x0.7, clamp 6 HDR (teal strips were over-saturated).
- Tonemap: exposure 3.4 (was 4.8), AgX, saturation 1.8 (post-AgX, compensates the near-neutral albedo), black point 0.015, gamma 1.2, S-curve 0.6 (mid contrast).
- Cheap occlusion: normal-map cavity term (bevels/grooves get 45%..100% ambient) multiplied into ORM AO, plus the dark ground hemisphere. SSAO NOT done: the HDR target depth is a renderbuffer that cannot be sampled in MonoGame GL, so it would need an extra depth/MRT pass.
- Alpha-blend items sorted back to front by item origin (coarse; per-part sorting not done).
- Ground: GroundProbe (Core/Game) tests level.scene.json box/cylinder colliders. Under the spawn the collider top is -1.200 but the player/visual floor is at -1.077, so the blob snaps to the feet when within 0.25 m of the collider top. Blob quad sits 0.12 m above that (visual floor is ~0.1 above the collider; lower values are hidden by depth) with depth bias.
- Blob: BlobShadow.fx never produced visible pixels on DesktopGL (its draw ran; cause unknown, 1 attempt, then the fx was bypassed). BlobShadow now uses BasicEffect + a procedural radial alpha texture; measured luma under the feet drops 52 -> 42 (-19%), a soft subtle shadow on a dark floor. BlobShadow.fx is unused.

Open: luma std slightly low (flat fog haze + uniform dark near geometry; real GI/probes would add variation); alpha sort per item origin only; no MSAA on HDR target; iteration count slightly exceeded the 6-run cap (debug runs for the blob).

## Pass 3 (contrast, 2026-11-17)

Reference framing `--noplayer --cam 64.8,0.7,78.8,55,-12` vs godot-02 (crop stats): baseline (pass 2) luma 0.219 / std 0.113 / sat 0.536 / hue 32 -> final (mono-22) 0.206 / 0.141 / 0.615 / 31 (target 0.207 / 0.153 / 0.587 / 33): luma -0.5%, std -7.8% (was -26%), sat +4.8%. Remeasured independently with Tools/ImageStats.

| # | change | luma | std | sat | hue |
|---|---|---|---|---|---|
| 0 | baseline | 0.219 | 0.113 | 0.536 | 32 |
| 1 | ambient x1.8, floor ambient gain 0.35 | 0.269 | 0.116 | 0.437 | 32 |
| 2 | key light direction (0.7,-0.35,0.62), key x2.5 | 0.252 | 0.139 | 0.544 | 32 |
| 3 | key x3.0, exposure 2.9 | 0.231 | 0.136 | 0.562 | 32 |
| 4 | cavity floor 0.25 | 0.229 | 0.137 | 0.564 | 32 |
| 5 | S-curve 0.85, exposure 2.8 | 0.205 | 0.140 | 0.617 | 31 |
| 6 | final defaults in code | 0.206 | 0.141 | 0.615 | 31 |

- What worked: Godot lights walls and leaves floors darker; our fake key light pointed straight down, which lit floors. It now travels (0.7,-0.35,0.62) so surfaces facing the player light up. Ambient reshaping (iteration 1) did not help: floors are lit by the key and local lights.
- The 32 lightProbes in level.scene.json hold only name + position (no irradiance/SH): Godot's baked colour lives in lightmaps we do not have, so per-area ambient from probes is NOT possible. Still no per-area ambient variation.
- Defaults changed: PostProcess exposure 3.4 -> 2.8 (TPS_EXP), S-curve 0.6 -> 0.85 (TPS_SCURVE). New `SceneLighting.ApplyLevelGrade()` (called from LevelRenderer ctor, level only): key colour x3.0 (TPS_KEY), key direction (TPS_KEYDIR), cavity floor 0.25 (TPS_CAVMIN), emission tuning rescaled by 3.4/exposure. New shader params `AmbShape` (TPS_AMBWALL/TPS_AMBFLOOR, defaults 1,1, unused) and `CavMin` in PbrCommon.fxh. MenuBackdrop.Draw saves/restores SCurve and forces 0.6 (TPS_MSCURVE) so the menu is unchanged (verified: menu shot stats identical to mono-20).
- Guard frames: player frame mono-23 luma 0.192 / std 0.134 / sat 0.599 (old 0.218 / 0.122 / 0.518); aim/fire frame 0.205 / 0.156 / 0.586 (old 0.229 / 0.138 / 0.516). Robot area frame (view near robot marker 1) also reads clearer, red robots stand out.
- Open: the key is a world-space stand-in tuned for the spawn view (other views light differently); a bright grazing specular glare on the floor ahead of the spawn view (visible in mono-22/mono-23) from the new key direction; shadows still out of scope; shader edits need `touch PbrLit.fx PbrLitSkinned.fx` when only the .fxh changes (full content rebuild after a long gap is ~4 min, shader-only ~15 s).

## Pass 4 (image shape, 2026-11-17) - PARTIAL

Why: pass 3 matched global stats of a level-only crop but the real start frame (default `--shot`, mono-23) vs godot-02 still differed: structure dark brown instead of light grey, near-monochrome orange, heavy fog, grey-white player. Metric now = 4x4 luma grid MAE + RGB/sat grid vs godot-02 (`imagestats.py --crop --ref godot-02-level-start.png <png>`, now prints a mean-RGB/sat grid).

| | grid luma MAE | luma std | luma | sat | hue |
|---|---|---|---|---|---|
| godot-02 | 0 | 0.153 | 0.207 | 0.587 | 33 |
| mono-23 (pass 3) | 0.0637 | 0.134 | 0.192 | 0.599 | 31 |
| mono-24 (pass 4) | 0.0487 (-24%, target -50% missed) | 0.174 (+14%, target +-10% missed) | 0.230 (+11%) | 0.377 (worse) | 42 |

Visual (independently judged against godot-02): structure is now light grey with bright edges, the player is cream, there is a teal bounce on the left floor bevels, less orange haze. Still wrong: near floors/road too black (Godot: dark grey/olive-brown), grate slats at the bottom white where Godot's are dark olive, no yellow/green hazard stripes, no teal/yellow structure through the windows (red bleeds in instead), right-edge crate too bright, robot red and player yellow slightly over-saturated, floors too grey in the colour grid (sat 0.2-0.3 vs 0.65-0.77).

Changes (all in `SceneLighting.ApplyLevelGrade()`, level only, env overrides): fog 0.006 from 25 m (TPS_FOG/TPS_FOGSTART); key x2.6, colour (1,0.85,0.62), direction (0.8,-0.2,0.6), key gain on floors 0.25, key spec x0.5, near-camera key + flat-ambient fade (TPS_KEYNEAR 0.1 over 14 m); flat ambient 0.35; albedo gain 1.7; metals fully diffuse (MetalDiff 1); spec floor 0.1; env reflection x0.3; a hand-placed fake teal bounce point light (TPS_TEAL "-5,4,0.3,1" relative to the spawn, negative side = left; no teal lights exist within 40 m of the spawn view in lights.json); player tint (1,0.82,0.5) via `lighting.PlayerTint` in PlayerRenderer (no near fade for the player); exposure 2.8 -> 2.4 (SceneLighting emissive rescale + PostProcess TPS_EXP). New shader params in PbrCommon.fxh: MetalDiff, SpecMin, KeySpec, AmbFlat, KeyFloor, AlbGain, KeyNear, EnvGain (neutral defaults, so the menu is unaffected: menu stats identical to mono-20).

Findings: bright bottom rows come from slat side faces lit by the key and ambient, not specular and not point lights. The key is an infinite warm light, so far structure turns orange (Godot: teal/grey). A distance-from-camera fade on key and flat ambient is the only generic way found to get Godot's dark near field and bright far structure. Next ideas (unmeasured): warm saturated ambient on up-facing surfaces; slat/grate underlay darkening; hazard-stripe colour from the lower layer; per-area ambient; real shadows.


## Effects polish (2026-11-17)

Scope: laser beam shader, real smoke/sparks/embers, bullet omni light. Files: `Core/Content/Effects/Fx.fx` (techniques Beam, Smoke, Barrel), `Core/Rendering/FxSpec.cs` (Godot constants + keyframe tracks, unit-tested), `CombatEffects.cs`, `SceneLighting.SetDynamicPoints`.

Godot facts (read from the .tscn/.gdshader, not the plan docs):
- Laser: `LaserShader.gdshader` (noise = texture(noise, UV*uv_scale + (offset_uv_x,0)).r; discard if noise < cutoff_value; EMISSION = color1*energy; vertex: last ring clipped to -clip, XY scaled by traversal_scale). Material `ShaderMaterial_p5m1n` red_robot.tscn:44-55: uv_scale (15,0.5), clip 8, color1 (1,0,0). Shoot animation tracks (red_robot.tscn ~10296-10360): traversal 0/0.3/1.5, energy 1/5, cutoff 1 -> 0.55 -> 0 -> 0.55 -> 1, offset_uv_x 0 -> 0.3 -> 0, all linear; ported verbatim in `FxSpec.BeamParams` (test checks keys). RayMesh sits 1.25 m along the cannon direction.
- Bullet OmniLight3D (bullet.tscn:~478): colour (0.125,0.627,1), energy 1.5, range 1.5, attenuation 2; explode animation: energy 1.5 -> 3.0 @0.25 -> 0 @0.7, range 3 -> 0 @0.85. Ported (`FxSpec.BulletLight`) and fed into the lights set: up to 3 transient lights take priority over the nearest-static selection (`SceneLighting.Apply`, per-light range in LPos.w). Gain `TPS_BULLETLIGHT` (default 6) is a guess for Godot-energy to our intensity scale.
- Bullet hit (bullet.tscn Blast/*): Smoke = 250 blue_myst quads, 0.6 s, sphere 0.1, vel 0..2 + radial 1, scale 0.01..0.2 x linear curve, alpha 0.56*0.5 fading; LightParticle = 40 cyan emissive balls r 0.03, 1.5 s, explosiveness 0.84, sphere 0.3, vel 0..3, gravity -9.81; BlastSparks = 10 spheres, 0.3 s, and the process material sets NO initial velocity (Godot default 0), so they do not fly: the old flying white sparks were invented and are replaced by one bright dot.
- Red robot: LaserEmber 200 particles / 2 s / randomness .85 / gravity (0,0.8,0) / colour ramp (1,.68,.68) -> black @0.45, emitting from 2.25 s; barrel Smoke 3 particles, 1.5 s, explosiveness .5, from 1.55 s, `smoke.gdshader` dissolve (ported as technique Barrel). Quirk: red_robot.gd:122-126 sets LaserEmber position.z / emission_box_extents.z but the node is in the bone's frame where the beam runs along Y, so in Godot the embers probably scatter sideways, not along the beam; we place them along the beam (intent).

Unverified / assumed: raymesh.mesh is a compressed binary, so the tube radius 0.2 m (x traversal 0.3..1.5) and length 8 m (UV.x 0..1) are ASSUMED; the FastNoiseLite noise textures are replaced by a tileable 3-octave value noise; the barrel-smoke plane size (0.5 half-extent) is assumed; glow quads for the 0.03 m ember/ball spheres are 3.5x larger to stand in for bloom; floor bounce of LightParticle (GPU collision) not ported; unshaded smoke colour gain `TPS_SMOKEGAIN` 1, beam gain `TPS_BEAMGAIN` 4 untuned (no tuning loop was run).
Verified: shots mono-34 (synthetic beam via `TPS_BEAMTEST=<shoot clock>`, a full-open tube at clock 2.25) and mono-35 (hit at age 0.13 s: blue smoke cloud, bullet light on the surroundings); menu backdrop stats identical to mono-20.
Not done: forklift exhaust particles + SpotLight3D, muzzle particles, laser impact_effect.tscn (embers/smoke at the hit point), BuildupParticles proper (a single glow stand-in), partial-cutoff (stripe) beam frames not looked at, a real robot-fired beam shot (robot needs ~9 s of sim and a level-matched approach; not reached).

### Effects part 2 (2026-11-18)
Godot facts: impact_effect.tscn (red_robot.gd:128-130 instantiates it at col.position): animation "blast" emits LightRays 0..0.2, BlastCenter 0..0.3 (0.2 s life, red sphere emission 5), Blast 0.2..0.5 (0.3 s, scale 1.2 -> 5, white -> red), ExplosionEmbers 0.2..0.5 (50, default life 1, explosiveness .9, gravity (0,-2,0), no initial velocity), SmokeParticle 0.3..0.6 (20, life 1, sphere r 1, gravity (0,2,0), scale 0 -> 1, red alpha 0 -> red -> grey -> alpha 0). player.tscn:551 ShootParticle: 10 spheres r 0.003, 0.3 s, explosiveness .79, gravity (0,-3,0), no velocity. red_robot.tscn:10675 BuildupParticles: 64, 0.4 s, emitting 0.2..1.0 s (track 12), sphere surface r 1, radial accel -10, tangential 1. bullet.tscn:416-428 LightParticle: rigid collision, bounce 0.6, friction 0.03. Built as in STATUS "effects part 2". Beam gain default 4 -> 1.2 after viewing the peak frame (flat white before). Shots: Documentation/reference/_tuning/e2-*.png. Not done: forklift exhaust + SpotLight3D, LaserCenter; sizes of BlastMesh_Sphere/LightRaysMesh assumed.

## Depth precision (2026-11-17)
Symptom: shimmering dark blotches on pale walls in the spawn area. Cause: material-less "-colonly" collision-proxy primitives of props.glb were drawn as the grey fallback material over the real props (coincident layers), not a precision limit. LevelRenderer now skips material-less parts (`TPS_DRAWNULL=1` draws them again). Debug aids: `TPS_SKIPMAT=<substr>` (skip by material/model), `TPS_DUMPWALL=1` (list nearby draws + level extent), `TPS_NEAR`, `TPS_FAR`. Metric: crop MAE vs TPS_NEAR=1.5 render: 38.7 before, 0.01 after (4 yaws). Near 0.05/Far 300 kept (Godot values); no depth bias needed. Shots mono-36/37.

## Pass 5 (floors and slats, 2026-11-17)

Finding: the "white grate slats" are NOT the grate (`floor_grate_wholes`, Godot alpha scissor 0.98, texture has no alpha channel, so a scissor does nothing) but the lower ribbed layer under it: material `tile_steel` (no textures, albedo white, metallic 1, roughness 1). Our pass-4 `MetalDiff 1` turned that pure-white metal into a white diffuse surface; Godot reflects a dark environment off it (dark olive with yellow-green). Found with `TPS_SKIPMAT=tile_steel` (white layer gone) after `TPS_SKIPMAT=floor_grate` showed it was the layer below. Alpha scissor (`transparency 2`: floor_grate_wholes, container4) is now supported in the shader (`MatFlags.y`, `TPS_NOCUT=1` disables) although it changes no pixel in the default shot.

Changes: `LevelRenderer` multiplies the albedo of `tile_steel` by `SceneLighting.SteelTint` (0.10, 0.10, 0.04; TPS_STEELR/G/B); new shader param `AmbWarm` (PbrCommon.fxh) = warm saturated ambient on up-facing surfaces, (0.30,0.19,0.07) x TPS_AMBWARM (default 0.12), set in `ApplyLevelGrade` only (menu unchanged, verified identical to mono-20).

| default shot vs godot-02 | grid luma MAE | luma | std | sat | hue |
|---|---|---|---|---|---|
| godot-02 | 0 | 0.207 | 0.153 | 0.587 | 33 |
| fresh baseline (HEAD, after colonly fix) | 0.0421 | 0.215 | 0.172 | 0.401 | 40 |
| AmbWarm 1.0, steel 0.22 | 0.0963 (floors far too orange) | | | 0.6+ | |
| AmbWarm 0.3, steel 0.16 | 0.0499 | 0.236 | 0.159 | 0.558 | 37 |
| AmbWarm 0.12, steel 0.10 (final, mono-38) | 0.0444 | 0.215 | 0.160 | 0.505 | 39 |
| + KeyFloor 0.1, AmbWarm 0.2 | 0.0465 | 0.222 | 0.159 | 0.536 | 37 |
| + AmbFlat 0.22 | 0.0528 | 0.190 | 0.143 | 0.535 | 39 |

Result: the luma MAE target (<= 0.035) was NOT met (0.0444, slightly worse than the 0.0421 baseline); luma, std and saturation (0.505 vs >= 0.5) are in range. Visually (mono-38 vs godot-02) it is clearly closer: slats are dark olive with yellow-green showing between them, floors dark warm brown instead of black. The remaining MAE comes from framing (Godot's player is larger and the view closer), a too-bright mid-ground diamond-plate walkway and right-hand floor (cells row 3-4, right), and the brighter cream player. Still wrong: no teal/yellow through the windows (red bleeds in), red crates/red hazard stripes far away, cyan bevel fringes on near floors, hazard stripes only appear as the slat underlay. Guards: aim frame luma 0.271 -> 0.270, sat 0.440 -> 0.507; robot-area frame luma 0.228 -> 0.230, sat 0.443 -> 0.529; menu identical, physics 27 PASS failures=0, ui 8/8, audio 7/7.

## Planned: Pass 6 before release (user decision 2026-11-17)
Another lighting pass is required before release. Leftovers from pass 5: windows red instead of teal/yellow, mid-ground diamond-plate walkway and right-hand floor too bright, cyan bevel fringes on near floors, player paler than Godot's yellow-tan, hazard stripes on the far rail missing, red crates too bright, grid luma MAE 0.0444 (target <= 0.035), check other untextured white metals in Materials.json. Take a fresh baseline first (the scene changed after the -colonly skip) and re-check once real shadows are in.

## Shadows (2026-11-17)
Godot: no directional light; casters are SpotLight3Ds (see STATUS 2026-11-17 shadows for file:line). Port design: ONE orthographic shadow map for the fake key light (2048 R32F, 64 m box following the player, shifted 8 m along the camera, view translation snapped to texels), depth-only pass (ShadowDepth.fx) of level items inside the box + player + robots, receivers = PbrLit/PbrLitSkinned "Shadow" techniques (3x3 PCF, normal offset 1 texel x (0.5+1.5 sin), bias 0.06 m, edge fade). Shadow light = key heading at 40 deg elevation (TPS_SHELEV); shadow darkens key fully and ambient by TPS_SHADOWAMB 0.6, only where the surface faces the light. Bias TPS_SHBIAS/TPS_SHNOFF. Settings > SHADOWS (default ON); `TPS_SHADOWS=0` forces off; blob shadows remain when off.

| 1280x720, no vsync, 300 frames x3 (`--bench 300`, CPU+GPU via 1-pixel backbuffer readback) | avg ms | p95 ms |
|---|---|---|
| OFF | 19.99 / 19.94 / 19.95 | 25.8 / 25.9 / 25.9 |
| ON | 20.62 / 20.73 / 20.76 | 24.7 / 24.6 / 26.0 |
(+3.7%, 970 caster draws of 2914.)

| default shot vs godot-02 | grid luma MAE |
|---|---|
| shadows off (pass 5) | 0.0444 |
| ON (amb 0.6, elev 40) | 0.0411 |
| ON amb 1.0 | 0.0584 (overhung deck turns black) |
| ON amb 1.0 elev 25 | 0.0579 |

Menu: the menu SpotLight3D (menu.tscn:126-128) gets a perspective map (100 deg, 1024, robot only caster); robot self-shadow applies to that spot's slot (ShadowSlot), the floor uses MenuFloor.fx with the lightmap without its fake blob, darkened 90% in shadow with PCF spacing 4 texels (TPS_MSHDARK, TPS_MSHSOFT). Result mono-39-shadow-menu: soft shadow to the screen-left like godot-01; robot torso darker than Godot. Level shots: mono-40/41/42; shadows visible mostly as rail/overhang shadows on the deck and darker slats; the player's own floor shadow is faint.
Not done: level spot-light shadows, debris, bullet light, forklift spot; higher-quality filtering (kept 3x3 PCF level, wider kernel on the menu only).

## Spot shadows (2026-11-18, WORKING, default ON; 2026-11-17 notes below are superseded)
Design (code in place, `TPS_SPOTSHADOWS=1`): the 3 level SpotLight3Ds nearest to the player (of the 13 in lights.json, outer cone 22.5 deg) each get a perspective map (1024, R32F, 4 tiles in one 2048 atlas, fov 2 x (outer + 4 deg), near 0.3, far 30), cast by level items inside the cone frustum, the player and the robots; receivers darken only that light's term (never ambient); 8-tap Vogel PCF (`SPOT_TAPS` in PbrCommon.fxh, 9 = old 3x3 grid, 25 = 5x5), normal offset + slope bias scaled by the receiver's texel size, edge and far fade. Switching between lamps: hysteresis 1.5 m on the nearest-3 choice, 0.35 s eased blend, plus a distance fade 14..26 m (the headless test shows 0 selection flips for +-0.1 m moves). Lamps are static, so these maps cannot shimmer with the camera.
Update 2026-11-18: root cause was the float4x4[4] uniform array (now float4 SpR[16]). Cost --bench 300 (avg ms): key map 20.8; spot 1024/8 taps 27.0 (+30%); spot 512/4 taps 23.5 (+13%). Default shot MAE 0.0444. Spot is the default, TPS_SPOTSHADOWS=0 = key map. Old text: the fragment program with the spot code produces one flat NaN-like colour on macOS GL (see STATUS "spot shadows"), so there is NO cost table, no before/after shots and no key-light decision. Default stays the key map (cost table above, MAE 0.0411). At the default spawn the lamps are 24 m or more away and mostly point away, so even working spot shadows would barely change that shot; the lamp-area shot (Spot.012/013 at 41.3,-4.9,78, axis -x; `tp:33,-3.5,77.5,-90;wait:1.5;look:90,-8`) is the place to judge them.

### Effects part 3 (2026-11-18)
Forklift exhaust (flying_forklift.tscn:50-113, 4 x 20 CPUParticles3D, world-space, 0.5 s, speed 3, gravity +6 y, additive alpha 0.176, ramp blue -> light blue -> lavender -> transparent, scale 0.3 -> 1), forklift SpotLight3D (colour (0.44,0.75,1), energy 12, range 50, angle 50; shadow off by flying_forklift.gd:8) and robot LaserCenter (red_robot.tscn:10725-10735, 1 sphere r 0.1, 1.5 s, Curve_8fal6, emitting 0.2 s) built as in STATUS "effects part 3". Light conversion = the bullet OmniLight one (energy x 6 -> colour x 72), range as the 4th-power window, joined into the nearest-8 set (focus within 70 m). Binary/unread: LaserCenter.material, dot.png. Shots: Documentation/reference/_tuning/e3-*.png. Not done: spot projector texture, exact spot_attenuation, real Godot comparison.

## Reference note (2026-11-18): newer Godot captures are darker
The user supplied two screenshots from a recent Godot build (godot-05/06, 1920x1080, not video-compressed). They are clearly darker and more saturated than the YouTube frames godot-01..04 we calibrated passes 1-5 against: deep orange-red bounce light everywhere, near-black shadows, high contrast, strong emissives (blown reactor disc), bronze rim light on the player. Lighting pass 6 must be re-baselined on these (and on new same-spot captures), not on godot-02 alone; the bullet blast ball in godot-06 is also a reference for the hit effect. Open: the positions of these captures are unknown (need a matching view in the port: ask for the player position or capture at known spots).

## Pass 6 (2026-11-18, partial: re-baseline on Godot 4.4.1)
Menu check first: default (shadows ON) menu stats are identical to mono-39 (luma 0.147, sat 0.143), so the spot-shadow work did NOT regress it (read side by side: clear cast shadow to the lower left, soft pool, robot cool grey-white). Versus godot-07 ours is still brighter (luma 0.147 vs 0.109) with a wider, lighter floor pool; not changed (MenuBackdrop untouched; TPS_SHADOWS=0 stats unchanged).
Level: godot-08 (4.4.1, luma 0.114, sat 0.65, hue 24) is much darker/warmer than godot-02. Grid luma MAE vs godot-08 / vs godot-02:
| variant | vs 08 | vs 02 |
|---|---|---|
| pass 5 defaults | 0.1001 | 0.0444 |
| AMBFLAT 0.2, KEY 1.8, player tint | 0.0732 | 0.0562 |
| + EXP 1.9 | 0.0589 | 0.0731 |
| + KEYC 1,0.6,0.3, player tint 0.4,0.14,0.08 (NEW DEFAULTS, mono-54) | 0.0560 | 0.0775 |
| + KEY 2.4, AMBFLAT 0.12 | 0.0591 | 0.0907 |
| AMBFLAT 0.1, AMBWARM 0.2 | 0.0624 | 0.0993 |
The two references conflict (Godot 4.4.1 is ~2x darker), so the godot-02 MAE target (<= 0.035) can no longer be met together with godot-08; the godot-02 number rose by design. New defaults: exposure 1.9 (PostProcess; emission compensated by 3.4/EXP), KEY 1.8, key colour (1,0.6,0.3), AMBFLAT 0.2, PlayerTint (0.4,0.14,0.08); SPOT_RADIUS 1.5 -> 2.5 (wider PCF softness, same tap count, bench 23.35 ms avg vs 23.5 before). Stalled after 3 iterations (<5% gain on godot-08), so no further tuning.
Not done: per-area coloured light (teal towers vs orange reactor), spot-mode ambient darkening, windows teal/yellow, walkway, cyan bevel fringes, hazard stripes, red crates, white metals. Reactor views (godot-11/12) and the standing-light view could not be matched: tried tp:24,-8.2,0,-90 / 9,-8.2,0,-90 / -34,-8.2,0,90 / -18,-8.2,0,90 (reactor point light is at the origin), all landed in other rooms; the user must supply the player position. Lamp view (mono-55) shows working spot shadows on a dark floor, comparable in spirit to godot-13 (the player is orange there, Godot's is pale pink: Godot's player colour is random per spawn, so the tint is a guess). Player still too saturated orange under the lamp.
Shots: mono-54 (default start), mono-55 (lamp, spot shadows), mono-56 (menu, shadows on); tuning in _tuning/p6-*.png.
