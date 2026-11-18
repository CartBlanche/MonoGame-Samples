# 03 - Player, camera, animation, input (Agent 3)

Sources read: `player/player.gd`, `player_input.gd`, `camera_noise_shake_effect.gd`, `player.tscn` (nodes, AnimationTree, camera rig, camera AnimationPlayer), `bullet/bullet.gd`, `project.godot` input map. Conventions follow 01/02 (right-handed Y-up, -Z forward for cameras, 1 unit = 1 m, XNA row-vector `Matrix`, no axis flips, clips at 30 fps).

**Key finding: the sibling `MonoGame-Samples/Shooter` (FPS Microgame port) already contains reusable, working C# for exactly this domain** (all under `Shooter/Core/`): `Physics/CharacterMotor.cs` (capsule, slope limit, step offset, skin width, CapsuleCast/Recover), `Physics/CollisionWorld.cs` (boxes/spheres/triangles, rays, capsule cast, dynamic spheres/boxes, owner ids), `Animation/AnimationPlayer.cs` (layers, bone masks, additive layers, CrossFade, `CopySkinMatricesTo`, `SetBoneModelRotation`), `Animation/AnimatorController.cs` (state machine driven by params), `SkinnedModelReader`/`AnimationClipReader`. Plan: copy these into `ThirdPersonShooter/Core/{Physics,Animation}` and adapt (namespace, root motion, blend spaces) rather than rewrite.

## 1. GDScript behaviours and constants

### Player (`player.gd`, CharacterBody3D)
| Behaviour | Godot constant / logic | MonoGame equivalent |
|---|---|---|
| Collision shape | `CapsuleShape3D` default (radius 0.5, height 2.0) at local y=1; + `SeparationRayShape3D` (default length 1.0) at y=1.00357 pointing down (floor stick on stairs). Layer 6 (=Level? no: bits 2+3 = Player, Player Only), mask 7 (Level, Player, PlayerOnly) | `CharacterMotor(world, height 2.0, radius 0.5, slope 45, stepOffset ~0.3, skin ~0.01)`. Separation ray = floor snap: after move, if was grounded and not jumping, capsule-cast down by 0.1 (Godot `floor_snap_length` default 0.1) plus step-down for stairs (ray reach ~1.0 from y=1.0 means feet probe to y~0.004) |
| Horizontal velocity | NOT a speed constant: `h_velocity = orientation.origin / delta` where `orientation.origin` = accumulated **root motion** of the `root` bone for this frame, rotated by the model orientation (`orientation *= root_motion`) | Extract root-bone translation delta per frame from the playing clips (blend-weighted, time-scaled), rotate by model yaw, divide by dt. See section 3 root motion |
| Gravity | `velocity += get_gravity()*delta`; project has no gravity override so default 9.8 m/s^2 along -Y (Godot default 3D gravity 9.8) | `const Gravity = 9.8f`; same semi-implicit order (add, then move) |
| Jump | `JUMP_SPEED = 5.0` sets `velocity.y`; allowed when `airborne_time <= MIN_AIRBORNE_TIME (0.1)`; after jump `airborne_time = 0.1` so on_air stays true next frame; jump flag consumed every frame | same; `jumping` latched by key-press edge, cleared each tick |
| Airborne | `airborne_time += delta`; `is_on_floor()` resets to 0; `on_air = airborne_time > 0.1`; landing event if `airborne_time > 0.5` at touchdown (plays Land sound, anim JUMP_DOWN) | same. `IsOnFloor` from motor (normal.y > cos 45 deg) |
| Floor | `set_up_direction(UP)`, defaults: floor_max_angle 45 deg, `floor_stop_on_slope` true, `floor_snap_length` 0.1, max_slides 6, safe_margin 0.001 | `CharacterMotor.IsWalkable` slope 45 deg; collide-and-slide up to 6 iterations; stop-on-slope: zero horizontal when grounded and no input |
| Motion smoothing | `MOTION_INTERPOLATE_SPEED = 10`: `motion = motion.lerp(input.motion, 10*delta)` (note: not framerate-independent; copy as is) | same formula |
| Facing (walk) | target = camX*motion.x + camZ*motion.y (camera basis flattened, normalised); if len > 0.001 slerp `orientation` toward `looking_at(target)` by `ROTATION_INTERPOLATE_SPEED=10 * delta`. Note `camera_z` is the camera's +Z (backwards) and motion.y = back-forward, so it yields forward = -camZ | `Quaternion.Slerp(q, lookAtQuat, 10*dt)`. Godot `Basis.looking_at(target)` makes **-Z** point at target. The model front is +Z (per 01), so Godot's glb import already compensates (the model node is rotated); we must verify by test: the player model must walk "face first". Build the look-at as `CreateWorld(0, target, up)` and test; add a fixed model yaw of 0 or pi to match |
| Facing (aiming) | slerp `orientation` toward `camera_base` quaternion (yaw only) at 10*delta | same; camera base yaw quaternion |
| Rotation of model | `player_model.global_transform.basis = orientation.basis` (body capsule never rotates) | model world = `Scale(1)*Rotation(orientation)*Translation(body.pos)` |
| Respawn | y < -40 -> teleport to `initial_position` (position at `_ready`) | same; spawn from level.scene player Marker3D (4 player spawns exist in level) |
| Fall fade (input script) | y < -17: black overlay alpha = min((-17 - y)/15, 1); else alpha *= `1 - delta*4` | full-screen black quad in UI (agent 5 owns UI; I provide `FadeAlpha`) |
| Model | `Robot_Skeleton` scale 0.803991 on the glb skeleton node; BoneAttachment `GunBone` on `hand.R` (bone 55), local xform rows (0.0889,0.3609,-0.9321 / 0.4157,0.8376,0.3640 / 0.9091,-0.4184,-0.0753) origin (-0.3947,1.0767,-0.0920); `ShootFrom` Marker3D at GunBone-local (0,0.4,0) | Muzzle world = `ShootFromLocal * GunBoneLocal * handR bone world * RobotSkeletonScale(0.803991) * modelWorld`. `AnimationPlayer.GetBoneTransform("hand.R")` exists in Shooter port. Needs agent 1 to expose the skeleton node scale (0.8039) and keep bone `hand.R` + `root` through joint pruning |

