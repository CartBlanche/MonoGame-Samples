#!/usr/bin/env python3
"""
Bakes the RAW Unity ParticleSystem / ParticleSystemRenderer properties kept by Tools/UnityExtractor into compact JSON files that the
runtime loader (Core/Rendering/Particles/ParticleSystemData.cs, ParticleSystemData.Load) reads:

    Core/Content/Particles/MainScene.particles.json      the 22 systems placed in MainScene (world matrices in MonoGame space)
    Core/Content/Particles/<Prefab>.particles.json       every VFX / enemy / weapon prefab that contains ParticleSystems
                                                         (world matrices are the prefab's own world transform; re-parent at spawn time)

Format: {"source": ..., "systems": [{"name", "path", "world": [16 floats, row-major, MonoGame space], "ps": {...}, "renderer": {...}}]}
Disabled modules are replaced by {"enabled": 0} to keep the files small. Values are left exactly as Unity serialised them.

Usage (from the Shooter folder):  python3 -I Tools/ParticleBake/bake_particles.py
"""
import glob
import json
import math
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT_DIR = os.path.join(ROOT, 'Core', 'Content', 'Particles')
EXTRACT = os.path.join(ROOT, 'Tools', 'UnityExtractor', 'out')

KEEP_ALWAYS = ('InitialModule', 'ShapeModule', 'EmissionModule')
RENDERER_KEYS = ('m_Enabled', 'm_Materials', 'm_RenderMode', 'm_SortMode', 'm_MinParticleSize', 'm_MaxParticleSize', 'm_CameraVelocityScale',
                 'm_VelocityScale', 'm_LengthScale', 'm_SortingFudge', 'm_NormalDirection', 'm_RenderAlignment', 'm_Pivot', 'm_Flip',
                 'm_AllowRoll', 'm_Mesh', 'm_Mesh1', 'm_Mesh2', 'm_Mesh3', 'm_RendererPriority', 'm_ApplyActiveColorSpace',
                 'm_MaskInteraction', 'm_SortingLayerID', 'm_SortingOrder')


def mg_world(tr):
    """Unity world TRS -> MonoGame row-major matrix (position (x,y,-z), quaternion (-x,-y,z,w))."""
    p = tr.get('worldPosition', [0, 0, 0])
    s = tr.get('worldScale', [1, 1, 1])
    q = tr.get('worldRotation', [0, 0, 0, 1])
    x, y, z, w = -q[0], -q[1], q[2], q[3]
    r = [
        [1 - 2 * (y * y + z * z), 2 * (x * y + z * w), 2 * (x * z - y * w)],
        [2 * (x * y - z * w), 1 - 2 * (x * x + z * z), 2 * (y * z + x * w)],
        [2 * (x * z + y * w), 2 * (y * z - x * w), 1 - 2 * (x * x + y * y)],
    ]
    m = []
    for i in range(3):
        m += [r[i][0] * s[i], r[i][1] * s[i], r[i][2] * s[i], 0.0]
    m += [p[0], p[1], -p[2], 1.0]
    return [round(v, 6) for v in m]


def trim_ps(props):
    out = {}
    for k, v in props.items():
        if isinstance(v, dict) and 'enabled' in v and not v.get('enabled') and k not in KEEP_ALWAYS:
            out[k] = {'enabled': 0}
        else:
            out[k] = v
    return out


def collect(go, path, out):
    name = go.get('name', '')
    here = path + '/' + name
    ps = rend = None
    for c in go.get('components', []):
        if c.get('type') == 'ParticleSystem':
            ps = c['props']
        elif c.get('type') == 'ParticleSystemRenderer':
            rend = c['props']
    if ps is not None:
        out.append({
            'name': name,
            'path': here,
            'active': go.get('activeInHierarchy', True),
            'world': mg_world(go.get('transform', {})),
            'localScale': go.get('transform', {}).get('localScale', [1, 1, 1]),
            'ps': trim_ps(ps),
            'renderer': {k: v for k, v in (rend or {}).items() if k in RENDERER_KEYS},
        })
    for ch in go.get('children', []):
        collect(ch, here, out)


def bake(src, dst, label):
    with open(src) as f:
        doc = json.load(f)
    systems = []
    for r in doc.get('roots', []):
        collect(r, '', systems)
    if not systems:
        return 0
    with open(dst, 'w') as f:
        json.dump({'source': label, 'systems': systems}, f, separators=(',', ':'))
    return len(systems)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    total = 0
    n = bake(os.path.join(EXTRACT, 'Scenes', 'MainScene.json'), os.path.join(OUT_DIR, 'MainScene.particles.json'), 'Scenes/MainScene')
    print('MainScene: %d systems' % n)
    total += n
    for path in sorted(glob.glob(os.path.join(EXTRACT, 'Prefabs', '*.json'))):
        base = os.path.splitext(os.path.basename(path))[0]
        if base == 'index':
            continue
        n = bake(path, os.path.join(OUT_DIR, base + '.particles.json'), 'Prefabs/' + base)
        if n:
            print('%s: %d systems' % (base, n))
            total += n
    print('total systems baked: %d' % total)


if __name__ == '__main__':
    main()
