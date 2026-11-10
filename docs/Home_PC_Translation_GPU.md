# For Claude on the home PC: put the translation service on the graphics card

From Willis's laptop session, November 10, 2026. Repo on this PC: `D:\Repos\IsaChatApp`.

## The goal

Isa's translation service (NLLB-200, port 8091, `ai-services/translation`) should run on this PC's NVIDIA graphics card instead of the CPU. On the CPU a Spanish reply takes 12 to 17 seconds to translate, so Spanish questions take about 20 seconds.

The change is already written, committed and pushed by Willis: commit `7a9cac0` "Translation cuda fix" (on Willis-Pearson and main).
- `ai-services/docker-compose.yml`: the `translation` service gets `DEVICE: cuda` and a `deploy.resources.reservations.devices` entry for the NVIDIA card.
- `ai-services/translation/app.py`: runs on the card only if one is visible in the container, otherwise on the CPU, and logs `[translation] model ready on cuda` (or `cpu`). `/health` now includes `"device"`.

**The problem:** Willis rebuilt the service, but `http://localhost:8091/health` still has no `"device"` field. So whatever answers on port 8091 is still the old code.

## What to do

1. **Check the code on disk is the new version.**
   ```
   git log -1 --oneline -- ai-services/translation/app.py
   ```
   Expect `7a9cac0 Translation cuda fix`, and `grep -n "_device" ai-services/translation/app.py` should find lines. If not, tell Willis the PC needs to pull (or is on a branch without the commit). Do not pull, commit, merge or switch branches yourself: Git is Willis's job only.

2. **Find out what is serving port 8091.**
   ```
   docker ps -a --format "{{.Names}} | {{.Image}} | {{.Status}} | {{.Ports}} | {{.Label \"com.docker.compose.project\"}}"
   ```
   The service should be the compose project `ai-services`, container like `ai-services-translation-1`, created minutes ago. If another container (a different compose project, or an old one "Up" for hours or days) holds 8091, that one is answering and the rebuilt one could not take the port. Stop only that old translation container (`docker stop <name>`); do not remove volumes.

3. **Rebuild and start only the translation service.**
   ```
   docker compose -f ai-services/docker-compose.yml up -d --build translation
   ```
   Only `app.py` changed, so this should be quick. If Docker's build cache is gone it downloads about 3 GB again; let it finish.

4. **Check it runs on the card.**
   - `docker compose -f ai-services/docker-compose.yml logs --tail 50 translation` shows `[translation] model ready on cuda`.
   - `http://localhost:8091/health` shows `"device": "cuda"` and `"model_ready": true`.
   - If the log says `DEVICE=cuda but no graphics card is visible in the container: running on the CPU`, Docker can't see the card. Check that Docker Desktop uses the WSL 2 backend and that `docker run --rm --gpus all nvidia/cuda:12.4.1-base-ubuntu22.04 nvidia-smi` shows the card. Report what you find to Willis rather than changing Docker Desktop's settings.

5. **Check Ollama still has its share of the card.**
   - `nvidia-smi`: note total and used memory. Both the translation model (half precision, about 1.3 GB) and Ollama's phi4-mini should be on it.
   - `http://localhost:11434/api/ps`: `phi4-mini:3.8b` should still show `size_vram` about 2.4 GB (2409155788 before the change). If it dropped, the card is too small for both, and AI answers will slow down. In that case set `DEVICE: cpu` back in `ai-services/docker-compose.yml`, rerun step 3, and tell Willis (do not commit).

6. **Quick translation test.**
   ```
   curl -s -X POST http://localhost:8091/translate -H "Content-Type: application/json" -d "{\"text\":\"The outgoing president should transfer relevant records, documentation, credentials, and institutional knowledge to the incoming president.\",\"source_lang\":\"eng\",\"target_lang\":\"spa\"}"
   ```
   It should come back in Spanish, `"fallback": false`, in about a second or two (it took several seconds on the CPU).

## Rules

- Never commit, push, merge, pull or switch branches. Willis does all Git.
- Never run `docker compose down -v` or the Keycloak reset scripts: they wipe data.
- Don't start or restart Docker Desktop from this session (a Claude app update once closed Docker along with the session that had started it). If Docker isn't running, ask Willis to start it.
- Don't touch the other containers (Keycloak, the voice, embeddings, Ollama) beyond reading their status.

## What to report back

- Which of steps 1 and 2 was the cause.
- The `/health` output after the rebuild.
- `nvidia-smi` memory before and after, and Ollama's `size_vram`.
- The time of the step 6 test.

Willis's laptop session will then time the Spanish demo questions end to end.
