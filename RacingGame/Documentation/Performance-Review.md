# RacingGame performance and maintainability review

Date 2026-11-23, base `8ed20d63` (branch `racinggame-lighting`), Desktop (DesktopGL, macOS) Release builds only. Review plus measurement; no game behaviour was changed. The instrumentation is in `Documentation/Performance-Rig.patch` (a diff against the state with the four rig patches applied; apply the four rig patches first, then this one, all with `git apply` from the repo root).

## 1. Bottom line

- On this rig the game is **not CPU bound and has no GC problem**. Update is 0.07 ms median, Draw 0.8 to 1.2 ms, the whole frame about 2.1 to 2.5 ms (400 to 490 fps, vsync off). Against a 16.7 ms budget at 60 Hz that is a large margin.
- The real risks are elsewhere:
  1. **GPU**: the game submits about 485 draw calls and **about 1,000,000 triangles per frame** on Expert (about 576,000 on Beginner), with no culling of the landscape or the track mesh. The CPU cannot measure the GPU, but the frame period is about 1.2 ms longer than Update plus Draw, so there is GPU/present time we cannot see.
  2. **The optional glow post effect** (Options "post screen effects"; the code default is ON, but the dev machine settings file had it OFF, so these runs had it off unless stated): turning it on takes the frame from 2.5 ms to 5.8 ms (171 fps), a Measured +3.4 ms of which only +0.6 ms is CPU. On a weaker GPU this is the first thing to hurt.
  3. **Spikes**: 1 to 2 percent of frames have an Update of about 3.5 ms (p99) up to 5 to 10 ms, located in the input polling (`Input.Update`, which warps the mouse every frame). Frame worst-case was 7 to 19 ms over 3 to 13 s runs, with 0 or 1 frames over 16.7 ms per 5000. No GC is involved (gen0 only, no gen1/gen2).
- One real allocation hot spot exists: **16.7 KB per frame, of which 11.9 KB is the car render** (string `ToLower` and `Split` per mesh part per frame). It is cheap to fix and risk free, but at 60 fps it is 1 MB/s, i.e. a gen0 GC about every 7 s. It cannot cause visible hitches by itself.
- Lighting High at night vs Off costs about +0.1 ms Draw CPU and about +0.15 ms per frame (same track, same resolution). It is cheap on this GPU.

## 2. Phase A: measurements

Setup: Release, Desktop, macOS, `RACINGGAME_THROTTLE=1 AUTOSTART=1 NOVSYNC=1 PERF=1`, 5000 samples after update 300 (the countdown ends near update 270), real variable time step (no fixed step). Settings file backed up and restored (verified identical). Update and Draw are Stopwatch times of `RacingGameManager.Update` and `BaseGame.Draw` (CPU time on the main thread, including GL driver calls). Frame is the wall time between successive Update starts. Allocation uses `GC.GetAllocatedBytesForCurrentThread`.

Columns are mean / p50 / p95 / p99 / max in ms.

