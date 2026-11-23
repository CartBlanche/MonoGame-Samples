# RacingGame Lighting Plan: time of day, headlights, tunnels, night driving

Branch: `racinggame-lighting` (from `main`, 2026-11-22). Desktop (DesktopGL, macOS) only: **no Android, Windows or iOS builds** until the user asks.

Written so that one orchestrating session can hand workstreams to 1 to 3 agents and have them start immediately. Everything in "Findings" was read from the source on 2026-11-22; anything not read is marked **(unverified)**.

---

## 1. Goals and non-goals

**Goals**

1. A single place that owns all light state (`TrackLighting`), replacing the scattered per-material and per-screen light values.
2. **Time of day**: day (today's look, unchanged), dusk, night, driven by one value, with sky tint, sun direction, ambient and diffuse levels, fog, glow, lens flare and shadow strength following it.
3. **Car headlights** (and tail/brake lights) that really light the road, terrain and objects ahead at dusk and night.
4. **Tunnels** that are properly dark inside, with the car's headlights doing the work, plus tunnel lamps.
5. **Street lamps** (`Laterne`, placed by the `Laterns` road helper) that glow and light the road at night.
6. A **Lighting** setting (Off / Low / High) so slow machines can drop the cost.

**Non-goals (decided already, do not reopen without the user)**

- Shadows were a non-goal for the lighting work itself. (An earlier version of this line said the user wants hard shadows; that was a misreading, corrected 2026-11-23: the user wants natural, environment-driven shadows, and the shadow prototype in `Shadow-Prototype-Plan.md` is built on that.) Background: the user reverted a soft/ortho shadow experiment on 2026-10-27 (derived from the first-person Shooter's `ShadowMapper.cs`, not ThirdPersonShooter); the committed shadow code (`4bc9294a`, perspective light, depth from clip `.w`, static const biases) is the classic baseline.
- No deferred rendering, no GI, no baked lightmaps, no light probes.
- No headlight shadow maps.
- No Android/iOS/Windows builds or fixes.
- Replays, highscores and physics must be unaffected (lighting is visual only).

---

## 2. Findings: how RacingGame is lit today

| Area | What it does | Where |
|---|---|---|
| Light | One directional light, `BaseGame.LightDirection` (normalized on set), reset to `LensFlare.DefaultLightPos` when a race starts; `CarSelection` and `LanLobbyScreen` set their own | `Graphics/BaseGame.cs:138`, `GameScreens/GameScreen.cs:42`, `CarSelection.cs:125,482`, `LanLobbyScreen.cs:107` |
| Colours | Per-material constants (`Material` ambient/diffuse/specular). Landscape 88/234/33 ("bright day"), tunnel 182/80/64 ("mainly ambient, tunnel uses lightmaps"), default 40/210/255. Shaders comment that colours are **pre-multiplied with the light colour** | `Landscapes/Landscape.cs:539`, `Tracks/Track.cs:84`, `Graphics/Material.cs:30` |
| Choke points that set light uniforms | `ShaderEffect.SetParameters(Material)`, `SetParametersOptimizedGeneral()`, `SetParametersOptimized(Material)` (they also clear the `lastUsed*` caches), plus `Model.RenderCar` which handles ambient/diffuse for the car parts itself | `Shaders/ShaderEffect.cs:741,792,825`, `Graphics/Model.cs:483+` |
| Shaders | `NormalMapping.fx` (792 lines, many techniques), `LandscapeNormalMapping.fx`, `LightingShader.fx` (simple, also ShadowCar), `ReflectionSimpleGlass.fx`. Tangent-space normal mapping; the **light vector is computed in the vertex shader** and passed in `COLOR0` (so only one directional light, no per-pixel position) | `Content/Shaders/*.fx` |
| Landscape | 257x257 height grid, 10 units per cell, one big mesh drawn with `DiffuseWithDetail20` | `Landscapes/Landscape.cs:1185` |
| Sky | Cube map (`SkyCubeMap.dds`) multiplied by one `ambientColor`; no gradient, no night | `Shaders/PreScreenSkyCubeMapping.cs`, `PreScreenSkyCubeMapping.fx` |
| Lens flare | Fixed sun position; switched off in tunnels via `Track.disableLensFlareInTunnel` (checked every 10 frames, `IsTunnel(segment)`) | `Tracks/Track.cs:1494-1528`, `Graphics/UIRenderer.cs:430` |
| Shadows | Hard shadow map, generated from the sun direction, post-blurred (`PostScreenShadowBlur`), drawn in `GameScreen.Render` via `ShadowMapShader.PrepareGameShadows()` and `ShowShadows()`; toggle `BaseGame.AllowShadowMapping` | `Shaders/ShadowMapShader.cs:398`, `GameScreens/GameScreen.cs:95,138` |
| Glow | `PostScreenGlow` bloom; parameters are not applied from the `.fx` on GL, they come from `FxDefaultValues` in `ShaderEffect.cs` | `Shaders/ShaderEffect.cs:640` |
| Tunnels | Real geometry: `RoadHelper` type `Tunnel` (start/end segment), `roadTunnelVertices`, material `RoadTunnel`; tunnel top height 7.125 | `Tracks/Track.cs:503+`, `Tracks/TrackVertex.cs:54`, `Tracks/TrackData.cs:178` |
| Street lamps | `Laterne` models placed along the road by the `Laterns` helper at alternating sides; also `Streetlamp*.tga`, `Light.tga`, `TLight.tga`, `Halo.tga`, `Glow.tga` exist in Content | `Tracks/Track.cs:812`, `Landscapes/Landscape.cs:229` |
| Car | `Car.x`, wheels are meshes with 2 parts; glass drawn in a second alpha pass | `Graphics/Model.cs` |

**Consequence:** the current shaders cannot do a headlight at all. A cone of light needs the pixel's world position, and today it only has a per-vertex tangent-space light vector. The landscape mesh is also coarse (10 unit cells), so per-vertex lighting of a headlight cone would be visibly blocky. Headlights therefore require a **per-pixel world-space light term**, which is the one genuinely risky technical item (see 6.1).

### Hard-won lessons from this port (apply to every agent)

From project notes of the earlier render fixes:

1. **`.fx` initial values are NOT applied by MonoGame on GL: every effect parameter starts at 0.** Any new parameter must be set from C# every frame (or added to `FxDefaultValues`), and the shader must behave sanely at 0 (zero lights, zero fog density). Never design a parameter where 0 means "black screen".
2. `const static` globals in `.fx` can break on GL (already removed from `LandscapeNormalMapping.fx`; `LightingShader.fx` still has `const static lightDir/ambient...`, which is why ShadowCar behaves differently).
3. Clip-space `z` differs between D3D and GL; use `.w` for linear depth (the shadow fix).
4. Values set from code for `static const` in a VS may not reach the shader on GL; keep such constants literal in the `.fx`.
5. Uniform arrays are fine on DesktopGL in other projects of this repo (the ThirdPersonShooter PBR shader passes 8-light arrays) but a comment in `ShadowMapShader.cs` says a `FilterTaps` array hit a **GLSL std140** problem here. Prefer a handful of individually named `float4` parameters, or test the array form first. Treat arrays as **(unverified)** in this codebase.
6. Compare against a baseline with measurements, one variable per run. Do not theorise.

---

## 3. What to borrow from ThirdPersonShooter (and what not to)

Reference: `ThirdPersonShooter/Core/Rendering/SceneLighting.cs`, `ShadowMap.cs`, `SpotShadows.cs`, `Core/Content/Effects/PbrCommon.fxh` (not read for this plan, so shader internals are **(unverified)**; read it before writing the HLSL).

| TPS idea | Use in RacingGame? |
|---|---|
| One `SceneLighting` object with `Apply(effect, focus)` that owns ambient, key light, fog and the light list | **Yes**: becomes `TrackLighting` |
| Hemisphere ambient (sky colour above, ground colour below) | **Yes**, cheap and better than flat ambient; replaces the flat ambient only when Lighting is High |
| Exponential fog with colour and start distance | **Yes**: hides pop-in, carries the dusk/night mood |
| Light array packed as `LPos`/`LCol`/`LDirI` with point-vs-spot encoded in `w`, nearest-N selection around the focus | **Yes**, with **N = 4** (2 headlights + 2 nearest lamps) |
| Dynamic lights merged with static ones, a few slots reserved for transient lights | **Yes** (headlights are the reserved dynamic slots) |
| Spot shadow atlas, hysteresis, fades | **No** (shadows are a non-goal), but copy the **hysteresis and ease-in** idea for lamp selection so lights do not pop |
| Texel-snapped ortho shadow map | **Not adopted** (shadows are a non-goal here). TPS's shadow code has not been tried in RacingGame; the 2026-10-27 experiment that was reverted came from the Shooter, not TPS |
| Per-feature env-var tuning (`TPS_*`) | **Yes**, as `RACINGGAME_LIGHT_*` while tuning; remove or fold into settings before merge |
| Settings toggle plus a measured frame cost | **Yes** |
| HDR/PBR pipeline | **No** |

---

## 4. Target architecture

```
                        +------------------------+
 TimeOfDay (0..1) ----> |  TrackLighting         |  Core/Graphics/TrackLighting.cs
 Preset keyframes       |  - sun dir/colour      |
 Tunnel blend (0..1) -> |  - ambient sky/ground  |
 Headlights on/off ---> |  - fog colour/density  |
 Lamp list (track) ---> |  - sky tint, glow, flare|
                        |  - light list (<=4)    |
                        +-----------+------------+
                                    | Apply(effect)  (called from the ShaderEffect choke points)
              +----------+----------+-----------+-----------+
              v          v          v           v           v
        NormalMapping  Landscape  Lighting   Glass/Sky   PostScreenGlow
            .fx          .fx       Shader.fx   shaders      + ShadowMap
```

### 4.1 `TrackLighting` (C#, new)

Single static-ish owner (`BaseGame.Lighting`, like `BaseGame.UI`). Responsibilities:

- `TimeOfDay` (0 = noon, 0.5 = dusk, 1 = midnight is one option; see decision D2) and an evaluated `Preset` (struct of colours/scalars, interpolated between keyframes).
- `TunnelBlend` (0 outside, 1 deep inside), smoothed over ~0.5 s of game time, driven by the track segment (section 5, WS3).
- `HeadlightsOn` (auto: on when `Preset.NightFactor > threshold` or `TunnelBlend > 0.3`, with hysteresis; manual override key).
- `ResolvedAmbient(Color material)`, `ResolvedDiffuse(Color material)`: the multiplier hooks (see 4.3).
- A fixed-size light list: `Light { Vector3 Pos; Vector3 Dir; Vector3 Color; float Range; float InnerCos; float OuterCos; bool Spot; }` with `SetCarLights(carMatrix)`, `SetLamps(IReadOnlyList<Light>)`, nearest-N selection with hysteresis around the camera focus.
- `Apply(ShaderEffect fx)`: sets the uniforms listed in 4.2 if the effect has them (`Parameters["x"] != null`), mirroring TPS `Apply`.

### 4.2 Shader parameter contract (the interface all workstreams share)

Defined in a new include `Content/Shaders/TrackLighting.fxh`, included by the four lit shaders. **Fixed at WS0; do not rename later.**

| Parameter | Type | Meaning | Value that means "off / today's look" |
|---|---|---|---|
| `LightCount` | float | number of active lights (0..4) | 0 |
| `LPos[4]` or `LPos0..3` | float4 | xyz world position, w = range | unused |
| `LCol[4]` | float4 | rgb = colour*intensity, w = cos(outer) for spots, -3 for points | unused |
| `LDir[4]` | float4 | xyz = direction, w = cos(inner) for spots, -2 for points | unused |
| `FogParams` | float4 | x = density, y = start distance, z = reserved, w = reserved | density 0 |
| `FogColor` | float3 | fog colour | any |
| `AmbSky`, `AmbGround` | float3 | hemisphere ambient multiplier (applied on top of the material ambient) | (1,1,1) both |
| `LightingMode` | float | 0 = legacy path, 1 = per-pixel lights | 0 |

Rules: every new parameter is **additive or multiplicative with neutral 0/1 defaults, and set from C# each frame**. With `LightingMode = 0` the shader output must be **bit-for-bit the same as today** (verify, section 7).

### 4.3 Where the numbers get applied (two paths on purpose)

- **Path A, C# only (time of day v1).** Because material ambient/diffuse/specular are already "pre-multiplied with the light colour", day/dusk/night colour changes can be done by multiplying the colours inside the `ShaderEffect` choke points and `Model.RenderCar` before they are uploaded (`AmbientColor = setMat.ambientColor * lighting.AmbientTint`). Zero shader risk. Also `BaseGame.LightDirection` for the sun, the sky's `ambientColor`, the glow parameters, flare alpha and the shadow `ShadowColor`/strength.
- **Path B, shader (fog, headlights, lamps, hemisphere ambient).** Needs the world-space per-pixel work in 6.1.

This split is what makes parallel work possible: WS1 (time of day) can ship on Path A alone while WS2/WS3 build Path B.

---

## 5. Workstreams, dependencies and parallelism

```
WS0 (serial, first)  Contract + audit + baseline + rig patch
        |
        +--> WS1  Time of day (Path A, C# only)                     [agent A]
        +--> WS2  Shader lighting core (fog, per-pixel lights)      [agent B]
                    |
                    +--> WS3  Headlights + tail lights               [agent B, then C]
                    +--> WS4  Tunnels + street lamps                 [agent C]
        WS5  Night polish: shadows fade, glow, sprites, sky night    [after WS1+WS2]
        WS6  Settings, debug keys, LAN/custom-track time of day      [last]
```

**Safe to run at the same time:** WS1 with WS2 (disjoint files, see ownership table). WS3 and WS4 can run together once WS2 lands, because both only *produce lights* for `TrackLighting` (different files). **Not safe in parallel:** anything that edits the same `.fx` file (WS2 owns all `.fx` edits; others request changes through it), and WS6 against WS3/WS4 (touches `Track.cs`, LAN code).

Max **2 agents at once** (user guardrail). Recommended: A and B in round 1, B continues into WS3 while C starts WS4, then A moves to WS5.

### File ownership (to avoid merge conflicts)

| File / area | Owner |
|---|---|
| `Core/Graphics/TrackLighting.cs` (new), `Content/Shaders/TrackLighting.fxh` (new) | WS0, then WS2 for the `.fxh`; WS1/3/4 only **add** members to `TrackLighting.cs` in their own `partial` file (`TrackLighting.TimeOfDay.cs`, `.Headlights.cs`, `.Lamps.cs`) |
| `Content/Shaders/NormalMapping.fx`, `LandscapeNormalMapping.fx`, `LightingShader.fx`, `ReflectionSimpleGlass.fx` | WS2 only |
| `Shaders/ShaderEffect.cs`, `Graphics/Material.cs` | WS0 (hooks), then WS2 |
| `Graphics/Model.cs` (`RenderCar`) | WS0 (hook), WS3 (light anchor points) |
| `Shaders/PreScreenSkyCubeMapping.*`, `Graphics/LensFlare.cs`, `Graphics/UIRenderer.cs` background, `Shaders/PostScreenGlow.cs` | WS1 |
| `Tracks/Track.cs`, `TrackData.cs`, `Landscapes/Landscape.cs` | WS4 (WS6 later) |
| `Shaders/ShadowMapShader.cs`, `Content/Shaders/ShadowMap.fx` | WS5 |
| `GameSettings.cs`, `GameScreens/Options.cs`, `Multiplayer/*`, `CustomTracks.cs` | WS6 |

---

## 6. Workstream detail

Every workstream: build **Desktop only**; report at each milestone and stop; max 3 attempts per approach; stop and report if the same metric has not improved in 3 consecutive iterations (see section 9).

### WS0: Contract, audit, baseline (one agent, serial, do first; small)

**Tasks**

1. Create `TrackLighting` with the `Preset` struct holding **exactly today's values** as the "Day" preset, and `Apply()` that sets the 4.2 parameters to their neutral values. No visual change.
2. Audit every place that sets ambient/diffuse/specular/lightDir (`grep` for `AmbientColor`, `DiffuseColor`, `SpecularColor`, `LightDirection`, `ambientColor`, `lightDir` across `Core`; there are at least the `ShaderEffect` setters, `Model.RenderCar`, `PreScreenSkyCubeMapping`, `CarSelection`, `LanLobbyScreen`). Write the list into this doc under "Audit results" and route them through `TrackLighting` hooks (still returning the unmodified values).
3. Add `TrackLighting.fxh` with neutral `#if`-free declarations, include it from the four lit shaders, and confirm the shaders still compile on `OPENGL` (MojoShader, ps_3_0) with no output change.
4. Record the **performance baseline**: average frame time over 30 s on the three shipped tracks at the user's usual window size, using a temporary Stopwatch in the capture hook (not committed).
5. Produce the **frame-capture rig as a patch file** (recipe in section 7) so later agents apply the same hooks in a few seconds instead of rewriting them. Store as `Documentation/Lighting-Rig.patch` (decision D6).
6. Capture **baseline frames** (day, three tracks, fixed seed/throttle) into the scratchpad for A/B.

**Acceptance:** A/B frame diff between baseline and WS0 build is **zero** (or max abs channel diff <= 1/255 on > 99.9% of pixels); frame time within noise.

### WS1: Time of day (Path A, C# only)

**Tasks**

1. `Preset` keyframes: Day (today), Golden hour, Dusk, Night. Fields: sun direction, ambient tint, diffuse tint, specular scale, sky tint, glow intensity/threshold, flare alpha, shadow colour/strength, fog colour (stored now, used after WS2).
2. Interpolation (smoothstep between keyframes) and `NightFactor`.
3. Apply through the audited choke points. Sun direction: set `BaseGame.LightDirection` and `LensFlare` position from the same value (the `LensFlare.RotateSun` helper exists).
4. Sky: **source a CC0 night cube map** (decision D5) and blend it with the day cube by `NightFactor` (two cube samples in `PreScreenSkyCubeMapping.fx`, tinted by the preset; dusk is a mix plus tint). Candidates found 2026-11-22, all **to verify before use** (license on the asset page, and whether the map has a ground/horizon that would clash with the desert track): Poly Haven `rogland_clear_night`, `clarens_night_01`, `clarens_night_02` (CC0, equirectangular HDRIs, 1K to 24K; need conversion); OpenGameArt CC0 sets (kurtk84 "space skybox 1", "space skyboxes 0", Screaming Brain Studios procedural skyboxes with night variants; faces may need swapping or rotating); Kenney skybox pack (CC0, unconfirmed). Conversion for equirect sources: `ffmpeg -i in.png -vf v360=e:c6x1:w=1024 faces.png` (or per-face output), tone-map to 8-bit, then build a `TextureCube` the same way `SkyCubeMap.dds` is built today (check `RacingGameContent.mgcb` for its importer and processor; DXT may fail to load on some platforms, the existing code falls back to no sky). Prefer a map with stars and a faint horizon glow and no ground. Record the chosen source, URL, license and author in `Documentation/` (credits file). Fallback if nothing fits: multiply the existing cube by a sky tint (deep blue-black). Stretch (separate commit): procedural night cube with stars and a horizon gradient blended with the day cube. Keep the DXT load-failure fallback working (`skyCubeMapTexture == null` returns early today).
5. Glow: night wants a lower highlight threshold so lamps and lights bloom. Change through `FxDefaultValues`/the glow setter, not by editing the `.fx`.
6. Debug-only env var `RACINGGAME_TIMEOFDAY=0..1` and keys `[` / `]` (debug builds or env gated) to scrub time.
7. **Running clock (test mode).** `TrackLighting.ClockSpeed` (game-time minutes of day per real second; 0 = frozen, the shipped behaviour). Env vars `RACINGGAME_TIMEOFDAY` (start value) and `RACINGGAME_CLOCK=<seconds per full day>` (for example 120 = a full cycle in two minutes, loops 0..1..0 as noon, dusk, night, dawn, noon). Debug keys: `]` cycle speed (0, 1x, 10x, 60x), `[` pause or resume, plus the scrub keys above. The clock only advances `TimeOfDay`; everything else reads the evaluated `Preset` every frame, so it exercises the real interpolation, headlight hysteresis, lamp selection and tunnel blending while driving. The clock must run on **render/game time, never touch physics, replays or LAN state**, and is hidden from the Options screen (decision D2 stays: fixed time per track in shipped play; a slow cycle option may come later as a toy mode). Also use it in the rig: `RACINGGAME_CLOCK=60` plus the 3-second frame capture gives a free day-to-night filmstrip per track.

**Acceptance:** at 0.0 identical to baseline; the running clock completes a full loop on each track with no pops, black or white frames; frames at 0.3 / 0.6 / 1.0 on each track look plausible (agent Reads the PNGs side by side); no black or white screen at any value; shadows still hard-edged; lens flare fades out by dusk.

**Out of scope here:** headlights, fog (needs WS2), per-pixel anything.

### WS2: Shader lighting core (the risky one; start immediately in parallel with WS1)

**6.1 Per-pixel world-space lighting.** Today the vertex shader writes a tangent-space `lightVec` into `COLOR0`. Required change (for the techniques actually used in-game; audit which of the ~15 `NormalMapping.fx` techniques the cars, road, tunnel and objects use and only convert those):

- Pass world position to the pixel shader (one extra `TEXCOORD`), and either the tangent basis (3 rows) or transform the sampled normal-map normal into world space in the PS.
- Loop `LightCount` times: Lambert + attenuation (range based, smooth falloff) + spot cone (`smoothstep(outerCos, innerCos, dot(-L, spotDir))`), accumulate into the diffuse term; add a cheap specular term only for the car/road techniques.
- Check **interpolator count** (ps_3_0 limits and MojoShader/GLSL mapping), instruction count and the std140/array caveat (lesson 5) **before** converting more than one technique. Prototype on `LandscapeNormalMapping.fx` `DiffuseWithDetail20` plus the road technique (`SpecularRoad20`) first.
- Gate everything on `LightingMode`: `LightingMode == 0` must take the legacy code path unchanged.

**6.2 Fog.** `lerp(color, FogColor, 1 - exp(-density * max(dist - start, 0)))` at the end of each lit pixel shader and in the sky/background so the horizon matches. `FogParams.x == 0` is a no-op.

**6.3 Hemisphere ambient.** `ambient = lerp(AmbGround, AmbSky, normal.z * 0.5 + 0.5) * material ambient` (world **Z is up** in this game). Neutral at (1,1,1).

**6.4 Glass and the car.** `ReflectionSimpleGlass.fx` and the car paint technique (`SpecularWithReflectionForCar20`) must receive the same light list so the car is lit by its own headlights' neighbours and by lamps. Do not touch the `carHueColor` logic.

**Fallback plan if per-pixel does not fit ps_3_0 on GL:** compute up to 2 light vectors per vertex for the **road and tunnel only** (those meshes are dense along the track) and skip headlight lighting on the landscape except via a cheap projected "light pool" decal; report the limitation to the user rather than silently degrading.

**Acceptance:** (a) `LightingMode = 0` and fog 0: frame diff vs baseline zero; (b) a debug scene with one test spot light placed in front of the car produces a correct cone on road, terrain and a palm; (c) cost: <= +1.0 ms at 1080p (measured, per section 7) with 4 lights on, <= +0.2 ms with 0 lights.

### WS3: Headlights and tail lights

Depends on WS2's contract (can start the C# half after WS0).

**Tasks**

1. Anchor points: two headlight positions and a forward direction in car local space. The car is `Car.x`; **Z is up and the car's forward axis must be verified** from `Player.CarRenderMatrix` and the model rather than assumed (unverified). Store anchors as constants in `Model.cs`, tune by looking at frames.
2. Each frame: transform anchors by `RacingGameManager.Player.CarRenderMatrix` (the same matrix used for rendering, so lights track bodywork, suspension and loopings), push two spot lights to `TrackLighting`. Tuning starting point: inner 12 degrees, outer 30 degrees, range 60 to 90 units (the road is 13.25 wide, so a 30 degree half-angle covers it at about 25 units), warm white, slight downward pitch (about 4 degrees) so the road, not the sky, is lit.
3. Tail lights: red emissive on the rear light meshes/texture while braking (`CarPhysics` knows when braking; read-only access), plus a small red point light behind the car (cheap, optional, off on Low).
4. Light sprites for the lamps themselves: `Halo.tga`/`Glow.tga`/`Lens.tga` already exist; billboard glow at each headlight, depth-tested, additive, scaled by distance. Reuse `LensFlare`-style quad rendering, do not invent a new sprite system.
5. **Other cars (LAN):** remote cars are rendered through `RenderCar(..., carWheelPos, useCarColor)`; headlights for them follow the same global `HeadlightsOn` and their own matrix, so **no protocol change**. Only the nearest remote car's lights consume light slots; distant ones get sprites only.
6. **Ghost/shadow car** (best-lap replay) gets no lights.
7. Headlight auto logic with hysteresis (`NightFactor > 0.35` on, `< 0.25` off, or `TunnelBlend > 0.3`), plus a manual key.

**Acceptance:** at night the road ahead of the car is visibly lit with a soft cone edge, no popping when lights toggle (0.3 s fade), headlights follow the car through hills and loopings; remote car lights appear in a two-instance LAN run (see 7); ghost car unlit.

### WS4: Tunnels and street lamps

**Tasks**

1. `TunnelBlend`: the track already finds the car's segment every frame and checks `IsTunnel` every 10 frames (for the flare). Do the check **every frame from the segment number** (it is cheap: `helperPositions` is small) or smooth between checks, and expose `Track.CurrentSegmentIsTunnel` plus distance to the tunnel mouth. Ease `TunnelBlend` with time (about 0.4 s in, 0.6 s out) so exiting does not snap.
2. Inside a tunnel (blend 1): sun diffuse and sun shadow fade to 0, sky/flare hidden (the flare is already off), ambient drops to a dim level, headlights on. Tunnel material (ambient 182, diffuse 80, "tunnel uses lightmaps") is already ambient-heavy and likely **too bright in a dark tunnel**; scale its ambient by `(1 - 0.8 * TunnelBlend)` rather than editing the texture.
3. Tunnel lamps: place point lights along the tunnel centre line every ~25 units from `Track` data (the tunnel segments are known: `RoadHelperPosition.startNum..endNum`) and register them as lights; nearest-2 selection with hysteresis. Add emissive strips or sprite glows at the lamp positions (tunnel texture is `RoadTunnel.tga`; changing art is out of scope, so use sprites).
4. Street lamps: `Laterne` objects are already generated per road point with known matrices. Register each lamp's light position (top of the pole, to be measured from `Laterne.X`) in a lamp list on `TrackLighting`. At night: glow sprite plus point light; nearest-2 selection around the car with hysteresis and 0.35 s ease in/out (the TPS `SpotShadows` pattern, without shadows).
5. Custom tracks and the track editor share the same `RoadHelper` types, so lamps and tunnels work for custom tracks with no format change.

**Acceptance:** driving through each shipped tunnel (find them via `RoadHelpers` in the three `.Track` files) the screen darkens smoothly on entry and recovers on exit, no flare, headlights on, tunnel lamps visible; at night lamps glow and light the road with no popping as the nearest set changes.

### WS5: Night polish (after WS1 and WS2)

- Shadow behaviour: shadow strength = `sunVisibility` (fade to 0 at night, keep the committed hard-shadow generation; the shadow direction follows the sun through `ShadowMapShader.ShadowLightPos`/`BaseGame.LightDirection`). Optional moonlight at 15 to 25 percent as a second, weaker shadow caster is **not** planned; just fade.
- Moon/stars (procedural), sun disc and lens flare phasing with elevation.
- Building windows (Hotel, Casino, Building*): emissive window texture or a night-only emissive multiplier on those materials. Pure polish; do last.
- Start lights (`StartLight`, `StartLight2`, `StartLight3`) get extra glow at night.
- Re-tune `PostScreenGlow` for night once all light sources exist.

### WS6: Settings, UI, track data, LAN (last)

1. `GameSettings`: `Lighting` = Off / Low / High (Off: legacy path, `LightingMode 0`, no fog; Low: no lamp lights, 2 headlight lights, no tail light; High: everything), and `TimeOfDay` mode = Track default / Day / Dusk / Night. Options screen row in the existing panel-button style (`PanelButtonGroup`); remember the Options layout rules in the earlier notes (window fit, painted labels erased from `OptionsScreenWindows.png`).
2. **Per-track time of day** (recommended, decision D3): optional `TimeOfDay` element in `.Track` (like `Checkpoints` and the metadata fields, see `Track-Format-Spec.md`), absent = Day. Built-in tracks: assign in code (Beginner = Day, Advanced = Dusk, Expert = Night is a suggestion). Because custom tracks are already shared over LAN (protocol 3), **the time of day travels with the track; no protocol bump is needed** if it is part of the file. If it is stored outside the file, the protocol must be bumped and the version-mismatch handling in the LAN notes considered.
3. Track editor (`TrackTool`, `Track-Editor-Plan.md`): add a time-of-day dropdown later; not required for this plan.
4. Highscores are per track, not per lighting, so nothing to do.

---

## 7. Verification method

The agent **can see rendered frames** (project notes). Recipe for the temporary rig (never commit these hooks except as the patch file in `Documentation/`, and keep `RACINGGAME_*` hooks out of normal commits):

1. `SplashScreen.Render`: if `RACINGGAME_AUTOSTART=1`, once `RacingGameManager.AddGameScreen(new GameScreen())`.
2. `CarPhysics` throttle: `Environment.GetEnvironmentVariable("RACINGGAME_THROTTLE") == "1" ||` before the accelerate test (the car drives straight into the rail, identical every run).
3. `BaseGame.Draw`, after `ui.RenderTextsAndMouseCursor()`: `Device.GetBackBufferData(Color[])` into a `Texture2D`, force alpha 255, `SaveAsPng` to `$RACINGGAME_FRAMES/frame_<sec>.png` every 3 s of `TotalTime` from 8 s.
4. New for this plan: `RACINGGAME_TIMEOFDAY`, `RACINGGAME_TRACK` (select built-in track), `RACINGGAME_LIGHTING` (Off/Low/High), `RACINGGAME_CAMPOS` (teleport the car to a segment number, so tunnels can be reached in seconds without driving), and frame-time logging (average ms over 30 s).
5. Build and run: `dotnet build PipelineExtension -c Release`, then build Desktop in Release, then `dotnet run --no-build` for about 50 s from `Desktop/`, kill, Read the PNGs. `ffmpeg` is installed (`hstack`/`vstack` for old-vs-new pairs). macOS: `caffeinate -u -t 2` before launching windows (TPS note).
6. A/B against baseline: `git worktree add --detach <scratch>/wt HEAD`, apply the rig patch, build there, capture, `git worktree remove --force`.
7. Compare numerically too (mean absolute difference per channel via a small Python script run with `python3 -I`), but **always Read both PNGs**; global stats alone were misleading in TPS.
8. LAN check (WS3 item 5): two instances as in the LAN notes (`RACINGGAME_TRACKS_DIR` and `RACINGGAME_PLAYER_NAME`), Release builds.
9. Settings file lives at `~/Library/Application Support/RacingGame/RacingGameSettings.xml`; **back it up** before toggling values in tests.

Acceptance numbers appear in each workstream. A shared rule: the **Day preset must remain visually identical to today** (the user already confirmed that look).

---

## 8. Risks and mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Per-pixel light term does not fit ps_3_0 / interpolators on GL | Medium | Prototype on one technique first; fallback plan in 6.1; report to the user |
| Uniform arrays hit GLSL std140 issues (seen once in `ShadowMapShader`) | Medium | Use named `float4` params, or test arrays first |
| New parameters read as 0 on GL and darken or break scenes | High if ignored | Neutral-at-zero design; set every frame from C#; add to `FxDefaultValues` where needed |
| Landscape mesh too coarse for headlight cone | Medium | Per-pixel on the landscape; light pool decal fallback |
| Frame cost on the user's Mac at 1080p fullscreen | Medium | Measure; Lighting Off/Low/High; 4 light cap; skip lights for far objects |
| Two agents editing one `.fx` | High if unmanaged | Ownership table: WS2 only |
| Time of day makes night unreadable (gameplay) | Medium | Headlight range/strength tuned for playability; minimum ambient floor; user review of night frames |
| Glow at night over-blooms | Low | WS5 retune |
| Replays, highscores or LAN desync from lighting | Low | Lighting reads game state only, never writes physics; per-track time of day in the file |
| `ps_2_0`-style fallback techniques (`...20`) still used on some paths | Medium | WS0 audit lists techniques in actual use |

---

## 9. Rules for agents working from this doc

(From the user's standing guardrails and global rules.)

- **Caps:** each workstream brief carries a tool-call budget and a maximum number of build/render/compare iterations per milestone; max 3 attempts per approach; **stall rule**: if the same metric has not improved meaningfully in 3 consecutive iterations, stop and report what was tried. No open-ended "polish until it matches" loops; fidelity tuning is a separate, user-approved step with a stated budget.
- **Concurrency:** at most 2 agents at a time; prefer cheaper models for implementation (Sonnet or Haiku) and keep a token ledger; ask the user to re-run `/usage` after each milestone and **pause at about 50 percent of the current session window**. Never claim to know the remaining quota.
- **Report at each milestone and stop.** A milestone is the "Acceptance" block of a workstream.
- **Isolation:** each agent uses its own git worktree off `racinggame-lighting` (`isolation: worktree`) and keeps its own build output; the orchestrator merges in order WS0, WS2, WS1, WS3, WS4, WS5, WS6.
- **Do not touch** unrelated dirty files in the repository (`CardsStarterKit/*`, `FirstPersonShooter/`, `TexturedQuad/*`). Stage by explicit path, never `git add -A`.
- **Builds:** Desktop only, Release. No Android, Windows or iOS builds.
- **Commits:** agents do not push, and do not commit unless the orchestrator says so. When committing: **one sentence, one line, no body**, prefixed `[RacingGame]`, about the change only (no lists, reasons, or measurements), and **no `Claude-Session:` line or any other trailer or attribution**. No em dash character (U+2014) anywhere: code, comments, docs, commit messages, chat.
- **Docs:** each workstream appends a short "Verified / Lesson / Open" entry to a changelog section at the bottom of this file (TPS `STATUS.md` style), and moves findings out of "unverified" when it checks them.
- **Dev log screenshots:** the user wants before/after screenshots for blog posts and videos. At each milestone copy the good frames (baseline "before" and the new "after", same track and update count where possible, plus contact sheets) into `RacingGame/Documentation/devlog-screenshots/NN-step-name/` (numbered, descriptive file names, short note in that folder's `README.txt`). The folder is in `.gitignore` and must never be committed; never overwrite or delete earlier numbered folders.
- **Cleanup:** remove or env-gate debug hooks and `RACINGGAME_LIGHT_*` tuning variables before the final merge; keep only the rig patch file.

---

## 10. Decisions needed from the user (recommended defaults first)

| # | Question | Recommendation |
|---|---|---|
| D1 | Scope of v1 | Ship WS0 to WS4 (time of day, headlights, tunnels, lamps); WS5/WS6 polish after a play-test |
| D2 | Time-of-day model | Fixed time per track in shipped play: predictable for racing, easy for LAN, easy to compare lap times. A running clock exists from WS1 as a **test/debug mode** (env var and keys, WS1 item 7); promoting it to a player-facing toy mode is a later, separate choice |
| D3 | Where time of day lives | Optional `TimeOfDay` element in the `.Track` file; built-ins assigned in code (Beginner Day, Advanced Dusk, Expert Night) |
| D4 | Light budget | 4 lights (2 headlights + 2 nearest lamps/tunnel lights) |
| D5 | Sky at night | **DONE 2026-11-22: CC0 stars cube added on top of the existing horizon** (Poly Haven "Rogland Clear Night", see `Credits-Night-Sky.md`); the day cube has no sun, only horizon and ground, so it stays |
| D6 | Rig patch | Keep `Documentation/Lighting-Rig.patch` in the repo so every agent starts from it |
| D7 | Lighting default | High on desktop; Low/Off available |

---

## 11. Milestones

| M | Content | Gate |
|---|---|---|
| M0 | WS0 | Zero diff vs baseline, rig patch, baseline numbers |
| M1 | WS1 | Time-of-day scrub looks right on three tracks, no regressions at Day |
| M2 | WS2 | Test spot light correct; fog works; cost within budget |
| M3 | WS3 + WS4 | Night driving with headlights, tunnels, lamps; no popping |
| M4 | WS5 + WS6 | Polish, settings, per-track time of day, LAN check |
| M5 | Cleanup | Hooks removed, docs and README updated, user play-test |

---

## Audit results and changelog

### WS0 (2026-11-22): contract, audit, baseline. Status: DONE, committed (`349a6456`, `a212b044`)

**Built (Desktop Release, 0 warnings, 0 errors):**

- `Core/Graphics/TrackLighting.cs`: `BaseGame.Lighting`. `Preset` (Day = today's look, all tints 1, fog 0), hooks `Ambient/Diffuse/Specular(Color|Vector4)`, `SunDirection`, `Sky`, `EffectHandles` (cached parameter handles per effect, `Apply` sets `LightingMode`, `LightCount`, `FogParams`, `FogColor`, `AmbSky`, `AmbGround` when the effect has them). `MaxLights = 4`.
- `Core/Content/Shaders/TrackLighting.fxh` (the parameter contract of section 4.2, including `LPos0..3`, `LCol0..3`, `LDir0..3` as named float4s because of the std140 array caveat) included by `NormalMapping.fx`, `LandscapeNormalMapping.fx`, `LightingShader.fx`, `ReflectionSimpleGlass.fx`. Declarations only; the compiler drops unused uniforms, so the C# `EffectHandles` are all null until WS2 uses them.
- Hooks wired into: `ShaderEffect` colour setters and `SetParametersOptimized` (all three of `AmbientColor/DiffuseColor/SpecularColor` plus the direct `ambientColor.SetValue` path), both `lightDir.SetValue` sites, `Model.RenderCar` (car ambient, diffuse and light direction), `PreScreenSkyCubeMapping` (`Sky()`; the sky opts out of the material tint through `ShaderEffect.TintColors`, and `RenderSky()` no longer feeds back its own last colour, which would have applied the sky tint twice).

**Audit results (every place that sets ambient, diffuse, specular or the light direction):**

| Site | Status |
|---|---|
| `ShaderEffect.AmbientColor/DiffuseColor/SpecularColor` setters (via `SetParameters(Material)`) | hooked |
| `ShaderEffect.SetParametersOptimized(Material)` (MeshRenderManager batches, `normalMapping` shared effect) | hooked |
| `ShaderEffect.SetParameters` and `SetParametersOptimizedGeneral` (`lightDir`) | hooked |
| `Model.RenderCar` car ambient/diffuse and `lightDir` (the car uses the model's own `Effect` instances, `cachedEffectParameters`, **not** `ShaderEffect` handles) | hooked for colours and sun; **lighting parameters (fog, lights) are not set on these effects yet: WS2 and WS3 must add them there** |
| `Model.cs` ~line 263: signs and banners get ambient 128 **once at load** | **not hooked: a time-of-day tint will not reach them.** WS1 must re-apply per frame (store base ambient, upload `Lighting.Ambient(base)`) |
| `Material.cs` ~289-302 reads colours back from an effect at load | read only, no change needed |
| `PreScreenSkyCubeMapping` ambient (sky colour) | hooked through `Sky()` |
| `BaseGame.LightDirection` writers: `GameScreen` (reset to `LensFlare.DefaultLightPos`), `CarSelection` (2 sites), `LanLobbyScreen` | unchanged; menus keep their own direction. WS1 must make `GameScreen` use the time-of-day sun and leave the menu screens alone |
| `LightingShader.fx` `const static lightDir/ambientColor/...` (ShadowCar, simple lighting) | still compile-time constants on GL: they do not follow any C# value. Only matters for the ghost car |

**Test rig:** `Documentation/Lighting-Rig.patch` (apply from the repo root with `git apply`; adds `Core/Helpers/Rig.cs` and small hooks in `SplashScreen`, `CarPhysics`, `BaseGame`, `TrackSelection`, `RandomHelper`). Env vars: `RACINGGAME_AUTOSTART=1`, `RACINGGAME_THROTTLE=1`, `RACINGGAME_TRACK=0..2`, `RACINGGAME_FRAMES=<dir>`, `RACINGGAME_EVERY=<updates, default 180>`, `RACINGGAME_EXIT=<frames to capture>`, `RACINGGAME_NOVSYNC=1`, `RACINGGAME_TIMING=1`. With `RACINGGAME_FRAMES` set the time step is fixed at 1/60 s and `RandomHelper` is seeded with 12345, which makes runs repeatable (see Lessons). Never commit these hooks except as the patch file. Helper scripts used (not in the repo): a PNG diff (`MAE`, max, fraction of channel values off by more than 2) and a run wrapper using `perl -e 'alarm N; exec @ARGV'` as a watchdog (macOS has no `timeout`).

**Verified:**

- Baseline (HEAD) vs WS0 frames, three built-in tracks, frames at 180/360/540/720 updates: Beginner MAE <= 0.0001; Advanced MAE 0.126, 0.084, 0.000, 0.002; Expert MAE <= 0.025. Max abs MAE is 0.13 of 255, i.e. noise level (see Lessons). Two WS0 runs against each other: MAE 0.0000.
- Draw CPU time (Expert track, vsync off, average over 900 frames, two runs each): baseline 1.057 / 1.039 ms, WS0 1.027 / 1.047 ms. No change. This is CPU time inside `Draw` only; GPU time is not measured.

**Lessons:**

- The game is **not** repeatable by default: `RandomHelper` is seeded from `DateTime.Now.Ticks` and the step is variable (`IsFixedTimeStep = false`, and the source says a fixed step changes the car's behaviour). Without the fixed step and seed, two runs of the *same* build differ by MAE 3 to 18.
- Residual nondeterminism with both fixed: a ~160x170 px region near the top middle of the Advanced track at update 180 and 360 (baseline vs baseline MAE 0.04 to 0.06, same size as baseline vs WS0). Not investigated (stall rule: not part of WS0). Treat differences below MAE 0.15 on Advanced as noise.
- macOS has no `timeout`; and zsh does not word-split an unquoted `$var`, so `set -- $p` loops silently pass one argument.

**Open:**

- The `EffectHandles` plumbing is compiled but untested until a shader uses a parameter (WS2).
- Windows `ContentWindows.contentproj` was not checked for the new `.fxh` (Desktop only, per the standing rule).
- WS0 is uncommitted; the rig worktrees (`wt_base`, `wt_new`) live in the session scratchpad and can be removed with `git worktree remove --force`.

### WS1 (2026-11-22): time of day, Path A. Status: DONE, committed (`7dcf1967`)

**Built:** `Core/Graphics/TrackLighting.TimeOfDay.cs` (keyframes Day 0.0, Golden 0.35, Dusk 0.65, Night 1.0; smoothstep; `NightFactor`, `FlareAlpha`, sun height, sky tint, glow, shadow colour; fog stored but unused). Hooks: `TrackLighting.Sky` (sky tint), `UIRenderer.RenderGameBackground` (flare colour, skipped at alpha 0), `PostScreenGlow.Show` (glow parameters set per frame), `GameScreen.Render` (calls `UpdateForGame`, sets `BaseGame.LightDirection` and `LensFlare.Origin3D` from the same sun, sets `ShadowColor` on the ShadowMap effect; `EndGame()` restores Day on leaving). Menus (`CarSelection`, `LanLobbyScreen`) are untouched. Inactive unless `RACINGGAME_TIMEOFDAY` or `RACINGGAME_CLOCK` is set, or `RACINGGAME_DEBUGKEYS=1` and a key is pressed (`]` speed 0/1x/10x/60x, `[` pause, `-`/`=` scrub). The clock advances on the frame's elapsed time inside `GameScreen.Render` only.

**Signs and banners:** no extra code needed: the load-time ambient 128 is baked into the part's `Material` (built from the effect) and every draw goes through `SetParametersOptimized`, which applies `Lighting.Ambient`.

**Verified:** TIMEOFDAY=0 vs baseline MAE 0.09 to 0.18 (Beginner; windmill blades animate on real time, 1.0 at update 720), 0.0000 (Advanced), 0.013 to 0.063 (Expert). CLOCK=30 filmstrip (20 frames per track): smooth day, golden, dusk, night, back to day; no black or white frames; flare gone by dusk; shadows hard-edged.

**Lesson (corrected by the orchestrator):** the day cube map has **no sun** (checked on `SkyCubeMap.png`); the bright sun in the frames is the lens flare, which follows the runtime sun. What IS baked into the cube is the mountain horizon and desert floor, which matters for the night sky (see WS1 item 4 and the night sky note below).

**Open:** no night cube yet (tint only), fog/headlights later, the `LightingShader.fx` ghost car ignores time of day.

### WS2 (2026-11-22): shader lighting core. Status: DONE, merged

**Built:** per-pixel world-space lights (up to 4, named `LPos0..3/LCol0..3/LDir0..3`), exponential fog and hemisphere ambient in `TrackLighting.fxh` (`TL_Lights`, `TL_Hemi`, `TL_Fog`, `TL_WorldNormal`). Each converted pixel shader has `if (LightingMode > 0.5) { new path } else { legacy code unchanged }`, then `TL_Fog` (no-op at density 0). The sun stays on the legacy per-vertex tangent-space vector; extra lights are added on top using the world position, world normal and tangent passed from the VS (3 extra float3 TEXCOORDs; the bitangent is rebuilt in the PS as `cross(N, T)`). Converted techniques (those used in-game): `DiffuseWithDetail20` (landscape), `Diffuse20`, `Specular20`, `DiffuseSpecular20`, `SpecularRoad20` (road), `SpecularWithReflection20`, `SpecularWithReflectionForCar20` (car paint), `ReflectionSpecular20` in NormalMapping.fx (the car glass really uses this, via the BasicEffect fallback in `Model.RenderCar`) and the same in `ReflectionSimpleGlass.fx`. Not converted: the ps_1_1 style techniques (`Diffuse`, `Specular`, `SpecularWithReflection`, `ReflectionSpecular`, `Diffuse20Transparent`), `LightingShader.fx` (ghost car), sky and glow shaders.
C#: `TrackLighting.Lights.cs` (`Light`, `SetLights`, `SetTestLights`, env `RACINGGAME_TESTLIGHT=1` spot / `2` omni point / `4` four lights for cost, `RACINGGAME_TESTFOG=<density>`), `EffectHandles` uploads LPos/LCol/LDir, `ShaderEffect.ApplyLighting()` called from `Model.RenderCar` for the shared NormalMapping effect.

**Verified (Release, Desktop, rig, fixed step):** LightingMode 0 vs HEAD baseline: Beginner MAE 0.002 to 0.003, Expert MAE 0.005 to 0.026 (noise level). Test spot gives a hot spot on the road ahead; omni test light lights road, left hillside, guardrail, car and the palms on the right; fog 0.006 blends distant terrain, road, rails and palms with no seams. Draw CPU (Expert, 900 frames): baseline 1.04 ms, mode 0 1.04 / 1.06, 4 lights 1.08 / 1.08 ms. GPU time not measured. Compiles for OPENGL ps_3_0 (MojoShader) with 7 TEXCOORD interpolators worst case; no limit hit.

**Open:** sky and horizon colour are not fogged (set `FogColor` to the horizon colour from C#); the `Car` path through the model's own non-Basic effects is not wired (all .x models load as BasicEffect here); light intensity and range for real headlights are WS3 tuning; GPU cost at 1080p unmeasured.

### Night sky note (2026-11-22, orchestrator)

The existing cube has the horizon and desert floor painted in, so do not use a landscape night HDRI. Plan: keep the day cube for horizon and ground (darkened by the night tint), add a **stars-only** CC0 cube (or procedural stars) additively in the sky shader, masked by how sky-blue the day pixel is (so stars never show on mountains) and scaled by `NightFactor`. Sun and moon are sprites at the runtime sun position (`Sun.tga`, `Glow.tga`). Needs a small edit of `PreScreenSkyCubeMapping.fx`.

### Night stars (2026-11-22, orchestrator): DONE

- `Core/Content/Textures/NightSkyCubeMap.dds` (1024 faces, DXT1, built by `scripts/make-night-sky-cube.py`, credit in `Credits-Night-Sky.md`) plus a content entry in `RacingGameContent.mgcb`.
- `PreScreenSkyCubeMapping.fx`: second cube sampler `nightTexture` and `NightSky` (x blend, y gain, z/w sine of elevation where stars fade in). Stars are added only where the day cube is open blue sky (`b - r`), and above about 5 to 15 degrees elevation, so mountains, desert and the photo's own horizon never show. `NightSky.x = 0` adds nothing (GL starts parameters at 0, so a missing cube or day time is safe).
- C#: `TrackLighting.StarBlend` (0 until the night factor passes 0.4, 1 at night), uploaded in `PreScreenSkyCubeMapping.RenderSky`; the night cube is optional (a load failure only logs).
- **Verified:** build clean; Expert and Advanced at time of day 1.0 show stars and the Milky Way haze over the dark sky, day clouds appear as dark shapes against them; seams between cube faces are continuous in the preview and in game; day output unchanged (blend 0).
- **Open:** gain 1.6 is a first guess and the Milky Way haze looks slightly purple-brown, so a tuning pass (gain, tint toward blue, optional yaw to put the Milky Way ahead of the start view, `make-night-sky-cube.py ... <yaw>`) is cheap; sun and moon sprites still to do (WS5); clouds currently hide the stars (reads as night clouds, fine).

### WS4 (2026-11-22): tunnels and street lamps. Status: DONE, uncommitted in worktree

**Built:** `Core/Graphics/TrackLighting.Lamps.cs` (partial): `TunnelBlend` eased every frame from the player's segment (`(int)Player.TrackProgress`, `Landscape.IsTunnel` -> `Track.IsTunnel`), 0.4 s in, 0.6 s out, run from `UpdateForGame` before the `Active` check, so it also works in shipped Day play. Tunnel look = multipliers on `Current`: ambient tint `(1 - 0.8 b)`, sun diffuse tint `(1 - b)`, shadow colour lerped to white (`ApplyShadowColor`), lens flare colour scaled by `(1 - b)` (`FlareColor`, `UIRenderer`). Exactly nothing happens at blend 0 (Day tints stay bit-identical, `Current` is reset to Day only on the frame the blend returns to 0). `CollectLampLights`: lamp list (tunnel lamps from `Track.GenerateTunnelLamps`, one point light every 25 units, 5 above the centre line; street lamps registered in `Landscape.AddObjectToRender` for "Laterne", light under the arm tip at model-space (2.4, 0, 8.0) after the object matrix, measured from `Laterne.X`: arm tip x 2.6, height 8.7, times the 1.2 scale). Picks the nearest `MaxLights - list.Count` enabled lamps (incumbent counts as 20 percent closer), eased 0.35 s in/out, outputs the strongest by intensity so a two-slot crossfade keeps total intensity continuous. Street lamps follow `NightFactor` (on from 0.3 to 0.7), tunnel lamps follow `TunnelBlend`; with none enabled the list stays empty, so `LightingMode` stays 0 and Day outside tunnels is unchanged. Read-only `LampPositions`, `LampIsTunnel(i)`, `LampIntensity(i)` for WS5 glow sprites. `ClearLamps()` is called from `Landscape.KillAllLoadedObjects` (track reload).
**Rig:** `Documentation/Lighting-Rig-ws4.patch` (apply after `Lighting-Rig.patch`): `RACINGGAME_CAMPOS=<segment>` teleports the player car at update 30 (reflection sets the private segment number; the countdown ends at about update 270, so frames before that show a standing car), and prints the helper positions (tunnel and lantern segment ranges) at update 10. Tunnels: Beginner 767-867; Advanced 562-757, 1329-1347; Expert 519-530, 542-550, 555-563, 574-582, 590-598, 605-610, 618-623, 629-632, 638-647, 3690-3839. Lanterns: Beginner 294-562, 867-998; Advanced 78-277, 255-562, 1137-1329, 1972-2169 (order in the probe log: tracks 1 then 2); Expert 1015-1484, 1484-1871, 2494-3080, 3080-3690, 4065-4463 (indicative, the two probe logs were printed together).

**Verified (Release, Desktop, rig):** Day, no tunnel, default runs vs HEAD baseline: Beginner MAE 0.002 to 0.003, Advanced 0.004 to 0.103 (known noise region), Expert 0.005 to 0.026. Beginner tunnel entry and exit (CAMPOS 764 and 860) in Day: darkens over about 0.4 s, lit by the tunnel lamps, recovers over 0.6 s, flare circles fade out at the mouth. Night (TIMEOFDAY=1): street lamps light the road on Beginner, Advanced and Expert, tunnels on Advanced and Expert dark with lamp pools. Draw CPU (Expert, 900 frames, vsync off): baseline 1.113 ms in the same session, WS4 1.096 (Day), 1.12 (night, lamps active); first WS4 run read 1.20, which was session noise.

**Lessons:** the car does not move until the start countdown ends (about 270 updates), so capture from update 280. `Rig.cs` is an untracked file, so undo with `git apply -R` of the ws4 patch first, then `Lighting-Rig.patch`. The segment range check misses nothing for shipped data, but a tunnel range that wraps past the last point would need `start > end` handling (none exists).

**Open:** no custom track sample exists in the repo, so the custom track path is not run (same `RoadHelper` code path, no format change). Headlights (WS3) are not merged here, so tunnel brightness with headlights lit is untested: lamp intensity 1.7 (tunnel) and 3.2 (street) are first guesses, tune when both exist. Lamp light position pops at the equal-intensity crossing point (no intensity pop). Glow sprites on lamps are WS5. Static `Track.disableLensFlareInTunnel` is still checked every 10 frames; the flare now also fades with the blend.

### WS3 (2026-11-22): headlights and tail lights. Status: DONE in worktree, uncommitted

**Built:** `Core/Graphics/TrackLighting.Headlights.cs` (`CollectHeadlights`: two warm spot lights anchored on the car through `Model.CarLightMatrix(Player.CarRenderMatrix)`, inner 12, outer 30 degrees, range 80, intensity 5 (env `RACINGGAME_LIGHT_INTENSITY/RANGE/INNER/OUTER` for tuning, remove before the final merge), about 7 degrees down; auto logic with hysteresis, night on above 0.35 and off below 0.25, tunnel on above 0.3 and off below 0.2; 0.3 s fade that scales the light colour; zero lights and no sprites when off, so Day without tunnel stays on the legacy path; debug key `H` (auto, on, off) only with `RACINGGAME_DEBUGKEYS=1`; optional red tail point light `RACINGGAME_TAILLIGHT=1`, off by default because it takes a light slot). `Core/Graphics/LightSprites.cs` (shared additive glow billboards, `Glow.tga`, one BasicEffect, depth read only, flushed from `GameScreen.Render` after all cars and before shadows and the glow post, size grows with distance; WS4 can call `BaseGame.Lighting.Sprites.Add`). Anchors are constants in `Model.cs`. `CarPhysics.IsBraking` (read only flag set where the brake is applied, speed above 0.5). LAN: `CarStatePacket.Flags` (bit 0 = braking, no protocol bump; old and new builds must not be mixed on one LAN, dev only), stored in the `RemoteCar` snapshot (flag of the state at or just before the sample time); remote cars get glow sprites with their own matrix and braking flag. The ghost car gets nothing.

**Axes (verified by logging):** model +Z maps to `Player.CarDirection` through `objectMatrix * CarRenderMatrix` and model +Y to the car up vector, so the nose is +Z. Car.x has no light meshes (the lamps are painted into the texture). Body bounds in model space (from the .x, frame scale 0.3667): z about -3.1 to +2.9, y 0.2 to 1.9. Tail anchors (x +-0.6, y 0.78, z -3.2) were confirmed on the round tail lamps in chase frames. Headlight anchors (x +-0.62, y 0.7, z 2.7) are not confirmed visually: the rig has no front view.

**Verified:** Day frames vs baseline: Beginner MAE 0.10, 0.11, 0.19, 1.0 (windmill at update 720, as in WS1), Advanced 0.13, 0.09, 0.004, 0.006, Expert 0.007 to 0.026 (noise). Night and dusk frames and timings: see the WS3 report.

**Lessons:** (1) A first test with the wrong sign (lights pointing backwards) lit the road behind the car: check on which side of the car the pools land. (2) The low chase camera hides most of the lit road behind the car body; the cone shows best on rails, palms and far road. (3) A shell `&&` chain after a failed `sed` silently skipped an edit (the GameScreen hook): check that every edit landed. (4) 7 degrees of downward pitch reads better than 4 from the chase view.

**Open:** headlight sprites not seen from the front; remote cars get glows only (the 2 headlight slots belong to the local car; sharing one slot was rejected to avoid a pop); intensity needs a user look; tail point light off by default; `RACINGGAME_LIGHT_*` envs and `RACINGGAME_LIGHT_BRAKE` to remove before the final merge; remote braking is binary (no ease).

### Combined check WS0-WS4 (2026-11-22, orchestrator)

- Merged all workstreams on `racinggame-lighting`; Desktop Release builds with 0 warnings and 0 errors.
- New rig patch `Documentation/Lighting-Rig-cam.patch` (apply after `Lighting-Rig.patch` and `Lighting-Rig-ws4.patch`): `RACINGGAME_CAM=front` or `34` views the car from in front or three quarters, which the WS3 agent could not do.
- **Verified by eye** (Read the PNGs): headlight glows sit on the painted front lamps with warm light pools on the road, from the front and three-quarter views; red tail glows sit on the rear lamps in the chase view; in a Beginner tunnel by day the headlights, tunnel lamps and tail lights work together and read as a lit tunnel; at night the road is lit by lamps plus headlights.
- **Tuned:** street lamp intensity 3.2 to 1.4 (at 3.2 the lamps washed out the road and hid the headlight pools). Tunnel lamps (1.7) left as they were.
- **Not verified:** the LAN two-instance run (remote car tail glow and braking flag), the Advanced track tunnel with headlights, custom tracks, GPU time and 1080p cost.
- **Cleanup still to do before the final merge:** fold or remove the `RACINGGAME_LIGHT_*`, `RACINGGAME_LIGHT_BRAKE`, `RACINGGAME_TAILLIGHT` tuning env vars; decide whether to keep the other env-gated test hooks (`RACINGGAME_TESTLIGHT`, `RACINGGAME_TESTFOG`, `RACINGGAME_TIMEOFDAY`, `RACINGGAME_CLOCK`, `RACINGGAME_DEBUGKEYS`).
- **Next:** WS5 (sun and moon sprites, lamp glow sprites through `LightSprites`, window lights on buildings, start lights, glow retune for night) and WS6 (Lighting Off/Low/High option, per-track time of day, LAN).

### WS5 step 1 (2026-11-22): moon (orchestrator)

- `PreScreenSkyCubeMapping.fx` draws the moon in the sky pass (`MoonDir`, `MoonParams`): a soft-edged disc (3.2 degree radius, enlarged) with faint markings and a halo, at the key light direction (`TrackLighting.LightPos`), faded in with `StarBlend`. It replaces the sky colour inside the disc (adding saturated it to flat white) and is hidden over terrain (brown, r > b) and mostly over the day clouds, so mountains and clouds cover it.
- The stars cube is now built with edge texels exactly on the face borders (`make-night-sky-cube.py`), so the face seams filter correctly.
- **Known, pre-existing:** a thin dotted dark line along a triangle diagonal of the sky cube mesh shows when looking steeply up (also in the original day sky). Cause is gaps between the cube's triangles showing the clear colour. Not fixed; cheap fixes are a watertight sky mesh or a sky-coloured clear.
- **Open:** the moon surface pattern is a cheap sine noise (reads as faint chevron patches when large); a small crater texture would be nicer. The sun at dusk still disappears with the flare (no sun disc sprite).

### WS5 step 2 (2026-11-22): lamp glows (orchestrator)

- `TrackLighting.AddLampGlows()` (in `TrackLighting.Lamps.cs`, called from `GameScreen.Render` next to `AddCarSprites`) queues a glow sprite for every lamp within 240 units of the camera (fading out from 140): street lamps with the night factor, tunnel lamps with the tunnel blend. The sprites go through the shared `LightSprites` renderer, depth tested, so hills and buildings hide them. They are independent of the 2 real light slots, so a row of lamps shows down the road.
- **Verified by eye:** soft warm halos on the lamp heads at night on Beginner, and on a tunnel lamp; no change by day.
- **Skipped:** building window lights (the tower windows already read as lit at night through the glow post). **Left:** start light glow, a night glow retune of `PostScreenGlow`, and a sun disc sprite at dusk.

### WS6 (2026-11-23): settings, Options rows, per-track time of day. Status: DONE in worktree, uncommitted

**Built:** `GameSettings.Lighting` (`LightingQuality` Off/Low/High, default High) and `GameSettings.TimeOfDayMode` (`TrackDefault/Day/Dusk/Night`), both persisted in `RacingGameSettings.xml`. `TrackLighting.Quality` is the single gate: `BeginRace(trackTimeOfDay)` (called from the `GameScreen` constructor, so LAN screens too) reads the settings, resets to Day, and picks the fixed race time (`RaceTimeOfDay()`: custom track `TrackData.TimeOfDay`, or built-in Beginner 0, Advanced 0.65, Expert 1.0; override Day 0 / Dusk 0.65 / Night 1.0; env `RACINGGAME_TIMEOFDAY/CLOCK` still wins as test mode). Off: `UpdateForGame` returns at once (no tunnel blend, tints, sun change), headlight fade stays 0 (this also removes all car glow sprites), `AddLampGlows` returns; Low: lamp glow sprites yes, `CollectLampLights` returns (no lamp or tunnel lamp lights), no tail point light; High: everything. `TrackData.TimeOfDay` (float, absent = 0), copied in `Track.CloneData`. No LAN protocol change (the field is in the shared file). Options screen: two groups of panel buttons "Lighting Off/Low/High" and "Time of day Track/Day/Dusk/Night" on the Graphic line right of the Fullscreen button (the painted background has no free line; a group label is drawn as text, there is no painted label to erase). They apply when the next race starts. Rig: `Lighting-Rig-ws6.patch` (apply last): `RACINGGAME_SCREEN=options` opens the Options screen, `RACINGGAME_CUSTOM=1` plays the first custom track from `RACINGGAME_TRACKS_DIR`.

**Verified (Release, Desktop, rig, 1280x720 windowed):** Off on all three tracks vs the HEAD baseline: Beginner MAE 0.04 to 0.08 (0.45 at update 720, windmill), Advanced 0.002 to 0.004, Expert 0.007 to 0.027 (noise). Default (High, track default): Beginner day (MAE 0.05 vs baseline), Advanced dusk, Expert night with stars, moon, lamp glows and headlights. Override: Night on Beginner, Day on Expert (Day frame MAE 0.009 vs baseline), Dusk on Beginner all visibly right. Custom track with `<TimeOfDay>1</TimeOfDay>` and `RACINGGAME_TRACKS_DIR` plays at night. Draw CPU on Expert, vsync off: Off 1.04 and 0.85 ms, High 0.94 and 0.85 ms (runs differ by more than Off vs High, so no measurable cost). Options screenshot checked at 1280x720: all seven new buttons fit, nothing overlaps painted text.

**Lessons:** `CalcRectangleKeep4To3` scales x by about 1.25 and y by 0.94 at 1280x720, so widths measured with `TextureFont` (screen pixels) must not be mixed with logical widths; the new groups are laid out in screen pixels from one logical start point. The Options background has no free line, so the two groups share the Graphic line; at 1024 wide or less they would not fit (not checked). The settings file was edited with a small script between runs and restored from a backup afterwards.

**Open / not verified:** saving the new settings through the Options screen (Escape path) was not driven; loading them from the file was (every override above). The Low vs High night difference (lamp lights) is subtle in the chase frames (MAE 6.6 at update 360 but that includes timing differences of the car); a close lamp frame would show it better. LAN two-instance run with different settings per player not run (no protocol change, so each peer only changes its own view). `TrackTool` does not write `TimeOfDay` yet. Settings changes take effect at the next race start, not while racing.

### Glow-on check and star gain (2026-11-23, orchestrator)

- **Finding:** the dev machine's settings file has `PostScreenEffects=false` (the code default is true), so every rig capture so far had the post-screen glow (bloom) OFF. Re-checked night, dusk, day and lamps with glow ON: dusk, lamps and headlight halos look good; full night was too milky because the stars layer (gain 1.6) was bright enough to bloom. Star gain lowered to 1.0 (NightSky.y in `PreScreenSkyCubeMapping.cs`). Changing the night glow keyframe values (threshold, intensity) made almost no visible difference, so they stay as WS1 set them.
- **Cost of glow** (performance review): about +3.4 ms per frame when on (Expert), 171 fps vs about 430 fps with it off; still far above 60 fps.
- **Rig note:** to test with glow on, set `<PostScreenEffects>true</PostScreenEffects>` in `~/Library/Application Support/RacingGame/RacingGameSettings.xml` (back it up and restore it).
- Performance review and its instrumentation: see `Performance-Review.md` and `Performance-Rig.patch` (gated by `RACINGGAME_PERF=1`).

### Cleanup (2026-11-23, orchestrator): DONE

- **Removed from the shipped code:** the tuning variables `RACINGGAME_LIGHT_INTENSITY/RANGE/INNER/OUTER` (now constants: 12 and 30 degrees, range 80, intensity 5), `RACINGGAME_LIGHT_BRAKE`, `RACINGGAME_TAILLIGHT` and the optional tail point light (never visually verified, off by default), and the WS2 test hooks `RACINGGAME_TESTLIGHT` (1, 2, 4) and `RACINGGAME_TESTFOG` with `SetTestLights`. They are kept as `Documentation/Lighting-Rig-testlights.patch` (apply with `git apply` from the repo root to bring them back for shader work).
- **Kept as documented dev/test modes** (inert unless set): `RACINGGAME_TIMEOFDAY`, `RACINGGAME_CLOCK`, `RACINGGAME_DEBUGKEYS` (see the README), plus the LAN test aids `RACINGGAME_TRACKS_DIR` and `RACINGGAME_PLAYER_NAME` that already existed.
- **Verified:** a night frame (three-quarter camera, headlights on) is identical before and after the cleanup (MAE 0.0000), and Release Desktop builds with 0 warnings.
- README now describes the lighting features, the Options and the developer test hooks.

### Correction (2026-11-23): where the earlier shadow experiment came from

An earlier answer in this session said the reverted 2026-10-27 shadow experiment was "TPS-style". That was wrong: ThirdPersonShooter's first commit is 2026-11-18 (its shadow files arrived in that commit), and the 2026-10-27 experiment used the first-person Shooter's `ShadowMapper.cs`. So TPS shadows (orthographic map that follows the player, spot-light shadow atlas, normal-offset and slope bias, in-shader lookup applied only to the key light) have **never been tried in RacingGame**, and nothing in the history argues against trying them. The remaining reasons for caution are about fit, not history: TPS's map covers a 32 m radius (a racing game needs far more range), its lookup lives in its own PBR shader (RacingGame's lit shaders would each need one), and the user prefers hard-edged shadows like the original. The hard-shadow preference still stands.
