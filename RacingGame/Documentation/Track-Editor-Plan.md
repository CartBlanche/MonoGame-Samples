# Track Editor — Analysis and Plan

Status: proposal / planning. **Phase 0 part 1 (format spec) is done: see [Track-Format-Spec.md](Track-Format-Spec.md).** Part 2 (WebGL spike) is unblocked: it works on 3.8.6 with `MonoGame.Runtime.Browser.WebGL2` (no need to wait for 3.8.7); waiting on WebGL binaries. Nothing is implemented in the game yet.
Scope: a block-based, browser-first editor for creating race tracks for RacingGame, including a way to produce facsimiles of real-world circuits.

---

## 1. Summary

- RacingGame already builds its road **procedurally from a small data file** (`*.Track`, XML). A track is a spline plus a few annotation lists, not baked geometry. This is the ideal target for an editor: the editor only has to *write that file*; the game does the rest.
- The recommended editor is a **web app (TypeScript + three.js)** that edits a **block-based track model** (straights, curves, ramps, tunnels, etc. that snap together) and **compiles it down to the existing `TrackData` XML**.
- Facsimiles of famous circuits come from a **separate import pipeline** (public geodata / hand-traced outlines → simplified spline → editable blocks). The block editor is then used to clean up the result.
- The main work on the game side is small: load tracks from outside the Content pipeline, and decide how custom tracks relate to the single shared landscape heightmap.
- Suggested team: **9 agent roles** (6 core, 3 supporting). See §8.

---

## 1a. Decisions (from review, 2026-11-04)

