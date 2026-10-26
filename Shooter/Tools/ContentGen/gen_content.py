#!/usr/bin/env python3
"""
Shooter content generator.

Reads the Unity extractor's asset-manifest.json (+ the Unity project's .meta files)
and produces, for the STATIC assets (textures, sprites, audio, fonts, static models):

  * copies/converts the source files into Core/Content/<Textures|Audio|Fonts|Models>/...
  * Core/Content/Shooter.mgcb         (MonoGame content pipeline project)
  * Core/Content/AssetMap.json        (Unity asset path/guid -> content asset name + import metadata)

Skinned rigs (HoverBot/Turret + '@' animation FBX files) are NOT handled here; they belong to
Characters.mgcb. Their TEXTURES are handled here (they are listed in the manifest as textures).

Run (from the Shooter folder):
    python3 -I Tools/ContentGen/gen_content.py

Requires: ImageMagick ('magick') for non-PNG / mislabelled textures. Nothing else.
The Unity project is only ever read.
"""

import argparse
import importlib.util
import json
import os
import re
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SHOOTER = os.path.abspath(os.path.join(HERE, "..", ".."))
DEFAULT_UNITY = os.path.abspath(os.path.join(SHOOTER, "..", "FirstPersonShooter"))
DEFAULT_MANIFEST = os.path.join(SHOOTER, "Tools", "UnityExtractor", "out", "asset-manifest.json")
DEFAULT_OUT = os.path.join(SHOOTER, "Core", "Content")

def _load_local(name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, name + ".py"))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod

fbx_info = _load_local("fbx_info")  # python -I does not put the script dir on sys.path

# --------------------------------------------------------------------------------------
# Configuration
# --------------------------------------------------------------------------------------

# Rigged models are owned by Characters.mgcb / Shooter.Pipeline.
def is_skinned_model(unity_path):
    base = os.path.basename(unity_path)
    return "@" in base or base in ("HoverBot.fbx", "Turret.fbx")

# TMP font asset -> source TTF (relative to Unity root) + style-name used for the spritefont file.
# Roboto TTFs are not in Assets/ (only the pre-baked SDF atlases are); the same family ships in
# the com.unity.searcher package cache, so we take them from there (Apache 2.0).
FONT_SOURCES = {
    "LiberationSans": {
        "ttf": "Assets/TextMesh Pro/Fonts/LiberationSans.ttf",
        "license": "Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt",
        "licenseName": "SIL OFL 1.1",
        "tmpAssets": ["Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
                      "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"],
    },
    "Roboto-Black": {
        "ttf": "Library/PackageCache/com.unity.searcher@a463122f2c00/Editor/Resources/FlatSkin/Font/Roboto-Black.ttf",
        "license": "Assets/FPS/Art/Fonts/ApacheLicense2.0.txt",
        "licenseName": "Apache 2.0",
        "tmpAssets": ["Assets/FPS/Art/Fonts/Roboto-Black SDF.asset"],
    },
    "Roboto-Regular": {
        "ttf": "Library/PackageCache/com.unity.searcher@a463122f2c00/Editor/Resources/FlatSkin/Font/Roboto-Regular.ttf",
        "license": "Assets/FPS/Art/Fonts/ApacheLicense2.0.txt",
        "licenseName": "Apache 2.0",
        "tmpAssets": ["Assets/FPS/Art/Fonts/Roboto-Regular SDF.asset"],
    },
}

# Pixel sizes of the TMP text used in IntroMenu/MainScene/WinScene/LoseScene + prefabs
# (taken from the extractor JSON m_fontSize / auto-size max; see ASSET-CONVENTIONS.md).
FONT_SIZES = {
    "LiberationSans": [10, 14, 16, 26],
    "Roboto-Black": [7, 9, 10, 18, 22, 24, 25, 35, 45, 72],
    "Roboto-Regular": [12],
}

# Audio: which clips are streamed music (Song) vs. SoundEffect.
SONG_CLIPS = {"Assets/FPS/Audio/Music/Wind Ambience.mp3"}

