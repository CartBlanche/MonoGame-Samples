#!/bin/zsh
# Catapult Wars Icon Generator
# Generates the Desktop (macOS/Windows) and Android icons from icon.svg
#
# Prerequisites:
# - Inkscape installed at /Applications/Inkscape.app
# - iconutil and sips (built into macOS)
# - ImageMagick (for the Windows .ico): brew install imagemagick
#
# Usage: ./generate-all-icons.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESOURCES_DIR="$SCRIPT_DIR"
SVG_SOURCE="$SCRIPT_DIR/icon.svg"
INKSCAPE="/Applications/Inkscape.app/Contents/MacOS/inkscape"

if [ ! -f "$SVG_SOURCE" ]; then
    echo "Error: icon.svg not found at $SVG_SOURCE"
    exit 1
fi

if [ ! -f "$INKSCAPE" ]; then
    echo "Error: Inkscape not found at $INKSCAPE"
    echo "Please install Inkscape to /Applications/"
    exit 1
fi

echo "========================================="
echo "Catapult Wars Icon Generator"
echo "========================================="
echo "Source SVG: $SVG_SOURCE"
echo "Output: $RESOURCES_DIR"
echo ""

mkdir -p "$RESOURCES_DIR"/{Android/drawable,Android/mipmap-mdpi,Android/mipmap-hdpi,Android/mipmap-xhdpi,Android/mipmap-xxhdpi,Android/mipmap-xxxhdpi,macOS,Windows}

echo "[1/4] Generating 1024x1024 master PNG from SVG..."
"$INKSCAPE" "$SVG_SOURCE" --export-type=png --export-filename="$RESOURCES_DIR/icon-1024.png" --export-width=1024 --export-height=1024

if [ ! -f "$RESOURCES_DIR/icon-1024.png" ]; then
    echo "Error: Failed to generate master PNG"
    exit 1
fi
echo ""

echo "[2/4] Generating Android icons..."
source "$SCRIPT_DIR/android-icons-generator.sh"
echo ""

echo "[3/4] Generating macOS icons..."
source "$SCRIPT_DIR/mac-icns-generator.sh"
echo ""

echo "[4/4] Generating Windows icons..."
source "$SCRIPT_DIR/windows-ico-generator.sh"
echo ""

echo "========================================="
echo "All icons generated."
echo "========================================="
echo "  Android: Android/"
echo "  macOS:   macOS/ (Icon.icns, Icon.bmp)"
echo "  Windows: Windows/ (Icon.ico)"
echo ""
echo "Platform .csproj files link these directly, so rebuild to pick them up."
