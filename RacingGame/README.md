# Racing Game
MonoGame port of the Racing Game Starter Kit to macOS, Linux, Windows (OpenGL and DirectX), Android and iOS

The code here was originally from [Infinite Space Studios] (https://github.com/infinitespace-studios/RacingGame)' repo.

Running on [Windows](https://www.youtube.com/watch?v=_iLWfWwkMJU).

Runnning in the [Android Emulator](https://www.youtube.com/watch?v=wkyNKm08tCk).

## Upcoming features

- **LAN multiplayer:** race against other players on your local network, in the style of the Blackjack sample's LAN play. This is planned and not implemented yet; see [LAN Multiplayer](Documentation/LAN-Multiplayer.md) for the design, requirements and work breakdown.

## Lighting and time of day

The race can be played by day, at dusk or at night. Each built-in track has its own time of day (Beginner day, Advanced dusk, Expert night) and a custom track can set one with a `TimeOfDay` element in its `.Track` file (see [Track Format](Documentation/Track-Format-Spec.md)).

- **Night and dusk:** darker lighting, a star and Milky Way sky with a moon (a CC0 sky photo, see [credits](Documentation/Credits-Night-Sky.md)), fog, and glow on the street and tunnel lamps.
- **Headlights and tail lights:** warm headlights light the road ahead at night and in tunnels; the tail lights brighten when braking (also on other cars in a LAN race).
- **Tunnels** darken smoothly on entry and are lit by their own lamps.
- **Options:** *Lighting* (Off, Low, High) and *Time of day* (Track, Day, Dusk, Night). Off gives the original look and costs nothing; settings apply at the next race.

Design, decisions, measurements and the dev log notes are in [Lighting Plan](Documentation/Lighting-Plan.md); frame time and allocation measurements are in [Performance Review](Documentation/Performance-Review.md).

### Developer test hooks

Environment variables for testing and screenshots (they do nothing unless set):

| Variable | Effect |
|---|---|
| `RACINGGAME_TIMEOFDAY=0..1` | Start time of day for a race (0 noon, 0.35 golden hour, 0.65 dusk, 1 night) |
| `RACINGGAME_DEMOCLOCK=HH:MM` | Start time of the menu demo's day (default: the computer's clock); the demo runs a full day in 120 s with a clock in the top right |
| `RACINGGAME_CLOCK=<seconds>` | A running clock: one full day in that many seconds |
| `RACINGGAME_DEBUGKEYS=1` | Debug keys: `]` clock speed, `[` pause, `-` and `=` scrub the time, `H` headlights auto/on/off |
| `RACINGGAME_TITLE_SUFFIX="- Old"` | Appended to the window title ("Racing Game - Old"), to tell two instances apart when comparing |
| `RACINGGAME_SHADOWS=classic\|integrated` | Forces the shadow mode for A/B tests. Default: integrated (lighting-integrated natural shadows) when Lighting is Low or High, the original screen-space overlay when Lighting is Off |
| `RACINGGAME_TRACKS_DIR`, `RACINGGAME_PLAYER_NAME` | LAN test aids (a second tracks folder, a different player name per instance) |

The automated frame-capture rig (fixed time step, repeatable runs, front view camera, timing) is kept as patch files in `Documentation/` and is not part of the game; see the *Test rig* notes in the Lighting Plan.

