# MonoGame FPS - Implementation Roadmap

This document outlines the phased approach to porting the Unity FPS Microgame to MonoGame.

## Current Status: ✅ Phase 0 Complete - Foundation & Architecture

---

## Phase 0: Foundation & Architecture ✅ COMPLETE

### Goals
- [x] Set up solution structure
- [x] Create core entity-component system
- [x] Implement plugin architecture
- [x] Design service locator pattern
- [x] Create event system
- [x] Set up scene management with JSON
- [x] Write comprehensive documentation

### Deliverables
- ✅ Shooter.sln with 6 projects
- ✅ Entity and GameComponent base classes
- ✅ Transform3D component with hierarchy
- ✅ IPhysicsProvider and IGraphicsProvider interfaces
- ✅ EventBus with game event types
- ✅ ServiceLocator for dependency injection
- ✅ SceneManager with JSON loading
- ✅ Unity-to-MonoGame mapping documentation
- ✅ Getting Started guide
- ✅ Example scene JSON template

---

## Phase 1: Core Systems Implementation ✅ COMPLETE

**Estimated Time: 2-3 weeks** | **Actual: Completed November 1, 2025**

### 1.1 Physics Integration ✅
- [x] Implement BepuPhysicsProvider
  - [x] Body creation and management
  - [x] Raycast, spherecast, boxcast
  - [x] Overlap queries
  - [x] Collision callbacks (OnCollisionEnter/Stay/Exit, OnTriggerEnter/Stay/Exit)
- [x] Ground detection using sphere cast
  - [x] Integrated into FirstPersonController
  - [x] Prevents bunny-hopping (mid-air jumps)
- [x] PhysicsBody component with full event support
- [x] Support for BoxShape, SphereShape, and CapsuleShape

**Files Completed:**
- `Shooter.Physics/BepuPhysicsProvider.cs` (607 lines)
- `Shooter.Core/Plugins/Physics/IPhysicsProvider.cs` (322 lines)
- `Shooter.Core/Components/Rigidbody.cs` (404 lines)

### 1.2 Graphics Implementation ✅
- [x] Implement ForwardGraphicsProvider
  - [x] Basic 3D rendering with MonoGame
  - [x] Camera system with FPS camera
  - [x] Primitive mesh generation (cubes, spheres, capsules)
  - [x] Lighting (ambient + 2 directional lights)
  - [x] BasicEffect with proper vertex/index buffers
- [x] CameraComponent with view/projection matrices
- [x] RenderableComponent (MeshRenderer)
- [x] Primitive mesh caching for performance

**Files Completed:**
- `Shooter.Graphics/ForwardGraphicsProvider.cs` (553 lines)
- `Shooter.Core/Components/Camera.cs` (372 lines)
- `Shooter.Core/Components/MeshRenderer.cs` (212 lines)

### 1.3 Input System ✅
- [x] Implement InputService
  - [x] Keyboard and mouse input
  - [x] Gamepad support (full integration)
  - [x] Left/right thumbsticks
  - [x] Triggers and buttons
  - [x] Input mapping with dead zones
  - [x] Cursor lock/unlock for FPS controls
- [x] Helper methods (GetMovementInput, IsJumpPressed, etc.)

**Files Completed:**
- `Shooter.Core/Services/InputService.cs` (341 lines)

### 1.4 Audio System ✅
- [x] Implement AudioService
  - [x] Sound effect playback
  - [x] 3D positional audio
  - [x] Music playback
  - [x] Volume control (master, SFX, music)
- [x] Audio pooling for performance

**Files Completed:**
- `Shooter.Core/Services/AudioService.cs` (149 lines)

### 1.5 Time System ✅
- [x] Implement TimeService
  - [x] Delta time tracking
  - [x] Total elapsed time
  - [x] Time scale support
  - [x] Fixed timestep accumulator for physics

**Files Completed:**
- `Shooter.Core/Services/TimeService.cs` (202 lines)

### 1.6 Entity & Component System ✅
- [x] Entity class (GameObject equivalent)
- [x] EntityComponent base class (MonoBehaviour equivalent)
- [x] Transform3D with parent-child hierarchy
- [x] Component lifecycle (Initialize, Update, Draw, OnDestroy)
- [x] Scene management with JSON loading

