# itch.io page settings for Catapult Wars

Copy these into **Edit game** on itch.io.

| Field | Value |
|---|---|
| Title | Catapult Wars: LAN Edition |
| Short description / tagline | Two catapults, one home network. Local multiplayer siege duels with family and friends. No accounts, no servers. |
| Kind of project | Downloadable |
| Classification | Games |
| Genre | Strategy (alternative: Action) |
| Pricing | Free / donations (your call) |
| Cover image | `cover-hit.gif` (630x500, about 1 MB, under itch's 3 MB limit): the shot flies, the camera zooms in and slows, and it hits. The older `cover.png` is the still alternative. |
| Screenshots (in this order) | `screenshot-gameplay.png`, `screenshot-hit.png`, `screenshot-halloween.png`, `screenshot-lan-lobby.png`, `screenshot-victory.png`, `screenshot-instructions.png` |

## Tags (max 10)
`lan`, `local-multiplayer`, `multiplayer`, `2d`, `artillery`, `turn-based`, `family-friendly`, `monogame`, `open-source`, `casual`

## Multiplayer
- Local multiplayer: **Yes**
- Networked multiplayer: **Yes** (LAN / System Link). Leave "Online" off if itch asks you to be specific; the game does not use internet matchmaking.
- Players: 2

## Uploads (you need to build these; the repo only has projects, no binaries)
```
dotnet publish Platforms/Desktop/CatapultNetWars.csproj -c Release -r win-x64
dotnet publish Platforms/Desktop/CatapultNetWars.csproj -c Release -r osx-arm64
dotnet publish Platforms/Desktop/CatapultNetWars.csproj -c Release -r linux-x64
```
Zip each output and tick the matching platform (Windows / macOS / Linux). Android: build the APK from `Platforms/Android` and upload it too.

## Community
Enable comments and add a topic called **"Challenge your family"** to encourage people to post their LAN match stories.

## Theme (Edit game > Edit theme)
Use `theme.css` in this folder in the "Custom CSS" box.

## Retaking the screenshots
The game can take its own: `--screenshots=<folder>` plays a scripted round against the AI and saves 800x480 PNGs
(`instructions`, `aiming`, `slowmo`, `hit`, `victory`). Add `--clip` to save every 4th frame of one shot instead, and stitch them with ImageMagick. `itch/make-cover.sh <theme> [--seed N]` does all of it and writes `itch/cover-<theme>.gif` (630x500, 128 colours, thinned automatically to stay under 3 MB).. Add `--theme=none` for the plain look or `--theme=halloween`.
For the lobby shot start two copies: `--screenshots=<folder> --lan=host`, then about 9 seconds later `--screenshots=<other folder> --lan=client --name="Roger Gracie"` (`--name` sets that copy's player name). Add `--play` to both to carry on into a LAN match: the host fires, then the client pulls back on the red side and fires (the client's `lan-client-view.png` is the red-side shot).
