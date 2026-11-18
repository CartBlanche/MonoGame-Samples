# 04 - Enemies, AI and gameplay (Godot 4.7 TPS demo -> MonoGame DesktopGL)

Owner: Agent 4. Sources read: `enemies/red_robot/red_robot.gd` + `.tscn` (blend tree, shoot animation, collision), `player/bullet/bullet.gd/.tscn`, `door/door.gd/.tscn`, `level/level.gd/.tscn`, `main/main.gd`, `parts/part.gd`, `blast.gd`, `part_disappear.gd`, `flying_forklift.gd`, `project.godot`. Player movement/camera/animation is plan 03; rendering/particles plan 02; audio/UI/packaging plan 05; asset baking plan 01.

## 0. Headline findings (these simplify the port a lot)

1. **There is NO navigation in this demo.** `level.tscn` has zero `NavigationRegion3D`/`NavigationAgent3D`/navmesh nodes (grep count 0). Robots do not path-find. They stand idle until the player enters a 20 m sphere, then **turn in place and walk straight at the player using animation root motion**, with gravity and `move_and_slide`. So the "pathfinding" task is: no navmesh, no waypoints. Do NOT build Recast. (Risk: robots get stuck on props; the original has the same behaviour.)
2. **The player has no health and cannot die.** The robot's laser only fires a hitscan ray and, if it hits the player, adds camera shake trauma 13.0 after 0.1 s. `player.hit()` = camera shake 0.75. There is no score, no win/lose, no pickups, no checkpoints, no doors that lock. The "game" is: shoot robots (5 hits each), they respawn 15 s after exploding.
3. **Multiplayer is everywhere in the code** (`@rpc`, `MultiplayerSynchronizer`, `is_server()`, `OfflineMultiplayerPeer`). Vertical slice = single player, treat as "always server". All RPC/synchronizer/spawner nodes are dropped.
4. **Robot movement is animation-driven.** Horizontal velocity comes from `AnimationTree.get_root_motion_position()` (walk/turn clips). This needs root-motion extraction from the `walk`, `turn_left`, `turn_right` clips of `red_robot.glb` (an interface request to plan 01/03, see section 6).
5. Level gameplay scripts are tiny: `level.gd` (spawn robots at 4 markers, players at 4 markers, quit on Esc), `door.gd` (open once when a player enters an Area3D), `main.gd` (scene switcher), `flying_forklift.gd` (random visible model + shadow flag). Everything else is data.

## 1. GDScript behaviour table (exact constants)

### 1.1 Red robot (`red_robot.gd`, CharacterBody3D, layer 3 / mask 3)

