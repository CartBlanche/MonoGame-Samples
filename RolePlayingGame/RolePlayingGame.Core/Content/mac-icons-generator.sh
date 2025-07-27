#!/bin/zsh

# Declare a constant variables
readonly top_level_path="../../RolePlayingGame.Desktop/AppIcon.xcassets"
readonly xcassets_path="$top_level_path/AppIcon.appiconset"

while true; do
  # Prompt for user confirmation
  echo -n "This script will delete your '$xcassets_path' directory and recreate all the assets again. Are you sure you wish to proceed? [(y)es/(n)o]: "
  read confirm

  # Check the user's response
  if [[ "${confirm:l}" == "y" || "${confirm:l}" == "yes" ]]; then
      # Check if the directory exists
      if [[ -d "$top_level_path" ]]; then
          # Top level directory exists, delete it
          rm -rf "$top_level_path"
          echo "'$top_level_path' directory deleted successfully."
      else
          echo "'$top_level_path' directory does not exist. Continuing."
      fi
      break
  elif [[ "${confirm:l}" == "n" || "${confirm:l}" == "no" ]]; then
      echo "Deletion canceled."
      exit 0
  else
      echo "Invalid input. Please enter 'y'/'yes' or 'n'/'no'."
  fi
done

echo "macOS Icon Generation Started!"

echo "Creating $xcassets_path directory"
mkdir -p "$xcassets_path"

# Generate the required icon sizes
echo "Generating macOS icons"
sips -Z 16 icon-1024.png -o "$xcassets_path/icon_16x16.png"
sips -Z 32 icon-1024.png -o "$xcassets_path/icon_32x32.png"
sips -Z 64 icon-1024.png -o "$xcassets_path/icon_64x64.png"
sips -Z 128 icon-1024.png -o "$xcassets_path/icon_128x128.png"
sips -Z 256 icon-1024.png -o "$xcassets_path/icon_256x256.png"
sips -Z 512 icon-1024.png -o "$xcassets_path/icon_512x512.png"
# yes I know it's the same size
sips -Z 1024 icon-1024.png -o "$xcassets_path/icon_1024x1024.png"

# Create the Contents.json file
echo "Generating Contents.json file"
cat > "$xcassets_path/Contents.json" <<EOF
{
  "images" : [
    {
      "filename": "icon_16x16.png",
      "idiom": "mac",
      "scale": "1x",
      "size": "16x16"
    },
    {
      "filename": "icon_32x32.png",
      "idiom": "mac",
      "scale": "2x",
      "size": "16x16"
    },
    {
      "filename": "icon_32x32.png",
      "idiom": "mac",
      "scale": "1x",
      "size": "32x32"
    },
    {
      "filename": "icon_64x64.png",
      "idiom": "mac",
      "scale": "2x",
      "size": "32x32"
    },
    {
      "filename": "icon_128x128.png",
      "idiom": "mac",
      "scale": "1x",
      "size": "128x128"
    },
    {
      "filename": "icon_256x256.png",
      "idiom": "mac",
      "scale": "2x",
      "size": "128x128"
    },
    {
      "filename": "icon_256x256.png",
      "idiom": "mac",
      "scale": "1x",
      "size": "256x256"
    },
    {
      "filename": "icon_512x512.png",
      "idiom": "mac",
      "scale": "2x",
      "size": "256x256"
    },
    {
      "filename": "icon_512x512.png",
      "idiom": "mac",
      "scale": "1x",
      "size": "512x512"
    },
    {
      "filename": "icon_1024x1024.png",
      "idiom": "mac",
      "scale": "2x",
      "size": "512x512"
    }
  ],
  "info" : {
    "author" : "xcode",
    "version" : 1
  }
}
EOF

echo "macOS Icon Generation Complete!"