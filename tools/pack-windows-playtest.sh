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

cat > "${ZIP_DIR}/CZYTAJ_MNIE.txt" <<'EOF'
Peloton Manager — playtest na Windows

To jeszcze nie jest skończona gra. To niebieska powłoka kariery
(Biurko, Skład, Sztab, kalendarz) plus prawdziwa pętla: kolejne dni,
przygotowanie czwórki, wyścig i tabela wyników.

Sponsorzy, finanse, skauting i OVR na tych stronach to jeszcze rysunek.
Nie zapisują się do kariery.

Jak uruchomić
1. Rozpakuj CAŁY folder, nie sam plik .exe.
2. Wejdź do folderu PelotonManager-playtest.
3. Kliknij dwukrotnie PelotonManager.exe.
4. Jeśli Windows pokaże SmartScreen: „Więcej informacji”, potem „Uruchom mimo to”.

Nie instalujesz Godota ani .NET. Wszystko jest w tym folderze.

Co robić na biurku
- ADVANCE DAY — świat idzie o jeden dzień.
- W dzień wyścigu przycisk zmienia się na Race next.
- W przygotowaniu ustaw jednego Leader i jednego Card, reszta wozi.
- JEDŹ WYŚCIG — dostajesz wynik i tabelę.
- WYNIK: Wszyscy, albo jeden zespół (Beskid–Vetter / Fala–Karpaty / Ost-Wind).
- Film jest wyłączony. W Ustawieniach FILM: WŁ włącza oglądanie etapu; wynik zostaje ten sam.

Zapis autosave leży w podfolderze saves obok gry.
EOF

# zip from dist so the archive has a single top-level folder
(
  cd "${ROOT}/dist"
  zip -qr "${ZIP}" PelotonManager-playtest
)

echo "Wrote ${ZIP}"
ls -lh "${ZIP}"