| Behaviour | GDScript | Constant / formula | MonoGame equivalent |
|---|---|---|---|
| States | `enum State {IDLE=0, APPROACH=1, AIM=2, SHOOTING=3}` | | `enum RobotState` |
| Health | `health: int = 5`; `hit()` does `health -= 1`; `== 0` -> dead | 5 hits, 1 per bullet | `int Health = 5; TakeHit()` |
| Detection | child `PlayerDetectionArea` Area3D, layer 2/mask 2, `SphereShape3D` radius **20.0**, centred on robot origin | enter -> `player = body; state = APPROACH`; exit -> `player = null; state = IDLE` | Plain distance test `dist(robot.pos, player.pos) <= 20` each tick (hysteresis not needed; Godot has none). Only the player body triggers it (also a node named `Target`, unused in this project: grep finds none) |
| Aim tolerance | `PLAYER_AIM_TOLERANCE_DEGREES = deg_to_rad(15.0)` (name says degrees, value is radians, 0.2618) | | `const float AimTolerance = MathHelper.ToRadians(15)` |
| Angle to player | `to_local = target_position * global_transform` (vector*transform = inverse-transform of the point, i.e. world->local for an orthonormal basis); `angle = atan2(to_local.x, to_local.z)`; front of robot is **+Z** | angle>+tol -> `turn_left`; < -tol -> `turn_right`; target==0 -> `idle`; else `walk` | `Vector3.Transform(target, Matrix.Invert(world))`, same `Math.Atan2(x, z)`. Keep the sign convention exactly (positive x local = turn_left clip) |
| Shoot wait | `SHOOT_WAIT = 6.0`; counts down only while angle within +-tol in APPROACH | reset to 6.0 in `resume_approach()` and after a failed sight check | `float shootCountdown` |
| Sight check (before AIM) | when `shoot_countdown < 0`: ray from `ray_from` (RayFrom BoneAttachment origin = cannon) to `player.origin + UP*1`, exclude self, mask 0xFFFFFFFF; if first hit collider == player -> AIM (`aim_countdown = AIM_TIME`, `aim_preparing = 0`), else `shoot_countdown = 6.0` | | Segment cast vs level colliders + player capsule (section 3.2) |
| Aim time | `AIM_TIME = 1.0` in state AIM; `aim_countdown -= delta`; when `< 0`: second sight ray (same endpoints); if player hit -> SHOOTING, `shoot_countdown = 6.0`, `play_shoot()`; else `resume_approach()` | | |
| Aim blend in | `AIM_PREPARE_TIME = 0.5`; `aim_preparing` counts DOWN 0.5->0 in APPROACH (`aiming/blend_amount = clamp(aim_preparing/0.5, 0, 1)`), counts UP 0->0.5 in AIM/SHOOTING | blend 1 = aim blend-space active (turret follows target), 0 = locomotion only | Blend weight on turret bone subset (section 2.3) |
| Cannon aim | `to_cannon_local = (target + UP) * ray_mesh.global_transform`; `h = deg(atan2(x, -z))`, `v = deg(atan2(y, -z))`; `blend_pos.x += BLEND_AIM_SPEED*delta*(-h)`; `blend_pos.y += BLEND_AIM_SPEED*delta*v`; each clamped to [-1, 1]; `BLEND_AIM_SPEED = 0.05` (note: degrees * 0.05 * delta => slow, integrating servo) | | 2D blend space, see 2.3 |
| `aim` BlendSpace2D points | (0,0), (-1,0), (1,0), (0,1), (0,-0.9) -> 5 animations (centre, left, right, up, down turret poses) | initial param (0.0048, 0.1266) | Same: bilinear/triangulated blend of 5 poses on turret bones |
| Hit reaction | `hit()`: pick `hit1..3` OneShot randomly (`randi()%3+1`), `request = 1`, play hit sound, `health -= 1` | OneShot fadeout 0.5 s each; `hit1->Animation5, hit2->Animation6, hit3->Animation7` | Fire a one-shot clip layered over everything, fade 0.5 s |
| Locomotion transition | `Transition` node: idle/turn_left/turn_right/walk, `xfade_time = 0.5`, input_N reset = true | | Crossfade 0.5 s between 4 looping clips |
| Shoot sequence | `ShootAnimation` AnimationPlayer clip `shoot`, length **4.0** s: LaserEmber emitting at 2.25; ray colour1 red->(1,0.79,0.79)->red at 1.65/2.1/2.45; **method calls `shoot_check` at 2.25 s and `resume_approach` at 3.0 s**; ShootLight visible/energy tracks | | Timeline `ShootSequence` (list of timed events) |
| Fire (hitscan) | `shoot_check()` sets `test_shoot = true`; next physics tick `shoot()`: ray from RayFrom origin along **RayFrom basis.y** (cannon bone is rotated 90 deg), length 1000, excludes self; clip laser mesh to hit distance; position LaserEmber at midpoint; spawn `impact_effect` at hit point; if collider == player: wait 0.1 s then `add_camera_shake_trauma(13.0)` | no damage to player | Same, `Raycast(origin, dir, 1000)` -> `LaserFiredEvent` |
| Laser clip while aiming | `laser_raycast` (RayCast3D, mask 3, 1000 long, disabled by `enabled=false` but queried via `is_colliding()`) -> `_clip_ray(dist)` writes shader param `clip = length + mesh_offset` | | Per-frame cast, push `Clip` into beam material (plan 02) |
| Movement | root motion: `orientation *= Transform3D(rootRot, rootPos)`; `h_velocity = orientation.origin / delta`; `velocity.xz = h_velocity.xz`; `velocity += gravity*delta`; `move_and_slide()` (up = +Y); then origin zeroed, basis orthonormalised and written to `global_transform.basis` | gravity = project default 9.8 (check `project.godot` physics/3d/default_gravity; Godot default 9.8) | Kinematic capsule/sphere mover (section 3.3) driven by clip root motion |
| No player | `target_position = 0`, animate idle, apply gravity only | | |
| Collision body | `SphereShape3D` radius **1.11815** at local (0, 1.11655, -0.0213); layer 3, mask 3 (Level + Player) | | Sphere for mover and for bullet hit test |
| Death | at `health == 0`: `dead = true`; AnimationTree off; hide model; show `Death` node; disable collision; DetachSpark1/2 emit; `explode()` on PartShield1, PartShield2, PartHead; explosion sound; emit `exploded`; server frees node after **10.0 s** | | `Robot.Die()` -> `RobotDied` event, debris rigid bodies (3.5) |
| Respawn | `level.gd`: `exploded` -> wait **15.0 s** -> `spawn_robot(spawn_point)` | spawn transform = marker transform | `SpawnerService` timer queue |
| Spawn points | 4 robot markers: M1 (71.5907,-6.05686,46.2736) rotY~-32 deg; M2 (53.2126,-6.05686,15.9321) rotY~70; M3 (-2.96096,-11.6923,20.2343) rotY~99; M4 (-9.15526,-11.6923,-16.9238) rotY~99 | from `level.tscn` | Agent 1's `level.scene` markers (names `RobotSpawnpoints/Marker3DN`) |
| Player spawns | 4 markers near (64.8,-1.08,74..80.8), rotY~-125 deg; `level.gd` shuffles and takes first for the local player | | pick `Marker3D1` or random |

