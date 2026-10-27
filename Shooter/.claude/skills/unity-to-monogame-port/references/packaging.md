# Packaging

Layout (as SolidOps): `Core/` shared library + `Content/`; `Platforms/Desktop/` DesktopGL launcher; `Platforms/Android/` net-android launcher. Solution file lists all.

## Icon master
Make the icon from the game itself with an `--icon <file>` flag that renders the hero object alone into a transparent PNG (1024 x 1024 default; `--icon-size`, orbit flags for yaw/pitch/roll, optional effect age/extras). Transparency trick: render once on **white** and once on **black**, then matte from the difference (alpha = 1 - (white - black)/255, colour recovered from the black render). Save as `Core/Content/Icon.png`.

## Generated icon assets
- `Core/Content/android-icons-generator.sh`: Android mipmaps/drawables, `Icon.ico`, `Icon.bmp` from `Icon.png`.
- `Platforms/Desktop/generate-icns.sh`: `.icns` for macOS.
Regenerate from the master; do not edit outputs.

## Desktop
- `Info.plist` and `app.manifest` for macOS / Windows packaging with MonoPack.
- csproj wires the icon (`Icon.ico`, `Icon.bmp`) and builds the content-pipeline extension before mgcb runs.
- Run: `dotnet run --project Platforms/Desktop/<Game>.csproj`.

## Android
- `dotnet build Platforms/Android/<Game>.csproj` needs the `android` workload and a JDK.
- Loose files that are not content-pipeline outputs (baked scene JSON, particle JSON, AssetMap) are included with `AndroidAsset`.
- Resources and icons are generated from the master.
- State honestly what is missing: the Shooter Android build has no touch controls and was never run on a device.

## Docs
README lists the layout, build/run commands, controls, every shot/test flag, and which parts are verified.