# Per-clip runtime hints discovered from AudioSource components in the extractor JSON
# (looping + 2D/3D). Key = clip file basename. spatialBlend: 0 = 2D, 1 = fully 3D.
AUDIO_HINTS = {
    "Wind Ambience.mp3": {"loop": True, "spatialBlend": 0.0, "note": "GameManager AudioSource: playOnAwake, loop, 2D"},
    "Hoverbot_Movement.wav": {"loop": True, "spatialBlend": 1.0, "minDistance": 1, "maxDistance": 20, "volume": 0.75},
    "Jetpack_Use.wav": {"loop": True, "spatialBlend": 0.0, "volume": 1.0},
    "Turret_Death.wav": {"loop": False, "spatialBlend": 0.8, "minDistance": 1, "maxDistance": 500, "note": "VFX_Explosion"},
    "Hoverbot_Death.wav": {"loop": False, "spatialBlend": 0.8, "minDistance": 1, "maxDistance": 500, "note": "VFX_HoverBot_Explosion"},
}
# Clips that are played through script-driven AudioSources: enemy sources are 3D-ish (0.5), player/UI/weapon 2D.
def default_audio_hint(unity_path):
    if "/Enemies/" in unity_path:
        return {"loop": False, "spatialBlend": 0.5, "minDistance": 1, "maxDistance": 20}
    if "/Weapons/" in unity_path:
        return {"loop": False, "spatialBlend": 0.0}
    return {"loop": False, "spatialBlend": 0.0}

# ModelProcessor options shared by all static models. RotationY (=180, the Unity->MonoGame mesh conversion
# Q = mirrorX then mirrorZ) and Scale (Unity import scale) are added per model in process_models().
# GenerateTangentFrames=True gives Tangent0 + Binormal0 vertex channels. See Core/Content/ASSET-CONVENTIONS.md.
MODEL_PARAMS = [
    ("DefaultEffect", "BasicEffect"),
    ("GenerateTangentFrames", "True"),
    ("PremultiplyTextureAlpha", "False"),
    ("PremultiplyVertexColors", "False"),
    ("ResizeTexturesToPowerOfTwo", "False"),
    ("GenerateMipmaps", "True"),
    ("RotationX", "0"),
    ("RotationZ", "0"),
    ("SwapWindingOrder", "False"),
    ("TextureFormat", "Color"),
]

# --------------------------------------------------------------------------------------
# Helpers
# --------------------------------------------------------------------------------------

def read_meta(unity_root, unity_path):
    p = os.path.join(unity_root, unity_path + ".meta")
    return open(p, encoding="utf-8").read() if os.path.exists(p) else ""

def meta_val(text, key, default=None):
    m = re.search(r"^\s*" + re.escape(key) + r": (.+?)\s*$", text, re.M)
    return m.group(1) if m else default

def meta_int(text, key, default=None):
    v = meta_val(text, key)
    try:
        return int(float(v))
    except (TypeError, ValueError):
        return default

def magick_identify(path):
    try:
        out = subprocess.run(["magick", "identify", "-format", "%w %h %m %[channels] %z", path + ("[0]" if path.lower().endswith(".psd") else "")],
                             capture_output=True, text=True, check=True).stdout.split()
        return {"w": int(out[0]), "h": int(out[1]), "format": out[2], "channels": out[3], "depth": int(out[-1])}
    except Exception:
        return None

def ensure_dir(p):
    os.makedirs(os.path.dirname(p), exist_ok=True)

def copy_if_changed(src, dst):
    ensure_dir(dst)
    if os.path.exists(dst) and os.path.getsize(dst) == os.path.getsize(src):
        with open(src, "rb") as a, open(dst, "rb") as b:
            if a.read() == b.read():
                return
    shutil.copyfile(src, dst)

def mgcb_entry(lines, name, importer, processor, params, build_path=None):
    lines.append("#begin " + name)
    lines.append("/importer:" + importer)
    lines.append("/processor:" + processor)
    for k, v in params:
        lines.append("/processorParam:%s=%s" % (k, v))
    lines.append("/build:" + (build_path or name))
    lines.append("")

# --------------------------------------------------------------------------------------
# Textures
# --------------------------------------------------------------------------------------

