# RacingGame Track Format and Loading Behaviour — Phase 0 Findings

Part 1 of Phase 0 in [Track-Editor-Plan.md](Track-Editor-Plan.md). Derived from reading `Core/Tracks/*`, `Core/Landscapes/Landscape.cs`, `Core/GameLogic/Replay.cs` and measuring the three shipped `.Track` files. Everything here comes from the source and the files; **nothing was run**. Items marked **(unverified)** need a test.

---

## 1. Coordinate system and units

- **Z is up.** X/Y are the ground plane (the data was exported from 3ds Max).
- Terrain: 257×257 height samples, **10 units per cell**, so the terrain spans **X, Y = 0..2560**. Height range is **0..300** (`MapZScale`).
- Shipped tracks sit inside the map: X 217–2297, Y 260–2308, Z 28–217.
- Terrain lookups (`GetMapHeight`) **wrap** (modulo) outside the map, but the terrain mesh does not. Tracks must stay inside 0..2560 or the road height logic will use wrapped terrain that isn't there.
- Road width = `13.25 × roadWidth`, where `roadWidth` is clamped to **0.25–2.0**. So the road is **3.3 to 26.5 units wide**, default 13.25.

## 2. The `.Track` file

XML, `XmlSerializer` of `TrackData`, root `<TrackData>`, four lists:

| Element | Item | Fields |
|---|---|---|
| `TrackPoints` | `Vector3` | `X, Y, Z` |
| `WidthHelpers` | `WidthHelper` | `pos` (Vector3), `scale` (float) |
| `RoadHelpers` | `RoadHelper` | `type` (`Tunnel`, `Palms`, `Laterns`, `Reset`), `pos` (Vector3) |
| `NeutralsObjects` | `NeutralObject` | `modelName`, `matrix` |

**Checkpoints (implemented 2026-11-05):** optional `Checkpoints` element, a list of `<float>` fractions of the lap (0..1, increasing, measured from the start line along the road, each between 2% and 98% and at least 1% apart). Absent: the game places one every 500 units (as before). Present but empty: no checkpoints. The game keeps the automatic margins (not within about 60 units of the start or finish, not closer than about 50 units together) and puts a banner across the road at each (not in loopings). They split the lap time, drive the best-lap comparison, and a lap counts only after all but one were passed.

**Heightmap (D11, implemented):** optional `Heightmap` element, deflate-compressed 257×257 bytes (`byte[]`, base64 in XML). `TrackData.CompressHeightmap/DecompressHeightmap`; wrong size is rejected. Missing: shipped terrain.

**Metadata (D10, implemented):** optional top-level elements `FormatVersion`, `Name`, `Author`, `OriginalAuthor`, `Description`, `Created`, written before `TrackPoints`. Verified by loader test: they load when placed before the lists, and also after them; the three shipped tracks (no metadata) still load. `Name` overrides the file name in the in-game list.

`matrix` is a serialized XNA `Matrix`: `M11..M44` plus derived `Up, Down, Right, Left, Forward, Backward, Translation` sub-elements. Shipped files write all of them; a writer should do the same to be safe. **Verified:** the reader accepts a matrix with only `M11..M44` (tested with a hand-written file through `CustomTracks.Load`).

Shipped content:

| | Beginner | Advanced | Expert |
|---|---|---|---|
| Control points | 18 | 41 | 89 |
| Point spacing min / avg / max | 107 / 188 / 378 | 37 / 132 / 366 | 50 / 125 / 294 |
| Polyline length | ~3.3k | ~5.4k | ~11.2k |
| Width helpers | 4 | 5 | 6 |
| Road helpers (Tunnel/Palms/Laterns/Reset) | 1/1/4/2 | 2/2/4/4 | 10/4/8/13 |
| Neutral objects | 58 | 44 | 72 |

Observations:
- The loop is **implicitly closed**: the last point does not repeat the first, there is a gap of 137–159 units, and the spline wraps around.
- **`points[0]` is the start/finish line and start direction.** The car starts there (`Track.StartPosition/StartDirection/StartUpVector`).
- Each file's first neutral object is named after the track (e.g. `TrackBeginner`) with an identity-ish matrix. It matches no model, is silently ignored, and is an export artifact. The editor shouldn't emit it.

## 3. What the game does with the data (in order)

### 3.1 Terrain collision fix-up (modifies your points)
For every control point, Z is raised to at least `terrainHeight(X,Y) + 4.5`. Then for 24 interpolation steps between each pair, the terrain is sampled at 4 offsets (±5 units) and **both endpoints are raised** if the road would be below terrain + 3.2.