| Run (track, lighting, window) | Update ms | Draw ms | Frame ms | fps | alloc B/frame mean / max | GC0/1/2 per min | frames >16.7 ms |
|---|---|---|---|---|---|---|---|
| T0 Beginner, High night, 1280x720 | 0.140 / 0.070 / 0.113 / 3.55 / 5.47 | 0.978 / 0.957 / 1.148 / 1.295 / 3.48 | 2.233 / 1.953 / 5.18 / 5.84 / 8.00 | 448 | 16726 / 278872 | 54/0/0 | 0 |
| T0 Beginner, Off, 1280x720 | 0.125 / 0.069 / 0.106 / 3.39 / 4.99 | 0.953 / 0.935 / 1.124 / 1.254 / 2.82 | 2.078 / 1.804 / 4.91 / 5.45 / 18.97 | 481 | 16712 / 278872 | 58/0/0 | 1 |
| T1 Advanced, High night, 1280x720 | 0.132 / 0.070 / 0.110 / 3.44 / 5.18 | 0.809 / 0.794 / 0.952 / 1.071 / 2.97 | 2.240 / 1.972 / 5.10 / 5.78 / 7.42 | 446 | 16721 / 278872 | 54/0/0 | 0 |
| T1 Advanced, Off, 1280x720 | 0.109 / 0.065 / 0.100 / 2.69 / 4.79 | 0.843 / 0.830 / 0.990 / 1.126 / 2.76 | 2.053 / 1.837 / 4.59 / 5.29 / 6.76 | 487 | 16713 / 278904 | 58/0/0 | 0 |
| T2 Expert, High night, 1280x720 | 0.127 / 0.070 / 0.115 / 3.41 / 9.85 | 1.156 / 1.142 / 1.339 / 1.488 / 2.83 | 2.456 / 2.170 / 5.12 / 5.93 / 13.39 | 407 | 16730 / 278872 | 49/0/0 | 0 |
| T2 Expert, Off, 1280x720 | 0.117 / 0.070 / 0.111 / 2.93 / 4.95 | 1.070 / 1.050 / 1.259 / 1.411 / 2.71 | 2.307 / 2.014 / 5.18 / 5.76 / 8.69 | 433 | 16726 / 278872 | 52/0/0 | 0 |
| T2 Expert, High day (tod 0), 1280x720 | 0.117 / 0.069 / 0.112 / 3.14 / 4.99 | 1.145 / 1.128 / 1.330 / 1.471 / 4.52 | 2.325 / 2.025 / 5.23 / 5.80 / 11.73 | 430 | 16728 / 278872 | 52/0/0 | 0 |
| T2 Expert, High night, 1414x796 (see note) | 0.099 / 0.051 / 0.100 / 2.61 / 5.64 | 1.238 / 1.219 / 1.448 / 1.633 / 2.93 | 2.628 / 2.382 / 5.32 / 6.14 / 7.95 | 380 | 16740 / 278872 | 46/0/0 | 0 |
| T2 Expert, Off, 1414x796 | 0.104 / 0.049 / 0.097 / 2.78 / 5.42 | 1.142 / 1.124 / 1.356 / 1.550 / 2.90 | 2.418 / 2.186 / 4.94 / 5.84 / 12.88 | 413 | 16730 / 278872 | 50/0/0 | 0 |
| T2 Expert, High night, glow ON, 1280x720 | 0.142 / 0.091 / 0.139 / 3.54 / 5.38 | 1.717 / 1.717 / 2.035 / 2.355 / 8.03 | 5.838 / 5.411 / 8.34 / 9.91 / 17.13 | 171 | 16813 / 278872 | 21/0/0 | 1 |

Notes:

- Window size: a request for 1920x1080 is clamped by `FitWindowedSize` to **1414x796** on this display (22 percent more pixels). Real fullscreen or a bigger window was not measured.
- "GC per minute" counts are normalised from 3 to 13 s runs and are at 400+ fps. At 60 fps with vsync the same 16.7 KB/frame is about 1 MB/s, so gen0 about every 7 s (here 1 gen0 per about 1.1 s at 7 MB/s). Gen1 and gen2 never happened in any run.
- The 278,872 byte maximum is the same single frame in every run (a one-off, probably the first sampled frame after warmup); the 131 KB Update maximum is also a one-off.

Per-frame render statistics (MonoGame `GraphicsMetrics`, Expert unless noted), per frame:

| Run | draws | triangles | texture sets | render target sets | clears | sprites |
|---|---|---|---|---|---|---|
| T2 High night, glow off | 485 | 998,387 | 98 | 6 | 10 | 93 |
| T0 Beginner Off, glow off | 423 | 575,697 | 102 | 6 | 10 | 95 |
| T2 High night, glow ON | 471 | 999,283 | 114 | 16 | 20 | 93 |

Draw CPU breakdown, Expert High night (mean ms per frame, a mark is the code since the previous mark): `MeshRenderManager.Render` (main pass draws) 0.650, shadow generate plus receive plus blur setup 0.255, landscape/track/object submission 0.090, car 0.059, sky 0.018, everything else under 0.015. With glow on the glow `Show` is +0.386 CPU, and the rest of the +0.6 ms is spread over the passes.

Update breakdown (mean 0.12 to 0.16 ms): base.Update plus `Input.Update` 0.147 (max 4.8 ms), player/physics update 0.003 (max 2.9 ms), screen update 0.002, `Sound.Update` 0.002, fps counter block 0.003 (max 1.2 ms), LAN pump 0.001.

