#!/bin/zsh
# Windows .ico Generator for CardsStarterKit
# Generates Windows icon file from icon-1024.png

if [ -z "$RESOURCES_DIR" ]; then
    echo "Error: RESOURCES_DIR not set. This script should be sourced by generate-all-icons.sh"
    exit 1
fi

echo "Generating Windows icons..."

SOURCE_PNG="$RESOURCES_DIR/icon-1024.png"
OUTPUT_DIR="$RESOURCES_DIR/Windows"

if [ ! -f "$SOURCE_PNG" ]; then
    echo "Error: icon-1024.png not found"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Generate all standard Windows icon sizes
echo "  → Generating Windows icon PNGs..."
sips -Z 16 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon-16.png" > /dev/null
sips -Z 24 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon-24.png" > /dev/null
sips -Z 32 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon-32.png" > /dev/null
sips -Z 48 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon-48.png" > /dev/null
sips -Z 64 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon-64.png" > /dev/null
sips -Z 256 "$SOURCE_PNG" --out "$OUTPUT_DIR/icon-256.png" > /dev/null

# Check if ImageMagick is available to create .ico
echo "  → Creating Icon.ico with ImageMagick..."
if command -v magick &> /dev/null; then
    magick "$OUTPUT_DIR/icon-16.png" "$OUTPUT_DIR/icon-24.png" "$OUTPUT_DIR/icon-32.png" \
           "$OUTPUT_DIR/icon-48.png" "$OUTPUT_DIR/icon-64.png" "$OUTPUT_DIR/icon-256.png" \
           "$OUTPUT_DIR/Icon.ico"

    if [ -f "$OUTPUT_DIR/Icon.ico" ]; then
        echo "✓ Icon.ico created successfully"
    else
        echo "⚠ Failed to create Icon.ico"
    fi
else
    echo "⚠ ImageMagick not found. Install with: brew install imagemagick"
    echo "  Individual PNG files have been created in $OUTPUT_DIR"
fi

echo "✓ Windows icon files generated"
echo "  Output: $OUTPUT_DIR/"
