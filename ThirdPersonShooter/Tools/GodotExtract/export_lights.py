# Exports KHR_lights_punctual from lights.glb (world position/direction/colour/cone) to out/lights.json.
import json, struct, sys, math
src = 'tps-demo-master/level/geometry/models/lights.glb'
b = open(src, 'rb').read()
n = struct.unpack('<I', b[12:16])[0]
j = json.loads(b[20:20+n])
L = j['extensions']['KHR_lights_punctual']['lights']
nodes = j['nodes']
parent = {}
for i, nd in enumerate(nodes):
    for c in nd.get('children', []): parent[c] = i
def mat(nd):
    if 'matrix' in nd:
        m = nd['matrix']; return [[m[c*4+r] for c in range(4)] for r in range(4)]
    t = nd.get('translation', [0,0,0]); q = nd.get('rotation', [0,0,0,1]); s = nd.get('scale', [1,1,1])
    x,y,z,w = q
    R = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
    return [[R[r][c]*s[c] for c in range(3)]+[t[r]] for r in range(3)]+[[0,0,0,1]]
def mul(a, b): return [[sum(a[r][k]*b[k][c] for k in range(4)) for c in range(4)] for r in range(4)]
def world(i):
    m = mat(nodes[i])
    while i in parent:
        i = parent[i]; m = mul(mat(nodes[i]), m)
    return m
out = []
for i, nd in enumerate(nodes):
    e = nd.get('extensions', {}).get('KHR_lights_punctual')
    if not e: continue
    l = L[e['light']]; m = world(i)
    d = [-m[0][2], -m[1][2], -m[2][2]]  # glTF lights point down local -Z
    o = {'name': nd.get('name'), 'type': l['type'], 'color': l['color'], 'intensity': l['intensity'], 'pos': [m[0][3], m[1][3], m[2][3]], 'dir': d}
    if 'spot' in l: o['inner'] = l['spot']['innerConeAngle']; o['outer'] = l['spot']['outerConeAngle']
    out.append(o)
json.dump(out, open('Tools/GodotExtract/out/lights.json', 'w'), indent=1)
for o in out: print(o['name'], o['type'], [round(v,1) for v in o['pos']], [round(v,2) for v in o['dir']])
