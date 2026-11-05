#!/usr/bin/env bash
set -euo pipefail

# Build Catapult Wars Desktop bundles with MonoPack and Android release binaries on macOS.
# Outputs are written under ./artifacts/<name>.<build_number>/

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

CONFIGURATION="${CONFIGURATION:-Release}"
BUILD_NUMBER="${BUILD_NUMBER:-$(date +%Y%m%d%H%M%S)}"
ANDROID_FRAMEWORK="${ANDROID_FRAMEWORK:-net10.0-android}"

DESKTOP_PROJECT="${ROOT_DIR}/Platforms/Desktop/CatapultNetWars.csproj"
ANDROID_PROJECT="${ROOT_DIR}/Platforms/Android/CatapultNetWars.csproj"

ARTIFACTS_ROOT="${ROOT_DIR}/artifacts"
PKG_IDENTIFIER="${PKG_IDENTIFIER:-com.cartblanche.catapultnetwars}"

usage() {
  cat <<'EOF'
Usage: scripts/build-macos-desktop-android.sh [options]

Options:
  --build-number <value>     Build number suffix for artifact folders.
  --configuration <value>    Build configuration. Default: Release
  --skip-desktop             Skip Desktop MonoPack packaging.
  --skip-pkg                 Skip building the macOS .pkg installer from the universal .app.
  --clear                    First delete the artifacts folder and every bin/ and obj/ folder next to a
                             .csproj (Core, Platforms), then carry on. With --skip-desktop --skip-android
                             it only cleans.
  --skip-android             Skip Android release publish.
  --help                     Show this help text.

Environment overrides:
  BUILD_NUMBER, CONFIGURATION, ANDROID_FRAMEWORK, PKG_IDENTIFIER, PKG_VERSION

Examples:
  scripts/build-macos-desktop-android.sh
  BUILD_NUMBER=42 scripts/build-macos-desktop-android.sh
  scripts/build-macos-desktop-android.sh --skip-android
  scripts/build-macos-desktop-android.sh --clear --build-number halloween5
EOF
}

SKIP_DESKTOP=0
SKIP_ANDROID=0
SKIP_PKG=0
CLEAR=0

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
    --skip-desktop)
      SKIP_DESKTOP=1
      shift
      ;;
    --skip-android)
      SKIP_ANDROID=1
      shift
      ;;
    --skip-pkg)
      SKIP_PKG=1
      shift
      ;;
    --clear)
      CLEAR=1
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

# After the flags, so --build-number is picked up
PKG_VERSION="${PKG_VERSION:-${BUILD_NUMBER}}"

DESKTOP_NAME="$(basename "${DESKTOP_PROJECT}" .csproj)"
DESKTOP_OUT_DIR="${ARTIFACTS_ROOT}/${DESKTOP_NAME}.${BUILD_NUMBER}"
ANDROID_OUT_DIR="${ARTIFACTS_ROOT}/CatapultWars.Android.${BUILD_NUMBER}"

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

clear_build_output() {
  log "Clearing build output"

  # Only the folder named artifacts under the repo root: never delete anything else
  if [[ -d "${ARTIFACTS_ROOT}" ]]; then
    log "Deleting ${ARTIFACTS_ROOT}"
    rm -rf "${ARTIFACTS_ROOT}"
  fi

  # bin/ and obj/ only where a .csproj sits beside them, never inside .git or node_modules
  local dir
  while IFS= read -r dir; do
    if compgen -G "$(dirname "${dir}")/*.csproj" >/dev/null; then
      log "Deleting ${dir}"
      rm -rf "${dir}"
    fi
  done < <(find "${ROOT_DIR}" \( -name .git -o -name node_modules \) -prune -o -type d \( -name bin -o -name obj \) -print -prune)
}

# MonoPack leaves the universal .app either as a folder or only inside a macOS archive; find it in either.
package_macos_pkg() {
  local app_bundle="" archive="" temp_extract_dir="" pkg_path
  app_bundle="$(find "${DESKTOP_OUT_DIR}" -type d -name "*.app" | head -n 1 || true)"

  if [[ -z "${app_bundle}" ]]; then
    archive="$(find "${DESKTOP_OUT_DIR}" -type f \( -name "*osx*.zip" -o -name "*macos*.zip" -o -name "*osx*.tar.gz" -o -name "*macos*.tar.gz" \) | head -n 1 || true)"
    if [[ -n "${archive}" ]]; then
      temp_extract_dir="$(mktemp -d)"
      if [[ "${archive}" == *.zip ]]; then
        ditto -x -k "${archive}" "${temp_extract_dir}"
      else
        tar -xzf "${archive}" -C "${temp_extract_dir}"
      fi
      app_bundle="$(find "${temp_extract_dir}" -type d -name "*.app" | head -n 1 || true)"
    fi
  fi

  if [[ -z "${app_bundle}" ]]; then
    echo "Unable to locate a macOS .app bundle in ${DESKTOP_OUT_DIR}" >&2
    exit 1
  fi

  pkg_path="${DESKTOP_OUT_DIR}/CatapultWars-macos-${BUILD_NUMBER}.pkg"

  log "Building macOS installer package"
  log "App bundle: ${app_bundle}"
  log "Output: ${pkg_path}"

  pkgbuild \
    --component "${app_bundle}" \
    --install-location "/Applications" \
    --identifier "${PKG_IDENTIFIER}" \
    --version "${PKG_VERSION}" \
    "${pkg_path}"

  if [[ -n "${temp_extract_dir}" && -d "${temp_extract_dir}" ]]; then
    rm -rf "${temp_extract_dir}"
  fi

  log "macOS pkg generated"
  log "The pkg is not signed or notarised: if testers hit Gatekeeper, they can right-click > Open, or run: xattr -d com.apple.quarantine \"${pkg_path}\""
}

package_desktop() {
  if [[ ! -f "${DESKTOP_PROJECT}" ]]; then
    echo "Desktop project not found: ${DESKTOP_PROJECT}" >&2
    exit 1
  fi

  local project_dir info_plist icns_file
  project_dir="$(dirname "${DESKTOP_PROJECT}")"
  info_plist="${project_dir}/Info.plist"
  icns_file="${ROOT_DIR}/Icons/macOS/Icon.icns"

  if [[ ! -f "${info_plist}" ]]; then
    echo "Info.plist required for MonoPack was not found: ${info_plist}" >&2
    echo "Create Platforms/Desktop/Info.plist (or adjust this script path) and rerun." >&2
    exit 1
  fi

  mkdir -p "${DESKTOP_OUT_DIR}"

  log "Packaging Desktop bundles with MonoPack"
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

  if [[ "${SKIP_PKG}" -eq 0 ]]; then
    package_macos_pkg
  else
    log "Skipping macOS pkg"
  fi
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

  if [[ "${CLEAR}" -eq 1 ]]; then
    clear_build_output
  fi

  mkdir -p "${ARTIFACTS_ROOT}"

  if [[ "${SKIP_DESKTOP}" -eq 0 ]]; then
    if [[ "${SKIP_PKG}" -eq 0 ]]; then
      require_cmd pkgbuild
      require_cmd ditto
      require_cmd tar
      require_cmd mktemp
    fi
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