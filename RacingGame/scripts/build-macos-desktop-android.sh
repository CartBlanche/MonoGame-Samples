#!/usr/bin/env bash
set -euo pipefail

# Build Desktop bundles with MonoPack and Android release binaries on macOS.
# Outputs are written under ./artifacts/<name>.<build_number>/
#
# Racing Game notes: the content pipeline only works in Release (the .mgcb refers to
# PipelineExtension/bin/Release), so keep CONFIGURATION=Release. Run from anywhere.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DESKTOP_PROJECT="${ROOT_DIR}/Desktop/RacingGame.csproj"
ANDROID_PROJECT="${ROOT_DIR}/Android/RacingGame.csproj"
CONFIGURATION="${CONFIGURATION:-Release}"
BUILD_NUMBER="${BUILD_NUMBER:-$(date +%Y%m%d%H%M%S)}"

ARTIFACTS_ROOT="${ROOT_DIR}/artifacts"

ANDROID_FRAMEWORK="${ANDROID_FRAMEWORK:-net10.0-android}"
PKG_IDENTIFIER="${PKG_IDENTIFIER:-com.cartblanche.RacingGame}"
# Signing and notarisation. Identities are auto-detected from the keychain unless set.
SIGN_APP_IDENTITY="${SIGN_APP_IDENTITY:-}"
SIGN_PKG_IDENTITY="${SIGN_PKG_IDENTITY:-}"
ENTITLEMENTS="${ENTITLEMENTS:-${ROOT_DIR}/Desktop/Entitlements.plist}"
NOTARY_PROFILE="${NOTARY_PROFILE:-savage-notary}"

usage() {
  cat <<'EOF'
Usage: scripts/build-macos-desktop-android.sh [options]

Options:
  --build-number <value>     Build number suffix for artifact folders.
  --configuration <value>    Build configuration. Default: Release
  --skip-desktop             Skip Desktop MonoPack packaging.
  --skip-android             Skip Android release publish.
  --skip-pkg                 Skip building the macOS .pkg installer from the universal .app.
  --sign                     Sign the .app (Developer ID Application, hardened runtime) and the .pkg
                             (Developer ID Installer). Needs the .pkg step.
  --notarize                 Sign (as --sign), then submit the .pkg to Apple, wait, and staple the ticket.
                             Needs a notarytool keychain profile (see NOTARY_PROFILE).
  --keep-macos-archive       Keep the macOS .zip/.tar.gz. By default, once the .pkg is signed (or notarised)
                             the archive is deleted, since the .pkg replaces it.
  --clear                    First delete the artifacts folder and every bin/ and obj/ folder next to a
                             .csproj, then carry on. With --skip-desktop --skip-android it only cleans.
  --help                     Show this help text.

Environment overrides:
  BUILD_NUMBER, CONFIGURATION, ANDROID_FRAMEWORK
  PKG_IDENTIFIER, PKG_VERSION,
  SIGN_APP_IDENTITY, SIGN_PKG_IDENTITY (default: auto-detected Developer ID identities),
  ENTITLEMENTS (default: Entitlements.plist beside the desktop Info.plist),
  NOTARY_PROFILE (default: savage-notary; create with: xcrun notarytool store-credentials)

Examples:
  scripts/build-macos-desktop-android.sh
  BUILD_NUMBER=42 scripts/build-macos-desktop-android.sh
  scripts/build-macos-desktop-android.sh --skip-android
  scripts/build-macos-desktop-android.sh --clear --sign --build-number 5
EOF
}

SKIP_DESKTOP=0
SKIP_ANDROID=0
SKIP_PKG=0
SIGN=0
NOTARIZE=0
KEEP_MACOS_ARCHIVE=0
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

DESKTOP_NAME="$(basename "${DESKTOP_PROJECT}" .csproj)"
PKG_VERSION="${PKG_VERSION:-${BUILD_NUMBER}}"
PKG_BASENAME="${PKG_BASENAME:-${DESKTOP_NAME}}"
DESKTOP_OUT_DIR="${ARTIFACTS_ROOT}/${DESKTOP_NAME}.${BUILD_NUMBER}"
ANDROID_OUT_DIR="${ARTIFACTS_ROOT}/Android.${BUILD_NUMBER}"

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

package_desktop() {
  if [[ ! -f "${DESKTOP_PROJECT}" ]]; then
    echo "Desktop project not found: ${DESKTOP_PROJECT}" >&2
    exit 1
  fi

  local project_dir info_plist icns_file
  project_dir="$(dirname "${DESKTOP_PROJECT}")"
  info_plist="${project_dir}/Info.plist"
  icns_file="${project_dir}/RacingGame.icns"

  if [[ ! -f "${info_plist}" ]]; then
    echo "Info.plist required for MonoPack was not found: ${info_plist}" >&2
    echo "Create Desktop/Info.plist (or adjust this script path) and rerun." >&2
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

  package_macos_pkg
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
  require_cmd find
  require_cmd head

  if [[ "${CLEAR}" -eq 1 ]]; then
    clear_build_output
  fi

  mkdir -p "${ARTIFACTS_ROOT}"

  if [[ "${SKIP_DESKTOP}" -eq 0 ]]; then
    check_pkg_prerequisites
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