def texture_role(name, tex_type, is_sprite):
    n = name.lower()
    if is_sprite:
        return "ui"
    if tex_type == 1 or n.endswith("_n") or "normal" in n:
        return "normal"
    if "metallic" in n or n.endswith("_ao") or "_ao" in n:
        return "data"
    if "emission" in n or "emissive" in n:
        return "emissive"
    if "albedo" in n:
        return "albedo"
    if "skybox" in n:
        return "skybox"
    return "vfx"

def process_textures(ctx):
    for a in ctx["assets"]:
        if a["type"] not in ("texture", "sprite"):
            continue
        up = a["path"]
        src = os.path.join(ctx["unity"], up)
        is_sprite = a["type"] == "sprite"
        # Collapse "Assets/FPS/Art/Textures/<Area>/<file>" -> Textures/<Area>/<file>.png
        rel = up.split("Art/Textures/", 1)[1] if "Art/Textures/" in up else os.path.basename(up)
        stem, ext = os.path.splitext(rel)
        name = "Textures/" + stem
        dst = os.path.join(ctx["out"], name + ".png")
        ident = magick_identify(src)
        if ident is None:
            raise SystemExit("cannot identify " + src)
        converted = None
        if ext.lower() == ".png" and ident["format"] == "PNG" and ident["depth"] <= 8:
            copy_if_changed(src, dst)
        else:
            # .psd/.tga, JPEG data inside a .png, or 16-bit PNG: flatten to 8-bit PNG.
            ensure_dir(dst)
            # keep RGB-only sources RGB-only so there is no pointless alpha channel
            fmt = "PNG32:" if ident["channels"].endswith("a") else "PNG24:"
            args = ["magick", src + ("[0]" if ext.lower() == ".psd" else ""), "-depth", "8", fmt + dst]
            subprocess.run(args, check=True)
            converted = "%s %s %dbit -> PNG 8bit" % (ident["format"], ident["channels"], ident["depth"])
        meta = read_meta(ctx["unity"], up)
        tex_type = meta_int(meta, "textureType", 0)
        srgb = meta_int(meta, "sRGBTexture", 1) == 1
        mip = meta_int(meta, "enableMipMap", 0) == 1
        plat = re.search(r"buildTarget: DefaultTexturePlatform\s+maxTextureSize: (\d+)", meta)
        max_size = int(plat.group(1)) if plat else meta_int(meta, "maxTextureSize", 2048)
        # TextureImporter alphaUsage: 0 none, 1 from input, 2 from gray scale. Unity then builds the alpha from the colour's brightness and ignores
        # any alpha in the file (the VFX sprites such as HealthPickUp.png are white-on-black RGB files that only become transparent this way).
        alpha_from_gray = meta_int(meta, "alphaUsage", 1) == 2
        if alpha_from_gray:
            subprocess.run(["magick", dst, "-alpha", "off", "(", "+clone", "-grayscale", "Rec709Luma", "-alpha", "off", ")",
                            "-compose", "CopyOpacity", "-composite", "PNG32:" + dst], check=True)
            converted = ((converted + "; ") if converted else "") + "alpha from gray scale (Unity alphaUsage 2)"
        out_w, out_h = ident["w"], ident["h"]
        if ctx["respect_max_size"] and not is_sprite and max(out_w, out_h) > max_size:
            # Unity's player build downsizes to the platform maxTextureSize; do the same so we render at
            # the resolution Unity actually showed (and save ~75% memory on the 1024/2048 sources).
            subprocess.run(["magick", dst, "-filter", "Lanczos", "-resize", "%dx%d>" % (max_size, max_size), dst], check=True)
            ident2 = magick_identify(dst)
            out_w, out_h = ident2["w"], ident2["h"]
            converted = ((converted + "; ") if converted else "") + "downscaled %dx%d -> %dx%d (Unity maxTextureSize %d)" % (ident["w"], ident["h"], out_w, out_h, max_size)
        wrap_u = {0: "repeat", 1: "clamp", 2: "mirror", 3: "mirrorOnce"}.get(meta_int(meta, "wrapU", 0), "repeat")
        wrap_v = {0: "repeat", 1: "clamp", 2: "mirror", 3: "mirrorOnce"}.get(meta_int(meta, "wrapV", 0), "repeat")
        filt = {0: "point", 1: "bilinear", 2: "trilinear"}.get(meta_int(meta, "filterMode", 1), "bilinear")
        role = texture_role(os.path.basename(stem), tex_type, is_sprite)
        generate_mips = mip and not is_sprite
        params = [
            ("ColorKeyColor", "255,0,255,255"),
            ("ColorKeyEnabled", "False"),            # MonoGame default is True: would punch out magenta pixels
            ("GenerateMipmaps", str(generate_mips)),
            ("MakeSquare", "False"),
            ("PremultiplyAlpha", "False"),           # Unity blends with straight alpha; alpha also carries smoothness
            ("ResizeToPowerOfTwo", "False"),
            ("TextureFormat", "Color"),
        ]
        mgcb_entry(ctx["mgcb"], name + ".png", "TextureImporter", "TextureProcessor", params)
        entry = {
            "unityPath": up, "guid": a["guid"], "name": name,
            "type": "Sprite" if is_sprite else "Texture2D",
            "role": role,
            "srgb": srgb,                                  # as authored in Unity (see ASSET-CONVENTIONS.md: data maps flagged sRGB in Unity too)
            "unityTextureType": {0: "default", 1: "normalMap", 8: "sprite", 10: "cookie"}.get(tex_type, str(tex_type)),
            "alphaIsTransparency": meta_int(meta, "alphaIsTransparency", 0) == 1,
            "hasAlpha": ident["channels"].endswith("a") or alpha_from_gray,
            "alphaFromGrayScale": alpha_from_gray,
            "wrapU": wrap_u, "wrapV": wrap_v, "filter": filt,
            "mipmaps": generate_mips,
            "width": out_w, "height": out_h, "sourceWidth": ident["w"], "sourceHeight": ident["h"],
            "unityMaxSize": max_size,
            "premultipliedAlpha": False,
            "source": rel,
        }
        if converted:
            entry["convertedFrom"] = converted
        if is_sprite:
            entry["pixelsPerUnit"] = meta_int(meta, "spritePixelsToUnits", 100)
            m = re.search(r"spriteBorder: \{x: ([\d.-]+), y: ([\d.-]+), z: ([\d.-]+), w: ([\d.-]+)\}", meta)
            entry["border"] = [float(x) for x in m.groups()] if m else [0, 0, 0, 0]
            m = re.search(r"spritePivot: \{x: ([\d.-]+), y: ([\d.-]+)\}", meta)
            entry["pivot"] = [float(x) for x in m.groups()] if m else [0.5, 0.5]
        ctx["map"][up] = entry

