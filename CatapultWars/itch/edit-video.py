#!/usr/bin/env python3
"""Cuts the YouTube draft from the three recordings made by itch/record-video.sh (one per look).

  python3 itch/edit-video.py [--render] [--out FILE] [--swap-seconds N] [--hold N] [--refresh]

Without --render it only prints the plan (every segment, and how well each swap lines up).

The cut, for a match where Dean wins and both players stay on screen:
  1. Halloween main menu, the cursor gliding to Play and clicking.
  2. The Play menu, the cursor gliding to Multiplayer and clicking.
  3. Jump cut to Dean pulling back for his first hit, played until it lands.
  4. From one second after Dominique's next launch, swap look every second until victory:
     winter, classic, Halloween, winter, classic ... (hard cuts, to the same moment of the other take).
  5. The end screen in Halloween.
The sound is Halloween's throughout. The picture is the recording itself, both windows side by side,
with the black bars above and below filled by a darkened, blurred copy of it.

Why the swaps need care: the three takes drift against each other by a few tenths of a second (the game
slows a little when the machine is busy), so "the same moment" is found per swap. It is measured from the
Angle/Power readouts in the video (they turn yellow while a shot is pulled back, the same in every look)
and refined from the audio around each swap.

Needs ffmpeg/ffprobe and numpy. Reads video/<theme>/timing.txt and the cue logs written by
record-video.sh for halloween, winter and none; the work files go in video/edit/ (git-ignored).
"""
import argparse, json, os, re, subprocess, sys
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VIDEO = os.path.join(ROOT, "video")
WORK = os.path.join(VIDEO, "edit")
TAKES = {"H": "halloween", "W": "winter", "C": "none"}   # Halloween is the base take
LABEL = {"H": "Halloween", "W": "winter", "C": "classic"}
CYCLE = ["W", "C", "H"]
FPS = 60
STRIP = (252, 576)   # rows of the frame that hold the two game windows (y, height)
# (x, y, w, h) of the Angle/Power readouts in the left (host) window: Dominique's, then Dean's
REGIONS = {"D": (12, 355, 200, 90), "E": (742, 355, 200, 90)}


# ---- reading the takes ---------------------------------------------------------------------

def load(theme):
    """The recording's file, and the cue events as (video time estimate, name)."""
    base = os.path.join(VIDEO, theme)
    timing = open(os.path.join(base, "timing.txt")).read()
    rec = float(re.search(r"recording_started_unix=([\d.]+)", timing).group(1))
    file = re.search(r"recording_file=(.*)", timing).group(1).strip()
    lines = open(os.path.join(base, "host", "cues-host.csv")).read().splitlines()
    x0 = int(re.search(r"unix ms (\d+)", lines[1]).group(1)) / 1000.0
    off = x0 - rec
    events = []
    for line in lines[2:]:
        seconds, name = line.split(",", 1)
        events.append((float(seconds) + off, name))
    return file, events


def audio_env(path, rate=8000, hop=40):
    """Loudness envelope at rate/hop Hz (200 Hz); the game's sounds are the same in every look."""
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-vn", "-ac", "1", "-ar", str(rate),
                          "-f", "f32le", "-"], capture_output=True).stdout
    a = np.frombuffer(raw, dtype=np.float32)
    n = len(a) // hop
    return np.sqrt((a[: n * hop].reshape(n, hop) ** 2).mean(axis=1)), rate / hop


def yellow_counts(path, box):
    """Per frame, how many pixels of the readout region are pure yellow (a held pull)."""
    x, y, w, h = box
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-vf", f"crop={w}:{h}:{x}:{y},format=rgb24",
                          "-f", "rawvideo", "-"], capture_output=True).stdout
    frames = np.frombuffer(raw, dtype=np.uint8).reshape(-1, h, w, 3).astype(np.int16)
    mask = (frames[..., 0] > 235) & (frames[..., 1] > 235) & (frames[..., 2] < 70)
    return mask.reshape(len(frames), -1).sum(axis=1)


def hud_signals(files, refresh):
    """The yellow-readout signals of all takes, cached in video/edit/ until a recording changes."""
    os.makedirs(WORK, exist_ok=True)
    cache = os.path.join(WORK, "hud.json")
    key = [[files[k], os.path.getsize(files[k])] for k in "HWC"]
    if not refresh and os.path.exists(cache):
        saved = json.load(open(cache))
        if saved.get("key") == key:
            return saved["data"]
    print("measuring the readouts in the three recordings (about 30 s)...")
    data = {k: {r: yellow_counts(files[k], box).tolist() for r, box in REGIONS.items()} for k in "HWC"}
    json.dump({"key": key, "data": data}, open(cache, "w"))
    return data


