# First Person Shooter (MonoGame port of Unity's FPS Microgame)

A like-for-like MonoGame port of the Unity FPS Microgame (`../FirstPersonShooter`).

## Layout

```
Core/                 Shared game library (net10.0) and content (Content/Shooter.mgcb)
Platforms/Desktop/    DesktopGL launcher (+ Info.plist, app.manifest, Shooter.icns for macOS / Windows packaging with MonoPack)
Platforms/Android/    Android launcher (net10.0-android; resources and icons generated from Core/Content/Icon.png)
Tools/UnityExtractor/ Converts Unity scenes/prefabs (YAML) to JSON for the port
.claude/skills/       Claude Code skill (unity-to-monogame-port) that captures the porting workflow (see below)
```

## Porting skill (Claude Code)

The workflow used to make this port is captured as a reusable Claude Code skill, `unity-to-monogame-port`, for the next Unity project. The skill lives in this repo at `.claude/skills/unity-to-monogame-port/`. Claude Code loads project skills from `.claude/skills/` automatically, so there is nothing to install: clone the repo, start Claude Code **from inside the `Shooter` folder** (`cd Shooter && claude`) and the skill is available. Starting Claude Code from the repo root or another sample folder will not load it.

To use it on a Unity port that lives elsewhere, copy it to your personal skills folder once, then restart Claude Code:

| OS | Personal skills folder |
|---|---|
| macOS / Linux | `~/.claude/skills/` |
| Windows | `%USERPROFILE%\.claude\skills\` |

```bash
# macOS / Linux (run from the Shooter folder)
mkdir -p ~/.claude/skills
cp -R .claude/skills/unity-to-monogame-port ~/.claude/skills/
```

```powershell
# Windows (PowerShell, run from the Shooter folder)
New-Item -ItemType Directory -Force "$env:USERPROFILE\.claude\skills" | Out-Null
Copy-Item -Recurse -Force .claude\skills\unity-to-monogame-port "$env:USERPROFILE\.claude\skills\"
```

`SKILL.md` must end up at `skills/unity-to-monogame-port/SKILL.md`. Re-run the copy to update; if you improve the skill, edit the copy in this repo and re-copy it.

```
unity-to-monogame-port/
  SKILL.md              workflow, phases, per-mechanic loop, rules (loaded when the skill triggers)
  references/           conventions, assets, pipeline, systems, pitfalls, verification, packaging, commit-groups
  examples/phase-entry.md   model entry for Tools/PORTING-PLAN.md
