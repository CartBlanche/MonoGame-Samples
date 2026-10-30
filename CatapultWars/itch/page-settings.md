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
| Cover image | `cover.png` (800x480; itch recommends 630x500, so crop or pad if you want it pixel-perfect) |
| Screenshots | `screenshot-gameplay.png`, `screenshot-victory.png`, `screenshot-instructions.png` |

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
