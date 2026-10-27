# System recipes

Each recipe: what to copy from the Unity scripts, what to bake, how to test. Numbers are Shooter's, as examples. Status words: **verified** (headless check or capture), **assumed** (from the script, never compared to Unity).

## Character controller + world collision
- Unity `CharacterController` / PhysX are not available. Build: triangle-soup `CollisionWorld` (uniform grid; capsule overlap/cast, raycast with grid walk + Moller-Trumbore) and a `CharacterMotor` stand-in, then a line-by-line port of the controller's `Update`.
- Expand non-convex MeshColliders to world-space two-sided triangles from the same buffers and matrices the renderer uses. Boxes/spheres become triangles. Skip trigger boxes and the player's own capsule.
- Motor: sub-step moves, overlap recovery after each, step offset (lift, move, drop; only walkable-to-walkable), slope limit, skin width. Cost was 0.35-1.3 ms/frame.
- Take Inspector values from the scene JSON (speeds, gravity, jump, heights, radius, slope, step, skin, kill height). Camera height 0.8 x height.
- Tests: settle on floor, wall stop at 60/30/10 fps incl. zero-thickness wall, wall slide, speeds, step 0.2/0.3 climbed and 0.35 blocked, jump apex, ramps, ceiling, kill height, fall velocity, real-level spawn vs Unity Play-mode value, fuzz. **Verified.** Feel versus Unity side by side: *assumed*.
- Input: take bindings from `*.inputactions`; mouse look scale from the script (0.2 deg/pixel = 0.001 x RotationSpeed 200). Provide keys for machines without a captured mouse.

## Weapons and projectiles
- Ammo as a refilling resource, fire gate, manual / automatic / charge, recoil, bob, aim zoom, switch state machine all come from `WeaponController` / `PlayerWeaponsManager` with **scene values**.
- Viewmodel = baked pieces placed at camera + socket (bob + recoil + aim + switch), rendered with the weapon FOV on both cameras. Keep logic in camera-local Unity axes.
- Projectile: speed, camera trajectory correction, camera-to-muzzle wall check, hit cast from last root to tip. Reproduce prefab authoring quirks (tip 10 m ahead).
- Damage path: projectile -> collider's `Damageable` -> `Health`, owner as source. Multipliers, explosion flag, self damage factor.
- Tests: fire rate (frames per shot), ammo curve and refill delay, recoil peak/return, aim FOV, hit within 1 mm of the screen centre, wall closer than the muzzle, independent-ray comparison on the real level. **Verified.**
- Effects: muzzle flash parented to the muzzle, destroyed after the script's lifetime; impact VFX at point + normal x offset with `LookRotation(normal)`; sounds from AudioSource volume. Overheat gem (gradient of 1 - ammo ratio), steam rate, fuel cells (container local offset through its baked matrix). Gem **verified** within 16 levels; the rest *assumed*.

## HUD (Unity UGUI)
- Source: prefab instance under the manager in the scene JSON. Resolve RectTransforms in y-up canvas space, scale = screen width / canvas reference width.
- Draw into an off-screen layer, composite in the final pass in linear light (see `conventions.md`).
- Per widget copy the script's smoothing (lerp sharpness), colour-change rules, fade timings and the Update order. Crosshair, ammo bar, health, stance, compass (scissor clipped), objective message and toast, notification toasts, jetpack bar, damage flash, low-health vignette (`(1 - ratio / critical) * max`, pulsing `sin(t*8)/2+0.5`).
- Compass markers: signed angle to the player's heading; scale lerp by distance.
- **Verified** against Play-mode captures within 1-2 px for the main HUD; flash/vignette/markers are *assumed*.

## Enemy AI
- Port `EnemyController`, `DetectionModule`, `EnemyMobile`, `EnemyTurret`, `NavMeshAgent` stand-in. Read Transform-reference fields from the raw prefab YAML (AimPoint, DetectionSourcePoint, muzzle, pivot).
- Detection: range check on the player root + line of sight from the detection source to the player's aim point; known target timeout; being shot makes the shooter's root the target.
- Movement stand-in: straight-line steering, wall sliding, velocity/acceleration/angular speed from the agent values; no pathfinding. Keep spawn height.
- Turret: LookRotation to target times the bind-pose offset between muzzle and pivot; slerp sharpness changes after `DetectionFireDelay`; late-update override of the pivot bone after the animator and before the skin palette; blend back over 1 s after loss.
- Animator: parameters (MoveSpeed, Alerted, Attack/OnDamaged triggers, IsActive) and state machine from the controller JSON; additive layers.
- Death: unregister, death VFX at the script's spawn point for 5 s, loot drop, sounds. Hit flash: gradient over (time - LastTimeDamaged) / duration, gamma -> linear.
- Tests: values read, detection at 19.9 m yes / 20.1 m no, line of sight cases, state transitions with exact times, fire cadence, projectile aim error (0.03 deg), patrol visits nodes without sticking, head hitbox follows its bone. **Verified headlessly; no Unity capture of behaviour** (*assumed* timing/feel).

## Particles / VFX
- Parse ParticleSystem props into JSON (ParticleBake); runtime billboards (stretch, horizontal, mesh), curves, gradients, shape, sub-emitter-free for now, trails, scaling mode, weapon-layer flag, pivot. See `pitfalls.md`.
- Systems are instantiated from baked prefab JSON by name; placement from the scene hierarchy; one-shots removed after their lifetime.
- Ambient emitters (dust, lava) come from the scene's particle JSON at load.
- Always compare the burst's size, delay and timing against a capture of the same event; fidelity gaps found: flare brightness (bloom), action-line shape, shard chunkiness.

## Audio
- `SfxPlayer`: 2D one-shots, 3D one-shots with Unity-style rolloff, looping instances with play state (jetpack, cooling cells with volume from the ammo curve).
- Footsteps: one per `1 / frequency` metres while grounded (counter after the jump check); landing `Land` / `Land_Damage`. Jump sound on the jump event.
- Sounds were **never heard** in the Shooter port; say so.

## Pickups and loot
- Bob `(sin(t * f)/2 + 0.5) * amount`, spin about the root's own axis (sign flip), trigger fires once per entry, `CanPickup`, feedback = 2D sfx + VFX at the current position. Loot dropped at the enemy's root on death.
- Capsule-vs-box trigger approximated by sampling (*assumed*). Mesh pose: first pickup's baked mesh matrix minus position gives the relative; the second is reproduced to 1e-4 (a test).
- Pickup placement not in any scene (e.g. a jetpack) is a design choice: agree it with the user and expose it as a baker constant.

## Game flow and menus
- `GameFlowManager` + `ObjectiveManager` as pure logic with timelines (lose: fade over 3 s then screen; win: message 2 s, fade 4 s later, screen at 7 s). Input stops while `GameIsEnding`; fade is a black quad drawn over the HUD; master volume ramps.
- Menus from the scene canvases: skybox background, vignette, title, buttons with Selectable tints, pop-ups. Mouse press/release on the same button; keyboard navigation first selects the default button. Pause menu: freeze updates (timeScale 0), free cursor, volume 0.5, sliders/toggles, framerate counter.
- Reload: rebuild director, weapons, pickups, flow (like reloading the scene); keep persistent toggles (invincibility).
- Tests: timelines, layout at 1280x720, click and keyboard behaviour. **Verified**; gamepad navigation not ported.

## Planning later mechanics
Before starting a big one (area damage, charge weapons), write a sub-step plan in `PORTING-PLAN.md` (assets and data, slots/pickups, mechanic, HUD, tests), estimate its size against earlier phases, and get a usage check from the user first.