### Shooting (player side)
| Item | Godot | MonoGame |
|---|---|---|
| Fire gate | only while `aiming` (and on ground); `shooting` = `shoot` action held; `FireCooldown` Timer `wait_time = 0.4`, one_shot, autostart. Cooldown restarts in `shoot()` | `fireCooldown` float; fire when `<= 0`; set 0.4 on shot. Semi-auto cadence = 2.5 shots/s while held |
| Aim ray | `Input` script: ray from camera through crosshair centre (screen centre = `crosshair.position + size/2`), length 1000, mask 0b11 (Level + Player), exclude self; hit -> `shoot_target = col.position`, else `ray_from + dir*1000`. Computed only while shooting | `CollisionWorld.Raycast` from camera pos through screen centre (`Viewport.Unproject` at centre or just camera forward). Mask: Level + other players (we are single-player: Level only) |
| Bullet spawn | origin = ShootFrom world pos; dir = normalize(target - origin); bullet CharacterBody3D, `look_at`, collision exception with player | `Projectile` struct pool; `Position = muzzle`, `Dir` |
| Bullet motion | `BULLET_VELOCITY = 20 m/s` along -Z of bullet basis; lifetime 5.0 s; `move_and_collide` (sphere r=0.170413); on hit: if collider has `hit()` call it; disable collision; explode (animation `explode`, light) | per-tick sphere sweep vs `CollisionWorld` (sphere cast) + enemy sphere/capsule list from agent 4; hit -> `IHittable.Hit()`; explode FX (agent 2 particles/light, agent 5 sound). Bullet mask details are in bullet.tscn (unread; agent 4 to confirm enemy layer) |
| Fire FX on player | `ShootParticle` CPUParticles3D: 10 particles, life 0.3, one_shot, explosiveness 0.79, tiny SphereMesh r=0.003 h=0.02, dir (0,0,-1), spread 15, gravity (0,-3,0); `MuzzleFlash`: 1 particle, life 0.1, mesh `muzzle_flash_particle_mesh_Cone.mesh`, quad texture `FlarePolar.png`, colour (0.125,0.627,1.0,0.376), scale 0.75 with curve (0,0)->(0.79,0.99)->(1,0), unshaded, cull none | agent 2's CPU particle system; I fire `OnShoot(muzzleWorldMatrix)` event |
| Camera kick on shot | `add_camera_shake_trauma(0.35)`; enemy hit on player `hit()` adds 0.75 | `CameraShake.AddTrauma` |
| Ammo / health | **None in the demo.** Player has no health, no ammo, no death; only fall-respawn | nothing to port (do not invent) |
| Sounds | jump, land, shoot (AudioStreamRandomizer), step (random pitch, played by anim method tracks) | event hooks `OnJump/OnLand/OnShoot/OnFootstep`; agent 5 plays |

