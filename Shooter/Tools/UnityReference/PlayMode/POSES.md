> **Superseded.** The values below are the original approximate readings. The corrected poses (Inspector values, compass-derived yaws, fitted positions, estimated pitches) are in `poses.json`; see `Tools/PORTING-PLAN.md` for the method.

# Play-mode reference shots (Game view crops in `game/`, ~1917x1081, Unity Play mode)

Approximate camera poses (Unity coordinates, player root; camera is ~1.0 m above root Y).

| File | Position X | Z | Yaw (deg) | Notes |
|---|---|---|---|---|
| pm_01_start | -28.5 | -7.25 | 76.02 | spawn, first frame |
| pm_02_turn90 | -28.5 | -7.25 | ~166 | floor/wall only |
| pm_03_turn180 | -28.5 | -7.25 | ~256 | wall |
| pm_04_turn270 | -28.5 | -7.25 | ~346 | sky + moon through roof opening |
| pm_05_look_up | -28.5 | -7.25 | 76.02 | pitched up |
| pm_06_look_down | -28.5 | -7.25 | 76.02 | pitched down, floor only |
| pm_07_centre-ish_doorway | -22.27 | -1.14 | 92 | read from Inspector (values partly clipped) |
| pm_08_right_30 | -22.27 | -1.14 | ~122 | assumed +30 from pm_07 |
| pm_09_left_30 | -22.27 | -1.14 | ~62 | assumed -30 from pm_07 |

Lit/shadow split: grey-purple = sunlit, teal = ambient only.