**Files Completed:**
- `Shooter.Core/Entities/Entity.cs` (232 lines)
- `Shooter.Core/Components/EntityComponent.cs` (102 lines)
- `Shooter.Core/Components/Transform3D.cs` (295 lines)
- `Shooter.Core/Scenes/SceneManager.cs` (288 lines)

### 1.7 Service Locator & Events ✅
- [x] ServiceLocator pattern for dependency injection
- [x] EventBus for decoupled messaging
- [x] Core game events defined

**Files Completed:**
- `Shooter.Core/Services/ServiceLocator.cs` (222 lines)
- `Shooter.Core/Events/EventBus.cs` (209 lines)

---

## Phase 1 Summary

**Total Lines of Code Added/Polished:** ~4,200 lines  
**All Core Systems:** ✅ Complete and tested  
**Architecture:** Solid foundation for FPS gameplay

**Key Achievements:**
- Full physics integration with BepuPhysics v2
- Forward rendering with lighting
- Complete input system (keyboard, mouse, gamepad)
- Audio and time services
- Entity-Component system matching Unity's paradigm
- Collision callbacks for gameplay events
- Ground detection for proper character controller

**Ready for Phase 2:** Player & Weapons expansion

---

## Phase 2: Player Systems 📋 NEXT

**Estimated Time: 2-3 weeks**

### 2.1 Player Movement
- [ ] PlayerController component
  - [ ] WASD movement
  - [ ] Sprint functionality
  - [ ] Jump mechanics
  - [ ] Crouch/stand
  - [ ] Ground detection
  - [ ] Air control
  - [ ] Fall damage
- [ ] FirstPersonCameraController
  - [ ] Mouse look
  - [ ] Camera bobbing
  - [ ] Aiming zoom

**Reference Unity Files:**
- `PlayerCharacterController.cs` (485 lines)
- `PlayerInputHandler.cs`

### 2.2 Weapon System
- [ ] WeaponController base class
  - [ ] Fire modes (manual, automatic, charge)
  - [ ] Ammo management
  - [ ] Reload mechanics
  - [ ] Recoil system
  - [ ] Spread/accuracy
- [ ] ProjectileBase and implementations
  - [ ] Standard projectile
  - [ ] Charged projectile
  - [ ] Projectile pooling
- [ ] PlayerWeaponsManager
  - [ ] Weapon switching
  - [ ] Weapon slots (9 weapons)
  - [ ] Weapon bob animation
  - [ ] Aiming state

**Reference Unity Files:**
- `WeaponController.cs` (651 lines)
- `PlayerWeaponsManager.cs` (604 lines)
- `ProjectileBase.cs`
- `ProjectileStandard.cs`

### 2.3 Health & Damage
- [ ] Health component
  - [ ] Take damage
  - [ ] Heal
  - [ ] Death handling
  - [ ] Invincibility
  - [ ] Critical health threshold
- [ ] Damageable component
  - [ ] Damage multipliers
  - [ ] Damage events
- [ ] DamageArea component (AoE damage)

**Reference Unity Files:**
- `Health.cs` (63 lines)
- `Damageable.cs`
- `DamageArea.cs`

---

## Phase 3: AI & Enemies 📋 PLANNED

**Estimated Time: 3-4 weeks**

### 3.1 Enemy Core
- [ ] EnemyController base class
  - [ ] State machine
  - [ ] Patrol behavior
  - [ ] Attack behavior
  - [ ] Death handling
  - [ ] Loot drops
- [ ] EnemyManager
  - [ ] Enemy registration
  - [ ] Enemy count tracking
  - [ ] Spawn management

**Reference Unity Files:**
- `EnemyController.cs` (410 lines)
- `EnemyManager.cs`

### 3.2 Detection System
- [ ] DetectionModule
  - [ ] Vision cone detection
  - [ ] Range checking
  - [ ] Line of sight
  - [ ] Attack range
  - [ ] Target tracking
- [ ] Audio detection (optional)

**Reference Unity Files:**
- `DetectionModule.cs` (191 lines)

### 3.3 Navigation & Pathfinding
- [ ] NavigationModule
  - [ ] Waypoint following
  - [ ] A* pathfinding
  - [ ] Obstacle avoidance