### Camera rig (`player.tscn`, `player_input.gd`)
Node chain under Player: `CameraBase` (Node3D at (0,1.6,0), yaw only) -> `CameraRot` (pitch only) -> `SpringArm3D` (spring_length 2.4, default margin 0.01, collision mask 1 = Level, no shape = ray) -> `Camera3D` (far 300, default FOV 75, near 0.05). SpringArm local basis is a fixed 180 deg yaw + 20 deg pitch: rows `(-1,0,0)(0,0.9397,0.3420)(0,0.3420,-0.9397)`.
| Behaviour | Constant | MonoGame |
|---|---|---|
| Pivot | player pos + (0,1.6,0); yaw `rotate_y(-dx)`, pitch `rotation.x += dy` | `yaw`, `pitch` floats; pitch positive = looking down |
| Mouse sensitivity | `CAMERA_MOUSE_ROTATION_SPEED = 0.001` rad per pixel (`screen_relative`), x0.75 when aiming | `dx = mouseDelta.X * 0.001`. Use raw delta (not scaled by window DPI); note macOS retina pixels vs points: test feel |
| Stick sensitivity | `CAMERA_CONTROLLER_ROTATION_SPEED = 3.0` rad/s * dt, x0.5 when aiming | same; MonoGame thumbstick Y is +up so `view = (rx, -ry)`. Deadzone 0.2 |
| Pitch limits | min -89.9 deg, max +70 deg (`CAMERA_X_ROT_MIN/MAX`) | clamp |
| Camera position | arm direction (local +Z of arm in CameraRot space) = (0, 0.342, -0.9397); camera at pivot + CameraRotation * (0,0.342,-0.9397) * len. Default len 2.4 => offset (0,0.82,-2.255) (behind and above); looks along (0,-0.342,0.9397), i.e. 20 deg down at pitch 0 | `camPos = pivot + Rot(yaw,pitch) * armDir * len`; view = look-at with the arm basis (camera orientation = CameraRot * armBasis). Camera local forward is -Z; since arm basis includes a 180 deg yaw, the view direction in CameraRot space is +Z |
| Spring-arm collision | length shortened to hit distance minus margin 0.01 (instant, no smoothing) against layer 1 | `CollisionWorld.Raycast(pivot, camPos)`; use `min(len, hit - 0.01)`; also sphere r ~0.1 optional (Godot ray has no shape) |
| Aim zoom/shoulder | `Animation "shoot"` (0.5 s): `SpringArm3D:position` (0,0,0) -> (-0.6,-0.3,1.1) with transition curve 0.3 (strong ease-out), crosshair `self_modulate` alpha 0->1 over 0.2 s. `"far"` is the reverse. Position is in CameraRot space (pitches with camera). X -0.6 puts the camera on the character's right shoulder | `armOffset` Vector3 tweened over 0.5 s with Godot ease(t, 0.3) = `pow(t, 0.3)`-style (Godot ease with negative/fractional curve; for 0<c<1 it is `1 - pow(1-t, 1/c)`-like ease-out; verify by sampling in-engine values or just use `1-(1-t)^3` as ease-out approximation). Reverse tween on release; tween restarts from the current value (Godot animation restarts from key 0: small pop; replicate by lerp from current) |
| FOV | no FOV change on aim (default 75) | fixed 75 (agent 2 assumed same) |
| Aim toggle | RMB / left trigger. Hold > `AIM_HOLD_THRESHOLD 0.4 s` = hold-to-aim (release stops); short tap (release at <= 0.4 s) = toggle stays aiming until pressed again (`toggled_aim`) | port the exact small state machine (`aiming_timer`, `toggled_aim`) |
| Trauma shake | `camera_noise_shake_effect.gd`: SPEED 1, DECAY_RATE 1.5/s, MAX_YAW 0.05, MAX_PITCH 0.05, MAX_ROLL 0.1 rad, MAX_TRAUMA 1.2; `shake = trauma^2`; time += dt*5000; noise = OpenSimplex 1D, seeds s, s+1, s+2, 1 octave; rotation = start + (pitch,yaw,roll) | `CameraShake`: use any smooth 1D noise (own simplex/value noise; sample x = time*5000 is a very fast sample rate in noise units: FastNoiseLite default frequency 0.01 => effective freq 50 Hz; set freq = 0.01 and keep time*5000) |
| Crosshair | 16x16 texture `crosshair.png` at screen centre, hidden when not aiming | agent 5 draws; I expose `CrosshairAlpha` |
| Player fade | agent 2: mesh dither fade by camera distance (0.2..0.8 m) | I expose `CameraPosition`; relevant when SpringArm collapses into walls |

