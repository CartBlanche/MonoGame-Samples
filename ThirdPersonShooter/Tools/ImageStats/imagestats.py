#!/usr/bin/env python3
"""Image statistics for lighting comparison. stdlib only. Usage: python3 -I imagestats.py a.png [b.png ...]
Prints mean RGB, mean luma (Rec.709 on sRGB values, 0..1), luma std, mean HSV saturation, hue of mean colour, 4x4 luma grid.
Supports 8-bit RGB/RGBA non-interlaced PNG. Pass --crop to ignore the top-left 300x130 px debug overlay of the Godot frames."""
import sys, zlib, struct, colorsys

def load(path):
    d = open(path, 'rb').read()
    assert d[:8] == b'\x89PNG\r\n\x1a\n', 'not a PNG'
    p = 8; idat = b''; w = h = ct = None
    while p < len(d):
        n, t = struct.unpack('>I4s', d[p:p + 8]); body = d[p + 8:p + 8 + n]; p += 12 + n
        if t == b'IHDR':
            w, h, bd, ct, _, _, il = struct.unpack('>IIBBBBB', body)
            assert bd == 8 and ct in (2, 6) and il == 0, 'need 8-bit RGB/RGBA non-interlaced'
        elif t == b'IDAT': idat += body
    bpp = 3 if ct == 2 else 4; stride = w * bpp
    raw = zlib.decompress(idat); out = []; prev = bytearray(stride); o = 0
    for _ in range(h):
        f = raw[o]; line = bytearray(raw[o + 1:o + 1 + stride]); o += 1 + stride
        for i in range(stride):
            a = line[i - bpp] if i >= bpp else 0; b = prev[i]; c = prev[i - bpp] if i >= bpp else 0
            if f == 1: line[i] = (line[i] + a) & 255
            elif f == 2: line[i] = (line[i] + b) & 255
            elif f == 3: line[i] = (line[i] + ((a + b) >> 1)) & 255
            elif f == 4:
                pa = abs(b - c); pb = abs(a - c); pc = abs(a + b - 2 * c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 255
        out.append(line); prev = line
    return w, h, bpp, out

def stats(path, crop=False, ref=None):
    w, h, bpp, rows = load(path)
    n = 0; sr = sg = sb = sl = sl2 = ss = 0.0
    grid = [[0.0, 0] for _ in range(16)]
    cg = [[0.0, 0.0, 0.0, 0.0, 0.0, 0] for _ in range(16)]  # R,G,B,sat,hue-vector-x,count (per cell)
    for y in range(0, h, 2):
        r = rows[y]
        for x in range(0, w, 2):
            if crop and x < 0.15 * w and y < 0.12 * h: continue
            i = x * bpp; R, G, B = r[i] / 255, r[i + 1] / 255, r[i + 2] / 255
            L = 0.2126 * R + 0.7152 * G + 0.0722 * B
            mx = max(R, G, B); mn = min(R, G, B)
            sr += R; sg += G; sb += B; sl += L; sl2 += L * L; ss += (mx - mn) / mx if mx > 0 else 0; n += 1
            ci = (y * 4 // h) * 4 + (x * 4 // w)
            g = grid[ci]; g[0] += L; g[1] += 1
            c = cg[ci]; c[0] += R; c[1] += G; c[2] += B; c[3] += (mx - mn) / mx if mx > 0 else 0; c[5] += 1
    ml = sl / n; sd = (sl2 / n - ml * ml) ** 0.5
    hue = colorsys.rgb_to_hsv(sr / n, sg / n, sb / n)[0] * 360
    print(f'{path}\n  mean RGB {sr/n:.3f} {sg/n:.3f} {sb/n:.3f}  luma {ml:.3f}  luma_std {sd:.3f}  sat {ss/n:.3f}  avg_hue {hue:.0f}deg')
    lg = [grid[i][0] / max(1, grid[i][1]) for i in range(16)]
    for r in range(4): print('  ' + ' '.join(f'{lg[r*4+c]:.3f}' for c in range(4)))
    print('  colour grid (mean R,G,B / sat per cell):')
    for r in range(4): print('  ' + '  '.join('%.2f,%.2f,%.2f/%.2f' % tuple([cg[r*4+c][k] / max(1, cg[r*4+c][5]) for k in (0, 1, 2, 3)]) for c in range(4)))
    cg2 = [[cg[i][k] / max(1, cg[i][5]) for k in range(4)] for i in range(16)]
    if ref is not None:
        rl, rc = ref
        print(f'  grid luma MAE vs ref {sum(abs(a - b) for a, b in zip(lg, rl)) / 16:.4f}   grid RGB MAE vs ref {sum(abs(cg2[i][k] - rc[i][k]) for i in range(16) for k in range(3)) / 48:.4f}   grid sat MAE {sum(abs(cg2[i][3] - rc[i][3]) for i in range(16)) / 16:.4f}')
    return lg, cg2

if __name__ == '__main__':
    # --ref <png>: print grid MAE vs that reference (cropped like the other frames)
    argv = sys.argv[1:]; ref = None
    if '--ref' in argv:
        i = argv.index('--ref'); ref = stats(argv[i + 1], True); del argv[i:i + 2]
    args = [a for a in argv if a != '--crop']
    for a in args: stats(a, '--crop' in sys.argv, ref)
