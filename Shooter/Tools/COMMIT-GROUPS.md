# Suggested commit groups (stage one, commit, then the next)

Only the final group makes the whole tree build, because the Desktop csproj references everything above it.
`Tools/UnityReference/Baseline/` is left out until the Play-mode baseline replaces it.

```
# A. Unity extraction tooling and reference screenshots
git add Tools/UnityExtractor Tools/UnityReference/*.png Tools/UnityReference/compare.py

# B. Static asset import
git add Tools/ContentGen Tools/Shooter.StaticPipeline Core/Content/Shooter.mgcb Core/Content/AssetMap.json \
        Core/Content/ASSET-CONVENTIONS.md Core/Content/Textures Core/Content/Audio Core/Content/Fonts Core/Content/Models
git add -u Core/Content/Content.mgcb   # stages the deletion of the old Content.mgcb

# C. Skinned characters and animation
git add Tools/Shooter.Pipeline Tools/Shooter.CharacterExport Core/Animation Core/Content/Characters Core/Content/Characters.mgcb

# D. Scene bake and loader
git add Tools/SceneBake Core/Scene Core/Content/Scenes

# E. Renderer (work in progress, unverified)
git add Core/Rendering Core/Content/Effects Core/Content/Effects.mgcb Core/Content/Particles Tools/ParticleBake

# F. Project wiring, docs, ledger
git add .config Core/Core.csproj Shooter.slnx README.md Platforms Core/ShooterGame.cs Tools/PORTING-PLAN.md Tools/AGENT-LEDGER.md Tools/COMMIT-GROUPS.md
```

## Phase 4a (player controller + collision), two groups: code first, docs second

```
# G. Code: collision world, character motor, player controller, input, self-test, loader + game wiring
git add Core/Physics Core/Gameplay Core/Scene/SceneLoader.cs Core/ShooterGame.cs

# H. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4b (weapons), two groups: code first, docs second

```
# I. Code: weapon manager / controller / projectiles, raycast, input, self-tests, baked scene + baker, loader + game wiring
git add Core/Gameplay Core/Physics Core/Scene Core/ShooterGame.cs Core/Content/Scenes Tools/SceneBake/Program.cs

# J. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-2 (in-game HUD), two groups: code first, docs second

```
# K. Code: HUD, crosshair / icon data, IsPointingAtEnemy, HUD composite in the final pass, game wiring, self-tests
git add Core/UI Core/Gameplay Core/Rendering Core/Content/Effects/FinalPost.fx Core/Scene/Diagnostics/BasicSceneRenderer.cs Core/ShooterGame.cs

# L. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-1 (weapon effects), two groups: code first, docs second

```
# M. Code: effects + audio, asset-name helpers, weapon / projectile data, baked scene (AudioSource) + baker, game wiring, self-tests
git add Core/Audio Core/Gameplay Core/Scene/SceneLoader.cs Core/ShooterGame.cs Core/Content/Scenes Tools/SceneBake/Program.cs

# N. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-1b (overheat gem, steam, fuel cells), two groups: code first, docs second

```
# O. Code: overheat visuals, piece offsets, rate multiplier, baked fuel cell names (+ baker), game wiring, self-tests
git add Core/Gameplay Core/Rendering/Particles Core/Scene Core/ShooterGame.cs Core/Content/Scenes Tools/SceneBake/Program.cs

# P. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3a (enemy health, damage, kills), two groups: code first, docs second

```
# Q. Code: combat + enemy classes, collision owner tags, loader hitboxes, projectile damage, HUD (counter, notifications, compass markers), game wiring, self-tests
git add Core/Gameplay Core/Physics Core/Scene Core/UI Core/ShooterGame.cs

# R. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3b (HoverBot behaviour), two groups: code first, docs second

