# Godot reference screenshots

Frames from a YouTube video (supposedly 1080p60, so compressed; use for look and layout, not pixel values).

| File | Shows | Notes for the port |
|---|---|---|
| godot-01-main-menu.png | Main menu | Black background with a cool blue-white spotlight on the robot and a real cast shadow on the floor (this is NOT the blob-shadow case). Title "TPS Demo" in a large monospace slab font (Courier-like), top left. Four parallelogram buttons (PLAY, PLAY ONLINE, SETTINGS, QUIT), dark fill, white bold sans label (PT Sans-like), blue outline on the focused button. Robot on the right is in a hero pose, light-grey/white with blue emissive eyes and chest. |
| godot-02-level-start.png | Start of the level, third person | Warm amber/orange ambient with teal and yellow emissive accents; grey-white structure; diamond-plate floors. Player is the cream/yellow variant seen from behind, centred low. Debug overlay top left: FPS, VSync, Memory, Online. |
| godot-03-laser-fire.png | Player firing, over-the-shoulder | Camera is very close behind the right shoulder; the weapon arm/barrel fills the lower left. Small "+" crosshair at screen centre. Blue plasma blobs along the shot line. Red robots visible down the corridor. Three small white squares near the crosshair line (unidentified; check the HUD scene). |
| godot-04-laser-hit.png | Shot hitting the enemy | Blue flash at the impact point. Same framing. Frame ~1 s after 03. |
| godot-05-reactor-fire-a.png | Reactor area, player aiming at robots (user capture 2026-11-18) | A DIFFERENT, NEWER reference: a screenshot of a recent Godot build (editor run window, 1920x1080, not video-compressed; FPS 92). Much darker and more saturated than godot-01..04: deep orange-red ambient and bounce light, near-black shadows, a huge blown-out yellow reactor disc, strong rim light on the player, blue bullet with bloom. Position not recorded. |
| godot-06-reactor-fire-b.png | Same spot a moment later | A blue glowing sphere (bullet blast ball with a swirling face-like noise) hits the far robots, with a trail of small blue light balls behind it. |
| godot-07-441-main-menu.png | Main menu, Godot 4.4.1 (user capture 2026-11-18, 1920x1080, direct screenshot) | Newer reference set (godot-07..10, same session as godot-05/06). Floor darker than godot-01, robot cast shadow to the left, robot torso lit cool grey-white, floor cone fades to black. |
| godot-08-441-level-start.png | Level start, Godot 4.4.1 | MUCH darker and warmer than godot-02: brown-orange structure, dark green-grey diamond plate, amber glowing windows with teal accents, player dark red-bronze. The camera is not the same as godot-02 (player stands further along the rail; spawn marker is random in Godot). |
| godot-09-441-player-fire.png | Player firing down the road, Godot 4.4.1 | Same road view as godot-03 but with real GI: warm brown/orange bounce, grey road, amber ceiling lights, high contrast. Blue bullet trail with bloom. |
| godot-10-441-robot-hit.png | Shot hitting the robots, Godot 4.4.1 | A large glowing cyan sphere (bullet blast ball) with a swirling face-like noise at the robots, plus a blue muzzle glow. Reference for the bullet hit effect. |
| godot-11-441-reactor-entrance.png | Entering the reactor, Godot 4.4.1 | Symmetric hall: teal glowing towers left and right, orange-red reactor with a blown-out yellow disc, red-orange glow in the air, dark floors with light edging. Strong teal vs orange complementary colours. |
| godot-12-441-reactor-front.png | In front of the reactor, Godot 4.4.1 | Heavily red-orange lit (red mesh floor), teal tower on the left; the player is lit orange from the reactor and teal from the tower. Local coloured lights dominate. |
| godot-13-441-standing-light-shadows.png | Next to a standing (tripod) work light, Godot 4.4.1 | The tripod light at the right (two bright panels) lights the player pink-white from the right and casts shadows on the floor: the reference for the 13 shadow-casting spot lights. Rest of the room is warm brown/amber. |

Calibration targets: overall warm/orange tint with strong contrast, bright emissive strips, soft ambient in shadows (not crushed black), blue plasma with a bloom halo.

## Milestone shots of this port added after the shadows pass (never overwritten)
mono-43 robot-fired laser (peak), mono-44 laser impact (0.5 s), mono-45 robot buildup, mono-46 bullet hit with bouncing light balls, mono-47 forklift exhaust trail (TPS_LEVEL_TIME orbit), mono-48 forklift spot light cone, mono-49 LaserCenter at 1.4 s, mono-50 / mono-51 spot shadows ON vs the old key-light map at the same lamp pose (`tp:33,-3.5,77.5,-90;wait:1.5;look:90,-8`), mono-52 / mono-53 default and aim views with spot shadows as the default. mono-36/37 wall z-fight before/after, mono-38 lighting pass 5, mono-39 menu shadow, mono-40..42 key-light shadows.
Rule: every milestone shot a pass produces goes here (next free mono number); tuning shots go in `_tuning/`.
