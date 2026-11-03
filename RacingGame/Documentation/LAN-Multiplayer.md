# LAN Multiplayer for the Racing Game (planned)

> **Status: planned, not implemented.** This document records what is required to add LAN multiplayer to the Racing Game, based on a read-through of the current code and of the LAN multiplayer in the ShipGame sample. Nothing described here exists in the game yet.

## Goal

Today the game is single player with a **ghost car** (a replay of your best lap) to race against. The goal is LAN multiplayer in the style of the ShipGame sample, where several players on the same network race the same track at the same time and see each other's cars.

### Decisions for the first version

- **Scope:** "networked ghosts". Up to **4 players** race the same track simultaneously and see each other's cars. There is **no car-to-car collision** in the first version (cars pass through each other).
- **Platforms:** Desktop (macOS and Windows) first. Android and iOS come later.

## What exists today

### The ShipGame LAN stack (`MonoGame.Samples/ShipGame/ShipGame.Core/Networking`)

- Networking is provided by the external library **`MonoGame.Xna.Framework.Net`** (a separate repository, `MonoGame.Xna.Framework.Net`), which reimplements the XNA `Microsoft.Xna.Framework.Net` API (`NetworkSession`, `NetworkGamer`, `PacketReader`/`PacketWriter`) over raw UDP. ShipGame references it as the NuGet package `MonoGame.Xna.Framework.Net` version `1.0.0-preview.1`. It targets `net10.0`, the same as the Racing Game.
- **Discovery:** the host broadcasts a session advertisement every 750 ms on UDP port 31337. Gameplay traffic uses UDP port 31338.
- **Roles and health:** one host and several clients. A heartbeat is sent every 3 s with a 9 s timeout. There is no reconnect logic.
- **Game code (`ShipGame.Core/Networking`):**
  - `ShipPacketType.cs` and `ShipPackets.cs`: a byte packet type plus a class per packet implementing `INetworkPacket` with `Serialize`/`Deserialize`.
  - `ShipPacketDispatcher`: dictionary dispatch, with a `PacketAudience` filter (`Any`, `FromHost`, `FromClient`) that drops packets from the wrong side and counts malformed or unknown ones.
  - `NetworkManager`: pumps the session once per frame from `Update`, resends unacknowledged data, queues the library's network-thread events and raises them on the game thread. It offers `Send`, `SendToHost`, `Broadcast` and `Relay`, each with a `reliable` flag, and reports a peer as stalled after 5 s of unacknowledged reliable data.
  - `MultiplayerSessionService`: backend selection and session create, find and join. It also has a `ProtocolVersion` (bumped when packet formats change, so mismatched builds do not join each other), an `InProgress` session property (set when the match starts, so the session is no longer advertised as joinable) and `DirectSession(IPAddress)` for joining a known address without discovery.
  - `ShipStateSync` (about 20 Hz state packets) with `RemoteShipInterpolator` (the equivalent of the `RemoteCar` planned here), plus `LanLobby`, `CombatSync` and `SpawnAssigner`.
  - Screen flow: `ScreenLanBrowser` → `ScreenLanLobby` → `ScreenGame`.
- **Packet id ranges:** the library treats a datagram whose first byte is a registered framework message id (currently 1 to 10) as its own message, and wraps every datagram in a reliable or unreliable channel whose first byte (0x21 to 0x25) must stay clear too. ShipGame therefore starts its gameplay ids at 64. The Racing Game packet types must use the same range.
- **Reliability:** the library has a `ReliableChannel`. `SendDataOptions.Reliable` sends are acknowledged and resent, and delivered in order to the receiver. `SendDataOptions.InOrder` (without `Reliable`) is lossy but sequenced, so only the newest packet is delivered, which suits car state. `None` sends a plain datagram. ShipGame uses reliable sends for lobby, match start, damage and results, and unreliable sends for the frequent ship state.

### The Racing Game (`RacingGame/Core`)

- **No networking today.** `Core` is a class library shared by the Desktop (`net10.0`), Windows (`net10.0-windows`), Android and iOS projects.
- **The car:** `Player : ChaseCamera : CarPhysics : BasePlayer`, a single global instance (`RacingGameManager.Player`). Physics uses a variable timestep, reads `Input.*` directly and has no car-to-car collision. The car state is small: position, direction, up vector, speed, wheel position and the pitch spring.
- **The ghost:** `Replay` stores one `Matrix` every 0.2 s. The ghost is drawn in `GameScreen.Render` with `RacingGameManager.CarModel.RenderCar(0, color, shadowCarMode: true, matrix)` and is not rendered through the shadow path.
- **Hidden globals that matter for extra cars:** `Model.RenderCar` reads `RacingGameManager.Player.CarWheelPos`; the car color goes through the global `RacingGameManager.currentCarColor`; `Landscape.NewReplay` and `BestReplay`, the highscores and the `Sound` engine cues are all single-player globals.
- **Screens:** an `IGameScreen` stack (`RacingGameManager.AddGameScreen`) with the flow `MainMenu` → `CarSelection` → `TrackSelection` → `GameScreen`.

## Recommended architecture: client-authoritative "puppet" cars

Each machine simulates **only its own car** with the existing physics. Remote cars are **puppets**: they never go through `CarPhysics` and are only drawn from interpolated network state. This avoids the large refactor (extracting a `Car` class from the `Player` singleton) that a fully simulated multi-car race would need, which is why the first version is comparatively cheap.

