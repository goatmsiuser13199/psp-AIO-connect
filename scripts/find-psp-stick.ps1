$candidates = Get-CimInstance Win32_LogicalDisk |
    Where-Object { $_.DriveType -eq 2 -or ($_.DriveType -eq 3 -and $_.DeviceID -ne 'C:' -and $_.DeviceID -ne 'D:') }

foreach ($disk in $candidates) {
    $root = "$($disk.DeviceID)\"
    $pspGame = Join-Path $root "PSP\GAME"
    $iso = Join-Path $root "ISO"
    if ((Test-Path $pspGame) -or (Test-Path $iso) -or (Test-Path (Join-Path $root "PSP"))) {
        [PSCustomObject]@{
            Drive = $disk.DeviceID
            Volume = $disk.VolumeName
            PspGame = $pspGame
        }
    }
}
