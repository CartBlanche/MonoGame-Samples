# Verification

Evidence ranked: (1) compared with a Godot reference capture, (2) headless self-test with real values, (3) looks right in a shot. State which backs each claim; write "not heard" / "not played by hand" when true. Everything below was used on the TPS demo.

## Godot reference
- Capture the source **before** porting: main menu, level start, fire, hit (here 4 YouTube frames, `Documentation/reference/godot-*.png`, compressed: use for look and layout, not pixel values). Keep a README saying what each shows. Ask the user if you cannot run Godot.
- Capture the running game, not the editor viewport. Debug overlays (FPS) sit top-left: crop them (`--crop` in ImageStats).
- Keep `mono-NN-*.png` milestone shots (never overwrite; tuning shots go in `_tuning/`).

## Self-test flags (exit code = failures; dispatch in `Desktop/Program.cs`)
| Flag | Checks (TPS, 2026-11-17) |
|---|---|
| `--test-physics` | 16: landing, wall, steps, jump apex, camera ray, root-motion speed, fire cooldown, bullets, shake, robot walk/turn/hits |
| `--test-ui` | 8: settings round-trip, Apply/Cancel, focus skips disabled, message box defaults, loading plan |
| `--test-audio` | 4: bus gain, attenuation + pan, event table + xnb present, footstep phases |
- Use real baked values, not literals; synthetic worlds for edge cases, the real level for integration. Timeline assertions for AI/weapons. Anything needing a GraphicsDevice gets a shot instead.
- Reader/extractor checks live in the tools (`GodotExtract selftest` 23, `GlbCheck selftest` 116 vs an independent JSON read).

## `--sim "<script>"` (headless fixed-step driver, no human at the keyboard)
Items separated by `;`: `w|a|s|d|w+d:sec`, `look:yaw,pitch`, `tp:x,y,z,yaw`, `space[:sec]`, `wait:sec`, `aim`/`noaim`, `fire[:n]`, `robot[:k|ahead,D|x,y,z,yaw]`. Combine with `--shot` to render the end state, or `--audio-log` for evidence of sounds. Flag modes skip the menu and load everything up front.

## `--shot` / `--cam` / `--pose` / `--screen` / `--ui`
`--shot png` renders t=0 (never calls Update); `--cam x,y,z,yaw,pitch` reproduces the Godot start framing; `--pose clip,t`; `--screen menu|play-list|settings|quit|pause|loading:p`; `--ui "down,enter"` feeds keys once. One flag per state; list every flag in the README. Run shots with a timeout wrapper (`Tools/ImageStats/run_shot.sh`) because the window can hang.

## Image metrics (Tools/ImageStats/imagestats.py, stdlib PNG decoder)
- Prints mean RGB, luma, luma std, saturation, hue, 4x4 luma grid and RGB/sat grid; `--crop` ignores the overlay; `--ref ref.png` gives a same-frame grid MAE.
- Global stats of a cropped frame can all match while the image looks wrong (pass 3 vs 4): use the **spatial** metric on the **same frame** as the reference and view both PNGs before accepting.
- Stall rule: no metric gain in 3 iterations -> stop and report.

## macOS display sleep
A run (also a baseline) can hang in SwapWindow when the display is asleep: `caffeinate -u -t 2` before launching.

## Per-phase report
Verified (against what), not verified, deviations (deliberate), open questions; append a STATUS entry with Verified / Lesson / Open lines.

## Diagnosing a rendering artefact (lessons from the TPS port)
1. **Reproduce the exact pose:** ask for an annotated screenshot, then place the player with `--sim "tp:x,y,z,yaw;look:yaw,pitch;aim"`; free-camera guesses wasted many calls.
2. **Bisect, do not theorise:** debug env flags that skip a material (`TPS_SKIPMAT`), a node (`TPS_SKIPNODE`) or dump the draws projecting into a screen box (`TPS_DUMPWALL` + `TPS_DUMPBOX=x,y,r`), then diff frames (changed-pixel count + bbox).
3. **A/B against an older commit:** `git worktree add` + copy gitignored generated files; render the same recipe; per-pixel diff (0.0 proves a commit did not change that view).
4. **Temporal artefacts** (shimmer) cannot be seen in stills: render 2-4 stills 0.25 deg or 1 cm apart and compare a crop against a clean reference; say that a live moving camera was not tested.
5. No PIL needed: `Tools/ImageStats/imagestats.py` has a stdlib PNG `load()`; write zoomed crops with `zlib` + `struct`.
6. **Benchmarks:** `--bench N` CPU+GPU frame time, 3 runs per setting, report avg and p95.
