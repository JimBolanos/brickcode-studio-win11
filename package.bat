@echo off
rem ---------------------------------------------------------------------------
rem  Builds the zip that goes on a GitHub release: the whole ready-to-run
rem  folder, exactly as someone downloads it.
rem
rem  It refuses to zip an exe that is older than the sources. 2.1.2 went out
rem  with a packaged exe one build behind its own code, which is the kind of
rem  thing nobody notices until someone asks why a menu entry is missing.
rem ---------------------------------------------------------------------------
setlocal

set EXE=package\BrickCodeStudio\BrickCodeStudio.exe
set ZIP=BrickCodeStudio-Windows-completo.zip

if not exist "%EXE%" (
  echo.
  echo   %EXE% is not there.
  echo   Run build.bat first.
  echo.
  pause
  exit /b 1
)

powershell -NoProfile -Command ^
  "$exe = (Get-Item '%EXE%').LastWriteTime;" ^
  "$new = Get-ChildItem src\*.cs ^| Where-Object { $_.LastWriteTime -gt $exe };" ^
  "if ($new) { Write-Host ''; Write-Host '  Newer than the packaged exe:';" ^
  "  $new ^| ForEach-Object { Write-Host ('    src\' + $_.Name) };" ^
  "  Write-Host ''; Write-Host '  Run build.bat first.'; Write-Host ''; exit 1 }"
if errorlevel 1 (
  pause
  exit /b 1
)

if exist "%ZIP%" del /q "%ZIP%"

echo Building %ZIP% ...
powershell -NoProfile -Command ^
  "Compress-Archive -Path 'package\BrickCodeStudio' -DestinationPath '%ZIP%' -CompressionLevel Optimal"
if errorlevel 1 (
  echo.
  echo   Zip failed.
  echo.
  pause
  exit /b 1
)

powershell -NoProfile -Command ^
  "$z = Get-Item '%ZIP%';" ^
  "Write-Host ('  Done: ' + $z.Name + '  ' + [math]::Round($z.Length / 1MB, 2) + ' MB')"

echo.
echo   That is the file to attach to the GitHub release.
echo.
pause
