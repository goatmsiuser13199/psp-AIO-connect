$ErrorActionPreference = "Stop"
$pack = Join-Path $PSScriptRoot "COPY-TO-MEMORY-STICK"
$eboot = Join-Path $pack "PSP\GAME\psp-connect\EBOOT.PBP"
$prx = Join-Path $pack "PSP\GAME\psp-connect\usb.prx"

if (-not (Test-Path $eboot) -or -not (Test-Path $prx)) {
    Write-Host "Dossier COPY-TO-MEMORY-STICK incomplet."
    exit 1
}

$candidates = Get-CimInstance Win32_LogicalDisk |
    Where-Object { $_.DriveType -eq 2 -or ($_.DriveType -eq 3 -and $_.DeviceID -ne 'C:' -and $_.DeviceID -ne 'D:') }

$stick = $null
foreach ($disk in $candidates) {
    $root = "$($disk.DeviceID)\"
    if ((Test-Path (Join-Path $root "PSP\GAME")) -or (Test-Path (Join-Path $root "PSP")) -or (Test-Path (Join-Path $root "ISO"))) {
        $stick = $disk.DeviceID
        break
    }
}

if (-not $stick) {
    Write-Host "Aucune Memory Stick PSP detectee (dossier PSP\GAME)."
    Write-Host "Branche la stick (mode USB PSP ou lecteur), puis relance."
    exit 2
}

$dest = Join-Path "$stick\" "PSP\GAME\psp-connect"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item -Force $eboot (Join-Path $dest "EBOOT.PBP")
Copy-Item -Force $prx (Join-Path $dest "usb.prx")
Write-Host "OK -> $dest"
exit 0
