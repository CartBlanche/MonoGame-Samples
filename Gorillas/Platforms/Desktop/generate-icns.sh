#!/bin/zsh
# Generate macOS .icns from Icon.png
# This script creates Gorillas.icns for MonoPack packaging

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
SOURCE_PNG="$PROJECT_ROOT/Core/Content/Icon.png"
OUTPUT_ICNS="$SCRIPT_DIR/Gorillas.icns"
ICONSET_DIR="$SCRIPT_DIR/Gorillas.iconset"

echo "Generating Gorillas.icns..."

if [ ! -f "$SOURCE_PNG" ]; then
    echo "Error: Icon.png not found at $SOURCE_PNG"
    exit 1
fi

TEMP_PNG="$SCRIPT_DIR/temp-icon.png"
cp "$SOURCE_PNG" "$TEMP_PNG"

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
echo "  → Creating Gorillas.icns..."
iconutil -c icns "$ICONSET_DIR" -o "$OUTPUT_ICNS"

# Clean up temporary files
rm -rf "$ICONSET_DIR" "$TEMP_PNG"

echo "✓ Gorillas.icns generated at: $OUTPUT_ICNS"
