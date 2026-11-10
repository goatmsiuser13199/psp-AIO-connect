$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

$makensis = @(
    "${env:ProgramFiles(x86)}\NSIS\makensis.exe",
    "$env:ProgramFiles\NSIS\makensis.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $makensis) {
    Write-Host "NSIS introuvable. Installe : winget install NSIS.NSIS" -ForegroundColor Red
    exit 1
}

$eboot = Join-Path $root "dist\EBOOT.PBP"
$prx = Join-Path $root "dist\usb.prx"
if (-not (Test-Path $eboot) -or -not (Test-Path $prx)) {
    Write-Error "dist\EBOOT.PBP et dist\usb.prx requis."
}

Write-Host "== Publish host ==" -ForegroundColor Cyan
$publish = Join-Path $root "publish\host"
dotnet publish (Join-Path $root "host\PspConnect.Host\PspConnect.Host.csproj") `
    -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -o $publish | Out-Host

if (-not (Test-Path (Join-Path $publish "PspConnect.Host.exe"))) {
    Write-Error "Publish host echoue."
}

Write-Host "== Stage payload ==" -ForegroundColor Cyan
$payloadHost = Join-Path $root "installer\payload\host"
$payloadHb = Join-Path $root "installer\payload\homebrew"
Remove-Item -Recurse -Force $payloadHost -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $payloadHost | Out-Null
New-Item -ItemType Directory -Force -Path $payloadHb | Out-Null

Copy-Item -Recurse -Force (Join-Path $publish "*") $payloadHost
Remove-Item (Join-Path $payloadHost "*.pdb") -ErrorAction SilentlyContinue
Copy-Item -Force $eboot (Join-Path $payloadHb "EBOOT.PBP")
Copy-Item -Force $prx (Join-Path $payloadHb "usb.prx")

Write-Host "== NSIS ==" -ForegroundColor Cyan
& $makensis "/V2" (Join-Path $root "installer\psp-connect.nsi")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setup = Join-Path $root "psp-connect-setup.exe"
if (-not (Test-Path $setup)) { Write-Error "Setup non genere." }

$item = Get-Item $setup
Write-Host ""
Write-Host "OK $($item.FullName)  ($([math]::Round($item.Length/1MB, 2)) Mo)" -ForegroundColor Green
