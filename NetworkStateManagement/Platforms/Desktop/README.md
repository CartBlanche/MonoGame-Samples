# NetworkStateManagement Desktop Targets

This folder now has two desktop targets:

- `NetworkStateManagement.csproj`: itch.io-style desktop build (no Steam bootstrap).
- `NetworkStateManagement.Steam.csproj`: Steam desktop build using `MonoGame.Xna.Framework.Net.Steam`.

## Build Commands

From repo root:

- Itch build:
  - `dotnet build NetworkStateManagement/Platforms/Desktop/NetworkStateManagement.csproj`
- Steam build:
  - `dotnet build NetworkStateManagement/Platforms/Desktop/NetworkStateManagement.Steam.csproj`

## Steam Notes

- Steam runtime is initialized in `Program.Steam.cs` via `SteamRuntime.Initialize()`.
- Steam callbacks are pumped each frame in `DesktopSteamGame`.
- Optional local debug file: `NetworkStateManagement/steam_appid.txt`
  - If present, it is copied to the build output automatically.
- The default test value `480` is Valve's Spacewar example App ID.
  - It is useful for basic Steam API initialization checks.
  - It should not be relied on for meaningful leaderboard/achievement data in this sample.
- For real leaderboard/achievement validation, use your own Steamworks App ID (or Steam Playtest App ID) and define stats/achievements/leaderboards in Steamworks App Admin.