### Input map (`project.godot`) -> MonoGame
| Action | Godot bindings | MonoGame binding | Notes |
|---|---|---|---|
| move_left/right/forward/back | A/D/W/S physical keys, arrow keys, d-pad, left stick axes 0/1 (deadzone 0.2) | Keyboard + `GamePad.ThumbSticks.Left` (Y +up: invert) + DPad | strength = analog 0..1; `motion = (right-left, back-forward)`; no normalisation (diagonal length 1.414 clamps only in blend space) |
| view_left/right/up/down | right stick axes 2/3 | right thumbstick | only gamepad; mouse via motion events |
| jump | Space (physical), pad button 0 (A) | Space, A | edge-triggered, consumed once |
| aim | RMB, left trigger (axis 4) | RMB, LT>0.5ish | hold/toggle logic above |
| shoot | LMB, right trigger (axis 5) | LMB, RT | held |
| quit | Escape, pad button 6 (Back/Start) | Esc, Back | -> pause/menu (agent 5) |
| toggle_fullscreen | key 4194342 (F11), Enter 4194309 | F11 | agent 5 |
| toggle_debug | key 4194334 (F3?) | F3 | agent 5/dev overlay |
| ui_* | arrows, Enter, Space, pad | menu nav | agent 5 |
Mouse capture: Godot `MOUSE_MODE_CAPTURED`. MonoGame DesktopGL: `IsMouseVisible=false`, recentre each frame with `Mouse.SetPosition(w/2,h/2)` and use the delta from centre (SDL relative mode via reflection on `Sdl.Mouse.SetRelativeMouseMode` is an optional upgrade); release on pause/menu/focus lost. Input class: `InputActions` with `Pressed/JustPressed/JustReleased/Strength` mimicking `Input.get_action_strength` so ported code reads the same.

## 2. Physics / character controller choice

Options: (a) BepuPhysics v2 NuGet; (b) small custom collider (capsule vs boxes/triangles, rays); (c) reuse Shooter port code.

Decision: **(c) = (b), reusing `Shooter/Core/Physics`** (`CollisionWorld` + `CharacterMotor`). Justification: level colliders are almost entirely boxes (447 `CollisionShape3D`, 31 `BoxShape3D` sub-resources, plus sphere/capsule/radial colliders and a few convex hulls per agent 1), all static; the demo has no rigid-body gameplay except red-robot debris (cosmetic, agent 4) and the player (kinematic character). The Godot demo uses `move_and_slide`/`move_and_collide`, not simulation, which maps 1:1 onto a collide-and-slide motor. Bepu would add a heavy dependency, a different character model, and no benefit for ray/sphere queries we already have working. Risk accepted: capsule vs OBB (and convex hull if any: agent 1 lists hulls) must be implemented/verified in `CollisionWorld` (it has `AddBox`, `AddSphere`, `AddTriangle`, `DeepestContact`, `CapsuleCast`, `Raycast`); add layers + collision exceptions (owner ids exist for dynamic bodies; add a mask int) and a hull-as-triangles fallback.

