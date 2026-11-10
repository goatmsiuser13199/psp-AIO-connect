# psp-connect

Écran **dédié** style watercooling AIO pour ta **PSP-3004 (CFW PRO)** : CPU / GPU / RAM / temps / disque en live via **USB**.

Ce n’est **pas** un moniteur Windows — Chrome / jeux / apps ne peuvent pas s’y coller. Tu configures couleurs + métriques dans l’app PC ; la PSP n’affiche que ce dashboard.

## Stack

| Couche | Techno |
| --- | --- |
| Host Win11 | .NET 8 panneau AIO + LibreHardwareMonitor + LibUsbDotNet |
| Protocole v2 | paquet 64 B : métriques + flags + couleurs RGB565 |
| PSP | homebrew kernel USB `054C:C001` · UI 480×272 |

## App PC (panneau AIO)

Raccourci Bureau (double-clic) :

```powershell
.\scripts\install-desktop.ps1
```

Puis double-clic sur **psp-connect** sur le Bureau (UAC admin : requis pour la temp CPU).

La temp CPU package passe par **PawnIO** (`namazso.PawnIO` via winget). Sans ça, LHM ne lit pas les MSR Intel/AMD — le host n’affiche plus de fausse zone ACPI.

Ou en ligne de commande :

```powershell
dotnet run --project host/PspConnect.Host
# ou
.\publish\host\PspConnect.Host.exe
```

- Coche les métriques (CPU, GPU, RAM, VRAM, temps)
- Choisis les couleurs (fond, accent, texte, barres)
- Preview 480×272 en direct
- Thème sauvé dans `%AppData%/psp-connect/theme.json`
- Preset **ice** inclus

Options : `--mock`, `--no-usb`, `--interval 500`

## Brancher la PSP (one-shot)

```powershell
.\scripts\go-live.ps1
```

Le script attend la Memory Stick, copie l’EBOOT, ouvre Zadig, lance le host.

Manuel :
1. `dist/EBOOT.PBP` → `ms0:/PSP/GAME/psp-connect/`
2. Lance **psp-connect** sur la PSP
3. USB → Zadig → **WinUSB** sur `054C:C001`
4. Host AIO — la PSP suit ton thème live

## Build homebrew (WSL)

```bash
bash scripts/setup-pspdev.sh
bash scripts/build-psp.sh
```

## Pourquoi pas un 2ᵉ écran Windows ?

Un vrai moniteur étendu laisserait Windows y poser des fenêtres. Ici la PSP reçoit uniquement un **paquet de stats + thème** : affichage verrouillé au dashboard.
