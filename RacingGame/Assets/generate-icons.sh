#!/bin/zsh
# Racing Game icon generator
#
# Makes the icons of every platform from one source picture, Assets/Icon-1024.png
# (square, 1024 x 1024, transparent background). Adapted from the SolidOps icon scripts.
#
# Usage:   ./Assets/generate-icons.sh        (from anywhere; macOS: needs sips and iconutil;
#                                              ImageMagick (brew install imagemagick) makes
#                                              proper multi-size .ico files)
#
# Writes:
#   Desktop/Icon.ico, Desktop/Icon.png, Desktop/Icon.bmp, Desktop/RacingGame.icns   (macOS and Linux desktop)
#   Windows/Icon.ico
#   Assets/RacingGame.ico, Assets/RacingGameThumbnail.png
#   Android/Resources/mipmap-*/ic_launcher.png, Android/Resources/Drawable/icon.png
#   iOS/GameThumbnail.png, iOS/Assets.xcassets/AppIcon.appiconset/ (icons + Contents.json)

set -e

ASSETS="${0:A:h}"
ROOT="${ASSETS:h}"
SOURCE="$ASSETS/Icon-1024.png"

if [[ ! -f "$SOURCE" ]]; then
  echo "Error: $SOURCE not found"
  exit 1
fi

# resize SOURCE to a square of the given size
resize() {  # size output
  mkdir -p "${2:h}"
  sips -z "$1" "$1" "$SOURCE" --out "$2" > /dev/null
  echo "  ✓ ${2#$ROOT/} ($1 x $1)"
}

# a multi-size .ico (256 down to 16) from SOURCE
make_ico() {  # output
  mkdir -p "${1:h}"
  if command -v magick > /dev/null; then
    magick "$SOURCE" -define icon:auto-resize=256,128,64,48,32,16 "$1"
    echo "  ✓ ${1#$ROOT/} (multi-size)"
  else
    sips -s format ico "$SOURCE" --out "$1" > /dev/null
    echo "  ✓ ${1#$ROOT/} (single size; install ImageMagick for a multi-size .ico)"
  fi
}

echo "========================================="
echo "Racing Game icon generator"
echo "Source: ${SOURCE#$ROOT/}"
echo "========================================="

echo ""
echo "[1/5] Windows and desktop"
make_ico "$ROOT/Desktop/Icon.ico"
make_ico "$ROOT/Windows/Icon.ico"
make_ico "$ASSETS/RacingGame.ico"
resize 256 "$ROOT/Desktop/Icon.png"
# Icon.bmp is embedded in the Desktop program: MonoGame uses it for the window and Dock icon.
# A 32-bit BMP keeps the transparency (a plain sips BMP would not).
if command -v magick > /dev/null; then
  magick "$SOURCE" -resize 256x256 -define bmp:format=bmp4 "$ROOT/Desktop/Icon.bmp"
else
  sips -z 256 256 "$SOURCE" --out "$ROOT/Desktop/Icon.bmp" -s format bmp > /dev/null
fi
echo "  ✓ Desktop/Icon.bmp (256 x 256)"
resize 128 "$ASSETS/RacingGameThumbnail.png"

echo ""
echo "[2/5] macOS (.icns)"
ICONSET="$(mktemp -d)/RacingGame.iconset"
mkdir -p "$ICONSET"
for size in 16 32 128 256 512; do
  sips -z $size $size "$SOURCE" --out "$ICONSET/icon_${size}x${size}.png" > /dev/null
  sips -z $((size * 2)) $((size * 2)) "$SOURCE" --out "$ICONSET/icon_${size}x${size}@2x.png" > /dev/null
done
iconutil -c icns "$ICONSET" -o "$ROOT/Desktop/RacingGame.icns"
rm -rf "${ICONSET:h}"
echo "  ✓ Desktop/RacingGame.icns"