Port plan for motion (`PlayerController.Tick(dt)`; fixed 60 Hz physics step like Godot `_physics_process`, camera/input in `Update`; render interpolation of the body position: project sets `physics_interpolation=true`, so interpolate body/model/camera pivot between ticks - recommended, else the camera jitters at 60 Hz sim vs vsync):
1. `motion = lerp(motion, input, 10*dt)`.
2. airborne/jump/aim/walk branches as in the table; compute `rootDelta` from animation.
3. `vel.XZ = rootDelta / dt`; `vel.Y -= 9.8*dt` (Godot adds to full velocity, and floor resets Y velocity inside `move_and_slide`; reproduce: when grounded after slide, `vel.Y = 0` and snap down 0.1).
4. `motor.MoveAndSlide(ref pos, ref vel, dt)`: up to 6 slides, floor test normal.y > 0.7071, slide along wall planes, snap.
5. y < -40 respawn.
Step-up: capsule r=0.5 with root-motion speeds has no stair handling in Godot besides the SeparationRay shape (it pushes the body up over small steps). Use motor `stepOffset` ~0.3-0.4 (tune against the level stairs `stair_pillar`; open question 3).

## 3. Animation system design (on agent 1's `SkinnedModelContent`)

Godot AnimationTree layout, exactly (from `player.tscn` sub-resources):
```
output <- eye_blend (Blend2, filter: eyelid.d/u.L/R, eyes; amount 1.0)
            in0 <- aim (Add3, filter: neck.001, upper_arm.R; add_amount = aim pitch -1..1)
                     in0(-1) <- AIM-Down   in1(base) <- land (OneShot, fade 0.25/0.25)   in2(+1) <- AIM-Up
                     land: in0 <- state(Transition xfade 0.2, reset=true), in1 <- jump_5_hardlanding
                     state: 0 "strafe" <- strafe_speed(TimeScale 1.2) <- strafe(BlendSpace2D)
                            1 "walk"   <- walk_speed(TimeScale 1.3)   <- walk(BlendSpace2D)
                            2 "jump_up"   <- jump_2_upwards
                            3 "jump_down" <- jump_4_falling
            in1 <- eyes (animation "eyes")
root motion track = Skeleton3D:root
```
Parameters driven by script: `state/transition_request` ("jump_up"/"jump_down"/"strafe"/"walk"); `aim/add_amount` = `get_aim_rotation()` while strafing else 0; `strafe/blend_position = (motion.x, -motion.y)`; `walk/blend_position = (motion.length(), 0)`. `land` OneShot is never requested by script (hardland unused); `eye_blend` constant 1.0.
BlendSpace2D `strafe`: points (1,0)=strafe_left, (-1,0)=strafe_right, (0,-1)=strafe_back, (0,1)=strafe_front, (0,0)=Idlecombat. `walk` (min (0,0), axes speed/gun): (0,0)=Idle, (1,0)=running_nogun, (1,1)=running_gun, (0,1)=Idlecombat; script only ever uses y=0 so effectively a 1-D lerp Idle->running_nogun by clamp(motion.length, 0, 1).

Aim pitch -> add amount: `x>=0: -x/70deg` (look-down => -1 => AIM-Down), `x<0: x/(-89.9deg)` (look-up => +1 => AIM-Up) (pitch sign per camera section; keep the code verbatim).

**Clip whitelist for agent 1 (answer to the request in `_SHARED.md`)** - names as in `player.glb`, exact case: `Idle`, `Idlecombat`, `running_nogun`, `running_gun` (optional; unused at gun=0), `strafe_left`, `strafe_right`, `strafe_front`, `strafe_back`, `jump_2_upwards`, `jump_4_falling`, `jump_5_hardlanding` (optional; node exists but never triggered), `AIM-Up`, `AIM-Down`, `eyes` (optional, cosmetic). Minimum for the slice: 10 clips (drop running_gun, hardland, eyes). Footstep `step` sounds are method tracks inside the glb animations or tscn; agent 1 should list any "footstep" markers/extras. **Bones that must survive joint pruning even without vertex weights**: `root` (root-motion source), `hand.R` (gun attachment, bone 55), `neck.001`, `upper_arm.R`, eyelid/eye bones if eyes are kept. Also keep the `root` track in every whitelisted clip (do not bake it away) and request each clip's loop flag (walk/strafe/idle loop; jump_up/jump_down: check, likely non-looping and held on last frame).