### What the numbers say

1. Draw is about 1 ms and Update about 0.1 ms mean: nothing to win on average CPU time. A 2x improvement of Draw CPU would move the frame by under 0.6 ms.
2. Frame minus (Update plus Draw) is about 1.1 to 1.3 ms on every run, and about 4 to 5 percent of frames take about 5 ms (p95) although Update and Draw are normal in those frames. That time is outside our code: buffer swap/present or waiting on the GPU. Cannot be split further from here.
3. Window size effect: +22 percent pixels gave +0.11 ms (Off) and +0.17 ms (High) per frame, about 0.5 to 0.8 ms per extra megapixel. So at this size the game is **only weakly resolution dependent**. This is a hint, not proof, that fill rate is not the limiter here; geometry (1 M triangles) and the per-frame render target passes are the more likely GPU costs. Fullscreen on a high-DPI display was not measured.
4. Lighting: High night vs Off on the same track: Draw +0.09 to 0.13 ms (T2, T0), frame +0.1 to 0.15 ms. T1 differs by +0.03 ms Draw only. Day vs night in High: +0.0. Consistent with the earlier +0.07 ms claim.
5. Glow ON is the single biggest measured cost (+3.4 ms/frame).

### What cannot be measured here

- **GPU time.** DesktopGL has no timestamp queries in MonoGame; no Metal frame capture was used. All GPU statements are inferences from frame period minus CPU time and from resolution scaling.
- Frame times with vsync on, loading/track-start hitches, memory use, behaviour on Windows/Android/iOS (out of scope), fullscreen and high-DPI, other GPUs, and the LAN path (no second instance was run).
- The car is held on the throttle and hits a rail, so the camera sees a limited part of each track; geometry-dependent costs (object counts in view) may differ elsewhere.

## 3. Ranked findings

Confidence: Measured (we have numbers for the cost), Likely (static evidence plus a consistent measurement), Speculative (plausible, not proven). Effort S/M/L. "Risk" is risk to behaviour or look.

1. **About 1,000,000 triangles and 485 draws per frame, no culling of the big static meshes. Likely (counts Measured, GPU cost inferred).**
   - Where: `Landscapes/Landscape.cs:1199-1201` and `:1221-1227` (one 257x257 grid, 131,072 triangles, drawn whole), `Landscape.cs:1251-1254` (the same grid drawn again in the shadow receive pass), `Tracks/Track.cs:1253-1277` (road, road back hull and tunnels drawn whole for the entire track, also drawn again for shadows). Beginner and Expert differ by 420k triangles, so much of the count is track meshes.
   - Cost: unknown GPU time, probably the main GPU cost; invisible geometry behind and far from the camera is shaded or at least vertex processed 2 to 3 times.
   - Fix: split the landscape and the track meshes into tiles/chunks (for example 16x16 cells, index ranges of the same buffers) and skip chunks outside the view frustum and the shadow range in both passes. Keep one vertex buffer; only the `DrawIndexedPrimitives` ranges change.
   - Estimated gain: 30 to 60 percent fewer triangles; GPU gain unverified. Effort M to L. Risk: low (seams or pop at tile edges if bounds are wrong; verify by A/B frames).
2. **Glow post effect costs +3.4 ms per frame when enabled. Measured.**
   - Where: `Shaders/RenderToTexture.cs:204-211` (MSAA count 2, or 4 **only if the back buffer height is exactly 720**, a resolution-keyed hack), `:224-246` (scene target tries `Rgba64` first, then `HdrBlendable`, then `Color`), `Shaders/PostScreenGlow.cs:150-235` (5 passes with four render target switches, a `Resolve` and an extra `effectPass.Apply()` after each; 16 render target sets and 20 clears per frame vs 6 and 10 without).
   - Evidence: frame 2.46 to 5.84 ms (171 fps), Draw CPU +0.6 ms, so the rest is GPU/resolve.
   - Fix: make the scene target `Color` or half-float without MSAA (or MSAA 2 only), drop the duplicate `effectPass.Apply()` calls if the parameter did not change, and compare frames. Offer the effect as Low/High quality.
   - Gain: up to about 2 to 3 ms with glow on; none when it is off (default). Effort S to M. Risk: medium for the look (bloom edge quality, precision of bright values); A/B against the baseline frames needed.