echo ""
echo "[3/5] Android"
for entry in mdpi:48 hdpi:72 xhdpi:96 xxhdpi:144 xxxhdpi:192; do
  resize "${entry#*:}" "$ROOT/Android/Resources/mipmap-${entry%%:*}/ic_launcher.png"
done
# the manifest refers to @drawable/icon
resize 192 "$ROOT/Android/Resources/Drawable/icon.png"

echo ""
echo "[4/5] iOS"
# iOS icons must not have transparency (the App Store rejects them): put the picture on black
FLAT="$(mktemp -d)/Icon-flat.png"
if command -v magick > /dev/null; then
  magick "$SOURCE" -background black -alpha remove -alpha off "$FLAT"
else
  cp "$SOURCE" "$FLAT"
  echo "  ⚠ ImageMagick not found: the iOS icons keep their transparency"
fi
SOURCE="$FLAT"
resize 180 "$ROOT/iOS/GameThumbnail.png"
XCASSETS="$ROOT/iOS/Assets.xcassets/AppIcon.appiconset"
rm -rf "$XCASSETS"
mkdir -p "$XCASSETS"
for size in 20 29 40 58 60 76 80 87 120 152 167 180 1024; do
  sips -z $size $size "$SOURCE" --out "$XCASSETS/icon_${size}x${size}.png" > /dev/null
done
cat > "$XCASSETS/Contents.json" <<JSON
{
  "images" : [
    { "filename" : "icon_40x40.png",   "idiom" : "iphone", "scale" : "2x", "size" : "20x20" },
    { "filename" : "icon_60x60.png",   "idiom" : "iphone", "scale" : "3x", "size" : "20x20" },
    { "filename" : "icon_58x58.png",   "idiom" : "iphone", "scale" : "2x", "size" : "29x29" },
    { "filename" : "icon_87x87.png",   "idiom" : "iphone", "scale" : "3x", "size" : "29x29" },
    { "filename" : "icon_80x80.png",   "idiom" : "iphone", "scale" : "2x", "size" : "40x40" },
    { "filename" : "icon_120x120.png", "idiom" : "iphone", "scale" : "3x", "size" : "40x40" },
    { "filename" : "icon_120x120.png", "idiom" : "iphone", "scale" : "2x", "size" : "60x60" },
    { "filename" : "icon_180x180.png", "idiom" : "iphone", "scale" : "3x", "size" : "60x60" },
    { "filename" : "icon_20x20.png",   "idiom" : "ipad",   "scale" : "1x", "size" : "20x20" },
    { "filename" : "icon_40x40.png",   "idiom" : "ipad",   "scale" : "2x", "size" : "20x20" },
    { "filename" : "icon_29x29.png",   "idiom" : "ipad",   "scale" : "1x", "size" : "29x29" },
    { "filename" : "icon_58x58.png",   "idiom" : "ipad",   "scale" : "2x", "size" : "29x29" },
    { "filename" : "icon_40x40.png",   "idiom" : "ipad",   "scale" : "1x", "size" : "40x40" },
    { "filename" : "icon_80x80.png",   "idiom" : "ipad",   "scale" : "2x", "size" : "40x40" },
    { "filename" : "icon_76x76.png",   "idiom" : "ipad",   "scale" : "1x", "size" : "76x76" },
    { "filename" : "icon_152x152.png", "idiom" : "ipad",   "scale" : "2x", "size" : "76x76" },
    { "filename" : "icon_167x167.png", "idiom" : "ipad",   "scale" : "2x", "size" : "83.5x83.5" },
    { "filename" : "icon_1024x1024.png", "idiom" : "ios-marketing", "scale" : "1x", "size" : "1024x1024" }
  ],
  "info" : { "author" : "xcode", "version" : 1 }
}
JSON
echo "  ✓ iOS/Assets.xcassets/AppIcon.appiconset (13 icons + Contents.json)"

echo ""
echo "[5/5] Done"
echo "========================================="
echo "Icons generated from ${SOURCE#$ROOT/}"
echo "========================================="
