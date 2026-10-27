# Conventions (coordinates, meshes, colour)

Status: **verified** on asymmetric meshes (door/wall tiling, weapon pieces) in the Shooter port unless marked *assumed*.

## Coordinate system
Unity: left-handed, Y up, +Z forward, 1 unit = 1 m. MonoGame: right-handed, Y up, -Z forward. Keep Y up and metres; **mirror Z**:

```
F = diag(1, 1, -1)
position   (x, y, z)    -> (x, y, -z)
quaternion (x, y, z, w) -> (-x, -y, z, w)     // F R F
scale      (x, y, z)    -> (x, y, z)
```
Convert each node's local TRS independently with the same rules (conjugation by F composes). World = `Scale * Rotation * Translation`, parent-multiplied (`child * parent`, XNA row vectors).

Camera: after F a Unity camera looks down -Z, MonoGame's default. Use `Camera.fieldOfView` directly as vertical FOV.

Keep gameplay math in Unity space where the script uses camera-local axes (yaw in Unity degrees, positive right; pitch positive down) and convert at the edge: formulas then stay line-for-line copies of the script, and Inspector values can be typed in directly. Rotating "about Y" by +angle in Unity is a negative angle in the mirrored world (e.g. pickup spin).

## Meshes (FBX/OBJ)
- Unity imports by mirroring X, then applies `globalScale * fileScale`. Unity vertex = `(-x_file, y_file, z_file) * scale`. Composed with F that is **RotationY(180)** of the file data. Bake that into the vertex data (positions, normals, tangents) in the content processor; do not mirror again at runtime.
- Scale = `globalScale * (UnitScaleFactor/100 if useFileScale)` from the `.meta` and FBX header.
- **Ignore `ModelBone` transforms** for static models. Unity prefabs already carry node TRS and the mesh asset is raw geometry. Use `ModelMesh.MeshParts` vertex data with the transform from the scene JSON.
- Match `MeshFilter.m_Mesh.name` to `ModelMesh.Name`. `MeshPart.Tag` = source material name; match to Unity materials by name where the OBJ importer remapped them, otherwise by submesh order. Material tags in FBX often do not match Unity material names; fall back to order.
- Vertex layout: Position, Normal, TexCoord0, Tangent, Binormal (stride 56, 16-bit indices). Replace the placeholder BasicEffect with the PBR effect.
- **Winding:** MonoGame's OpenAssetImporter already flips triangle order so default CullCounterClockwise is right. Do nothing extra. If you ever apply F to a mesh directly you must swap winding (you shouldn't).
- **UV:** content pipeline applies `FlipUVs` (`v' = 1 - v`). Textures are loaded unflipped. Do not flip again. UI sprite rects: Unity is bottom-left based, SpriteBatch is top-left, flip Y.
- **Tangent space:** `Tangent0 = dP/du`, `Binormal0 = dP/dv'`. Unity normal maps are OpenGL-style. Shader: `T = Tangent0; B = -Binormal0; N = Normal0; n = normalize(T*nx + B*ny + N*nz)`.
- Proof method for handedness: take an asymmetric mesh placed with a rotation in a prefab and check it fills the gap between known neighbours (see the door/wall check in Shooter's ASSET-CONVENTIONS.md section 6).

## Colour space
Unity runs linear. MonoGame's TextureProcessor has no sRGB output. Decision used: store textures as plain `Color`, record `srgb` per texture in AssetMap, render the 3D scene in linear into an HDR-capable target (`HalfVector4` / `Rgba1010102`), decode sRGB textures at sample time (a `ColorSRgb` copy or `pow` in the shader), and encode in a final full-screen pass.
- Unity marks AO and MetallicSmooth maps sRGB too; decode them the same way to match the picture.
- **UI blends over the finished frame in linear light** (measured: black alpha 0.6 over 32 gives 18, gamma blend gives 13). Draw the HUD into an off-screen layer with premultiplied accumulation and linear tints and composite it in the final pass before the sRGB encode. Plain SpriteBatch alpha (gamma) looks slightly different (*assumed acceptable for menus*).
- Some Unity UI colours shift (red 255,0,0 -> 234,51,35) for an unidentified reason; the Shooter port draws them as authored (*open*).
- HDR emission set via a MaterialPropertyBlock is treated by Unity as gamma -> linear; the material asset's value is not what is drawn (enemy eye colour). A gradient used as authored (overheat gem) matched better than converting it. Test both against a capture.
