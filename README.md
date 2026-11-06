# Semantic Search POC

Proves Option A from the spec: file content embedded in-memory → float[] vectors stored in pgvector → natural language search returns file metadata only. No content is ever persisted.

## Prerequisites

- Docker Desktop
- .NET 10 SDK
- Node.js 20+

## Quick Start

### Fastest path: `start.sh`

`start.sh` brings up Docker infra (Postgres + Ollama), pulls the embedding model if
needed, then runs whichever services you ask for. Default is everything:

```bash
./start.sh              # same as: ./start.sh all
```

Pass a mode as the first argument to run a subset:

```bash
./start.sh all          # Worker + API + Web UI (default)
./start.sh worker       # Worker only — indexes then exits
./start.sh api          # API only
./start.sh worker-api   # Worker + API, no UI
./start.sh ui-api       # Web UI + API, no Worker
```

Logs stream to `.logs/{worker,api,web}.log`. Ctrl+C stops whichever services were
started (Docker infra keeps running). The manual step-by-step walkthrough below is
useful if you want to run/debug a single service directly with `dotnet run` / `npm run dev`
instead of through the script.

### 1. Start infrastructure

```bash
docker compose up -d
```

Then pull the embedding model (first time only, ~270 MB):

```bash
docker exec -it semantic-search-poc-ollama-1 ollama pull nomic-embed-text
```

Confirm Postgres is ready:

```bash
docker exec -it semantic-search-poc-postgres-1 psql -U postgres -d semantic_search -c "SELECT COUNT(*) FROM file_chunks;"
```

### 2. Point the worker at your document source

By default the worker reads straight from the AjourBox storage path configured in
`IndexingFolder` (see Configuration below) — no manual file transfer is needed. All
existing documents in that folder are picked up automatically the first time the worker
runs.

For local demo/dev without a live AjourBox mount, you can instead point `IndexingFolder`
at `data/sample-files/` (bundled PDFs/DOCX). Files whose filenames don't hint at their
content work best for demonstrating semantic advantage (e.g. `Report_Final_v3.pdf`
containing "risk mitigation").

### 3. Run the indexing worker

```bash
cd src/SemanticSearch.Worker
dotnet run
```

The worker scans the configured `IndexingFolder`, extracts text in-memory, chunks at ~400 words with 10% overlap, embeds each chunk via Ollama, and stores only the float[768] vector in Postgres. Watch the log for `"Indexed X: N chunks"`.

### 4. Run the API

```bash
cd src/SemanticSearch.Api
dotnet run
```

API listens on `http://localhost:5050`. Test:

```bash
curl "http://localhost:5050/api/search?query=risk+mitigation"
```

### 5. Run the React UI

```bash
cd src/SemanticSearch.Web
npm run dev
```

Open `http://localhost:5173`. Search using natural language — e.g. "employee compensation policy" — and observe that files with no filename match still surface via semantic similarity.

## Resetting the index

Use `reset.sh` when you want to wipe the search index and re-index from scratch.

```bash
./reset.sh          # soft reset (default)
./reset.sh soft     # same — truncates index tables, containers stay up
./reset.sh hard     # full wipe — destroys Docker volumes, requires model re-download
```

| Mode | What it removes | Containers | Ollama model |
|------|----------------|------------|--------------|
| `soft` | Vectors + file metadata (DB tables only) | Stay up | Preserved |
| `hard` | All Docker volumes (`pgdata` + `ollama_models`) | Stopped | Re-downloaded on next `./start.sh` |

**Soft reset** is the right choice when you just want to re-index (e.g. after changing files or config). Postgres stays running and the Ollama model is kept, so re-indexing starts immediately:

```bash
./reset.sh
dotnet run --project src/SemanticSearch.Worker
```

**Hard reset** tears everything down cleanly. Use it when you need a completely fresh environment or suspect volume corruption:

```bash
./reset.sh hard
./start.sh          # re-pulls Ollama model (~270 MB) then starts all services
```

## Accuracy Demo

Run the four queries from the spec against keyword-only vs semantic:

| Query | Keyword | Semantic |
|---|---|---|
| `Q3 invoices` against `Invoice_Q3_2024.pdf` | ✅ | ✅ |
| `risk mitigation` against `Report_Final_v3.pdf` | ❌ | ✅ |
| `employee compensation policy` (no filename hint) | ❌ | ✅ |
| Scanned image | ❌ | ⚠️ OCR not in POC scope |

## Architecture

```
data/sample-files/  →  Worker (extract+chunk+embed)  →  pgvector
                                                             ↓
                                              API (embed query + cosine search)
                                                             ↓
                                              React UI (result cards)
```

See `docs/specs/semantic-search-option-a.md` for the full compliance and architecture spec.

## Compliance guarantees

- No file content is written to disk or database — only float[] vectors
- Ollama runs on-premise; nothing leaves the server
- Schema has no `text`/`content` column by design

## Configuration

All settings in `appsettings.json` in each project:

| Key | Default | Description |
|---|---|---|
| `ConnectionStrings:Postgres` | `Host=localhost;Port=5432;...` | pgvector connection |
| `OllamaBaseUrl` | `http://localhost:11434` | Ollama REST API |
| `IndexingFolder` | `C:\ajourweb-development\ajourbox\files` | AjourBox storage folder to bulk-index. The worker reads files directly from this path — set it to `data/sample-files` for local demo/dev without a live AjourBox mount. |
