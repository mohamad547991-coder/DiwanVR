@echo off
setlocal
cd /d "%~dp0"
set "DiwanPowerShell=powershell.exe"
where pwsh.exe >nul 2>&1
if not errorlevel 1 set "DiwanPowerShell=pwsh.exe"
"%DiwanPowerShell%" -NoProfile -File "%~dp0Test_Unity_0.4.4.ps1" -Batch
if errorlevel 1 goto failed
"%DiwanPowerShell%" -NoProfile -File "%~dp0Build_Quest_0.4.4.ps1"
if errorlevel 1 goto failed
echo Verified APK: %~dp0Install\DiwanVR_0.4.4_Quest.apk
pause
exit /b 0
:failed
echo Validation or build failed. Read the reported log; no successful release is claimed.
pause
exit /b 1
