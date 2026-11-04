@echo off
rem Stops ONLY the embeddings service. Frees its memory.
cd /d "%~dp0"
docker compose -f ai-services\docker-compose.yml stop embeddings
echo.
echo Embeddings stopped.
