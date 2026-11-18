# Conventions: Godot vs MonoGame

Verified on the TPS demo unless marked (unverified).

| Topic | Godot 4 | MonoGame | Action |
|---|---|---|---|
| Handedness | right-handed, Y up | right-handed, Y up | none; no axis mirror, no scale bake |
| Camera/forward | looks -Z | looks -Z | none (but see camera rig below) |
| Units | 1 unit = 1 m | use 1 = 1 m | none |
| Winding | glTF front = CCW; XNA treats CW as front | no index flip in the importer | draw with `RasterizerState.CullClockwise` (level upright, not inside-out). Early plan docs saying CullCounterClockwise were wrong |
| UV origin | top-left | top-left | no FlipUVs |
| Normal map | OpenGL (+Y up) | shader decides | sample as +Y up, no green flip |
| Tangent | glTF vec4, w = handedness | Vector4 | keep w; generate if missing |
| Quaternion | (x,y,z,w) | (X,Y,Z,W) | copy |
| Matrix | column-vector, basis columns | row-vector, `v * M` | transpose |

Unlike Unity there is **no axis mirror, 180 deg turn or scale bake**. If something is mirrored, suspect the importer or a rig basis first.

## Transform3D in .tscn
`Transform3D(b00..b22, ox, oy, oz)`: the 9 numbers are the Basis **rows**, then origin. XNA `Matrix` Right/Up/Backward = Godot basis **columns** (transpose), Translation = origin. Compose world matrices in the baker as `child * parent`. A rotated node (a forklift) is a good unit test.

## Camera rig lesson (cost one wrong start yaw)
- A camera rig does not necessarily look along its parent's -Z. Multiply out the whole basis chain (player.tscn CameraBase > CameraRot > SpringArm3D; SpringArm basis x=-1, z=-0.94 is turned ~180 deg) to get the real view direction.
- TPS demo result: the camera looks along the spawn marker's **+Z**, and the glb model also faces +Z, so the start yaw comes from the spawn's Backward and the model needs **no extra 180 deg turn** at placement (only the XNA heading convention RotY(pi) inside the renderer). (The first version assumed "player faces -Z, add 180": wrong.)
- Consequence: Godot's camera offset (-0.6,-0.3,1.1) for the aim shoulder becomes (+0.6,-0.3,-1.1) in the port. Apply camera shake only to the final view matrix.
- Check: take a screenshot at the spawn and compare with the reference frame before building anything on top.
- `Basis.looking_at(target)` points **-Z**; glTF models usually face +Z. Use a debug arrow to confirm.

## Other
- Euler orders: avoid; use matrices/quaternions (`rotation_degrees` in tscn is YXZ).
- Colour: keep `Color` plain; flag `srgb` per texture in AssetMap and decode in the shader. Data textures (ORM, normal): no sRGB, no premultiply.
- `physics_ticks_per_second` default 60; fixed-step sim, render from the latest state. Fix the step in the sim, never wall-clock.
- Gamepad: XNA stick Y is +up (Godot +down): negate. Godot trigger axes 4/5 = LT/RT, A = button 0.
- Mouse look: Godot value is rad/pixel (0.001 in TPS); stick look in rad/s times dt.
