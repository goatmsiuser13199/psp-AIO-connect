# psp-connect

**Language:** **English** | [Français](README.fr.md)

<p align="center">
  <img src="assets/logo-circle-256.png" alt="psp-connect" width="96" height="96">
</p>

Turn a **PSP** (CFW) into an **AIO-style display** for your PC: live CPU, GPU, RAM, VRAM and temperatures over USB.

This is **not** a Windows secondary monitor — only the dashboard you configure on the PC.

## Root files

| Path | Purpose |
| --- | --- |
| **`psp-connect-setup.exe`** | Windows installer |
| **`COPY-TO-MEMORY-STICK/`** | Folder to copy onto the Memory Stick |
| `README.md` | This guide |

```text
COPY-TO-MEMORY-STICK/
  README.txt
  PSP/
    GAME/
      psp-connect/
        EBOOT.PBP
        usb.prx
```

## Requirements

| | |
| --- | --- |
| PSP | 2000 / 3000 recommended, **Custom Firmware** |
| Cable | USB mini-B **data** (not charge-only) |
| Stick | Memory Stick |
| PC | Windows 10/11 **64-bit** |
| Admin | UAC is **only for CPU temperature** (PawnIO). Without admin, everything else works; CPU temp stays empty. |

## Install

### 1. Memory Stick

Copy **`COPY-TO-MEMORY-STICK/PSP`** to the root of your stick (merge with an existing `PSP` folder if needed).

### 2. PC app

Run **`psp-connect-setup.exe`** (enable .NET / PawnIO / Zadig if offered).

### 3. Connect

1. On the PSP: **Game → psp-connect**, then plug USB (stay in the app — not XMB USB Connection)
2. **Zadig** → List All Devices → **`054C:C001`** → **WinUSB**
3. Launch **psp-connect** on the PC (accept UAC for CPU temp)

## Usage

- Toggle tiles **CPU · GPU · RAM · VRAM**
- Colors: **Background, RAM, Text, Title, CPU, GPU, VRAM**
- **Save / Rename / Delete** profiles (with confirmation)
- **Origine** = reset default colors
- Close / minimize → **tray**; **Quit** from the tray menu

Profiles: `%AppData%\psp-connect\`

| Flag | Effect |
| --- | --- |
| `--mock` | Fake metrics |
| `--no-usb` | PC preview only |
| `--interval 500` | Refresh rate (ms) |

## Troubleshooting

| Issue | Fix |
| --- | --- |
| No data on PSP | Keep `usb.prx`, Zadig WinUSB on `054C:C001`, start host **after** the PSP app |
| Empty CPU temp | Accept UAC admin + install **PawnIO** |
| Stick not found | Open `COPY-TO-MEMORY-STICK` (also under `Program Files\psp-connect\`) |

## Dev / sources

| Folder | Role |
| --- | --- |
| `host/` | .NET 8 Windows app |
| `psp/` | Homebrew sources |
| `installer/` | NSIS script |
| `protocol/` | USB packet |
| `scripts/` | Build helpers |

```powershell
winget install NSIS.NSIS
.\scripts\build-installer.ps1   # → psp-connect-setup.exe
```

```bash
bash scripts/setup-pspdev.sh
bash scripts/build-psp.sh       # updates COPY-TO-MEMORY-STICK
```

## License

Solo project, provided as-is. CFW / PawnIO / Zadig = at your own risk / third-party licenses.
