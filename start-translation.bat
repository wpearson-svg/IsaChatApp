@echo off
rem Starts ONLY the translation service (Eliyas's NLLB-200, port 8091).
rem Uses the image already built. Docker builds it only if it is missing (the first time: slow, about 3 GB to download).
rem After changing ai-services\translation\app.py, rebuild once with:
rem   docker compose -f ai-services\docker-compose.yml up -d --build translation
cd /d "%~dp0"
docker compose -f ai-services\docker-compose.yml up -d translation
echo.
echo Translation asked to start. Check: http://localhost:8091/health  (model_ready false until the model has loaded)