Consequence: **the road never clips into terrain; it lifts** (making bridges/ramps over hills, and ratcheting heights upward). Editor point heights are therefore not what the game uses. For exact preview and for helper placement, the editor needs to run the same algorithm (see §3.5).

### 3.2 Automatic loop-the-loops (a trap for generated tracks)
For control points `1 .. n-4`, if a step has `|dz|/2 > horizontalDistance` **and** the next step roughly reverses it (`|dz1+dz2| < |dz|/2`), the game replaces three points with a **vertical looping** (9 points, radius = `|dz|`). Loops are only meaningful if the author intended them. The editor must either **forbid steep up-then-down pairs** (validation) or deliberately offer a "loop" block that produces this pattern. The first and last 3 points are exempt.

### 3.3 Spline
Closed **Catmull-Rom** through all points. Sampling: `40 iterations per 100 units` of point distance (minimum 1), so ~2.5 units per sample. Track `Length` = sample count × 2.5. Up/right/dir vectors are derived and smoothed. Roads can twist near loops. Widths come from helpers (below) and blend into the start width over the last ~7 samples so the loop closes cleanly.

### 3.4 Width helpers
Each sample takes influence from any helper within **25 units (3D distance)**: `influence = 1 − dist/25`. The width is then a slow 90/10 blend toward the target, clamped 0.25–2.0. So a width change spans roughly 25 units each side of the helper and is smoothed. Shipped scales: ~0.9–1.8.

### 3.5 Road helpers: a small state machine
Walking the samples in order, a helper "fires" when a sample is **within 25 units (3D)** of it; each helper fires once and is consumed (first matching in list order).
- No region open → region opens with that helper's type.
- Region open → the current region closes at this sample, then: if the helper is `Reset`, nothing opens; otherwise a new region of the helper's own type opens.
- A region still open at the end is closed at `count − 3`.

So to author a tunnel: place `Tunnel` at the start and `Reset` at the end. Regions chain: `Palms` → `Laterns` → `Reset` is valid. Helper positions are matched against **adjusted** (terrain-lifted) road heights, so a helper placed at the unadjusted Z can miss by more than 25 units on steep lifts. **The editor/compiler must place helpers on the adjusted road.**

Regions drive: tunnel geometry (`Tunnel`), and auto-placement of palms (`Palms`, skipped where road is >11 units above ground, loops excluded) and lanterns (`Laterns`).

