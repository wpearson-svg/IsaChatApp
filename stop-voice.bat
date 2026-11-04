@echo off
rem Stops ONLY the voice. Frees its memory. Listen falls back to the device voice.
cd /d "%~dp0"
docker compose stop kokoro
echo.
echo Voice stopped.
