#!/usr/bin/env bash
set -euo pipefail

export PSPDEV="${PSPDEV:-$HOME/pspdev}"
ARCHIVE="$HOME/pspdev-ubuntu-latest-x86_64.tar.gz"
URL="https://github.com/pspdev/pspdev/releases/latest/download/pspdev-ubuntu-latest-x86_64.tar.gz"

if [[ ! -x "$PSPDEV/bin/psp-config" ]]; then
  if [[ ! -f "$ARCHIVE" ]]; then
    echo "Download pspdev..."
    wget -O "$ARCHIVE" "$URL"
  fi
  echo "Extract -> $PSPDEV"
  rm -rf "$PSPDEV"
  mkdir -p "$HOME"
  tar -xvf "$ARCHIVE" -C "$HOME"
fi

if ! grep -q 'PSPDEV=' "$HOME/.bashrc" 2>/dev/null; then
  {
    echo ''
    echo 'export PSPDEV="$HOME/pspdev"'
    echo 'export PATH="$PATH:$PSPDEV/bin"'
  } >> "$HOME/.bashrc"
fi

export PATH="$PSPDEV/bin:$PATH"
psp-config --pspdev-path
echo "pspdev OK"
