#!/usr/bin/env bash
# Records the Racing Game "old lighting, new lighting" video from the rolling demo.
#
#   Two copies of the game run side by side as two windows: "Old" (the original game as it is on the main branch, built in
#   its own worktree under video/old-main with itch/video/old-main.patch) and "New" (this branch: the default lighting and shadows). Both are parked on the rolling demo; the recording starts on Old; a go file starts both demos together,
#   so they are at the same point of the replay all the time. Old shows the overlay texts; at the swap time the director swipes
#   to New, a few seconds before the tunnel (the headlights come on), then New's clock runs through dusk to night.
#
#   itch/video/record-video.sh [--obs] [--no-build] [--offset S] [--swap S] [--quit S] [--day H]
#
#   --obs         drive OBS: make the two scenes, record, swipe, stop at the end. Needs the OBS_PASSWORD environment
#                 variable (OBS WebSocket password; never stored here); OBS_HOST / OBS_PORT optional (localhost:4455).
#                 Without it the two games just play the script (to check it, and to read the tunnel time from the log).
#   --no-build    skip building (the game is built with the DevShots switch: dotnet build Desktop -c Release -p:DevShots=true)
#   --offset S    seconds into the replay where the demo starts (default 32). The tunnel is entered at 45.0 s of the replay
#                 ("tunnel-enter ... demo-s 45.0" in video/New.log), so offset 32 with swap 9 puts it 4 s after the swap.
#                 The demo is fast-forwarded to the offset while the loading card shows.
#   --swap S      seconds after the go file when the swipe to New happens (default 9)
#   --quit S      seconds after the go file when both games quit (default 45)
#   --day H       hour of the day the New copy shows until the night (default 15)
#
# Keep both windows fully visible: a window that is covered or minimised is throttled. Run it from a Terminal, with the
# display awake (this script uses caffeinate). The games quit by themselves. Files go in video/ (git-ignored): a log per copy
# (with the cue lines) and timing.txt; with --obs OBS writes the recording to its own folder (the script prints the path).
set -euo pipefail

BUILD=1; OBS=0; OFFSET=32; SWAP=9; QUIT=45; DAY=15
while [[ $# -gt 0 ]]; do
    case "$1" in
        --obs) OBS=1; shift ;;
        --no-build) BUILD=0; shift ;;
        --offset) OFFSET="$2"; shift 2 ;;
        --swap) SWAP="$2"; shift 2 ;;
        --quit) QUIT="$2"; shift 2 ;;
        --day) DAY="$2"; shift 2 ;;
        -h|--help) sed -n '2,23p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
done

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/video"
mkdir -p "$OUT"
DLL="$ROOT/Desktop/bin/Release/net10.0/RacingGame.dll"
OLD="$OUT/old-main"            # worktree of the main branch with the harness hooks
OLD_DLL="$OLD/RacingGame/Desktop/bin/Release/net10.0/RacingGame.dll"
obs() { python3 -I "$ROOT/itch/video/obs.py" "$@"; }

if [[ $BUILD -eq 1 ]]; then
    dotnet build "$ROOT/Desktop" -c Release -p:DevShots=true -v q --nologo
    # the original game: a detached worktree of main, with the harness hooks (the harness file is shared)
    if [[ ! -d "$OLD" ]]; then git -C "$ROOT" worktree add --detach "$OLD" main; fi
    git -C "$OLD" checkout -q -- . && git -C "$OLD" checkout -q --detach main
    git -C "$OLD" apply "$ROOT/itch/video/old-main.patch"
    cp "$ROOT/Core/Helpers/DevShots.cs" "$OLD/RacingGame/Core/Helpers/DevShots.cs"
    dotnet build "$OLD/RacingGame/Desktop" -c Release -p:DevShots=true -v q --nologo
fi
if [[ $OBS -eq 1 && -z "${OBS_PASSWORD:-}" ]]; then
    echo "--obs needs the OBS_PASSWORD environment variable (OBS WebSocket password)" >&2
    exit 1
fi

PIDS=(); RECORDING=0; WATCHDOG=""; DIRECTOR=""
stop_recording() {
    if [[ $RECORDING -eq 1 ]]; then
        sleep 2
        FILE="$(obs stop || true)"
        echo "Recording: $FILE"
        echo "recording_file=$FILE" >> "$OUT/timing.txt"
        RECORDING=0
        if [[ -n "$FILE" ]]; then
            for _ in $(seq 1 15); do     # OBS finishes writing the file a moment after "stop"
                if ffprobe -v error -show_entries stream=codec_type,codec_name,width,height -show_entries format=duration -of compact "$FILE" 2>/dev/null; then
                    break
                fi
                sleep 2
            done
        fi
    fi
}
cleanup() {
    [[ -n "$WATCHDOG" ]] && kill "$WATCHDOG" 2>/dev/null || true
    [[ -n "$DIRECTOR" ]] && kill "$DIRECTOR" 2>/dev/null || true
    stop_recording
    for p in "${PIDS[@]:-}"; do kill "$p" 2>/dev/null || true; done
}
trap cleanup EXIT INT TERM

