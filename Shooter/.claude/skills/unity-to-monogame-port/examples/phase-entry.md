# Example PORTING-PLAN entry (condensed from Shooter phase 4c-3c, Turret behaviour)

Use this shape for every phase. Notes in [brackets] explain why each part exists.

## Phase 4c-3c: Turret behaviour

- **Code:** `Core/Gameplay/Enemies/EnemyTurret.cs` (EnemyTurret.cs ported in Unity space, converted at the edges), `Enemy` (resolves child nodes through baked local matrices), `EnemyRig` (late-update hook after the animator, before the palette), `CollisionWorld` dynamic boxes, SceneBake (`enemies[].hierarchy`), `EnemyTurretSelfTest`. [Files, so the next agent can find them.]
- **Data (scene = prefab):** DetectionRange 30, AttackRange 35, KnownTargetTimeout 4, AimRotationSharpness 5, LookAtRotationSharpness 2.5, DetectionFireDelay 1.1, weapon DelayBetweenShots 0.06. Transform references (TurretAimPoint, TurretPivot) read from the raw prefab YAML. [Where each number came from.]
- **Behaviour copied from the script:** Idle until `onDetectedTarget`; in Attack the pivot's desired rotation is LookRotation(target dir) x bind-pose offset, slerped at 2.5/s, 5/s after DetectionFireDelay; blend back to the animation over 1 s after loss. [Prose of the script's logic, not of your code.]
- **Findings:** the baker's flat hierarchy put the head hitbox 0.88 m above the root; the skeleton puts it 1.84 m. Fixed by following the bone. The Turret's NavMeshAgent never gets a destination, so it does not move. [Surprises worth keeping.]
- **Verified (`--test-physics`, 12 new, 92 total):** detection at 29.9 m yes / 30.1 m no; first shot 1.1167 s after detection; 43 of 49 shots hit a standing capsule for exactly 3; target forgotten 4.0 s after last seen; head hitbox follows its bone on the real level. `--enemy-sim 3` on pm_17 shows the head turned to the camera. [Each claim has a number or a named shot.]
- **Not verified:** no Unity capture of the Turret in action exists (swivel speed, burst timing, animations not compared); bind pose assumed for the muzzle-to-pivot offset because the Animator is assumed not to have run when `Start` executes. [The honest list. Never skip this.]
- **Not done (by design):** hit flash, death VFX, worldspace health bars, player Health (planned for 3d). [Scope boundary.]