# --------------------------------------------------------------------------------------
# Audio
# --------------------------------------------------------------------------------------

def process_audio(ctx):
    for a in ctx["assets"]:
        if a["type"] != "audio":
            continue
        up = a["path"]
        src = os.path.join(ctx["unity"], up)
        rel = up.split("FPS/Audio/", 1)[1]
        stem, ext = os.path.splitext(rel)
        stem = stem.replace(" ", "_")
        name = "Audio/" + stem
        dst = os.path.join(ctx["out"], name + ext.lower())
        copy_if_changed(src, dst)
        is_mp3 = ext.lower() == ".mp3"
        is_song = up in SONG_CLIPS
        importer = "Mp3Importer" if is_mp3 else "WavImporter"
        processor = "SongProcessor" if is_song else "SoundEffectProcessor"
        mgcb_entry(ctx["mgcb"], name + ext.lower(), importer, processor, [("Quality", "Best")])
        meta = read_meta(ctx["unity"], up)
        hint = dict(AUDIO_HINTS.get(os.path.basename(up), default_audio_hint(up)))
        hint.setdefault("loop", False)
        ctx["map"][up] = {
            "unityPath": up, "guid": a["guid"], "name": name,
            "type": "Song" if is_song else "SoundEffect",
            "sourceFormat": ext.lower().lstrip("."),
            "unityLoadType": {0: "decompressOnLoad", 1: "compressedInMemory", 2: "streaming"}.get(meta_int(meta, "loadType", 0), "?"),
            "forceToMono": meta_int(meta, "forceToMono", 0) == 1,
            "unity3D": meta_int(meta, "3D", 1) == 1,
            "normalize": meta_int(meta, "normalize", 1) == 1,
            "playback": hint,
        }

