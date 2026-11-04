#!/usr/bin/env python3
"""Build data/vectors.db: one vector per approved passage in data/content.db (search by meaning).

Needs the embedding service running (ai-services/docker-compose.yml, port 8092). Safe to re-run:
it replaces everything in the passage_vectors table. Run it again whenever content.db is rebuilt
(content\\build_content_db.py), because the vectors are matched to passages by their id.

    python content\\build_vectors.py
    python content\\build_vectors.py --url http://localhost:8092 --content data\\content.db --out data\\vectors.db

Only passages that are in content.db are embedded, so only approved content can be searched.
Uses only the Python standard library.
"""
import argparse, array, json, os, sqlite3, sys, urllib.request, urllib.error

here = os.path.dirname(os.path.abspath(__file__))
ap = argparse.ArgumentParser()
ap.add_argument("--url", default="http://localhost:8092")
ap.add_argument("--content", default=os.path.join(here, "..", "data", "content.db"))
ap.add_argument("--out", default=os.path.join(here, "..", "data", "vectors.db"))
ap.add_argument("--batch", type=int, default=16)
args = ap.parse_args()


def embed(texts):
    req = urllib.request.Request(
        args.url.rstrip("/") + "/embed",
        data=json.dumps({"texts": texts, "mode": "passage"}).encode("utf-8"),
        headers={"Content-Type": "application/json"},
    )
    try:
        with urllib.request.urlopen(req, timeout=300) as r:
            body = json.load(r)
    except (urllib.error.URLError, OSError) as ex:
        sys.exit(f"The embedding service at {args.url} did not answer ({ex}). Start it first: docker compose up -d in ai-services.")
    if not body.get("vectors"):
        sys.exit("The embedding service answered but its model is not ready yet (it downloads on the first start). Wait a minute and run this again.")
    return body["vectors"], body.get("model_id", "")


if not os.path.exists(args.content):
    sys.exit(f"{args.content} not found. Build it first: python content\\build_content_db.py")

src = sqlite3.connect(f"file:{args.content}?mode=ro", uri=True)
rows = src.execute("SELECT id, document, section, page, text FROM passages ORDER BY id").fetchall()
src.close()
if not rows:
    sys.exit("content.db has no passages.")

os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
out = sqlite3.connect(args.out)
out.execute(
    "CREATE TABLE IF NOT EXISTS passage_vectors ("
    "passage_id INTEGER PRIMARY KEY, document TEXT, section TEXT, page TEXT, "
    "model_id TEXT, dims INTEGER, embedding BLOB)"
)
out.execute("DELETE FROM passage_vectors")

done = 0
for i in range(0, len(rows), args.batch):
    chunk = rows[i:i + args.batch]
    vectors, model_id = embed([r[4] for r in chunk])
    if len(vectors) != len(chunk):
        sys.exit("The embedding service returned a different number of vectors than passages sent.")
    for r, v in zip(chunk, vectors):
        out.execute(
            "INSERT INTO passage_vectors VALUES (?,?,?,?,?,?,?)",
            (r[0], r[1], r[2], r[3], model_id, len(v), array.array("f", v).tobytes()),
        )
    done += len(chunk)
    print(f"  {done}/{len(rows)} passages embedded")
out.commit()
out.close()
print(f"built {args.out}: {done} vectors ({len(vectors[0])} dimensions, model {model_id})")
