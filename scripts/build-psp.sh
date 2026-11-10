#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PSPDEV="${PSPDEV:-$HOME/pspdev}"
export PATH="$PSPDEV/bin:$PATH"

if ! command -v psp-config >/dev/null 2>&1; then
  echo "pspdev introuvable. Lance scripts/setup-pspdev.sh"
  exit 1
fi

cd "$ROOT/psp"
make clean || true
make -j"$(nproc)"
ls -la EBOOT.PBP
echo "OK: $ROOT/psp/EBOOT.PBP"