# --------------------------------------------------------------------------------------
# Fonts
# --------------------------------------------------------------------------------------

SPRITEFONT = """<?xml version="1.0" encoding="utf-8"?>
<!-- Generated by Tools/ContentGen/gen_content.py : {family} {size}px ({lic}) -->
<XnaContent xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics">
  <Asset Type="Graphics:FontDescription">
    <FontName>{family}</FontName>
    <Size>{size}</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>Regular</Style>
    <DefaultCharacter>?</DefaultCharacter>
    <CharacterRegions>
      <CharacterRegion>
        <Start>&#32;</Start>
        <End>&#255;</End>
      </CharacterRegion>
      <CharacterRegion>
        <Start>&#8211;</Start>
        <End>&#8212;</End>
      </CharacterRegion>
      <CharacterRegion>
        <Start>&#8226;</Start>
        <End>&#8226;</End>
      </CharacterRegion>
    </CharacterRegions>
  </Asset>
</XnaContent>
"""

def process_fonts(ctx):
    for family, cfg in FONT_SOURCES.items():
        ttf = os.path.join(ctx["unity"], cfg["ttf"])
        if not os.path.exists(ttf):
            raise SystemExit("font source missing: " + ttf)
        copy_if_changed(ttf, os.path.join(ctx["out"], "Fonts", family + ".ttf"))
        lic = os.path.join(ctx["unity"], cfg["license"])
        lic_dst = os.path.join(ctx["out"], "Fonts", family + ".LICENSE.txt")
        if os.path.exists(lic):
            copy_if_changed(lic, lic_dst)
        for size in FONT_SIZES[family]:
            name = "Fonts/%s_%d" % (family, size)
            sf = os.path.join(ctx["out"], name + ".spritefont")
            ensure_dir(sf)
            open(sf, "w", encoding="utf-8", newline="\n").write(SPRITEFONT.format(family=family, size=size, lic=cfg["licenseName"]))
            mgcb_entry(ctx["mgcb"], name + ".spritefont", "FontDescriptionImporter", "FontDescriptionProcessor",
                       [("PremultiplyAlpha", "True"), ("TextureFormat", "Color")])
            ctx["map"]["font:%s:%d" % (family, size)] = {
                "unityPath": cfg["tmpAssets"][0], "guid": None, "name": name, "type": "SpriteFont",
                "family": family, "sizePx": size, "license": cfg["licenseName"], "source": cfg["ttf"],
            }

# --------------------------------------------------------------------------------------
# Models
# --------------------------------------------------------------------------------------

