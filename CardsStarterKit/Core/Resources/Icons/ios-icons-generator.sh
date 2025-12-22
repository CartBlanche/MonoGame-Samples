#!/bin/zsh
# iOS Icons Generator for CardsStarterKit
# Generates all required iOS icon sizes from icon-1024.png
# Updated for iOS 17+ requirements

if [ -z "$RESOURCES_DIR" ]; then
    echo "Error: RESOURCES_DIR not set. This script should be sourced by generate-all-icons.sh"
    exit 1
fi

echo "Generating iOS icons..."

SOURCE_PNG="$RESOURCES_DIR/icon-1024.png"
OUTPUT_DIR="$RESOURCES_DIR/iOS"

if [ ! -f "$SOURCE_PNG" ]; then
    echo "Error: icon-1024.png not found"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# App Store icon (required)
echo "  → Generating App Store icon (1024x1024)..."
cp "$SOURCE_PNG" "$OUTPUT_DIR/Icon-AppStore.png"

# iPhone icons
echo "  → Generating iPhone icons..."
sips -Z 20 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-20.png" > /dev/null
sips -Z 40 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-20@2x.png" > /dev/null
sips -Z 60 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-20@3x.png" > /dev/null

sips -Z 29 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-29.png" > /dev/null
sips -Z 58 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-29@2x.png" > /dev/null
sips -Z 87 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-29@3x.png" > /dev/null

sips -Z 40 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-40.png" > /dev/null
sips -Z 80 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-40@2x.png" > /dev/null
sips -Z 120 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-40@3x.png" > /dev/null

sips -Z 120 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-60@2x.png" > /dev/null
sips -Z 180 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-60@3x.png" > /dev/null

# iPad icons
echo "  → Generating iPad icons..."
sips -Z 76 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-76.png" > /dev/null
sips -Z 152 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-76@2x.png" > /dev/null
sips -Z 167 "$SOURCE_PNG" --out "$OUTPUT_DIR/Icon-83.5@2x.png" > /dev/null

# Mac icons (for iOS apps on Mac)
echo "  → Generating Mac Catalyst icons..."
sips -Z 16 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_16x16.png" > /dev/null
sips -Z 32 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_16x16@2x.png" > /dev/null
sips -Z 32 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_32x32.png" > /dev/null
sips -Z 64 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_32x32@2x.png" > /dev/null
sips -Z 128 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_128x128.png" > /dev/null
sips -Z 256 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_128x128@2x.png" > /dev/null
sips -Z 256 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_256x256.png" > /dev/null
sips -Z 512 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_256x256@2x.png" > /dev/null
sips -Z 512 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_512x512.png" > /dev/null
sips -Z 1024 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon_512x512@2x.png" > /dev/null

echo "✓ iOS icons generated (28 files)"
echo "  Output: $OUTPUT_DIR/"
