# Tarmac Toolbox: itch.io launch plan (handoff)

Status as of 2026-11-04. Read `Track-Editor-Plan.md` (decision log D1-D13, progress log) and `Track-Format-Spec.md` first.

## Decision: WebGL is parked
No MonoGame WebGL binaries are available yet (user is waiting for the `MonoGame.Runtime.Browser.WebGL2` packages). So:
- The browser **editor** (`prototype/tarmac-toolbox.html`, plain HTML/JS, no build step) is the editor for v1.x. It is NOT being rebuilt in C#/Gum.
- The **game** stays desktop (Mac/Windows/Linux). Custom tracks are played by downloading the `.Track` file into the Tracks folder.
- Everything already built in C# is kept and documented: `Core/Tracks/CustomTracks.cs` (loader/validation), `CustomTrackSelection.cs` (list, preview, paging), `TrackData` (metadata, checkpoints, heightmap), `TrackTool/` (command-line tool), props keep-out, LAN-race groundwork.
- Revisit WebGL when the packages exist. Known risks: custom shaders compiling for WebGL, performance (no threads), no UDP so no LAN races in a browser build, no Tracks folder (would need a file picker + browser storage). Idea if it happens: game + editor as two pages in one itch project so they share storage and the editor can have "Play this track".

## Goal for the next session: prepare the editor launch on itch.io
1. **Package the editor** as an itch.io HTML project: zip containing `index.html` (the editor, copy of `prototype/tarmac-toolbox.html`) + `outlines/` (index.json + the 4 outline files). Test it from a plain local web server (`python3 -m http.server`) and a headless/real browser: load, trace an outline from the library (needs `fetch`), draw a track, export `.Track`.
2. **Save/download behaviour:** the page currently offers a download for the `.Track` file (in the Claude artifact it had to save `.Track.txt`; on itch a normal download works, so use the real `.Track` name). Check the on-screen "where to save it" instructions (Mac `~/Library/Application Support/RacingGame/Tracks`, Windows `%LOCALAPPDATA%\RacingGame\Tracks`, Linux `~/.local/share/RacingGame/Tracks`; Android/iOS: not supported for the desktop game). Remove anything specific to the Claude artifact environment.
3. **Page polish for a public page:** title/About/credits, version number, licence note for the outline library (see `prototype/outlines/README.md`), no IP names in the UI (generic punny names only), "works best on desktop" note, mobile layout sanity check.
4. **Name check:** "Tarmac Toolbox" needs proper trademark clearance before going public (only a quick web check was done). Have a fallback name ready.
5. **itch.io page copy:** short description, how it works (4 steps: design, export, save into the Tracks folder, pick Custom in the game), screenshots/GIF, tags, a link to the game download once the desktop build is on itch (desktop builds uploaded via butler/zip are a separate step the user decides on).
6. **Not possible on itch:** a ranked community gallery (no backend). Realistic alternative: comments / a shared folder of tracks.
7. Ask the user before publishing anything outside the repo, and before committing. The whole editor/game change is ONE commit that the user triggers (see Rules below).

## Rules to keep following
- ONE commit for the whole editor change, only when the user says so; ask first. Unrelated fixes may be committed separately on request, one-line messages (done so far: 3fe77554, b5cd25a9, a710f80e).
- Before that commit, delete the TEMP-RIG pieces: `Core/Landscapes/TempRig.cs`, `Core/Landscapes/TempFingerprintDump.cs`, and the lines marked TEMP-FINGERPRINT / TEMP-RIG in `RacingGameManager.cs`, `SplashScreen.cs`, `BaseGame.cs` (autostart, capture, NOVSYNC, a Console.Error line in the render-loop catch).
- Generic, humorous pun names; existing art only; closed loops only; desktop first.
- Uncommitted editor work (the one commit): loader, validation, heightmap/checkpoints/author metadata, props keep-out, Custom button on TrackSelection (+ Enter there), custom list preview + paging, LineManager2D limit 64->1024, UIRenderer RenderPanel public + RenderPageArrow, TrackTool, docs, prototype.

## Other open items
- **Speed units (km/h and mph), game feature not built.** Work out how best to support both in the UI, probably a setting on the Options screen that toggles between them. It needs an in-game change: the speed is shown in mph in `GameScreen.cs` (HUD, via `CarPhysics.MeterPerSecToMph`) and `CarSelection.cs` (car stats with "mph" text); internally the game uses metres per second. Decide the default (by system region?), and make sure the setting is saved with the other options.
- Host-only sending of custom tracks to LAN clients (design at the end of `Track-Editor-Plan.md`). Check the LAN work is committed first.
- Optional extras for the custom list: terrain thumbnail, elevation profile.
- Lobby screen changes (Back/Ready on shared buttons) were not visually tested; needs two instances.
