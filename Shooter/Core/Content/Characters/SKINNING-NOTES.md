# Skinned characters (HoverBot, Turret) - data formats and conventions

Owner: skinned-character pipeline. Files in this folder:

| File | Origin |
| --- | --- |
| `HoverBot.fbx`, `Turret.fbx` | copies of `Assets/FPS/Animation/Rigs/*.fbx` (skinned mesh + skeleton) |
| `HoverBot@*.fbx`, `Turret@*.fbx` | copies of the clip files (one take "Take 001" each) |
| `*.animator.json` | Unity Animator controller exported by `Tools/Shooter.CharacterExport` |
| `*.skeleton.json` | real bind-pose transform of every bone, same tool |
| `../Characters.mgcb` | content project (references `Tools/Shooter.Pipeline/bin/Shooter.Pipeline.dll`) |

Regenerate the JSON: `dotnet run --project Tools/Shooter.CharacterExport` (reads the Unity project read-only).

## Content pipeline

* `SkinnedFbxImporter` (`Tools/Shooter.Pipeline`) wraps MonoGame's `OpenAssetImporter`. Verified on MonoGame 3.8.5:
  it delivers the node tree (FBX pivots / pre-rotations are baked into node transforms), `Weights0`
  `BoneWeightCollection` vertex channels, normals and UVs (no tangents), and for the @ files one `AnimationContent`
  per animated node whose keyframes are full node-local matrices. The stock `ModelProcessor` drops all of this.
* `SkinnedModelProcessor` -> `SkinnedModelContent` -> `SkinnedModelWriter` -> runtime `Shooter.Animation.SkinnedModel`.
* `AnimationClipProcessor` -> `AnimationClipContent` -> `AnimationClipWriter` -> runtime `Shooter.Animation.AnimationClip`.
* Runtime readers live in `Core/Animation` (`SkinnedModelReader`, `AnimationClipReader`, assembly `Shooter.Core`).
  Writer/reader binary layout is versioned (`FormatVersion = 1` on both sides); keep them in sync.
* Load: `Content.Load<SkinnedModel>("Characters/HoverBot")`, `Content.Load<AnimationClip>("Characters/HoverBot@RunFoward")`.
* **Toolchain constraint**: the pinned `mgcb` tool (3.8.5, `.config/dotnet-tools.json`) runs on .NET 8 and can only load
  extensions built for `net8.0` against `MonoGame.Framework.Content.Pipeline` **3.8.5** exactly. `Shooter.Pipeline` and
  `Shooter.CharacterExport` therefore target net8.0 / 3.8.5 (not net10 / 3.8.*). Bump them together with the tool manifest.
  `Shooter.Pipeline` builds into a fixed `bin/` (no Debug/Release subfolder) so the .mgcb `/reference` is stable, and
  `Platforms/Desktop/Shooter.csproj` has a `ProjectReference ... ReferenceOutputAssembly=false` so it is built first.

## Coordinate conventions (single place: `Tools/Shooter.Pipeline/SpaceConversion.cs`)

Project-wide: right-handed MonoGame world, Y up, -Z forward (see `../ASSET-CONVENTIONS.md` sections 1-2). Unity world -> MonoGame
is `F = diag(1,1,-1)`. FBX native (right-handed, cm) -> Unity import (X mirror, 0.01 from .meta `useFileScale`) -> F nets to
**RotationY(180) of the native data plus the 0.01 scale** (same as the static meshes). Default `Space=MonoGame`:

* position `(x,y,z)` -> `(-x, y, -z) * 0.01`;  normals / tangents / directions -> `(-x, y, -z)`
* rotation quaternion `(qx,qy,qz,qw)` -> `(-qx, qy, -qz, qw)`
* node-local matrix `A` (row-vector) -> `S*A*S`, `S = diag(-1,1,-1)`, translation * 0.01
* applied identically to mesh vertices, bind poses / inverse bind poses, animation tracks and `*.skeleton.json`.
  Tangents are generated after the conversion. `Space=Unity` (X mirror, left-handed) and `Space=Native` exist as options only.
* The conversion is a proper rotation, so there is no winding flip: MonoGame's importer output (clockwise front faces) works with
  standard right-handed `CreateLookAt` / `CreatePerspectiveFieldOfView` and the default `CullCounterClockwise`. Checked: the
  right-handed cross product of every stored triangle points AGAINST the vertex normal (0 of 5100 / 3017 agree = clockwise
  front), identical to the static-mesh rule. `SwapWindingOrder` stays off.
