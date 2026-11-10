param(
    [int]$WaitSeconds = 180,
    [switch]$SkipHost,
    [switch]$Mock
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$eboot = Join-Path $root "COPY-TO-MEMORY-STICK\PSP\GAME\psp-connect\EBOOT.PBP"
$prx = Join-Path $root "COPY-TO-MEMORY-STICK\PSP\GAME\psp-connect\usb.prx"
$hostExe = Join-Path $root "publish\host\PspConnect.Host.exe"
if (-not (Test-Path $hostExe)) {
    $hostExe = Join-Path $root "host\PspConnect.Host\bin\Release\net8.0-windows\win-x64\PspConnect.Host.exe"
}
$zadig = Join-Path $root "tools\zadig.exe"
$findScript = Join-Path $PSScriptRoot "find-psp-stick.ps1"

Write-Host "=== psp-connect go-live ===" -ForegroundColor Cyan
if (-not (Test-Path $eboot) -or -not (Test-Path $prx)) {
    Write-Error "Homebrew manquant dans COPY-TO-MEMORY-STICK\PSP\GAME\psp-connect\"
}

if (-not (Test-Path $hostExe)) {
    Write-Host "Publish host..."
    dotnet publish (Join-Path $root "host\PspConnect.Host\PspConnect.Host.csproj") `
        -c Release -r win-x64 --self-contained false `
        -p:PublishSingleFile=true -o (Join-Path $root "publish\host") | Out-Host
    $hostExe = Join-Path $root "publish\host\PspConnect.Host.exe"
}

Write-Host "1) Copie homebrew — branche la Memory Stick..." -ForegroundColor Yellow
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$stick = $null
while ((Get-Date) -lt $deadline) {
    $found = @(& $findScript)
    if ($found.Count -gt 0) {
        $stick = $found[0]
        break
    }
    Start-Sleep -Seconds 2
    Write-Host "." -NoNewline
}
Write-Host ""

if (-not $stick) {
    Write-Host "Aucune stick PSP detectee (dossier PSP\GAME)." -ForegroundColor Red
    Write-Host "Copie manuelle : COPY-TO-MEMORY-STICK\PSP → racine de la stick"
    exit 2
}

$destDir = Join-Path "$($stick.Drive)\" "PSP\GAME\psp-connect"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item -Force $eboot (Join-Path $destDir "EBOOT.PBP")
Copy-Item -Force $prx (Join-Path $destDir "usb.prx")
Write-Host "OK -> $destDir" -ForegroundColor Green

Write-Host ""
Write-Host "2) Sur la PSP: quitte le mode USB stick, lance Game → psp-connect, rebranche le cable." -ForegroundColor Yellow
Write-Host "3) Zadig: Options → List All Devices → 054C:C001 → WinUSB → Install Driver" -ForegroundColor Yellow

if (Test-Path $zadig) {
    Start-Process $zadig
    Write-Host "Zadig ouvert."
} else {
    Write-Host "Zadig manquant dans tools\ (option dans l'installateur)"
}

if (-not $SkipHost) {
    Write-Host ""
    Write-Host "4) Lancement host AIO..." -ForegroundColor Yellow
    $hostArgs = @()
    if ($Mock) { $hostArgs += "--mock" }
    Start-Process -FilePath $hostExe -ArgumentList $hostArgs -WorkingDirectory (Split-Path $hostExe -Parent) -Verb RunAs
    Write-Host "Host lance." -ForegroundColor Green
}
