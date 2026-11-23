#!/usr/bin/env python3
"""Build Core/Content/Textures/NightSkyCubeMap.dds (6 faces of 1024, DXT1, no mips, same
layout as SkyCubeMap.dds) from an equirectangular night sky photo.

Source used: Poly Haven "Rogland Clear Night" by Greg Zaal (CC0), tonemapped JPG:
https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/rogland_clear_night.jpg
(md5 5565a41460b4f851fcea727d5dcfc009). See Documentation/Credits-Night-Sky.md.

Needs: ffmpeg, ImageMagick (magick), python3 with numpy. Usage:
  python3 scripts/make-night-sky-cube.py rogland_clear_night.jpg Core/Content/Textures/NightSkyCubeMap.dds [yaw_degrees]
Face directions use the D3D/GL cube convention (+X -X +Y -Y +Z -Z, Y up), matching the game's
sky shader, which samples with (x, z, y) because the game's up axis is Z.
"""
import math, os, struct, subprocess, sys, tempfile
import numpy as np

W, H, N = 4096, 2048, 1024
FACES = {
    'px': lambda u, v: (np.ones_like(u), -v, -u),
    'nx': lambda u, v: (-np.ones_like(u), -v, u),
    'py': lambda u, v: (u, np.ones_like(u), v),
    'ny': lambda u, v: (u, -np.ones_like(u), -v),
    'pz': lambda u, v: (u, -v, np.ones_like(u)),
    'nz': lambda u, v: (-u, -v, -np.ones_like(u)),
}

def main(src, out, yaw_deg=0.0):
    yaw = math.radians(yaw_deg)
    with tempfile.TemporaryDirectory() as tmp:
        raw = os.path.join(tmp, 'eq.rgb')
        subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-i', src, '-vf', f'scale={W}:{H}:flags=lanczos',
                        '-f', 'rawvideo', '-pix_fmt', 'rgb24', raw], check=True)
        img = np.fromfile(raw, dtype=np.uint8).reshape(H, W, 3).astype(np.float32)
        # Edge texels sit exactly on the face borders (not half a texel inside), so adjacent faces share
        # identical edge values: GL/MonoGame filter cube maps per face without seamless filtering, and
        # otherwise a thin line shows along the cube edges.
        t = np.linspace(-1.0, 1.0, N, dtype=np.float32)
        u, v = np.meshgrid(t, t)
        body = b''
        for name in ['px', 'nx', 'py', 'ny', 'pz', 'nz']:
            x, y, z = FACES[name](u, v)
            n = np.sqrt(x * x + y * y + z * z); x, y, z = x / n, y / n, z / n
            lon = np.arctan2(x, z) + yaw
            lat = np.arcsin(np.clip(y, -1, 1))
            px = ((lon / (2 * math.pi) + 0.5) % 1.0) * W - 0.5
            py = (0.5 - lat / math.pi) * H - 0.5
            x0 = np.floor(px).astype(int); y0 = np.clip(np.floor(py).astype(int), 0, H - 2)
            fx = (px - x0)[..., None]; fy = (py - y0)[..., None]
            x0 %= W; x1 = (x0 + 1) % W
            c = (img[y0, x0] * (1 - fx) * (1 - fy) + img[y0, x1] * fx * (1 - fy) +
                 img[y0 + 1, x0] * (1 - fx) * fy + img[y0 + 1, x1] * fx * fy)
            rgb = os.path.join(tmp, name + '.rgb'); np.clip(c, 0, 255).astype(np.uint8).tofile(rgb)
            png = os.path.join(tmp, name + '.png'); dds = os.path.join(tmp, name + '.dds')
            subprocess.run(['magick', '-size', f'{N}x{N}', '-depth', '8', 'rgb:' + rgb, png], check=True)
            subprocess.run(['magick', png, '-define', 'dds:compression=dxt1', '-define', 'dds:mipmaps=0', dds], check=True)
            d = open(dds, 'rb').read(); assert len(d) - 128 == N * N // 2
            body += d[128:]; hdr = d[:128]
        hdr = bytearray(hdr)
        # DDSD_CAPS|HEIGHT|WIDTH|PIXELFORMAT|LINEARSIZE, DXT1, no mipmaps, cube map with all six faces
        struct.pack_into('<I', hdr, 8, 0x81007); struct.pack_into('<I', hdr, 28, 0)
        hdr[32:76] = bytes(44)  # reserved (ImageMagick writes its signature here)
        struct.pack_into('<I', hdr, 108, 0x1008); struct.pack_into('<I', hdr, 112, 0xFE00)
        open(out, 'wb').write(bytes(hdr) + body)
    print('wrote', out)

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], float(sys.argv[3]) if len(sys.argv) > 3 else 0.0)
