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
BrandingText "psp-connect — AIO PSP display"
VIProductVersion "${PRODUCT_VERSION}.0"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "FileDescription" "psp-connect setup"
VIAddVersionKey "LegalCopyright" "psp-connect"

!define MUI_ABORTWARNING
!define MUI_ICON "..\host\PspConnect.Host\Assets\app.ico"
!define MUI_UNICON "..\host\PspConnect.Host\Assets\app.ico"
!define MUI_WELCOMEPAGE_TITLE "psp-connect"
!define MUI_WELCOMEPAGE_TEXT "FR: Installe l'app PC et le homebrew PSP.$\r$\nCe n'est PAS un second moniteur Windows.$\r$\nUAC admin = UNIQUEMENT la temperature CPU.$\r$\n$\r$\nEN: Installs the PC app and PSP homebrew.$\r$\nThis is NOT a Windows secondary monitor.$\r$\nAdmin UAC = CPU temperature ONLY."
!define MUI_FINISHPAGE_RUN "$INSTDIR\PspConnect.Host.exe"
!define MUI_FINISHPAGE_RUN_TEXT "FR: Lancer / EN: Run psp-connect (UAC = CPU temp)"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\AFTER-INSTALL.txt"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "FR: Lire les etapes / EN: Read PSP + Zadig steps"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "license.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "French"

Section "PC app + stick pack (required) / App PC + pack stick (obligatoire)" SecApp
  SectionIn RO
  SetOutPath "$INSTDIR"
  File /r "payload\host\*.*"

  SetOutPath "$INSTDIR\COPY-TO-MEMORY-STICK"
  File /r "payload\COPY-TO-MEMORY-STICK\*.*"

  SetOutPath "$INSTDIR"
  File "payload\AFTER-INSTALL.txt"
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

Section "Desktop shortcut / Raccourci Bureau" SecDesktop
  CreateShortcut "$DESKTOP\psp-connect.lnk" "$INSTDIR\PspConnect.Host.exe" "" "$INSTDIR\PspConnect.Host.exe" 0
SectionEnd

Section "Start Menu / Menu Demarrer" SecStart
  CreateDirectory "$SMPROGRAMS\psp-connect"
  CreateShortcut "$SMPROGRAMS\psp-connect\psp-connect.lnk" "$INSTDIR\PspConnect.Host.exe"
  CreateShortcut "$SMPROGRAMS\psp-connect\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$SMPROGRAMS\psp-connect\After install.lnk" "$INSTDIR\AFTER-INSTALL.txt"
  CreateShortcut "$SMPROGRAMS\psp-connect\Memory Stick folder.lnk" "$INSTDIR\COPY-TO-MEMORY-STICK"
SectionEnd

Section ".NET 8 Desktop Runtime (if needed)" SecDotNet
  DetailPrint "Checking / installing .NET 8 Desktop Runtime..."
  nsExec::ExecToLog 'cmd /c winget install --id Microsoft.DotNet.DesktopRuntime.8 -e --silent --accept-package-agreements --accept-source-agreements'
SectionEnd

Section "PawnIO (CPU temperature / temp CPU)" SecPawnIO
  DetailPrint "PawnIO: CPU package temperature (needs admin UAC when running the app)."
  nsExec::ExecToLog 'cmd /c winget install --id namazso.PawnIO -e --silent --accept-package-agreements --accept-source-agreements'
SectionEnd

Section "Copy homebrew to Memory Stick / Copier sur la stick" SecStick
  DetailPrint "Looking for a PSP Memory Stick (PSP\GAME)..."
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -File "$INSTDIR\deploy-stick.ps1"'
  Pop $0
  ${If} $0 != 0
    DetailPrint "Stick not found — copy COPY-TO-MEMORY-STICK\PSP manually."
    MessageBox MB_OK|MB_ICONINFORMATION "FR: Memory Stick non detectee.$\r$\nOuvre :$\r$\n$INSTDIR\COPY-TO-MEMORY-STICK$\r$\nCopie le dossier PSP a la racine de la stick.$\r$\n$\r$\nEN: Memory Stick not detected.$\r$\nOpen:$\r$\n$INSTDIR\COPY-TO-MEMORY-STICK$\r$\nCopy the PSP folder to the stick root.$\r$\n$\r$\nYou can run deploy-stick.ps1 later."
  ${EndIf}
SectionEnd

Section "Open Zadig (USB driver)" SecZadig
  DetailPrint "Downloading Zadig..."
  CreateDirectory "$INSTDIR\tools"
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri \"https://github.com/pbatard/libwdi/releases/download/v1.5.1/zadig-2.9.exe\" -OutFile \"$INSTDIR\tools\zadig.exe\" -UseBasicParsing } catch { exit 1 }"'
  Pop $0
  ${If} $0 == 0
  ${AndIf} ${FileExists} "$INSTDIR\tools\zadig.exe"
    MessageBox MB_OK|MB_ICONINFORMATION "FR: Zadig va s'ouvrir.$\r$\n1) PSP: lance psp-connect, branche USB$\r$\n2) Zadig: Options → List All Devices$\r$\n3) 054C:C001 → WinUSB → Install$\r$\n$\r$\nEN: Zadig will open.$\r$\n1) PSP: launch psp-connect, plug USB$\r$\n2) Zadig: Options → List All Devices$\r$\n3) 054C:C001 → WinUSB → Install"
    Exec '"$INSTDIR\tools\zadig.exe"'
  ${Else}
    MessageBox MB_OK|MB_ICONEXCLAMATION "FR: Telechargement Zadig echoue.$\r$\nEN: Zadig download failed.$\r$\nhttps://zadig.akeo.ie/$\r$\nThen: 054C:C001 → WinUSB"
  ${EndIf}
SectionEnd

LangString DESC_SecApp ${LANG_ENGLISH} "Windows app + COPY-TO-MEMORY-STICK pack."
LangString DESC_SecApp ${LANG_FRENCH} "Application Windows + pack COPY-TO-MEMORY-STICK."
LangString DESC_SecDesktop ${LANG_ENGLISH} "Create a Desktop shortcut."
LangString DESC_SecDesktop ${LANG_FRENCH} "Cree un raccourci sur le Bureau."
LangString DESC_SecStart ${LANG_ENGLISH} "Start Menu shortcuts."
LangString DESC_SecStart ${LANG_FRENCH} "Raccourcis Menu Demarrer."
LangString DESC_SecDotNet ${LANG_ENGLISH} "Install .NET 8 Desktop Runtime if missing."
LangString DESC_SecDotNet ${LANG_FRENCH} "Installe le runtime .NET 8 Desktop si absent."
LangString DESC_SecPawnIO ${LANG_ENGLISH} "PawnIO for CPU package temperature. App UAC admin is only for that."
LangString DESC_SecPawnIO ${LANG_FRENCH} "PawnIO pour la temperature CPU. L'UAC admin de l'app sert uniquement a ca."
LangString DESC_SecStick ${LANG_ENGLISH} "Copy homebrew onto a detected Memory Stick."
LangString DESC_SecStick ${LANG_FRENCH} "Copie le homebrew sur une Memory Stick detectee."
LangString DESC_SecZadig ${LANG_ENGLISH} "Download and open Zadig for WinUSB on 054C:C001."
LangString DESC_SecZadig ${LANG_FRENCH} "Telecharge et ouvre Zadig pour WinUSB sur 054C:C001."

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
