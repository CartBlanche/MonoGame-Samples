# Packaging

Desktop (DesktopGL) only unless the user asks otherwise. Verified on macOS arm64 only.

## Layout (mirrors RacingGame; verified, builds clean)
`Core/` (net10.0 library + content), `Desktop/` (launcher, `Program.cs` dispatches the `--test-*` flags), `PipelineExtension/` (net8.0), `Tools/`, `scripts/`, `Documentation/`, `.sln`, `dotnet-tools.json` copied from RacingGame.

## Release-only PipelineExtension rule (verified)
- `Content.mgcb` has `/reference:../../PipelineExtension/bin/Release/net8.0/PipelineExtension.dll`; `Core.csproj` references the extension with `ReferenceOutputAssembly="false"` so it builds first (`MonoGameContentReference` for the .mgcb).
- `dotnet build Desktop/<Game>.csproj -c Release` works; a **Debug** build looks for a DLL that was never built. Always build Release (or change the reference path).
- Content volume: ~232 MB (149 MB textures, DXT); first content build takes minutes, a clean Desktop Release build ~91 s, shader-only rebuild ~15 s (touch `.fx` when only an include changed).
- The `.mgcb` is hand-structured with a generated texture section (ContentGen markers); never hand-edit that section. Loose data (AssetMap, `level.scene.json`, settings of the bake) copied to the output.

## launch / tasks
`.vscode/launch.json`: Launch Desktop, free-fly camera (`--fly`), physics self-test; `tasks.json` builds Release.

## Not yet done / unverified
- Icon: master PNG rendered from the game, `.ico/.icns` by script (see the Unity skill's packaging reference).
- macOS/Windows packaging with `Info.plist`/`app.manifest`, notarisation, Windows/Linux/Android/iOS builds (the UI kit omits QUIT on iOS but this is untested).
- The loading bar is staged groups, not byte progress; say so.

## Licence and README
- Source demo is MIT code + CC-BY assets: keep `LICENSE.md` unchanged, credit in the README Credits section (assets, music, font OFL, code), note changes (re-encoded audio, converted textures).
- README contents: screenshots, status, controls, build/run (Release rule, caffeinate), every flag with an example, layout, how it is verified, known limitations, doc pointers.
