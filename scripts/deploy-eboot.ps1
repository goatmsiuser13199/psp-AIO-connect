param(
    [Parameter(Mandatory = $true)]
    [string]$DriveRoot
)

$src = Join-Path $PSScriptRoot "..\psp\EBOOT.PBP"
$src = [System.IO.Path]::GetFullPath($src)
if (-not (Test-Path $src)) {
    Write-Error "EBOOT.PBP introuvable. Build d'abord (scripts/build-psp.sh)."
    exit 1
}

$destDir = Join-Path $DriveRoot "PSP\GAME\psp-connect"
New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item -Force $src (Join-Path $destDir "EBOOT.PBP")
Write-Host "Copie OK -> $destDir\EBOOT.PBP"
