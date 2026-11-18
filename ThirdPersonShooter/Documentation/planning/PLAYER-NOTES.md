# Player controller notes (part A)

Status: `--test-physics` 10/10 pass; `--sim "<script>" --shot <png>` renders the end state (mono-09-walk.png, mono-10-walk-turn.png).

## What exists
- `Core/Physics/CollisionWorld.cs`, `CharacterMotor.cs`: adapted from `../Shooter` (same author, namespace changed; TryStep extended, see comment). `LevelColliders` builds two worlds from level.scene.json: LevelWorld (layer 1: camera/bullets/sight) and PlayerWorld (layers 1|4). Boxes are exact (12 triangles), the 12 cylinders are 24-sided circumscribed prisms. No sphere/capsule/hull colliders exist in the extract.
- `Core/Player/PlayerConstants.cs` (all Godot constants with file:line), `PlayerController` (fixed 60 Hz, `IPlayerView`), `Core/Game`-style camera `Core/Player/ThirdPersonCamera.cs` (pivot 1.6, arm 2.4 at 20 deg, ray vs LevelWorld, margin 0.01, no smoothing as in Godot).
- `Core/Input/InputMap, InputCapture, PlayerInput`; `Core/Verification/Simulation, PhysicsSelfTest`.
- Game: default = play mode; `--fly` (or `--cam`) = old free camera; `--sim` script items: `w|a|s|d|w+d:sec`, `look:yawDeg,pitchDeg` (yaw right +), `space[:sec]`, `wait:sec`.

## Seams for part B
- `PlayerController.DesiredHorizontalVelocity` (TEMP 4.5 m/s, `PlayerConstants.TempRunSpeed`): replace by root motion / dt (player.gd:131-163).
- `PlayerController.TempClip` and the clip switch in `ThirdPersonShooterGame.Update`: replace by the animation tree.
- Aim branch (face camera yaw, strafe), shooting, aim toggle, spring-arm `ArmOffset` shoulder tween, camera shake: not done. `ThirdPersonCamera.ArmOffset` exists for the tween.
- Events `OnJump`/`OnLand` fire; `FadeAlpha` computed, not drawn.

## Conventions / decisions
- View axis is XNA Forward (-Z); `Yaw` is RotationY of the view direction. Initial yaw = spawn forward (as the earlier shots). Pitch = Godot sign (positive = look down). Model world = RotY(pi) * heading * T, as before.
- Godot's SeparationRayShape step behaviour is approximated with StepOffset 0.4 (plan 03). Step-up pops forward up to one radius when the capsule rim is over the ledge.
- Floor handling: while grounded vertical motion is zeroed and a 0.1 m snap is used (avoids a 60 Hz bob).
- Jump apex measures 1.234 vs ideal 1.2755 (3.2% low: semi-implicit 60 Hz, same order as Godot).

## Open issues
- TEMP speed 4.5 m/s is a guess; real value = root-motion of running_nogun (plan 03 risk 1).
- Mouse capture on macOS uses Mouse.SetPosition recentre; not exercised in this session (no interactive window). Gamepad untested.
- Interactive play not exercised, only fixed-step headless + screenshots. Walking into stair_pillar / real stairs not tested on the level beyond the spawn corridor; 0.4 step offset unverified vs the level's tallest stair.
- Spawn rest height: capsule rests ~0.01 above collider top (skin width), so feet sit 0.01 above the probe ground.
