# Physics, player, combat, enemy AI (verified headless; not played by hand)

## Reuse
- `CollisionWorld` + `CharacterMotor` come from `../Shooter/Core/Physics` (same author); `TryStep` was extended. `LevelColliders` builds worlds from `level.scene.json`.
- Two worlds: **LevelWorld** (layer 1: camera, bullets, robot sight) and a character world. Decode `project.godot [layer_names]` into a flags enum on day one.

## Layers and masks (exactly as Godot, even when it looks like a bug)
- Bullet `collision_mask = 3` (Level + Player). Props and railings on layer 4 (Player Only) are **ignored by bullets**, as in Godot (~54 prop boxes, ~79 railing colliders). Kept like-for-like; the fix is listed in `IMPROVEMENTS.md`.
- Robot body collision uses layer 1 only; its sight and laser rays use all static layers (Godot ray mask 0xFFFFFFFF).
- `Area3D` body_entered/exit -> explicit trigger volume; copy the exit quirk (robot 20 m detection).

## CharacterBody3D.move_and_slide
Floor angle 45, 6 slides, snap 0.1, stop on slope, margin 0.001, gravity 9.8, jump 5.0, motion/rotation lerp 10, respawn y -40. Verified: lands within 0.02 of the ground probe, wall slide, 0.35 m step climbed, 0.8 m ledge blocked, jump apex 1.234 vs ideal 1.2755 (3.2% low, 60 Hz semi-implicit; acceptable). Step offset 0.4 approximates SeparationRayShape (unverified on the level's real stairs). While grounded zero vertical motion and snap 0.1 (no 60 Hz bob).

## Camera
Pivot 1.6, spring arm 2.4 at 20 deg, pitch -89.9..70 deg, FOV 75. Spring arm = ray from pivot to the desired point, camera at `hit - margin` (0.01). Aim: shoulder offset tween 0.5 s ease 0.3; aim toggle with 0.4 s hold threshold. Shake is **view-only** (physics and aim ray unaffected): trauma 0.35 then 1.5/s decay, offset back to exactly 0.

## Bullets
Swept-sphere pool at 60 Hz: 20 m/s, expires at tick 300, stops at wall face + 0.17, fire cooldown 0.4 s (3 per second). `IDamageable`/`SweepTargets` hook for enemies. Effects are additive glow billboards drawn in the HDR pass so bloom picks them up (simplification of meshes/GPU particles).

## Enemy (red_robot.gd -> RedRobot)
- State machine IDLE/APPROACH/AIM/SHOOTING; detection 20 m, sight rays, 4 s shoot timeline with a hitscan laser, health 5, trauma 13 on a player hit. Walk speed and turn rate come from root motion (`animation.md`).
- Spawn markers can float above the floor (robot marker y 0.33): snap down in the sim.
- Not done: debris, sparks, 15 s respawn (level.gd); the death effect is a glow explosion.

## Freeze / pause
While a modal is open run only the UI: the 60 Hz sim, AI, bullets, animation, shake and the accumulator stop; no wall-clock in the sim. On resume prime the input and swallow the first mouse delta.

## Input
Keyboard + arrows + d-pad + stick, RMB/LT aim, LMB/RT shoot, mouse capture by recentring with `Mouse.SetPosition`. Mouse and gamepad never exercised interactively (unverified).

## Contacts between enemies and debris (v0.4, verified headless + one shot)
- Decode layers/masks from the .tscn first (RedRobot layer 3 / mask 3; Part* RigidBody3D layer 3 / mask 3, masses 2000/2000/6600, frozen and collision-disabled until `explode()`; player layer 6 / mask 7). Consequence: robots collide with each other, debris collides with level, robots and the player, and a dead robot stops blocking (`collision_shape.disabled`).
- Cheap, adequate implementation: after each robot's move, push it horizontally out of every live peer's body sphere and every live debris sphere; feed the player capsule and live robot spheres to the debris solver as static obstacles; give the player's capsule queries transient debris spheres (never rays). Debris is treated as immovable (2000 kg+), no momentum transfer.
- Test by outcome: two robots converging on the player stay >= 2.236 m apart (without peers 2.039 m), so the test is sensitive; debris rests on the floor; the player is stopped by a part and a part dropped on the player does not pass through.
- Not done: part-part collision, respawn on top of a robot.
