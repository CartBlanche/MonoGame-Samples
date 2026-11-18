# Animation (verified on the TPS player and red robot)

## Generic AnimationPlayer (Core/Animation)
- API shape: `BeginFrame` / `Add(clip, time, weight)` / `Finish` + `RootMotion` output; `AddOverlay` for bone-filtered blends and one-shots. Reusable across characters.
- **Root bone name is a constructor parameter** (player `root`, robot `MASTER`). Root rotation delta is opt-in (`ExtractRootRotation`; the robot turn clips need it, the player does not).
- **Root motion goes through the root's parent chain to model space.** Robot_Skeleton carries a 0.803991 scale; with it per-cycle distances come out clean (2.667 m, 1.5 m), without it they do not. The root bone is held at bind translation in the visual pose so the mesh does not travel; the delta moves the capsule.
- Root motion speed is the source of truth for walk speed: running_nogun 3.200 m/s (x1.3 timescale = 4.16 in play), strafe 1.8 (x1.2 = 2.16), robot walk 0.800 m/s, robot turns 27 deg/s. Do not guess a speed (a temp 4.5 m/s was wrong).
- Handle loop wrap when taking the delta between two times. Pose updates run at the 60 Hz tick.

## AnimationTree mapping (player.tscn)
| Godot node | Port |
|---|---|
| Animation / StateMachine (walk, strafe, jump_up, jump_down, 0.2 s xfade) | small hand-coded state machine with cross-fade |
| BlendSpace2D (strafe) | barycentric blend over the 4 triangles of the point set |
| BlendSpace1D | linear blend between two points |
| TimeScale | multiply clip time (also scales root motion) |
| Add2/Add3 (AIM-Up/Down) | **additive layer relative to the rest pose**, not to frame 0 of another clip |
| Blend2 + bone filter | `AddOverlay` with a bone set (menu: `eyes` clip on eyelid/eyes bones) |
| OneShot | one-shot overlay (robot Damage_take*); drop it if the .gd never requests it (player land OneShot) |
| Transition (robot locomotion) | 0.5 s xfade |

- Read **both** the AnimationTree and the .gd: which parameters are actually set decides what to port (player land OneShot, eyes layer, running_gun were unused and dropped).
- Aim: AIM-Up/Down is a rest-relative additive layer (no Godot frame to compare: still unproven by capture). Robot aim: blend on 3 cannon bones.
- A clip name used by any tree (including the **menu** scene) must be in the processor ClipWhitelist (`pipeline.md`).
- When a reference pose does not match, check the original AnimationTree for the clip name before blaming camera or lighting.

## Events inside clips
- AnimationPlayer method/audio tracks hide gameplay: robot `shoot_check`, step and sound keys. Port them as per-clip events with times (robot walk sound keys 0.8 s and 2.033 s; shoot timeline 4 s with the laser at 2.05 s; clip freeze 2.6-2.9 s).
- Footsteps are not in the player clips: derive foot-contact phases from foot-bone heights (`audio.md`).

## Tips
- `--pose clip,t` shows any clip at time t; `--shot` is always t=0 (it never calls Update).
- Test with real data: assert root-motion speed against the sim speed (`--test-physics`).
