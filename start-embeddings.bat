@echo off
rem Starts ONLY the embeddings service (Eliyas's multilingual-e5-base, port 8092). Builds it the first time (slow, large download).
cd /d "%~dp0"
docker compose -f ai-services\docker-compose.yml up -d --build embeddings
echo.
echo Embeddings asked to start. Check: http://localhost:8092/health  (model_ready false until the model has loaded)
