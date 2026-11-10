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

STICK="$ROOT/COPY-TO-MEMORY-STICK/PSP/GAME/psp-connect"
mkdir -p "$STICK"
cp -f EBOOT.PBP "$STICK/EBOOT.PBP"
if [[ -f usb_prx/usb.prx ]]; then
  cp -f usb_prx/usb.prx "$STICK/usb.prx"
elif [[ -f usb.prx ]]; then
  cp -f usb.prx "$STICK/usb.prx"
fi
ls -la "$STICK"
echo "OK: $STICK"
