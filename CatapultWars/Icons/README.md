# Catapult Wars icons

Edit `icon.svg`, then regenerate everything:

```bash
cd Icons
./generate-all-icons.sh
```

Needs Inkscape in `/Applications`, ImageMagick (`brew install imagemagick`), and macOS `sips` / `iconutil`.

| Output | Used by |
|---|---|
| `Windows/Icon.ico` | Desktop and Windows `.csproj` (exe icon), embedded in Desktop as the window icon |
| `macOS/Icon.icns`, `macOS/Icon.bmp` | `.app` bundle icon; `Icon.bmp` is embedded in Desktop as the window icon |
| `Android/mipmap-*`, `Android/drawable/icon.png` | Android `.csproj` links these into `Resources` |

Same pattern as Blackjack in CardsStarterKit (`2-Core/Resources/Icons`). iOS icons are not generated yet.

To package Desktop (win/linux/macOS) and Android builds: `scripts/build-macos-desktop-android.sh --help`.
