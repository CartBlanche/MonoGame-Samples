#!/usr/bin/env bash
# Trims the OBS recording to the story, from the cue timestamps record-video.sh wrote to video/timing.txt, and adds fades.
#
#   itch/video/edit-video.sh <recording.mov> [--lead S] [--tail S]
#
#   --lead S   seconds of the parked demo kept before the go cue (default 0.3)
#   --tail S   seconds kept after the quit cue (default 0)
# Output: next to the recording, "<name> final.mp4" (H.264 + AAC, 0.4 s fade in and 0.8 s fade out).
set -euo pipefail
REC="${1:?usage: edit-video.sh <recording> [--lead S] [--tail S]}"; shift || true
LEAD=0.3; TAIL=0
while [[ $# -gt 0 ]]; do
    case "$1" in
        --lead) LEAD="$2"; shift 2 ;;
        --tail) TAIL="$2"; shift 2 ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
done
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
T="$ROOT/video/timing.txt"
REC_START="$(sed -n 's/^recording_started_unix=//p' "$T" | tail -1)"
GO_MS="$(grep -h 'cue go ' "$ROOT/video/New.log" | sed -n 's/.*unix-ms \([0-9]*\).*/\1/p' | tail -1)"
QUIT_MS="$(grep -h 'cue quit ' "$ROOT/video/New.log" | sed -n 's/.*unix-ms \([0-9]*\).*/\1/p' | tail -1)"
[[ -n "$REC_START" && -n "$GO_MS" && -n "$QUIT_MS" ]] || { echo "timing.txt / New.log have no recording start, go or quit cue" >&2; exit 1; }
read -r START END < <(python3 -I -c "
r=$REC_START; g=$GO_MS/1000.0; q=$QUIT_MS/1000.0
s=max(0.0, g-r-$LEAD); e=q-r+$TAIL
print('%.3f %.3f' % (s, e))")
DUR="$(python3 -I -c "print('%.3f' % ($END-$START))")"
OUTFILE="${REC%.*} final.mp4"
echo "cut $START s to $END s ($DUR s) -> $OUTFILE"
ffmpeg -hide_banner -loglevel error -y -ss "$START" -t "$DUR" -i "$REC" \
    -vf "fade=t=in:st=0:d=0.4,fade=t=out:st=$(python3 -I -c "print('%.2f' % ($DUR-0.8))"):d=0.8" \
    -af "afade=t=in:st=0:d=0.4,afade=t=out:st=$(python3 -I -c "print('%.2f' % ($DUR-0.8))"):d=0.8" \
    -c:v libx264 -crf 18 -preset medium -pix_fmt yuv420p -c:a aac -b:a 192k "$OUTFILE"
ffprobe -v error -show_entries format=duration -of csv=p=0 "$OUTFILE"