if pkill -f "[R]acingGame.dll" 2>/dev/null; then echo "Stopped a leftover game window" >&2; sleep 2; fi

if [[ $OBS -eq 1 ]]; then
    if obs status | grep -q "^recording"; then
        echo "OBS is already recording: stop that first (this script only stops a recording it started)" >&2
        exit 1
    fi
    obs call SetInputMute '{"inputName":"Mic/Aux","inputMuted":true}' >/dev/null 2>&1 || true
fi

rm -f "$OUT/go"
for n in Old New; do : > "$OUT/$n.log"; done
echo "script=old and new lighting, offset=$OFFSET swap=$SWAP quit=$QUIT" > "$OUT/timing.txt"

# The overlay texts (the game's own font), seconds after the go file. The tunnel comes about 4 s after the swap.
S=$SWAP
f() { python3 -I -c "print(round($1, 2))"; }
OLD_TEXTS="0.6-3.6|The old Racing Game lighting model dates back to 2007;3.9-5.6|This was great for its time.;5.9-$(f "$S-0.4")|But it's time for an update!"
NEW_TEXTS="$(f "$S+0.8")-$(f "$S+6.3")|Now with dynamic time of day lighting, real shadows and softer shadow edges;$(f "$S+17")-$(f "$S+23")|Headlights, street lamps and the Milky Way at night"
# New's clock: steady daylight, then (after the tunnel) a quick run through dusk to night
NEW_CLOCK="0:$DAY,$(f "$S+9"):$DAY,$(f "$S+19"):22.5,300:22.5"

common=(DEVSHOT_GOFILE="$OUT/go" DEVSHOT_OFFSET="$OFFSET" DEVSHOT_CUES="swap:$SWAP" DEVSHOT_QUITAT="$QUIT")
start() {   # name, dll, window, extra environment...
    local name="$1" dll="$2" win="$3"; shift 3
    env "${common[@]}" DEVSHOT_NAME="$name" DEVSHOT_WIN="$win" \
        RACINGGAME_TITLE_SUFFIX="- $name" "$@" \
        caffeinate -d -i dotnet "$dll" >> "$OUT/$name.log" 2>&1 &
    PIDS+=($!)
    sleep 0.8       # so the windows have ascending ids: Old first
}
# Both windows are 1280x720 points (the picture is drawn at that size). Old sits on screen; New sits below it with only its top
# edge on screen, so it stays visible (a covered window is throttled) while OBS captures all of its picture.
start Old "$OLD_DLL" "0,30,1280,720" DEVSHOT_TEXTS="$OLD_TEXTS"
start New "$DLL" "0,790,1280,720" DEVSHOT_TEXTS="$NEW_TEXTS" DEVSHOT_CLOCK="$NEW_CLOCK"

MAX=$(( QUIT + 120 ))
( sleep "$MAX"; echo "Took longer than ${MAX}s: stopping" >&2; pkill -f "[R]acingGame.dll" ) &
WATCHDOG=$!

for n in Old New; do
    for _ in $(seq 1 240); do
        grep -q "cue ready" "$OUT/$n.log" && break
        sleep 0.5
    done
    grep -q "cue ready" "$OUT/$n.log" || { echo "$n never got to the rolling demo" >&2; exit 1; }
done

if [[ $OBS -eq 1 ]]; then
    obs bind-all Old New
    obs cut Old
    START="$(python3 -I -c 'import time; print("%.3f" % time.time())')"
    obs start
    RECORDING=1
    echo "recording_started_unix=$START" >> "$OUT/timing.txt"
    sleep 1.5       # a moment of the parked demo first
else
    export DIRECTOR_NO_OBS=1
fi

python3 -I "$ROOT/itch/video/director.py" "$OUT" &
DIRECTOR=$!

wait "${PIDS[1]}" || true       # New is the last one started: when it quits, we are done
sleep 2
grep -h "cue" "$OUT/New.log" | tee -a "$OUT/timing.txt" || true
cleanup
trap - EXIT
echo "Done. Logs and timing are in $OUT"
