#!/bin/zsh
# CardsStarterKit Icon Generator
# Generates all platform icons from icon.svg
#
# Prerequisites:
# - Inkscape installed at /Applications/Inkscape.app
# - iconutil (built-in macOS for .icns generation)
# - sips (built-in macOS for PNG resizing)
# - ImageMagick (for Windows .ico generation)
#
# Usage: ./generate-all-icons.sh

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESOURCES_DIR="$SCRIPT_DIR"
SVG_SOURCE="$SCRIPT_DIR/icon.svg"
INKSCAPE="/Applications/Inkscape.app/Contents/MacOS/inkscape"

# Check if SVG source exists
if [ ! -f "$SVG_SOURCE" ]; then
    echo "Error: icon.svg not found at $SVG_SOURCE"
    exit 1
fi

# Check if Inkscape is installed
if [ ! -f "$INKSCAPE" ]; then
    echo "Error: Inkscape not found at $INKSCAPE"
    echo "Please install Inkscape to /Applications/"
    exit 1
fi

echo "========================================="
echo "CardsStarterKit Icon Generator"
echo "========================================="
echo "Source SVG: $SVG_SOURCE"
echo "Output: $RESOURCES_DIR"
echo ""

# Create output directories
mkdir -p "$RESOURCES_DIR"/{Android/drawable,Android/mipmap-mdpi,Android/mipmap-hdpi,Android/mipmap-xhdpi,Android/mipmap-xxhdpi,Android/mipmap-xxxhdpi,iOS,macOS,Windows}

# Step 1: Generate high-res master PNG from SVG
echo "[1/5] Generating 1024x1024 master PNG from SVG..."
"$INKSCAPE" "$SVG_SOURCE" --export-type=png --export-filename="$RESOURCES_DIR/icon-1024.png" --export-width=1024 --export-height=1024

if [ ! -f "$RESOURCES_DIR/icon-1024.png" ]; then
    echo "Error: Failed to generate master PNG"
    exit 1
fi

echo "✓ Master PNG generated: icon-1024.png"
echo ""

# Step 2: Generate Android icons
echo "[2/5] Generating Android icons..."
source "$SCRIPT_DIR/android-icons-generator.sh"
echo ""

# Step 3: Generate iOS icons
echo "[3/5] Generating iOS icons..."
source "$SCRIPT_DIR/ios-icons-generator.sh"
echo ""

# Step 4: Generate macOS icons
echo "[4/5] Generating macOS icons..."
source "$SCRIPT_DIR/mac-icns-generator.sh"
echo ""

# Step 5: Generate Windows icons
echo "[5/5] Generating Windows icons..."
source "$SCRIPT_DIR/windows-ico-generator.sh"
echo ""

echo "========================================="
echo "✓ All icons generated successfully!"
echo "========================================="
echo ""
echo "Generated files in: $RESOURCES_DIR"
echo "  Android icons:  Android/"
echo "  iOS icons:      iOS/"
echo "  macOS icons:    macOS/"
echo "  Windows icons:  Windows/"
echo ""
echo "Icons are centralized and will be linked by platform .csproj files"
echo ""
