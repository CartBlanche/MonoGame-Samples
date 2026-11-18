# GlbCheck

Summary/selftest for the GLB reader (`PipelineExtension/Glb/GlbFile.cs`, linked in, System.Numerics only).

    dotnet run -- selftest            # 116 checks vs numbers from an independent python3 JSON read
    dotnet run -- summary [files]     # per-glb meshes/prims/verts/tris/nodes/skins/joints/anims/channels/lights
    dotnet run -- dump <glb> nodes|prims|lights|anims

## Pipeline output (M2)
`GlbImporter` + `GodotStaticModelProcessor` (Release build of PipelineExtension is referenced by the .mgcb).
Geometry is emitted once per glTF mesh as bone `mesh/<name>`; each instancing node keeps its name/local transform and has a child marker bone `@<meshName>`.
Each ModelMeshPart.Tag = glTF material name (join via Materials.json glbMaterialMap[model][name]). Vertex: Position, Normal, Tangent (Vector4), UV0.
Built: lights.xnb (344 v / 160 t), structure.xnb (797767 v / 284878 t, 38 MB), matching the summary.

## Open issues / unverified
- XNB not loaded at runtime yet (only produced); Tag and vertex declaration not read back.
- Materials get the default BasicEffect from ModelProcessor (runtime must substitute its own effect).
- Skinned export, KHR lights export, animation clips: reader exposes them, no processor yet.
- UV1 (lightmap) dropped; sparse accessors unsupported; joints > 255 flagged via JointsOverflow only.
