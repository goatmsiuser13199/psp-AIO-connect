param(
    [int]$WaitSeconds = 180,
    [switch]$SkipHost,
    [switch]$Mock
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$eboot = Join-Path $root "dist\EBOOT.PBP"
$hostExe = Join-Path $root "host\PspConnect.Host\bin\Release\net8.0-windows\PspConnect.Host.exe"
$zadig = Join-Path $root "tools\zadig.exe"
$findScript = Join-Path $PSScriptRoot "find-psp-stick.ps1"

Write-Host "=== psp-connect go-live ===" -ForegroundColor Cyan
if (-not (Test-Path $eboot)) {
    Write-Error "EBOOT manquant: $eboot"
}

if (-not (Test-Path $hostExe)) {
    Write-Host "Build host Release..."
    dotnet build (Join-Path $root "host\PspConnect.Host\PspConnect.Host.csproj") -c Release | Out-Host
}

Write-Host "1) Copie EBOOT — branche la Memory Stick (USB data / mode stick PSP)..." -ForegroundColor Yellow
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
    Write-Host "Sur la PSP: XMB → USB Connection, ou lecteur de stick externe."
    Write-Host "Puis: .\scripts\go-live.ps1"
    exit 2
}

$destDir = Join-Path "$($stick.Drive)\" "PSP\GAME\psp-connect"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item -Force $eboot (Join-Path $destDir "EBOOT.PBP")
Write-Host "OK EBOOT -> $destDir\EBOOT.PBP" -ForegroundColor Green

Write-Host ""
Write-Host "2) Sur la PSP: quitte le mode USB stick, lance Game → psp-connect, rebranche le cable." -ForegroundColor Yellow
Write-Host "3) Zadig: Options → List All Devices → 054C:C001 → WinUSB → Install Driver" -ForegroundColor Yellow

if (Test-Path $zadig) {
    Start-Process $zadig
    Write-Host "Zadig ouvert."
} else {
    Write-Host "Zadig manquant dans tools\"
}

if (-not $SkipHost) {
    Write-Host ""
    Write-Host "4) Lancement host AIO..." -ForegroundColor Yellow
    $hostArgs = @()
    if ($Mock) { $hostArgs += "--mock" }
    Start-Process -FilePath $hostExe -ArgumentList $hostArgs -WorkingDirectory $root
    Write-Host "Host lance. La PSP doit afficher LIVE #..." -ForegroundColor Green
}

Write-Host ""
Write-Host "Checklist:" -ForegroundColor Cyan
Write-Host " [x] EBOOT sur stick"
Write-Host " [ ] Homebrew psp-connect lance sur PSP"
Write-Host " [ ] WinUSB Zadig sur 054C:C001"
Write-Host " [ ] Host USB connecte + LIVE"
