# UI, loading, settings (verified headless + screenshots; interaction not tried by hand)

## UI kit (Core/UI, hand-written)
- No Control/anchor system: `UiTheme` holds the Godot StyleBox constants copied from `button_*.tres` (slanted parallelogram buttons via a shared slanted-box shader, dark fill, white bold label, blue focus outline). Font: the same PT Sans Bold TTF as Godot, as pipeline SpriteFonts (22/30/44).
- Widgets: UiButton, UiRadioRow, UiSlider, UiLabel, UiPanel, UiImage; `UiFocus` = keyboard/pad/mouse navigation, never lands on disabled entries (MULTIPLAYER greyed "COMING SOON", clicks ignored). Focus/navigation first, style second.
- `UiScreen` is the base class (MainMenuScreen, SettingsScreen, LoadingScreen, UiMessageBox). Virtual-resolution scaling from the layout size.
- Menu structure from `menu.tscn`: PLAY > (MULTIPLAYER / SINGLEPLAYER / BACK), SETTINGS, QUIT (omitted on iOS). Esc/B = back; on the root it opens the Quit box.

## UiMessageBox
Title, left/right labels, default button, callback; **Esc/B always = Left**. Pause "Leave?" Stay (default) / Leave!; Quit "Quit?" No / Quit! (default Quit!, a user decision). Results fire once; test default focus and Esc-with-Quit!-default.

## Settings
`GameSettings` JSON under ApplicationData/<Game>/settings.json: display mode, resolution, VSync, bloom, mouse sensitivity, master/music/SFX. `SettingsEditor` works on a clone: Apply = sanitize + copy + save, Cancel = drop. Return target is a parameter (menu or pause). `--shot` runs use defaults and a temp settings path so images do not depend on the machine. Fullscreen/resolution handling is copied from RacingGame (borderless at desktop size, 160 px window-decoration allowance). English only.

## Menu backdrop
Posed player (Idlecombatrest loop + `eyes` overlay as in menu.tscn), baked HDR floor lightmap + soft shadow, spot lights from `menu.tscn`, no shadow maps. Emissive needs its own scale (`rendering.md`).

## Loading screen and lazy boot
- Boot loads only what the menu needs; Singleplayer loads the level behind the loading screen. `LoadPlan`: LoadStep, LoadTable (Boot + Play + FullSet), LoadRunner with weighted, monotonic, clamped progress. Boot 6 + play 8 steps = the 14 old groups (asserted in `--test-ui`).
- `LevelScene.LoadSteps()` yields one model (with its textures) per tick. Single-threaded: GL stays on the main thread, no worker. Esc and clicks ignored while loading.
- Layout: logo, "Loading..." label, dark slanted track with blue fill and %; hold 0.5 s = `menu.tscn` DoneTimer wait_time.
- Measured (warm cache): first menu frame ~1.3-1.6 s (about 1 s is process/window start-up, menu-song init ~200 ms the biggest boot step); play load 0.5 s of work over 45 frames. A real progress bar is staged groups, not byte progress.
- Flags that skip the menu (`--sim`, `--shot`, ...) and `--eager` load everything up front; `--screen loading:0.6` freezes the bar for a screenshot. Re-entry after Leave! skips the loading screen but builds a fresh sim.
- Not seen by eye: bar animation (log only).
