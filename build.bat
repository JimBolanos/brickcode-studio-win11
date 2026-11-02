@echo off
rem ---------------------------------------------------------------------------
rem  Builds BrickCode Studio (Windows edition) with the C# compiler that ships
rem  with the .NET Framework. Nothing else to install.
rem ---------------------------------------------------------------------------
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist "%CSC%" (
  echo.
  echo   The .NET Framework C# compiler was not found.
  echo   Looked for: %WINDIR%\Microsoft.NET\Framework[64]\v4.0.30319\csc.exe
  echo.
  echo   Install the .NET Framework 4.x developer pack, or open this folder
  echo   in Visual Studio instead.
  echo.
  pause
  exit /b 1
)

if not exist dist mkdir dist

echo Building BrickCode Studio...
"%CSC%" /nologo /target:winexe /optimize+ ^
  /out:dist\BrickCodeStudio.exe ^
  /win32icon:assets\brickcode.ico ^
  /reference:System.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  src\*.cs

if errorlevel 1 (
  echo.
  echo   Build failed.
  pause
  exit /b 1
)

rem ---------------------------------------------------------------------------
rem  The ready-to-run folder carries its own copy of the exe, and 2.1.2 shipped
rem  with that copy left behind one build. Refresh it here so it cannot drift
rem  from the sources again.
rem ---------------------------------------------------------------------------
if exist package\BrickCodeStudio (
  copy /y dist\BrickCodeStudio.exe package\BrickCodeStudio\BrickCodeStudio.exe >nul
  if errorlevel 1 (
    echo.
    echo   Built, but could not refresh package\BrickCodeStudio\BrickCodeStudio.exe
    echo   Close the program if it is running, then run this again.
    echo.
    pause
    exit /b 1
  )
  echo   Refreshed: package\BrickCodeStudio\BrickCodeStudio.exe
)

echo.
echo   Done: dist\BrickCodeStudio.exe
echo.
echo   Put nqc.exe next to it, or anywhere on your PATH.
echo.
echo   To build the zip for a release, run:  package.bat
echo.
pause