```
# S. Code: AI classes, animated enemy view, collision (dynamic sphere, ray ignore), enemy projectiles, loader + game wiring, baker + rebaked scene, animator JSON copy, self-tests
git add Core/Gameplay Core/Physics Core/Scene Core/ShooterGame.cs Core/Content/Scenes Tools/SceneBake/Program.cs Platforms/Desktop/Shooter.csproj

# T. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3c (Turret behaviour), two groups: code first, docs second

```
# U. Code: EnemyTurret, skeleton-driven nodes / weapon / hitbox, animation player helpers, dynamic boxes, loader + director wiring, baker + rebaked scene, self-tests
git add Core/Animation Core/Gameplay Core/Physics Core/Scene Core/ShooterGame.cs Core/Content/Scenes Tools/SceneBake/Program.cs

# V. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-1 (player health and damage), two groups: code first, docs second

```
# W. Code: player Health / Damageable in the director, controller health binding, HUD flash + vignette, game wiring, self-tests
git add Core/Gameplay Core/UI Core/ShooterGame.cs

# X. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-2 (health pickups and loot), two groups: code first, docs second

```
# Y. Code: pickup manager + effects, loot in the director, dynamic pickup items in the loader / culling, inactive particle systems, game wiring, self-tests
git add Core/Gameplay Core/Scene Core/Rendering/Particles Core/ShooterGame.cs

# Y2. Content: gray-scale alpha textures (generator fix, regenerated PNGs, AssetMap)
git add Tools/ContentGen/gen_content.py Core/Content/AssetMap.json Core/Content/Textures/VFX

# Z. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-3 (enemy and player feedback), two groups: code first, docs second

```
# AA. Code: enemy effects, hit flash, death point, spatial one-shots, landing sounds, game wiring, self-tests
git add Core/Gameplay Core/Scene Core/Audio Core/ShooterGame.cs

# AB. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-4a (worldspace health bars), two groups: code first, docs second

```
# AC. Code: EnemyHealthBars + game wiring and --bars shot flag
git add Core/UI/EnemyHealthBars.cs Core/ShooterGame.cs

# AD. Docs
git add Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-4b (game flow and menu screens), two groups: code first, docs second

```
# AE. Code: GameFlow, MenuScreens, HUD message delay, enemies stop attacking while GameIsEnding, game wiring, self-test
git add Core/UI/GameFlow.cs Core/UI/MenuScreens.cs Core/UI/Hud.cs Core/Gameplay/GameFlowSelfTest.cs Core/Gameplay/PhysicsSelfTest.cs Core/Gameplay/Enemies/Enemy.cs Core/Gameplay/Enemies/EnemyDirector.cs Core/ShooterGame.cs

# AF. Docs
git add README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-5a (enemy detect and hit particle systems), two groups: code first, docs second

```
# AG. Code: attached enemy particle systems (alert, steam, sparks), VfxLibrary.Remove, --alert shot flag
git add Core/Gameplay/Effects/EnemyEffects.cs Core/Gameplay/Effects/VfxLibrary.cs Core/ShooterGame.cs

# AH. Docs
git add Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-5b (particle Trails module), two groups: code first, docs second

```
# AI. Code: trail data + simulation (local / world space) + ribbon drawing, trail material lookup, health bar draw order, self-test
git add Core/Rendering/Particles/ParticleSystemData.cs Core/Rendering/Particles/ParticleSystemInstance.cs Core/Rendering/Particles/ParticleManager.cs Core/Gameplay/ParticleTrailSelfTest.cs Core/Gameplay/PhysicsSelfTest.cs Core/UI/EnemyHealthBars.cs Core/ShooterGame.cs

# AJ. Docs
git add Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-5c (particle ScalingMode Local), two groups: code first, docs second

```
# AK. Code + content: localScale baked into the particle JSON, ScalingMode Local, self-test
git add Tools/ParticleBake/bake_particles.py Core/Content/Particles Core/Rendering/Particles/ParticleSystemData.cs Core/Rendering/Particles/ParticleSystemInstance.cs Core/Gameplay/ParticleTrailSelfTest.cs

