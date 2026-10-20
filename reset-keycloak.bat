@echo off
cd /d "%~dp0"
echo Resets Keycloak and re-imports the isa realm from keycloak\import (no rebuild).
echo To rebuild the realm file first, run reset-keycloak-rebuild.bat.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0reset-keycloak.ps1"
echo.
pause