def intervals(signal, threshold, minimum=30):
    on = np.array(signal) > threshold
    out, i = [], 0
    while i < len(on):
        if on[i]:
            j = i
            while j < len(on) and on[j]:
                j += 1
            if j - i > minimum:
                out.append((i / FPS, j / FPS))
            i = j
        else:
            i += 1
    return out


def f(t):
    return int(round(t * FPS))


# ---- the plan --------------------------------------------------------------------------------

def make_plan(args):
    files = {k: load(TAKES[k])[0] for k in "HWC"}
    for k, path in files.items():
        if not os.path.exists(path):
            sys.exit(f"missing recording for {TAKES[k]}: {path}")
    hud = hud_signals(files, args.refresh)

    # pull intervals (start, release) per take, measured in the video: Dominique (D) and Dean (E)
    pulls = {}
    for k in "HWC":
        pulls[k] = {}
        for r in "DE":
            sig = np.array(hud[k][r])
            pulls[k][r] = intervals(sig, 0.4 * np.percentile(sig, 99.5))
            if len(pulls[k][r]) != 7:
                sys.exit(f"expected 7 pulls for {r} in {TAKES[k]}, found {len(pulls[k][r])}: "
                         "is it the scripted match (video/<theme>/timing.txt)?")

    releases = sorted([(pulls["H"][r][i][1], r, i) for r in "DE" for i in range(7)])
    anchors = {k: [(t, pulls[k][r][i][1] - t) for t, r, i in releases] for k in "WC"}

    def anchor_delta(k, t):
        d = anchors[k][0][1]
        for at, ad in anchors[k]:
            if at <= t:
                d = ad
        return d

    env = {k: audio_env(files[k]) for k in "HWC"}
    rate = env["H"][1]

    def local_lag(k, t0, t1, guess, span=0.3):
        """Seconds to add to a Halloween time for the same moment in take k, from the audio around it."""
        h, x = env["H"][0], env[k][0]
        i0, i1 = int(t0 * rate), int(t1 * rate)
        a = h[i0:i1] - h[i0:i1].mean()
        best = (-9.0, guess)
        for lag in range(int((guess - span) * rate), int((guess + span) * rate) + 1):
            b = x[i0 + lag: i1 + lag]
            if len(b) != len(a):
                continue
            b = b - b.mean()
            c = float((a * b).sum() / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-9))
            if c > best[0]:
                best = (c, lag / rate)
        return best[1], best[0]

    # cue-based times (menu clicks, game over), corrected with what the video shows
    _, ev = load(TAKES["H"])
    fire_d = [t for t, e in ev if e == "Dominique (host) Firing"]
    eps = float(np.median([pulls["H"]["D"][i][1] - fire_d[i] for i in range(7)]))
    ripple1 = [t for t, e in ev if e.startswith("click MainMenuScreen")][0] - 0.2 + eps
    ripple2 = [t for t, e in ev if e.startswith("click PlayMenuScreen")][0] - 0.2 + eps
    fire_e = [t for t, e in ev if e == "Dean Firing"]
    game_over = [t for t, e in ev if e == "game over"][0]
    G = pulls["H"]["E"][6][1] + (game_over - fire_e[6])

    t_dean_aim = pulls["H"]["E"][1][0] - 0.3   # Dean's first hit is his 2nd shot: from just before he pulls
    t0 = pulls["H"]["D"][2][1]                 # Dominique's launch of the shot that follows

    segs = [("H", f(ripple1 - 1.3), f(1.8), f(ripple1 - 1.3)),
            ("H", f(ripple2 - 1.3), f(1.8), f(ripple2 - 1.3))]
    f_start, f_swap0, f_g = f(t_dean_aim), f(t0 + args.swap_seconds), f(G)
    segs.append(("H", f_start, f_swap0 - f_start, f_start))
    n, fs, swaps = 0, f_swap0, []
    while fs < f_g:
        take = CYCLE[n % 3]
        fe = min(fs + f(args.swap_seconds), f_g)
        if take == "H":
            segs.append(("H", fs, fe - fs, fs))
        else:
            guess = anchor_delta(take, fs / FPS)
            lag, r = local_lag(take, fs / FPS - 0.5, fe / FPS + 0.5, guess)
            if r < 0.5:
                lag = guess
            swaps.append((fs / FPS, take, guess, lag, r))
            segs.append((take, fs + f(lag), fe - fs, fs))
        fs, n = fe, n + 1
    segs.append(("H", f_g, f(args.hold), f_g))

    total = sum(s[2] for s in segs)
    print(f"Play click {ripple1:.2f}s, Multiplayer click {ripple2:.2f}s, Dean pulls from {t_dean_aim + 0.3:.2f}s, "
          f"Dominique launches {t0:.2f}s, game over {G:.2f}s (Halloween recording time)")
    print(f"{len(segs)} segments, {total} frames = {total / FPS:.1f}s")
    t_out = 0.0
    for take, start, frames, h_start in segs:
        print(f"  out {t_out:6.2f}s  {LABEL[take]:9s} source {start / FPS:7.2f}s  Halloween time {h_start / FPS:7.2f}s  "
              f"{frames:3d} frames")
        t_out += frames / FPS
    print("alignment of the swaps (Halloween time, take, from the readouts, refined from the audio, match):")
    for t, take, g, lag, r in swaps:
        note = "  <- weak match, used the readouts" if r < 0.5 else (
            f"  (moved {1000 * (lag - g):+.0f} ms)" if abs(lag - g) > 0.02 else "")
        print(f"  {t:7.2f}s {take}  {g:+.3f}  {lag:+.3f}  r={r:.2f}{note}")
    if swaps:
        rs = [s[4] for s in swaps]
        print(f"{len(swaps)} swaps, mean match {np.mean(rs):.2f}, lowest {min(rs):.2f}")
    return files, segs, f_start, f_g


