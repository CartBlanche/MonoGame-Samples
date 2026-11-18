# Rendering on DesktopGL

Verified on the TPS demo (macOS arm64 GL) unless marked. Lighting reached a **partial** match only (grid luma MAE -24%, target was -50%).

## Pipeline
- Scene renders to an **HDR target** (`HalfVector4` works on macOS GL; Rgba1010102 and Color fallbacks coded, untested), then full-screen **tonemap** (AgX polynomial, Godot's own curve; not generic ACES) and **bloom**. Glow billboards draw into the HDR pass so bloom lifts them.
- Materials: `StandardMaterial3D` -> `PbrLit.fx` (albedo sRGB decode in shader, normal, ORM, emission, GGX + ambient). ORM: R=AO, G=rough, B=metal; channel selectors from the `.tres`. Keep Godot parameter names.
- Alpha-blend materials, ShaderMaterials and material-less primitives were not drawn in the slice (not done).
- Read `Environment` field by field and honour `*_enabled` (disabled effects keep stored values). `.gdshader` -> HLSL (unverified): keep uniform names.

## Lighting without GI (what worked, in order of effect)
1. **Key light direction** gave the biggest contrast gain (pass 3): Godot lights walls and leaves floors dim; a fake directional light slanted toward surfaces facing the player. Reshaping ambient did nothing.
2. Hemisphere ambient (neutral sky, dark ground); fog distance-only (start ~15-25 m, never tint near surfaces).
3. Exposure + black point + S-curve; neutral reflections, metals diffuse-ish (no probes). 8 nearest of 20 lights per draw.
4. **Distance fade** on key and flat ambient: dark near field, bright far structure (pass 4); a hand-placed fake teal light stands in for baked bounce.
- Raw Godot ambient/fog values over-brighten without VoxelGI; calibrate against reference screenshots, not numbers. The tuned values are eyeballed, not derived.
- Probes: Light/ReflectionProbe export **positions only**; baked colour is in lightmaps, so no per-area ambient from probes.
- The key is a world-space stand-in tuned for the spawn view; other views light differently.

## Emissive is relative to the pass exposure
A second pass with another exposure needs its own emission scale: the menu pass (exposure 1.3) showed eyes/chest ~2.6x dimmer than the game (3.4) until emission was rescaled by `3.4/exposure` and saturation mix set to 1.

## Tuning method
- Numbers: `Tools/ImageStats/imagestats.py` (luma, std, sat, hue, 4x4 luma/RGB grid, `--ref`). Same frame as the reference. **Mean/std/sat/hue alone can match while the image is wrong**; use the spatial grid metric and **always look at both PNGs** side by side before accepting a pass.
- Stall rule: no gain in 3 iterations -> stop and report. Keep env-var overrides (`TPS_EXP`, `TPS_KEY`, ...) for iteration without rebuild.
- Shader edits: if only a `.fxh` include changed, `touch` the `.fx` files so the content builder rebuilds them.

## GL gotchas
- Effect parameters default to 0: set every parameter from C# every frame.
- Use `.w` not `.z` for depth in shaders; MGFX compiles only on a supported host; keep shaders SM3-compatible.
- **SSAO is not possible** when the HDR depth buffer can't be sampled: use a normal-map cavity term. Decals the same: projected quads / `BasicEffect` with a radial alpha texture (a custom shader never drew the blob shadow; snap it to the feet, the visual floor sat ~0.1 m above the collider top).
- Premultiply off and no sRGB for data textures.
- Real shadow maps, volumetric fog, sky: not done (unverified approach).

## Effects polish (verified on the TPS demo)
- Read the particle/light **sub-resources** (`CPUParticles3D`/`GPUParticles3D`, `OmniLight3D` energy/range curves in the AnimationPlayer) and port counts, lifetimes, gravity, scale curves; Godot defaults apply for anything unset (initial velocity 0). Beam shaders (`LaserShader.gdshader`): noise discard + emission, driven by the shoot-clip tracks; the ray mesh is compressed binary so tube radius/length are assumptions (say so).
- Transient lights (bullets) go into the nearest-N light set with their own range and priority slots; keep per-light range in the shader params.
- **Collision-only proxies:** skip material-less glb primitives (`-colonly`).

## Real shadows (limited form verified)
- Godot demos may have NO directional light: check which lights have `shadow_enabled` (and the Settings key, e.g. `rendering/shadow_mapping`) before choosing a design.
- Level: one orthographic map (2048 R32F colour target, depth written as colour) following the player, texel-snapped, bounding-sphere culled casters (player, robots, level items; skip alpha, transparent and material-less), receivers with PCF + normal-offset bias, edge fade, Settings toggle with the old blob shadow as fallback. Measured cost +4 % (20.0 -> 20.8 ms at 1280x720, 970 of 2914 draws casting).
- Menu: a perspective spot map from the menu SpotLight3D with the robot as the only caster; the floor needs its own shader so the baked fake blob can be dropped.
- Cost method: a `--bench N` path (fixed camera, no vsync, 1-pixel backbuffer readback so GPU time counts), 3 runs OFF vs ON, avg and p95.
- Without GI, shadow visibility depends on how dark the floors are: darken ambient on light-facing surfaces, not only the key.

## Spot shadows from the level's own SpotLight3Ds (verified, v0.4)
- If the Godot scene has no directional light, the shadow casters are its SpotLight3Ds: count them and read `spot_range`, `spot_angle`, `shadow_enabled`. Do not invent a sun.
- Design that worked: the 3 nearest spots (hysteresis 1.5 m, 0.35 s fade, distance fade 14..26 m) each render a 512 tile of one R32F atlas (cone frustum fov = 2 x (outer + 4 deg)); receivers use a 4-tap Vogel PCF with radius 2.5 texels and a slope/normal bias; cost about +14 % in the first version (target <= +12 %). Keep the old key-light map behind an env flag as a fallback and a Settings toggle for the whole feature.
- **MGFX/GLSL macOS pitfall:** never upload `float4x4 X[n]` uniform arrays; upload `float4 R[4n]` rows and multiply by hand (see pitfalls). Bisect shader faults by adding one use at a time, because an unused uniform is optimised away and hides the fault.
- Distant lamps visibly "switch on" as they enter the nearest-N set; this reads as a feature (kept, a flicker-on effect is listed in IMPROVEMENTS), not a bug.

## Re-baselining a lighting pass (v0.4)
- Mixed reference sets conflict: godot-08 (4.4.1) was about half as bright as godot-02 (YouTube, luma 0.114 vs 0.207), so improving one metric worsens the other. Pick one baseline per pass and retire the other target.
- What moved the metric most: exposure 2.4 -> 1.9, key gain 2.6 -> 1.8 with a redder key colour, flat ambient 0.35 -> 0.2 (grid luma MAE vs godot-08 0.100 -> 0.056). Variants that were worse: key 2.4 with ambient 0.12; ambient 0.1 with warm 0.2.
- The shadowed menu is judged against the 4.4.1 menu capture with shadows on; keep a separate "shadows off" stats check only for the fallback path.
- Player colour is random in Godot; a tint is a guess, do not tune it to a single capture.
