# LAN Multiplayer for the Racing Game (planned)

> **Status: planned, not implemented.** This document records what is required to add LAN multiplayer to the Racing Game, based on a read-through of the current code and of the LAN multiplayer in the Blackjack sample. Nothing described here exists in the game yet.

## Goal

Today the game is single player with a **ghost car** (a replay of your best lap) to race against. The goal is LAN multiplayer in the style of the Blackjack sample, where several players on the same network race the same track at the same time and see each other's cars.

### Decisions for the first version

- **Scope:** "networked ghosts". Up to **4 players** race the same track simultaneously and see each other's cars. There is **no car-to-car collision** in the first version (cars pass through each other).
- **Platforms:** Desktop (macOS and Windows) first. Android and iOS come later.

## What exists today

### The Blackjack LAN stack (`CardsStarterKit/3-Games/Blackjack`)

- Networking is provided by the external library **`MonoGame.Xna.Framework.Net`** (a separate repository, `MonoGame.Xna.Framework.Net`), which reimplements the XNA `Microsoft.Xna.Framework.Net` API (`NetworkSession`, `NetworkGamer`, `PacketReader`/`PacketWriter`) over raw UDP. Blackjack references it with a relative `ProjectReference`. It targets `net10.0`, the same as the Racing Game.
- **Discovery:** the host broadcasts a session advertisement every 750 ms on UDP port 31337. Gameplay traffic uses UDP port 31338.
- **Roles and health:** one host and several clients. A heartbeat is sent every 3 s with a 9 s timeout. There is no reconnect logic.
- **Game code:** `PacketTypes.cs` and `Packets.cs` (a byte packet type plus a class per packet with `Serialize`/`Deserialize`), `GameplayPacketDispatcher` (dictionary dispatch, drained on the main thread each `Update`), `MultiplayerSessionService` (backend selection) and the screen flow `SessionBrowserScreen` → `BlackjackLobbyScreen` → `GameplayScreen`.
- **Important limitation:** the UDP path has **no reliability layer**. `SendDataOptions.Reliable` is accepted but ignored; every send is a plain datagram. This is fine for a card game on a LAN, but a race must add its own resend for key events (see Protocol).

### The Racing Game (`RacingGame/Core`)

- **No networking today.** `Core` is a class library shared by the Desktop (`net10.0`), Windows (`net10.0-windows`), Android and iOS projects.
- **The car:** `Player : ChaseCamera : CarPhysics : BasePlayer`, a single global instance (`RacingGameManager.Player`). Physics uses a variable timestep, reads `Input.*` directly and has no car-to-car collision. The car state is small: position, direction, up vector, speed, wheel position and the pitch spring.
- **The ghost:** `Replay` stores one `Matrix` every 0.2 s. The ghost is drawn in `GameScreen.Render` with `RacingGameManager.CarModel.RenderCar(0, color, shadowCarMode: true, matrix)` and is not rendered through the shadow path.
- **Hidden globals that matter for extra cars:** `Model.RenderCar` reads `RacingGameManager.Player.CarWheelPos`; the car color goes through the global `RacingGameManager.currentCarColor`; `Landscape.NewReplay` and `BestReplay`, the highscores and the `Sound` engine cues are all single-player globals.
- **Screens:** an `IGameScreen` stack (`RacingGameManager.AddGameScreen`) with the flow `MainMenu` → `CarSelection` → `TrackSelection` → `GameScreen`.

## Recommended architecture: client-authoritative "puppet" cars

Each machine simulates **only its own car** with the existing physics. Remote cars are **puppets**: they never go through `CarPhysics` and are only drawn from interpolated network state. This avoids the large refactor (extracting a `Car` class from the `Player` singleton) that a fully simulated multi-car race would need, which is why the first version is comparatively cheap.

- **Transport:** reference `MonoGame.Xna.Framework.Net` with the same relative `ProjectReference` Blackjack uses. The Desktop `Program.cs` initialises the UDP SystemLink backend, copying the `MultiplayerSessionService` pattern. One host and the clients share one `NetworkSession` of type SystemLink with a maximum of 4 gamers.
- **Trust model:** clients are trusted (LAN, friends). There is no anti-cheat.
- **Clock and start:** the host sends a `RaceStart` message with the countdown, and clients start their 5 s zoom-in and traffic-light countdown on receipt (LAN latency is a few ms). Race times are measured locally, and the host ranks players by their reported finish times.

### Protocol

- **Car state, about 15 to 20 Hz, unreliable.** Sequence number, sender race time, position, forward, up, speed, wheel position, pitch, lap, checkpoint index, and a finished flag with the finish time. About 70 bytes per packet.
- **Events with their own resend:** race start, player finished and results. Resend 3 times at 100 ms intervals (or until acknowledged), and repeat the same flags in the periodic car-state packets so a lost event repairs itself.
- **Smoothing of remote cars:** keep the last two or three snapshots, render about 100 ms in the past, lerp the position and slerp the orientation as a quaternion. Do not reuse `Matrix.Lerp` from `Replay`, because it distorts rotation.

## Work breakdown (first version: 4 players, Desktop)

