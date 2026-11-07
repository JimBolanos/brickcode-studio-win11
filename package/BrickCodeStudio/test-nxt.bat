@echo off
rem ---------------------------------------------------------------------------
rem  NXT test for Windows. Run it with the NXT switched on and plugged in by USB.
rem  Step 1 asks the NXT who it is, step 2 beeps, step 3 sends nxt-test\hello.rxe
rem  and starts it, step 4 (only if nbc.exe is in this folder) compiles
rem  nxt-test\hello.nxc with it first.
rem ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0"

set PY=
where py >nul 2>nul && set PY=py
if "%PY%"=="" where python >nul 2>nul && set PY=python
if "%PY%"=="" (
  echo Python 3 was not found. Install it from the Microsoft Store ^(Python 3.13^).
  pause
  exit /b 1
)

echo.
echo === 1. Is the NXT there? ===
%PY% nxt_tool.py info
if errorlevel 1 goto failed

echo.
echo === 2. Beep ===
%PY% nxt_tool.py beep
if errorlevel 1 goto failed

if exist nbc.exe (
  echo.
  echo === 3. Compile hello.nxc with nbc.exe ===
  nbc.exe nxt-test\hello.nxc -O=nxt-test\hello.rxe
  if errorlevel 1 goto failed
)

echo.
echo === 4. Send hello.rxe and run it ===
%PY% nxt_tool.py upload nxt-test\hello.rxe run
if errorlevel 1 goto failed

echo.
echo ALL GOOD: the screen should say "Hello from Windows!" and you should hear two beeps.
pause
exit /b 0

:failed
echo.
echo SOMETHING FAILED. Copy everything above and send it to Claude.
pause
exit /b 1