### 1.2 Debris (`part.gd`, RigidBody3D) and effects

| Item | Value |
|---|---|
| `lifetime = 3.0`, `lifetime_random = 3.0`, `disappearing_time = 0.5` | after `explode()`: wait `3.0 + 3.0*randf()` s, then `fade_value = (counter/0.5)^2` into the `next_pass` `emission_cutout` shader param; at `counter >= 0.5-0.2 = 0.3` s spawn puff (`part_disappear`) and free after 0.2 s |
| `explode()` | unfreeze, enable `Col1/Col2`, `linear_velocity = 3.0 * UP`, `angular_velocity = (normalized(rand3) * 2 - ONE) * 10` |
| Masses | PartShield1/2 **2000**, PartHead **6600**; layer 3 / mask 3; boxes ~0.86x1.03x0.095 (shields), sphere r 1.0 (head); PhysicsMaterial friction/bounce on sub-resources 56/61/64 (bounce 0.6 seen at line 427, friction 0.03: verify per body) |
| `blast.gd` (impact effect, CPUParticles3D) | `MiniBlasts.emitting` at t=0, main emit at t=0.2 s, `queue_free` after `lifetime*2` |
| `part_disappear.gd` | `LightRays` billboard-looks at camera each frame, frees after `AnimationPlayer.animation_finished` |

### 1.3 Player bullet (`bullet.gd`, CharacterBody3D) and firing (from `player.gd`)

| Item | Value | MonoGame |
|---|---|---|
| Speed | `BULLET_VELOCITY = 20.0` m/s along **-basis.z** | `Projectile` with swept sphere test per tick |
| Lifetime | `time_alive = 5.0`; on timeout explode (no damage) | |
| Collision | `move_and_collide(displacement)`; collision shape sphere r 0.170413; ignores shooter (`add_collision_exception_with(player)`); on hit: if `collider.has_method("hit")` -> `hit()` (robot takes 1 damage), disable shape, play `explode` animation, `hit = true` | Swept sphere vs static colliders + robot sphere; call `IDamageable.TakeHit()` |
| Explosion anim | length 1.6 s; OmniLight energy 1.5 -> 3.0 (0.25 s) -> 0.0 (0.7 s); `omni_range` 1.5; method track at 1.5 s calls `destroy()` (queue_free); audio at 0.01 s; Blast/BlastParticle, LightBlast, BlastSparks, Smoke emit at 0 | Handoff to plan 02/05 as `ImpactEffect` |
| Muzzle | player `FireCooldown.wait_time = 0.4`; fire only while aiming && shoot pressed && cooldown idle; origin `ShootFrom` marker, dir `(shoot_target - origin).normalized()`, bullet oriented with `look_at` | plan 03 owns input/aim point; plan 04 owns `ProjectileSystem.Spawn(origin, dir, owner)` |
| Player feedback | `shoot()`: particle restart, sound, camera trauma **0.35**; `hit()`: trauma 0.75 (never called by bullets); robot laser: trauma **13.0** | trauma values go into plan 03 camera shake |
| Shadow detail | explosion enables omni shadows only if `shadow_mapping` setting | drop (no shadow maps; plan 02 uses blob shadows) |