- [ ] PatrolPath system
  - [ ] Path nodes
  - [ ] Path following

**Reference Unity Files:**
- `NavigationModule.cs`
- `PatrolPath.cs`

### 3.4 Enemy Types
- [ ] EnemyMobile (ground enemies)
- [ ] EnemyTurret (stationary)
- [ ] Special behaviors

**Reference Unity Files:**
- `EnemyMobile.cs`
- `EnemyTurret.cs`

---

## Phase 4: Game Systems 📋 PLANNED

**Estimated Time: 2 weeks**

### 4.1 Objective System
- [ ] Objective base class
  - [ ] Progress tracking
  - [ ] Completion detection
  - [ ] Events
- [ ] ObjectiveManager
  - [ ] Multiple objectives
  - [ ] Win condition
- [ ] Objective types:
  - [ ] ObjectiveKillEnemies
  - [ ] ObjectiveReachPoint
  - [ ] ObjectivePickupItem

**Reference Unity Files:**
- `Objective.cs`
- `ObjectiveManager.cs`
- `ObjectiveKillEnemies.cs`

### 4.2 Pickup System
- [ ] Pickup base class
  - [ ] Collision detection
  - [ ] Pickup effects
  - [ ] Respawn logic
- [ ] Pickup types:
  - [ ] Health pickup
  - [ ] Ammo pickup
  - [ ] Weapon pickup

**Reference Unity Files:**
- `Pickup.cs`
- `HealthPickup.cs`
- `AmmoPickup.cs`

### 4.3 Game Flow
- [ ] GameFlowManager
  - [ ] Game states (menu, playing, paused, game over)
  - [ ] Win/lose conditions
  - [ ] Scene transitions
- [ ] ActorsManager
  - [ ] Player tracking
  - [ ] Actor registration

**Reference Unity Files:**
- `GameFlowManager.cs`
- `ActorsManager.cs`

---

## Phase 5: UI Implementation 📋 PLANNED

**Estimated Time: 2-3 weeks**

### 5.1 Gum Integration
- [ ] Set up Gum runtime
- [ ] Create base UI components
- [ ] UI service for Gum management

### 5.2 HUD Elements
- [ ] Crosshair
  - [ ] Dynamic crosshair
  - [ ] Enemy detection indicator
- [ ] Health bar
- [ ] Ammo counter
- [ ] Weapon display
- [ ] Objective tracker
- [ ] Compass/minimap
- [ ] Notification system

**Reference Unity Files:**
- `CrosshairManager.cs`
- `PlayerHealthBar.cs`
- `AmmoCounter.cs`
- `WeaponHUDManager.cs`
- `ObjectiveHUDManager.cs`
- `NotificationHUDManager.cs`

### 5.3 Menus
- [ ] Main menu
- [ ] Pause menu
- [ ] Options/settings
- [ ] Win/lose screens

**Reference Unity Files:**
- `InGameMenuManager.cs`
- `MenuNavigation.cs`

---

## Phase 6: Polish & Features 📋 PLANNED

**Estimated Time: 2-3 weeks**

### 6.1 Visual Effects
- [ ] Muzzle flash
- [ ] Impact effects
- [ ] Explosion effects
- [ ] Particle systems
- [ ] Death effects

### 6.2 Audio Implementation
- [ ] Footstep sounds
- [ ] Weapon sounds
- [ ] Impact sounds
- [ ] Ambient audio
- [ ] Music system

### 6.3 Advanced Features
- [ ] Jetpack system
- [ ] Charging weapons
- [ ] Weapon fuel cells
- [ ] Overheat mechanics
- [ ] Position bobbing

**Reference Unity Files:**
- `Jetpack.cs`
- `ChargedWeaponEffectsHandler.cs`
- `OverheatBehavior.cs`
- `PositionBobbing.cs`

---

## Phase 7: Optimization & Testing 📋 PLANNED

**Estimated Time: 1-2 weeks**

### 7.1 Performance
- [ ] Object pooling for projectiles
- [ ] Frustum culling
- [ ] LOD system (if using complex models)
- [ ] Profiling and optimization

