# Old vs new lighting video

Scripts that record the "old Racing Game lighting vs the new one" video from the rolling demo: two real game windows,
cut between by OBS. The idea and the pitfalls come from the Vector Rumble `TRAILER-GUIDE.md`.

- `record-video.sh`: builds both games with the dev-only harness, starts them, records with OBS, runs the director. `--help` lists the options.
- `director.py`, `obs.py`: the cuts (waits for cue lines in the game logs) and a small OBS WebSocket client.
- `edit-video.sh`: trims the recording to the story from the cue timestamps, with fades.
- `old-main.patch`: the harness hooks for the original game. The script builds `main` in a worktree under `video/old-main`,
  applies this patch and copies `Core/Helpers/DevShots.cs` (the same file serves both games).
- `Core/Helpers/DevShots.cs`: the harness, compiled only with `-p:DevShots=true` (a normal build has none of it).

## To make it again

1. In OBS: Tools > WebSocket Server Settings > enable it with a password; add a "Swipe" transition (Scene Transitions panel, +, Swipe).
2. In a terminal: `export OBS_PASSWORD=...` (never put it in a file or in chat), then start Claude from that terminal if it should run the take.
3. `itch/video/record-video.sh --obs` (about 2.5 minutes for the first build, then about a minute per take). Leave both windows visible.
   The tunnel is entered 45.0 s into the replay; the default offset 32 with a swap at 9 s puts it 4 s after the swipe.
   Without `--obs` the games just play the script, to check the cues.
4. `itch/video/edit-video.sh "<recording>.mov"` writes `<recording> final.mp4` next to it.

Texts, timings and the car are set at the top of `record-video.sh` (overlay texts) and by `DEVSHOT_CAR` / `DEVSHOT_COLOUR` (see `DevShots.cs`).
Recordings and logs are not committed: `RacingGame/video/` is git-ignored and OBS writes to `~/Movies`.
If the game changes a lot, check `old-main.patch` still applies to `main` (the script fails at that step).
