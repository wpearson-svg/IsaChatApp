@echo off
rem Starts ONLY Keycloak (port 8080). The voice and the helper services are separate.
cd /d "%~dp0"
docker compose up -d keycloak
echo.
echo Keycloak asked to start. It takes about a minute: http://localhost:8080
