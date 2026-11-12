@echo off
rem Starts ONLY the embeddings service (Eliyas's multilingual-e5-base, port 8092).
rem Uses the image already built. Docker builds it only if it is missing (the first time: slow, about 3 GB to download).
rem After changing ai-services\embeddings\app.py, rebuild once with:
rem   docker compose -f ai-services\docker-compose.yml up -d --build embeddings
cd /d "%~dp0"
docker compose -f ai-services\docker-compose.yml up -d embeddings
echo.
echo Embeddings asked to start. Check: http://localhost:8092/health  (model_ready false until the model has loaded)
