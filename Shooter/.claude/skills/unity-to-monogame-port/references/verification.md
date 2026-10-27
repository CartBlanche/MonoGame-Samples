# Verification

Three kinds of evidence, in order of strength. Say which one backs each claim.
1. **Compared with a Unity Play-mode capture** (strongest).
2. **Headless self-test** (exact values from scripts/prefabs).
3. **Looks right in a shot / ran without errors** (weakest). Say "not played by hand" / "not heard" when true.

## Unity reference capture
- Edit-mode renders are **not** faithful to Play mode (the player settles under gravity, scripts change poses). Capture the Game view in Play mode, ~1917x1081 including HUD and weapon.
- Keep `poses.json` (Unity coordinates) next to the PNGs: name, position, yaw, pitch.
- Reading a pose: select the Player in Play mode, read Transform Position and Rotation Y; the camera sits at root Y + camera local Y (1.44 standing, so 1.691 world at rest). Pitch is the Main Camera's local Rotation X (clamped +-89) and differs per shot, so capture it. Widen the Inspector so values are not clipped.
- With only a screenshot: compass heading + 90 = yaw (validated on 4 poses). Estimates from apparent size are weak; mark poses as exact / fitted / estimated.
- Player settles slightly differently from the baked editor start; record the Play-mode value.
- When behaviour is the question (enemy AI, explosions, damage), a screen recording is worth more than another round of reading code; several fixes came from user captures (alert "!", HP bar colour, bots stopping fire during the fade, trails).

## Shot harness (`--shot`)
`--shot <dir> --poses <json> [--pose <name>]` renders PNGs at the capture resolution; render each pose twice so motion blur does not smear from the previous pose. Add one flag per feature you want to inspect: `--weapon`, `--hud`, `--hud-time`, `--vfx`, `--ammo`, `--enemy-sim`, `--hit-flash`, `--alert`, `--pickups`, `--loot`, `--jetpack`, `--menu`, `--pause`, `--bars`. Put the flag in the README as you add it.

Compare by **feature positions** (wall corners, seams, doorway, markers, text boxes) as fractions of frame width/height, plus mean colour of a world-only region. Contrast-stretch crops to see near-flat walls. Typical achieved accuracy: HUD within 1-2 px, translucent overlays within ~5 levels, emissive gem within ~16 levels.

## Headless self-test (`--test-physics`)
- Prints each check, exits with the failure count. Needs a window only when content must load.
- Test the scene's real values (read from baked data), not literals.
- Pattern that worked: synthetic worlds for edge cases (zero-thickness wall, ramps, ceilings), the real level for integration, fuzz runs (random input, 60/30/15 fps) asserting no overlap and no escape, timing checks at several frame rates.
- Behaviour tests for AI/weapons: timeline assertions (first shot at 1.1167 s, 5 shots in 4 s, forgot target at 4.0 s not before 3.99), counts (hits on capsule), invariants (hitbox inside mesh bounds).
- When a formula is a port, compare against an *independent* computation (e.g. camera ray vs projectile stop point, worst 3.6 mm).
- Things needing a GraphicsDevice (HUD, bars, effects) get a shot, not a headless check; say so.
- Keep a running check count in the plan (60, 80, 98 ... 142) so regressions are visible.

## What to report each phase
Check counts (and how many new), shots taken and what they were compared with, and an explicit **Not verified** list. Do not claim "matches Unity" from a code read.
