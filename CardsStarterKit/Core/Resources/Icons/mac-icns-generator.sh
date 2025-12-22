#!/bin/zsh
# macOS .icns Generator for CardsStarterKit
# Generates macOS icon bundle from icon-1024.png

if [ -z "$RESOURCES_DIR" ]; then
    echo "Error: RESOURCES_DIR not set. This script should be sourced by generate-all-icons.sh"
    exit 1
fi

echo "Generating macOS icons..."

SOURCE_PNG="$RESOURCES_DIR/icon-1024.png"
OUTPUT_DIR="$RESOURCES_DIR/macOS"
ICONSET_DIR="$OUTPUT_DIR/blackjack.iconset"

if [ ! -f "$SOURCE_PNG" ]; then
    echo "Error: icon-1024.png not found"
    exit 1
fi

# Create output directories
mkdir -p "$OUTPUT_DIR"
mkdir -p "$ICONSET_DIR"

# Generate iconset with all required sizes
echo "  → Generating iconset..."
sips -Z 16 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_16x16.png" > /dev/null
sips -Z 32 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_16x16@2x.png" > /dev/null
sips -Z 32 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_32x32.png" > /dev/null
sips -Z 64 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_32x32@2x.png" > /dev/null
sips -Z 128 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_128x128.png" > /dev/null
sips -Z 256 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_128x128@2x.png" > /dev/null
sips -Z 256 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_256x256.png" > /dev/null
sips -Z 512 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_256x256@2x.png" > /dev/null
sips -Z 512 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_512x512.png" > /dev/null
sips -Z 1024 "$SOURCE_PNG" --out "$ICONSET_DIR/icon_512x512@2x.png" > /dev/null

# Convert iconset to .icns
echo "  → Creating Icon.icns..."
iconutil -c icns "$ICONSET_DIR" -o "$OUTPUT_DIR/Icon.icns"

# Create Icon.bmp for older systems
echo "  → Creating Icon.bmp (256x256)..."
sips -Z 256 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon.bmp" -s format bmp > /dev/null

# Clean up temporary iconset directory
rm -rf "$ICONSET_DIR"

echo "✓ macOS icons generated"
echo "  Icon.icns: $OUTPUT_DIR/Icon.icns"
echo "  Icon.bmp:  $OUTPUT_DIR/Icon.bmp"
