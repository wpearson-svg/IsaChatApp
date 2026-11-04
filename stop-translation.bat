@echo off
rem Stops ONLY the translation service. Frees its memory.
cd /d "%~dp0"
docker compose -f ai-services\docker-compose.yml stop translation
echo.
echo Translation stopped.
