# UnityExtractor

Command-line tool (.NET 10, no NuGet dependencies) that converts a Unity project's YAML scenes and prefabs
into JSON, as the data foundation for porting the Unity FPS Microgame to MonoGame. It never modifies the
Unity project.

## Usage

```
dotnet run -c Release -- <unityProjectRoot> <outputDir> [--scenes A,B,C] [--flatten] [--prefabs-all]
```

Example (the 4 build scenes, flattened):

```
dotnet run -c Release -- ../../../FirstPersonShooter out --flatten
```

| Option | Meaning |
| --- | --- |
| `--scenes` | Comma separated scene names (looked up as `Assets/**/<name>.unity`). Default: `IntroMenu,MainScene,WinScene,LoseScene`. |
| `--flatten` | Resolve nested prefab instances: apply overrides, removed components/GameObjects, added objects, and emit final GameObject trees with world transforms. Without it, output is "raw" (1:1 with the YAML, instances left as overrides). |
| `--prefabs-all` | Also export every prefab under `Assets/`, not only those reachable from the scenes. |
| `--extra` | Comma separated asset paths (e.g. `Assets/FPS/Prefabs/Pickups/Pickup_Jetpack.prefab`) used as additional roots: they and everything they reference are added to the manifest and exported, without pulling in a whole other scene. |

Output layout:

```
<outputDir>/
  Scenes/<SceneName>.json
  Prefabs/<PrefabName>.json      (name gets parent-folder prefixes only on collision)
  Prefabs/index.json             (prefab asset path -> json file)
  asset-manifest.json
  component-inventory.json
  materials.json
```

## How it works

* **GUID index**: every `*.meta` under `Assets/`, `Packages/` and `Library/PackageCache/` (shown as
  `Packages/<name>/...`) maps `guid -> asset path`.
* **YAML**: a small hand-rolled parser for the Unity YAML subset (multi-document `--- !u!<classId> &<fileID> [stripped]`,
  block/flow collections, wrapped scalars). Integers are kept as exact 64-bit values (fileIDs).
* **Prefab expansion** (`--flatten`): each PrefabInstance is expanded recursively from its source prefab, then its
  `m_Modifications` (`propertyPath`/`value`/`objectReference`, including `Array.size` and `Array.data[N]`),
  `m_RemovedComponents` and `m_RemovedGameObjects` are applied. Stripped documents are resolved through
  `m_PrefabInstance` + `m_CorrespondingSourceObject`. Hierarchy is rebuilt from `Transform.m_Father` (so objects
  added to an instance from the outer file attach correctly); child order follows `m_Children`; scene roots follow
  the `SceneRoots` document. Override targets inside nested-in-nested prefabs use Unity's
  `innerFileID ^ nestedInstanceFileID` keys.
