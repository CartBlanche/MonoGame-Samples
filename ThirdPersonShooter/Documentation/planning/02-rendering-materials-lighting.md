# 02 - Rendering, materials and lighting (Godot TPS demo -> MonoGame DesktopGL)

Scope: what the Godot demo renders with, the MonoGame equivalent, shader list, pass order, vertical-slice subset, risks, ordered tasks.
Source reads: `project.godot`, `level/geometry/environment.tres`, `level/level.tscn`, `menu/menu.tscn`, `player/player.tscn`, `player/bullet/bullet.tscn`, `enemies/red_robot/*`, `level/geometry/models/*.tres`, all `.gdshader`. No binary (.glb, .res, .exr) was opened. Anything marked (?) is unverified.

## 1. Key findings (these change the plan)

1. **There is no DirectionalLight3D, no LightmapGI and no WorldEnvironment sky in the level.** The level is lit by: constant ambient colour + a baked **VoxelGI** (`level/geometry/giprobe_data.res`, subdiv 2, size 227.9 x 97.6 x 234.1 m) + emissive materials + point/spot lights that may be embedded in `lights.glb` (?) + 3 reflection probes that are **hidden** (`visible = false`). Consequence: **no cascaded shadow maps are needed**, and no sky IBL. The big fidelity gap is the baked GI, which cannot be reused (Godot-binary voxel data) and must be approximated.
2. In `environment.tres` the `ssao_*`, `sdfgi_*` and `glow_*` values are present but **`ssao_enabled`, `sdfgi_enabled` and `glow_enabled` are absent, so they are default `false`**. SSAO, SDFGI and the level glow are *off* in the shipped level. Only tonemap, fog, volumetric fog and colour adjustment are on. (Glow is on in the menu environment, see table.) SSAO is therefore out of scope.
3. Character shadows are **blob decals** (`Decal` nodes in player and robot) not shadow maps. The only real shadows: menu SpotLight3D (`shadow_enabled`), flying forklift SpotLight (`shadow_enabled`, `light_projector` texture, energy 12), robot OmniLight (hidden, energy 0 at rest, `shadow_enabled`, range 8.69). All are optional for the slice.
4. The look is dominated by: **AgX tonemap at exposure 4.5**, saturation 1.4, orange ambient, orange-brown fog, many emissive trims (`emission_energy_multiplier` up to 8.5), PBR metal with ORM textures, and additive laser/spark/ember FX.

## 2. Godot feature table (real values) and MonoGame approach

