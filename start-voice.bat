@echo off
rem Starts ONLY Isa's spoken voice (Kokoro, port 8880). The first start downloads the voice model.
cd /d "%~dp0"
docker compose up -d kokoro
echo.
echo Voice asked to start: http://localhost:8880
