# ContentGen

Rewrites the generated texture section of `Core/Content/ThirdPersonShooter.mgcb` (between the `BEGIN/END GENERATED TEXTURES` markers) from `Tools/GodotExtract/out/AssetMap.json` + `Materials.json`. Deterministic, re-runnable.

    dotnet run --project Tools/ContentGen -- [--all] [--format Color|DxtCompressed] [--only a,b]

Default = textures referenced (albedo/normal/orm/emission) by the materials of structure, props, core, lights, flying_forklift (90 of 137 PNGs). `--all` = every PNG (.hdr/.exr skipped). Content name = `Textures/<AssetMap name>`. Processor: `GodotTextureProcessor` (PipelineExtension; MaxSize=2048 downscale, mipmaps, no sRGB conversion).

## Compression spike (macOS arm64, MonoGame 3.8.6.1 mgcb, door_albedo + door_normal 2048x2048)
| Format | .xnb total | mgcb time |
|---|---|---|
| Color | 43.7 MB | 2 s |
| DxtCompressed | 5.5 MB | 3.6 s (builds fine on arm64) |

DxtCompressed builds and loads in the DesktopGL runtime on macOS arm64 (verified by rendering). Chosen. Full level set (90 textures, DXT): Textures 149 MB of 232 MB Content; clean Release build of Desktop about 91 s.
