# psp-connect

Transforme une **PSP** (CFW) en **écran AIO** pour ton PC : CPU, GPU, RAM, VRAM et températures en live, via câble USB.

Ce n’est **pas** un second moniteur Windows. Aucune fenêtre ne peut s’y coller — uniquement le dashboard que tu configures sur le PC.

<p align="center">
  <img src="assets/logo-circle-256.png" alt="psp-connect" width="96" height="96">
</p>

## Ce qu’il te faut

| | |
| --- | --- |
| PSP | 2000 / 3000 recommandée, **Custom Firmware** (PRO / ME / Infinity…) |
| Câble | USB mini-B **data** (pas charge-only) |
| Stick | Memory Stick avec un dossier `PSP/GAME/` |
| PC | Windows 10/11 **64-bit** |
| Admin | L’app demande l’UAC **uniquement pour afficher la température CPU** (via PawnIO). Sans admin, le reste marche, la temp CPU reste vide. |

## Installation

1. Lance **`psp-connect-setup.exe`** (à la racine du projet / release)
2. Suis l’installateur (options : .NET 8, **PawnIO**, copie Memory Stick, **Zadig**)
3. Lis *Après install*, puis lance l’app

> **UAC :** les droits admin servent **uniquement** à afficher la température CPU.

### Étapes PSP

1. Homebrew : `PSP/GAME/psp-connect/` (`EBOOT.PBP` + `usb.prx`)
2. Sur la PSP : lance **psp-connect**, branche le USB (pas le mode USB du XMB)
3. **Zadig** → Options → List All Devices → **`054C:C001`** → **WinUSB**
4. Lance **psp-connect** sur le PC

## Utilisation

- Tuiles **CPU · GPU · RAM · VRAM**
- Couleurs : **Fond, RAM, Texte, Titre, CPU, GPU, VRAM**
- **Sauver** = profil ; **Renommer** / **Supprimer** (confirmation)
- **Origine** = couleurs par défaut
- Fermer / minimiser → **tray** ; **Quitter** via le menu icône

Profils : `%AppData%\psp-connect\`

| Flag | Effet |
| --- | --- |
| `--mock` | Fausses métriques |
| `--no-usb` | Preview PC seul |
| `--interval 500` | Rafraîchissement (ms) |

## Dépannage

| Problème | Piste |
| --- | --- |
| PSP noire / pas de data | `usb.prx` à côté de l’EBOOT, Zadig WinUSB sur `054C:C001`, host **après** l’app PSP |
| Temp CPU vide | Accepte l’UAC (admin = temp CPU) + **PawnIO** |
| Stick non détectée | Copie depuis `Program Files\psp-connect\homebrew\` |

## Contenu du dépôt

| Chemin | Rôle |
| --- | --- |
| `psp-connect-setup.exe` | **Installateur Windows** (NSIS) |
| `dist/` | Homebrew : `EBOOT.PBP` + `usb.prx` |
| `installer/` | Script NSIS |
| `host/` | Sources app .NET 8 |
| `psp/` | Sources homebrew |
| `protocol/` | Paquet USB 64 o |

## Build depuis les sources

```powershell
winget install NSIS.NSIS   # une fois
.\scripts\build-installer.ps1
```

→ régénère `psp-connect-setup.exe` à la racine.

**PSP** (WSL + [pspdev](https://github.com/pspdev/pspdev)) :

```bash
bash scripts/setup-pspdev.sh
bash scripts/build-psp.sh
```

## Licence / usage

Projet solo, fourni tel quel. Homebrew = CFW à tes risques. PawnIO et Zadig = outils tiers, licences séparées.
