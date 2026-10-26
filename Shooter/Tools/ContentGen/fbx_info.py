"""Minimal binary-FBX header reader (no dependencies). Only used to read GlobalSettings
(UnitScaleFactor, axes) so Shooter content generation can compute Unity's import scale."""
import struct
import zlib


def _parse(path):
    d = open(path, "rb").read()
    if d[:20] != b"Kaydara FBX Binary  ":
        return None
    ver = struct.unpack("<I", d[23:27])[0]
    big = ver >= 7500

    def node(pos):
        if big:
            end, np_, _pl, nl = struct.unpack("<QQQB", d[pos:pos + 25]); hdr = 25
        else:
            end, np_, _pl, nl = struct.unpack("<IIIB", d[pos:pos + 13]); hdr = 13
        if end == 0:
            return None, pos + hdr
        name = d[pos + hdr:pos + hdr + nl].decode("latin1")
        p = pos + hdr + nl
        props = []
        for _ in range(np_):
            t = chr(d[p]); p += 1
            if t in "CYIFDL":
                fmt = {"C": "<b", "Y": "<h", "I": "<i", "F": "<f", "D": "<d", "L": "<q"}[t]
                props.append(struct.unpack(fmt, d[p:p + struct.calcsize(fmt)])[0]); p += struct.calcsize(fmt)
            elif t in "fdilb":
                n, enc, cl = struct.unpack("<III", d[p:p + 12]); p += 12
                p += cl  # arrays are not needed here
                props.append(None)
            elif t in "SR":
                l = struct.unpack("<I", d[p:p + 4])[0]; p += 4
                props.append(d[p:p + l]); p += l
        children = []
        while p < end:
            c, p = node(p)
            if c is None:
                break
            children.append(c)
        return (name, props, children), end

    pos = 27
    nodes = []
    while pos < len(d) - 200:
        n, pos = node(pos)
        if n is None:
            break
        nodes.append(n)
    return nodes


def global_settings(path):
    """Returns dict of GlobalSettings properties (UnitScaleFactor, UpAxis, ...) or None if not binary FBX."""
    nodes = _parse(path)
    if nodes is None:
        return None
    for n in nodes:
        if n[0] == "GlobalSettings":
            for c in n[2]:
                if c[0] == "Properties70":
                    return {p[1][0].decode(): p[1][4] for p in c[2] if len(p[1]) > 4}
    return None