3. **16.4 KB per frame of Draw allocation, of which 11.9 KB is `Model.RenderCar` (every frame, 2 alpha passes). Measured (by per-section allocation marks).**
   - Where: `Graphics/Model.cs:742` (`mesh.Name.ToLower().StartsWith("glass")` per mesh per pass), `Graphics/Material.cs:178-190` (`TechniqueNameFromPartInfo`: `info.Split(...)`, `StartsWith`, `Substring` per part per frame) and `:206-209` (`IsGlassPartInfo`: `Contains` on the part tag string, called twice per part). Also load-time `ToLower` at `Model.cs:222-227, 241, 278, 291` (harmless).
   - Cost: allocation only (0.059 ms CPU for the whole car). Not a hitch cause by itself.
   - Fix: parse the part tag once at load into a small struct per part (technique name, isGlass) and a `isGlassMesh` bool per mesh; use those in `RenderCar`.
   - Gain: about 70 percent of all per-frame allocation (16.7 KB down to about 4.8 KB). Effort S. Risk: none if the cached values equal the computed ones.
4. **Update spikes of 3 to 10 ms in about 1 to 2 percent of frames, located in `Input.Update`. Likely.**
   - Where: `GameLogic/Input.cs:896-951`; the mark covering base.Update plus `Input.Update` has a max of 4.8 ms and a mean of 0.147 ms while everything else in Update is under 0.01 ms mean. In the race the code calls `Mouse.SetPosition(...)` and then `Mouse.GetState()` again every frame (`Input.cs:926`, `:930`), a window system call that is the prime suspect on macOS. `player.Update` also had a single 2.9 ms max (not located).
   - Evidence: p99 Update 2.6 to 3.6 ms vs p95 0.10 to 0.13 ms in all 10 runs; Update p50 0.07 ms. We did not isolate `SetPosition` from `GetState`, so this is Likely, not Measured.
   - Fix: first confirm with a two-line timing around each call; then warp the mouse only when it has moved from the centre (compare with the last warped position) or use relative mouse mode if MonoGame exposes it, and skip it when the gamepad or keyboard is in use (`mouseDetected == false`).
   - Gain: removes most of the frame spikes (about 3.5 ms of 6 to 9 ms worst frames on this rig). Effort S to M. Risk: medium for mouse steering feel; needs a play test.
5. **About 1.2 ms per frame and about 5 percent of frames (about 5 ms) are not Update or Draw. Speculative.**
   - Evidence: frame p95 5.1 ms vs Update plus Draw p95 about 1.4 ms, in all runs, also with Lighting Off; independent of lighting and of resolution.
   - Probably buffer swap/present or driver work on the GL thread; could also be macOS compositor effects with vsync off. Needs a GPU capture (Metal or Instruments) to split. No fix proposed yet; an item for measurement on a second machine.
