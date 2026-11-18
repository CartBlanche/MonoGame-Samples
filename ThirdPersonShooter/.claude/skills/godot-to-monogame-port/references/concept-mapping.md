# Godot concept -> MonoGame

Marked **(v)** = used and verified on the TPS demo; otherwise (planned, unverified).

| Godot | MonoGame equivalent |
|---|---|
| Node tree / `PackedScene` **(v)** | Baked scene JSON with composed world matrices + prefab sections; plain C# classes (no ECS at demo scale) |
| GDScript `class_name`, `@export` **(v)** | C# class; `@export` default is overridden by `.tscn` values; read both; cite file:line per constant |
| `_process` / `_physics_process` **(v)** | `Update(dt)` / fixed 60 Hz step accumulator |
| Signals | C# events; connections in `.tscn` `[connection]` and `.connect()` calls |
| Groups, Autoload | grep first (often unused); autoload -> static service listed in `project.godot [autoload]` |
| `await create_timer(x)` **(v)** | timer field in the entity updated each tick |
| `@rpc`, `MultiplayerSynchronizer`, `is_server` **(v)** | delete; single player = always server. Multiplayer/LAN itself is unproven |
| `Area3D` + `body_entered` **(v)** | explicit trigger volume with enter/exit events (copy the exit quirk) |
| `CharacterBody3D.move_and_slide` **(v)** | capsule motor (see `physics.md`) |
| `RigidBody3D`, `StaticBody3D` **(v)** | shared CollisionWorld (reuse `Shooter/Core/Physics`); static = colliders baked from the tscn |
| Collision layer/mask ints **(v)** | flags enum from `[layer_names]`; copy odd masks as-is |
| `RayCast3D`, `SpringArm3D` **(v)** | world ray cast; spring arm: ray pivot -> desired camera point, camera at `hit - margin` |
| `AnimationPlayer` method/audio tracks **(v)** | event list per clip (time, method/clip); gameplay logic hides here |
| `AnimationTree` **(v)** | small hand-coded graph: states + xfade, BlendSpace2D (barycentric), TimeScale, additive Add2, Blend2 with bone filter, OneShot (`animation.md`) |
| Root motion (`root_motion_track`) **(v)** | keep the root track; delta through the parent-chain scale; loop wrap |
| `NavigationAgent3D` | none in the TPS demo (audit said no navmesh); if present: grid/navmesh A* |
| `AudioStreamPlayer3D` **(v)** | own attenuation + pan, then `SoundEffectInstance` (OpenAL does not pan stereo: `audio.md`) |
| Audio buses **(v)** | volume per bus (-10 dB master); reverb/EQ dropped |
| `Input.get_action_strength` **(v)** | own `InputMap` -> `PlayerInput` struct |
| `MOUSE_MODE_CAPTURED` | recentre with `Mouse.SetPosition` + hide cursor (not run interactively) |
| `Control`/anchors, `canvas_items` stretch **(v)** | hand-written UI kit with virtual-resolution scaling (`ui.md`) |
| `GPUParticles3D` **(v)** | additive glow billboards / CPU particles; simplified |
| `Environment` (glow, tonemap, fog, ambient) **(v)** | HDR target + post passes, partial match (`rendering.md`) |
| VoxelGI / LightmapGI / probes **(v)** | not extractable: fake key light + ambient + fog |
| `.gdshader` | HLSL/MGFX (unverified); keep uniform names |
