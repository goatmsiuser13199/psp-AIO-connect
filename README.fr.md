# psp-connect

**Langue :** [English](README.md) | **Français**

<p align="center">
  <img src="assets/logo-circle-256.png" alt="psp-connect" width="96" height="96">
</p>

Transforme une **PSP** (CFW) en **écran AIO** pour ton PC : CPU, GPU, RAM, VRAM et températures en live via USB.

Ce n’est **pas** un second moniteur Windows — uniquement le dashboard que tu configures sur le PC.

## Fichiers à la racine

| Chemin | Usage |
| --- | --- |
| **`psp-connect-setup.exe`** | Installateur Windows |
| **`COPY-TO-MEMORY-STICK/`** | Dossier à copier sur la Memory Stick |
| `README.fr.md` | Ce guide |

```text
COPY-TO-MEMORY-STICK/
  README.txt
  PSP/
    GAME/
      psp-connect/
        EBOOT.PBP
        usb.prx
```

## Prérequis

| | |
| --- | --- |
| PSP | 2000 / 3000 recommandée, **Custom Firmware** |
| Câble | USB mini-B **data** (pas charge-only) |
| Stick | Memory Stick |
| PC | Windows 10/11 **64-bit** |
| Admin | L’UAC sert **uniquement à la température CPU** (PawnIO). Sans admin, le reste marche ; temp CPU vide. |

## Installation

### 1. Memory Stick

Copie **`COPY-TO-MEMORY-STICK/PSP`** à la racine de ta stick (fusionne avec un `PSP` déjà présent).

### 2. App PC

Lance **`psp-connect-setup.exe`** (coche .NET / PawnIO / Zadig si proposé).

### 3. Brancher

1. Sur la PSP : **Game → psp-connect**, puis câble USB (reste dans l’app — pas le mode USB du XMB)
2. **Zadig** → List All Devices → **`054C:C001`** → **WinUSB**
3. Lance **psp-connect** sur le PC (accepte l’UAC pour la temp CPU)

## Utilisation

- Tuiles **CPU · GPU · RAM · VRAM**
- Couleurs : **Fond, RAM, Texte, Titre, CPU, GPU, VRAM**
- **Sauver / Renommer / Supprimer** des profils (avec confirmation)
- **Origine** = couleurs par défaut
- Fermer / minimiser → **tray** ; **Quitter** via le menu

Profils : `%AppData%\psp-connect\`

| Flag | Effet |
| --- | --- |
| `--mock` | Fausses métriques |
| `--no-usb` | Preview PC uniquement |
| `--interval 500` | Rafraîchissement (ms) |

## Dépannage

| Problème | Piste |
| --- | --- |
| Pas de data sur PSP | `usb.prx` présent, Zadig WinUSB sur `054C:C001`, host **après** l’app PSP |
| Temp CPU vide | Accepte l’UAC admin + **PawnIO** |
| Stick | Rouvre `COPY-TO-MEMORY-STICK` (aussi dans `Program Files\psp-connect\`) |

## Dev / sources

| Dossier | Rôle |
| --- | --- |
| `host/` | App Windows .NET 8 |
| `psp/` | Sources homebrew |
| `installer/` | Script NSIS |
| `protocol/` | Paquet USB |
| `scripts/` | Build |

```powershell
winget install NSIS.NSIS
.\scripts\build-installer.ps1   # → psp-connect-setup.exe
```

```bash
bash scripts/setup-pspdev.sh
bash scripts/build-psp.sh       # met à jour COPY-TO-MEMORY-STICK
```

## Licence

Projet solo, fourni tel quel. CFW / PawnIO / Zadig = à tes risques / licences tierces.