### 1.4 Door (`door.gd`, Area3D)

| Item | Value |
|---|---|
| Trigger | Area3D with `BoxShape3D` size (8.85, 6.21, 11.07); `body_entered`: if `not open and body is Player` -> play `doorsimple_opening` on the model's AnimationPlayer, `open = true` (one-way, never closes) |
| Audio | `sound` AudioStreamPlayer3D, `unit_size = 7.0`, file `open_close.wav` (plan 05) |
| MonoGame | `TriggerVolume` (OBB) + `Door` entity; `DoorOpened` event; plays skinned clip `doorsimple_opening` (needs door glb clip + whitelist). Door collision: the model's collider is part of the door scene; in the original it is a static body that the clip does NOT disable (verify, open question Q3) |

### 1.5 Level and main

| Item | Value | MonoGame |
|---|---|---|
| `main.gd` | sets display mode from settings, `randomize()`, loads `menu.tscn`, swaps child scenes; scenes expose `quit` / `replace_main_scene` signals; `go_to_main_menu` closes multiplayer | `GameStateManager` (Menu / Playing / Paused); `quit` -> back to menu |
| `level.gd` | choose GI mode (VoxelGI/SDFGI/Lightmap) from settings; server spawns 4 robots; spawns players; Esc (`quit` action) -> mouse visible + `quit.emit()` | Spawn from markers; GI mode irrelevant (plan 02) |
| Forklift | random one of 3 child models visible (`floor(randf()*count)`), spot light shadows per setting; AnimationPlayer on parent moves them | `LevelScene` loader picks a model index with seeded RNG; animation path = plan 02/01 |
| Sound areas | `SoundOutside` (CollisionPolygon3D) and `SoundReactorRoom` (shape), `collision_mask = 0`, only `audio_bus_override` -> bus Outside/Reactor | Plan 05: zone volumes polled against listener (listener-in-volume test). Plan 04 supplies the `TriggerVolume` class |
| `debug.gd` | FPS label toggled by `toggle_debug` | Plan 05 UI |
| Collision layers | 1 Level, 2 Player, 3 Player Only. Robot layer 3 (=L1+L2), mask 3; player layer 6 (L2+L3), mask 7; detection area layer 2/mask 2 | `CollisionMask` flags: Level, Player, PlayerOnly. Robot blocks and is blocked by player; robots do not collide with "Player Only" (invisible walls for the player) |

## 2. AI and "pathfinding" design

### 2.1 Decision: no navmesh

The original has none. A navmesh would change behaviour (robots would navigate around corners; the originals bump along walls). For like-for-like: direct steer-to-player with root motion plus collision sliding. If robots visibly stall in playtests, optional upgrade (post slice): a coarse 2D grid/waypoint graph baked from agent 1's collider boxes, used only when the sight ray to the player fails for > N seconds. Justification: the demo's design intent is a "move toward player in 20 m bubble" showcase; adding path-finding is scope creep and a long-tail risk.

### 2.2 State machine (per robot, fixed 60 Hz tick, delta 1/60)

