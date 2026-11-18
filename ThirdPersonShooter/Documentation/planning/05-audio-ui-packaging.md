# 05 - Audio, UI/menus and packaging (Godot TPS demo -> MonoGame DesktopGL)

Owner: Agent 5. Sources read: `default_bus_layout.tres`, `level/level.tscn`, `menu/menu.tscn` + `menu.gd`, `player/player.tscn`, `red_robot.tscn` (audio nodes + tracks), `door.tscn`, `bullet.tscn`, `project.godot`, `RacingGame/` (Desktop csproj, scripts, Documentation), `~/.claude/skills/unity-to-monogame-port/`, plans 01-04. Claims are **assumed** (read from files) unless marked verified. Nothing was built or run.

## 0. Answers to other agents

- **Agent 1 (ogg):** Music = `Song` (ogg via the standard importer, `MediaPlayer.IsRepeating`); all 11 `.wav` = `SoundEffect`. No manual conversion in the converter tool. Assumed to work on DesktopGL; early spike (task 3) checks looping on macOS. Fallbacks: convert to mp3/wav, or a full-length looping `SoundEffectInstance`. Record the result in `AssetMap.json` (`audio.kind`).
- **Reverb buses:** not feasible in MonoGame DesktopGL: the public API has no reverb effect (EFX is not exposed; `ReverbPreset` is not usable on OpenAL). Slice = dry audio, zone logic kept as a data stub. Post-slice option = software Freeverb on a `DynamicSoundEffectInstance` mix bus (task 17). README states "no reverb".
- **Agent 4 (robot walk audio):** `red_robot.tscn` track 67 is an **AudioStreamPlayer3D playback track** on `SoundEffects/Walk` inside the `walk` animation (two `walk.wav` clips, `start_offset 0`, `end_offset 0`; timing is the track key times, not the script). Cannon (`charge`/`shoot`) is likewise driven by track 2 in the shoot animation (`SoundEffects/Cannon`). Hit and Explosion are played from script (`hit()`, `explode()`). Recommendation: agent 1's baker extracts audio tracks as `{clip, trackPath, times[], stream}` in `animations.json`; agent 4's `AnimationController` raises `OnAudioKey(name)` events at those times (30 fps sampling, same clock). Fallback: loop `walk` at the clip length whenever state == Walk. Agent 4 owns the event hookup; I own `Audio.Play3D`.

## 1. Audio inventory and bus mapping

13 sources: 11 `.wav` (2.8 MB), 2 `.ogg`. Verified by listing: `menu/menu_intro_music.ogg`, `level/level_music.ogg`, `door/open_close.wav`, `player/audio/{step,jump,land,shoot}.wav`, `player/bullet/bullet_explode.wav`, `enemies/red_robot/audio/{charge,shoot,explosion,walk,hit}.wav`.

### Buses (`default_bus_layout.tres`)

| Godot bus | volume_db | Effect | MonoGame mapping | Feasibility |
|---|---|---|---|---|
| Master | -10.0 | none | `AudioSettings.MasterGain = 10^(-10/20) = 0.316` multiplied into every `Volume`; also `SoundEffect.MasterVolume` and `MediaPlayer.Volume` | exact |
| Outside (send Master) | 0 | Reverb room 0.98, hipass 0.42, wet 0.29 | `AudioZone.Outside`: gain 1.0; reverb dropped (optional fake tap) | gain only |
| Reactor (send Master) | 0 | Reverb room 0.74, hipass 0.14, wet 0.56 | `AudioZone.Reactor`: gain 1.0; reverb dropped | gain only |

Zone routing in Godot: `SoundOutside` (CollisionPolygon3D) and `SoundReactorRoom` (shape) are `Area3D` with `audio_bus_override = true`: the *listener* (camera) inside the area sends all `AudioStreamPlayer3D` to that bus. Port: `AudioZoneSystem` polls the listener position against the baked audio volumes in `level.scene` (agent 1) each frame, sets `Audio.CurrentZone`; zone only changes the fake-reverb tap (off by default) so for the slice it is a data-only stub with a unit test. Non-3D players (Music, step, jump, land, shoot) stay on Master (they have no bus set).

### Nodes (measured from `.tscn`)