| # | Phase | Main changes | Rough effort |
|---|---|---|---|
| 0 | Library and session plumbing | `ProjectReference` in `Core.csproj`; new `Core/Multiplayer/` with `MultiplayerSessionService.cs`, `RacePacketTypes.cs`, `RacePackets.cs` and `RaceNetSession.cs` (host, join, send, dispatch on the main thread); backend initialisation in the Desktop `Program.cs` | 1 to 2 days |
| 1 | Menus and lobby | Mode choice (Single / Multiplayer) before `CarSelection`; text-based `MultiplayerMenuScreen`, `SessionBrowserScreen` and `LobbyScreen` (player names, car and color per player, host Start); a start-grid lane per slot, from the track start position plus a lateral offset | 2 to 3 days |
| 2 | State sync and remote cars | `RemoteCar` (snapshot buffer and interpolation); per-car overloads of `Model.RenderCar` (wheel position and color passed in instead of read from `Player` and `currentCarColor`); a `MultiplayerGameScreen` derived from `GameScreen` that draws all remote cars (the ghost is disabled in multiplayer) | 2 to 3 days |
| 3 | Race rules and HUD | Synchronised countdown; per-player lap and finish tracking; position (1st of 4), name tags above cars, results screen; "DNF" instead of "Game Over" when leaving the track; disconnect and host-left handling; no multiplayer times written to the single-player highscores | 2 days |
| 4 | Test and tune | Two-machine LAN test; the library's `SimulatedPacketLoss`; tuning of smoothing and send rate; bug fixing | 2 to 3 days |

**Total: about 9 to 13 working days** for a working 4-player Desktop LAN prototype. This is a rough estimate; the main uncertainty is the Phase 2 rendering changes and LAN testing. For comparison, the existing ghost car is about a tenth of this.

### Later options (not in the first version)

| Option | Rough effort | Notes |
|---|---|---|
| Car-to-car collisions | 4 to 7 days | Needs new collision code and a decision on who resolves it |
| Per-car engine sound | 1 to 2 days | The engine sound is a single global cue pair today |
| Remote cars casting shadows | about 1 day | Explicit `GenerateShadow` calls; the shadow path has been fragile |
| Android | 1 to 2 days | Manifest permissions and a `MulticastLock` for LAN discovery |
| iOS | 1 to 2 days | Local-network permission handling |
| Rejoin after a disconnect | about 2 days | |
| A proper `Car` class extracted from `Player` | 5 to 8 days | Only needed for collisions or AI cars |

## Risks and gotchas

- **No reliability in the UDP layer:** design every critical message to be idempotent and repeated.
- **Fixed discovery ports (31337 and 31338):** two instances on one machine can collide. Test on two machines, or add a development option for the ports.
- **Firewalls and access-point client isolation** can block LAN broadcast. macOS may show a "local network" prompt the first time.
- **Network events arrive on background threads** in the library. Only touch game state from `Update`, using the Blackjack pattern: queue the packets and drain them on the main thread.
- **The Windows (DirectX) project** would pull in the library, which references `MonoGame.Framework.DesktopGL` privately. Blackjack's Windows project already does this, but the Racing Game Windows project needs to be checked.
- **Variable-timestep physics** is fine here because each machine simulates only its own car. It is also a reason not to use lockstep or server-authoritative simulation.
- **Global state to keep in mind:** `currentCarNumber` and `currentCarColor`, `Player.CarWheelPos` in `RenderCar`, `Landscape.NewReplay` and the checkpoint progress (using the local player only is fine for the first version), and the `Sound` engine cues.

## Files involved

- **New:** `Core/Multiplayer/*` (session service, packets, `RaceNetSession`, `RemoteCar`) and new screens under `Core/GameScreens/` (`MultiplayerMenuScreen`, `SessionBrowserScreen`, `LobbyScreen`, `MultiplayerGameScreen` and a results screen).
- **Changed:** `Core/Core.csproj` (project reference), the Desktop `Program.cs` (backend initialisation), `Core/GameScreens/MainMenu.cs` or `CarSelection.cs` (mode choice), `Core/Graphics/Model.cs` (`RenderCar` with per-car wheel position and color), `Core/GameScreens/GameScreen.cs` (base for the multiplayer screen), `Core/GameLogic/Player.cs` and `BasePlayer.cs` (DNF instead of game over), `Core/Graphics/UIRenderer.cs` (HUD additions) and `Core/Landscapes/Landscape.cs` and the track code (start-grid lanes).
- **Reuse from Blackjack:** `GameplayPacketDispatcher`, `MultiplayerSessionService`, and the screen flow of `SessionBrowserScreen`, the lobby and gameplay.

## Verification plan

1. Round-trip tests for each packet's `Serialize` and `Deserialize`.
2. A one-machine test with two instances on loopback (needs the port option), checking host, join, lobby and start.
3. A two-machine LAN test: join, synchronised countdown, 4 cars visible, smooth remote motion, and laps and finish ranking agreeing on every machine.
4. Resilience: run with the library's `SimulatedPacketLoss` (for example 10 to 20 percent) and confirm race start, finish and results still work. Kill a client mid-race (the others continue and its car disappears or is marked DNF). Quit the host (clients return to the menu with a message).
5. Regression: single player, including the ghost, the highscores and the replay car in the menus, must be unchanged.