```
IDLE      --player within 20 m-->  APPROACH (aim_preparing=0.5, shoot_countdown=6)
APPROACH  --player beyond 20 m -->  IDLE
APPROACH  : turn/walk by angle; aim_preparing -> 0; if |angle|<15deg: shoot_countdown -= dt;
            if <0: sight ray ok -> AIM (aim_countdown=1, aim_preparing=0) else shoot_countdown=6
AIM       : aim_preparing -> 0.5; aim_countdown -= dt; if <0: sight ray ok -> SHOOTING
            (shoot_countdown=6, start ShootSequence) else resume_approach()
SHOOTING  : plays 4 s clip; t=2.25 -> Fire() (hitscan); t=3.0 -> resume_approach() => APPROACH
any       : Health==0 -> DEAD (no revive; respawn is a new robot)
```

Note the original quirk: in IDLE/AIM/SHOOTING the exit event sets IDLE even mid-shoot; faithfully port (exit sets `state = IDLE` and `player = null`, while a running shoot clip still calls `resume_approach` which re-enters APPROACH with `player == null` -> robot walks nowhere because target is zero; harmless).

### 2.3 Animation (shared with plan 03's runtime)

The robot AnimationTree = Transition(idle, turn_left, turn_right, walk; 0.5 s crossfade) -> Blend2 "aiming" filtered to bones `Cannon-TrackTurret`, `CannonAnimRecoil`, `Cannon_TrackElevation` (input 1 = BlendSpace2D aim) -> 3 chained OneShots hit1/2/3 (fadeout 0.5 s) -> output. Plan 03 owns the generic `AnimationController` (clip sampling, crossfade, per-bone-subset blend, 1D/2D blend). Plan 04 needs these from it: (a) `Transition(name, 0.5)`, (b) bone-mask layer with weight, (c) `BlendSpace2D` of 5 poses, (d) one-shot layer with fade-out, (e) **root-motion delta per tick** (translation + yaw) from the locomotion clip: needs the root bone name (red_robot `Armature` root track) from plan 01's processor. If (e) is too hard in the slice: fallback `walkSpeed` m/s and `turnRate` rad/s constants measured from the clip's root-bone first/last keyframe (record in a data file). Clips from `red_robot.glb`: idle, turn_left, turn_right, walk, 5 aim poses, 3 hit, shoot (whitelist to plan 01; names in the AnimationLibrary of `red_robot.tscn`; confirm exact names in task 1).

### 2.4 Line of sight and aggro

Both checks use one `Physics.Raycast(from, to, ignore: self)` returning the first hit among: static level colliders (boxes/hulls from `level.scene`), the player capsule, other robots' spheres. "Sees player" = first hit is the player. Because the original uses the full mask 0xFFFFFFFF, other robots and debris also block. Aggro radius = 20 m, flat distance not used (3D distance, centre to centre; Godot Area3D overlaps shapes, player capsule radius adds a bit: use `dist <= 20 + playerRadius`).

## 3. Physics and collision plan (MonoGame has none built in)

3.1 **Library choice.** Options: BepuPhysics v2 (supports kinematic character, ray casts, convex hulls, RigidBody debris; heavier, needs .NET 6+ unsafe code) or a hand-written collision module (box/sphere/capsule/hull static world + swept sphere + ray). Recommendation: **hand-written for the slice** (static boxes + sphere + capsule, BVH or uniform grid over 447+ boxes, ray, swept-sphere "collide-and-slide"), because every gameplay body in this demo is kinematic except 3 debris RigidBodies per robot, which can be a tiny custom integrator (gravity, one sphere-vs-world bounce, damped angular spin; they are visual only). Re-evaluate Bepu only if plan 03's player capsule (with `SeparationRayShape3D`) proves hard. **Shared decision needed** (agent 3 will need the same module): see Q1 and Proposed changes.

3.2 **Queries required**: `Raycast(from, to, mask, ignore) -> {point, normal, collider}`; `SweepSphere(from, to, r, mask, ignore)`; `MoveAndSlide(body, velocity, dt, up)`; `OverlapSphere` (detection, optional). Colliders from `level.scene` (plan 01: box/sphere/capsule/hull + layer mask).

