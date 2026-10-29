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

echo.
echo   Done: dist\BrickCodeStudio.exe
echo.
echo   Put nqc.exe next to it, or anywhere on your PATH.
echo.
pause