def process_models(ctx):
    for a in ctx["assets"]:
        if a["type"] != "model" or is_skinned_model(a["path"]):
            continue
        up = a["path"]
        src = os.path.join(ctx["unity"], up)
        if "Art/Models/" in up:
            rel = "Models/" + os.path.basename(up)
        elif "RootObjects" in up:
            rel = "Models/Level/" + os.path.basename(up)
        elif "Signs" in up:
            rel = "Models/Signs/" + os.path.basename(up)
        else:
            rel = "Models/" + os.path.basename(up)
        dst = os.path.join(ctx["out"], rel)
        copy_if_changed(src, dst)
        if up.endswith(".obj"):
            # keep sibling .mtl (Assimp reads it; material names come from it)
            txt = open(src, encoding="utf-8", errors="ignore").read()
            m = re.search(r"^mtllib\s+(.+)$", txt, re.M)
            if m:
                mtl_name = m.group(1).strip()
                mtl_src = os.path.join(os.path.dirname(src), mtl_name)
                if os.path.exists(mtl_src):
                    copy_if_changed(mtl_src, os.path.join(os.path.dirname(dst), mtl_name))
        meta = read_meta(ctx["unity"], up)
        global_scale = float(meta_val(meta, "globalScale", "1"))
        use_file_scale = meta_int(meta, "useFileScale", 1) == 1
        fbx = fbx_info.global_settings(src) if up.lower().endswith(".fbx") else None
        file_scale = 1.0
        if fbx and use_file_scale and "UnitScaleFactor" in fbx:
            file_scale = float(fbx["UnitScaleFactor"]) / 100.0     # FBX unit = UnitScaleFactor cm; Unity wants metres
        mesh_scale = global_scale * file_scale
        params = list(MODEL_PARAMS) + [("RotationY", "180"), ("Scale", repr(round(mesh_scale, 6)))]
        mgcb_entry(ctx["mgcb"], rel, "OpenAssetImporter", "StaticModelProcessor", params)
        # material names in Unity's import order + project-material remaps (externalObjects)
        mat_names = re.findall(r"first:\s+21: \d+\s+second: (.+)", meta)
        ext = {}
        for m in re.finditer(r"name: (\S+)\s+second: \{fileID: \d+, guid: ([0-9a-f]{32}), type: 2\}", meta):
            ext[m.group(1)] = m.group(2)
        entry = {
            "unityPath": up, "guid": a["guid"], "name": os.path.splitext(rel)[0], "type": "Model",
            "importer": "OpenAssetImporter", "processor": "StaticModelProcessor", "tangents": True,
            "bakedIntoVertices": {"mirrorX": True, "mirrorZ": True, "scale": mesh_scale,
                                  "note": "RotationY=180 (== x->-x then z->-z) and Scale baked into vertex data; IGNORE ModelBone transforms; use ModelMesh.Name == Unity mesh name; ModelMeshPart.Tag == source material name"},
            "unityGlobalScale": global_scale,
            "unityUseFileScale": use_file_scale,
            "fbxUnitScaleFactor": fbx.get("UnitScaleFactor") if fbx else None,
            "unityBakeAxisConversion": meta_int(meta, "bakeAxisConversion", 0) == 1,
            "unityWeldVertices": meta_int(meta, "weldVertices", 1) == 1,
            "materialNames": mat_names,
            "externalMaterialGuids": ext,
        }
        ctx["map"][up] = entry

# --------------------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--unity", default=DEFAULT_UNITY)
    ap.add_argument("--manifest", default=DEFAULT_MANIFEST)
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--full-res", action="store_true", help="do not downscale textures to Unity's maxTextureSize")
    args = ap.parse_args()

    manifest = json.load(open(args.manifest, encoding="utf-8"))
    header = [
        "",
        "#----------------------------- Global Properties ----------------------------#",
        "",
        "/outputDir:bin/$(Platform)",
        "/intermediateDir:obj/$(Platform)",
        "/platform:DesktopGL",
        "/config:$(Configuration)",
        "/profile:HiDef",
        "/compress:False",
        "",
        "#-------------------------------- References --------------------------------#",
        "",
        "/reference:../../Tools/Shooter.StaticPipeline/bin/Release/net8.0/Shooter.StaticPipeline.dll",
        "",
        "#---------------------------------- Content ---------------------------------#",
        "",
        "# GENERATED by Tools/ContentGen/gen_content.py - do not edit by hand, re-run the script.",
        "",
    ]
    ctx = {"unity": args.unity, "out": args.out, "assets": sorted(manifest["assets"], key=lambda x: x["path"]),
           "mgcb": [], "map": {}, "respect_max_size": not args.full_res}
    process_textures(ctx)
    process_audio(ctx)
    process_fonts(ctx)
    process_models(ctx)

    with open(os.path.join(args.out, "Shooter.mgcb"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(header + ctx["mgcb"]))

    guid_index = {v["guid"]: k for k, v in ctx["map"].items() if v.get("guid")}
    asset_map = {
        "version": 1,
        "contentRoot": "Content",
        "note": "assets keyed by Unity asset path (fonts keyed 'font:<family>:<px>'). 'name' is the ContentManager.Load name (no extension).",
        "unresolvedGuids": manifest.get("unresolvedGuids", []),
        "assets": ctx["map"],
        "guidIndex": guid_index,
    }
    with open(os.path.join(args.out, "AssetMap.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(asset_map, f, indent=2)
        f.write("\n")

    counts = {}
    for v in ctx["map"].values():
        counts[v["type"]] = counts.get(v["type"], 0) + 1
    print("generated:", json.dumps(counts))

if __name__ == "__main__":
    main()
