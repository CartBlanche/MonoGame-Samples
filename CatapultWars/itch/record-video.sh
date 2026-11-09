#!/usr/bin/env bash
# Plays the scripted LAN match for the YouTube video: Dominique hosts, Dean finds the game and joins,
# and they play to a win (Dean). Run it once per look and cut the three recordings together in post.
#
#   itch/record-video.sh <theme> [--obs] [--lead N] [--client-delay N] [--no-build] [--seed N]
#
#   <theme>           none (classic), halloween, winter ... any name --theme= understands
#   --obs             also drive OBS: switch to the "CatapultWars LAN" scene, point its two window
#                     sources at the game windows, record, and stop when the game quits.
#                     Needs the OBS_PASSWORD environment variable (OBS WebSocket password), and
#                     optionally OBS_HOST / OBS_PORT (default localhost:4455). See itch/obs.py.
#   --lead N          seconds on the main menu before the host starts (default 8, or 22 with --obs
#                     so the recording is running before anything happens)
#   --client-delay N  seconds after the host starts before Dean's window starts (default 8, 4 with --obs)
#   --no-build        skip building the Desktop project first
#   --seed N          battlefield seed (default 12345; keep it the same for all three runs)
#   --attempts N      takes before giving up (default 3). A take that diverges from the plan (a shot
#                     that should have hit did not) or runs past --max-seconds (default 330) is
#                     stopped, its recording deleted, and it starts again.
#
# Two windows open side by side (Dominique left, Dean right, each 800x480 points). Keep both fully
# visible: a covered window gets throttled and the match stalls. Without --obs, record them yourself.
# Dean's copy is muted; the sound comes from Dominique's window.
# Each instance writes a cue log (when each shot is aimed, fired, lands, scores) to
# video/<theme>/host/cues-host.csv and video/<theme>/guest/cues-client.csv, for the edit, and with
# --obs video/<theme>/timing.txt says when the recording started (unix seconds) and where the file is.
# The game quits by itself about 9 seconds after the victory screen appears.
set -euo pipefail

THEME="${1:-}"
if [[ -z "$THEME" || "$THEME" == --* ]]; then
    sed -n '2,29p' "$0" | sed 's/^# \{0,1\}//'
    exit 1
fi
shift

LEAD=""; DELAY=""; BUILD=1; SEED=12345; OBS=0; ATTEMPTS=3; MAX_SECONDS=330
while [[ $# -gt 0 ]]; do
    case "$1" in
        --obs) OBS=1; shift ;;
        --lead) LEAD="$2"; shift 2 ;;
        --client-delay) DELAY="$2"; shift 2 ;;
        --no-build) BUILD=0; shift ;;
        --seed) SEED="$2"; shift 2 ;;
        --attempts) ATTEMPTS="$2"; shift 2 ;;
        --max-seconds) MAX_SECONDS="$2"; shift 2 ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
done
if [[ $OBS -eq 1 ]]; then
    : "${LEAD:=22}" "${DELAY:=4}"
    if [[ -z "${OBS_PASSWORD:-}" ]]; then
        echo "--obs needs the OBS_PASSWORD environment variable (OBS WebSocket password)" >&2
        exit 1
    fi
    export OBS_HOST="${OBS_HOST:-localhost}"
else
    : "${LEAD:=8}" "${DELAY:=8}"
fi

# Dean starts clicking about 6 seconds after Dominique, so the game he looks for exists by then
# and Dominique sits in the lobby alone for a few seconds first
GUEST_LEAD=$(( LEAD + 6 - DELAY ))
[[ $GUEST_LEAD -lt 1 ]] && GUEST_LEAD=1

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/Platforms/Desktop/CatapultNetWars.csproj"
OUT="$ROOT/video/$THEME"
mkdir -p "$OUT"
obs() { python3 -I "$ROOT/itch/obs.py" "$@"; }

if [[ $BUILD -eq 1 ]]; then
    dotnet build "$PROJECT" -v q --nologo
fi