| Godot node | File | Sound | Settings | MonoGame |
|---|---|---|---|---|
| `Music` AudioStreamPlayer | menu.tscn | menu_intro_music.ogg | autoplay, loop per import | `Song`, `MediaPlayer.IsRepeating` |
| `Music` | level.tscn | level_music.ogg | autoplay | `Song`, stop menu song on entry, crossfade 0.5 s (own volume ramp) |
| `SoundEffects/Step` | menu.tscn | UI step (button focus) | 2D | `SoundEffect.Play()` on focus change |
| `Step`,`Jump`,`Land` | player.tscn | step/jump/land.wav | 2D, Step random pitch (`step_random_pitch.tres`, range to be read from the .tres by agent 1) | 2D `SoundEffectInstance`; pitch random in range |
| `Shoot` | player.tscn | shoot.wav via AudioStreamRandomizer (1 stream: so no variation, only pitch/volume randomiser params, check .tres) | 2D | 2D, fire-and-forget |
| `ExplosionAudio` | bullet.tscn | bullet_explode.wav | 3D unit_size 20; `volume_db -80` in player's BulletCache (silences the cached bullet) | 3D one-shot at impact position; ignore the -80 override (cache only) |
| `Cannon` | red_robot.tscn | charge.wav / shoot.wav | 3D unit_size 15 | 3D, anim track keys |
| `Explosion` | red_robot.tscn | explosion.wav | 3D unit_size 15, +12 dB | 3D, gain clamp 1 (see risks) |
| `Hit` | red_robot.tscn | hit.wav | 3D unit_size 15, +3 dB | 3D |
| `Walk` | red_robot.tscn | walk.wav | 3D unit_size 15, +3 dB, anim track | 3D, anim track keys |
| `sound` | door.tscn | open_close.wav | 3D unit_size 7, +4 dB | 3D |

### 3D attenuation

Godot `AudioStreamPlayer3D` default attenuation model is `inverse distance`: gain = `unit_size / (unit_size + (d - unit_size)...)`; precisely Godot: `att = 1 / (1 + (d/unit_size))` is NOT used; it uses `ATTENUATION_INVERSE_DISTANCE`: `gain = unit_size / max(d, unit_size)`-style clamp (assumed; verify in Godot source `audio_stream_player_3d.cpp`: `db = vol_db + 20*log10(unit_size/d)` for inverse, `max_distance 0` = unlimited). Port: do **not** use `SoundEffectInstance.Apply3D` attenuation (OpenAL inverse-distance-clamped uses `DistanceScale`, reference-distance 1). Instead implement **own `AudioEmitter3D`**: compute `gain = clamp(unit_size/d, 0, 1)` (inverse-distance, in linear), pan from listener right vector (`pan = dot(dir, right)` clamped), and set `Volume`/`Pan` per frame. This avoids OpenAL differences, gives exact like-for-like, and works for mono and stereo WAVs (OpenAL `Apply3D` requires mono; stereo sources ignore it). `AudioListener` is not used; the camera view matrix gives right/forward. Doppler off (Godot default `doppler_tracking` disabled). Voice limit: 32 `SoundEffectInstance` pooled (OpenAL Soft default 256 but MonoGame caps instance creation to 256; pool avoids GC).

### API sketch (static `Audio`, agent 4's `Services.Audio`)

`Audio.Init(ContentManager, AudioSettings)`, `Audio.PlayMusic(name, loop)`, `Audio.Play2D(name, volume, pitch)`, `Audio.Play3D(name, worldPos, unitSize, volumeDb)`, `Audio.UpdateListener(Matrix view)`, `Audio.Update(dt)`, `Audio.SetZone`, `Audio.Pause/Resume` (pause menu), `Audio.Dispose`. Data from `AssetMap.json` (`unitSize`, `volumeDb`, `loop`) so it is table-driven.

### Options

Master/Music/SFX sliders (0-100 %, default Godot -10 dB master = 0.316 baked as default master factor 1.0 `x` base). Godot demo has **no** volume UI (see section 2), so this is an optional addition; keep, since it costs little.

## 2. UI plan

### What the Godot demo has (verified in `menu.gd`/`menu.tscn`)