* Forward: the HoverBot faces Unity +Z; after conversion the front (`Front_Panel_C_Jnt_End`, `IrisBlade_C_Jnt`) lies at negative Z
  relative to `Robot_Root_C_Jnt` (-Z forward).
* Unity-space values (if a scene loader needs them): apply F again: position `(x, y, -z)`, quaternion `(-x,-y,z,w)`.
  `*.skeleton.json` bones also carry `unityGlobalPosition`.
* UVs: V flipped (`v' = 1 - v`, as MG's importer does). Tangents: `tangent.w = +/-1`, `bitangent = cross(normal, tangent.xyz) * tangent.w`.
* Matrices: XNA row-vector. `local = S * R * T`, `global[i] = local[i] * global[parent]`,
  `skin[i] = InverseBindPose[i] * global[i]`, `v' = v * skin[i]`. Inverse bind poses equal Assimp's bone offset matrices
  (checked to 1e-7) because the rig FBX nodes are in bind pose.

## Vertex layout (`VertexPositionNormalTextureTangentBoneWeights`, stride 68)

| offset | type | HLSL semantic |
| --- | --- | --- |
| 0 | float3 | `POSITION0` |
| 12 | float3 | `NORMAL0` |
| 24 | float2 | `TEXCOORD0` |
| 32 | float4 | `TANGENT0` (w = handedness) |
| 48 | byte4 (unsigned, NOT normalised) | `BLENDINDICES0` |
| 52 | float4 | `BLENDWEIGHT0` (sum = 1; unused slots have index 0, weight 0) |

Bone matrix palette assumed by the vertex format: `SkinnedModel.MaxBones = 64` `float4x4`s (`Matrix[]`, indices are a
byte so the hard limit is 256). HoverBot uses 18 bones, Turret 7 (bones = every node under the skeleton root, including
unweighted `*_End` / `IrisBlade` joints, so VFX can attach to them). `AnimationPlayer.CopySkinMatricesTo(Matrix[64])`
fills a palette (extra entries identity). Bone index order = parent-first DFS of the skeleton (`SkinnedModel.BoneNames`).

Mesh parts: one `SkinnedMeshPart` per FBX geometry (= one material). HoverBot: Top, EyeBall_0 (HoverBot_Body),
EyeBall_1 (HoverBot_Lens), EyeBall_2 (HoverBot_Eye), Bottom. Turret: TurretBottom, TurretCenter, TurretTop_0 (Mat_Body),
TurretTop_1 (Mat_Glass), TurretTop_2 (Mat_Light). `MaterialName` and `DiffuseTexture` (file name from the FBX) are
informational; textures are separate content. CPU vertex/index arrays are always kept; GPU buffers are created by the
reader when an `IGraphicsDeviceService` is registered (tests/tools without a device get CPU data only).

Verified sizes (metres): HoverBot bind bounds 0.65 x 1.00 x 0.64 (roughly 1 m tall); Turret 0.99 x 0.83 x 0.99
retracted, 1.36 tall when raised; with the scene scale 4 from the extractor that is 4.0 x 3.3 x 4.0 m and 5.4 m raised.

## Animation clip format

* Whole take exported (every take equals the clip range selected in the .meta files: key count == lastFrame-firstFrame+1;
  Hurt/AttackIn/AttackOut/ActivatedHurt have no clip settings and use the full take, named after the text after `@`).
* **Keys are kept as authored** (30 fps, one key per frame), not resampled. Per bone: times (s), translation, rotation
  (quaternion, sign-continuous), scale. Tracks whose values never change collapse to one key. Runtime: linear
  T/S, slerp R. Bones without a track use the bind pose.
* `Loop` = Unity `loopTime` (set per clip in `Characters.mgcb`: RunFoward, RunFowardClosed, Idle, IdleClosed, Turret Idle,
  AttackLoop). `loopBlend` is 0 everywhere; the looping clips end on their first pose (checked: pose(end) == pose(0)).
* `Duration = (frames-1)/30`. Clip name = Unity clip name; **names repeat across rigs** (`Idle`), so always address clips by
  content asset name (`Characters/<Rig>@<Clip>`), which the animator JSON carries.
* Root motion: `HasRootMotion` / `RootMotionDelta` come from the skeleton root bone (`Robot_Root_C_Jnt`; the avatar's
  `rootMotionBoneName`). **No shipped clip animates it** (constant), so the player does not apply root motion.
* The rig file and the clip files have slightly different rest transforms (e.g. `Robot_FKRoot_C_Jnt`); clips replace the local
  transform entirely, the rig's values are only the bind pose.

## AnimationPlayer (Core/Animation)

`new AnimationPlayer(model)`; `Play(clip, normalizedTime, speed)`, `CrossFade(clip, duration, normalizedOffset, speed,
fixedDuration)` (Unity transition semantics: seconds when fixed, otherwise fraction of the source state length; interrupting
a running fade freezes the blended pose as the new source), `Update(dt)`, `SkinMatrices`, `GlobalPose`, `GetBoneTransform(name)`,
`AddLayer(weight, additive, maskBones)`. `AnimationState.NormalizedTime` is unbounded like Unity (1.0 = one clip length,
keeps growing while looping). Layers: override layers lerp/slerp towards their pose per masked bone; additive layers add
`(clipPose - bindPose)` scaled by weight (approximation of Unity's additive reference pose; clips here are not flagged
additive). `CrossFade(null, d)` fades a layer out (state without motion).

## Animator JSON (`*.animator.json`, format `shooter.animator/1`)

POCOs: `Core/Animation/AnimatorControllerData.cs`; evaluator: `AnimatorController` (drives an `AnimationPlayer`):

```csharp
var data = AnimatorControllerData.FromJson(File.ReadAllText("Content/Characters/HoverBot.animator.json"));
var player = new AnimationPlayer(Content.Load<SkinnedModel>("Characters/HoverBot"));
var anim = new AnimatorController(data, player, Content.Load<AnimationClip>);
anim.SetFloat("MoveSpeed", 1f); anim.SetTrigger("Attack"); anim.Update(dt);
```

* `parameters[]`: name, type float/int/bool/trigger, defaults. HoverBot: Attack(trigger), MoveSpeed(float), Alerted(bool),
  OnDamaged(trigger), Death(bool, unused by transitions). Turret: OnDamaged(trigger), IsActive(bool).
* `layers[]`: name, blendMode override/additive, defaultWeight, avatarMask (`includedTransforms` = bones with weight > 0),
  `defaultState`, `anyStateTransitions[]`, `states[]` (id = path such as `Locomotion/Patrolling`, motion, speed,
  speedParameter, cycleOffset, transitions[]). Sub-state-machines are flattened into path ids (they are only visual groups
  here; no entry / sub-machine transitions exist). Transitions without a target but not `isExit` (orphans) are dropped.
  An Exit transition returns to the layer's `defaultState`.
* Transition fields: destination, hasExitTime, exitTime (normalized), hasFixedDuration, duration, offset, interruptionSource,
  orderedInterruption, canTransitionToSelf, conditions[{parameter, mode if/ifNot/greater/less/equals/notEqual, threshold}].
* `motion`: `{type:"clip", clip, asset}` or `{type:"blendTree", ...}` (HoverBot has only empty trees in unreachable states, no
  runtime support) or `null`.
* HoverBot layers: Base (override), Attack (additive, default weight 0.7, mask HoverBot), Dammaged (additive, weight 1, same
  mask). Note state `Idle` plays clip **IdleClosed**, `Locomotion/Patrolling` plays **RunFowardClosed**,
  `Alerted/Following` plays **RunFoward**; `HoverBot@Idle` is not referenced by the controller.
* Turret layers: Base (override: Idle -> AttackIn -> AttackLoop -> AttackOut), Damaged (additive, weight 1, mask Turret:
  Handle_R, Turret_Head, IrisBlade).

## Skeleton JSON (`*.skeleton.json`, format `shooter.skeleton/1`)

`space`, `scale`, `mirrorX`, `skeletonRoot`, `bones[]` (index, name, parent, path, `local` and `global` bind transforms each as
`position[3]`, `rotation[4]` (x,y,z,w), `scale[3]`, `matrix[16]` row-major row-vector), `otherNodes[]` (mesh holder nodes).
Positions are in MonoGame world space (metres); see `unityGlobalPosition` for Unity values. Use `global.position` to attach VFX (e.g. `EyeBall_C_Jnt`,
`IrisBlade_C_Jnt`, `Turret_Head_C_Jnt`) at bind pose, or `AnimationPlayer.GetBoneTransform(name)` at runtime.

## Limitations

* Blend trees, synced layers, mirror, IK, animation events (none exist) and transition interruption are not implemented.
* Additive layers use the bind pose as reference.
* Maximum 64 bones per skeleton, 4 influences per vertex (extra weights are dropped and the rest renormalised).
* Hierarchy scale is assumed uniform and positive; meshes are assumed to be in bind space (mesh node transforms ignored
  with a warning; they are identity in both rigs).
* Verification harness is not in the repo (scratchpad console); `Tools/Shooter.CharacterExport` is the only committed tool.
