; psp-connect NSIS installer
Unicode true
SetCompressor /SOLID lzma

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
!include "FileFunc.nsh"

!define PRODUCT_NAME "psp-connect"
!define PRODUCT_PUBLISHER "psp-connect"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_WEB "https://github.com/goatmsiuser13199/psp-connect"
!define INSTALL_DIR "$PROGRAMFILES64\psp-connect"

Name "${PRODUCT_NAME}"
OutFile "..\psp-connect-setup.exe"
InstallDir "${INSTALL_DIR}"
InstallDirRegKey HKLM "Software\psp-connect" "InstallDir"
RequestExecutionLevel admin
BrandingText "psp-connect — ecran AIO PSP"
VIProductVersion "${PRODUCT_VERSION}.0"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "FileDescription" "Installateur psp-connect"
VIAddVersionKey "LegalCopyright" "psp-connect"

!define MUI_ABORTWARNING
!define MUI_ICON "..\host\PspConnect.Host\Assets\app.ico"
!define MUI_UNICON "..\host\PspConnect.Host\Assets\app.ico"
!define MUI_WELCOMEPAGE_TITLE "psp-connect"
!define MUI_WELCOMEPAGE_TEXT "Installe l'app PC et le homebrew PSP.$\r$\n$\r$\nCe n'est PAS un second moniteur Windows : la PSP affiche uniquement le dashboard AIO.$\r$\n$\r$\nLes droits administrateur (UAC) servent UNIQUEMENT a afficher la temperature CPU. Sans admin, le reste fonctionne."
!define MUI_FINISHPAGE_RUN "$INSTDIR\PspConnect.Host.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Lancer psp-connect (UAC = temp CPU)"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\APRES-INSTALL.txt"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Lire les etapes PSP / Zadig"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "license.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "French"

Section "Application PC (obligatoire)" SecApp
  SectionIn RO
  SetOutPath "$INSTDIR"
  File /r "payload\host\*.*"

  SetOutPath "$INSTDIR\homebrew"
  File "payload\homebrew\EBOOT.PBP"
  File "payload\homebrew\usb.prx"

  SetOutPath "$INSTDIR"
  File "payload\APRES-INSTALL.txt"
  File "deploy-stick.ps1"

  WriteRegStr HKLM "Software\psp-connect" "InstallDir" "$INSTDIR"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect" "DisplayName" "psp-connect"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect" "DisplayIcon" "$INSTDIR\PspConnect.Host.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect" "URLInfoAbout" "${PRODUCT_WEB}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect" "DisplayVersion" "${PRODUCT_VERSION}"
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect" "EstimatedSize" "$0"
SectionEnd

Section "Raccourci Bureau" SecDesktop
  CreateShortcut "$DESKTOP\psp-connect.lnk" "$INSTDIR\PspConnect.Host.exe" "" "$INSTDIR\PspConnect.Host.exe" 0
SectionEnd

Section "Menu Demarrer" SecStart
  CreateDirectory "$SMPROGRAMS\psp-connect"
  CreateShortcut "$SMPROGRAMS\psp-connect\psp-connect.lnk" "$INSTDIR\PspConnect.Host.exe"
  CreateShortcut "$SMPROGRAMS\psp-connect\Desinstaller.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$SMPROGRAMS\psp-connect\Apres install.lnk" "$INSTDIR\APRES-INSTALL.txt"
SectionEnd

Section "Runtime .NET 8 Desktop (si besoin)" SecDotNet
  DetailPrint "Verification / installation .NET 8 Desktop Runtime..."
  nsExec::ExecToLog 'cmd /c winget install --id Microsoft.DotNet.DesktopRuntime.8 -e --silent --accept-package-agreements --accept-source-agreements'
SectionEnd

Section "PawnIO (temperature CPU)" SecPawnIO
  DetailPrint "PawnIO permet la lecture de la temp CPU package (avec UAC admin)."
  nsExec::ExecToLog 'cmd /c winget install --id namazso.PawnIO -e --silent --accept-package-agreements --accept-source-agreements'
SectionEnd

Section "Copier le homebrew sur la Memory Stick" SecStick
  DetailPrint "Recherche d'une stick PSP (PSP\GAME)..."
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -File "$INSTDIR\deploy-stick.ps1"'
  Pop $0
  ${If} $0 != 0
    DetailPrint "Stick non detectee — copie manuelle : $INSTDIR\homebrew\ vers ms0:/PSP/GAME/psp-connect/"
    MessageBox MB_OK|MB_ICONINFORMATION "Memory Stick non detectee.$\r$\n$\r$\nCopie manuellement :$\r$\n$INSTDIR\homebrew\EBOOT.PBP$\r$\n$INSTDIR\homebrew\usb.prx$\r$\n$\r$\nvers ms0:/PSP/GAME/psp-connect/$\r$\n$\r$\nTu peux relancer deploy-stick.ps1 plus tard."
  ${EndIf}
SectionEnd

Section "Ouvrir Zadig (driver USB)" SecZadig
  DetailPrint "Telechargement de Zadig..."
  CreateDirectory "$INSTDIR\tools"
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri \"https://github.com/pbatard/libwdi/releases/download/v1.5.1/zadig-2.9.exe\" -OutFile \"$INSTDIR\tools\zadig.exe\" -UseBasicParsing } catch { exit 1 }"'
  Pop $0
  ${If} $0 == 0
  ${AndIf} ${FileExists} "$INSTDIR\tools\zadig.exe"
    MessageBox MB_OK|MB_ICONINFORMATION "Zadig va s'ouvrir.$\r$\n$\r$\n1) Sur la PSP : lance psp-connect, branche le USB$\r$\n2) Zadig : Options → List All Devices$\r$\n3) Device 054C:C001 → WinUSB → Install"
    Exec '"$INSTDIR\tools\zadig.exe"'
  ${Else}
    MessageBox MB_OK|MB_ICONEXCLAMATION "Telechargement Zadig echoue. Telecharge-le sur https://zadig.akeo.ie/$\r$\nPuis : 054C:C001 → WinUSB"
  ${EndIf}
SectionEnd

LangString DESC_SecApp ${LANG_FRENCH} "Application Windows + fichiers homebrew PSP."
LangString DESC_SecDesktop ${LANG_FRENCH} "Cree un raccourci sur le Bureau."
LangString DESC_SecStart ${LANG_FRENCH} "Raccourcis dans le menu Demarrer."
LangString DESC_SecDotNet ${LANG_FRENCH} "Installe le runtime .NET 8 Desktop si absent (requis pour l'app)."
LangString DESC_SecPawnIO ${LANG_FRENCH} "Driver PawnIO pour la temperature CPU. L'app demande l'UAC admin uniquement pour ca."
LangString DESC_SecStick ${LANG_FRENCH} "Copie EBOOT.PBP + usb.prx sur une Memory Stick detectee."
LangString DESC_SecZadig ${LANG_FRENCH} "Telecharge et ouvre Zadig pour installer WinUSB sur 054C:C001."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecApp} $(DESC_SecApp)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} $(DESC_SecDesktop)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecStart} $(DESC_SecStart)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDotNet} $(DESC_SecDotNet)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecPawnIO} $(DESC_SecPawnIO)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecStick} $(DESC_SecStick)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecZadig} $(DESC_SecZadig)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section "Uninstall"
  Delete "$DESKTOP\psp-connect.lnk"
  RMDir /r "$SMPROGRAMS\psp-connect"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\psp-connect"
  DeleteRegKey HKLM "Software\psp-connect"
SectionEnd
