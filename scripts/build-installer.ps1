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

$stickGame = Join-Path $root "COPY-TO-MEMORY-STICK\PSP\GAME\psp-connect"
$eboot = Join-Path $stickGame "EBOOT.PBP"
$prx = Join-Path $stickGame "usb.prx"
if (-not (Test-Path $eboot) -or -not (Test-Path $prx)) {
    Write-Error "COPY-TO-MEMORY-STICK\PSP\GAME\psp-connect\ (EBOOT.PBP + usb.prx) requis."
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
$payloadStick = Join-Path $root "installer\payload\COPY-TO-MEMORY-STICK"
Remove-Item -Recurse -Force $payloadHost, $payloadStick -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $payloadHost | Out-Null

Copy-Item -Recurse -Force (Join-Path $publish "*") $payloadHost
Remove-Item (Join-Path $payloadHost "*.pdb") -ErrorAction SilentlyContinue
Copy-Item -Recurse -Force (Join-Path $root "COPY-TO-MEMORY-STICK") $payloadStick

Write-Host "== NSIS ==" -ForegroundColor Cyan
& $makensis "/V2" (Join-Path $root "installer\psp-connect.nsi")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setup = Join-Path $root "psp-connect-setup.exe"
if (-not (Test-Path $setup)) { Write-Error "Setup non genere." }

$item = Get-Item $setup
Write-Host ""
Write-Host "OK $($item.FullName)  ($([math]::Round($item.Length/1MB, 2)) Mo)" -ForegroundColor Green