HOST=""; GUEST=""; RECORDING=0; WATCHDOG=""
stop_recording() {
    if [[ $RECORDING -eq 1 ]]; then
        sleep 2
        FILE="$(obs stop || true)"
        echo "Recording: $FILE"
        echo "recording_file=$FILE" >> "$OUT/timing.txt"
        RECORDING=0
    fi
}
cleanup() {
    stop_recording
    [[ -n "$WATCHDOG" ]] && kill "$WATCHDOG" 2>/dev/null || true
    [[ -n "$HOST" ]] && kill "$HOST" 2>/dev/null || true
    [[ -n "$GUEST" ]] && kill "$GUEST" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

if [[ $OBS -eq 1 ]]; then
    obs status >/dev/null     # fail early if OBS cannot be reached
    obs scene "CatapultWars LAN"
    # The mic stays out of the recording
    obs call SetInputMute '{"inputName":"Mic/Aux","inputMuted":true}' >/dev/null 2>&1 || true
fi

COMMON=(--video --theme="$THEME" --seed="$SEED")

# One take. Returns 0 when the match played through to the victory screen, non-zero when the game
# diverged from the plan or took too long (a match is about 2.5 minutes, so MAX_SECONDS is generous).
take() {
    # A game window left over from an earlier take would join in (and get captured), so none may be running
    if pkill -f "[D]ebug/net10.0/CatapultNetWars" 2>/dev/null; then
        echo "Stopped a leftover game window" >&2
        sleep 2
    fi
    dotnet run --no-build --project "$PROJECT" -- "${COMMON[@]}" --lan=host --name="Dominique" \
        --lead="$LEAD" --pos=40,60 --screenshots="$OUT/host" &
    HOST=$!

    sleep "$DELAY"
    dotnet run --no-build --project "$PROJECT" -- "${COMMON[@]}" --lan=client --name="Dean" --mute \
        --lead="$GUEST_LEAD" --pos=860,60 --screenshots="$OUT/guest" &
    GUEST=$!

    # Watchdog: whatever goes wrong, the games (and so the recording) end after MAX_SECONDS
    ( sleep "$MAX_SECONDS"; echo "Took longer than ${MAX_SECONDS}s: stopping" >&2; kill $HOST $GUEST 2>/dev/null ) &
    WATCHDOG=$!

    if [[ $OBS -eq 1 ]]; then
        sleep 3
        # "set -e" does not apply inside "if take", so a failure here has to end the take explicitly
        if ! obs bind; then
            echo "Could not bind the OBS sources to the game windows" >&2
            kill $HOST $GUEST "$WATCHDOG" 2>/dev/null || true
            wait $HOST $GUEST 2>/dev/null || true
            HOST=""; GUEST=""; WATCHDOG=""
            return 1
        fi
        START="$(python3 -I -c 'import time; print("%.3f" % time.time())')"
        obs start || { kill $HOST $GUEST "$WATCHDOG" 2>/dev/null || true; HOST=""; GUEST=""; WATCHDOG=""; return 1; }
        RECORDING=1
        echo "recording_started_unix=$START" > "$OUT/timing.txt"
    fi

    local rc_host=0 rc_guest=0
    wait $HOST || rc_host=$?
    wait $GUEST || rc_guest=$?
    kill "$WATCHDOG" 2>/dev/null || true
    HOST=""; GUEST=""; WATCHDOG=""
    [[ $rc_host -eq 0 && $rc_guest -eq 0 ]]
}

for ATTEMPT in $(seq 1 "$ATTEMPTS"); do
    if take; then
        echo "Cue logs: $OUT/host/cues-host.csv and $OUT/guest/cues-client.csv"
        exit 0
    fi
    stop_recording
    echo "Take $ATTEMPT failed (see the cue logs in $OUT)." >&2
    BAD="$(grep recording_file "$OUT/timing.txt" 2>/dev/null | tail -1 | cut -d= -f2- || true)"
    if [[ -n "$BAD" && -f "$BAD" ]]; then
        echo "Removing the failed recording $BAD" >&2
        rm -f "$BAD"
    fi
    sleep 3
done
echo "Giving up after $ATTEMPTS takes." >&2
exit 1