* **References**: `{fileID, guid}` becomes `{"guid", "path", "fileID", "name"?}` (`name` = sub-asset name, e.g. the mesh
  inside an FBX, from the importer's `internalIDToNameTable`). Built-in resources get `"builtin": true`. Missing assets
  get `"missing": true`. Local references (same file) become `{"ref": "<uid>", "class": "<type>"}` in flattened output
  and `{"fileID": n}` in raw output.

## Output schema

### Scenes/*.json and Prefabs/*.json

Common header: `source`, `kind` (`scene`|`prefab`), `mode` (`flattened`|`raw`), `guid`, `settings`
(`RenderSettings`, `LightmapSettings`, `NavMeshSettings`, `OcclusionCullingSettings` property maps), `prefabInstances`.

`prefabInstances[]` (direct instances in that file, both modes):

```
{ id, sourcePrefab, sourcePrefabGuid, parentTransform,
  overrides: [ { target: {fileID, type?, gameObject?}, propertyPath, value, objectReference? } ],
  removedComponents: [fileID], removedGameObjects: [fileID],
  addedGameObjects: [...], addedComponents: [...] }
```

**Flattened** adds `roots[]` (GameObject trees) and `stats`; `warnings[]` appears when something could not be resolved.

```
GameObject: { id, name, active, activeInHierarchy, layer, tag, staticFlags,
              prefab?,                       // asset the GameObject originates from
              transform: { localPosition[3], localRotation[4 xyzw], localScale[3],
                           worldPosition[3], worldRotation[4], worldScale[3] },
              rectTransform?: { m_AnchorMin, m_AnchorMax, m_AnchoredPosition, m_SizeDelta, m_Pivot },
              components: [...], children: [...] }
component:  { id, type, classId, props }                         // built-in components, raw serialized props
            { id, type: "MonoBehaviour", classId: 114, script, scriptPath, scriptGuid, fields }
```

`id` values are unique strings: the fileID for objects declared in the file itself, `instanceFileID/.../fileID`
for objects that come from prefab instances. World values are computed in double precision and rounded to 6 decimals;
local values are the raw serialized numbers. The root of a prefab file is in "prefab space" (its own origin).

**Raw** has `gameObjects[]` (`fileID`, `name`, `active`, `layer`, `tag`, `components[]` including the Transform with
`m_Father`/`m_Children` fileIDs) and `strippedObjects[]`.

#### Model (FBX/OBJ) instances

Instances of model assets (e.g. `HoverBot.fbx`) have no YAML hierarchy. They are flattened to a GameObject carrying a
`ModelInstance` pseudo-component (`model`, `nodeOverrides[]`: overrides on inner renderers/animator with the node name).
Inner bones/nodes that other objects are parented to are materialised as placeholder GameObjects (name from the
importer table, `ModelNode` pseudo-component) with an identity local transform: their real transform lives in the
FBX file and is unknown to this tool.

### asset-manifest.json

Transitive closure (by GUID, scanning scenes, prefabs, materials, controllers, `.asset` files and model importer
metas) of everything the scenes use:

```
{ scenes[], summaryByType{}, assets: [ { path, guid, type, referencedBy[], subAssetsUsed?[{fileID,name,class}], import? } ],
  unresolvedGuids: [ { guid, referencedBy[] } ] }
```

`type`: `model`, `material`, `texture`, `sprite`, `audio`, `animationClip`, `animatorController`, `avatarMask`,
`audioMixer`, `font`, `fontAsset` (TMP), `shader`, `script`, `prefab`, `scene`, `scriptableObject`, `lightingSettings`,
`inputActions`, `asset`, `other`. `import` has texture size/type, model scale, audio flags where known.
Animation clips are normally sub-assets of FBX files (see `subAssetsUsed` with class `AnimationClip`).

### component-inventory.json

`gameObjectsPerScene`, `components[]` (built-in component types) and `monoBehaviourScripts[]` (by script class):
`totalInScenes`, `perScene{}` (flattened counts) and `prefabAssetsContaining` (number of exported prefab assets
whose resolved content includes it). Scripts have `scriptPath`/`projectScript` (false for package scripts such as
UGUI `Image`).

### materials.json

For each material used: `shader {name,path,guid}`, `keywords`, `renderQueue`, `textures{prop:{texture,tiling,offset}}`,
`colors{prop:[r,g,b,a]}`, `floats{}`, plus `derived` (baseColor, baseMap, smoothness, metallic, emission,
cullMode, surfaceType, blendMode, alphaClip, alphaCutoff, zWrite, src/dst blend). Materials embedded in model
files are listed under `embeddedModelMaterials`.

## Limitations

* The internal node hierarchy and transforms of FBX/OBJ models are not read (see above).
* Override targets that no longer exist (stale overrides, e.g. `m_Mesh` on `Floor_*` prefabs) are skipped and reported
  in `warnings`, as Unity itself ignores them.
* `ParticleSystem` and other components are emitted as raw serialized props, not interpreted.
* Material variants (`m_Parent`) are reported but properties are not merged with the parent's.
* World scale is the lossy column-length scale; shear from non-uniform parent scale is not represented.
* Unresolved GUIDs (assets missing from the project, e.g. generated lighting settings) are listed in the manifest.
