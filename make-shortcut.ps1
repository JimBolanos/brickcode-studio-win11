# Puts a BrickCode Studio shortcut on the desktop.
# Run by build.bat; it can also be run by hand:
#   powershell -ExecutionPolicy Bypass -File make-shortcut.ps1 -Target path\to\BrickCodeStudio.exe
param(
    [Parameter(Mandatory = $true)][string]$Target
)

$ErrorActionPreference = 'Stop'
try {
    $exe = (Resolve-Path -LiteralPath $Target).Path
    # GetFolderPath follows a desktop that OneDrive has moved.
    $desktop = [Environment]::GetFolderPath('Desktop')
    $link = Join-Path $desktop 'BrickCode Studio.lnk'

    $shell = New-Object -ComObject WScript.Shell
    $sc = $shell.CreateShortcut($link)
    $sc.TargetPath = $exe
    # The program looks for nqc, nbc, the python tools and the block editors
    # beside the exe, so it has to start in its own folder.
    $sc.WorkingDirectory = Split-Path -Parent $exe
    $sc.IconLocation = "$exe,0"
    $sc.Description = 'BrickCode Studio - RCX, Scout, NXT and EV3'
    $sc.Save()
    Write-Host "  Desktop shortcut: $link"
}
catch {
    Write-Host "  Could not create the desktop shortcut: $($_.Exception.Message)"
    exit 1
}
