#!/usr/bin/env python3
"""Side-by-side comparison of Unity baseline renders and MonoGame renders.

Usage (run with `python3 -I`):
    python3 -I Tools/UnityReference/compare.py <monogameDir> <outDir> [unityDir]

For every Baseline/MainScene_<pose>.png that also exists in <monogameDir> writes
<outDir>/cmp_<pose>.png = Unity | MonoGame | abs-diff (each 1280x720 -> 3840x720) and prints the mean absolute
difference per pose (0..255 scale, RGB average). Imaging backend: ImageMagick (`magick` or `convert`), because
Pillow is not installed on the dev machine.
"""
import os
import shutil
import subprocess
import sys


def find_magick():
    for name in ("magick", "convert"):
        path = shutil.which(name)
        if path:
            return path
    sys.exit("ImageMagick (magick/convert) not found")


def run(cmd):
    return subprocess.run(cmd, check=True, capture_output=True, text=True).stdout.strip()


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    mono_dir = os.path.abspath(sys.argv[1])
    out_dir = os.path.abspath(sys.argv[2])
    here = os.path.dirname(os.path.abspath(__file__))
    unity_dir = os.path.abspath(sys.argv[3]) if len(sys.argv) > 3 else os.path.join(here, "Baseline")
    os.makedirs(out_dir, exist_ok=True)
    magick = find_magick()
    base = [magick]

    rows = []
    for fname in sorted(os.listdir(unity_dir)):
        if not (fname.startswith("MainScene_") and fname.endswith(".png")):
            continue
        mono = os.path.join(mono_dir, fname)
        if not os.path.exists(mono):
            continue
        unity = os.path.join(unity_dir, fname)
        pose = fname[len("MainScene_"):-4]
        out = os.path.join(out_dir, "cmp_" + pose + ".png")
        # Normalise both to 1280x720 RGB, build the diff, append horizontally.
        norm = ["-alpha", "off", "-resize", "1280x720!"]
        diff = [
            "(", unity, *norm, mono, *norm, "-compose", "difference", "-composite", ")",
        ]
        run(base + ["(", unity, *norm, ")", "(", mono, *norm, ")", *diff, "+append", out])
        mean = run(base + [unity, *norm, mono, *norm, "-compose", "difference", "-composite",
                           "-format", "%[fx:mean*255]", "info:"])
        rows.append((pose, float(mean)))
    if not rows:
        sys.exit("no matching MainScene_*.png in " + mono_dir)
    width = max(len(p) for p, _ in rows)
    for pose, mean in rows:
        print("%-*s  mean abs diff %.2f" % (width, pose, mean))
    print("%-*s  mean abs diff %.2f" % (width, "AVERAGE", sum(m for _, m in rows) / len(rows)))


if __name__ == "__main__":
    main()
