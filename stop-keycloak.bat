@echo off
rem Stops ONLY Keycloak. Its data is kept. The voice keeps running.
cd /d "%~dp0"
docker compose stop keycloak
echo.
echo Keycloak stopped.
