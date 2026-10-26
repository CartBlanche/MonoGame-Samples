#!/bin/zsh
# Android Icon Generator for Shooter
# Generates Android icons from Icon.png
#
# Usage: ./android-icons-generator.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_PNG="$SCRIPT_DIR/Icon.png"
ANDROID_OUTPUT="$SCRIPT_DIR/../../Platforms/Android/Resources"

if [ ! -f "$SOURCE_PNG" ]; then
    echo "Error: Icon.png not found at $SOURCE_PNG"
    exit 1
fi

echo "========================================="
echo "Shooter Android Icon Generator"
echo "========================================="
echo "Source: Icon.png"
echo "Output: $ANDROID_OUTPUT"
echo ""

# Android icon sizes for different densities
# mipmap icons for launcher
declare -A MIPMAP_SIZES=(
    ["mdpi"]=48
    ["hdpi"]=72
    ["xhdpi"]=96
    ["xxhdpi"]=144
    ["xxxhdpi"]=192
)

# Android splash/drawable icon sizes
declare -A DRAWABLE_SIZES=(
    ["mdpi"]=192
    ["hdpi"]=192
    ["xhdpi"]=192
    ["xxhdpi"]=192
    ["xxxhdpi"]=192
)

echo "[1/3] Generating mipmap launcher icons..."
# Generate mipmap icons for different densities
for density size in ${(kv)MIPMAP_SIZES}; do
    mkdir -p "$ANDROID_OUTPUT/mipmap-$density"
    output_file="$ANDROID_OUTPUT/mipmap-$density/ic_launcher.png"
    sips -z $size $size "$SOURCE_PNG" --out "$output_file" > /dev/null 2>&1
    echo "  ✓ mipmap-$density/ic_launcher.png ($size x $size)"
done

echo ""
echo "[2/3] Generating drawable icons..."
# Generate drawable icons for different densities
for density size in ${(kv)DRAWABLE_SIZES}; do
    mkdir -p "$ANDROID_OUTPUT/drawable-$density"
    output_file="$ANDROID_OUTPUT/drawable-$density/icon.png"
    sips -z $size $size "$SOURCE_PNG" --out "$output_file" > /dev/null 2>&1
    echo "  ✓ drawable-$density/icon.png ($size x $size)"
done

echo ""
echo "[3/3] Generating Icon.bmp and Icon.ico..."
# Generate Icon.bmp (256x256)
sips -z 256 256 "$SOURCE_PNG" --out "$SCRIPT_DIR/Icon.bmp" -s format bmp > /dev/null 2>&1
echo "  ✓ Icon.bmp (256 x 256)"

# Generate Icon.ico using ImageMagick if available, otherwise use sips
if command -v magick &> /dev/null; then
    magick "$SOURCE_PNG" -define icon:auto-resize=256,128,64,48,32,16 "$SCRIPT_DIR/Icon.ico"
    echo "  ✓ Icon.ico (multi-size)"
else
    # Fallback: just copy as .ico (not ideal but works for basic needs)
    sips -s format ico "$SOURCE_PNG" --out "$SCRIPT_DIR/Icon.ico" > /dev/null 2>&1
    echo "  ✓ Icon.ico (converted from PNG)"
    echo "  ⚠ Note: For proper multi-size .ico, install ImageMagick: brew install imagemagick"
fi

echo ""
echo "========================================="
echo "✓ Android icons generated successfully!"
echo "========================================="
echo ""
echo "Generated files:"
echo "  Launcher icons: $ANDROID_OUTPUT/mipmap-*/"
echo "  Drawable icons: $ANDROID_OUTPUT/drawable-*/"
echo "  Icon.bmp:       $SCRIPT_DIR/Icon.bmp"
echo "  Icon.ico:       $SCRIPT_DIR/Icon.ico"
echo ""