Runtime design (C#, namespace `ThirdPersonShooter.Animation`; copy `AnimationPlayer`/`AnimationLayer`/`Pose` from Shooter):
- `AnimTree` = small hand-coded graph, not a general node-editor: `PlayerAnimator` class holding: `BlendSpace1D walk`, `BlendSpace2D strafe` (4 triangles around the centre point; barycentric weights; clamp blend position to the diamond, Godot clamps to the hull), `StateTransition state` (current state, xfade 0.2 s linear, `reset=true` => new state's clip time starts at 0 when it becomes active; clips keep their own time otherwise), `TimeScale` multipliers 1.2/1.3, then an **additive masked layer** (Add3 over `neck.001`, `upper_arm.R`, weight = |aim| selecting AIM-Up or AIM-Down; AIM clip sampled at its (probably single-pose or looped) time).
- Pose blending: each leaf clip samples (`Vector3` T, `Quaternion` R, `Vector3` S per bone); weights blend as normalized weighted sum (lerp T/S, nlerp R with sign fix); cross-fade between states = blend of two full sub-poses by `t/0.2`.
- Additive semantics: Godot 4 applies additive animation as delta from the bone rest pose (open question 4); implement `delta = clip * rest^-1` for rotation (`R_out = R_base * (R_rest^-1 * R_clip)` slerped by amount from identity) and `T_out = T_base + amount*(T_clip - T_rest)`; restricted to the filter bones (`neck.001`, `upper_arm.R`).
- **Root motion**: Godot's `get_root_motion_position/rotation` are the per-frame *delta* of the `root` bone track (weighted sum across the active animations, scaled by TimeScale since the delta follows the advanced time), in the AnimationTree `root_node` (PlayerModel) space; the `root` bone track is removed from the visual pose (bone stays at rest). Implement in the sampler: for each active clip compute `rootT(t_now) - rootT(t_prev)` with loop wrap handling (add `rootT(end)-rootT(prev)+rootT(now)-rootT(0)` on wrap), multiply by blend weight, sum; set the root bone's pose to its rest/bind pose. Rotation delta likewise (the script multiplies it in but results are small/zero for these clips). Use only the weights of the *current* walk/strafe state during cross-fades (including fading-out state, as Godot does).
  - **Scale question**: root translation is in skeleton/bone units; `Robot_Skeleton` has scale 0.803991 (and the glb armature may also scale). Whether Godot's root motion delta is multiplied by that scale depends on how the root track is accumulated; resolve empirically: measure walking speed in real Godot if the user runs the demo, else make the world speed `rootDelta * skeletonScale` and compare against level proportions (a robot walking ~ 4-5 m/s is plausible). Expose `PlayerTuning.RootMotionScale` (default 0.803991, test 1.0).
- Output: `Matrix[] palette = inverseBind * pose` up to 72 (agent 2), `CopySkinMatricesTo(palette)` from the Shooter code.
- Events from animations (footstep, jump, land, shoot): script calls and tscn method tracks -> explicit C# events (`OnJump`, `OnLand`, `OnShoot`, `OnFootstep` from the footstep frames of `running_nogun`/`strafe_*` if present in glb; else timer/stride-based trigger).
- IK: none in the player tree; only aim-up/down additive. No IK needed.
- Multiplayer: `rpc`, `MultiplayerSynchronizer`, `player_id`, `ServerSynchronizer` are dropped (single local player, `multiplayer.is_server()` branch always true; the `current_animation` replication path is unnecessary).

## 4. Interfaces

Produces:
- `[agent3] produces` `IPlayerView { Vector3 Position; Matrix ModelWorld; Matrix[] SkinPalette; Vector3 CameraPosition; Matrix View; Matrix Projection(fov 75, near 0.05, far 300); float CrosshairAlpha; float FadeAlpha; Matrix MuzzleWorld }` for rendering/UI.
- `[agent3] produces` events `OnShoot(muzzleWorld)`, `OnJump`, `OnLand`, `OnFootstep`, `OnCameraShake`; `bool IsOnFloor`, `Func<Vector3, (bool,float y,Vector3 normal)> GroundProbe` for agent 2's blob shadow.
- `[agent3] produces` `Projectile` system (`ProjectileManager.Spawn(origin, dir, owner)`, 20 m/s, 5 s life, sphere r 0.17, hit callback) for agent 4/2.
- `[agent3] produces` `CollisionWorld` loaded from `level.scene` colliders (shared with agent 4 for enemy movement/line of sight): API `Raycast`, `SphereCast`, `CapsuleCast`, layers.
Expects:
- agent 1: bones `root`, `hand.R`, `neck.001`, `upper_arm.R` kept; clip whitelist above with loop flags, root track retained, skeleton scale (0.803991) and bone rest transforms; player prefab `player.json` with capsule/spawn; collider list with Godot layer bits; player spawn Marker3D transforms (4 available); confirmation of the 72-bone split (agent 2's question) since split-by-bone-subset affects `CopySkinMatricesTo`.
- agent 2: skinned effect taking `Matrix[]` palette; camera Position for dither; particle API for muzzle flash/shoot particles; blob shadow consuming `GroundProbe`; answer to agent 2: scripts never change Environment (no glow/damage flash). The only screen effect is the black fall fade (`ColorRect`), done in UI.
- agent 4: `IHittable { void Hit(Vector3 point); }` for bullets; red robot's `hit` on the player calls `Player.Hit()` -> `AddTrauma(0.75)` (the player has no health); enemy layer/sphere for projectile sweeps; enemy lasers also raycast the player capsule (shared `CollisionWorld` dynamic capsule owner id).
- agent 5: crosshair texture + alpha, pause/mouse-release API (`InputCapture.Set(bool)`), key remap decisions, sounds bound to the events.

## 5. Risks / open questions
1. **Root-motion speed and scale** (see section 3): the demo's player speed is entirely animation-driven; scale (0.804) and whether `root` bone data survives agent 1's pruning decide movement feel. Needs a Godot reference measurement (e.g. user-supplied video/recording or numbers from `Debug` print of `velocity`).
2. **Additive blend semantics of Add3** (rest-relative vs first-frame) for the AIM-Up/Down clips; wrong choice twists the neck/arm. Verify visually; both are a one-line change.
3. **Stairs / step handling**: Godot uses a SeparationRayShape on the capsule rather than a step offset; `stair_pillar` and slopes need a test. A step offset in the motor approximates it; check on the real level before declaring done.
4. **Physics tick vs render**: Godot has physics interpolation enabled; copy fixed-step + interpolation or the third-person camera will micro-stutter.
5. **Facing convention** for `Basis.looking_at` vs model +Z front (see table). Easy to get 180 deg wrong; self-test with a debug arrow.
6. **Camera spring ease curve**: the `shoot`/`far` tween uses transition value 0.3 (Godot ease curve), approximation acceptable but compare against reference frames.
7. Mouse capture on macOS DesktopGL (`SetPosition` recentre) may lose events at high DPI/fullscreen; fallback to SDL relative mode.
8. Bullet collision masks (`bullet.tscn`) and the enemy hit interface were not read: owned by agent 4, referenced here as expectation.
9. Footstep triggers: whether the glb carries markers or the sound is timed by tscn tracks (`step_random_pitch.tres`); verify with agent 1; fallback = stride distance.

## 6. Ordered task list (S <= 0.5 day, M ~ 1 day, L = 2-3 days)
1. (S) Copy Shooter `Physics/` and `Animation/` into the new project; rename namespace; compile with stub content. Add collision layer mask + owner exclusion to `CollisionWorld`.
2. (S) `InputActions` map (keyboard/mouse/gamepad, edge/strength, deadzones 0.2), mouse capture helper.
3. (M) `PlayerCamera`: yaw/pitch, sensitivities, limits, spring arm ray, aim tween, shoulder offset, trauma shake; test camera with a free-standing debug box scene.
4. (M) `PlayerController` capsule motion: gravity 9.8, jump 5.0, airborne timers, move-and-slide, floor snap, respawn at y -40; temporarily drive horizontal velocity from a fixed speed (e.g. 4 m/s) until root motion is available. Fixed-step + interpolation.
5. (M) Load player `SkinnedModelContent` (agent 1) and play clips by name; draw with agent 2's skinned effect: verify bones/palette.
6. (L) `PlayerAnimator`: walk 1-D blend, strafe 2-D blend, state cross-fade 0.2, TimeScale 1.2/1.3, aim additive masked layer, and **root-motion extraction**; replace the fixed speed in 4.
7. (M) Facing logic (walk slerp, aim slerp to camera yaw) and verify front axis.
8. (S) Muzzle attachment (`hand.R` * gun bone local * ShootFrom) and debug marker.
9. (M) Shooting: aim toggle/hold logic, crosshair ray, 0.4 s cooldown, `ProjectileManager` (20 m/s, 5 s, sphere sweep), events -> particles/sound/shake. Enemy hit hookup with agent 4.
10. (S) Fall fade (alpha formula) and respawn at a level player spawn marker.
11. (S) Load level colliders into `CollisionWorld` from `level.scene` (with agent 1), including layers and the 4 player spawns.
12. (M) Self-test harness (as in Shooter: headless self-tests with scripted input: walk forward N seconds -> distance; jump apex; stairs; camera clipping) and compare to Godot where reference numbers exist.
13. (S) Polish: tune `RootMotionScale`, step offset, camera feel; footstep events.
Vertical-slice cut: tasks 1-11 (3-8 are core); eyes blend, `hardland` oneshot, `running_gun`, gamepad rumble, FOV settings and rebinding UI are out.

## 7. Lessons for the porting skill (Godot -> MonoGame)
- `CharacterBody3D.move_and_slide` = kinematic collide-and-slide: write one capsule motor (slope limit 45 deg, 6 slides, snap 0.1). Defaults that matter: `floor_max_angle 45`, `floor_snap_length 0.1`, `floor_stop_on_slope true`, `max_slides 6`, `safe_margin 0.001`; gravity default 9.8 m/s^2 from `ProjectSettings`, absent in `project.godot` means default.
- Godot demo logic often hides constants inside `.tscn` (spring length, FireCooldown wait_time, animation tweens, shape sizes, default shapes e.g. capsule r 0.5/h 2): always grep the `.tscn`, not only the script. Also read property defaults when a value is missing from the `.tscn`.
- `AnimationTree`: BlendTree nodes map to a small hand-coded C# graph: `Animation` (clip), `BlendSpace1D/2D` (barycentric triangles), `Transition` (state + xfade, `reset`), `TimeScale`, `Add2/Add3/Blend2` (+ bone filter = masked additive/override layer), `OneShot`; read `parameters/...` in the `.tscn` and in the script for the contract. Check if script ever triggers a node (here the `land` OneShot never fires).
- Root motion: Godot `root_motion_track` removes the root bone movement from the pose and feeds it to the script; the demo derives velocity from it. Keep the root bone through joint pruning and clip resampling and port delta extraction (with loop wrap), including scale.
- `Basis.looking_at` points **-Z**; glTF/Godot models front +Z: check facing by a debug arrow rather than reasoning.
- `Input.get_action_strength` -> own `InputActions`; gamepad stick Y has opposite sign in XNA; Godot trigger "axis" 4/5 = LT/RT; `MOUSE_MODE_CAPTURED` -> recentre mouse + hide cursor.
- `MultiplayerSynchronizer/rpc/is_server`: single player = always server; delete replication and keep the logic of the server branch only.
- `SpringArm3D` = ray cast from pivot to desired camera point, camera at `hit - margin`; its local basis (here 180 deg yaw + 20 deg pitch) defines the default camera offset; compute in matrices, not in Euler.
- `AnimationPlayer` tweening camera nodes (`position` track with transition curve) = tiny tween struct with Godot ease function; the curve value is the `transitions` array.
- `physics_interpolation=true` in project settings means fixed-step simulation with render interpolation; port to avoid camera jitter.
- Search the sibling port (Shooter) first: character motor, collision world, skinned animation player and animator controller were already solved in C# once; copying beats re-deriving.
