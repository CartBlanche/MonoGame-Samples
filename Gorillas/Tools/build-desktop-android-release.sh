#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

DESKTOP_PROJECT="${DESKTOP_PROJECT:-${ROOT_DIR}/Platforms/Desktop/Gorillas.csproj}"
ANDROID_PROJECT="${ANDROID_PROJECT:-${ROOT_DIR}/Platforms/Android/Gorillas.csproj}"
DESKTOP_INFO_PLIST="${DESKTOP_INFO_PLIST:-${ROOT_DIR}/Platforms/Desktop/Info.plist}"
DESKTOP_ICON="${DESKTOP_ICON:-${ROOT_DIR}/Platforms/Desktop/Gorillas.icns}"
CONFIGURATION="${CONFIGURATION:-Release}"
DESKTOP_RIDS="${DESKTOP_RIDS:-win-x64,linux-x64,osx-x64,osx-arm64}"
ARTIFACTS_ROOT="${ARTIFACTS_ROOT:-${ROOT_DIR}/artifacts}"
BUILD_NUMBER="${BUILD_NUMBER:-$(date +%Y%m%d%H%M%S)}"
# Output folders are worked out after the arguments are parsed, so --build-number takes effect.
DESKTOP_OUT_DIR_OVERRIDE="${DESKTOP_OUT_DIR:-}"
ANDROID_OUT_DIR_OVERRIDE="${ANDROID_OUT_DIR:-}"

SKIP_DESKTOP=0
SKIP_ANDROID=0

usage() {
  cat <<'EOF'
Usage: build-desktop-android-release.sh [options]

Options:
  --build-number <value>    Build number suffix for artifact folders.
  --configuration <value>   Build configuration. Default: Release
  --desktop-rids <value>    MonoPack RIDs list. Default: win-x64,linux-x64,osx-x64,osx-arm64 (the two macOS ones become one universal .app; single-file publish, which a universal build needs)
  --skip-desktop            Skip the Desktop/MonoPack packaging step.
  --skip-android            Skip Android build/publish.
  --help                    Show this help text.

Environment overrides:
  DESKTOP_PROJECT, ANDROID_PROJECT, DESKTOP_INFO_PLIST, DESKTOP_ICON,
  CONFIGURATION, DESKTOP_RIDS, ARTIFACTS_ROOT, BUILD_NUMBER,
  DESKTOP_OUT_DIR, ANDROID_OUT_DIR

Examples:
  ./Tools/build-desktop-android-release.sh
  DESKTOP_RIDS="osx-arm64" ./Tools/build-desktop-android-release.sh --skip-android
  BUILD_NUMBER=42 ./Tools/build-desktop-android-release.sh
EOF
}

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
    --desktop-rids)
      DESKTOP_RIDS="$2"
      shift 2
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

DESKTOP_OUT_DIR="${DESKTOP_OUT_DIR_OVERRIDE:-${ARTIFACTS_ROOT}/Gorillas.${BUILD_NUMBER}}"
ANDROID_OUT_DIR="${ANDROID_OUT_DIR_OVERRIDE:-${ARTIFACTS_ROOT}/Android.${BUILD_NUMBER}}"

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

  log "MonoPack not found. Installing as a local global dotnet tool..."
  dotnet tool install --global MonoPack

  if [[ ":${PATH}:" != *":${HOME}/.dotnet/tools:"* ]]; then
    export PATH="${HOME}/.dotnet/tools:${PATH}"
  fi

  if ! command -v monopack >/dev/null 2>&1; then
    echo "MonoPack installed but not in PATH. Add ${HOME}/.dotnet/tools to PATH and rerun." >&2
    exit 1
  fi
}

ensure_desktop_icon() {
  if [[ -f "${DESKTOP_ICON}" ]]; then
    return
  fi

  log "Desktop icns not found at ${DESKTOP_ICON}. Generating it from Icon.png..."
  if [[ -f "${ROOT_DIR}/Platforms/Desktop/generate-icns.sh" ]]; then
    bash "${ROOT_DIR}/Platforms/Desktop/generate-icns.sh"
  else
    echo "No icon generator found at ${ROOT_DIR}/Platforms/Desktop/generate-icns.sh" >&2
    exit 1
  fi
}

build_desktop_release() {
  if [[ ! -f "${DESKTOP_PROJECT}" ]]; then
    echo "Desktop project not found: ${DESKTOP_PROJECT}" >&2
    exit 1
  fi

  if [[ ! -f "${DESKTOP_INFO_PLIST}" ]]; then
    echo "Info.plist not found: ${DESKTOP_INFO_PLIST}" >&2
    exit 1
  fi

  ensure_desktop_icon
  mkdir -p "${ARTIFACTS_ROOT}"

  log "Building Desktop Release binaries"
  log "Project: ${DESKTOP_PROJECT}"
  log "Output: ${DESKTOP_OUT_DIR}"
  log "RIDs: ${DESKTOP_RIDS}"
  log "Icon: ${DESKTOP_ICON}"

  dotnet build "${DESKTOP_PROJECT}" -c "${CONFIGURATION}"

  monopack \
    -p "${DESKTOP_PROJECT}" \
    -e Gorillas \
    -o "${DESKTOP_OUT_DIR}" \
    -rids "${DESKTOP_RIDS}" \
    -i "${DESKTOP_INFO_PLIST}" \
    -c "${DESKTOP_ICON}" \
    -z \
    -v \
    --macos-universal \
    --publish-args "-p:Configuration=${CONFIGURATION} -p:PublishSingleFile=true"

  log "Desktop artifacts generated"
  find "${DESKTOP_OUT_DIR}" -maxdepth 1 -type f \( -name "*.zip" -o -name "*.tar.gz" -o -name "*.app" \) | sort || true
}

build_android_release() {
  if [[ ! -f "${ANDROID_PROJECT}" ]]; then
    echo "Android project not found: ${ANDROID_PROJECT}" >&2
    exit 1
  fi

  mkdir -p "${ARTIFACTS_ROOT}"

  log "Restoring Android workload and building Android Release"
  log "Project: ${ANDROID_PROJECT}"
  log "Output: ${ANDROID_OUT_DIR}"

  sudo dotnet workload restore "${ANDROID_PROJECT}"
  dotnet publish "${ANDROID_PROJECT}" \
    -c "${CONFIGURATION}" \
    -f net10.0-android \
    -o "${ANDROID_OUT_DIR}" \
    -p:AndroidKeyStore=False

  # The signing step leaves the APK with a 1981 timestamp; give it the real build time.
  find "${ANDROID_OUT_DIR}" -name "*.apk" -exec touch {} +

  log "Android artifacts generated"
  find "${ANDROID_OUT_DIR}" -type f \( -name "*.apk" -o -name "*.aab" -o -name "*.dll" \) | sort || true
}

main() {
  require_cmd dotnet
  require_cmd bash
  require_cmd find
  require_cmd sort
  require_cmd mkdir

  if [[ "${SKIP_DESKTOP}" -eq 0 ]]; then
    require_cmd monopack || true
    install_monopack_if_missing
    build_desktop_release
  else
    log "Skipping Desktop packaging"
  fi

  if [[ "${SKIP_ANDROID}" -eq 0 ]]; then
    build_android_release
  else
    log "Skipping Android build"
  fi

  log "Release build complete"
  log "Artifacts root: ${ARTIFACTS_ROOT}"
}

main "$@"