| # | Decision |
|---|---|
| D1 | Custom tracks must play in **release desktop builds**. Workflow is file based: the editor downloads a `.Track` file and shows per-platform "save it here" instructions. The game scans a `Tracks/` folder at runtime. Android/iOS follow the same model where the OS allows (see §3.6). |
| D2 | **Per-track heightmap: yes**, and make it a feature (hills, dips, banked terrain), not just flatten-under-road. |
| D3 | **Generic, humorous play-on-words names** for famous-circuit facsimiles (e.g. "Monza" → pun-style names). No official names, logos or branding. |
| D4 | v1.x uses **existing art only**. Asset import is a later feature if the game proves popular. |
| D5 | **Closed loops only** for v1.x. |
| D6 | Web stack: **MonoGame WebGL** (works on 3.8.6 via the `MonoGame.Runtime.Browser.WebGL2` package) using the WebGL project template (MonoGame.Templates PR #72). See §3.3: this makes the editor able to reuse the game's own `Track` code. |
| D8 | **Interim target: desktop first.** The editor is built as a desktop MonoGame app now; the WebGL target is added once the spike (§3.3) passes. |
| D9 | **UI toolkit: Gum** (`MonoGameGum` package; docs: https://docs.flatredball.com/gum/code/monogame) for toolbar, block palette, inspector, validation list and dialogs. The 3D viewport, block dragging and snapping are drawn directly with MonoGame. Fallback: Myra. Avoid ImGui.NET (native dependency, likely a problem under WebAssembly). |
| D10 | **Every track file records its creator** (implemented 2026-11-04). Optional elements added to `TrackData`, written before `TrackPoints` in this order: `FormatVersion` (int, 1), `Name`, `Author`, `OriginalAuthor`, `Description`, `Created` (ISO date). All free text. Shipped tracks load unchanged (missing elements are ignored). Decisions: `Author` is free text (no verified identity); editing someone else's track **keeps `OriginalAuthor`** and sets `Author` to the editor; the in-game custom track list shows "Name by Author". The editor must require `Author` before export. |
| D11 | **One file per track, heightmap embedded.** The `.Track` file carries an optional `Heightmap` element: the 257×257 byte grid (same layout as `LandscapeHeights.data`), deflate-compressed, stored as a `byte[]` (XmlSerializer writes base64). Expected size: a few KB for smooth terrain (raw 66 KB, 88 KB as base64). Missing element means the default terrain, so the three built-in tracks load unchanged. Rejected alternatives: sibling `.Heights` file (easy to separate), zip package (revisit if asset import arrives, D4), procedural recipe (needs bit-identical generation on all platforms). Nobody hand-edits the blob: tracks are made by the editor or by agents/scripts. |
| D12 | **Command-line track tool** (read/write/validate `.Track`, including the heightmap), sharing code with the editor's exporter. Includes "dump heightmap to PNG" and "set heightmap from greyscale PNG" so humans and agents can view and replace terrain as a picture. The PNG is an import/export view only, never shipped with the track. Doubles as the test-track generator. |
| D13 | **Commit policy:** the whole track-editor change (loader, metadata, heightmap, editor, docs) goes in **one commit**, made only when the user says so. Do not commit in pieces. |
| D7 | Community gallery with ranking is wanted; feasibility on itch.io is covered in §3.7. |

## 2. What the game consumes today

Findings from `Core/Tracks/`, `Core/Landscapes/` and `Core/Content/`:

| Item | Where | Notes |
|---|---|---|
| Track file | `Core/Content/Track{Beginner,Advanced,Expert}.Track` | XML, `XmlSerializer` of `TrackData`. 60–105 KB each. Loaded via `TitleContainer` from `Content/`. |
| `TrackPoints` | `TrackData.cs` | `List<Vector3>` — the spline control points (exported from 3ds Max originally). |
| `WidthHelpers` | `TrackData.cs` | `{pos, scale}` — local road width multipliers along the spline. |
| `RoadHelpers` | `TrackData.cs` | `{type, pos}`; types: `Tunnel`, `Palms`, `Laterns`, `Reset`. Tunnels are start/end pairs. |
| `NeutralsObjects` | `TrackData.cs` | `{modelName, matrix}` — scenery placed by hand (buildings, signs, rocks, etc.). |
| Combi models | `Core/Content/*.CombiModel` | Reusable scenery clusters (palms, ruins, hotels…), also used by the random object generator. |
| Scenery models | `Core/Content/Models/*.X` | ~60 models: Banner*, Building*, Casino, Hotel, Kaktus*, Laterne, Roadsign, Sign*, StartLight*, Goal, Blockade, etc. |
| Road generation | `Track.cs` (1.4k lines), `TrackLine.cs`, `TrackVertex.cs`, `GuardRail.cs`, `TrackColumns.cs` | Spline → vertices, guard rails, support columns, tunnels, checkpoints, start position. |
| Landscape | `Landscape.cs`, `LandscapeHeights.data` | A **single** 257×257 heightmap, `MapZScale = 300`. Selected by `RacingGameManager.Level` (Beginner/Advanced/Expert), which also picks the track. |
| Derived data | `Track.cs` | Start position/direction/up, length, segment count and checkpoint positions are all **computed**, not stored. |

### Implications for the editor

1. **The output format is tiny and already defined.** No new road renderer is needed.
2. **Blocks are an editor concept, not a game concept.** A block track is compiled to a spline + helpers. The game never needs to know about blocks.
3. **Track points live in landscape space**, and the landscape is one shared heightmap. A custom track on that terrain will clip into hills or float unless the editor accounts for it (see §3, risk R1).
4. **Tracks must be loadable at runtime** from outside the content pipeline (user file, URL, share code). Today they are only read from `Content/`.
5. `Level` is a fixed 3-value enum; custom tracks need a different selection path.

---

## 3. Key design decisions

### 3.1 Block model

A track is an ordered **chain of blocks**. Each block has an entry and an exit **socket** (position, heading, pitch, bank, width). Two blocks snap when exit socket N equals entry socket N+1 within tolerance.

Initial block catalogue:

| Category | Blocks |
|---|---|
| Straights | short / medium / long; width-taper |
| Corners | 30°, 45°, 90°, 180° hairpin; left/right; radius S/M/L; banked variants |
| Elevation | ramp up/down, crest, dip, S-bend with grade |
| Special | tunnel (start+end → `Tunnel` helper), start/finish, jump (if physics allows), reset point |
| Scenery packs (attach to blocks) | palms, lanterns, buildings, signs (map to `RoadHelpers` + `NeutralsObjects` + combi models) |

Branching/forks are **out of scope** for v1 (the game's `Track` is a single loop/line).

Loop closure: the editor must detect the start/finish gap and offer **"auto-close"** (solve for a connecting curve) or flag it as invalid.

### 3.2 Compilation: blocks → `TrackData`

0. Write the metadata block (D10): `Author` (required), `Name`, `Description`, `Created`, `FormatVersion`.

1. Walk sockets, emit centre-line samples per block (clothoid/arc/line math).
2. Re-sample to match the density the game's spline expects (derive from the three shipped tracks).
3. Emit `WidthHelpers` from width changes, `RoadHelpers` from tunnel/scenery blocks, `NeutralsObjects` from placed props.
4. Serialize to the **exact XML schema** `XmlSerializer` expects (including the `Matrix` element layout).
5. **Golden test:** import each shipped `.Track` into the editor's internal model and re-export; the game must load the result identically.

### 3.3 Runtime: browser + game parity (revised for D6)

With MonoGame WebGL, the editor can be a **C# MonoGame WebGL app that references the game's own `Core/Tracks` and `Landscape` code**. That gives exact road generation, tunnels, rails and terrain in the preview, with no second implementation to keep in sync.

| Option | Pros | Cons |
|---|---|---|
| **A. MonoGame WebGL editor sharing game code** (now recommended) | Exact parity; one language; compiler/importer reuse `TrackData` directly; no XML-writer drift | Needs the WebGL2 runtime package (works on 3.8.6; binaries being sourced); RacingGame's custom shaders/GL features must work under WebGL (the port already needed GL fixes, see memory notes); UI toolkit is less rich than HTML; larger download; mobile browser performance unknown |
| B. TypeScript + three.js preview | Light, flexible HTML UI, available today | Second implementation of the road; parity tests needed; format-writer drift risk |
| C. Hybrid: HTML UI shell + MonoGame WebGL canvas | Best UI, exact preview | Interop complexity |

Recommendation: **A**, with a **Phase 0 spike**: get the existing RacingGame road rendering in a WebGL build. If shaders or content loading block it, fall back to B or C.

What the WebGL template (MonoGame.Templates PR #72, a Draft at the time of reading) requires:
- `Microsoft.NET.Sdk.WebAssembly`, `net9.0`, `RuntimeIdentifier browser-wasm`, `WasmBuildNative`, `AllowUnsafeBlocks`.
- Packages: `MonoGame.Framework.Native` 3.8.*, `MonoGame.Runtime.Browser.WebGL2` 3.8.*, `MonoGame.Content.Builder.Task` 3.8.*.
- The `wasm-tools` workload (`dotnet workload install wasm-tools`).
- Entry point: `BrowserGame.RunAsync(new Game1())`; a `wwwroot/index.html` that imports a generated `_monogame/monogame-web-host.js`.
- **Content is built with `/platform:WebGL2` and `/profile:Reach`.**

Spike risks found by comparing with RacingGame (which is `net10.0`, DesktopGL, **`/profile:HiDef`**):
1. **Reach vs HiDef.** RacingGame builds all content as HiDef and has 10 custom `.fx` shaders (normal mapping, shadow map + blur, sky cube map, glow post-processing, reflection). Whether these compile and run for WebGL2/Reach is unknown and is the biggest risk. Shader and render-target format limits are the likely failure points.
2. **Target framework.** Template is `net9.0`, game is `net10.0`; the editor project may need to multi-target or pin.
3. **Toolchain.** Needs the `wasm-tools` workload installed locally.
4. **Serialization.** `XmlSerializer` under WebAssembly trimming (see Track-Format-Spec C7).
5. **Loading from files.** Browsers have no folder access; the editor works with in-memory data and downloads, consistent with D1.

Spike scope (smallest useful test): (a) build the template, confirm it runs, **and that Gum loads and a basic Gum screen (button, scrolling list, text box, slider) works in the WebGL build, including mouse and touch input**; (b) render the landscape + one shipped track with simplified shaders (or a fallback "unlit" effect path); (c) report which shaders fail. A reduced-fidelity editor preview (flat-shaded road, simple terrain) is an acceptable fallback that still uses the real `Track`/`TrackLine` geometry code.

### 3.4 Landscape / terrain (risk R1)

Options, in order of effort:

1. **Flat or editor-chosen terrain mask.** Custom tracks set a flat/zero-height ground and skip the heightmap. Needs a game change in `Landscape`.
2. **Per-track heightmap.** Track file carries (or references) its own 257×257 heightmap; editor includes a basic terrain tool (flatten under road, raise/lower, smooth). Cleanest long-term.
3. **Reuse shipped heightmap, snap the road to it.** Cheapest, but limits layout to what the terrain allows.

Decision D2: **option 2**. v1.x editor features: auto-flatten under road, raise/lower/smooth brushes, noise/hill presets, optional DEM import for real elevation. Track file gains an optional heightmap (decided: embedded in the `.Track` file, see D11).

### 3.5 Famous-track facsimiles

Pipeline (offline or in-browser):

```
source outline ──► clean/simplify ──► fit to blocks ──► human cleanup in editor
(GPX/GeoJSON,       (projection,       (arc + straight     (fix chicanes,
 OSM way,            smoothing,         segmentation,       add scenery,
 traced image)       Douglas-Peucker)   curvature fit)      tune width)
```

- **Source data:** OpenStreetMap `highway=raceway` / `sport=motor` ways (ODbL licence), user-supplied GPX, or a hand-traced overlay image the user draws over.
- **Elevation:** optional, from public DEM (SRTM) sampled along the path, scaled into game units.
- **Fitting:** segment the curve into lines and constant-curvature arcs by curvature analysis, then snap to the nearest catalogue block lengths/radii. Provide a **fidelity slider** (exact spline ↔ clean blocks).
- **Scale:** real circuits are 3–7 km; the game tracks are far smaller. Provide a **scale-to-fit** with a warning on minimum corner radius vs. car turning ability.
- **Naming (D3):** generic pun names only, e.g. a sample pack with titles in the spirit of "Mon-Zzzz-a" or "Spa-ghetti". The editor should ship a name generator/suggestions. (Placeholder examples; final names are a content task.)
- **Legal/ethical:** circuit layouts are factual geography, but **names, logos and trademarks** (e.g. official circuit or series names, sponsor branding) are not. Ship facsimiles as "inspired by" with generic names unless licensed. Keep OSM attribution. Needs a decision (see §10).

### 3.6 Track delivery and folder locations (D1)

The game reads `*.Track` (plus optional heightmap) from a user-writable `Tracks/` folder and lists them under a "Custom tracks" menu. The game should also **show the exact folder path in-game**, so instructions never go stale.

Proposed locations (to confirm in Phase 1; not yet verified in code):

| Platform | Proposed folder | Notes |
|---|---|---|
| macOS | `~/Library/Application Support/RacingGame/Tracks/` | **Confirmed** (`LocalApplicationData/RacingGame/Tracks`, next to settings). Editor message tells users to open via Finder > Go > Go to Folder |
| Windows | `%LOCALAPPDATA%\RacingGame\Tracks\` | Expected from `LocalApplicationData` (unverified on Windows) |
| Linux | `~/.local/share/RacingGame/Tracks/` | Expected from `LocalApplicationData` (unverified) |
| Android | App-specific external storage (`.../Android/data/<package>/files/Tracks/`) | Android 11+ restricts access to this folder from file managers; may need an in-game "Import track" file picker instead. Needs a spike. |
| iOS | App `Documents/Tracks/` with file sharing enabled (visible in Files app) | Requires `UIFileSharingEnabled` |

The editor shows the platform-specific message (detected from the browser), with a copy-path button. Also consider an in-game **"Import from file"** picker, which sidesteps path problems on mobile.

### 3.7 Community gallery and ranking (D7)

itch.io HTML5 pages are **static hosting only**: no server code and no database. Ranking therefore needs an external service. Options:

| Option | Feasibility | Effort | Notes |
|---|---|---|---|
| Curated gallery in a **GitHub repo** (tracks submitted by PR, ranked by reactions/stars) | High | Low | No backend; moderation by review; not slick |
| **Small hosted backend** (Cloudflare Workers + D1/KV, Supabase or Firebase) called from the itch.io page | High technically | Medium | Needs anonymous voting, rate limiting, abuse/spam handling, a moderation queue, privacy policy |
| itch.io comments/devlogs/community only | Trivial | None | No real ranking |
| Discord/forum with manual leaderboard | Trivial | Low | Manual |

Caveats: itch.io embeds run in an iframe, so cross-origin calls need CORS on the backend; the editor can't write to the user's disk, so "download" is a file save. User-uploaded content carries moderation and legal responsibility.

Recommendation: **v1.x = files only. v1.5 = GitHub-hosted curated gallery. v2 = small backend with voting** once there is an audience.

---

## 4. Architecture

```
┌──────────────────────── Browser (static site / PWA) ────────────────────────┐
│  UI (React or Svelte)                                                       │
│   ├─ Block palette  ├─ 3D viewport (three.js)  ├─ Inspector  ├─ Validation   │
│  Core (pure TypeScript, no DOM — shared with CLI/tests)                     │
│   ├─ Track model (blocks, sockets, snap rules, undo/redo)                   │
│   ├─ Geometry (lines, arcs, clothoids, banking, closing solver)             │
│   ├─ Compiler: blocks → TrackData (XML writer)                              │
│   ├─ Importer: TrackData → blocks (golden-test + edit shipped tracks)       │
│   └─ Facsimile import (GPX/GeoJSON/image → simplified path → blocks)        │
│  Storage: IndexedDB + export file (.rrtrack JSON + .Track XML)              │
└─────────────────────────────────────────────────────────────────────────────┘
                 │  export .Track (+ optional heightmap, props)
                 ▼
┌──────────────────────── RacingGame (C#/MonoGame) ───────────────────────────┐
│  CustomTrackLoader: file/URL/share-code → TrackData                          │
│  Landscape: per-track heightmap or flat ground                               │
│  Track selection: "Custom tracks" entry (replaces fixed 3-value enum use)    │
└─────────────────────────────────────────────────────────────────────────────┘
```

UI layer (D9): Gum screens/components for all panels; each block type is a reusable Gum component (icon, label, drag source). Gum's behaviour is unverified for: scrolling lists, text input, drag-and-drop and touch, which the first spike must confirm (docs page didn't cover them).

Native project file `.rrtrack` (JSON, versioned, human-diffable) holds the block chain; the `.Track` XML is a **build artifact**. Keeping the source of truth as blocks makes tracks re-editable.

Optional later: a tiny hosted service for **share links and a gallery** (not needed for v1; share via file/URL-fragment first).

---

## 5. Validation rules (editor must enforce)

- Track closes (or is explicitly point-to-point if the game supports it — to verify).
- Minimum corner radius ≥ car turning limit; maximum slope; maximum bank.
- No self-intersection (outside of explicit bridges, if ever supported).
- Width within the range the game supports; tunnel start/end paired.
- Length within a playable range (target lap time bounds).
- Props not on the road; object count budget (performance on mobile).
- Final gate: **round-trip load** in the game test harness without exceptions.

---

## 6. Phased plan

| Phase | Deliverable | Exit criteria |
|---|---|---|
| **0. Discovery** (part 1 done; part 2 WebGL spike pending) | Written spec of the `.Track` XML (incl. `Matrix` layout), spline density, units, how terrain interacts with the road, whether point-to-point is supported | Spec reviewed; answers to R1/R2 |
| **1. Game-side loader** (started: desktop loader + simple list screen implemented, uncommitted; see §11) | `CustomTrackLoader` + "Custom" selection path, flat/per-track terrain option | A hand-edited `.Track` loads and plays |
| **2. Core library** | TS track model, geometry, compiler, importer | Golden test: all 3 shipped tracks round-trip identically |
| **3. Editor MVP** | Browser app: palette, snapping, 3D view, undo/redo, validation, export | A new user builds and exports a playable loop in <10 min |
| **4. Scenery + terrain** | Props, combi models, tunnels, auto-flatten terrain | Tracks look finished, not bare |
| **5. Facsimile import** | GPX/GeoJSON/image tracing → blocks, fidelity slider, scale-to-fit | 3 recognisable circuits created end to end |
| **6. Polish and share** | Sharing, gallery (optional), docs/tutorial, mobile/touch UX | Public beta |

Phases 1 and 2 can run in parallel.

---

## 7. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | Single shared heightmap makes arbitrary tracks clip or float | Per-track heightmap / flat ground; early prototype in Phase 1 |
| R2 | Original road code has implicit assumptions (spline density, closed loop, checkpoint spacing) that aren't documented | Phase 0 spec; round-trip tests; fuzz generated tracks through the game |
| R3 | three.js preview diverges from actual in-game road | Parity checks (compare centre-line, width, length numerically against game output) |
| R4 | Real circuits are too tight/large for the car physics | Scale-to-fit, radius warnings, fidelity slider |
| R5 | Licensing of circuit names/branding, OSM attribution, DEM data | Decide policy up front; generic naming |
| R6 | Scope creep (forks, multi-surface, AI racing lines, online gallery) | Explicit non-goals in v1 |
| R7 | XML serialization quirks (`XmlSerializer` + `Matrix` fields) break hand-written writer | Golden-file tests against real game output |

**Non-goals for v1:** branching tracks, new road/car art, AI line editing, an online gallery/moderation, new scenery models.

---

## 8. Agent roles required

**9 roles: 6 core + 3 supporting.** One agent per role (several can run in parallel); roles marked ⟂ are independent and parallelisable.

### Core

| # | Agent | Responsibility | Inputs → Outputs | Depends on |
|---|---|---|---|---|
| 1 | **Format & Game-Integration Analyst** (C#) ⟂ | Phase 0 spec of `.Track` XML, spline/terrain assumptions; implement `CustomTrackLoader`, track selection path, per-track terrain in the game | Game source → spec doc, C# changes, test tracks | — |
| 2 | **Track Model & Geometry Engineer** (TS) ⟂ | Block/socket model, snapping, arc/clothoid/elevation/bank math, loop-closing solver, undo/redo | Spec → core library | 1 (spec) |
| 3 | **Compiler / Importer Engineer** (TS) | Blocks → `TrackData` XML and the reverse; golden tests against shipped tracks | Core lib + spec → exporter/importer | 1, 2 |
| 4 | **Editor UI/3D Engineer** (TS/three.js) | Viewport, palette, drag-snap interaction, inspector, validation panel, preview road, touch support | Core lib → web app | 2, 3 |
| 5 | **Facsimile / Geodata Engineer** | GPX/GeoJSON/OSM/image trace import, projection, simplification, curvature segmentation, block fitting, scale-to-fit, DEM elevation | Geodata → block chain | 2 |
| 6 | **Terrain & Scenery Engineer** | Heightmap tool (flatten-under-road, edit), prop/combi placement, tunnels/palms/lanterns mapping; preview of scenery | Models list → terrain + props | 1, 3, 4 |

### Supporting

| # | Agent | Responsibility |
|---|---|---|
| 7 | **QA / Verification Agent** | Golden round-trip tests, fuzzing generated tracks through the game, parity checks (editor vs game length/width), validation-rule tests, performance on mobile |
| 8 | **UX / Design Agent** | Block iconography, onboarding flow, accessibility, mobile layout, tutorial |
| 9 | **Docs / Legal-Content Agent** | User docs, tutorial track, attribution (OSM/DEM), naming policy for famous-circuit facsimiles, sample track pack |

### Orchestration

- **Lead/coordinator** (the human, or one orchestrating session) owns the spec and integration; agents 1 → 2 → 3 are the critical path.
- Suggested waves:
  1. **Wave A (parallel):** Agent 1 (spec + loader), Agent 8 (UX concepts), Agent 9 (licence/naming policy).
  2. **Wave B (parallel):** Agent 2 (core), then Agent 3 (compiler) once the spec lands; Agent 7 starts golden tests.
  3. **Wave C (parallel):** Agents 4, 5, 6 on top of the core library.
  4. **Wave D:** Agent 7 full QA, Agent 9 docs, Agent 8 polish.
- Guardrails: apply the usual agent guardrails — cap per-agent scope, stall rule, token ledger, pause at 50% of budget. Each agent owns a directory and must not edit another's files.

A leaner option (4 agents) is feasible for an MVP without facsimile import: merge 1+3 (format/compiler), 2+4 (model/UI), 6 (terrain/scenery), 7 (QA).

---

## 9. Suggested repository layout

```
RacingGame/
  Documentation/Track-Editor-Plan.md      (this file)
  TrackEditor/
    packages/core/        model, geometry, compiler, importer   (TS, no DOM)
    packages/geodata/     GPX/GeoJSON/image → blocks
    apps/editor/          web UI (Vite + three.js)
    tests/golden/         shipped .Track round-trips
    samples/              example .rrtrack + exported .Track
  Core/Tracks/CustomTrackLoader.cs        (new, game side)
```

Hosting: static site (GitHub Pages / any CDN). No backend in v1.

---

## 10. Open questions (remaining)

Resolved: distribution (D1), terrain (D2), naming (D3), scenery (D4), loops (D5), web stack (D6).

1. **WebGL timing:** resolved, spike can run on 3.8.6. Resolved (D8): desktop first.
2. **Android/iOS:** is a folder-based workflow enough, or do you want an in-game "Import from file" picker (my recommendation)?
3. **Gallery:** are you comfortable with the staged approach in §3.7 (files, then GitHub-curated, then backend)?
4. **Execution:** see the clarification in the chat reply on multi-agent vs sequential and timeframe.

Note: with D6, §4 (architecture), §8 (agent roles) and §9 (layout) were written for TypeScript. Under option A, roles 2-4 become C# roles and `TrackEditor/` becomes a MonoGame project sharing `Core/Tracks`. Update those sections once the Phase 0 spike confirms option A.

---

## 11. Phase 1 progress log

**2026-11-04: desktop `Tracks/` loader implemented (uncommitted, desktop build only, not yet driven in the game).**

New files: `Core/Tracks/CustomTracks.cs` (scan, load, validate), `Core/GameScreens/CustomTrackSelection.cs` (list screen).
Edited: `TrackData.cs` (`Load(Stream)`), `Track.cs` (ctor/`Reload` from data), `Landscape.cs` (`ReloadCustomTrack`, `IsCustomTrack`, `HighscoreLevel`), `Replay.cs` and `Highscores.cs` (slot -1 = custom: no stored scores/replays, ghost drives at ~45 units/s), `TrackSelection.cs` (`C` / gamepad Y opens the list; `CustomTrack`, `HighscoreLevel`), `GameScreen.cs`, `BasePlayer.cs`, `RacingGameManager.cs`.

How it works: `Tracks/*.Track` files are parsed and validated (≥4 points, finite, inside 0..2560 map, length ≥500). Invalid files are listed in red with the reason. Selecting one loads it on the existing terrain through the normal `Track` path. Highscores and replays are skipped for custom tracks in this step.

Verified by a throwaway loader test: valid hand-written file loads (matrix with only `M11..M44` is fine); garbage XML, off-map points and <4 points are rejected with a message. **Verified in game by the user (2026-11-04):** `MinimalOval.Track` (hand-written, 16 control points, one Windmill) loads from the Tracks folder and is playable. **Not yet verified:** per-track heightmaps, LAN with custom tracks (LAN screens reset the selection to built-ins).

Note: these edits sit in the same files as the uncommitted LAN work (`Landscape.cs`, `TrackSelection.cs`, `GameScreen.cs`, `RacingGameManager.cs`). Nothing has been committed. A pre-change snapshot of the LAN diff is in the session scratchpad, so the two can be separated if needed.

### Heightmap refactor baseline (captured 2026-11-05, before any terrain change)

Purpose: prove the refactor leaves the three built-in tracks and the default terrain bit-for-bit identical. Method: a **temporary** environment-variable hook (`Core/Landscapes/TempFingerprintDump.cs` plus one marked line `TEMP-FINGERPRINT` in `RacingGameManager.cs`) hashes the real in-game data (terrain vertices incl. normals, height grid, sampled heights, and per built-in track the generated road samples, helper regions, checkpoints, start data) with SHA-256 and exits. Run from `Desktop/bin/Release/net10.0`:
`RACINGGAME_FINGERPRINT=/path/out.txt dotnet RacingGame.dll` (macOS has no `timeout`; use `perl -e 'alarm 120; exec @ARGV'` if a limit is wanted).

Two runs were identical for everything except object counts, which differ by design (random palm/lantern choice changes which props get skipped), so counts are informational only.

```
terrain.vertices   192aa1039c32cab8 items=66049 bytes=2906156
terrain.mapHeights a6faaaedd49a2b86 items=66049 bytes=264196
terrain.sampled    a4a1b8cfe9b8e1a7 n=4884
Beginner.points      a2475ce7ac34db02 items=1332 bytes=79920
Beginner.helpers     1658f819dffecb25 items=4 bytes=48
Beginner.checkpoints 658bd8a973472a54 items=6 bytes=24
Beginner.start       d2f3e32b5d7ea458 items=5 bytes=44
Beginner.objectCount 1231 (informational: palms use random)
Advanced.points      ca9eb67cc6c2fb37 items=2172 bytes=130320
Advanced.helpers     cdb02044e8b6e00b items=8 bytes=96
Advanced.checkpoints 0e52802e31042f74 items=10 bytes=40
Advanced.start       13b5cf6e0407e5c3 items=5 bytes=44
Advanced.objectCount 1855 (informational: palms use random)
Expert.points      0f64613358c8aec2 items=4466 bytes=267960
Expert.helpers     04946ae6c9732be0 items=20 bytes=240
Expert.checkpoints 4f5fc0fbb00ed440 items=21 bytes=84
Expert.start       983e450d23c89d42 items=5 bytes=44
Expert.objectCount 3471 (informational: palms use random)
```

**Both temporary files must be removed before the single commit** (`TempFingerprintDump.cs` and the `TEMP-FINGERPRINT` line). Nothing else was changed for the baseline.

### Heightmap refactor: done and verified (2026-11-05)

Implemented (uncommitted): `Landscape` terrain build moved out of the constructor into `BuildTerrain` / `ApplyTerrain` (terrain is applied **before** the track so the road lifts over it; vertex buffer refilled in place, index buffer built once; shipped terrain cached for switching back; city plane tied to the terrain; render null-guarded). `TrackData.Heightmap` (+ compress/decompress helpers, size check), `CustomTrackInfo.Heights`.

Verification with the temporary hook:
- **Built-in tracks and shipped terrain: all hashes identical to the baseline** (terrain mesh, height grid, sampled heights, road samples, helper regions, checkpoints, start data for Beginner/Advanced/Expert).
- **Custom track with its own terrain** (`HillOval.Track`, a hill in the middle): height at centre 235.3 (expected 235.3), corner 11.8 (expected 11.8); the road, authored flat at 80, was lifted to 79.6–96.1 over the hill; no city plane (no buildings in that track).
- **Switching back to Beginner restored the shipped terrain exactly** (hashes equal the baseline, city plane back).
- Heightmap blob: 66,049 raw bytes → 5.8 KB compressed; whole test file 10.7 KB. A wrong-size heightmap is rejected in the list with a message (`BadHeights.Track`).
- **Not yet verified:** how it looks in the game (visual check by the user), custom terrain textures/detail on very different terrain, LAN with custom tracks.

### Command-line track tool (D12): done (2026-11-05)

`RacingGame/TrackTool/` (`tracktool`, net10.0, not part of `RacingGame.sln`): links `TrackData.cs`, `CustomTracks.cs` and `Log.cs` from the game, so there is one definition of the format. Packages: MonoGame.Framework.DesktopGL (for `Vector3`/`Matrix`), StbImageSharp + StbImageWriteSharp (PNG).

Run: `dotnet build TrackTool` then `dotnet TrackTool/bin/Debug/net10.0/tracktool.dll <command>`.

| Command | What it does |
|---|---|
| `info <file>` | Metadata, counts, length, extent, heightmap state, validity, warnings |
| `validate <file\|folder>...` | Same checks as the game's loader; exit 1 if any invalid. Warns about missing Author, odd point spacing, accidental loop-the-loops |
| `new <circle\|oval\|rounded-square\|wavy> -o f [--name --author --description --points --radius --z --terrain none\|flat\|hill\|noise --seed]` | Generates a valid track (optionally with its own terrain; road height follows the terrain unless `--z` is given) |
| `meta <file> [--name --author --description --created]` | Edits metadata; changing `Author` keeps the old one as `OriginalAuthor` (D10) |
| `heightmap dump <file> <out.png> [--default LandscapeHeights.data]` | Terrain as a 257x257 greyscale PNG (black = 0, white = 300 units) |
| `heightmap set <file> <in.png> [--resample]` | Replace the terrain from a greyscale PNG (must be 257x257 unless `--resample`) |
| `heightmap gen <file> --terrain flat\|hill\|noise [--seed]` | Generate terrain for an existing track |
| `heightmap clear <file>` | Back to the shipped terrain |

Edits are in place unless `-o` is given. Exit codes: 0 ok, 1 invalid track or failure, 2 usage error.

Tested: all four shapes and terrains; `validate` on a folder and on the shipped `TrackBeginner.Track` (valid); author change keeps `OriginalAuthor`; PNG dump then set round trip is byte-identical; resample; clear; error cases (missing file, bad XML, unknown command, out-of-range radius) give the right exit codes. Not yet tested in the game: the tool-made tracks `rolling.Track` (noise terrain, 40 points, radius 900) and `rounded-square.Track` were copied to the user's Tracks folder for a visual check.

Observation from dumping the shipped terrain: it is a bowl. A high, rugged rim runs round the whole map edge, a central mountain rises in the middle, and the roads run in the dark flat lowlands between. Editor terrain presets should probably mimic that (keep the edge high, the playing area low).

### Findings from the first in-game tests of tool-made tracks (2026-11-05)

**Terrain poking through the road on the shipped terrain (user saw it on `rounded-square`, east side).** Measured on the real generated road with the temporary hook (terrain height at the road centre and at lateral offsets up to 1.25 x half-width, minus road surface height): `rounded-square` has 35 of 2237 samples where an edge is buried (worst 7.0 units, near (569,1904) west and (2077,1003) east); the road *centre* is never under the ground (the game's lift works for the centreline). `rolling` (own terrain, road built 15 above it) has none; the plain `oval` has 45 edge samples and 15 centre samples within 0.5 units.
Cause: the game's terrain lift protects the control polyline (+/-5 units) only, not the full road width or the guard rails, so a road running across a slope can bury one edge.
Mitigations for the editor/tool, in order of value: (1) a terrain-aware check that runs the same road generation and flags buried edges (overlay in the editor, `tracktool validate --terrain` later); (2) the planned **flatten-under-road** terrain brush, which is the real fix for custom terrain; (3) on the shipped terrain, steer new tracks to the flat lowlands (see the heightmap PNG: dark = low). Changing the game's lift for custom tracks is possible but deliberately not done (the built-in tracks must stay identical).

**In-game lap counter is wrong for races that are not 3 laps.** The HUD panel is a baked sprite in `Ingame.png` (rect 381,132, 222x160) with "/3 Laps" painted into it; only the orange current-lap digit is drawn dynamically (`TextureFontBigNumbers`). LAN races allow 1 to 5 laps (`Player.RaceLaps`). The sheet also holds blank grey rounded panels and the orange digits 0-9 (no slash glyph), so a dynamic panel (9-slice of a blank panel, orange digit, "of N" + "Laps" in the game font) is feasible without new art. Idea for later: let a track file suggest its lap count.
Also seen: the top-5 times panel shows 0:00.00 on custom tracks (no stored highscores yet; planned: key by track file hash).

### Dynamic laps panel (2026-11-05, implemented, uncommitted)

The baked "/3 Laps" HUD panel is replaced by a drawn one: layout `1/3` in the big orange digits with `Laps` (or `Lap` for a 1-lap race) beneath, the "/" being the orange bar from the sprite sheet, rotated; the panel is a 9-slice of a blank panel from `Ingame.png` (rect 727,2,280x60, corner 20) and grows with the digit count (checked at 1/3 and 1/99; designed for 99/99). Total comes from `Player.RaceLaps` (3 in single player, the lobby value in LAN); the shown lap is clamped to the total. Code: `UIRenderer.RenderLapsPanel/RenderPanel`, `TextureFontBigNumbers.GetNumberWidth`. Verified by screenshots from the temporary rig. Not yet checked in an actual LAN race with 5 laps (same render path).

Temporary files to delete before the one commit (now three): `Core/Landscapes/TempFingerprintDump.cs`, `Core/Landscapes/TempRig.cs`, and the lines marked `TEMP-FINGERPRINT` (RacingGameManager.cs) and `TEMP-RIG` (SplashScreen.cs, BaseGame.cs). Rig usage: `RACINGGAME_AUTOSTART=1 RACINGGAME_LAPS=<n> RACINGGAME_FRAMES=<dir> dotnet RacingGame.dll` from `Desktop/bin/Release/net10.0`, screenshots land at 12 s, 15 s, ... (the in-game ones from about 15 s).

### Frame-rate independence of the car (2026-11-05, implemented, uncommitted)

Found while testing: car behaviour depended on the frame rate (`CarPhysics.cs`).
- **Speed/gears:** friction was applied once per frame, so ~230 fps capped the car at ~145 mph (3rd gear) while 60 fps reached 200+ (4th/5th). Fixed with `frameScale = moveFactor * 60` on the force decay, the speed gained from the force, the speed friction and its per-step cap (identical at 60 fps). Measured (throttle held): 60 fps 197 -> 198 mph; ~230 fps 147 -> 246 mph (gear 3 -> 5). User confirmed the gears feel right.
- **Steering:** keyboard and analog stick/D-pad were already frame-rate independent (the steering value is reset each frame when no key is held). The **mouse** was not: its per-frame input is a displacement (already proportional to frame time) and was multiplied by the frame time again. Fixed by using 1/60 s instead of `moveFactor` for the mouse. Measured with a simulated constant mouse sweep from standstill: before 49.9 deg at 47 fps vs 6.8 deg at 298 fps; after 33.2 deg at 48 fps vs 33.4 deg at 264 fps.
- A first, more elaborate steering fix (decay and input scaling for all inputs) was tried and **rejected**: the real code's per-frame reset made it wrong for the stick, which the A/B test exposed. Lesson: the keyboard/stick code stays as it was.
- Not touched: camera smoothing and anything else that runs once per frame. Debug builds run uncapped (vsync is only disabled in `#if DEBUG`), so they exposed this; Release at 60 Hz always behaved as designed.

Temporary files/markers to delete before the one commit (updated): `Core/Landscapes/TempFingerprintDump.cs`, `Core/Landscapes/TempRig.cs`, and the marked lines `TEMP-FINGERPRINT` (RacingGameManager.cs) and `TEMP-RIG` (SplashScreen.cs, BaseGame.cs: autostart, screenshot capture, RACINGGAME_NOVSYNC). `CarPhysics.cs` no longer has any rig hooks.

### Editor design prototype: "Tarmac Toolbox" (2026-11-05; first called Tarmac Toolbox, renamed the same day)

A working single-page prototype of the editor, built in the browser while the WebGL packages are unavailable, so the design and workflow can be judged and tweaked now. Source: `Documentation/prototype/tarmac-toolbox.html` (also published as a private Claude artifact: https://claude.ai/artifact/FkGgmnojm3LdDZfvhUBWQs ). It is plain HTML/JS with no build step; the geometry, terrain, validation and export code is one self-contained block (`/*CORE-START*/ ... /*CORE-END*/`) that is the reference for the C# editor core.

What it demonstrates (all working):
- **Block palette and snapping:** straights, bends, sweepers, hairpins, chicanes, climbs/descents, tunnel/palm/lamp scenery. Click to append, or drag onto the map: the block snaps to the nearest block end and the rest of the track shifts. Chain strip with reorder (drag), duplicate, mirror, delete, undo/redo.
- **Top-down map with terrain**, elevation profile with the road lifted over the ground (the game's +4.5 rule), checkpoint ticks every 500 units, start line, auto-close loop (a Hermite connector back to the start).
- **Checks tab:** author required, loop closed, inside the map, tight corners, steep up-then-down (accidental loop-the-loop), gradient, road edges buried in terrain (with a one-click "Flatten"), self-crossing, length and estimated lap. Click a check to jump to the spot.
- **Terrain tab:** game terrain (preview only) or an own heightmap (desert bowl, rolling, hill, flat) with "flatten under the road" (margin and falloff).
- **Trace tab (facsimile):** four stylised outlines with pun names, or paste GeoJSON/GPX/"lat, lon" lines; scale, rotate, mirror, move; "Build blocks from outline" with a detail slider (the Spa-ghetti outline becomes 32 blocks that follow it; Auto-close joins the loop).
- **Export:** the real `.Track` XML (metadata first, `Heightmap` deflate-raw + base64, width/road helpers placed on the lifted road), a preview, "Save", "Copy XML" and the per-platform "save it to your folder" instructions with the visitor's platform highlighted.

Verified: core logic under Node (closure, bounds, checks, XML, outline-to-blocks, parsers); the page in headless Chrome (no script errors, dark and light, no page-level horizontal overflow at 500/700/1000 px); click, undo/redo, drag-and-drop, inspector edits and delete through the real handlers; and **exports from the page load and validate with the game's own loader code** via `tracktool` (with and without a browser-compressed heightmap; the heightmap decompresses to exactly 257x257 in .NET). Two exports were copied into the Tracks folder as "Forge Sample (game terrain)" and "Forge Sample (own terrain)" for an in-game check.

Known prototype limits: 2D top-down view only (the real 3D preview waits for WebGL); the "game terrain" preview is a stand-in, not the shipped heightmap; the artifact sandbox can only save `.txt` files, so the page saves `Name.Track.txt` and asks the user to rename (the real editor saves `.Track`); no scenery props placement yet (only the road-side regions); no touch drag for the palette (click works); outlines are loose sketches, not survey data.

### Prototype v2: direct manipulation of the track (2026-11-05)

Dragging track pieces now works two ways in the prototype (published as version 2 of the same artifact; source `prototype/tarmac-toolbox.html`).

1. **Handles on the selected block** (white dots): a straight's end dot sets its length (snaps to 10 units); a curve's end dot sets its angle (snaps to 5 degrees); a curve's middle dot sets its radius. Everything after the block rides along; the auto-closing connector absorbs the change. Hold Alt to turn snapping off.
2. **Elastic road drag**: grab any point on the road and drag. The blocks around the grab point flex (Flex slider: 1-4 blocks each side); the two ends of that window are pinned, so the rest of the track and the closed loop do not move. A straight that has to bend is first split into `straight, arc, straight, arc, straight` (two flex arcs that start almost straight and cancel); afterwards arcs under 1 degree become straights again, matching neighbours merge, and tiny pieces vanish. Hold Shift to move only the grabbed block (the rest follows rigidly). Drag empty ground, hold Space, or middle-drag to pan.

**How the solver works** (pure code in the `CORE` block: `solveWindow`, `splitForFlex`, `cleanupWindow`): unknowns are each window block's length (straights) or angle and radius (arcs, sign kept, radius 60-900, length 1-900); a damped Gauss-Newton / Levenberg-Marquardt solve with a numeric Jacobian, a few iterations per pointer move, warm-started from the previous move. Residuals: the grabbed point's distance to the cursor (weight 1), the window end's position and heading versus where it was (weight 1; heading counts as 150 units per radian), and a small pull towards the original values so the smallest change wins. The end-pose weight matters: at 8 the solver was too timid to move a point against a fixed end (it stalled), at 1 it converges.

Measured under Node (sample track): a 60-unit sideways drag on a 400-unit straight converges to 0.0 units of lag with the end pose held exactly and the loop gap unchanged (7 units); a 90-unit drag the other way finds the S-bend (+/-31 degrees) and a wider hairpin (radius 240 to 311) with lag 0.3; very large requests (200 units, or pulling a point on a 180-degree hairpin) get part of the way and stop, and the page shows a red "limit" marker and a toast. In headless Chrome through the real pointer handlers: 8 blocks become 12 with lag 0 and the loop still closed; undo restores the original 8; handles, Shift-rigid drag, pan and undo all work with no script errors.

Not done yet: the red "limit" line was verified in code but not seen on screen (in testing even extreme drags still found a solution); handle drag with Auto-close off leaves the loop open (it opens, by design); no touch handling for these drags beyond what pointer events give; elastic drag changes x/y only (climb values are untouched); the solver is not aware of terrain or self-crossing while dragging (the Checks tab reports those afterwards).

### Track file names and the track list (2026-11-05)

**Finding:** the preview page can only save `.txt` files, so a track arrives as `Name.Track.txt`. macOS "hide extension" shows that as `Name.Track` (it hides only the last extension), so the file looked right but the game, which only scanned `*.Track`, skipped it.

**Fix (game, uncommitted):** `CustomTracks.IsTrackFile` accepts `Name.Track` and `Name.Track.txt` in any letter case, and ignores hidden files and macOS `._` companions (`CustomTracks.FileDisplayName` strips `.txt` and `.Track`). The command-line tool shares the code. Verified with a mixed folder: `.Track`, `.TRACK`, `.Track.txt`, `.track.TXT` load; `._X.Track`, `notes.txt`, `.json`, `.Track.bak` do not. The page's save button now names the exact file it will save and the note says it can be used as it is.

**Track list scrolling:** the list shows 8 rows and scrolls with Up/Down or the gamepad; the mouse wheel now scrolls it too, and the footer says "n / N (Up/Down or mouse wheel for more)" with "more above" and "more below" markers.

### Track selection "Custom" button, prop keep-out, timing segments (2026-11-05)

**Track selection screen (done, uncommitted).** A fourth button labelled "Custom" (a label-only panel, no artwork) sits after the three built-in tracks; the row is recentred, it highlights like the others, and selecting it (click, Enter/Space/A) opens the custom track list. The hidden shortcut is **C** (or the gamepad Y button); the bottom hint text is gone. Multiplayer is unaffected: `NumberOfTracks` is still 3, and while Custom is highlighted `SelectedTrackNumber` reports the last built-in track. `UIRenderer.RenderPanel` is now public with an optional tint (it also draws the lap panel). Verified with screenshots from the rig (default highlight and Custom highlighted).

**Props and the road.** Measured on `Burger-Ring` and `Forge-Sample`: of ~450 scenery props the game generates (palms, rocks, ruins, cacti, windmills, oil pumps, bins, lamps) **none** is on the road surface: the random decoration is spawned 26-88 units away, palm/lamp avenues sit about 0.6 road-widths out. The only things measured "on the road" are by design: guard-rail posts at the edge, checkpoint gantries and start light over the road, and signs whose centre is placed 0.005-0.15 units inside the edge. Tarmac Toolbox does not place individual props yet (only palm/lamp/tunnel stretches along the road, which the game places), so the editor cannot put one on the road today.
**Safety net (game, custom tracks only):** hand-written or generated `NeutralsObjects` whose centre is within `PropRoadMargin` (2 units) of the road surface are skipped (gantries, signs and start/finish scenery are exempt); the built-in tracks are untouched (baseline hashes still identical). Test: of 4 hydrants in a file, the 3 placed on the road were left out and the one 45 units away was kept. **Rule for the editor's future prop tool:** refuse or push out any placement closer than road half-width + margin + the model's radius, and flag existing ones in the Checks tab.

**Timing segments (not editable yet).** Today checkpoints are automatic: `Track.cs` puts a banner every 500 units along the lap (skipping the last ~60 units and loops) and stores the sample indices in `checkpointSegmentPositions`; they split the lap time, drive the "ahead/behind best lap" display, and gate lap completion (a lap only counts when all but one checkpoint were passed). They are not in the `.Track` file. To make them editable: an optional `Checkpoints` element (distances along the lap in units, ascending, at least ~150 apart and away from the start line); `Track.cs` uses them instead of the 500-unit rule when present (distance to sample index through the cumulative length of the road samples); the editor shows draggable flags on the map and profile with add, remove and "reset to every 500". Not implemented; waiting for a go-ahead.

Temporary rig additions (delete before the commit, with the rest): `RACINGGAME_SCREEN=tracks` and `RACINGGAME_SELECTBTN=<n>` in `TempRig.cs`; the props analysis block in `TempFingerprintDump.cs`.

### Editable checkpoints (2026-11-05, implemented, uncommitted)

**File:** optional `Checkpoints` (`float[]`, fractions of the lap), see the format spec. **Game:** `Track.GenerateCustomCheckpoints` uses them instead of the 500-unit rule (built-in tracks never have the element, so they are unchanged: baseline hashes identical). **Tool:** `tracktool info` shows them; `tracktool checkpoints <file> --auto | --none | --count n | --set 0.25,0.5` (validated). **Validation** in the game's loader and the tool: each between 2% and 98%, increasing, at least 1% apart.
**Tarmac Toolbox:** new **Timing** tab (Automatic / Custom, add, space evenly, every 500 units, remove, per-sector length and ghost-pace time), numbered **flags you drag** on the map (to the nearest road point) and on the elevation profile (by distance); spacing rule 120 units between flags and 80 units from the start/finish; the Checks tab reports none or a very long sector. Checkpoints are anchored to a block (id plus fraction along it), so they travel with the road when you lengthen, drag or reorder blocks; if a block is split or merged by the elastic drag they are re-anchored by distance. They are saved as fractions of the lap.
**Verified:** the core under Node (anchoring, lengthening, deleting a block, edge and crowding rules, checks, XML); in headless Chrome through the real handlers (switch to custom, drag on map and profile, the 120-unit rule holding back a drag, add, remove, space evenly, undo, export); the page's export through `tracktool` and then loaded in the game (asked 32.4, 35.6, 38.8, 53.9, 67.4, 80.9, 94.3% and the game placed them at 32.5, 35.7, 39.0, 54.1, 67.4, 81.0, 94.5%); tool and game with a custom list, three evenly spaced, none (zero checkpoints loads and plays) and automatic.
**Not done / caveats:** the editor's lap length (block arcs) and the game's (the spline through the control points) differ by a small amount, hence the 0.1-0.2% drift above; no per-checkpoint names or colours; not tried in a full lap in the game with a human driving (zero and custom lists load, and the lap-counting logic only depends on the list).

### Open design questions asked 2026-11-05 (not built)

- **Editing the terrain the track sits on:** today the Terrain tab offers presets, a seed and "flatten under the road"; there is no sculpting. Outside the page, `tracktool heightmap dump` writes the terrain as a greyscale PNG that any paint program can edit, and `heightmap set` puts it back. Proposed: raise / lower / smooth / flatten brushes on the map (own-heightmap presets only; the game's shipped terrain can't be edited).
- **Dragging the elevation profile:** feasible (see the reply recorded in the chat). Each block already has a climb; dragging a point on the profile would solve the climb values of the blocks around it, with the heights at both ends of that window pinned, the same idea as the elastic road drag but one-dimensional and linear. Pros: direct, fast to learn, ground and road shown together, keeps the loop's start and end height. Cons: the profile is piecewise linear per block (the game smooths it, but long blocks make long ramps, so it needs automatic splitting for crests and dips); the game lifts the road above the ground, so pulling it below the terrain only works with flatten on or an own heightmap; steep pairs become loop-the-loops, so the drag must respect the gradient limit; tunnels and the closing connector need rules.

### Name: Tarmac Toolbox (2026-11-05)

The editor was renamed from "Track Forge" to **Tarmac Toolbox** (page title, header, docs; the browser storage key keeps its old internal name so saved work is still found). **Name check (web search only, not legal advice):** no product, app or game called "Tarmac Toolbox" turned up. Related things found: *Tarmac* is a large UK/US building-materials and road-surfacing company (Tarmac America holds registrations for concrete products); a **dead** TARMAC trademark in Class 9 (software) owned by equinux USA (filed 2010, registered 2011); a separate 1996 TARMAC software mark (HE Holdings, training software for aviation ground crews); the open-source Roblox tool `tarmac` and Arm's "Tarmac trace utilities" (unrelated developer tools); "tarmac" is also a common word for road surface in British English, and "toolbox" is generic. **Risk assessment:** probably low for a free sample editor (unrelated classes, descriptive use), but "Tarmac" is a known brand in exactly this domain (roads), so a company could object if the name is promoted widely. **Before public release:** run a proper clearance search (USPTO TSDR, UK IPO, EUIPO) and consider a fallback name. This is decision D3's spirit (generic, humorous names) applied to the tool itself.

### Elevation limits (design, for the profile drag and the Checks tab)

Yes, the elevation editor needs both a floor and a ceiling, plus the slope limit. Proposed limits:
- **Floor (per point):** road height at least the ground height plus 4.5 units, because the game lifts any road that is lower (so pulling below that would show nothing). With the game's own terrain the floor is the ground (a hard limit that follows the hills). With an own heightmap and "flatten under the road" on, the ground follows the road down, so the floor is the terrain's minimum: the road can go down to about 6 units (flatten sets the ground 6 below the road, never below 0).
- **Ceiling:** 300 units, the top of the terrain scale (`MapZScale`); the shipped tracks peak at about 217. Show a soft warning above ~220.
- **Slope:** at most about 30% between neighbouring points, which also keeps clear of the accidental loop-the-loop rule (a steep rise followed by a fall).
- **Per block:** climb limited to +/-30% of the block's length (the climb slider currently allows +/-120 whatever the length); start height already limited to 0-300.
- **Enforcement:** as bounds inside the 1-D solver, with the same red "limit" cue as the road drag, and floor and ceiling drawn on the profile (ground+4.5 as a line, 300 as a dashed line).
- **Today:** typed values are clamped (climb +/-120, start 0-300), and the Checks tab covers slope and buried edges, but there is no check for a height ceiling or for the road sitting below the ground+4.5 floor. Not built yet.

### Elevation drag and terrain brushes (2026-11-05, implemented in the prototype)

**Elevation drag.** Grab the road line on the elevation profile and drag it up or down. The block under the pointer is split at that point without changing the road (a pure split: end position, heading, height and length are unchanged), the blocks around it (Flex, 1-4 either side) become a window with both ends pinned, and a one-dimensional solver (`solveProfile`) moves the grabbed height to the pointer while changing the slopes as little as possible (cost: length times slope change squared, solved by Gauss-Seidel sweeps). Limits, as designed: **floor** = ground + 4.5 (the game's lift rule), or about 6 when the ground follows the road (own heightmap with flatten on); **ceiling** 300; **slope** at most 30% per block, applied to every block in the window. When the pointer asks for more, the grabbed point stops at the limit and the profile shows a red tag ("slope limit", "ceiling", "lowest the road can go here", or "limits clash") and a toast on release. The profile now draws the floor (dashed, following the ground) and a dashed ceiling line, with small markers at block joints; during a drag the view is fixed at 0-300 so the ceiling is visible. On release, split pieces that make the same road merge again (same slope, feature and width; arcs also same radius and direction), so a drag that changes nothing leaves no extra blocks. The closing curve cannot be dragged; the very start and end of the lap are refused (set the start height in the start line settings). New Checks item "Heights in range" (error above 300, warning above 220 or near the floor).
**Cleanup fix:** the cleanup used by the road drag used to merge neighbouring straights even when their slopes differed (silently changing the profile); it now merges only pieces that make the same road, and also merges matching arcs.

**Terrain brushes** (Terrain tab, own-heightmap presets only; the game's terrain cannot be edited, so the button is disabled with an explanation): Raise, Lower, Smooth, Level (to the height where you first press), with Size (40-500 units) and Strength. Paint by dragging on the map; Space or middle-drag still pans; Esc stops. Edits are stored as a small signed offset per height cell on top of the preset, run-length encoded (an edit of one hill is about 1 KB in the saved state and undo history), applied before "flatten under the road", and exported inside the track's heightmap. Changing the preset or seed clears the sculpting; "Clear sculpting" removes it (undoable). Each stroke is one undo step.

**Verified:** Node: splitting is exact for straights and arcs; the profile solver lifts to a smooth bump, stops at the slope limit (50 + 0.3 x 600 = 230), the floor and the ceiling, and copes with a profile that already breaks the slope limit; cleanup merges a split back and keeps different slopes apart; run-length coding round-trips exactly; brush raise/lower/smooth/level behave and clamp. Headless Chrome through the real pointer handlers: an elevation drag splits 8 blocks into 9 and undo restores 8; a huge drag up stops at slope 0.300 exactly, a huge drag down stops at the floor; the brush is disabled on the game terrain, raises ground from 11.8 to 37.6 units on a flat preset, undo and redo work, and the export carries a valid heightmap that the C# tool accepts (peak 32 over a flat 10). All earlier interaction tests (road drag, handles, checkpoints, export) give identical results.
**Not done / caveats:** the floor uses the editor's stand-in for the game's terrain when the preset is "Game terrain", so there it is an estimate; no gradient smoothing between blocks (the game's spline smooths it); the brush works at the terrain's own 10-unit grid; the profile drag is vertical only; no touch testing.

### Outline library as files; Open file; Save outline (2026-11-05)

**Library as files.** The four built-in circuit outlines were moved out of the page into `Documentation/prototype/outlines/` (`index.json` lists the files; `mon.json`, `spa.json`, `sil.json`, `nur.json`; `README.md` documents the format). The page fetches `outlines/index.json` and each file at start (published as files next to the page in the artifact). A file is `{format:"tarmac-toolbox-outline", version, id, name, note, source, license, points:[[x,y],...]}`; `points` is a closed loop in any units (the editor centres, scales and smooths it). Each carries a `source` and `license` field so credit travels with the shape; the four built-ins are loose sketches, not survey data. If the files cannot be loaded the Trace tab says so and Open file still works.
**Open file.** A button in the Trace tab reads a GPX, GeoJSON, "lat, lon" CSV/text file or one of the outline JSON files (the paste box still works). Unreadable files leave the current outline alone and say what formats are accepted.
**Save outline file.** With an outline loaded, a name box and "Save outline file" write it out as an outline JSON (with prompts to fill in `source` and `license`, with the OpenStreetMap credit reminder); add it to the `outlines` folder and `index.json` to share it. Without the downloads capability the JSON is copied to the clipboard instead. Not stored in the `.Track` file (decision: it would bloat the file and carry data-licence obligations).
**Verified:** served over local HTTP in headless Chrome: the library loads 4 outlines from files; picking one sets the trace; Open file with a GPX (6 points), a GeoJSON (5), a CSV with a header line (5) and an outline JSON (5) all load and are named from the file; a non-coordinate file is rejected with a message and changes nothing; Save produces a valid outline file; building blocks from an opened outline works; with the library unavailable (page opened from disk) the message appears and there are no script errors. The published artifact lists `outlines/*.json` among its files.
**Not done:** the `outlines` folder is not read by the game (the game never needs it); no in-page editing of an outline's points; a shared gallery of outlines is a later step.

### Enter key on the track selection screen; custom list preview (2026-11-05)

**Fix:** Enter now starts the highlighted track, or opens the custom list when the Custom button is highlighted (the screen only accepted Space, the mouse and gamepad A, as the original game does; the custom list and the multiplayer screens already accepted Enter). The main menu, car selection, splash and game-over prompts also accept Enter (added in commit a710f80e; checked in the code 2026-11-05).

**Preview on the custom list (estimate, not built):** the list would become two columns: tracks on the left, a panel for the highlighted track on the right. *Core (about 1-2 hours):* a top-down map of the track drawn from the file's control points with the game's own Catmull-Rom smoothing, using the existing `BaseGame.DrawLine` 2D line drawing (a few offset lines for road width), a start marker and a direction arrow; text: name, author, description (word-wrapped), created date, length, estimated lap at ghost pace, number of checkpoints, own terrain yes/no; layout work for 4:3 and wide windows. *Extras (another 2-3 hours):* a terrain thumbnail behind the map (decompress the heightmap, colour it like the editor, cache one texture per selected track) and a mini elevation profile. *Not recommended:* embedding a preview PNG in the `.Track` file (extra size, can go stale if the file is edited outside the editor); computing it from the points is always accurate. A 3D preview or fly-through would need to load the track and terrain and is a much bigger job.

### Custom list preview (done)
`CustomTrackSelection` is now two columns: list on the left, preview on the right: top-down map from the control points via `Vector2.CatmullRom` (north up), start arrow, cyan ticks for custom checkpoints, name/author/date/length/checkpoints/terrain, description (2 lines) below. `LineManager2D.MaxNumOfLines` raised 64 -> 1024 (preview needs it). Enter now also on MainMenu, CarSelection, SplashScreen, GameScreen game-over. Verified by screenshot (Burger-Ring). Lap-time estimate deliberately left out (no reliable speed model). Extras (terrain thumbnail, elevation profile) not built.
Temp: TempRig has a `custom` screen option and delays AutoStart until TotalTime>=3; BaseGame catch has a TEMP-RIG Console.Error line (delete with the rest).

### Custom list paging (done)
8 per page, page follows selection (Up/Down/wheel cross pages; Left/Right/PageUp/PageDown or the bottom-right arrows change page). Arrows + "Page n / N" only when more than 8 tracks. Back button bottom-left (`UIRenderer.RenderBackButtonLeft`, also needed for touch). Arrows via `UIRenderer.RenderPageArrow` (reuses SelectionArrowGfxRect). Screenshot-verified with 11 tracks.
Open design question: host sends custom track to clients in LAN races (host-only). See reply in chat.

### TODO (next session, after a /clear): host sends custom tracks to LAN clients
Agreed rule: only the HOST may send a custom track. Design notes:
- Lobby sends a track ID (name + hash of the file). Client with the same file just readies up; client without it shows "Downloading track..." and the host sends the file.
- File is small (~5-30 KB, heightmap already compressed): send in chunks with acks over the existing UDP LAN layer.
- Client validates with `CustomTracks.Validate` (+ size cap, never use sender-supplied paths); treat as untrusted.
- Keep received track in memory for the race; offer "Save to my tracks" afterwards (do not auto-write into the Tracks folder).
- Author name travels with the track.
- Hash mismatch (same name, different file) must be detected.
- Check first that the other agent's LAN work is committed (see project_racinggame_lan memory) before touching the LAN code.

### Bottom buttons unified (done)
`UIRenderer.RenderBottomButtons` now puts Back (B) bottom-left and Select (A) bottom-right on every screen (TrackSelection, CarSelection, Options, Highscores, Help, CustomTrackSelection) via one private helper `RenderBottomButton`. The separate `RenderBackButtonLeft` was removed. Screenshot-verified on TrackSelection. The LAN/multiplayer screens draw their own prompts and were not touched.

### LAN screens on the shared bottom buttons (done)
PlayModeScreen, LanBrowserScreen and LanLobbyScreen now use `RenderBottomButtons` (Back left, Select right); their "Back" list rows and footer hint lines were removed. Lobby: Select = ready/not ready, Back = leave, host's "[ Start race ]" box unchanged. Not changed: in-race Menu overlay and results page of MultiplayerGameScreen. Screenshot-verified: PlayMode, LanBrowser. Lobby not visually tested (needs a live session). Temp rig gained `play` and `lan` screen options.

### Decision 2026-11-04: WebGL parked, launch the web editor on itch.io
No WebGL binaries yet. Keep the C# work (loader, list/preview, tools) and the HTML editor as the v1.x editor. Next session prepares the itch.io launch of the editor. Full handoff: `Documentation/Itch-Launch-Plan.md`.