# AL. Docs
git add Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```


## Phase 4c-3d-5d (weapon-layer particles, footsteps / jump sounds, ambient emitters), two groups: code first, docs second

```
# AM. Code (+ the one-line localScale change of the baker that group AK left out): weapon-layer particles (instance flags, manager collect, flash / steam / cylinders), footsteps + jump sound, ambient emitters, self-tests
git add Tools/ParticleBake/bake_particles.py Core/Rendering/Particles/ParticleSystemInstance.cs Core/Rendering/Particles/ParticleManager.cs Core/Rendering/SceneRenderer.cs Core/Gameplay/Effects/VfxLibrary.cs Core/Gameplay/Effects/WeaponEffects.cs Core/Gameplay/Effects/OverheatVisuals.cs Core/Gameplay/PlayerController.cs Core/Gameplay/PlayerSettings.cs Core/Gameplay/GameFlowSelfTest.cs Core/Gameplay/ParticleTrailSelfTest.cs Core/ShooterGame.cs

# AN. Docs (also holds the 3d-5c section of group AL if that was not committed yet)
git add Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-5e (explosion action lines, pose pitch), two groups: code first, docs second

```
# AO. Code + reference: stretched billboards with a negative velocity scale, pm_11 pitch
git add Core/Rendering/Particles/ParticleManager.cs Tools/UnityReference/PlayMode/poses.json

# AP. Docs
git add Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-5f (enemy systems in play, pause menu), two groups: code first, docs second

```
# AQ. Code (with group AO: ParticleManager.cs holds both changes): KeepWhenFinished, --alert idle / --hit-all, PauseMenu + wiring, self-tests
git add Tools/UnityReference/PlayMode/poses.json Core/UI/PauseMenu.cs Core/Rendering/Particles/ParticleSystemInstance.cs Core/Rendering/Particles/ParticleManager.cs Core/Gameplay/Effects/EnemyEffects.cs Core/Gameplay/GameFlowSelfTest.cs Core/ShooterGame.cs

# AR. Docs
git add Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```

## Phase 4c-3d-6 (jetpack), two groups: code and content first, docs second

```
# AS. Code + content: extractor --extra, extra prefab placement + JetpackPickup in the baker and the rebaked scene, imported jetpack assets, Jetpack / JetpackEffects / pickup kinds, HUD bar + toast, looping sound, emission toggle, self-tests
git add Tools/UnityExtractor/Program.cs Tools/SceneBake/Program.cs Core/Content/Scenes Core/Content/Shooter.mgcb Core/Content/AssetMap.json Core/Content/Models/Mesh_Jetpack.fbx Core/Content/Textures/Pickups Core/Content/Audio/SFX/Pickups/Pickup_Jetpack.wav Core/Gameplay Core/Scene/SceneLoader.cs Core/Audio/SfxPlayer.cs Core/Rendering/Particles/ParticleSystemInstance.cs Core/UI/Hud.cs Core/ShooterGame.cs

# AT. Docs
git add README.md Tools/UnityExtractor/README.md Tools/PORTING-PLAN.md Tools/COMMIT-GROUPS.md
```
Shared files: none with earlier groups (all earlier ones are committed). `Core/Content/Textures/Pickups` also holds the health pickup textures, which are unchanged.

## Icon and platform packaging, four groups

```
# AU. App icon master and the --icon renderer
git add Core/Rendering/IconShot.cs Core/ShooterGame.cs Core/Content/Icon.png

# AV. Desktop packaging: Info.plist, app.manifest, icns, csproj icon wiring, generated Icon.ico / Icon.bmp
git add Platforms/Desktop/Info.plist Platforms/Desktop/app.manifest Platforms/Desktop/generate-icns.sh Platforms/Desktop/Shooter.icns Platforms/Desktop/Shooter.csproj Core/Content/Icon.ico Core/Content/Icon.bmp Core/Content/Shooter.icns

# AW. Android project, its generated icons and the solution entry
git add Platforms/Android Core/Content/android-icons-generator.sh Shooter.slnx

# AX. Docs
git add README.md Tools/COMMIT-GROUPS.md
```
