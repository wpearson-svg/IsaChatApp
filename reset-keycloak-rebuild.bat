@echo off
cd /d "%~dp0"
echo Rebuilds the isa realm file, then resets Keycloak and re-imports it.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0reset-keycloak.ps1" -Rebuild
echo.
pause