- **Transport:** reference `MonoGame.Xna.Framework.Net` with the same `PackageReference` (version `1.0.0-preview.1`) that ShipGame uses. The Desktop `Program.cs` initialises the UDP SystemLink backend, copying the `MultiplayerSessionService` pattern. One host and the clients share one `NetworkSession` of type SystemLink with a maximum of 4 gamers.
- **Trust model:** clients are trusted (LAN, friends). There is no anti-cheat.
- **Clock and start:** the host sends a `RaceStart` message with the countdown, and clients start their 5 s zoom-in and traffic-light countdown on receipt (LAN latency is a few ms). Race times are measured locally, and the host ranks players by their reported finish times.

### Protocol

- **Car state, about 15 to 20 Hz, unreliable (or `InOrder`, so stale packets are dropped).** Sequence number, sender race time, position, forward, up, speed, wheel position, pitch, lap, checkpoint index, and a finished flag with the finish time. About 70 bytes per packet.
- **Events, reliable:** race start, player finished and results use `SendDataOptions.Reliable`, so the library's reliable channel acknowledges and resends them. Also repeat the finished flag and finish time in the periodic car-state packets, so state still converges if a peer stalls or rejoins.
- **Packet ids:** start the `RacePacketTypes` ids at 64 (see the packet id ranges above), and include a `ProtocolVersion` check and an `InProgress` session property as ShipGame does.
- **Smoothing of remote cars:** (model it on ShipGame's `RemoteShipInterpolator`) keep the last two or three snapshots, render about 100 ms in the past, lerp the position and slerp the orientation as a quaternion. Do not reuse `Matrix.Lerp` from `Replay`, because it distorts rotation.

## Work breakdown (first version: 4 players, Desktop)

| # | Phase | Main changes | Rough effort |
|---|---|---|---|
| 0 | Library and session plumbing | `PackageReference` in `Core.csproj`; new `Core/Multiplayer/` with `MultiplayerSessionService.cs`, `RacePacketTypes.cs`, `RacePackets.cs`, a `RacePacketDispatcher` and `RaceNetSession.cs` (modelled on ShipGame's `NetworkManager`: host, join, send, dispatch on the main thread); backend initialisation in the Desktop `Program.cs` | 1 to 2 days |
| 1 | Menus and lobby | Mode choice (Single / Multiplayer) before `CarSelection`; text-based `MultiplayerMenuScreen`, `LanBrowserScreen` and `LanLobbyScreen` (after ShipGame's `ScreenLanBrowser` and `ScreenLanLobby`) (player names, car and color per player, host Start); a start-grid lane per slot, from the track start position plus a lateral offset | 2 to 3 days |
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

- **Reliable and unreliable sends must be chosen per message:** use reliable for events and unreliable for car state. A reliable channel delivers in order, so a stalled peer can delay its own later reliable messages. Keep the reliable traffic small, and handle the stall timeout (ShipGame uses 5 s).
- **Fixed discovery ports (31337 and 31338):** two instances on one machine can collide. Test on two machines, or add a development option for the ports.
- **Firewalls and access-point client isolation** can block LAN broadcast. macOS may show a "local network" prompt the first time.
- **Network events arrive on background threads** in the library. Only touch game state from `Update`, using the ShipGame pattern: queue the packets and drain them on the main thread.
- **The Windows (DirectX) project** would pull in the library, which references `MonoGame.Framework.DesktopGL` privately. ShipGame's Windows project already does this, but the Racing Game Windows project needs to be checked.
- **Variable-timestep physics** is fine here because each machine simulates only its own car. It is also a reason not to use lockstep or server-authoritative simulation.
- **Global state to keep in mind:** `currentCarNumber` and `currentCarColor`, `Player.CarWheelPos` in `RenderCar`, `Landscape.NewReplay` and the checkpoint progress (using the local player only is fine for the first version), and the `Sound` engine cues.

## Files involved

- **New:** `Core/Multiplayer/*` (session service, packets, `RaceNetSession`, `RemoteCar`) and new screens under `Core/GameScreens/` (`MultiplayerMenuScreen`, `LanBrowserScreen`, `LanLobbyScreen`, `MultiplayerGameScreen` and a results screen).
- **Changed:** `Core/Core.csproj` (package reference), the Desktop `Program.cs` (backend initialisation), `Core/GameScreens/MainMenu.cs` or `CarSelection.cs` (mode choice), `Core/Graphics/Model.cs` (`RenderCar` with per-car wheel position and color), `Core/GameScreens/GameScreen.cs` (base for the multiplayer screen), `Core/GameLogic/Player.cs` and `BasePlayer.cs` (DNF instead of game over), `Core/Graphics/UIRenderer.cs` (HUD additions) and `Core/Landscapes/Landscape.cs` and the track code (start-grid lanes).
- **Reuse from ShipGame:** `ShipPacketDispatcher` and `PacketAudience`, `NetworkManager`, `MultiplayerSessionService` (including `ProtocolVersion`, `InProgress` and `DirectSession`), `RemoteShipInterpolator`, and the screen flow of `ScreenLanBrowser`, `ScreenLanLobby` and `ScreenGame`. Copy the code and rename it; it is not a shared library.

## Verification plan

1. Round-trip tests for each packet's `Serialize` and `Deserialize`.
2. A one-machine test with two instances on loopback (needs the port option), checking host, join, lobby and start.
3. A two-machine LAN test: join, synchronised countdown, 4 cars visible, smooth remote motion, and laps and finish ranking agreeing on every machine.
4. Resilience: run with the library's `SimulatedPacketLoss` (for example 10 to 20 percent) and confirm race start, finish and results still work. Kill a client mid-race (the others continue and its car disappears or is marked DNF). Quit the host (clients return to the menu with a message).
5. Regression: single player, including the ghost, the highscores and the replay car in the menus, must be unchanged.
