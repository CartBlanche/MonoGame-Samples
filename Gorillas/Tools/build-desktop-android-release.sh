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
PKG_IDENTIFIER="${PKG_IDENTIFIER:-com.cartblanche.gorillas}"
# Signing and notarisation. Identities are auto-detected from the keychain unless set.
SIGN_APP_IDENTITY="${SIGN_APP_IDENTITY:-}"
SIGN_PKG_IDENTITY="${SIGN_PKG_IDENTITY:-}"
ENTITLEMENTS="${ENTITLEMENTS:-${ROOT_DIR}/Platforms/Desktop/Entitlements.plist}"
NOTARY_PROFILE="${NOTARY_PROFILE:-savage-notary}"

SKIP_DESKTOP=0
SKIP_ANDROID=0
SKIP_PKG=0
SIGN=0
NOTARIZE=0
KEEP_MACOS_ARCHIVE=0
CLEAR=0

usage() {
  cat <<'EOF'
Usage: build-desktop-android-release.sh [options]

Options:
  --build-number <value>    Build number suffix for artifact folders.
  --configuration <value>   Build configuration. Default: Release
  --desktop-rids <value>    MonoPack RIDs list. Default: win-x64,linux-x64,osx-x64,osx-arm64 (the two macOS ones become one universal .app; single-file publish, which a universal build needs)
  --skip-desktop            Skip the Desktop/MonoPack packaging step.
  --skip-android            Skip Android build/publish.
  --skip-pkg                Skip building the macOS .pkg installer from the universal .app.
  --sign                    Sign the .app (Developer ID Application, hardened runtime) and the .pkg
                            (Developer ID Installer). Needs the .pkg step.
  --notarize                Sign (as --sign), then submit the .pkg to Apple, wait, and staple the ticket.
                            Needs a notarytool keychain profile (see NOTARY_PROFILE).
  --keep-macos-archive      Keep the macOS .zip/.tar.gz. By default, once the .pkg is signed (or notarised)
                            the archive is deleted, since the .pkg replaces it.
  --clear                   First delete the artifacts folder and every bin/ and obj/ folder next to a
                            .csproj, then carry on. With --skip-desktop --skip-android it only cleans.
  --help                    Show this help text.

Environment overrides:
  DESKTOP_PROJECT, ANDROID_PROJECT, DESKTOP_INFO_PLIST, DESKTOP_ICON,
  CONFIGURATION, DESKTOP_RIDS, ARTIFACTS_ROOT, BUILD_NUMBER,
  DESKTOP_OUT_DIR, ANDROID_OUT_DIR,
  PKG_IDENTIFIER, PKG_VERSION,
  SIGN_APP_IDENTITY, SIGN_PKG_IDENTITY (default: auto-detected Developer ID identities),
  ENTITLEMENTS (default: Entitlements.plist beside the desktop Info.plist),
  NOTARY_PROFILE (default: savage-notary; create with: xcrun notarytool store-credentials)

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
    --skip-pkg)
      SKIP_PKG=1
      shift
      ;;
    --sign)
      SIGN=1
      shift
      ;;
    --notarize)
      SIGN=1
      NOTARIZE=1
      shift
      ;;
    --keep-macos-archive)
      KEEP_MACOS_ARCHIVE=1
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

PKG_VERSION="${PKG_VERSION:-${BUILD_NUMBER}}"
PKG_BASENAME="${PKG_BASENAME:-Gorillas}"
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