3.3 **Robot mover**: sphere r 1.11815 at y offset 1.11655 above origin, gravity 9.8, floor snapping = `MoveAndSlide` with up = +Y, `floor_max_angle` default 45 deg, 4 slide iterations. Spawn y is below 0 (-6.06 / -11.69): world coordinates are level-local, no reorigin.

3.4 **Bullet vs robot**: sphere (r 0.1704) swept against robot sphere (r 1.118) - radii sum 1.288. Bullet ignores its shooter. Bullets pass over dead robots (collision disabled).

3.5 **Debris**: 3 bodies, mass irrelevant (no body-vs-body response needed); initial `linear_velocity (0,3,0)`, `angular_velocity` per formula; gravity; collide with level via sphere/box approximations; bounce 0.6 / friction 0.03 (verify). Free after fade (>= 3 s + rand 0..3 s + 0.5 s).

## 4. Entity architecture

**Decision: plain C# classes with a small interface set and a per-frame `World` tick list. No ECS, no generic component bag.** Justification: only ~6 entity kinds (Player, RedRobot, Bullet, Door, DebrisPart, Effect), zero data-driven composition needs, and RacingGame already uses simple class-based game objects. An ECS would add ceremony without payoff; a Godot-style scene tree is unneeded because the baked `level.scene` already carries world transforms.

```
interface IEntity { bool Alive; void Update(GameTime, World); void Draw(RenderContext); }
interface IDamageable { void TakeHit(int amount, Vector3 point); bool IsDead { get; } }
abstract class Entity : IEntity { Matrix World; Collider Body; ... }
class World   { List<Entity>; Add/Remove deferred; Physics; Events; Services; fixed-step 1/60 accumulator }
class Player : Entity (plan 03)   class RedRobot : Entity, IDamageable
class Projectile : Entity          class Door : Entity           class DebrisPart : Entity
class TriggerVolume                class RobotSpawner (timer queue)
```

- Fixed 60 Hz update for gameplay (`_physics_process` parity: robot logic and bullets), render interpolation optional (not needed at 60 Hz VSync).
- Deferred add/remove (`queue_free` equivalent) at end of tick; timers via a `TimerQueue` (replaces `await get_tree().create_timer()`).
- Per-entity `Random` seeded from a world seed (original `randomize()`; for tests use fixed seed).
- Draw: entities submit `ModelInstance` + pose + material overrides to plan 02's renderer; they do not issue GPU calls themselves.

## 5. Godot -> C# mapping

| Godot | C# |
|---|---|
| `signal exploded()` | `event Action<RedRobot> Exploded` (World/Spawner subscribes) |
| `signal quit`, `replace_main_scene` | `event Action QuitRequested`; `GameStateManager.ChangeState` |
| `Area3D.body_entered/exited` | `TriggerVolume` with `Entered/Exited` events, evaluated per tick against entity bodies filtered by mask |
| Groups | none are used in this project (grep: no `add_to_group`); `body is Player` -> `is Player` type test; `has_method("hit")` -> `is IDamageable` |
| Autoload `Settings` (only autoload) | `GameSettings` singleton/service: display mode, rendering (shadow_mapping, gi_*) read from `config.cfg`-equivalent JSON; plan 05 owns persistence |
| `multiplayer.is_server()`, `@rpc`, Synchronizers, Spawner | removed; code paths always "server". Keep `PlayerId` field = 1 to ease an optional LAN mode later (RacingGame has LAN; not in slice) |
| `get_tree().create_timer(t).timeout` | `World.Timers.After(t, action)` |
| `queue_free` | `World.Remove(entity)` (deferred) |
| `PackedScene.instantiate` | entity factories keyed by name, params from `level.scene` prefab sections |
| `move_and_slide`, `move_and_collide`, `intersect_ray` | collision module (section 3) |
| `Input.set_mouse_mode` | plan 03/05 input layer |
| `preload("res://...")` | `ContentManager.Load` from AssetMap content names |
| Service locator | `static class Services { T Get<T>() }` holding `World`, `Physics`, `Audio`, `Particles`, `Settings`, `Content`, `Clock`; kept tiny; passed into constructors where reasonable (avoid global use in hot loops) |

## 6. Interfaces to other agents