Main (Play, Settings, Quit; focus starts on Play), Online (host/join with address and port; **dropped**, singleplayer only), Settings (`DisplayMode`: windowed/fullscreen/exclusive fullscreen; `VSync`: disabled/enabled/adaptive/mailbox; `MaxFPS`: 30..144/unlimited; `ResolutionScale`: 6 presets; `ScaleFilter`: nearest/bilinear/FSR1/FSR2/MetalFX; `TAA`; `MSAA`: off/2x/4x/8x; `ScreenSpaceAA`: off/FXAA/SMAA; `ShadowMapping`; `GIType`, `GIQuality`; `SSAO`; `SSIL`; `Bloom`; `VolumetricFog`; Apply/Cancel), Loading (ProgressBar + DoneTimer; `ResourceLoader.load_threaded_*` of the level), HUD = 16x16 crosshair texture (`player/crosshair.png`) at screen centre shown while aiming (plan 03: `CrosshairAlpha`), full-screen black fall-fade (plan 03: `FadeAlpha`). Theme: `button_*.tres` StyleBoxes (normal/hover/pressed/focus/action_pressed), `button_font.tres` using **PT Sans Bold** (`menu/font/PT_Sans-Web-Bold.ttf`, OFL). Menu background is a 3D scene (WorldEnvironment + `experiment.hdr`, `floor.png`, `tps_demo.png` logo). No key-rebinding UI; no pause menu (Esc -> `quit` returns to menu: `level.gd:126` sets mouse VISIBLE, then the main scene switches). Input: `Esc`, `F11/Enter` fullscreen, `ui_*` map. I did not read the full `menu.tscn` layout (positions/anchors): task 5 reads it.

### Approach: SpriteBatch + SpriteFont/FontStashSharp, hand-written widgets (no Myra)

Justification: the UI is ~6 button types and 1 layout; Myra adds a dependency (+Gum/XNA assets, own theming) and pipeline coupling and diverges from the RacingGame sibling (which uses its own screens). Hand-written `UiButton`, `UiRadioRow`, `UiSlider`, `UiProgressBar`, `UiLabel`, `UiPanel` in a `Core/UI/` namespace (~600 lines), focus model like Godot: a `UiFocus` manager with up/down/left/right navigation (gamepad dpad/stick, arrows), `Enter/Space/A` = press, `Esc/B` = back, mouse hover moves focus and click presses (Godot `grab_focus` + `mouse_filter`). Fonts: **PT Sans Bold via FontStashSharp** (TTF at runtime, any size, crisp at 4K, no per-size SpriteFont bake) - one NuGet (`FontStashSharp.MonoGame`); fallback = pipeline SpriteFont at 3 sizes (24/32/48) if the user rejects the dependency (open question Q3). UI draws in a virtual 1920x1080 space scaled to the back buffer (Godot `stretch/mode=canvas_items`, `aspect=expand`: scale = min(w/1920, h/1080), expand means extra width is revealed; implement with `Matrix.CreateScale` and anchors left/centre/right).

Look: replicate StyleBoxes by reading the five `button_*.tres` (colours, corner radius, border, margins) -> `UiTheme.json` generated by agent 1's tool or hand-copied constants for the slice (S); draw rounded rects with a 9-slice texture generated at runtime (or tiny Texture2D) in `RoundedRect`. HDR `request_hdr_output` is **dropped** (document).

### Screens and flow

`GameStateManager` (agent 4: Menu / Loading / Playing / Paused). `MenuScreen` (Play, Settings, Quit; focus Play; logo `tps_demo.png`; menu music; Step sound on focus), `SettingsScreen` (below), `LoadingScreen`, `HudOverlay` (crosshair at screen centre, alpha from `IPlayerView.CrosshairAlpha`; fall-fade black quad `FadeAlpha`), `PauseOverlay` (**added**, not in demo: Resume / Settings / Quit to menu; Esc toggles; Godot returns directly to menu so a pause is an improvement; make `--like-for-like` default off... default = Esc opens pause. Q4). Mouse capture: `Mouse.SetPosition` + `IsMouseVisible=false` + relative deltas via `InputCapture.Set(bool)` (agent 3 expects it); release in menu/pause (Godot `MOUSE_MODE_VISIBLE`).

