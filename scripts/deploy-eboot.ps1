param(
    [Parameter(Mandatory = $true)]
    [string]$DriveRoot
)

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$srcDir = Join-Path $root "COPY-TO-MEMORY-STICK\PSP\GAME\psp-connect"
$eboot = Join-Path $srcDir "EBOOT.PBP"
$prx = Join-Path $srcDir "usb.prx"

if (-not (Test-Path $eboot) -or -not (Test-Path $prx)) {
    Write-Error "COPY-TO-MEMORY-STICK\PSP\GAME\psp-connect\ incomplet."
    exit 1
}

$destDir = Join-Path $DriveRoot "PSP\GAME\psp-connect"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item -Force $eboot (Join-Path $destDir "EBOOT.PBP")
Copy-Item -Force $prx (Join-Path $destDir "usb.prx")
Write-Host "Copie OK -> $destDir"