clear_build_output() {
  log "Clearing build output"

  # Only the folder named artifacts under the project root: never trust ARTIFACTS_ROOT to be something safe to delete
  if [[ -d "${ARTIFACTS_ROOT}" ]]; then
    if [[ "${ARTIFACTS_ROOT}" == "${ROOT_DIR}/artifacts" ]]; then
      log "Deleting ${ARTIFACTS_ROOT}"
      rm -rf "${ARTIFACTS_ROOT}"
    else
      log "ARTIFACTS_ROOT is ${ARTIFACTS_ROOT}, not ${ROOT_DIR}/artifacts: leaving it alone"
    fi
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

# First matching "Developer ID ..." identity name in the keychain.
find_identity() {
  security find-identity -v | sed -n "s/.*\"\(${1}[^\"]*\)\".*/\1/p" | head -n 1
}

# Signs every Mach-O file inside the bundle, then the bundle itself, with the hardened runtime
# and a secure timestamp (both are required for notarisation).
sign_app() {
  local app="$1" file
  log "Signing ${app}"

  if [[ ! -f "${ENTITLEMENTS}" ]]; then
    echo "Entitlements file not found: ${ENTITLEMENTS}" >&2
    exit 1
  fi

  while IFS= read -r file; do
    if file -b "${file}" | grep -q "Mach-O"; then
      codesign --force --options runtime --timestamp \
        --entitlements "${ENTITLEMENTS}" --sign "${SIGN_APP_IDENTITY}" "${file}"
    fi
  done < <(find "${app}/Contents" -type f)

  codesign --force --options runtime --timestamp \
    --entitlements "${ENTITLEMENTS}" --sign "${SIGN_APP_IDENTITY}" "${app}"
  codesign --verify --deep --strict --verbose=2 "${app}"
}

notarize_pkg() {
  local pkg="$1" output
  log "Submitting ${pkg} for notarisation (this can take a few minutes)"

  output="$(xcrun notarytool submit "${pkg}" --keychain-profile "${NOTARY_PROFILE}" --wait 2>&1)" || true
  echo "${output}"

  if ! grep -q "status: Accepted" <<<"${output}"; then
    local id
    id="$(sed -n 's/^ *id: *//p' <<<"${output}" | head -n 1)"
    echo "Notarisation was not accepted." >&2
    [[ -n "${id}" ]] && echo "See why with: xcrun notarytool log ${id} --keychain-profile ${NOTARY_PROFILE}" >&2
    exit 1
  fi

  xcrun stapler staple "${pkg}"
  spctl -a -vv -t install "${pkg}" || true
  log "Notarised and stapled: ${pkg}"
}

# MonoPack leaves the universal .app either as a folder or only inside a macOS archive; find it in either.
package_macos_pkg() {
  if [[ "${SKIP_PKG}" -eq 1 ]]; then
    log "Skipping macOS pkg"
    return
  fi

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

  pkg_path="${DESKTOP_OUT_DIR}/${PKG_BASENAME}-macos-${BUILD_NUMBER}.pkg"

  local sign_args=()
  if [[ "${SIGN}" -eq 1 ]]; then
    sign_app "${app_bundle}"
    sign_args=(--sign "${SIGN_PKG_IDENTITY}")
  fi

  log "Building macOS installer package"
  log "App bundle: ${app_bundle}"
  log "Output: ${pkg_path}"

  pkgbuild \
    --component "${app_bundle}" \
    --install-location "/Applications" \
    --identifier "${PKG_IDENTIFIER}" \
    --version "${PKG_VERSION}" \
    ${sign_args[@]+"${sign_args[@]}"} \
    "${pkg_path}"

  if [[ -n "${temp_extract_dir}" && -d "${temp_extract_dir}" ]]; then
    rm -rf "${temp_extract_dir}"
  fi

  log "macOS pkg generated"
  if [[ "${NOTARIZE}" -eq 1 ]]; then
    notarize_pkg "${pkg_path}"
  elif [[ "${SIGN}" -eq 1 ]]; then
    log "The pkg is signed but not notarised (use --notarize)"
  else
    log "The pkg is not signed or notarised: if testers hit Gatekeeper, they can right-click > Open, or run: xattr -d com.apple.quarantine \"${pkg_path}\""
  fi

  # The signed pkg replaces the unsigned macOS archive, so drop it to save space (reaching here means signing and notarising worked)
  if [[ "${SIGN}" -eq 1 && "${KEEP_MACOS_ARCHIVE}" -eq 0 ]]; then
    local old
    while IFS= read -r old; do
      log "Removing ${old} (replaced by the signed pkg)"
      rm -f "${old}"
    done < <(find "${DESKTOP_OUT_DIR}" -maxdepth 1 -type f \( -name "*osx*.zip" -o -name "*macos*.zip" -o -name "*osx*.tar.gz" -o -name "*macos*.tar.gz" \))
  fi
}

check_pkg_prerequisites() {
  if [[ "${SIGN}" -eq 1 && "${SKIP_PKG}" -eq 1 ]]; then
    echo "--sign/--notarize work on the .pkg step, so they can't be combined with --skip-pkg." >&2
    exit 1
  fi
  if [[ "${SIGN}" -eq 1 ]]; then
    require_cmd codesign
    require_cmd security
    [[ -n "${SIGN_APP_IDENTITY}" ]] || SIGN_APP_IDENTITY="$(find_identity 'Developer ID Application')"
    [[ -n "${SIGN_PKG_IDENTITY}" ]] || SIGN_PKG_IDENTITY="$(find_identity 'Developer ID Installer')"
    if [[ -z "${SIGN_APP_IDENTITY}" || -z "${SIGN_PKG_IDENTITY}" ]]; then
      echo "Could not find both a Developer ID Application and a Developer ID Installer identity (security find-identity -v)." >&2
      exit 1
    fi
    log "App identity: ${SIGN_APP_IDENTITY}"
    log "Installer identity: ${SIGN_PKG_IDENTITY}"
  fi
  if [[ "${NOTARIZE}" -eq 1 ]]; then
    require_cmd xcrun
    if ! xcrun notarytool history --keychain-profile "${NOTARY_PROFILE}" >/dev/null 2>&1; then
      echo "Notary profile '${NOTARY_PROFILE}' not found or not working. Create it with:" >&2
      echo "  xcrun notarytool store-credentials \"${NOTARY_PROFILE}\" --apple-id <id> --team-id <team> --password <app-specific-password>" >&2
      exit 1
    fi
  fi
  if [[ "${SKIP_PKG}" -eq 0 ]]; then
    require_cmd pkgbuild
    require_cmd ditto
    require_cmd tar
    require_cmd mktemp
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

  package_macos_pkg
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

  if [[ "${CLEAR}" -eq 1 ]]; then
    clear_build_output
  fi

  if [[ "${SKIP_DESKTOP}" -eq 0 ]]; then
    check_pkg_prerequisites
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
