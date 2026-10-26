#!/bin/zsh
# Generate macOS .icns from Icon.png
# This script creates Shooter.icns for MonoPack packaging (Info.plist CFBundleIconFile)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
SOURCE_ICO="$PROJECT_ROOT/Core/Content/Icon.png"
OUTPUT_ICNS="$SCRIPT_DIR/Shooter.icns"
ICONSET_DIR="$SCRIPT_DIR/Shooter.iconset"

echo "Generating Shooter.icns..."

if [ ! -f "$SOURCE_ICO" ]; then
    echo "Error: Icon.png not found at $SOURCE_ICO"
    exit 1
fi

# Convert .ico to .png first
TEMP_PNG="$SCRIPT_DIR/temp-icon.png"
sips -s format png "$SOURCE_ICO" --out "$TEMP_PNG" > /dev/null

# Create iconset directory
mkdir -p "$ICONSET_DIR"

# Generate all required icon sizes
echo "  → Generating iconset..."
sips -Z 16 "$TEMP_PNG" --out "$ICONSET_DIR/icon_16x16.png" > /dev/null
sips -Z 32 "$TEMP_PNG" --out "$ICONSET_DIR/icon_16x16@2x.png" > /dev/null
sips -Z 32 "$TEMP_PNG" --out "$ICONSET_DIR/icon_32x32.png" > /dev/null
sips -Z 64 "$TEMP_PNG" --out "$ICONSET_DIR/icon_32x32@2x.png" > /dev/null
sips -Z 128 "$TEMP_PNG" --out "$ICONSET_DIR/icon_128x128.png" > /dev/null
sips -Z 256 "$TEMP_PNG" --out "$ICONSET_DIR/icon_128x128@2x.png" > /dev/null
sips -Z 256 "$TEMP_PNG" --out "$ICONSET_DIR/icon_256x256.png" > /dev/null
sips -Z 512 "$TEMP_PNG" --out "$ICONSET_DIR/icon_256x256@2x.png" > /dev/null
sips -Z 512 "$TEMP_PNG" --out "$ICONSET_DIR/icon_512x512.png" > /dev/null
sips -Z 1024 "$TEMP_PNG" --out "$ICONSET_DIR/icon_512x512@2x.png" > /dev/null

# Convert iconset to .icns
echo "  → Creating Shooter.icns..."
iconutil -c icns "$ICONSET_DIR" -o "$OUTPUT_ICNS"

# Clean up temporary files
rm -rf "$ICONSET_DIR" "$TEMP_PNG"

echo "✓ Shooter.icns generated at: $OUTPUT_ICNS"