### 7.2 Testing
- [ ] Unit tests for core systems
- [ ] Integration tests
- [ ] Gameplay testing
- [ ] Balance tweaking

### 7.3 Documentation
- [ ] API documentation
- [ ] Tutorial videos
- [ ] Example projects
- [ ] Best practices guide

---

## Phase 8: Distribution 📋 PLANNED

**Estimated Time: 1 week**

### 8.1 Build Pipeline
- [ ] Windows build
- [ ] Linux build (optional)
- [ ] macOS build (optional)

### 8.2 Packaging
- [ ] Content pipeline optimization
- [ ] Asset compression
- [ ] Installer creation

### 8.3 Release
- [ ] GitHub repository
- [ ] Documentation website
- [ ] Sample game builds
- [ ] Tutorial series

---

## Feature Comparison with Unity FPS Sample

### Core Features

| Feature | Unity Original | MonoGame Port | Status |
|---------|---------------|---------------|--------|
| Player Movement | ✅ | 🔄 | Phase 2 |
| Weapon System | ✅ | 🔄 | Phase 2 |
| Enemy AI | ✅ | 📋 | Phase 3 |
| Pathfinding | ✅ NavMesh | 📋 A* | Phase 3 |
| Health/Damage | ✅ | 📋 | Phase 2 |
| Objectives | ✅ | 📋 | Phase 4 |
| Pickups | ✅ | 📋 | Phase 4 |
| UI/HUD | ✅ uGUI | 📋 Gum | Phase 5 |
| Audio | ✅ | 📋 | Phase 6 |
| VFX | ✅ | 📋 | Phase 6 |

### Advanced Features

| Feature | Unity Original | MonoGame Port | Status |
|---------|---------------|---------------|--------|
| Jetpack | ✅ | 📋 | Phase 6 |
| Charge Weapons | ✅ | 📋 | Phase 6 |
| Multiple Objectives | ✅ | 📋 | Phase 4 |
| Enemy Types (3+) | ✅ | 📋 | Phase 3 |
| Weapon Switching | ✅ | 📋 | Phase 2 |
| Crouch/Sprint | ✅ | 📋 | Phase 2 |
| Fall Damage | ✅ | 📋 | Phase 2 |

---

## Development Guidelines

### Code Standards
- **Documentation**: Every public class/method must have XML comments
- **Unity Comparison**: Include comments explaining differences from Unity
- **Educational Focus**: Prioritize code clarity over cleverness
- **Testing**: Write tests for core systems

### Commit Strategy
- Commit after completing each sub-feature
- Use descriptive commit messages
- Reference phase numbers in commits
- Tag major phase completions

### Code Review Points
- Is it educational? Can a beginner understand it?
- Is it documented? Are Unity comparisons clear?
- Is it modular? Can components be swapped?
- Is it performant? Are we pooling objects?

---

## Milestones & Demos

### Milestone 1: "First Playable" (End of Phase 2)
**Demo**: Player can walk, look around, and shoot basic projectiles

### Milestone 2: "Combat Ready" (End of Phase 3)
**Demo**: Player can fight and destroy basic enemies

### Milestone 3: "Full Game Loop" (End of Phase 4)
**Demo**: Complete game with objectives, win/lose conditions

### Milestone 4: "Polish Complete" (End of Phase 6)
**Demo**: Full feature parity with Unity sample

### Milestone 5: "Release Candidate" (End of Phase 7)
**Demo**: Optimized, tested, ready for release

---

## Next Immediate Steps

1. **Implement BepuPhysicsProvider** (Phase 1.1)
   - Critical for character movement and collision

2. **Create BasicGraphicsProvider** (Phase 1.2)
   - Need to see objects on screen

3. **Build InputService** (Phase 1.3)
   - Required for any player interaction

4. **Implement TimeService** (Phase 1.5)
   - Foundation for all timed gameplay

5. **Create First Player Prototype** (Phase 2.1)
   - FPS controller with basic movement

---

**Total Estimated Time: 15-20 weeks for complete implementation**

**Legend:**
- ✅ Complete
- 🔄 In Progress  
- 📋 Planned
- ⏸️ Blocked
- ❌ Cancelled

---

*Last Updated: October 27, 2025*
