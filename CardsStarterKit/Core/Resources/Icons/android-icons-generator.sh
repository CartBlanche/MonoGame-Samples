#!/bin/zsh
# Android Icon Generator
# Generates Android mipmap icons from master PNG
#
# This script is sourced by generate-all-icons.sh
# Requires: $RESOURCES_DIR to be set

if [ -z "$RESOURCES_DIR" ]; then
    echo "Error: RESOURCES_DIR not set. This script should be sourced by generate-all-icons.sh"
    exit 1
fi

MASTER_PNG="$RESOURCES_DIR/icon-1024.png"
ANDROID_OUTPUT="$RESOURCES_DIR/Android"

# Android icon sizes for different densities
# mipmap icons for launcher
declare -A MIPMAP_SIZES=(
    ["mdpi"]=48
    ["hdpi"]=72
    ["xhdpi"]=96
    ["xxhdpi"]=144
    ["xxxhdpi"]=192
)

echo "Generating Android mipmap icons..."

# Generate mipmap icons for different densities
for density size in ${(kv)MIPMAP_SIZES}; do
    output_file="$ANDROID_OUTPUT/mipmap-$density/ic_launcher.png"
    sips -z $size $size "$MASTER_PNG" --out "$output_file" > /dev/null 2>&1
    echo "  ✓ mipmap-$density/ic_launcher.png ($size x $size)"
done

# Generate drawable/icon.png (high-res for splash screen)
sips -z 192 192 "$MASTER_PNG" --out "$ANDROID_OUTPUT/drawable/icon.png" > /dev/null 2>&1
echo "  ✓ drawable/icon.png (192 x 192)"

echo "✓ Android icons generated in $ANDROID_OUTPUT"