```

Use it by asking Claude Code to port a Unity project, scene, prefab or mechanic to MonoGame (for example "port the Unity project at ../MyGame to MonoGame, scope: MainScene"); it triggers on its description, or type `/unity-to-monogame-port`. It reads `SKILL.md` first and opens a reference file only when the task needs it. Its rules: regenerate generated content instead of editing it, copy the scene's values rather than script defaults, stop and report after each phase with a "Not verified" list, and never stage or commit (it proposes commit groups for you).

This folder is the worked example the skill points to: `Tools/PORTING-PLAN.md` (log), `Core/Content/ASSET-CONVENTIONS.md`, `Tools/UnityExtractor/README.md` and `Tools/COMMIT-GROUPS.md`. Fix something in the skill's references when a port teaches you something new.

## Build and run

```
dotnet run --project Platforms/Desktop/Shooter.csproj
```

Android: `dotnet build Platforms/Android/Shooter.csproj` (needs the `android` workload and a JDK). It builds an APK with the content, but the game only reads keyboard and mouse, so there are no touch controls yet and it has not been run on a device. Icons: `Core/Content/Icon.png` is the 1024 x 1024 master (made with `--icon`); `Core/Content/android-icons-generator.sh` writes the Android mipmaps / drawables, `Icon.ico` and `Icon.bmp`; `Platforms/Desktop/generate-icns.sh` writes `Shooter.icns`.

Status: Phase 3 (rendering) verified for the static scene; Phase 4 milestones 1-2 (first-person controller + world collision; weapon handling and projectiles) done and tested headlessly; the in-game HUD (crosshair, ammo, health, stance, compass, pause hint, objective message and toast) is in (4c-2); effects, enemies, audio not started. See `Tools/PORTING-PLAN.md`.

## Controls (current build)

The desktop build starts at the intro screen (Play, Controls). `--freefly` starts as a scene inspector with a free-fly camera over MainScene instead. `--play` (or F2 at runtime) switches to the first-person player; F2 again goes back to free-fly at the player's eye position.

Play mode (Unity bindings): W A S D / arrows move, mouse looks (0.2 degrees per pixel like Unity's x0.001 x RotationSpeed 200), Shift sprints, C crouches, Space jumps, left mouse fires the Blaster (hold for automatic fire), right mouse aims (zoom, slower look). Also: I J K L look (for machines without a captured mouse), Tab releases / captures the mouse, R respawns, Esc exits. Dying (or falling past the kill height at y = -50) fades to black over 3 s and opens the Lose screen; killing both enemies completes the objective, shows the win message after 2 s and opens the Win screen after 7 s. Both screens offer Play Again (reloads the level) and Menu (the intro screen: Play, Controls). `--intro` starts at the intro screen.

Free-fly:

| Key | Action |
| --- | --- |
| Tab | capture / release the mouse |
| W A S D, Q / E (or Ctrl / Space) | move, down / up; Shift is faster |
| N / P | next / previous pose from `poses.json` |
| F | toggle the weapon viewmodel |
| C | toggle culling |
| Esc | exit |

## Screenshots against the Unity reference

```
dotnet run --project Platforms/Desktop/Shooter.csproj -- --shot <outDir> --weapon \
    --poses Tools/UnityReference/PlayMode/poses.json [--pose <name>]
```

Renders 1280x720 PNGs, one per pose. Compare them with `Tools/UnityReference/PlayMode/game/`; the method is in the porting plan.

Add `--hud` to draw the in-game HUD (the Unity captures include it). `--hud-time <s>` sets the HUD clock: the default 1 s shows the centre objective message (the first seconds of play), 4.5 s or more shows the objective toast at the top left instead. `--hud-target` draws the crosshair as if an enemy were in the sights. `--enemy-sim <s>` puts the player at each pose and runs the enemy AI for s seconds before rendering (patrol, detection, animation, eye colour; the bot closes in to about 1 m, so keep it short). `--hud-kills <n>` kills the first n enemies before the HUD is drawn (objective counter, notification, compass markers of the rest; the Turret is enemy 0). `--hud-health <ratio>` damages the player down to that ratio just before the HUD is drawn (health bar, a 0.05 s old damage flash, steady critical vignette at 0.3 or below). `--pickups <s>` runs the health pickups (bob, spin, glow particles) for s seconds before the shot and `--loot` kills the HoverBot first so its Loot_Health shows where it died, `--pick` hurts the player and steps into the first pickup so the heal burst shows. `--icon <file>` renders the Blaster alone into a transparent PNG (default 1024 x 1024, `--icon-size`; `--icon-yaw` / `--icon-pitch` / `--icon-roll` orbit the camera, `--icon-flash <s>` sets the age of the muzzle flash shown in front of the muzzle, 0 for none, `--icon-blast <m>` adds a shortened bolt) and exits: it is how `Core/Content/Icon.png` was made (render on white and black, matte from the difference). `--jetpack <s>` puts the player on the Jetpack pickup (it unlocks), jumps, presses jump again in the air and holds it for s seconds, then renders the pose with the HUD (fuel bar, toast, trails). `--hit-flash <s>` gives the HoverBot a player hit and runs the enemies for s seconds (white body flash; 0.05 is its brightest). `--vfx [seconds]` spawns the Blaster muzzle flash and an impact 8 m ahead of the camera, aged by that many seconds. `--ammo <ratio>` draws the weapon with that ammo ratio (gem colour, fuel cells, steam with `--vfx`).

## Player controller self-test

```
dotnet run --project Platforms/Desktop/Shooter.csproj -- --test-physics
```

Runs scripted input against synthetic worlds and the real level, for the player controller and the weapons (needs a window for the content load), prints each check and exits with the number of failures as exit code.
