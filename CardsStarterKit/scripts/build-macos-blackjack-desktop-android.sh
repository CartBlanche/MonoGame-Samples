#!/usr/bin/env bash
set -euo pipefail

# Build Blackjack Desktop bundles with MonoPack and Android release binaries on macOS.
# Outputs are written under ./artifacts/<name>.<build_number>/

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Default to itch.io build; use --steam to switch to the Steam build.
DESKTOP_VARIANT="${DESKTOP_VARIANT:-itch}"
CONFIGURATION="${CONFIGURATION:-Release}"
BUILD_NUMBER="${BUILD_NUMBER:-$(date +%Y%m%d%H%M%S)}"
ANDROID_FRAMEWORK="${ANDROID_FRAMEWORK:-net9.0-android}"

DESKTOP_ITCH_PROJECT="${ROOT_DIR}/3-Games/Blackjack/Desktop/BlackJack.csproj"
DESKTOP_STEAM_PROJECT="${ROOT_DIR}/3-Games/Blackjack/Desktop/BlackJack.Steam.csproj"
ANDROID_PROJECT="${ROOT_DIR}/3-Games/Blackjack/Android/BlackJack.csproj"

ARTIFACTS_ROOT="${ROOT_DIR}/artifacts"

usage() {
  cat <<'EOF'
Usage: scripts/build-macos-blackjack-desktop-android.sh [options]

Options:
  --build-number <value>     Build number suffix for artifact folders.
  --configuration <value>    Build configuration. Default: Release
  --steam                    Package the desktop build for Steam instead of itch.io.
  --skip-desktop             Skip Desktop MonoPack packaging.
  --skip-android             Skip Android release publish.
  --help                     Show this help text.

Environment overrides:
  BUILD_NUMBER, CONFIGURATION, ANDROID_FRAMEWORK, DESKTOP_VARIANT (itch|steam)

Examples:
  scripts/build-macos-blackjack-desktop-android.sh
  BUILD_NUMBER=42 scripts/build-macos-blackjack-desktop-android.sh
  scripts/build-macos-blackjack-desktop-android.sh --steam
  scripts/build-macos-blackjack-desktop-android.sh --skip-android
EOF
}

SKIP_DESKTOP=0
SKIP_ANDROID=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --build-number)
      BUILD_NUMBER="$2"
      shift 2
      ;;
    --configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    --steam)
      DESKTOP_VARIANT="steam"
      shift
      ;;
    --skip-desktop)
      SKIP_DESKTOP=1
      shift
      ;;
    --skip-android)
      SKIP_ANDROID=1
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ "${DESKTOP_VARIANT}" == "steam" ]]; then
  DESKTOP_PROJECT="${DESKTOP_STEAM_PROJECT}"
else
  DESKTOP_PROJECT="${DESKTOP_ITCH_PROJECT}"
fi

DESKTOP_NAME="$(basename "${DESKTOP_PROJECT}" .csproj)"
DESKTOP_OUT_DIR="${ARTIFACTS_ROOT}/${DESKTOP_NAME}.${BUILD_NUMBER}"
ANDROID_OUT_DIR="${ARTIFACTS_ROOT}/Blackjack.Android.${BUILD_NUMBER}"

log() {
  printf '\n[%s] %s\n' "$(date +%H:%M:%S)" "$*"
}

require_cmd() {
  local cmd="$1"
  if ! command -v "${cmd}" >/dev/null 2>&1; then
    echo "Required command not found: ${cmd}" >&2
    exit 1
  fi
}

install_monopack_if_missing() {
  if command -v monopack >/dev/null 2>&1; then
    return
  fi

  log "MonoPack not found. Installing as a global dotnet tool..."
  dotnet tool install --global MonoPack

  if [[ ":${PATH}:" != *":${HOME}/.dotnet/tools:"* ]]; then
    export PATH="${HOME}/.dotnet/tools:${PATH}"
  fi

  if ! command -v monopack >/dev/null 2>&1; then
    echo "MonoPack installed but not in PATH. Add ${HOME}/.dotnet/tools to PATH and rerun." >&2
    exit 1
  fi
}

package_desktop() {
  if [[ ! -f "${DESKTOP_PROJECT}" ]]; then
    echo "Desktop project not found: ${DESKTOP_PROJECT}" >&2
    exit 1
  fi

  local project_dir info_plist icns_file
  project_dir="$(dirname "${DESKTOP_PROJECT}")"
  info_plist="${project_dir}/Info.plist"
  icns_file="${ROOT_DIR}/2-Core/Resources/Icons/macOS/Icon.icns"

  if [[ ! -f "${info_plist}" ]]; then
    echo "Info.plist required for MonoPack was not found: ${info_plist}" >&2
    echo "Create 3-Games/Blackjack/Desktop/Info.plist (or adjust this script path) and rerun." >&2
    exit 1
  fi

  mkdir -p "${DESKTOP_OUT_DIR}"

  log "Packaging Desktop bundles with MonoPack"
  log "Variant: ${DESKTOP_VARIANT}"
  log "Project: ${DESKTOP_PROJECT}"
  log "Output: ${DESKTOP_OUT_DIR}"

  local common_args
  common_args=(
    -p "${DESKTOP_PROJECT}"
    -o "${DESKTOP_OUT_DIR}"
    -rids "win-x64,linux-x64,osx-x64,osx-arm64"
    -i "${info_plist}"
    -v
    --macos-universal
    --publish-args "-p:Configuration=${CONFIGURATION} -p:PublishSingleFile=true"
  )

  if [[ -f "${icns_file}" ]]; then
    log "Using icon: ${icns_file}"
    monopack "${common_args[@]}" -c "${icns_file}"
  else
    log "No macOS icon found at ${icns_file}; packaging without custom icon"
    monopack "${common_args[@]}"
  fi

  log "Desktop artifacts generated"
  find "${DESKTOP_OUT_DIR}" -type f \( -name "*.zip" -o -name "*.tar.gz" \) | sort || true
}

publish_android_release() {
  if [[ ! -f "${ANDROID_PROJECT}" ]]; then
    echo "Android project not found: ${ANDROID_PROJECT}" >&2
    exit 1
  fi

  mkdir -p "${ANDROID_OUT_DIR}"

  log "Publishing Android release binaries"
  log "Project: ${ANDROID_PROJECT}"
  log "Output: ${ANDROID_OUT_DIR}"

  dotnet publish "${ANDROID_PROJECT}" \
    -c "${CONFIGURATION}" \
    -f "${ANDROID_FRAMEWORK}" \
    -o "${ANDROID_OUT_DIR}" \
    -p:AndroidKeyStore=False

  log "Android artifacts generated"
  find "${ANDROID_OUT_DIR}" -type f \( -name "*.apk" -o -name "*.aab" -o -name "*.dll" \) | sort || true
}

main() {
  require_cmd dotnet
  require_cmd bash

  mkdir -p "${ARTIFACTS_ROOT}"

  if [[ "${SKIP_DESKTOP}" -eq 0 ]]; then
    install_monopack_if_missing
    package_desktop
  else
    log "Skipping Desktop packaging"
  fi

  if [[ "${SKIP_ANDROID}" -eq 0 ]]; then
    publish_android_release
  else
    log "Skipping Android publish"
  fi

  log "Build complete"
  log "Artifacts root: ${ARTIFACTS_ROOT}"
}

main