# ---- rendering ------------------------------------------------------------------------------

def render(args, files, segs, f_start, f_g):
    idx = {"H": 0, "W": 1, "C": 2}
    lines = []
    for take in "HWC":
        ids = [i for i, s in enumerate(segs) if s[0] == take]
        if not ids:
            continue
        pads = "".join(f"[i{take}{j}]" for j in range(len(ids)))
        lines.append(f"[{idx[take]}:v]split={len(ids)}{pads}" if len(ids) > 1 else f"[{idx[take]}:v]null[i{take}0]")
        for j, i in enumerate(ids):
            _, start, frames, _ = segs[i]
            lines.append(f"[i{take}{j}]trim=start_frame={start}:end_frame={start + frames},setpts=PTS-STARTPTS[s{i}]")
    lines.append("".join(f"[s{i}]" for i in range(len(segs))) + f"concat=n={len(segs)}:v=1:a=0[vc]")
    y, h = STRIP
    lines.append("[vc]split=2[vm][vb]")
    lines.append(f"[vm]crop=1920:{h}:0:{y}[strip]")
    lines.append(f"[vb]crop=1920:{h}:0:{y},scale=-2:270,crop=480:270,boxblur=12:2,scale=1920:1080:flags=bicubic,"
                 "eq=brightness=-0.18:saturation=0.9[bg]")
    lines.append(f"[bg][strip]overlay=0:{y},format=yuv420p[vout]")
    # sound: Halloween's, over the same three stretches of its timeline as the picture's cuts
    ranges = [(segs[0][3], segs[0][3] + segs[0][2]), (segs[1][3], segs[1][3] + segs[1][2]), (f_start, f_g + f(args.hold))]
    lines.append("[0:a]asplit=3[a0][a1][a2]")
    for j, (a, b) in enumerate(ranges):
        d = (b - a) / FPS
        lines.append(f"[a{j}]atrim=start={a / FPS:.5f}:end={b / FPS:.5f},asetpts=PTS-STARTPTS,"
                     f"afade=t=in:st=0:d=0.012,afade=t=out:st={d - 0.012:.5f}:d=0.012[x{j}]")
    lines.append("[x0][x1][x2]concat=n=3:v=0:a=1[aout]")
    graph = os.path.join(WORK, "graph.txt")
    open(graph, "w").write(";\n".join(lines))
    cmd = ["ffmpeg", "-y", "-hide_banner", "-i", files["H"], "-i", files["W"], "-i", files["C"],
           "-filter_complex_script", graph, "-map", "[vout]", "-map", "[aout]",
           "-c:v", "libx264", "-preset", "medium", "-crf", "14", "-r", "60",
           "-c:a", "aac", "-b:a", "320k", "-movflags", "+faststart", args.out]
    print("rendering to", args.out)
    return subprocess.run(cmd).returncode


def main():
    p = argparse.ArgumentParser(description="Cuts the YouTube draft from the three recordings.")
    p.add_argument("--render", action="store_true", help="render the video (otherwise only print the plan)")
    p.add_argument("--out", default=os.path.expanduser("~/Movies/CatapultWars-video.mp4"), help="output file")
    p.add_argument("--swap-seconds", type=float, default=1.0, help="seconds between look swaps (default 1)")
    p.add_argument("--hold", type=float, default=6.0, help="seconds of the end screen (default 6)")
    p.add_argument("--refresh", action="store_true", help="measure the recordings again instead of using the cache")
    args = p.parse_args()
    files, segs, f_start, f_g = make_plan(args)
    os.makedirs(WORK, exist_ok=True)
    json.dump({"segments": segs}, open(os.path.join(WORK, "plan.json"), "w"))
    if args.render:
        sys.exit(render(args, files, segs, f_start, f_g))


if __name__ == "__main__":
    main()
