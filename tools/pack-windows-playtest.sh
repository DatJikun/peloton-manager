#!/usr/bin/env bash
set -euo pipefail

# Pack a Windows playtest zip of the Godot career shell.
# Requires Godot 4.4.1 .NET and matching mono export templates.

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GODOT="${GODOT:-}"
OUT="${ROOT}/dist/windows"
ZIP_DIR="${ROOT}/dist/PelotonManager-playtest"
ZIP="${ROOT}/dist/PelotonManager-playtest-windows.zip"

if [[ -z "${GODOT}" ]]; then
  if [[ -x /tmp/godot-setup/Godot_v4.4.1-stable_mono_linux.x86_64 ]]; then
    GODOT=/tmp/godot-setup/Godot_v4.4.1-stable_mono_linux.x86_64
  elif [[ -x /tmp/godot-setup/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64 ]]; then
    GODOT=/tmp/godot-setup/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64
  else
    found="$(find /tmp/godot-setup -maxdepth 3 -type f -name 'Godot_v4.4.1-stable_mono_linux*' ! -name '*.zip' -perm -u+x 2>/dev/null | head -n 1 || true)"
    if [[ -n "${found}" ]]; then
      GODOT="${found}"
    elif command -v godot >/dev/null 2>&1; then
      GODOT="$(command -v godot)"
    else
      echo "Set GODOT to the Godot 4.4.1 .NET binary." >&2
      exit 1
    fi
  fi
fi

rm -rf "${OUT}" "${ZIP_DIR}" "${ZIP}"
mkdir -p "${OUT}"

"${GODOT}" --headless --path "${ROOT}/src/Peloton.Client.Godot" --export-release "Windows Desktop" "${OUT}/PelotonManager.exe"

if [[ ! -f "${OUT}/PelotonManager.exe" ]]; then
  echo "Export did not write PelotonManager.exe" >&2
  exit 1
fi

# C# assemblies are embedded in the .exe/.pck. A template-only binary is ~94MB.
size="$(stat -c%s "${OUT}/PelotonManager.exe")"
if (( size < 120000000 )); then
  echo "Export looks like a template-only binary (${size} bytes); C# publish probably failed." >&2
  exit 1
fi

mkdir -p "${ZIP_DIR}"
cp -a "${OUT}/." "${ZIP_DIR}/"
mkdir -p "${ZIP_DIR}/content"
cp -a "${ROOT}/content/peloton.skeleton" "${ZIP_DIR}/content/"
cp -a "${ROOT}/content/peloton.race-prototype" "${ZIP_DIR}/content/"
cp -a "${ROOT}/content/peloton.wt-2026" "${ZIP_DIR}/content/"
cp -a "${ROOT}/playtest/CZYTAJ_MNIE.txt" "${ZIP_DIR}/CZYTAJ_MNIE.txt"

# zip from dist so the archive has a single top-level folder
(
  cd "${ROOT}/dist"
  zip -qr "${ZIP}" PelotonManager-playtest
)

echo "Wrote ${ZIP}"
ls -lh "${ZIP}"
