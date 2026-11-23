# Night sky credit

`Core/Content/Textures/NightSkyCubeMap.dds` is converted from **"Rogland Clear Night"** by **Greg Zaal**, published on [Poly Haven](https://polyhaven.com/a/rogland_clear_night) under **CC0** (public domain; no attribution required, given here anyway).

- Source file used: the tonemapped JPG, 8192x4096, md5 `5565a41460b4f851fcea727d5dcfc009` (matches the checksum in Poly Haven's file API on 2026-11-22).
- Converted with `scripts/make-night-sky-cube.py` (equirectangular to six 1024x1024 cube faces, DXT1, no mipmaps, same layout as `SkyCubeMap.dds`). The script reproduces the shipped file byte for byte.
- Only the stars and Milky Way are used: the sky shader (`PreScreenSkyCubeMapping.fx`) adds the cube on top of the darkened day sky, masked to open blue sky and faded out near the horizon, so the photo's own landscape never shows.
