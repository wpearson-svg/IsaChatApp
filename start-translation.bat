@echo off
rem Starts ONLY the translation service (Eliyas's NLLB-200, port 8091). Builds it the first time (slow, large download).
cd /d "%~dp0"
docker compose -f ai-services\docker-compose.yml up -d --build translation
echo.
echo Translation asked to start. Check: http://localhost:8091/health  (model_ready false until the model has loaded)
