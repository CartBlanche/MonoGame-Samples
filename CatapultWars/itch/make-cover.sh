#!/usr/bin/env bash
# Makes the animated itch.io cover for one look: the shot flies, the camera zooms in and slows, and it hits.
#
#   itch/make-cover.sh <theme> [--seed N] [--no-build] [--keep]
#
#   <theme>      none (the classic look), halloween, winter ... any name --theme= understands
#   --seed N     play the same battlefield every time (default: random, so a different shot each run)
#   --no-build   skip building the Desktop project first
#   --keep       leave the captured frames in a temp folder and print where
#
# Output: itch/cover-<theme>.gif, 630x500, under itch's 3 MB limit. Needs ImageMagick (magick) and dotnet.
set -euo pipefail

THEME="${1:-}"
if [[ -z "$THEME" || "$THEME" == --* ]]; then
    sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
    exit 1
fi
shift

SEED=""; BUILD=1; KEEP=0
while [[ $# -gt 0 ]]; do
    case "$1" in
        --seed) SEED="$2"; shift 2 ;;
        --no-build) BUILD=0; shift ;;
        --keep) KEEP=1; shift ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
done

command -v magick >/dev/null || { echo "ImageMagick (magick) is required." >&2; exit 1; }

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/Platforms/Desktop/CatapultNetWars.csproj"
OUT="$ROOT/itch/cover-$THEME.gif"
FRAMES="$(mktemp -d "${TMPDIR:-/tmp}/catapult-cover.XXXXXX")"
[[ $KEEP -eq 1 ]] || trap 'rm -rf "$FRAMES"' EXIT

# The letterbox bars: a dark shade of each look's dusk
case "$THEME" in
    winter) BARS='#141a38' ;;
    *)      BARS='#1a1830' ;;
esac

# Frame size: the game is 800x480, scaled to 630 wide and centred on a 630x500 canvas
WIDTH=630; HEIGHT=500
LIMIT=$((3 * 1024 * 1024 - 100 * 1024))   # stay a little under 3 MB

[[ $BUILD -eq 0 ]] || dotnet build "$PROJECT" -v q --nologo | tail -n 3

ARGS=(--screenshots="$FRAMES" --clip --theme="$THEME")
[[ -z "$SEED" ]] || ARGS+=(--seed="$SEED")
echo "Capturing the shot ($THEME)..."
dotnet run --no-build --project "$PROJECT" -- "${ARGS[@]}" > "$FRAMES/game.log" 2>&1 || { cat "$FRAMES/game.log" >&2; exit 1; }

shopt -s nullglob
CAPTURED=("$FRAMES"/frame*.png)
[[ ${#CAPTURED[@]} -gt 0 ]] || { echo "The game saved no frames; see $FRAMES/game.log" >&2; KEEP=1; exit 1; }
echo "Captured ${#CAPTURED[@]} frames."

# Build the GIF. If it is too big, thin the colours and then the frames until it fits.
make_gif() {  # colours, keep every Nth frame, delay in 1/100 s
    local colours="$1" every="$2" delay="$3" picked=() i=0
    for f in "${CAPTURED[@]}"; do
        (( i++ % every == 0 )) && picked+=("$f")
    done
    magick -delay "$delay" "${picked[@]}" \
        -resize "${WIDTH}x" -background "$BARS" -gravity center -extent "${WIDTH}x${HEIGHT}" \
        +repage -colors "$colours" -dither FloydSteinberg -loop 0 -layers OptimizePlus "$OUT"
}

for attempt in "128 1 7" "96 1 7" "64 1 7" "64 2 14" "48 2 14"; do
    set -- $attempt
    make_gif "$1" "$2" "$3"
    SIZE=$(wc -c < "$OUT")
    if (( SIZE <= LIMIT )); then
        printf 'Wrote %s (%s colours, every %s frame, %s KB)\n' "$OUT" "$1" "$2" $((SIZE / 1024))
        [[ $KEEP -eq 0 ]] || echo "Frames kept in $FRAMES"
        exit 0
    fi
done
echo "Could not get the GIF under 3 MB (last try $((SIZE / 1024)) KB)." >&2
exit 1