- [agent4 expects from agent1] `level.scene` colliders with layer bits (Level=1, PlayerOnly=4), marker lists `RobotSpawnpoints`/`PlayerSpawnpoints` (4+4, transforms listed in 1.1), Area/trigger volumes (door box 8.85x6.21x11.07, sound areas), door prefab placements, red_robot sphere/box shapes and PlayerDetectionArea radius 20.
- [agent4 expects from agent1] red_robot clip list incl. `Armature` root-bone track for root motion and whitelist: idle, turn_left, turn_right, walk, aim poses (5), hit1-3 (Animation 5/6/7 in blend tree), shoot (4.0 s clip is in the tscn AnimationLibrary, not in the glb: it is an event timeline; port as C# data), door clip `doorsimple_opening`.
- [agent4 expects from agent3] `AnimationController` features in 2.3 (crossfade transition, bone-mask layer, 2D blend space, one-shot, root-motion deltas) and a skinned-pose output `Matrix[]` for plan 02; `Camera.AddTrauma(float)`; `Player` exposes `Position`, capsule (radius/height), `ShootOrigin`, `AimTarget` (world point), `Aiming`, `Shooting`.
- [agent4 produces for agent3] `Physics` module API (3.2), `IDamageable`, `World.Timers`, `ProjectileSystem.Spawn`. Camera shake amounts: shoot 0.35, hit 0.75, laser hit 13.0 (delivered 0.1 s after the shot).
- [agent4 produces for agent2] events: `LaserFired(origin, dir, length)`, `ImpactAt(point, normal)`, `RobotDied(position)`, `ProjectileExploded(point)`, `BulletTrail`; laser `Clip` per frame; debris `FadeValue` 0..1 (emission cutout); ShootLight on/energy timeline; billboard LightRays face camera.
- [agent4 expects from agent2] particle emit API (`Emit(effectName, transform)`), laser beam draw, debris dissolve shader (`emission_cutout` next_pass), blob shadow raycast callback served by the Physics module.
- [agent4 expects from agent5] `Audio.Play3D(name, position)` and zone-volume polling; sounds: red_robot charge, shoot, explosion, walk (footsteps via animation events? not wired in gd: walk loop sound is on the robot, verify), hit; bullet_explode; door open_close.

## 7. Risks and open questions

1. **Physics module scope** (biggest): 447+ static boxes, kinematic robots and player, swept sphere, rays. Must be correct before AI is testable. Mitigation: unit tests (Raycast/slide on box scenes), visual collider debug overlay.
2. **Root motion** from glTF clips: robots walk by root motion; if the processor drops root tracks, movement breaks. Fallback constants (2.3).
3. **Blend tree fidelity**: 2D aim blend + masked layer + one-shots is the "hardest-looking" part of the enemy. Fallback: aim as procedural turret yaw/pitch on the two turret bones.
4. Layer semantics of the bullet (CharacterBody3D collision layers not read; need `bullet.tscn` header lines) - Q2.
5. Robots can wedge against props (same as original). Optional waypoint fallback post-slice.
6. `shoot_countdown` is initialised to 6.0 so the first shot occurs >= 6 s after first aggro and the first aim; plan playtests with `test_shoot` debug toggle (`F` key in debug build).

Open questions:
- Q1 [→lead/agent3] Shared physics module: custom (my proposal) vs BepuPhysics. Decide before task 3.
- Q2 Bullet collision layer/mask and whether bullets collide with `Player Only` (layer 3).
- Q3 Does the door's collider move/disable on open? Check `door.tscn` physics body inside the door model scene.
- Q4 Exact clip names + loop flags for red_robot (23 `Animation` resources, a few `length 0.001` are static pose clips for aim).
- Q5 Robot walk/turn loop audio: where is it triggered? (not in `red_robot.gd`; likely animation method/audio tracks).
- Q6 Gravity value in `project.godot` (assume 9.8).

## 8. Vertical-slice cut (one enemy type)

In: `World`/fixed tick, collision module (static boxes, sphere/capsule, raycast, move-and-slide), `RedRobot` full FSM, aggro 20 m, sight rays, aim blend (procedural fallback acceptable), laser hitscan + camera trauma, `Projectile` (speed 20, 5 s, 1 damage, sphere swept), 5-hit death, debris (3 parts, simplified bounce, dissolve), 4 robot spawns + 15 s respawn, impact effect hand-off, door trigger + opening clip, `GameStateManager` Menu<->Playing<->Quit(Esc).
Out (post slice): menu details (plan 05), sound-zone buses, forklift animation, LAN multiplayer, optional waypoint path fallback, settings UI.

## 9. Ordered task list (S <= 0.5 d, M ~1-2 d, L ~3+ d)

1. (S) Read remaining data: bullet.tscn layers, door model collider, red_robot clip/loop names, `project.godot` gravity; close Q2-Q6.
2. (S) Define `IEntity`, `IDamageable`, `World`, `TimerQueue`, `Services`, `GameStateManager` shell (Menu/Playing/Paused, Esc -> menu).
3. (L) Collision module: collider types from `level.scene`, uniform-grid broadphase, `Raycast`, `SweepSphere`, `MoveAndSlide`, layer masks; tests.
4. (S) `TriggerVolume` + `Door` entity (+ plays clip via plan 03's controller).
5. (M) `RedRobot` skeleton: state machine, detection, angle math, countdowns, shoot timeline data (4.0 s events), unit tests with fake clock for transitions (sections 2.2).
6. (M) Locomotion: root-motion delta source (or constants) + `MoveAndSlide`; walk/turn animation hook-up through plan 03's controller.
7. (M) Aim: procedural turret yaw/pitch first, then 2D blend space if available; laser raycast + `Clip` value to plan 02.
8. (M) Hit/death: `IDamageable`, hit one-shots, `Die()`, debris `DebrisPart` simple integrator + dissolve, events to plan 02/05, 10 s corpse removal, `RobotSpawner` 15 s respawn from markers.
9. (M) `Projectile` + `ProjectileSystem`; fire interface with plan 03 (cooldown 0.4 s lives in player); impact + explosion timeline (1.6 s, light envelope).
10. (S) Camera trauma hook (0.35 / 0.75 / 13.0, 0.1 s delay) and `ImpactAt` events to plan 02.
11. (S) Level loader glue: spawn markers -> robots/players, door placements, forklift random model.
12. (M) Playtest harness: debug overlay (colliders, rays, states, `test_shoot`), scripted self-test that drives a robot through APPROACH->AIM->SHOOTING and logs timings against section 1.1 numbers.
13. (S) Settings service stub (`Settings` autoload equivalent) reading plan 05's config.
14. (M, optional) Waypoint/grid fallback for stuck robots.

## 10. Lessons for the porting skill (godot-to-monogame-port)

- Audit first for the engine feature the plan assumes: grep for `Navigation*` before planning pathfinding. Here the demo has none; the brief's "navmesh" assumption was wrong. Check the same way for health/score/win-lose before designing them.
- Strip multiplayer idioms (`@rpc`, `Synchronizer`, `is_server`, `await create_timer`) and treat the project as always-server single-player; list them in the mapping table so nothing is half-ported.
- GDScript constants live in three places: the .gd file, `@export` defaults, and the .tscn (overrides, sub-resource radii, AnimationPlayer method tracks, timer `wait_time`). Method-call tracks (`shoot_check` at 2.25 s) are gameplay logic hidden in animation data: grep `"method":` in every `.tscn`.
- `vector * transform` in GDScript is the inverse transform (world->local), and `deg_to_rad` consts compared to `atan2` radians: record formulae verbatim, not intent.
- Root motion in the AnimationTree drives locomotion; the clip processor must keep the root track or provide a constant fallback.
- Collision layers are in `project.godot [layer_names]` and `.tscn` `collision_layer/mask` integer bitfields: decode them into a named flags enum on day one.
- Godot `Area3D` triggers -> explicit `TriggerVolume` with events; `body is Player` -> type test; groups unused here, but grep them anyway.
- Choose plain classes over ECS for demo-scale games (six entity kinds); keep a fixed-timestep world so `_physics_process` timing matches.