### 3.6 Things generated automatically (the editor does not author these)
- **Checkpoints** every 500 units (skipping the last 24 samples and loops), each with a banner across the road.
- **Curve-warning signs** (every ~24 units where the curve is tight).
- **Guard rails, support columns, start lights, goal** (`StartLight*`, `Goal` are created by `Track.cs`; so start-line scenery isn't in the file).
- Brake-track marks at runtime.

### 3.7 Neutral objects (scenery)
- `modelName` is matched first against **combi models** (`CombiPalms`, `CombiBuildings`, …), then against the loaded model list. Unknown names are **silently dropped** (log in DEBUG only).
- Legacy aliases are remapped: `OilWell→OilPump`, `Palm→AlphaPalm`, `PalmSmall`/`AlphaPalm4→AlphaPalmSmall`, `Casino→Casino01`, `Combi→CombiPalms`.
- Z is clamped to the terrain, everything is scaled by an extra **×1.2**, and objects closer than `size²/4` to an existing one are **skipped** (except Banner*, Sign*, StartLight*).
- Large `Hotel*` / `Building*` objects get shadows; the first one also defines the "city" ground plane (see §4).
- Object vocabulary used by shipped tracks (the usable v1.x palette, 30+ names): Windmill, Hotel01/02, Building/3/5, Casino/Casino01, OilTanks, Kaktus family, AlphaPalm/3, AlphaDeadTree, SharpRock/2, Ruin, SandCastle, Trashcan, Hydrant, Roadsign/2, AlphaTrain, plus combis CombiBuildings, CombiOilTanks, CombiPalms/2, CombiRuins/2, CombiSandCastle, CombiStones/2.

## 4. Coupling that blocks custom tracks (answers for risk R1/R2)

| # | Coupling | Where | Needed change |
|---|---|---|---|
| C1 | Track loaded via `TitleContainer.OpenStream("Content/<name>.Track")` | `TrackData.Load` | Add a loader from a file/stream path for `Tracks/` |
| C2 | Track name = `"Track" + Level.ToString()`; `Level` is a 3-value enum | `Landscape.ReloadLevel`, `RacingGameManager`, `TrackSelection` | Replace with a track descriptor (id, name, source) |
| C3 | ~~**One terrain**~~ **RESOLVED 2026-11-05** (terrain build split into `BuildTerrain`/`ApplyTerrain`; see plan §11). Original problem: **one terrain**: `LandscapeHeights.data` is read **only in the `Landscape` constructor**, the vertex/index buffers and `mapHeights` are built once; `ReloadLevel` never touches them | `Landscape.cs` ctor | Split terrain build out of the ctor so it can be rebuilt (and disposed) per track |
| C4 | ~~City ground plane~~ **RESOLVED 2026-11-05** (plane belongs to the terrain: shipped terrain keeps the original, a custom heightmap gets one from the track's first big building or none). Original problem: city ground plane is created once in the ctor from the first "big building" | `Landscape.cs` ~950 | Recompute on track load, or make it optional |
| C5 | Replays: three hard-coded filenames, `trackNum` 0–2 | `Replay.cs` | Key by track id (e.g. hash of file) |
| C6 | High scores: arrays indexed by level (`NumOfHighscoreLevels`), stored in `GameSettings` | `Highscores.cs` | Per-track id storage, or no scores for custom tracks in v1.x |
| C7 | `XmlSerializer` for `TrackData` | `TrackData.Load` | Fine on desktop; **risky under WebGL (WebAssembly) trimming (unverified)**. Consider a JSON/hand-written reader for the web editor |
| C8 | Terrain textures, sky and city ground are fixed | landscape materials | v1.x: one visual theme (desert/city) only |

Terrain **file format** (for per-track heightmaps): raw **257×257 bytes**, row-major (`index = x + y*257`), `height = byte/255 × 300`. The shipped file is exactly 66,049 bytes. A per-track heightmap can reuse this verbatim: an editor can emit it as a sibling `<name>.Heights` file or a base64/embedded field. Resolution is coarse (10 units/cell, 256 height levels = ~1.2 units steps), which is fine for gentle hills but not sharp features.

## 5. Implications for the editor's compiler

1. **Emit control points, not samples.** Target spacing roughly **35–380 units** (shipped range). Denser is allowed, but loops auto-trigger on steep pairs.
2. **Close the loop by wrapping**: don't repeat the first point; keep the last→first gap similar to normal spacing (shipped gap ≈ 140–160 units).
3. **Block → points**: sample each block's centre line at ~100-unit spacing (corners: ≥1 point per ~20° of arc) so Catmull-Rom reproduces the arc without overshoot.
4. **Run the same terrain lift** (§3.1) in the editor so the previewed and exported heights match, and so helper positions land within 25 units of the real road.
5. **Helpers:** emit in track order; tunnels = `Tunnel` + `Reset`; width changes = `WidthHelper` with scale 0.25–2.0 at the block boundaries, noting the 25-unit influence radius and slow blend (don't expect sharp width changes under ~50 units).
6. **Validation:** keep inside 0..2560; forbid steep up-then-down pairs (accidental loops); minimum control point spacing; max slope; at least 3 points; avoid helper overlaps (<25 units apart will consume each other).
7. **Scenery:** restrict to the palette in §3.7; warn when two objects are closer than `size²/4` (the second is dropped).

## 6. Answers to the Phase 0 questions

| Question | Answer |
|---|---|
| Is point-to-point supported? | **No.** Everything is a closed Catmull-Rom loop with wrap-around indexing (consistent with D5). |
| How does terrain interact with the road? | The road is **lifted** above terrain; it never carves or clips. Terrain is global and loaded once per process. |
| Spline density? | 3.3k–11k units of track from 18–89 control points; see §2. |
| Is a per-track heightmap feasible? | **Yes, but needs refactoring** of the Landscape constructor (C3, C4). Format is trivial (§4). |
| Can existing helpers/scenery be reused? | Yes; the vocabulary in §3.7 covers the v1.x art requirement (D4). |

## 7. Not yet verified (candidates for the next steps)

- ~~Hand-written XML with only `M11..M44`~~ **verified OK** (loader test). Also verified end to end: the game built and played it (user-confirmed).
- Real lap length vs lap time and a sensible min/max length for the validator (needs a driven lap).
- Minimum viable corner radius for the car at speed (needs driving tests, or the physics code).
- Whether `XmlSerializer` works in the MonoGame WebGL target (needs the spike).
- How the hard-coded 1.2× object scale and `size²/4` rule interact with combi models.
- Whether the terrain mesh/texture mapping tolerates a different heightmap without visual artifacts (city plane, detail texture).