| Godot feature | Real value (file) | MonoGame equivalent / approach | Fidelity |
|---|---|---|---|
| Renderer | Forward+ (default), `scaling_3d/mode=2` (FSR2-class upscaler), `use_debanding=true`, `occlusion_culling=true`, `viewport/hdr_2d=true`, `request_hdr_output=true`, aniso 4, decal filter 1, fullscreen 1920x1080 stretch canvas_items/expand | Native-res forward renderer, HDR RT, no upscaler. Debanding -> 1-LSB triangular dither in tonemap pass. Aniso 4 on the sampler states. HDR output: skip. Occlusion culling: frustum cull only (+ optional per-room AABB grouping) | Exact except upscaler/HDR |
| Tonemap | `tonemap_mode=4` (AgX), `tonemap_exposure=4.5`, `tonemap_white=6.0`, `tonemap_agx_contrast=1.53` | Port Godot's `tonemap.glsl` AgX (MIT, in Godot repo `servers/rendering/renderer_rd/shaders/effects/tonemap.glsl`) to HLSL in `PostTonemap.fx`. Exposure multiplies linear scene colour first | Exact if ported from Godot source |
| Colour adjustment | `adjustment_enabled`, `adjustment_saturation=1.4` (brightness/contrast default 1) | Saturation lerp on luminance after tonemap (Godot applies adjustment after tonemap, in display space) | Exact |
| Background | `background_mode=1` (Color, default black), `background_energy_multiplier=0.4` | Clear colour black (this is indoor/enclosed; sky not visible) (?) verify from screenshots | Exact |
| Ambient | `ambient_light_source=2` (Color) `Color(0.258824,0.203922,0.152941)`; menu: `(0.164706,0.184314,0.270588)` | Constant ambient term x albedo x AO, plus ambient specular from the proxy cubemap (below). Note Godot multiplies ambient by `ambient_light_energy` (1.0 default) | Exact for ambient only |
| Reflected light | `reflected_light_source=1` (Disabled) so reflections come from VoxelGI + probes | Substitute: one small **runtime-captured cubemap** (or per-room 3) rendered once at load from probe positions with the unlit/ambient+emissive pass; metals sample it with roughness-to-mip. Fallback: flat ambient colour cubemap | Approximation (R1) |
| VoxelGI | `subdiv=2`, size 227.9x97.6x234.1 at (-6.1,1.3,2.9), baked `giprobe_data.res`; env `sdfgi_energy=8` (not enabled) | **Not portable.** Approximate with (a) constant ambient, (b) a small **baked irradiance-probe grid** produced by an offline tool (CPU ray cast from emissive surfaces; 3D texture or a few thousand probes, 6-direction ambient cube per probe, trilinear sample in the shader) - phase 2, (c) point lights placed at emissive trims (`flat_emitter_*`, `trim_emission_lights`, `reactor_emitter`). Slice uses (a)+(c) | Approximation (R1, biggest) |
| ReflectionProbe x3 | hidden group, `box_projection` on 2 of 3, sizes ~75x100x166 / 72x100x129 / 78x100x74 | Same as "Reflected light" row; box projection optional in shader | Approximation |
| LightmapProbe x32 | present, but no LightmapGI node | Ignore (dead data) | n/a |
| Fog | `fog_enabled`, `fog_light_color=(0.4,0.25098,0.101961)`, `fog_density=0.002` (exponential) | In forward shader: `f = 1 - exp(-density * dist)`, lerp to fog colour (Godot fog is applied pre-tonemap in linear). Density is tiny: subtle haze | Exact |
| Volumetric fog | `volumetric_fog_enabled`, `density=0.001`, `gi_inject=0`; menu: FogVolume 16x4x16, FogMaterial density 0.002 emission (1,2,3), noise 3D | Level: ignore (0.001). Light shafts are authored meshes (`light_shafts.tres`, see shaders). Menu: optional cheap scrolling-noise billboard fog (menu is outside slice) | Dropped / cheap fake |
| Glow (level) | `glow_*` present but **not enabled**; values `levels/3=0 /4=1 /7=1`, `intensity=0.1`, `hdr_threshold=0.9`, `luminance_cap=3.0` | Implement bloom anyway behind a flag using those values (cheap, and Godot's `hdr_2d`/emissive FX rely on it if the real level did enable it - check screenshot). Default OFF for level | Exact when on |
| Glow (menu) | enabled, levels 1,4 = 1.0, 7 = 0.5, `glow_mix=0.153`, `blend_mode=4` (Mix), `hdr_threshold=0.36` | Same bloom code, menu parameters | Exact-ish |
| SSAO | `ssao_intensity=3.0`, `light_affect=0.59`, `ao_channel_affect=1.0` but **disabled** | Skip. Material AO (ORM red channel) still applies | n/a |
| SDFGI | disabled (`probe_ray_count=2` project value only) | Skip | n/a |
| Shadows | positional atlas 2048, quadrants 2 and 3 subdiv 2 (i.e. 4 slots each); no directional light | Spot-light shadow map 1024-2048 depth RT for forklift/menu only (phase 2). No CSM | Reduced |
| Lights | Bullet OmniLight: colour (0.125,0.627,1.0), energy 1.5 (animated), range 1.5, attenuation 2.0, `shadow_bias=0.03`, `blur=2`, bake_mode 0 (Disabled), vol-fog energy 15. Forklift SpotLight: colour (0.44,0.75,1.0), energy 12, projector, shadow. Robot OmniLight: colour (0.99,0.71,0.71), energy 0 -> animated, range 8.69, shadow, omni_shadow_mode 0 (dual paraboloid). Menu: SpotLight (shadow) + SpotLightBounce colour (1,0.655,0.051), energy 0.3, specular 0 | Forward shading with a fixed-size dynamic light array (e.g. 8 point/spot lights: nearest to object + always-on bullets). Godot attenuation: `omni_attenuation=2` -> use `pow(1-(d/range),2)`-style falloff, not 1/d^2 (verify against Godot `light_omni.glsl`: `pow(clamp(1-d/range,0,1), attenuation)`) | Good |
| Light projector | forklift `light_projector` texture | Spot cone multiply by texture (phase 3) | Phase 3 |
| Decals | Player: size (1.5,2,1.5), `albedo_mix=0.5`, `upper_fade=3.64`, `lower_fade=1`, texture `blob_shadow` radial gradient (black a=1 at centre, a=0 at edge, offsets 0/0.2/1). Robot: size (5,2,5) same params | **Not** full deferred decals (DesktopGL cannot sample the depth buffer). Use a ground-projected blob quad: ray-cast down to floor (physics), draw an alpha quad with the gradient, normal-aligned, multiplicative-ish alpha black. Distinct pass after opaque. Also covers shots/bullet-hit decals if present (none seen) | Good for blob use |
| Particles | GPUParticles3D: bullet blast/light/sparks/smoke (amount 1/1/10/250, lifetime 0.6/0.15/0.3/0.6, +LightParticle 40 @1.5 s with `collision_base_size`, InnerBlastLight 1 @0.2). CPUParticles3D: player muzzle (10 @0.3 s, 1 @0.1 s one_shot), robot (64 @0.4 s local, 200 @2 s explosiveness 0.95, 3 @1.5 s), forklift thrusters (4 x 20 @0.5 s, sphere r 0.1), core.tscn, impact/part_disappear. `cast_shadow=0` on all | CPU-simulated particle system, drawn as instanced billboards or mesh instances (BlastMesh.glb for the blast). Port Godot ParticleProcessMaterial fields to a small `ParticleSystemDef`. Sort/blend per material. GPUParticlesCollision (SDF 140x40x140, `level.GPUParticlesCollisionSDF3D_data.exr`): replace by ground-plane bounce or ray cast | Good |
| Standard PBR materials | See section 3 | `PbrLit.fx` | Good |
| Skeleton skinning | player.glb and red_robot.glb, SpringArm camera 2.4 m, camera far=300 | Skinned variant of PbrLit (technique per variant), bone matrices palette | Exact |
| Camera | `Camera3D` far=300, near/fov default (near 0.05, fov 75 vertical) (?) | Perspective, vertical FOV 75 deg, near 0.05, far 300 | Exact |

## 3. Materials actually used (counted from `.tres` in models/ model/ door/ forklift/ bullet)

- `StandardMaterial3D` dominant. Counts: albedo_texture 32, normal_enabled 32, ao_enabled 32 (**all from the packed ORM texture**, R=AO, G=roughness via `roughness_texture_channel=1`, B=metallic via `metallic_texture_channel=2`), metallic_texture 64 refs, `texture_filter=5` (linear mipmap anisotropic) on 38, emission_enabled 16 (`emission_energy_multiplier` set on 12; max 8.5 on `flat_emitter_powerful_white`, emission colour (0.741,0.678,0.584)), emission_texture 10, transparency 4, shading_mode 2 (unshaded), vertex_color_use_as_albedo 1.
- Typical: `metallic=0.5` scalar times ORM.b, `metallic=1.0` for `playerobot`. `flat_*` emitters: albedo (0.8), metallic 0.5, roughness 0.54, no textures.
- `tile_glass`: `transparency=1` (alpha blend), albedo (0.012,0.043,0.047,a=0.243), roughness 0.5, emission texture, depth-pre-pass not used.
- `playerobot`: `distance_fade_mode=3` (object dither), min 0.2 m, max 0.8 m: the player mesh fades out when the camera is close. Implement as screen-door dither discard keyed on camera distance (HLSL `clip` with 4x4 Bayer).
- Diffuse = Burley, specular = Schlick-GGX (Godot defaults; the particle shaders even state `diffuse_burley, specular_schlick_ggx`).
- No `heightmap`, `refraction`, `rim`, `clearcoat`, `subsurf`, `triplanar`, `billboard` in standard materials (grep). Good: material model is small.
- Godot StandardMaterial3D `specular=0.5` default -> F0 = 0.04 for dielectrics.

## 4. Custom shaders (`.gdshader`, 6 files + inline)

| Shader | Behaviour | HLSL plan |
|---|---|---|
| `laser/LaserShader.gdshader` | spatial, cull_disabled. Vertex: for UV.x>0.9 push VERTEX.z to `-clip`(4.0), rescale UV.x, scale XY by `traversal_scale`. Fragment: noise tex (uv*`uv_scale` (5,1)+`offset_uv_x`), `discard` if noise<`cutoff_value`(0.6), ALBEDO=EMISSION=`color1*energy` (color1 default (0,0,1)) | `LaserBeam.fx`, opaque pass, `clip()`; emission only so shading irrelevant. Uniforms mirror names |
| `laser/smoke.gdshader` | cull_disabled; `lifetime_percent = INSTANCE_CUSTOM.y`; alpha = smoothstep(0,1-life, clamp(tex.r*noise(uv+(0,life)) - life^2,0,1)*COLOR.a) | `ParticleSmoke.fx`, alpha blend; per-instance colour + life via vertex stream (instance buffer) |
| `laser/impact_effect/EmissionEmbers.gdshader` (15 lines) and `EmissionAlbedoParticle.gdshader` (32) | albedo*texture*COLOR, EMISSION = ALBEDO*`emission_energy` | `ParticleEmissive.fx` (additive-ish/alpha, output = colour*energy; HDR RT keeps >1) |
| `parts/ray.gdshader` | unshaded, cull_disabled, wobble `VERTEX.xy += 0.01*deform*(sin(z*1.4321), sin(z*2.12351))`, `dist=abs(z)` discard if >`clip`; colour mix(ray,smoke,`smoke`)*energy, alpha*`transparency` | `RayBeam.fx` alpha blend |
| `parts/sparks_effect/SparkShader.gdshader` | alpha from `sin((UV.y+life+id)*pi*3)*sign(sin(...*0.1))*COLOR.a*tex.r`, EMISSION = COLOR*`emission_factor`(3.0), `id=INSTANCE_ID` | `ParticleSpark.fx`; instance id from vertex stream or per-particle seed |
| `enemies/red_robot/smoke.gdshader` | **0 lines (empty file)** | ignore |
| `light_shafts.tres` (inline) | unshaded, `depth_draw_never`; alpha = pow(tex.r,`curve`=5.87)*colour.a(0.2235)*dp where dp=pow(max(0,dot(NORMAL,(0,0,0.1))),`dp_curve`=0.56)*smoothstep(-VERTEX.z, `max_dist`=2, `min_dist`=5) | `LightShaft.fx`. Note Godot `smoothstep(edge0,edge1,x)` argument order here is odd (x is the constant 5): result is a constant per the GLSL spec only if edge0<edge1; replicate expression literally and compare visually |
| `bullet.tscn` inline shader | `emission_energy=7.34`, `point_size`, burley, albedo tex*COLOR -> EMISSION = ALBEDO*energy | covered by `ParticleEmissive.fx` |
| `blob_shadow.tres` | GradientTexture2D radial | Generate a 128x128 radial alpha texture at build time (asset tool) or procedurally at runtime |

Godot -> HLSL mapping: `ALBEDO/EMISSION/ALPHA` -> returned colour; `COLOR` -> vertex colour stream; `INSTANCE_CUSTOM.y` (life 0..1) and `INSTANCE_ID` -> per-instance vertex stream; `UV`, `NORMAL`, `VERTEX` (local) -> VS output; `discard` -> `clip()`.

## 5. Shader list (proposed, `Core/Content/Shaders/`)

| File | Purpose | Slice |
|---|---|---|
| `Macros.fxh` | copy from RacingGame (`#if OPENGL` entry-point macros, SM3) | yes |
| `PbrLit.fx` | uber: techniques `Opaque`, `OpaqueSkinned`, `Alpha`, `AlphaSkinned`, `DepthOnly`. Inputs: albedo, ORM, normal (tangent frame), emission tex; params `AlbedoColor`, `Metallic`, `Roughness`, `EmissionColor`, `EmissionEnergy`, `AmbientColor`, fog, light array (8), cubemap | yes |
| `PostTonemap.fx` | AgX + exposure + saturation + dither; samples HDR RT + bloom RT | yes |
| `PostBloom.fx` | threshold(0.9, cap 3)/downsample/blur(levels 4 and 7)/upsample | yes (flag) |
| `LaserBeam.fx`, `RayBeam.fx`, `ParticleSmoke.fx`, `ParticleEmissive.fx`, `ParticleSpark.fx`, `LightShaft.fx` | FX above | one enemy: laser + particles |
| `BlobShadow.fx` (or `SpriteBatch`-like basic effect) | decal quads | yes |
| `DistanceFadeDither` | part of PbrLit (`#define DITHER_FADE`) | yes |
| `ShadowSpot.fx` | depth-only spot shadow | phase 3 |
| `ReflectionCapture` | no shader; reuse PbrLit in `Unlit/Ambient` technique to render cubemap faces | phase 2 |

## 6. DesktopGL / MGFX constraints (generalisable)

- Effects compile with MGFX to **GLSL via MojoShader**: use `vs_3_0/ps_3_0` (macros `#if OPENGL` -> `vs_3_0`). No SM4/5 features: no geometry/compute, no `SV_` semantics outside the macros, no structured buffers, no texture arrays, no `Load()`; sampler states are set from C# or declared in the fx with `sampler = sampler_state`.
- Max 16 texture units; PbrLit uses albedo, ORM, normal, emission, cubemap = 5; bloom up to 3.
- Interpolators: keep <= 8 `TEXCOORD`. Loops must be statically unrollable (`[unroll]`), no dynamic array index on SM3 for float4 arrays of unknown size; the light array uses a fixed count with a `LightCount` early-out via `if`.
- Skinning: bone palette `float4x3` or `float4x4` array. SM3 vertex constants: 256 float4 total in MojoShader -> max ~**72 bones as `float4x3`** (matches MonoGame SkinnedEffect). Check robot/player bone counts (Q4).
- **Effect parameter initial values are not applied on GL (all start at 0)** - set every parameter from C# (RacingGame fix, `project_racinggame_port_fixes.md`).
- **Output Z remap (-w..w on GL):** do not use `pos.z` for custom depth; use `pos.w` (linear) when writing/reading depth in shadow/fog code (RacingGame shadow bug root cause).
- HDR RT: `SurfaceFormat.HalfVector4` is supported on GL when the extension exists; MonoGame can throw on unsupported formats. Probe `GraphicsAdapter.IsProfileSupported`/try-create, fall back to `Rgba1010102`, then `Color` with pre-tonemap scale (RacingGame had an Rgba64 fallback in `RenderToTexture`). **MRT supported** (`SetRenderTargets`) but cannot read the depth buffer: write linear depth into a colour target if ever needed.
- Premultiplied alpha: MonoGame pipeline premultiplies textures by default; set `PremultiplyAlpha=false` for ORM/normal/emission (they carry data in channels) and correct blend states (alpha-write issues in the RacingGame glow fix).
- sRGB: pipeline texture processor does not reliably produce `ColorSRGB` on GL (?). Plan: keep textures `Color`, **decode albedo/emission with `pow(x,2.2)` in the shader** and tonemap to sRGB at the end (Q5).
- Normal maps: Godot/glTF = OpenGL convention (+Y up). Do **not** use DXT5nm for ORM; compress normal maps DXT5 without premultiply (RacingGame normal-map fix). DXT/BC textures need power-of-two and GL S3TC; check `.mgcb` `TextureFormat=DXTCompressed` on macOS works (it did in RacingGame).
- Mipmaps: `GenerateMipmaps=True`; aniso 4 via `SamplerState.Anisotropic` `MaxAnisotropy=4`.
- MGFX compile runs `mgfxc` through wine on macOS ... in current MonoGame (3.8.2+) it needs the **`dotnet-mgcb` + wine-free MojoShader path**; RacingGame already builds `.fx` on this machine, so reuse its `.mgcb` effect settings and tool manifest.

## 7. Render pass order (per frame)

1. (Load-time) capture ambient/reflection cubemap(s) from probe positions.
2. (Phase 3) spot shadow depth maps.
3. Opaque: level geometry, props, player (skinned), robot (skinned) to HDR RT + depth. Forward lighting: ambient + ambient-cube/probe + up to 8 lights + emission + fog + dither fade.
4. Alpha-tested FX: laser beam (clip).
5. Blob shadow quads (depth test on, depth write off).
6. Transparent sorted back to front: glass, light shafts, ray, particle smoke/spark/embers/blast (blend per material).
7. Bloom chain (if enabled) from HDR RT.
8. Tonemap (AgX, exposure 4.5, white 6, contrast 1.53) + saturation 1.4 + dither -> back buffer.
9. HUD/UI via SpriteBatch (owned by agent 5).

## 8. Skinned meshes, particles from the render side

- Skinning: bone matrices = `inverseBind * worldPose` computed in CPU (animation owned by agent 3). Vertex format: position, normal, tangent, uv0, 4 joint indices (Byte4), 4 weights (Vector4). Need a custom `VertexDeclaration` (RacingGame has a model processor in `PipelineExtension/RacingGameModelProcessor.cs`; extend it to carry tangents and joints). Both player and robot have a **shared per-mesh materials** problem: Godot `material_override`/ per-surface materials; carry a material id per mesh part.
- Skinned bounds: compute from bind pose + margin to avoid culling (Godot also does).
- Particles: CPU sim into a dynamic vertex buffer / instance buffer (`DynamicVertexBuffer`, hardware instancing needs `VertexBufferBinding` with `InstanceFrequency=1`, works on DesktopGL SM3 via MojoShader instancing? GL instancing is supported by MonoGame GL 3.0+). Fallback: write 4 verts per particle each frame (simple, ~600 particles in a blast is fine).

## 9. Vertical-slice subset (cost/fidelity order)

In: forward PbrLit with textures (albedo/ORM/normal/emission), constant ambient, fog, 8-light array (bullet light + emissive proxy lights), HDR RT + AgX tonemap + saturation, skinned player and robot, blob shadows, player-camera dither fade, laser beam, bullet + blast + spark + smoke particles, glass alpha, light shafts.
Out (later): irradiance-probe GI bake, reflection cubemap capture (use flat ambient cubemap first), spot shadows, projector, bloom (flag, trivial once chain exists), menu fog volume, forklift thrusters, all-level lighting polish, door emission animation.

## 10. Fidelity risks and open questions

Risks (ranked)
- **R1 baked VoxelGI + reflections (high):** level will look flat/dark or wrong versus Godot. Mitigation: ambient + emissive proxy lights first, probe grid bake later. Needs screenshot references.
- **R2 AgX exposure 4.5 (medium):** values from Godot energy units may not match; solve by porting Godot's tonemap literally and calibrating ambient/exposure against reference screenshots. Godot 4.7 changes to default light units (?) - check `Environment` and `Light3D` unit conversion (Godot `light_energy` is multiplied by pi for directional/omni internally in some versions).
- **R3 MGFX/GL compile limits (medium):** PbrLit with skinning + 8 lights + 5 textures may exceed ps_3_0 instruction or MojoShader register limits; split into variants/techniques (Q3).
- **R4 HDR RT format support on macOS GL (low-medium).**
- R5 particle parity (CPUParticles + GPUParticles curves/gradients) (low-medium): curves in `.tres` sub-resources need an extractor (agent 1).
- R6 lights embedded in `lights.glb` unknown; may be 0.

Open questions
- Q1 Does `lights.glb` contain `KHR_lights_punctual` nodes (count/colours/shadows)? (agent 1: inspect glTF JSON, no binary needed beyond header).
- Q2 Can the user run the Godot demo and supply reference screenshots (menu, level start, laser hit, light shafts) and the `Environment` after scene load? Needed for calibration; Godot editor headless could also export rendered frames.
- Q3 Is PbrLit one uber shader or multiple fx files per variant? Decide after first compile on this machine.
- Q4 Bone counts of `player.glb` and `red_robot.glb` (limit 72 on SM3?).
- Q5 sRGB texture decode in-shader vs `ColorSRGB` surface format on DesktopGL.
- Q6 Is `viewport/hdr_2d`/HDR output relevant? Plan: no.
- Q7 FOV and near plane of `Camera3D` in player (defaults assumed).
- Q8 Is the glow really off in the level, or enabled by a script (`grep glow_enabled` in `.gd`/`.cs`)? Check the game scripts (agent 3/4) for runtime environment changes (e.g. a pause or death effect).

## 11. Ordered task list (S <= 0.5 day, M ~ 1-2 days, L > 2 days)

1. S - Copy `Macros.fxh`, effect build settings and mgfx tool config from RacingGame; compile a trivial `Unlit.fx` on this machine to validate the toolchain.
2. M - `RenderTargets` helper with HDR fallback chain (HalfVector4 -> Rgba1010102 -> Rgba64 -> Color), resize handling, depth RT.
3. M - `PbrLit.fx` static opaque: albedo/ORM/normal/emission, Burley+GGX, ambient, 8 point lights, fog, set-all-parameters-from-C# rule. Material class loading the agent-1 manifest.
4. M - `PostTonemap.fx`: port Godot AgX (exposure 4.5, white 6, contrast 1.53), saturation 1.4, dither. Camera + test scene.
5. M - Skinned variant + vertex declaration + pipeline processor extension (with agent 3).
6. S - Player camera-near dither fade (`distance_fade` 0.2 to 0.8 m).
7. S - Blob shadow quads (ground ray cast hook from physics).
8. M - Alpha pass: sorted transparent queue, glass, light shafts shader.
9. M - Particle system (CPU sim, billboard/mesh) + `ParticleSmoke/Emissive/Spark` shaders + bullet blast prefab.
10. S - `LaserBeam.fx` + `RayBeam.fx`.
11. M - Emissive-proxy light extraction (scan emissive materials, cluster triangles into lights, JSON) in an offline tool; per-object nearest-8 selection.
12. S - Bloom (parameters above), behind flag.
13. M - Calibration pass vs Godot reference screenshots (ambient, exposure, fog, saturation).
14. L - Irradiance-probe grid bake tool (CPU ray cast, emissive lighting + ambient) and shader sampling. (phase 2)
15. M - Reflection cubemap capture at probe positions, roughness mips, optional box projection. (phase 2)
16. M - Spot shadow maps + light projector (forklift, menu). (phase 3)
17. S - Menu environment (ambient colour 0.16,0.18,0.27; glow mix 0.153; cheap fog billboard). (phase 3)

## 12. Lessons for the porting skill (godot-to-monogame-port)

- **Read the Environment resource field by field and check `*_enabled` flags**: a `.tres` stores parameters of disabled effects; only values that differ from defaults are written, so an absent `glow_enabled` means off.
- Absent light nodes + `VoxelGI`/`LightmapGI` means baked GI: decide early (ambient + proxy lights vs own bake). Binary GI data is unusable.
- Godot and MonoGame share handedness (right-handed, Y up, -Z forward), CCW front faces and top-left UV origin: **no axis flip** needed; flips only come from the glTF -> MonoGame importer step and from skeletons.
- Godot ORM packing (AO=R, rough=G, metal=B via `*_texture_channel` 0..4 = R,G,B,A,grey) must be preserved in the asset pipeline; channel indices are in the `.tres` (`metallic_texture_channel=2`).
- Godot `texture_filter=5` = linear mipmap anisotropic; decal/projector filter ints are enums in `project.godot`.
- Godot built-in tonemap/AgX source (MIT) is the reference; do not use a generic ACES substitute.
- Port `.gdshader` by mapping built-ins (ALBEDO/EMISSION/ALPHA/COLOR/INSTANCE_CUSTOM/INSTANCE_ID) to vertex streams; keep uniform names identical to the Godot parameters so `shader_parameter/*` values from `.tres` copy straight over.
- Empty `.gdshader` files exist; check `wc -l` before porting.
- Decals: DesktopGL cannot sample depth; replace deferred decals by projected quads where used as blob shadows.
- MonoGame GL gotchas inherited from RacingGame: parameters start at 0; use `.w` not `.z` for depth; alpha-channel writes in RTs; HDR format fallbacks; premultiply off for data textures.
- Take reference screenshots of the source before porting; calibrate ambient/exposure against them rather than by numbers alone.
