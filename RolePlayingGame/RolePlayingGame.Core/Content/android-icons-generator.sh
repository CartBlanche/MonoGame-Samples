#!/bin/zsh

# Declare a constant variables
readonly resources_base_path="../../RolePlayingGame.Android/Resources"
readonly drawable_resources_path="$resources_base_path/drawable-"
readonly mdpi="mdpi"
readonly hdpi="hdpi"
readonly xhdpi="xhdpi"
readonly xxhdpi="xxhdpi"
readonly xxxhdpi="xxxhdpi"

echo "Generating Android icons"

echo "Generating Android splash screens"
mkdir -p "$drawable_resources_path$mdpi"
mkdir -p "$drawable_resources_path$hdpi"
mkdir -p "$drawable_resources_path$xhdpi"
mkdir -p "$drawable_resources_path$xxhdpi"
mkdir -p "$drawable_resources_path$xxxhdpi"

sips -Z 48 icon-1024.png -o "$drawable_resources_path$mdpi/icon.png"
sips -Z 72 icon-1024.png -o "$drawable_resources_path$hdpi/icon.png"
sips -Z 96 icon-1024.png -o "$drawable_resources_path$xhdpi/icon.png"
sips -Z 144 icon-1024.png -o "$drawable_resources_path$xxhdpi/icon.png"
sips -Z 192 icon-1024.png -o "$drawable_resources_path$xxxhdpi/icon.png"

sips -Z 470 splash.png -o "$drawable_resources_path$mdpi/splash.png"
sips -Z 640 splash.png -o "$drawable_resources_path$hdpi/splash.png"
sips -Z 960 splash.png -o "$drawable_resources_path$xhdpi/splash.png"
sips -Z 1440 splash.png -o "$drawable_resources_path$xxhdpi/splash.png"
sips -Z 1920 splash.png -o "$drawable_resources_path$xxxhdpi/splash.png"

echo "Android Generation Complete!"