$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$exe = Join-Path $root "publish\host\PspConnect.Host.exe"

if (-not (Test-Path $exe)) {
    Write-Host "Publication de l'app..."
    Get-Process PspConnect.Host -ErrorAction SilentlyContinue | Stop-Process -Force
    dotnet publish (Join-Path $root "host\PspConnect.Host\PspConnect.Host.csproj") `
        -c Release -r win-x64 --self-contained false `
        -p:PublishSingleFile=true -o (Join-Path $root "publish\host") | Out-Host
}

$desktop = [Environment]::GetFolderPath("Desktop")
$lnkPath = Join-Path $desktop "psp-connect.lnk"

$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($lnkPath)
$ico = Join-Path $root "host\PspConnect.Host\Assets\app.ico"
$lnk.TargetPath = $exe
$lnk.WorkingDirectory = Split-Path $exe -Parent
$lnk.WindowStyle = 1
$lnk.Description = "psp-connect - panneau AIO PSP (pas un moniteur Windows)"
if (Test-Path $ico) { $lnk.IconLocation = "$ico,0" }
$lnk.Save()

Write-Host "Raccourci cree: $lnkPath"
Write-Host "Double-clic sur 'psp-connect' sur le Bureau pour lancer."