Loading: ContentManager has no threaded load progress. Build `LoadScreen` that loads in steps on the Update thread (one asset group per frame: models, textures, audio, level.scene) updating `Progress = i/n`, then 0.5 s DoneTimer-like hold (Godot's `DoneTimer`; read its wait_time in task 5) before switching. For large content (~hundreds MB) a background `Task` loading textures is unsafe with GL; keep sequential, GL calls on main thread, with the heavy decode (level.scene JSON/binary parse) on a worker. 

### Settings mapping (what is kept for MonoGame)

| Godot setting | MonoGame | Slice? |
|---|---|---|
| DisplayMode windowed / fullscreen / exclusive | `IsFullScreen`, `HardwareModeSwitch` (borderless vs exclusive); also F11 | yes |
| VSync (4 modes) | `SynchronizeWithVerticalRetrace` on/off (adaptive/mailbox dropped) | yes |
| MaxFPS | `TargetElapsedTime` / `IsFixedTimeStep=false` + limiter | later (S) |
| ResolutionScale + ScaleFilter | internal render scale of the HDR RT (agent 2's `RenderTargets`), bilinear/nearest only; FSR/MetalFX dropped | later (M) |
| TAA, SSAO, SSIL, SDFGI/GIType/GIQuality, VolumetricFog | **not implemented** (agent 2: no GI/SSAO); settings rows hidden | no |
| MSAA 2/4/8 | `PreferMultiSampling` + RT sample count | later |
| ScreenSpaceAA FXAA/SMAA | FXAA post (agent 2 optional) | later |
| ShadowMapping on/off | blob shadows always; toggle = off/on blob (agent 2) | later |
| Bloom | toggle for agent 2's `PostBloom` | yes |
| **added**: Window resolution list (like RacingGame fullscreen + resolution list), Master/Music/SFX volume, mouse sensitivity (if agent 3 exposes) | | yes |

Persist as `settings.json` in `Environment.SpecialFolder.ApplicationData/ThirdPersonShooter/` (RacingGame has a `GameSettings.cs`; mirror it). Apply/Cancel semantics kept (edit a copy; Apply commits). Key rebinding: **not in demo, skipped**.

## 3. Repo layout (mirrors RacingGame)

RacingGame has `Desktop/ Core/ Assets/ Documentation/ PipelineExtension/ scripts/ Android iOS Windows` plus `TrackTool`; `Desktop/` holds `Program.cs`, the csproj, `Icon.ico/.bmp/.png/.icns`, `Info.plist`, `Entitlements.plist`, `app.manifest`; Core/Content has the `.mgcb` (referenced as `MonoGameContentReference`); the content pipeline only works in Release because the .mgcb refers to `PipelineExtension/bin/Release` (copy that rule). Its `scripts/build-macos-desktop-android.sh` does MonoPack bundles, universal `.app`, `.pkg`, `--sign`, `--notarize` (profile `savage-notary`), `--clear`, `--build-number`. RacingGame `Program.cs` has **no** flags; the Shooter port had many (`--shot --pose --menu --hud --icon ...`) so flags are added here, parsed in Core (`CommandLine.cs`).

```
ThirdPersonShooter/
  ThirdPersonShooter.sln
  README.md                     credits Godot demo (MIT), build/run, controls, flags, verified-vs-assumed
  LICENSE.md                    MIT, copy of the Godot demo's (keep) + our code licence line
  .gitignore                    bin/ obj/ artifacts/ Assets/Source/ (see section 4) *.DS_Store *.user, shots/
  .claude/skills/godot-to-monogame-port/   (section 7)
  Assets/
    README.md                   how to fetch the source demo
    Source/                     (gitignored) the Godot demo, populated by scripts/fetch-demo.sh
    Converted/                  converted-glb/textures/audio inputs for mgcb (git, optionally LFS)
  Core/
    Core.csproj                 net10.0 (match RacingGame), refs MonoGame.Framework.DesktopGL, FontStashSharp
    Content/ThirdPersonShooter.mgcb  (+ Content/ generated; Materials.json, AssetMap.json, level.scene)
    Game/ Rendering/ Physics/ Player/ Enemies/ Audio/ UI/ Input/ Settings/ Verification/
    ThirdPersonShooterGame.cs
  Desktop/
    ThirdPersonShooter.csproj   DesktopGL, ApplicationIcon, MonoGameContentReference -> ../Core/Content/...
    Program.cs  Info.plist  Entitlements.plist  app.manifest
    Icon.ico Icon.bmp Icon.png ThirdPersonShooter.icns
  PipelineExtension/            GlbImporter, GodotStaticModelProcessor, SkinnedModelProcessor, Audio passthrough? (agent 1)
  Tools/                        GodotExtract (tscn/tres/gd-data parser), ContentGen, SceneBake, icons script  (agent 1)
  Documentation/
    planning/  (these plans)  PORTING-PLAN.md  Itch-Page-Copy.md (later)
    reference/                  Godot reference screenshots from the user (agent 2 Q)
  scripts/
    fetch-demo.sh               downloads the pinned demo release into Assets/Source
    build-macos-desktop.sh      adapted from RacingGame, Android + iOS parts removed
    generate-icons.sh           icon.png -> Icon.ico/.bmp/.icns (RacingGame has equivalents)
  tps-demo-master/              the current in-repo demo copy: MOVE to Assets/Source (see section 4)
```
Placement: `Tools/` and `PipelineExtension/` both at the repo root (RacingGame puts `PipelineExtension` at root and `TrackTool` at root; the Unity skill put tools under `Tools/`). Dev-only tools never ship in the app bundle.

## 4. Asset storage recommendation

Facts: the demo is 471 MB (`.png` 211 MB, `.glb` 96 MB, `.exr` 65 MB, `.res` 40 MB). Neither output nor MonoGame content needs the EXR/`.res`/`.lmbake`/`.import`/`.tscn` data at runtime.

Options: (A) **Git LFS** for the whole source: a clone pulls ~470 MB even for people not converting; GitHub LFS quota (1 GB/month free) is easily exhausted; contributors need LFS. (B) **Fetch script**: `scripts/fetch-demo.sh` downloads a pinned tag/commit zip from `godotengine/tps-demo` (MIT, public) into gitignored `Assets/Source/`; repo stays tiny; reproducible by commit hash; needs network once. (C) **Keep only converted content** in git: the `.xnb`s are built from the source anyway so they are not stored; but the *intermediate* converted inputs (texture-resized PNG 2048 max, trimmed glTF) are what the .mgcb eats. 

**Recommendation: B + minimal C.** Gitignore `Assets/Source/` (do not commit the 471 MB; move the current `tps-demo-master/` there and `git rm --cached` anything already tracked, user decides). Commit only: the generator outputs that are small and deterministic-or-expensive (`Materials.json`, `AssetMap.json`, `level.scene`, `.mgcb`, ~few MB) and the small CC/MIT UI textures (crosshair, logo, font + OFL.txt, icon) so a clone with no network can still build the *code* and run the `--test` checks; the content build needs `fetch-demo.sh` + `Tools/ContentGen` (script `scripts/build-content.sh` does fetch + extract + bake + mgcb). Pin by commit SHA in `scripts/demo-version.txt` (the demo has Godot 4.x master; the 4.7 .tscn uid format `unique_id` shows it is recent, pin matters). Optional later: GitHub Release asset with a prebuilt `Content.zip` (~100-150 MB after 2048 cap + DXT) so end users skip the converter. LFS is only for that zip, not for the repo. The repo today already shows `?? ./` untracked; do not add the source tree.

## 5. Packaging

- **Icon:** `tps-demo-master/icon.png` is the Godot icon (check dimensions in task 14). Use it as the master per the user's request; `scripts/generate-icons.sh` produces `Icon.ico` (multi-size), `Icon.bmp` (32x32 for the GL window), `.icns` (iconutil iconset). Optionally later a `--icon <file>` render of the player robot (Unity-skill technique, transparent PNG) as an alternative master. Wire like RacingGame csproj: `ApplicationIcon`, embed `Icon.ico/.bmp` as `EmbeddedResource` with `LogicalName`, set `.icns` in the bundle.
- **macOS app bundle / notarisation:** copy RacingGame script minus Android; keep `--sign`, `--notarize`, `--skip-pkg`, `--clear`; env `PKG_IDENTIFIER=com.cartblanche.ThirdPersonShooter`, `NOTARY_PROFILE=savage-notary`. Notarisation profile set-up is the user's pending task (memory reminder 2026-11-13); I do not run it. Entitlements.plist copied from RacingGame. Fullscreen/GL: no extra entitlement.
- **CI:** repo has `.github/workflows/build-samples.yml` (builds samples). Add a ThirdPersonShooter job later (S) that builds `Core` + runs `--test` but **skips content build** (no assets in CI) unless the fetch script is cached. Mirror the "restore race fix" learnt from Shooter (restore before parallel builds) - see the memory note `project_shooter_ci_and_todo`.
- **README:** layout, build/run (`dotnet run --project Desktop/ThirdPersonShooter.csproj -c Release`; Release because of the pipeline rule), controls table (agent 3), flags table, credits (Godot demo MIT, `LICENSE.md` kept, PT Sans OFL), what is verified, honest gaps (no reverb, no GI, no multiplayer).

## 6. Verification flags (`Core/Verification/CommandLine.cs`, model: Shooter + Unity skill)

| Flag | Behaviour | Owner |
|---|---|---|
| `--test` | headless self-tests of all domains (exit code = failure count): audio gain table, zone lookup, UI focus navigation, settings round trip, plus others register via `ITestSuite` | all |
| `--test-audio` | gain/pan maths vs Godot formula, voice pooling, zone polling at known points | 5 |
| `--shot <dir> [--pose <name>] [--poses <json>]` | render PNGs at 1920x1080 then exit; render each pose twice (Unity skill pitfall: smear) | 2/3 |
| `--menu`, `--settings`, `--loading`, `--pause`, `--hud` | shot of that UI state (hud: crosshair alpha 1, aiming) | 5 |
| `--play` | skip menu, straight into level (also `--level-seed` unneeded) | 4 |
| `--aim`, `--fall-fade <a>` | force aiming / fade alpha for shots | 3/5 |
| `--audio-log` | print every Play2D/3D with gain/pan to stdout (verify sounds fire without hearing them) | 5 |
| `--mute` | audio off (CI / headless) | 5 |
| `--fullscreen`, `--res WxH`, `--no-vsync` | override settings | 5 |
| `--icon <file>` (optional) | render robot to transparent PNG | 2/3 |
| `--seed N`, `--frames N` | deterministic run length for shots | 4 |

Rule: tests that need audio hardware report "not heard" in the plan; use `--audio-log` and a null-device mode (`OPENAL_DRIVERS`/`ALSOFT_DRIVERS=null` env, assumed to work on DesktopGL).

## 7. Skill layout: `ThirdPersonShooter/.claude/skills/godot-to-monogame-port/`

```
SKILL.md                     headings reused from the Unity skill: Working rules, Pipeline, Phases (0 Extract+baseline, 1 Scaffold, 2 Assets, 3 Rendering, 4 Gameplay, 5 Packaging), Per-mechanic loop, PORTING-PLAN entry template, Reference files
references/
  conventions.md             Godot: no flip, Transform3D rows, quaternions, winding, UVs, ORM, sRGB, AgX
  godot-formats.md           tscn/tres/import parsing, uid/ext_resource/sub_resource, AnimationTree dumps, bus layout   (Godot only)
  assets.md                  textures (srgb flag, 2048 cap), glb static/skinned processors, audio (ogg/wav), fonts
  pipeline.md                extractor, ContentGen, SceneBake, pipeline extension, runtime
  systems.md                 recipes: controller, camera, AnimationTree -> state machines, projectiles, AI, particles, audio buses, UI/Control, game flow
  pitfalls.md                symptom -> cause -> fix (GL fx defaults 0, tonemap, skinning palette limits...)
  verification.md            Godot reference capture (Movie Maker / --write-movie, screenshots), shot flags, self-test
  packaging.md               Desktop only, icon, bundle, notarise, CI, asset storage
  commit-groups.md           (identical to Unity)
examples/phase-entry.md      (identical)
```
**Shared vs specific.** Share (could live in a common repo, e.g. `monogame-port-skills/common/`): `commit-groups.md`, `examples/phase-entry.md`, Working rules, PORTING-PLAN template, `verification.md` (shot/test method; only the "reference capture" section differs), `packaging.md` (icon, MonoPack, notarise, CI; only Android section is Unity-only), the audio and UI recipes in `systems.md` (SoundEffect/Song/Apply3D, SpriteBatch menu), `pitfalls.md` rendering/content-pipeline sections, and the generic phase list. Engine-specific: `conventions.md` (Unity needs `diag(1,1,-1)` flip + scale bake; Godot none), formats (`UnityYAML/GUID` vs `tscn/tres/uid`), extractor, prefab vs scene instancing, AnimatorController vs AnimationTree, mixer snapshots vs bus layout, TextMeshPro vs `Theme/StyleBox`. Proposed split: `common/` + `unity/` + `godot/` with SKILL.md files that `see ../common/...`; Claude skills load per folder so the common files are duplicated by a sync script or git submodule (open question Q5).

## 8. Interfaces to other agents

- [agent5 produces] `Audio` static module: `Play2D/Play3D/PlayMusic/UpdateListener/Pause/Resume/SetZone`, `AudioSettings` (Master/Music/SFX), `AudioZoneSystem`; names from `AssetMap.json` audio entries.
- [agent5 produces] `UiFocus`/widgets, `MenuScreen/SettingsScreen/LoadingScreen/HudOverlay/PauseOverlay`, `InputCapture.Set(bool)` (mouse capture), `GameSettings` (settings.json), `CommandLine` flags.
- [agent5 expects from agent1] audio assets in `AssetMap.json` with `kind` (Song/SoundEffect), `unitSize`, `volumeDb`, `loop`, `mono/stereo`; animation audio tracks (clip + key times) for `walk` and `shoot`; audio zone volumes in `level.scene` (SoundOutside polygon, SoundReactorRoom shape, with the bus name); UI textures (crosshair, logo, floor), `menu/button_*.tres` values as `UiTheme.json`; step-random-pitch range; PT Sans TTF copied as `Content/Fonts/`.
- [agent5 expects from agent2] logical resolution scale for UI (virtual 1920x1080), post-process toggles for Bloom/MSAA/scale from `Settings`, a way to draw UI after tonemap (SpriteBatch pass onto the back buffer); menu 3D background (experiment.hdr scene) is agent 2's, if wanted.
- [agent5 expects from agent3] `IPlayerView.CrosshairAlpha/FadeAlpha`, events `OnShoot/OnJump/OnLand/OnFootstep` (with positions for 3D), key map in a `InputMap` I can read for the settings help screen; Esc/Back -> `GameStateManager.Pause()`.
- [agent5 expects from agent4] `GameStateManager` (Menu/Loading/Playing/Paused), `Services.Audio`, `OnRobotHit/OnRobotExploded/OnBulletImpact` events with positions, door open/close event, `OnAudioKey(name)` from animation audio tracks.

## 9. Risks and open questions

Risks:
1. **No reverb on DesktopGL** (OpenAL EFX not exposed): audible difference in the reactor and outside areas; mitigated by honest documentation and an optional software reverb later.
2. **OGG `Song` on DesktopGL** (macOS looping gaps, `MediaPlayer` quirks) - fallback to wav/mp3 or `SoundEffectInstance` loop; decided by an early spike.
3. **Asset size (471 MB) + licence**: fetch-script depends on upstream URL stability; pin SHA and consider a mirror release. PT Sans OFL requires shipping OFL.txt with the font.
4. **Gain > 1**: explosion +12 dB, hit/walk +3 dB, door +4 dB cannot exceed `Volume 1.0`; Master is -10 dB so relative headroom exists: apply master first (0.316) and boost per-sound up to clamp 1.0 -> likely OK (0.316 * 3.98 = 1.26 clipped to 1.0 for explosion: tiny loss; or pre-normalise the WAV in the converter by -12 dB and set master 1.0; decided in task 2).
5. **Loading progress** is fake-granular (sequential content loads), possible long freeze on 100s of MB textures.
6. FontStashSharp new dependency; HDR output not ported.

Open questions:
- Q1 [lead] Is the pause overlay allowed (Godot goes straight to menu on Esc)? Default proposed: yes, with `--like-for-like` off.
- Q2 [lead] Move `tps-demo-master/` into `Assets/Source/` and gitignore (needs `git rm --cached` if tracked)? Currently untracked; recommended.
- Q3 [lead] FontStashSharp (runtime TTF) vs pipeline SpriteFont sizes?
- Q4 [agent3] Esc and Back both open pause? F11 and Enter toggle fullscreen as in `project.godot`?
- Q5 [lead] Skill shared-vs-specific: separate repo with a sync script, or duplicate files?
- Q6 [agent1] Does the shoot AudioStreamRandomizer / `step_random_pitch.tres` carry pitch/volume ranges (I saw 1 stream, count 1)?
- Q7 verify Godot's inverse-distance attenuation formula and `unit_size` semantics in engine source (assumed `unit_size/d`).

## 10. Ordered task list

1. (S) Create repo skeleton: `.sln`, Core/Desktop csproj, `Program.cs`, `.gitignore`, empty README with MIT credit; copy `LICENSE.md`. Mirror RacingGame csproj wiring (Release rule).
2. (S) Decide `Assets/Source` move and write `scripts/fetch-demo.sh` + `demo-version.txt`.
3. (S) Audio spike: import ogg as `Song`, loop test on macOS; wav as `SoundEffect`; record finding (answers risk 2).
4. (M) `Audio` module: pool, `Play2D/3D` (own inverse-distance + pan), music, master/music/SFX volumes, `--audio-log`, `--test-audio`.
5. (M) Read `menu.tscn` layout + `button_*.tres`; write `UiTheme` + FontStashSharp text; `UiButton/Radio/Slider/ProgressBar`, `UiFocus` (keyboard, pad, mouse).
6. (M) Main menu screen + menu music + Step sound + `GameStateManager` hookup (with agent 4); `--menu` shot.
7. (M) Loading screen (staged loads) + level start; `--loading` shot.
8. (S) HUD overlay: crosshair + fall-fade quad; `--hud`.
9. (M) Settings screen (display mode, vsync, bloom, window resolution, volumes) + `settings.json`; apply/cancel.
10. (S) Pause overlay + mouse capture API (`InputCapture`).
11. (M) Wire sounds to events: player (step/jump/land/shoot), bullet impact, robot (cannon/hit/explosion/walk keys), door; unit sizes + dB from data.
12. (S) `AudioZoneSystem` stub (listener-in-volume polling, bus name, tests); optional fake reverb tap (M, post-slice).
13. (M) `CommandLine` flags and `--test` aggregator (see section 6); README flags table.
14. (S) Icons: `generate-icons.sh` from `icon.png` -> ico/bmp/icns; csproj wiring.
15. (M) `scripts/build-macos-desktop.sh` adapted from RacingGame (MonoPack, universal .app, pkg, sign, notarize flags); notarisation run is the user's step.
16. (S) CI job (build Core + `--test`, no content).
17. (L, post-slice) Software reverb (Freeverb on `DynamicSoundEffectInstance`) for Outside/Reactor, per-zone wet/room.
18. (M, post-slice) Remaining settings (MSAA, FXAA, render scale), max-FPS limiter.
19. (M) Write the skill (`SKILL.md` + references) at the end; split common vs engine files per section 7.

## 11. Lessons for the porting skill

- Read the engine's audio *graph* (buses, effects, send routing, zones) early and write down which effects the target API cannot do; do not discover "no reverb" late.
- Audio is data-driven in Godot scenes: unit_size, volume_db, bus, autoplay, stream properties on each node; also **animation audio tracks** (clip + times on the AnimationPlayer): the extractor must dump them, not just look at scripts.
- Implement 3D attenuation yourself (gain/pan from the listener matrix) rather than trusting the platform's `Apply3D`; it is engine-formula exact and testable headless.
- dB sums: record per-source dB and master dB; clamp to 1.0 and note clipping, or pre-normalise in the converter.
- UI: recreate focus/navigation semantics (grab_focus, mouse hover moves focus) first; style second. Virtual-resolution scaling with anchors mirrors `canvas_items` stretch.
- Replace async loader progress with staged loads; state honestly it is not a real progress bar.
- Keep source assets out of git (fetch script with pinned SHA); commit only small generator outputs; tools and pipeline extension at the repo root.
- Verification flags per UI state (`--menu --hud --pause --settings`) plus `--audio-log` give evidence without a person listening; always write "not heard" when true.
- Shared skill parts (commit groups, phase-entry, working rules, verification, packaging, audio/UI recipes) are engine-agnostic: factor them out once a second source engine exists.