6. **Shadow pipeline does three full passes every frame. Likely.**
   - Where: `Shaders/ShadowMapShader.cs:285-325` (generate into a 2048x2048, or 1024, `Rgba64`/`Single` target; `RenderToTexture.cs:105-115`), `:340-376` (a screen sized receive pass re-draws landscape, nearby objects and car), `Shaders/ShadowMapBlur.cs:125, 171` (blur into two more targets), `:426` show. 6 render target sets and 10 clears per frame with glow off. CPU is only 0.255 ms for all of it; GPU cost not measured.
   - Fix options: (a) render the landscape into the receive pass from the same culled chunks as in finding 1; (b) lower the shadow map to 1024 on Low quality; (c) skip the blur when shadow strength is near zero at night (`BaseGame.Lighting` already scales shadow colour). Option (a) is free once finding 1 exists.
   - Gain: unknown (GPU). Effort M. Risk: medium for the look (hard shadows must stay as they are, per the plan's non-goals).
7. **Per-frame delegate and closure allocations, about 1.2 KB/frame. Measured (sections) plus static.**
   - Where: `Landscapes/Landscape.cs:1201` and `Tracks/Track.cs:1259, 1268, 1276` (`new BaseGame.RenderHandler(...)` each frame), the `delegate { ... }` closures in `Shaders/ShadowMapShader.cs:373, 404, 413` (capture `this` and arguments), plus `Landscape.cs:1421`.
   - Fix: cache the delegates in fields (created once). Effort S. Gain: about 1 KB per frame. Risk: none. Low value on its own, do it together with other edits in the same files.
8. **HUD per frame allocation, about 2.4 KB in `GameScreen`/`RenderGameUI` plus 512 B in text rendering. Measured (sections), causes Likely.**
   - Where: `GameScreens/GameScreen.cs:181` calls `Highscores.GetTop5LapTimes` every frame, which allocates `new int[5]` (`GameScreens/Highscores.cs:170-183`); `Graphics/UIRenderer.cs` builds time and speed strings per frame (not located line by line; the section total is 2,370 B).
   - Fix: cache the top-5 array per track at race start; build strings with a cached `StringBuilder` or only when the displayed value changes (time text changes every frame, speed in mph only when the integer changes). Effort S. Gain: about 3 KB per frame. Risk: none.
9. **Per-mesh draw overhead in `MeshRenderManager`. Speculative (probably does not matter).**
   - Where: `Graphics/MeshRenderManager.cs:119-140`: every instance does `WorldMatrix =` plus `Passes[0].Apply()` (uniform upload of all effect parameters that changed) plus a draw. 485 draws cost 0.65 ms, about 1.3 microseconds each. Batching by technique and material is already done.
   - Fix if ever needed: hardware instancing or a merged static batch for palms and rails. Effort L. Do not start unless a slower CPU shows Draw above 4 ms.
10. **Lit shaders carry both paths and 4 light slots, evaluated per pixel when High. Speculative (GPU).**
    - Where: `Content/Shaders/TrackLighting.fxh:41-96` and the 7 `if (LightingMode > 0.5)` blocks in `NormalMapping.fx` (lines 228, 399, 452, 595, 738, 830, 902) and 1 in `LandscapeNormalMapping.fx:528`. Per pixel in the new path: `TL_WorldNormal` does 3 normalizes plus a cross, a normalize of the view vector, and per active light a `length`, a divide, a `smoothstep`, a `normalize(L+V)` and a `pow`. `LightCount` is a uniform, so the `if`s are uniform branches (cheap), but the legacy and the new path are compiled into one shader, so register use is the maximum of both and the legacy path still receives the 9 extra interpolants (worldPos, worldNormal, worldTangent) if the vertex shader writes them unconditionally (not verified for each technique).
    - Evidence: High vs Off costs +0.1 ms CPU and about +0.15 ms frame here; no GPU detail.
    - Fix (only if a GPU capture shows it matters): use the legacy view vector instead of a second normalize, `rsqrt` instead of `length` plus divide, a cheaper specular term for lamps, and a `#define LIGHTING_PERPIXEL` variant technique set so Off compiles a lean shader. Effort M. Risk: medium for the look.
11. **Fog and sky overdraw and sprites: not a cost. Measured (negligible).** Sky 0.018 ms, 93 sprites per frame, flares and glows are in the sprite batch. No action.
12. **LAN: not a performance risk. Likely.** `Multiplayer/RaceNetSession.cs:184-191` creates a `MemoryStream`, `BinaryWriter` and a result array per packet; car state is sent every 0.05 s (`GameScreens/MultiplayerGameScreen.cs:26`), so about 20 packets/s of a few hundred bytes. Not measured (no second instance). No action, except a reusable buffer if someone touches the file anyway.
13. **`Input.cs:944` allocates a `new List<Keys>(GetPressedKeys())` per frame. Measured as a small part of the 32 B per Update.** Trivial; keep a reusable list. Effort S, no risk.
14. **Stringly `Parameters["..."]` lookups per frame** (`Graphics/Model.cs` 16 sites, `Shaders/ShaderEffect.cs` 19, `Shaders/PostScreenGlow.cs` 11, `Graphics/MeshRenderManager.cs:219-222`). These are dictionary lookups with no allocation, a few hundred per frame at tens of nanoseconds each; Draw is 1 ms total. **Not worth changing for speed**; only cache them when touching the file for maintainability (see section 4).
15. **Physics, track and landscape CPU: fine.** Update is 0.003 ms for the player/physics step and the object lists are plain `List<T>` with `for` loops (`Landscape.cs:1210-1213`, `:1239-1243`); `Model.Render` culls per object with a distance test and an angle test (`Graphics/Model.cs:410-442`, includes a `Normalize` and an angle per object, about 0.09 ms for all objects). No finding.
16. **Loading and track start: not measured.** `Landscape.cs:540` (vertex array for 66k vertices), `:935-976` (height map and smoothed normals as 2-D arrays) run at track creation; a hitch there would not show in any of the numbers above. Measure with a Stopwatch around `Landscape` creation before anyone optimises it.
17. **GC settings:** 16.7 KB/frame with gen0 only is harmless here. If the allocation fixes (findings 3, 7, 8, 13) are done the rate drops to about 1 KB/frame and a gen0 GC would occur about once a minute at 60 fps. Optionally set `TieredPGO`/`ConcurrentGarbageCollection=false` in the csproj; not measured, do not do it without a test.

(Only 17 findings are listed; the rest of the review found nothing that matters at this cost level.)

## 4. Maintainability

Evidence for each is in the files named; none of these changes behaviour.

- **Very large files.** `Graphics/BaseGame.cs` 1,660 lines (device setup, resolution and fullscreen handling, line managers, render target bookkeeping, Update/Draw, fps counter, static helpers), `Tracks/Track.cs` 1,568, `Landscapes/Landscape.cs` 1,430, `GameLogic/CarPhysics.cs` 1,239, `Graphics/UIRenderer.cs` 1,133, `GameLogic/Input.cs` 953, `Shaders/ShaderEffect.cs` 941, `Graphics/Model.cs` 930. Suggested cuts, in order of payoff: split `BaseGame` (display/resolution settings into `DisplaySettings.cs`; render target helpers into `RenderTargets.cs`; fps/view distance into `FrameStats.cs`), split `Track` into geometry building (`Track.cs:331-650`) and rendering, split `Landscape` into height map and mesh generation vs object management vs rendering.
- **`Model.RenderCar` is a 330-line method** (`Graphics/Model.cs:504-835`) with two alpha passes, index counters over parallel `cachedEffectParameters`, `cachedIsReflectionSpecularTechnique` and `cachedPartMaterials` lists (`Model.cs:119-139`), a shadow-car branch, a BasicEffect fallback and a car-colour technique swap. It duplicates what `MeshRenderManager` does for normal models. Replace the parallel lists with a small `CarPart` struct array built once at load (this is also the fix for finding 3).
- **Lighting block repeated 8 times in the shaders** (7 in `NormalMapping.fx`, 1 in `LandscapeNormalMapping.fx`), each about 15 to 25 lines that differ only in how the diffuse, ambient and detail terms are combined. Move the shared part into one function in `TrackLighting.fxh` (for example `TL_Shade(P, nts, Nw, Tw, ...)` returning diffuse-light and specular-light terms) and keep only the combine line per technique. Needs the A/B frame check (Day with Lighting Off must stay identical to the baseline).
- **String-typed material data at runtime:** part info is a `;`-separated string in the mesh part tag and is re-parsed (`Material.cs:178-209`, `:221+`). Parse once into a typed record at load.
- **Dead and platform code from the XNA port:** `#if XBOX360` blocks (`Input.cs` 14 mentions, `BaseGame.cs`, `UIRenderer.cs`, `Options.cs`, `Helpers/Log.cs`, `FileHelper.cs`, `ScreenshotCapturer.cs`, `Model.cs`), commented-out render state code (`MeshRenderManager.cs:221-248` TODO alpha test, `RenderToTexture.cs:213-218` "UWP COMMENT OUT"), 15 TODO/UWP markers in total, the `MultisampleCount` 720-height hack (`RenderToTexture.cs:206-211`). Remove what the DesktopGL/Android/iOS targets cannot reach; keep notes in the docs.
- **Rig and tuning hooks:** the lighting plan says the `RACINGGAME_LIGHT_*`, `RACINGGAME_TESTLIGHT`, `RACINGGAME_TESTFOG`, `RACINGGAME_LIGHT_BRAKE` and similar env hooks must be folded or removed before the final merge. Add them to the cleanup list; keep `Rig.cs` out of the shipped tree (it is only a patch).
- **Shared code with other samples (only where it pays):**
  - A frame capture plus perf probe helper exists in four places: `RacingGame/Core/Helpers/Rig.cs` (patch only), `ThirdPersonShooter/Core/ThirdPersonShooterGame.cs`, `Shooter/Core/ShooterGame.cs`, `CatapultWars/Core/Utility/ScreenshotDirector.cs` (all use `GetBackBufferData`). A small env-gated `FrameProbe` (capture every N updates, fixed step option, percentile summary like the one in `Performance-Rig.patch`) would save every future port the rewrite. It does pay, it is optional for the game itself, so keep it as a patch or a shared source file, not in game code.
  - `ThirdPersonShooter/Core/Rendering/SceneLighting.cs` (155 lines) vs `TrackLighting*.cs` (1,181 lines in 5 files): different shader contracts (PBR with an array vs Blinn-Phong with named slots), so sharing code would not pay. Sharing the **idea** already happened (the plan borrows the light list, hysteresis and fog).
  - Glow/bloom: the repository has a `BloomEffect` sample; the RacingGame glow has custom radial blur and 5 passes (`PostScreenGlow.fx`, 413 lines). Not verified whether the sample's two-pass bloom could replace it; only worth checking if finding 2 is acted on.

## 5. Proposed work packages (independent, disjoint files)

| WP | Scope | Owns (only these files) | Depends on | Effort | Expected result |
|---|---|---|---|---|---|
| WP1 Car render allocation | Finding 3 plus the `CarPart` struct (section 4) | `Graphics/Model.cs`, `Graphics/Material.cs` | none | S | 11.9 KB/frame saved, `RenderCar` readable; A/B frames identical |
| WP2 Allocation clean-up | Findings 7, 8, 13 | `Shaders/ShadowMapShader.cs`, `GameScreens/GameScreen.cs`, `GameScreens/Highscores.cs`, `Graphics/UIRenderer.cs`, `Graphics/TextureFont.cs`, `GameLogic/Input.cs` (only the `keysPressedLastFrame` line) | none (if WP3 also edits `Input.cs`, let WP3 take the `List<Keys>` line) | S | about 4 KB/frame saved |
| WP3 Input spike | Finding 4: time `SetPosition` and `GetState` separately, then change the warp logic | `GameLogic/Input.cs` (whole file), `GameLogic/Player.cs` | none | S to M | removes most 3 to 10 ms frames; needs a play test |
| WP4 Static mesh culling | Finding 1 and finding 6a | `Landscapes/Landscape.cs`, `Tracks/Track.cs`, `Tracks/TrackLine.cs` (only if needed), `Graphics/TangentVertex.cs` | none (do not run with WP7 on `Landscape.cs`) | M to L | up to half the triangles; needs frame A/B and a GPU capture to confirm |
| WP5 Glow and render targets | Finding 2 (and the MSAA 720 hack) | `Shaders/RenderToTexture.cs`, `Shaders/PostScreenGlow.cs`, `Shaders/PostScreenMenu.cs`, `Shaders/ShadowMapBlur.cs`, `Content/Shaders/PostScreenGlow.fx` | none | S to M | 2 to 3 ms with glow on; look must be A/B checked |
| WP6 Shader dedupe and cost | Findings 10 and the 8x lighting block | `Content/Shaders/NormalMapping.fx`, `LandscapeNormalMapping.fx`, `TrackLighting.fxh`, `LightingShader.fx`, `ReflectionSimpleGlass.fx` | needs a GPU capture to justify the cost half; the dedupe half has no dependency | M | smaller shaders, same output |
| WP7 BaseGame split | Section 4 BaseGame split and dead platform code | `Graphics/BaseGame.cs` (and the new files it creates) | do last, after WP2 and WP5 land (they touch nothing in `BaseGame.cs` but call into it) | M | no behaviour change; smaller files |
| WP8 Measurement | Keep `Performance-Rig.patch` current, add GPU capture notes, run fullscreen and vsync-on runs, measure loading time, run the LAN pair | `Documentation/Performance-Rig.patch`, `Documentation/Performance-Review.md`, scripts | none | S | fills the "not measured" list |

WP1, WP3, WP4, WP5 and WP6 can all run in parallel. Do WP7 last.

## 6. What to do first

1. WP1 and WP2 (an hour, no risk, 16.7 KB down to under 2 KB per frame).
2. WP3 step one (time the two input calls); it is the only thing in the numbers that looks like a hitch.
3. WP8: take a Metal frame capture to see the GPU time split between the shadow passes, the landscape pass and present; that decides how much WP4 and WP6 are worth.
4. WP5 only if glow on by default is on the roadmap (it is off today).

## 6. Fixes applied (2026-11-23, after the review)

Measured with `Performance-Rig.patch` on Expert at night, 3000 to 3500 samples, vsync off. Frames A/B against the baseline on all three tracks: unchanged within the known noise.

| Change | Files | Before | After |
|---|---|---|---|
| Parse the part tag once per distinct string (`TechniqueNameFromPartInfo`, `IsGlassPartInfo` cached in dictionaries), `StartsWith(..., OrdinalIgnoreCase)` instead of `ToLower().StartsWith` (finding 3) | `Graphics/Material.cs`, `Graphics/Model.cs` | allocation 16,700 B/frame | 4,490 B/frame (-73 percent) |
| Reuse one top-5 array, no `new int[5]` per frame (finding 8, part) | `GameScreens/Highscores.cs` | | |
| Previous `KeyboardState` instead of a new `List<Keys>` per update (finding 13) | `GameLogic/Input.cs` | | |
| Cached `RenderHandler` delegates for landscape, road, road back, tunnel, plane and brake tracks; the brake-track closure became a method (finding 7, part) | `Landscapes/Landscape.cs`, `Tracks/Track.cs`, `Graphics/PlaneRenderer.cs` | | |
| Skip `Mouse.SetPosition` when the mouse is already at the centre (finding 4) | `GameLogic/Input.cs` | Update p99 2.9 ms, mean 0.127 ms | Update p99 0.03 ms, mean 0.018 ms |

Finding 4 was confirmed by timing each call separately for 3000 frames: `Mouse.SetPosition` mean 0.10 ms, p50 0.06 ms, **p99 2.6 ms, max 4.8 ms**; `Mouse.GetState` (both calls), `Keyboard.GetState` and `GamePad.GetState` are all under 0.004 ms at p99. The fix is behaviour-identical (the next frame's movement is measured from the same centre point), but it only removes the spikes while the mouse is still, which is keyboard and gamepad play; when steering with the mouse the call still happens every frame that the mouse moved. Not play-tested with a mouse.

**Still open from the review:** HUD string allocations (about 2.4 KB/frame, finding 8), the three closures in `ShadowMapShader.cs` (finding 7), frame time outside Update and Draw (finding 5, needs a GPU capture), triangle count and culling (finding 1), glow cost (finding 2), shader dedupe, `BaseGame` split, dead XBOX360 code.

### 6b. HUD text and the shadow closure (2026-11-23, later)

| Change | Files | Effect |
|---|---|---|
| Cache the "m:ss.hh" time strings by sign and hundredths of a second (the HUD writes 7 times per frame, 6 of them unchanged from frame to frame) | `Graphics/TextureFont.cs` (`WriteGameTime`, `GameTimeText`) | HUD section (`g9`) 2,370 B to about 790 B per frame |
| Cached strings for the small numbers of the big HUD digits (speed, gear, laps) and no `ToCharArray()` copies | `Graphics/TextureFontBigNumbers.cs` | |
| Iterate the text string directly in `GetTextWidth` and `WriteAll` instead of copying it to a `char[]` per text per frame | `Graphics/TextureFont.cs` | text section (`d6`) 512 B to 0 per frame |
| The shadow receive pass was a closure capturing `this` and the draw callback (2 allocations per frame); now a cached delegate plus a field. The other two delegates in `PrepareGameShadows` capture nothing, so the compiler already caches them. | `Shaders/ShadowMapShader.cs` | about 80 B per frame |

Total allocation per frame, Expert at night: **16,700 B originally, 4,490 B after the first round, 2,350 B now (-86 percent)**. Frames A/B identical (MAE 0.0000 on the night HUD frames and on the shadowed day frames). The remaining allocation per frame is spread thinly: shadow setup 384 B, landscape 394 B, shadow plus post 467 B, the HUD 790 B (mostly the running time string, which is new every 10 ms), car 104 B. Not worth more work at this size.
