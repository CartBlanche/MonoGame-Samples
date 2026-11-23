#!/usr/bin/env python3
"""Cuts between the Old and New game windows while record-video.sh records, following the games' own cue lines.

  OBS_PASSWORD=... itch/video/director.py <folder with Old.log, New.log and the go file>

The script of the video:
  both copies are parked on the rolling demo (same track, same replay point); the recording starts on the Old one
  (Lighting Off, the original look) with its overlay texts; the go file starts both demos together;
  at the "swap" cue (printed by both games at the same demo time) swipe to the New one, which is at exactly the same
  point of the replay, a few seconds before the tunnel so the headlights come on; later its clock runs to night.
Each game prints "[DEVSHOT] cue <name> unix-ms <n> demo-s <t>" (see Core/Helpers/DevShots.cs). If a cue never comes the
director carries on after a timeout, so a take is never stuck.
"""
import os, sys, time
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import obs as obsmod

SWIPE_MS = 700


def cues(folder, name):
    try:
        lines = open(os.path.join(folder, name + ".log"), errors="replace").read().splitlines()
    except OSError:
        return []
    return [l.split("cue ", 1)[1].split()[0] for l in lines if "[DEVSHOT] cue " in l]


def wait(folder, name, cue, timeout):
    end = time.time() + timeout
    while time.time() < end:
        if cue in cues(folder, name):
            return True
        time.sleep(0.02)
    print("director: no %r cue from %s after %ds, carrying on" % (cue, name, timeout), file=sys.stderr)
    return False


def main(folder):
    # DIRECTOR_NO_OBS=1: no OBS (a run to check the games and their cues)
    obs = None if os.environ.get("DIRECTOR_NO_OBS") else obsmod.Obs(
        os.environ.get("OBS_HOST", "localhost"), int(os.environ.get("OBS_PORT", "4455")), os.environ.get("OBS_PASSWORD", ""))
    try:
        if obs:
            obsmod.cut(obs, "Old")
        wait(folder, "Old", "ready", 90)
        wait(folder, "New", "ready", 90)
        open(os.path.join(folder, "go"), "w").close()          # both demos start together
        print("director: go", flush=True)
        wait(folder, "New", "swap", 120)
        if obs:
            obsmod.cut(obs, "New", True, SWIPE_MS)
        print("director: swipe to New", flush=True)
        wait(folder, "New", "quit", 300)
        print("director: done", flush=True)
    finally:
        if obs:
            obs.close()


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(1)
    try:
        main(sys.argv[1])
    except (RuntimeError, OSError) as e:
        print("director: %s" % e, file=sys.stderr)
        sys.exit(2)
