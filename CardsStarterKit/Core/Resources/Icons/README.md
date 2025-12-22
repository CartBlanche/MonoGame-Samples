# Centralized Icon Resources

This directory contains the source icon file and all generated platform-specific icons for the CardsStarterKit Blackjack game.

## Structure

```
Icons/
├── icon.svg                      # Source SVG file (edit this)
├── icon-1024.png                 # Generated master PNG (1024x1024)
├── generate-all-icons.sh         # Main generation script
├── android-icons-generator.sh    # Android-specific generator
├── ios-icons-generator.sh        # iOS-specific generator
├── mac-icns-generator.sh         # macOS-specific generator
├── windows-ico-generator.sh      # Windows-specific generator
├── Android/                      # Android icons
│   ├── drawable/
│   │   └── icon.png             # Splash screen icon (192x192)
│   ├── mipmap-mdpi/             # 48x48
│   ├── mipmap-hdpi/             # 72x72
│   ├── mipmap-xhdpi/            # 96x96
│   ├── mipmap-xxhdpi/           # 144x144
│   └── mipmap-xxxhdpi/          # 192x192
├── iOS/                          # 28 iOS icon files
├── macOS/                        # Icon.icns + Icon.bmp
└── Windows/                      # Icon.ico + source PNGs
```

## Usage

### Regenerating All Icons

After editing `icon.svg`, regenerate all platform icons:

```bash
cd Core/Resources/Icons
./generate-all-icons.sh
```

### Prerequisites

- **Inkscape**: Install from [inkscape.org](https://inkscape.org) to `/Applications/Inkscape.app`
- **ImageMagick**: Install with `brew install imagemagick` (for Windows .ico generation)
- **sips** and **iconutil**: Built-in macOS tools (already available)

### Platform Integration

Icons are linked from platform `.csproj` files:

- **Android**: `Platforms/Android/BlackJack.csproj` links mipmap and drawable icons
- **iOS**: `Platforms/iOS/BlackJack.csproj` links all iOS icons to xcassets
- **Desktop**: `Platforms/Desktop/BlackJack.csproj` links Icon.ico and Icon.bmp
- **Windows**: `Platforms/Windows/BlackJack.csproj` links Icon.ico

### iOS Launch Screen

A copy of `Icon-60@3x.png` is placed at `Platforms/iOS/Resources/LaunchIcon.png` for the LaunchScreen.storyboard to reference.

## Icon Specifications

### Android
- **Launcher Icons**: 5 densities (mdpi to xxxhdpi)
- **Splash Screen**: 192x192 high-res PNG

### iOS
- **App Icons**: 28 sizes for iPhone, iPad, Mac Catalyst, and App Store
- **Launch Screen**: 180x180 (Icon-60@3x)

### macOS
- **Icon.icns**: Multi-resolution icon bundle (16x16 to 512x512 @2x)
- **Icon.bmp**: 256x256 bitmap fallback

### Windows
- **Icon.ico**: Multi-resolution icon (16x16 to 256x256)
- Contains 6 embedded sizes for different display contexts

## Theme

All icons use a maroon theme (#800000) to match the app's branding.
